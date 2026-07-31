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
        await StoreAsync(fixture.SpaceId, "archive/one", Passport, Lease);

        var matches = await SearchAsync(fixture.SpaceId, "когда выдан паспорт", minScore: 0);

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
        await StoreAsync(fixture.SpaceId, "archive/two", Passport, Lease);

        // Порог отсекает всё: иначе поиск всегда возвращал бы topK строк, даже когда
        // в архиве нет ничего похожего на вопрос.
        var matches = await SearchAsync(fixture.SpaceId, "совершенно посторонний вопрос", minScore: 0.99);

        matches.ShouldBeEmpty();
    }

    [Fact]
    public async Task Search_returns_no_more_than_top_k()
    {
        await fixture.ResetAsync(Ct);
        await StoreAsync(fixture.SpaceId, "archive/three", Passport, Lease, "справка из банка о состоянии счёта");

        var repository = Repository(out var scope);
        using (scope)
        {
            var matches = await repository.SearchAsync(
                fixture.SpaceId,
                FakeEmbeddingGenerator.Vectorize("паспорт", PgVectorFixture.Dimensions),
                topK: 2,
                minScore: 0,
                Ct);

            matches.Count.ShouldBe(2);
        }
    }

    [Fact]
    public async Task A_chunk_of_another_space_is_never_returned()
    {
        await fixture.ResetAsync(Ct);

        // Один и тот же текст лежит в двух space. Изоляция арендатора — ключевое требование фичи:
        // из своего space чужой фрагмент не виден даже при полном совпадении с вопросом.
        await StoreAsync(fixture.ForeignSpaceId, "archive/foreign", Passport);

        var matches = await SearchAsync(fixture.SpaceId, "когда выдан паспорт", minScore: 0);

        matches.ShouldBeEmpty();
    }

    [Fact]
    public async Task Spaces_do_not_see_each_others_chunks_even_with_the_same_source_key()
    {
        await fixture.ResetAsync(Ct);

        // Локатор источника уникален внутри space, а не глобально: совпадение ключей в двух
        // space — обычное дело, и оно не должно ни ломать вставку, ни смешивать выдачу.
        await StoreAsync(fixture.SpaceId, "archive/shared", Passport);
        await StoreAsync(fixture.ForeignSpaceId, "archive/shared", Lease);

        var mine = await SearchAsync(fixture.SpaceId, "паспорт", minScore: 0);
        var foreign = await SearchAsync(fixture.ForeignSpaceId, "паспорт", minScore: 0);

        mine.ShouldHaveSingleItem().Content.ShouldBe(Passport);
        foreign.ShouldHaveSingleItem().Content.ShouldBe(Lease);
    }

    [Fact]
    public async Task Replace_removes_the_previous_version_of_the_source()
    {
        await fixture.ResetAsync(Ct);
        await StoreAsync(fixture.SpaceId, "archive/four", Passport, Lease);

        await StoreAsync(fixture.SpaceId, "archive/four", "полностью новый текст источника");

        (await fixture.CountChunksAsync(fixture.SpaceId, "archive/four", Ct)).ShouldBe(1);

        var matches = await SearchAsync(fixture.SpaceId, "паспорт", minScore: 0);
        matches.ShouldNotContain(match => match.Content == Passport);
    }

    [Fact]
    public async Task Replace_does_not_touch_the_same_source_in_another_space()
    {
        await fixture.ResetAsync(Ct);
        await StoreAsync(fixture.SpaceId, "archive/parallel", Passport);
        await StoreAsync(fixture.ForeignSpaceId, "archive/parallel", Lease);

        await StoreAsync(fixture.SpaceId, "archive/parallel", "новая версия своего источника");

        // Переиндексация своего источника не имеет права стереть чужой с тем же ключом.
        (await fixture.CountChunksAsync(fixture.ForeignSpaceId, "archive/parallel", Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Deleting_a_source_is_idempotent()
    {
        await fixture.ResetAsync(Ct);
        await StoreAsync(fixture.SpaceId, "archive/five", Passport);

        var repository = Repository(out var scope);
        using (scope)
        {
            (await repository.DeleteBySourceAsync(fixture.SpaceId, "archive/five", Ct)).ShouldBe(1);
            (await repository.DeleteBySourceAsync(fixture.SpaceId, "archive/five", Ct)).ShouldBe(0);
        }

        (await fixture.CountChunksAsync(fixture.SpaceId, "archive/five", Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task Deleting_a_source_of_another_space_deletes_nothing()
    {
        await fixture.ResetAsync(Ct);
        await StoreAsync(fixture.ForeignSpaceId, "archive/six", Passport);

        var repository = Repository(out var scope);
        using (scope)
        {
            (await repository.DeleteBySourceAsync(fixture.SpaceId, "archive/six", Ct)).ShouldBe(0);
        }

        (await fixture.CountChunksAsync(fixture.ForeignSpaceId, "archive/six", Ct)).ShouldBe(1);
    }

    private async Task StoreAsync(Guid spaceId, string sourceKey, params string[] contents)
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
            await repository.ReplaceAsync(spaceId, sourceKey, chunks, "fake-embed", Ct);
        }
    }

    private async Task<IReadOnlyList<ChunkMatch>> SearchAsync(Guid spaceId, string question, double minScore)
    {
        var repository = Repository(out var scope);
        using (scope)
        {
            return await repository.SearchAsync(
                spaceId,
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
