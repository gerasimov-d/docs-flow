namespace DocsFlow.Users;

/// <summary>
/// Профиль пользователя в нашей базе. Личность подтверждает Keycloak, здесь живёт только то,
/// что принадлежит сервису: собственный идентификатор и снимок профиля с момента последнего входа.
/// </summary>
/// <param name="Id">
/// Собственный идентификатор. Не <c>sub</c> из Keycloak: он уходит во внешние ссылки и в чужие
/// таблицы, поэтому не должен зависеть от жизни записи в IdP.
/// </param>
/// <param name="KeycloakSubject">Claim <c>sub</c> — единственная связь с IdP, неизменен.</param>
public sealed record User(
    Guid Id,
    string KeycloakSubject,
    string Email,
    string? DisplayName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastLoginAt);
