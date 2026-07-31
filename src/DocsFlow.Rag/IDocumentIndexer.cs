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
    /// <param name="sourceKey">Локатор первоисточника, например ключ файла в объектном хранилище.</param>
    /// <returns>Сколько фрагментов сохранено.</returns>
    /// <exception cref="RagException">Эмбеддинги не получены или их размерность не совпала со схемой.</exception>
    Task<int> IndexAsync(string sourceKey, string text, CancellationToken cancellationToken = default);
}
