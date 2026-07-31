using Dapper;
using DocsFlow.Database;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace DocsFlow.Rag.Tests;

public sealed class DocumentIndexerTests(PgVectorFixture fixture) : IClassFixture<PgVectorFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_long_text_is_indexed_as_several_chunks()
    {
        await fixture.ResetAsync(Ct);

        var text = string.Join(" ", Enumerable.Range(0, 600).Select(i => $"слово{i}"));

        using var scope = fixture.Services.CreateScope();
        var count = await scope.ServiceProvider
            .GetRequiredService<IDocumentIndexer>()
            .IndexAsync(fixture.SpaceId, "archive/long", text, Ct);

        count.ShouldBeGreaterThan(1);
        (await fixture.CountChunksAsync(fixture.SpaceId, "archive/long", Ct)).ShouldBe(count);

        // Номера фрагментов идут подряд с нуля: по ним собирается ссылка на первоисточник.
        (await OrdinalsAsync("archive/long")).ShouldBe(Enumerable.Range(0, count).ToArray());
    }

    [Fact]
    public async Task Reindexing_replaces_the_previous_version()
    {
        await fixture.ResetAsync(Ct);

        using var scope = fixture.Services.CreateScope();
        var indexer = scope.ServiceProvider.GetRequiredService<IDocumentIndexer>();

        await indexer.IndexAsync(
            fixture.SpaceId,
            "archive/versioned",
            string.Join(" ", Enumerable.Range(0, 600).Select(i => $"слово{i}")),
            Ct);

        var count = await indexer.IndexAsync(fixture.SpaceId, "archive/versioned", "короткая новая версия", Ct);

        count.ShouldBe(1);
        (await fixture.CountChunksAsync(fixture.SpaceId, "archive/versioned", Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task An_empty_text_clears_the_index_of_the_source()
    {
        await fixture.ResetAsync(Ct);

        using var scope = fixture.Services.CreateScope();
        var indexer = scope.ServiceProvider.GetRequiredService<IDocumentIndexer>();

        await indexer.IndexAsync(fixture.SpaceId, "archive/emptied", "текст, который потом исчезнет", Ct);

        (await indexer.IndexAsync(fixture.SpaceId, "archive/emptied", "   ", Ct)).ShouldBe(0);
        (await fixture.CountChunksAsync(fixture.SpaceId, "archive/emptied", Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task The_embedding_model_is_stored_next_to_the_vector()
    {
        await fixture.ResetAsync(Ct);

        using var scope = fixture.Services.CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<IDocumentIndexer>()
            .IndexAsync(fixture.SpaceId, "archive/model", "паспорт выдан в 2019 году", Ct);

        // Пространства разных моделей несравнимы: без записи модели нельзя понять,
        // какие строки после её смены ещё не переиндексированы.
        (await ScalarAsync<string>("SELECT embedding_model FROM rag_chunks WHERE source_key = 'archive/model'"))
            .ShouldBe("fake-embed");
    }

    [Fact]
    public async Task A_dimension_mismatch_is_reported_clearly()
    {
        await fixture.ResetAsync(Ct);

        using var scope = fixture.Services.CreateScope();

        // Провайдер, который не умеет усекать вектор: колонка ждёт 1024, модель отдаёт 512.
        var indexer = CreateIndexer(scope, new FakeEmbeddingGenerator(512, honorRequestedDimensions: false));

        var error = await Should.ThrowAsync<RagException>(
            () => indexer.IndexAsync(fixture.SpaceId, "archive/mismatch", "текст источника", Ct));

        error.Message.ShouldContain("512");
        error.Message.ShouldContain("1024");
    }

    [Fact]
    public async Task A_failing_embedding_provider_is_not_swallowed()
    {
        await fixture.ResetAsync(Ct);

        using var scope = fixture.Services.CreateScope();
        var indexer = CreateIndexer(scope, new FailingEmbeddingGenerator());

        // Здесь деградировать нечем: без эмбеддингов документ не попадёт в индекс, и тихий
        // «успех» с пустым результатом был бы хуже явной ошибки.
        var error = await Should.ThrowAsync<RagException>(
            () => indexer.IndexAsync(fixture.SpaceId, "archive/failing", "текст источника", Ct));

        error.InnerException.ShouldBeOfType<InvalidOperationException>();
        (await fixture.CountChunksAsync(fixture.SpaceId, "archive/failing", Ct)).ShouldBe(0);
    }

    private static DocumentIndexer CreateIndexer(
        IServiceScope scope,
        IEmbeddingGenerator<string, Embedding<float>> generator)
        => new(
            generator,
            scope.ServiceProvider.GetRequiredService<IChunkRepository>(),
            Options.Create(new RagOptions()),
            NullLogger<DocumentIndexer>.Instance);

    private async Task<int[]> OrdinalsAsync(string sourceKey)
    {
        var factory = fixture.Services.GetRequiredService<IDbConnectionFactory>();
        await using var connection = await factory.OpenConnectionAsync(Ct);

        var ordinals = await connection.QueryAsync<int>(new CommandDefinition(
            "SELECT ordinal FROM rag_chunks WHERE source_key = @sourceKey ORDER BY ordinal",
            new { sourceKey },
            cancellationToken: Ct));

        return ordinals.ToArray();
    }

    private async Task<T?> ScalarAsync<T>(string sql)
    {
        var factory = fixture.Services.GetRequiredService<IDbConnectionFactory>();
        await using var connection = await factory.OpenConnectionAsync(Ct);

        return await connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, cancellationToken: Ct));
    }

    private sealed class FailingEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Провайдер эмбеддингов недоступен.");

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
