namespace DocsFlow.Storage;

/// <summary>
/// Сбой при работе с хранилищем. Исключения SDK наружу не выпускаются — вызывающий код
/// не должен знать, какой провайдер стоит за <see cref="IObjectStorage"/>.
/// </summary>
public class ObjectStorageException : Exception
{
    public ObjectStorageException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class ObjectNotFoundException : ObjectStorageException
{
    public ObjectNotFoundException(string key, Exception? innerException = null)
        : base($"Объект '{key}' не найден в хранилище.", innerException)
    {
        Key = key;
    }

    public string Key { get; }
}
