using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace DocsFlow.Api.Authentication;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

        // Единственная точка, откуда происходит редирект в Keycloak.
        group.MapGet("/login", (string? returnUrl) => Results.Challenge(
            new AuthenticationProperties { RedirectUri = LocalUrlOrRoot(returnUrl) },
            [AuthenticationSchemes.Keycloak]));

        group.MapPost("/logout", async (HttpContext http) =>
        {
            // id_token нужен Keycloak как id_token_hint, иначе он не примет запрос на выход.
            // Читаем его до того, как cookie будет удалена.
            var authentication = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var idToken = authentication.Properties?.GetTokenValue(OpenIdConnectParameterNames.IdToken);

            var properties = new AuthenticationProperties { RedirectUri = "/" };

            if (!string.IsNullOrEmpty(idToken))
            {
                properties.StoreTokens([
                    new AuthenticationToken { Name = OpenIdConnectParameterNames.IdToken, Value = idToken },
                ]);
            }

            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Гасит и SSO-сессию Keycloak: иначе повторный вход прошёл бы без ввода пароля.
            await http.SignOutAsync(AuthenticationSchemes.Keycloak, properties);
        });

        return endpoints;
    }

    /// <summary>
    /// Пропускает только локальные пути. Без этой проверки <c>returnUrl</c> превращает вход
    /// в open redirect: ссылка на наш домен увела бы пользователя на чужой сайт после логина.
    /// </summary>
    private static string LocalUrlOrRoot(string? returnUrl) =>
        returnUrl is not null
        && returnUrl.Length > 0
        && returnUrl[0] == '/'
        && (returnUrl.Length == 1 || (returnUrl[1] != '/' && returnUrl[1] != '\\'))
            ? returnUrl
            : "/";
}
