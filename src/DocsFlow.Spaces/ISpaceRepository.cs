namespace DocsFlow.Spaces;

/// <summary>Чем закончилась выдача доступа.</summary>
public enum AddMemberResult
{
    Added,

    /// <summary>Пользователь уже состоит в space — повторная выдача ничего не меняет.</summary>
    AlreadyMember,

    /// <summary>Такого пользователя в системе нет. Приглашений незарегистрированных пока нет.</summary>
    UserNotFound,
}

/// <summary>Чем закончился отзыв доступа.</summary>
public enum RemoveMemberResult
{
    Removed,

    /// <summary>Пользователь и так не состоял в space.</summary>
    NotMember,

    /// <summary>
    /// Отозвать доступ у владельца нельзя: space остался бы без владельца, а передачи владения нет.
    /// </summary>
    OwnerCannotBeRemoved,
}

/// <summary>
/// Доступ к space и их составу.
/// </summary>
/// <remarks>
/// Репозиторий не решает, можно ли текущему пользователю трогать space, — он лишь отвечает,
/// состоит ли тот в нём (<see cref="FindRoleAsync"/>). Само решение принимается один раз
/// в конвейере API, чтобы проверку нельзя было забыть в отдельном эндпоинте.
/// </remarks>
public interface ISpaceRepository
{
    /// <summary>Создаёт space и делает автора владельцем. Обе записи появляются одной транзакцией.</summary>
    Task<Space> CreateAsync(Guid ownerId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт первый space пользователя, если тот ещё не состоит ни в одном. Идемпотентен
    /// и безопасен при параллельных вызовах: два одновременных входа не создадут два space.
    /// </summary>
    /// <returns>Созданный space либо <c>null</c>, если space у пользователя уже был.</returns>
    Task<Space?> CreateFirstIfMissingAsync(Guid ownerId, string name, CancellationToken cancellationToken = default);

    /// <summary>Space, в которых состоит пользователь, вместе с его ролью в каждом.</summary>
    Task<IReadOnlyList<SpaceMembership>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Роль пользователя в space или <c>null</c>, если он в нём не состоит. Единственный источник
    /// ответа на вопрос «есть ли доступ»: несуществующий space и чужой отсюда неразличимы,
    /// и наружу они обязаны выглядеть одинаково.
    /// </summary>
    Task<SpaceRole?> FindRoleAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Space по идентификатору. Членство не проверяет — это забота вызывающего кода.</summary>
    Task<Space?> FindAsync(Guid spaceId, CancellationToken cancellationToken = default);

    /// <summary>Переименовывает space.</summary>
    /// <returns><c>false</c>, если space не найден.</returns>
    Task<bool> RenameAsync(Guid spaceId, string name, CancellationToken cancellationToken = default);

    /// <summary>Состав space с профилями участников.</summary>
    Task<IReadOnlyList<SpaceMember>> ListMembersAsync(Guid spaceId, CancellationToken cancellationToken = default);

    /// <summary>Даёт доступ зарегистрированному пользователю. Идемпотентен.</summary>
    Task<AddMemberResult> AddMemberAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Отзывает доступ. Идемпотентен.</summary>
    Task<RemoveMemberResult> RemoveMemberAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
