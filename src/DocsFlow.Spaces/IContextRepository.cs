namespace DocsFlow.Spaces;

/// <summary>
/// Доступ к контекстам внутри space.
/// </summary>
/// <remarks>
/// Все методы принимают space явным параметром и фильтруют по нему: контекст вне своего space
/// не существует, поэтому запроса «найти контекст по идентификатору» здесь нет вовсе — он открыл бы
/// путь к чужому контексту по угаданному UUID.
/// </remarks>
public interface IContextRepository
{
    /// <summary>
    /// Создаёт контекст в space.
    /// </summary>
    /// <returns>
    /// Созданный контекст либо <c>null</c>, если имя в этом space уже занято: сравнение
    /// регистронезависимое, «Авто» и «авто» — одно и то же имя.
    /// </returns>
    Task<SpaceContext?> CreateAsync(Guid spaceId, string name, CancellationToken cancellationToken = default);

    /// <summary>Контексты space. Пустой список — нормальное состояние, а не ошибка.</summary>
    Task<IReadOnlyList<SpaceContext>> ListAsync(Guid spaceId, CancellationToken cancellationToken = default);
}
