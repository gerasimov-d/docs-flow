namespace DocsFlow.Users;

/// <summary>
/// Личность, подтверждённая внешним провайдером. Собирается веб-слоем из claims — сюда
/// <c>ClaimsPrincipal</c> не протекает, поэтому провижининг тестируется без поднятия приложения,
/// а будущая схема аутентификации для мобильного клиента переиспользует его как есть.
/// </summary>
/// <param name="Subject">Claim <c>sub</c>.</param>
/// <param name="EmailVerified">
/// Claim <c>email_verified</c>. Проверяется перед провижинингом: Keycloak с включённым
/// подтверждением email до входа и не пустит, это второй рубеж.
/// </param>
public sealed record ExternalIdentity(
    string Subject,
    string Email,
    string? DisplayName,
    bool EmailVerified);
