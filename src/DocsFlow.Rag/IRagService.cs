namespace DocsFlow.Rag;

/// <summary>
/// Вопрос к архиву: поиск по смыслу плюс ответ модели, обязательно со ссылками на фрагменты.
/// </summary>
public interface IRagService
{
    /// <summary>
    /// Отвечает на вопрос по проиндексированным источникам.
    /// </summary>
    /// <remarks>
    /// Никогда не падает из-за недоступной генерации: если модель выключена или не ответила,
    /// возвращается статус <see cref="RagAnswerStatus.GenerationUnavailable"/> с найденными
    /// фрагментами.
    /// </remarks>
    /// <exception cref="RagException">Не удалось посчитать эмбеддинг вопроса — искать нечем.</exception>
    Task<RagAnswer> AskAsync(string question, CancellationToken cancellationToken = default);
}
