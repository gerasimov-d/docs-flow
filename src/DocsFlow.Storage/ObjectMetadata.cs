namespace DocsFlow.Storage;

/// <param name="Size">Размер объекта в байтах.</param>
/// <param name="ETag">Тег версии, назначенный хранилищем. Кавычки вокруг значения снимаются.</param>
public sealed record ObjectMetadata(
    long Size,
    string ContentType,
    string ETag,
    DateTimeOffset LastModified);
