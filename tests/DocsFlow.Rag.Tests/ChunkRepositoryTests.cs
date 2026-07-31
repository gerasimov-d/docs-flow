using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DocsFlow.Rag.Tests;

public sealed class ChunkRepositoryTests(PgVectorFixture fixture) : IClassFixture<PgVectorFixture>
{
    private const string Passport = "паспорт выдан отделом МВД в 2019 году";
    private const string Lease = "договор аренды квартиры подписан в мае";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Search_ranks_the_closest_chunk_first()
    {
        await fixture.ResetAsync(Ct);
        await StoreAsync("archive/one", Passport, Lease);

        var matches = await SearchAsync("когда выдан паспорт", minScore: 0);

        matches.Count.ShouldBe(2);
        matches[0].Content.ShouldBe(Passport);
        matches[0].Ordinal.ShouldBe(0);
        matches[0].SourceKey.ShouldBe("archive/one");
        matches[0].Score.ShouldBeGreaterThan(matches[1].Score);
    }

    [Fact]
    public async Task Search_respects_the_score_threshold()
    {
        await fixture.ResetAsync(Ct);
        await StoreAsync("archive/two", Passport, Lease);

        // Порог отсекает всё: иначе поиск всегда возвращал бы topK строк, даже когда
        // в архиве нет ничего похожего на вопрос.
        var matches = await SearchAsync("совершенно посторонний вопрос", minScore: 0.99);

        matches.ShouldBeEmpty();
    }

    [Fact]
    public async Task Search_returns_no_more_than_top_k()
    {
        await fixture.ResetAsync(Ct);
        await StoreAsync("archive/three", Passport, Lease, "справка из банка о состоянии счёта");

        var repository = Repository(out var scope);
        using (scope)
        {
            var matches = await repository.SearchAsync(
                FakeEmbeddingGenerator.Vectorize("паспорт", PgVectorFixture.Dimensions),
                topK: 2,
                minScore: 0,
                Ct);

            matches.Count.ShouldBe(2);
        }
    }

    [Fact]
    public async Task Replace_removes_the_previous_version_of_the_source()
    {
        await fixture.ResetAsync(Ct);
        await StoreAsync("archive/four", Passport, Lease);

        await StoreAsync("archive/four", "полностью новый текст источника");

        (await fixture.CountChunksAsync("archive/four", Ct)).ShouldBe(1);

        var matches = await SearchAsync("паспорт", minScore: 0);
        matches.ShouldNotContain(match => match.Content == Passport);
    }

    [Fact]
    public async Task Deleting_a_source_is_idempotent()
    {
        await fixture.ResetAsync(Ct);
        await StoreAsync("archive/five", Passport);

        var repository = Repository(out var scope);
        using (scope)
        {
            (await repository.DeleteBySourceAsync("archive/five", Ct)).ShouldBe(1);
            (await repository.DeleteBySourceAsync("archive/five", Ct)).ShouldBe(0);
        }

        (await fixture.CountChunksAsync("archive/five", Ct)).ShouldBe(0);
    }

    private async Task StoreAsync(string sourceKey, params string[] contents)
    {
        var chunks = contents
            .Select((content, ordinal) => new ChunkEmbedding(
                ordinal,
                content,
                FakeEmbeddingGenerator.Vectorize(content, PgVectorFixture.Dimensions)))
            .ToArray();

        var repository = Repository(out var scope);
        using (scope)
        {
            await repository.ReplaceAsync(sourceKey, chunks, "fake-embed", Ct);
        }
    }

    private async Task<IReadOnlyList<ChunkMatch>> SearchAsync(string question, double minScore)
    {
        var repository = Repository(out var scope);
        using (scope)
        {
            return await repository.SearchAsync(
                FakeEmbeddingGenerator.Vectorize(question, PgVectorFixture.Dimensions),
                topK: 10,
                minScore,
                Ct);
        }
    }

    // Репозиторий scoped — на каждое обращение берётся свой scope, как это делает приложение.
    private IChunkRepository Repository(out IServiceScope scope)
    {
        scope = fixture.Services.CreateScope();

        return scope.ServiceProvider.GetRequiredService<IChunkRepository>();
    }
}
