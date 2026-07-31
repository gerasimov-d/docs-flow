namespace DocsFlow.Api.Authentication;

/// <summary>Имена схем аутентификации.</summary>
public static class AuthenticationSchemes
{
    /// <summary>OIDC-схема. Задействуется только на входе и выходе, обычные запросы её не трогают.</summary>
    public const string Keycloak = "keycloak";
}

/// <summary>Типы claim'ов, которые сервис добавляет сам.</summary>
public static class DocsFlowClaims
{
    /// <summary>
    /// Внутренний идентификатор пользователя (<c>users.id</c>). Кладётся в cookie при входе:
    /// он неизменен, поэтому не устаревает. Изменяемая часть профиля в cookie не хранится.
    /// </summary>
    public const string UserId = "docsflow:user_id";
}

/// <summary>Имена claim'ов, приходящих от Keycloak. Не смаплены в URI-имена: <c>MapInboundClaims</c> выключен.</summary>
internal static class OidcClaims
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string EmailVerified = "email_verified";
    public const string Name = "name";
    public const string PreferredUsername = "preferred_username";
}
