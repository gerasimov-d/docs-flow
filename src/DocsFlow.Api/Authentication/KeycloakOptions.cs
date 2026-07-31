using System.ComponentModel.DataAnnotations;

namespace DocsFlow.Api.Authentication;

public sealed class KeycloakOptions
{
    public const string SectionName = "Authentication:Keycloak";

    /// <summary>
    /// Адрес realm, например <c>http://localhost:8080/realms/docsflow</c>. От него берётся
    /// метаданные OIDC-провайдера (<c>/.well-known/openid-configuration</c>).
    /// </summary>
    [Required]
    [Url]
    public string Authority { get; init; } = null!;

    /// <summary>Идентификатор клиента в realm.</summary>
    [Required]
    public string ClientId { get; init; } = null!;

    /// <summary>
    /// Секрет клиента. Клиент конфиденциальный: секрет не покидает сервер, поэтому в браузер
    /// не попадает ни он, ни выданные по нему токены.
    /// </summary>
    [Required]
    public string ClientSecret { get; init; } = null!;

    /// <summary>
    /// Развёртывание работает только по HTTPS. Управляет двумя вещами сразу: требованием HTTPS при
    /// загрузке метаданных провайдера и флагом <c>Secure</c> у cookie сессии.
    /// </summary>
    /// <remarks>
    /// Выключается только там, где и Keycloak, и приложение поднимаются без TLS — локальная
    /// разработка и тесты. Это отдельная настройка, а не вывод из имени окружения: настройка,
    /// ослабляющая защиту сессии, должна быть видна в конфиге, а не выводиться из «Development».
    /// </remarks>
    public bool RequireHttps { get; init; } = true;
}
