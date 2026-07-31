namespace DocsFlow.Users;

/// <summary>
/// Доступ к таблице <c>users</c>.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Создаёт запись для внешней личности или обновляет профиль уже существующей.
    /// Возвращает актуальное состояние. Идемпотентен и безопасен при параллельных вызовах.
    /// </summary>
    Task<User> UpsertBySubjectAsync(ExternalIdentity identity, CancellationToken cancellationToken = default);

    /// <summary>Возвращает пользователя по внутреннему идентификатору или <c>null</c>, если его нет.</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
