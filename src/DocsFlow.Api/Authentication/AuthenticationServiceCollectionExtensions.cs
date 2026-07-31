using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

// RedirectContext есть и у cookie-схемы, и у OIDC-схемы — разводим их явным алиасом.
using CookieRedirectContext =
    Microsoft.AspNetCore.Authentication.RedirectContext<
        Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>;

namespace DocsFlow.Api.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>Имя cookie сессии. Меняется только вместе с осознанным разлогином всех пользователей.</summary>
    private const string SessionCookieName = "docsflow.session";

    /// <summary>
    /// Собирает аутентификацию по схеме BFF: токены Keycloak остаются на сервере внутри
    /// шифрованного тикета, браузер получает только httpOnly cookie. Настройки читаются из секции
    /// <see cref="KeycloakOptions.SectionName"/> и проверяются на старте приложения.
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(KeycloakOptions.SectionName);

        services.AddOptions<KeycloakOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<SessionRefresher>();

        // Конвейер аутентификации собирается до построения провайдера сервисов, поэтому настройки
        // читаются из конфигурации напрямую. Понятное сообщение о незаполненных полях даёт
        // ValidateOnStart выше — он срабатывает раньше, чем эти значения кому-то понадобятся.
        var options = section.Get<KeycloakOptions>() ?? new KeycloakOptions();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(cookie =>
            {
                cookie.Cookie.Name = SessionCookieName;

                // В этом весь смысл BFF: содержимое cookie недоступно JavaScript, поэтому чужой
                // скрипт на странице не может унести сессию.
                cookie.Cookie.HttpOnly = true;

                // Lax, а не Strict: Strict не отправит cookie при возврате из Keycloak и вход
                // зациклится. При этом Lax уже не отправляется при cross-site POST — это и есть
                // базовая защита от CSRF, пока GET-эндпоинты остаются без побочных эффектов.
                cookie.Cookie.SameSite = SameSiteMode.Lax;

                // Локально Keycloak и приложение работают без TLS. Требовать Secure там нельзя:
                // такую cookie не вернёт ни один клиент, ходящий по http.
                cookie.Cookie.SecurePolicy = options.RequireHttps
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;

                // Срок жизни сессии определяется обменом refresh-токена, а не активностью.
                cookie.ExpireTimeSpan = TimeSpan.FromDays(14);
                cookie.SlidingExpiration = false;

                // Для API «нужно войти» — это 401. Дефолтный редирект на страницу входа отдал бы
                // клиенту HTML логин-страницы вместо разбираемого ответа. Редирект в Keycloak
                // происходит только через явный GET /api/auth/login.
                cookie.Events.OnRedirectToLogin = StatusCode(StatusCodes.Status401Unauthorized);
                cookie.Events.OnRedirectToAccessDenied = StatusCode(StatusCodes.Status403Forbidden);

                cookie.Events.OnValidatePrincipal = context => context.HttpContext.RequestServices
                    .GetRequiredService<SessionRefresher>()
                    .ValidateAsync(context);
            })
            .AddOpenIdConnect(AuthenticationSchemes.Keycloak, oidc =>
            {
                oidc.Authority = options.Authority;
                oidc.ClientId = options.ClientId;
                oidc.ClientSecret = options.ClientSecret;
                oidc.RequireHttpsMetadata = options.RequireHttps;

                oidc.ResponseType = OpenIdConnectResponseType.Code;

                // Ответ приходит query-параметрами, то есть обычной навигацией. При form_post
                // возврат был бы cross-site POST'ом, и correlation-cookie пришлось бы отдавать
                // с SameSite=None — то есть обязательно Secure, что ломает локальную разработку.
                oidc.ResponseMode = OpenIdConnectResponseMode.Query;
                oidc.UsePkce = true;

                // Токены складываются в тикет, а тикет — в шифрованную cookie. Наружу не уходят.
                oidc.SaveTokens = true;

                // Без этого sub/email переименовываются в длинные URI-имена и код начинает
                // зависеть от таблицы легаси-маппинга.
                oidc.MapInboundClaims = false;

                // profile и email приносят нужные claim'ы в id-токене, ходить в userinfo не за чем.
                oidc.GetClaimsFromUserInfoEndpoint = false;

                oidc.Scope.Clear();
                oidc.Scope.Add("openid");
                oidc.Scope.Add("profile");
                oidc.Scope.Add("email");
                // offline_access намеренно не запрашивается: он выдаёт offline-токен, живущий
                // дольше SSO-сессии, и «выход из Keycloak» перестал бы гасить доступ.

                oidc.TokenValidationParameters.NameClaimType = OidcClaims.PreferredUsername;

                oidc.CorrelationCookie.SameSite = SameSiteMode.Lax;
                oidc.NonceCookie.SameSite = SameSiteMode.Lax;

                oidc.Events.OnTicketReceived = LoginProvisioning.OnTicketReceivedAsync;
            });

        services.AddAuthorization();

        return services;
    }

    private static Func<CookieRedirectContext, Task> StatusCode(int statusCode) =>
        context =>
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        };
}
