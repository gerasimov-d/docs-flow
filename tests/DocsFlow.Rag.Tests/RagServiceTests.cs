using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace DocsFlow.Rag.Tests;

public sealed class RagServiceTests(PgVectorFixture fixture) : IClassFixture<PgVectorFixture>
{
    private const string Question = "когда выдан паспорт";
    private const string PassportText = "паспорт выдан отделом МВД в 2019 году";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task An_answer_with_citations_is_returned()
    {
        await IndexAsync("archive/passport", PassportText);

        var chat = FakeChatClient.Returning("Паспорт выдан в 2019 году.", 1);
        var answer = await AskAsync(chat);

        answer.Status.ShouldBe(RagAnswerStatus.Answered);
        answer.Text.ShouldBe("Паспорт выдан в 2019 году.");

        var citation = answer.Citations.ShouldHaveSingleItem();
        citation.Number.ShouldBe(1);
        citation.SourceKey.ShouldBe("archive/passport");
        citation.Ordinal.ShouldBe(0);
        citation.Content.ShouldBe(PassportText);

        // В контекст модели ушёл сам фрагмент, а не только вопрос.
        chat.LastRequest.Last().Text.ShouldContain(PassportText);
    }

    [Fact]
    public async Task An_answer_without_citations_is_not_shown()
    {
        await IndexAsync("archive/passport", PassportText);

        var answer = await AskAsync(FakeChatClient.Returning("Паспорт выдан в 2019 году."));

        // Продуктовый принцип «никаких ответов без ссылки»: текст без подтверждения отбрасывается,
        // но найденные фрагменты пользователь всё равно получает.
        answer.Status.ShouldBe(RagAnswerStatus.NoGrounding);
        answer.Text.ShouldBeNull();
        answer.Citations.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Citations_pointing_outside_the_context_are_dropped()
    {
        await IndexAsync("archive/passport", PassportText);

        // Модель сослалась на фрагмент, которого ей не давали — такая ссылка ничего не подтверждает.
        var answer = await AskAsync(FakeChatClient.Returning("Паспорт выдан в 2019 году.", 42));

        answer.Status.ShouldBe(RagAnswerStatus.NoGrounding);
        answer.Text.ShouldBeNull();
    }

    [Fact]
    public async Task A_failing_provider_degrades_to_the_found_fragments()
    {
        await IndexAsync("archive/passport", PassportText);

        var answer = await AskAsync(FakeChatClient.Failing(new HttpRequestException("провайдер недоступен")));

        answer.Status.ShouldBe(RagAnswerStatus.GenerationUnavailable);
        answer.Text.ShouldBeNull();
        answer.Citations.ShouldNotBeEmpty();
        answer.Citations[0].Content.ShouldBe(PassportText);
    }

    [Fact]
    public async Task An_unparsable_response_degrades_to_the_found_fragments()
    {
        await IndexAsync("archive/passport", PassportText);

        var answer = await AskAsync(FakeChatClient.ReturningRaw("я отвечу просто текстом, без JSON"));

        answer.Status.ShouldBe(RagAnswerStatus.GenerationUnavailable);
        answer.Citations.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Without_a_chat_client_the_answer_is_the_fragments()
    {
        await IndexAsync("archive/passport", PassportText);

        var answer = await AskAsync(chatClient: null);

        answer.Status.ShouldBe(RagAnswerStatus.GenerationUnavailable);
        answer.Citations.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task A_document_of_another_space_never_reaches_the_model()
    {
        await IndexAsync(fixture.ForeignSpaceId, "archive/foreign", PassportText);

        var chat = FakeChatClient.Returning("Паспорт выдан в 2019 году.", 1);
        var answer = await AskAsync(fixture.SpaceId, chat);

        // Фрагмент чужого space не попадает ни в выдачу, ни в контекст модели: до генерации
        // дело не доходит вовсе, потому что искать в своём space нечего.
        answer.Status.ShouldBe(RagAnswerStatus.NothingFound);
        answer.Citations.ShouldBeEmpty();
        chat.LastRequest.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_empty_index_reports_nothing_found()
    {
        await fixture.ResetAsync(Ct);

        var answer = await AskAsync(FakeChatClient.Returning("Ответ", 1));

        answer.Status.ShouldBe(RagAnswerStatus.NothingFound);
        answer.Citations.ShouldBeEmpty();
    }

    [Fact]
    public void The_service_resolves_when_generation_is_disabled()
    {
        // В контейнере фикстуры IChatClient не зарегистрирован — ровно как при Llm:Chat:Enabled=false.
        using var scope = fixture.Services.CreateScope();

        Should.NotThrow(() => scope.ServiceProvider.GetRequiredService<IRagService>());
    }

    private Task IndexAsync(string sourceKey, string text) => IndexAsync(fixture.SpaceId, sourceKey, text);

    private async Task IndexAsync(Guid spaceId, string sourceKey, string text)
    {
        await fixture.ResetAsync(Ct);

        using var scope = fixture.Services.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IDocumentIndexer>()
            .IndexAsync(spaceId, sourceKey, text, Ct);
    }

    private Task<RagAnswer> AskAsync(IChatClient? chatClient) => AskAsync(fixture.SpaceId, chatClient);

    private async Task<RagAnswer> AskAsync(Guid spaceId, IChatClient? chatClient)
    {
        using var scope = fixture.Services.CreateScope();

        // Порог обнуляем: фейковые вектора считаются по совпадению слов, и здесь проверяется
        // поведение сервиса, а не качество ранжирования — за него отвечают тесты репозитория.
        var options = Options.Create(new RagOptions { MinScore = 0 });

        var service = new RagService(
            scope.ServiceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
            scope.ServiceProvider.GetRequiredService<IChunkRepository>(),
            options,
            NullLogger<RagService>.Instance,
            chatClient);

        return await service.AskAsync(spaceId, Question, Ct);
    }
}
