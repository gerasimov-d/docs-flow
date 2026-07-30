namespace DocsFlow.Storage;

/// <summary>
/// Объектное хранилище. Работает строго по ключу — схему ключей формирует вызывающий код.
/// </summary>
public interface IObjectStorage
{
    /// <summary>Записывает объект. Существующий объект с тем же ключом перезаписывается.</summary>
    Task PutAsync(string key, Stream content, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Открывает поток на чтение объекта. Поток обязан быть освобождён вызывающим кодом.
    /// </summary>
    /// <exception cref="ObjectNotFoundException">Объекта с таким ключом нет.</exception>
    Task<Stream> GetAsync(string key, CancellationToken ct = default);

    /// <summary>Удаляет объект. Идемпотентно: удаление отсутствующего ключа — не ошибка.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <returns>Метаданные объекта либо <c>null</c>, если объекта нет.</returns>
    Task<ObjectMetadata?> GetMetadataAsync(string key, CancellationToken ct = default);

    /// <summary>Перечисляет ключи объектов с указанным префиксом. Пагинация выполняется прозрачно.</summary>
    IAsyncEnumerable<string> ListAsync(string prefix, CancellationToken ct = default);

    /// <summary>
    /// Строит presigned URL на скачивание. Подпись считается локально, обращения к хранилищу нет.
    /// </summary>
    Uri GetPresignedUrl(string key, TimeSpan ttl);
}
