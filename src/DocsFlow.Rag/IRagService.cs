namespace DocsFlow.Rag;

/// <summary>
/// Вопрос к архиву: поиск по смыслу плюс ответ модели, обязательно со ссылками на фрагменты.
/// </summary>
public interface IRagService
{
    /// <summary>
    /// Отвечает на вопрос по проиндексированным источникам одного space.
    /// </summary>
    /// <param name="spaceId">
    /// Границы поиска. Ни в выдачу, ни в контекст модели не попадает ничего за их пределами:
    /// изоляция арендатора держится здесь, а не в тексте промпта.
    /// </param>
    /// <remarks>
    /// Никогда не падает из-за недоступной генерации: если модель выключена или не ответила,
    /// возвращается статус <see cref="RagAnswerStatus.GenerationUnavailable"/> с найденными
    /// фрагментами.
    /// </remarks>
    /// <exception cref="RagException">Не удалось посчитать эмбеддинг вопроса — искать нечем.</exception>
    Task<RagAnswer> AskAsync(Guid spaceId, string question, CancellationToken cancellationToken = default);
}
