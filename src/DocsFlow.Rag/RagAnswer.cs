namespace DocsFlow.Rag;

/// <summary>
/// Результат вопроса к архиву. Найденные фрагменты возвращаются при любом статусе, поэтому
/// показать первоисточник можно даже тогда, когда ответа модели нет.
/// </summary>
/// <param name="Text">
/// Текст ответа. Заполняется только при <see cref="RagAnswerStatus.Answered"/>: ответ без ссылки
/// на фрагмент наружу не выходит.
/// </param>
public sealed record RagAnswer(
    RagAnswerStatus Status,
    string? Text,
    IReadOnlyList<RagCitation> Citations);

/// <param name="Number">Номер фрагмента в контексте (1-based) — на него ссылается текст ответа.</param>
public sealed record RagCitation(int Number, string SourceKey, int Ordinal, string Content, double Score);

public enum RagAnswerStatus
{
    /// <summary>Модель ответила, и ответ опирается хотя бы на один реальный фрагмент.</summary>
    Answered,

    /// <summary>Поиск не нашёл ничего, что проходит порог близости.</summary>
    NothingFound,

    /// <summary>
    /// Фрагменты нашлись, но модель не сослалась ни на один из них. Текст такого ответа
    /// отбрасывается: доверять утверждению, которое нечем подтвердить, нельзя.
    /// </summary>
    NoGrounding,

    /// <summary>
    /// Генерация выключена конфигом или провайдер недоступен. Фрагменты найдены и возвращены —
    /// сценарий деградирует до поиска, а не падает.
    /// </summary>
    GenerationUnavailable,
}
