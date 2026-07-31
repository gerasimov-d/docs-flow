using System.ComponentModel.DataAnnotations;

namespace DocsFlow.Llm;

/// <summary>
/// Общая часть настроек обращения к OpenAI-совместимому провайдеру. Чат и эмбеддинги настраиваются
/// раздельно: у них разные модели, разные эндпоинты и принципиально разное время ответа.
/// </summary>
public abstract class LlmEndpointOptions
{
    /// <summary>
    /// База OpenAI-совместимого API, например <c>https://api.openai.com/v1</c> или
    /// <c>http://localhost:11434/v1</c> для локального сервера.
    /// </summary>
    [Required]
    [Url]
    public string Endpoint { get; init; } = null!;

    /// <summary>
    /// Ключ API. Не обязателен: локальные серверы его не спрашивают.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>Идентификатор модели в терминах провайдера.</summary>
    [Required]
    public string Model { get; init; } = null!;

    /// <summary>Таймаут одной попытки запроса.</summary>
    [Range(1, 3600)]
    public int AttemptTimeoutSeconds { get; init; }

    /// <summary>
    /// Общий таймаут запроса вместе с ретраями. Обязан быть больше <see cref="AttemptTimeoutSeconds"/>,
    /// иначе пайплайн устойчивости не проходит валидацию на старте.
    /// </summary>
    [Range(1, 3600)]
    public int TotalTimeoutSeconds { get; init; }

    /// <summary>
    /// Сколько раз повторять запрос при таймауте, 429 и 5xx. Ноль недопустим: стратегия ретраев
    /// требует хотя бы одной попытки, и нулём приложение упало бы уже на старте — пусть лучше
    /// значение отсекает валидация конфигурации с внятным сообщением.
    /// </summary>
    [Range(1, 10)]
    public int MaxRetryAttempts { get; init; } = 3;
}
