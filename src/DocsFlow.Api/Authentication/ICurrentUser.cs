using DocsFlow.Users;

namespace DocsFlow.Api.Authentication;

/// <summary>
/// Пользователь текущего запроса.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Внутренний идентификатор из claim'а cookie — без обращения к базе.
    /// <c>null</c>, если запрос не аутентифицирован.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Профиль из базы. <c>null</c>, если запрос не аутентифицирован или записи больше нет.
    /// Читается из базы, а не из cookie: изменяемые данные в cookie устаревали бы до перелогина.
    /// </summary>
    Task<User?> GetAsync(CancellationToken cancellationToken = default);
}
