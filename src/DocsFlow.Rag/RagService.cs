using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocsFlow.Rag;

internal sealed class RagService : IRagService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IChunkRepository _repository;
    private readonly RagOptions _options;
    private readonly ILogger<RagService> _logger;
    private readonly IChatClient? _chatClient;

    /// <param name="chatClient">
    /// Необязателен: при выключенной генерации (<c>Llm:Chat:Enabled=false</c>) клиента в контейнере
    /// нет, и сервис работает как поиск по смыслу.
    /// </param>
    public RagService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IChunkRepository repository,
        IOptions<RagOptions> options,
        ILogger<RagService> logger,
        IChatClient? chatClient = null)
    {
        _embeddingGenerator = embeddingGenerator;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
        _chatClient = chatClient;
    }

    public async Task<RagAnswer> AskAsync(
        Guid spaceId,
        string question,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        var queryVector = await EmbedQuestionAsync(question, cancellationToken);

        // Границы space задаются здесь и дальше по цепочке не теряются: в контекст модели уходят
        // только те фрагменты, что вернул поиск, а он за пределы space не выходит.
        var matches = await _repository.SearchAsync(
            spaceId,
            queryVector,
            _options.TopK,
            _options.MinScore,
            cancellationToken);

        if (matches.Count == 0)
        {
            return new RagAnswer(RagAnswerStatus.NothingFound, Text: null, Citations: []);
        }

        var found = ToCitations(matches);

        if (_chatClient is null)
        {
            _logger.LogInformation(
                "Генерация выключена: вопрос обслужен поиском, найдено {Count} фрагментов.",
                matches.Count);

            return new RagAnswer(RagAnswerStatus.GenerationUnavailable, Text: null, found);
        }

        var generated = await GenerateAsync(question, matches, cancellationToken);

        if (generated is null)
        {
            return new RagAnswer(RagAnswerStatus.GenerationUnavailable, Text: null, found);
        }

        // Номера, которых в контексте не было, отбрасываем: модель могла сослаться на фрагмент,
        // которого ей не давали, и такая ссылка ничего не подтверждает.
        var used = (generated.Citations ?? [])
            .Where(number => number >= 1 && number <= found.Count)
            .Distinct()
            .Order()
            .Select(number => found[number - 1])
            .ToArray();

        if (used.Length == 0 || string.IsNullOrWhiteSpace(generated.Answer))
        {
            // Продуктовый принцип «никаких ответов без ссылки» держится здесь, а не в промпте:
            // инструкцию модель может проигнорировать, проверку — нет.
            _logger.LogWarning("Ответ модели не опирается ни на один фрагмент и отброшен.");

            return new RagAnswer(RagAnswerStatus.NoGrounding, Text: null, found);
        }

        return new RagAnswer(RagAnswerStatus.Answered, generated.Answer.Trim(), used);
    }

    private async Task<ReadOnlyMemory<float>> EmbedQuestionAsync(string question, CancellationToken cancellationToken)
    {
        var options = new EmbeddingGenerationOptions { Dimensions = _options.EmbeddingDimensions };

        try
        {
            return await _embeddingGenerator.GenerateVectorAsync(question, options, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            throw new RagException($"Не удалось получить эмбеддинг вопроса: {e.Message}", e);
        }
    }

    /// <returns>Разобранный ответ модели либо <c>null</c>, если генерация не удалась.</returns>
    private async Task<GeneratedAnswer?> GenerateAsync(
        string question,
        IReadOnlyList<ChunkMatch> matches,
        CancellationToken cancellationToken)
    {
        List<ChatMessage> messages =
        [
            new(ChatRole.System, RagPrompt.System),
            new(ChatRole.User, RagPrompt.BuildUserMessage(question, matches)),
        ];

        // Температура 0: задача фактологическая, разнообразие формулировок здесь только вредит.
        var chatOptions = new ChatOptions { Temperature = 0 };

        try
        {
            var response = await _chatClient!.GetResponseAsync<GeneratedAnswer>(
                messages,
                chatOptions,
                _options.UseJsonSchemaResponseFormat,
                cancellationToken);

            if (response.TryGetResult(out var result))
            {
                return result;
            }

            _logger.LogWarning("Ответ модели не разобрался в структуру: {Response}", response.Text);

            return null;
        }
        catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Деградация вместо отказа: фрагменты уже найдены, показать их полезнее, чем ошибку.
            _logger.LogError(e, "Провайдер генерации недоступен, отвечаем найденными фрагментами.");

            return null;
        }
    }

    private static IReadOnlyList<RagCitation> ToCitations(IReadOnlyList<ChunkMatch> matches)
        => [.. matches.Select((match, index) => new RagCitation(
            index + 1,
            match.SourceKey,
            match.Ordinal,
            match.Content,
            match.Score))];

    /// <summary>
    /// Структура ответа модели. Номера фрагментов приходят отдельным полем, а не внутри текста:
    /// разбирать «[1]» регуляркой означало бы зависеть от того, как модель оформила ссылку.
    /// </summary>
    internal sealed class GeneratedAnswer
    {
        [Description("Ответ на вопрос строго по приведённым фрагментам")]
        public string? Answer { get; set; }

        [Description("Номера фрагментов (начиная с 1), на которых основан ответ")]
        public int[]? Citations { get; set; }
    }
}
