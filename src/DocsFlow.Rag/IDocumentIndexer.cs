namespace DocsFlow.Rag;

/// <summary>
/// Кладёт текст источника в поисковый индекс: режет на фрагменты, считает эмбеддинги, сохраняет.
/// </summary>
public interface IDocumentIndexer
{
    /// <summary>
    /// Индексирует текст источника, заменяя прежнее содержимое. Пустой текст очищает индекс
    /// источника — это не ошибка, а «искать здесь больше нечего».
    /// </summary>
    /// <param name="spaceId">Space, которому принадлежит источник. Индекс живёт в его границах.</param>
    /// <param name="sourceKey">Локатор первоисточника, например ключ файла в объектном хранилище.</param>
    /// <returns>Сколько фрагментов сохранено.</returns>
    /// <exception cref="RagException">Эмбеддинги не получены или их размерность не совпала со схемой.</exception>
    Task<int> IndexAsync(
        Guid spaceId,
        string sourceKey,
        string text,
        CancellationToken cancellationToken = default);
}
