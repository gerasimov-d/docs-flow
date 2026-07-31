namespace DocsFlow.Rag;

/// <summary>
/// Сбой пайплайна, при котором продолжать бессмысленно: например, эмбеддинг не получен или
/// его размерность не совпала со схемой хранилища. Недоступность генерации ответа сюда не
/// относится — это штатная деградация, см. <see cref="RagAnswerStatus.GenerationUnavailable"/>.
/// </summary>
public sealed class RagException : Exception
{
    public RagException(string message) : base(message)
    {
    }

    public RagException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
