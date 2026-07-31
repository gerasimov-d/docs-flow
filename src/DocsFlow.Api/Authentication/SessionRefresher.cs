using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace DocsFlow.Api.Authentication;

/// <summary>
/// Продлевает сессию, обменивая refresh-токен в Keycloak, и завершает её, если обмен не удался.
/// </summary>
/// <remarks>
/// Это не только про долгие сессии. Через обмен до приложения доходит отзыв доступа: пользователь,
/// отключённый или удалённый в Keycloak либо нажавший «выйти со всех устройств», перестаёт
/// проходить обновление и выпадает из сервиса в пределах времени жизни access-токена, а не через
/// две недели. Поэтому локального флага «заблокирован» в таблице <c>users</c> нет.
/// </remarks>
internal sealed class SessionRefresher
{
    // Обновляемся заранее: иначе есть окно, в котором access-токен уже истёк.
    private static readonly TimeSpan RefreshBefore = TimeSpan.FromSeconds(60);

    private const string ExpiresAtTokenName = "expires_at";

    private readonly IOptionsMonitor<OpenIdConnectOptions> _oidcOptions;
    private readonly ILogger<SessionRefresher> _logger;

    public SessionRefresher(
        IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
        ILogger<SessionRefresher> logger)
    {
        _oidcOptions = oidcOptions;
        _logger = logger;
    }

    public async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        if (!DateTimeOffset.TryParse(
                context.Properties.GetTokenValue(ExpiresAtTokenName),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expiresAt))
        {
            // Тикета без срока быть не должно (SaveTokens включён). Не роняем сессию из-за этого:
            // её всё равно ограничивает время жизни cookie.
            return;
        }

        if (expiresAt - DateTimeOffset.UtcNow > RefreshBefore)
        {
            return;
        }

        var refreshToken = context.Properties.GetTokenValue(OpenIdConnectParameterNames.RefreshToken);

        if (string.IsNullOrEmpty(refreshToken))
        {
            await RejectAsync(context, "в тикете нет refresh-токена");
            return;
        }

        var options = _oidcOptions.Get(AuthenticationSchemes.Keycloak);
        var cancellationToken = context.HttpContext.RequestAborted;

        try
        {
            var configuration = await options.ConfigurationManager!.GetConfigurationAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, configuration.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = OpenIdConnectGrantTypes.RefreshToken,
                    ["client_id"] = options.ClientId!,
                    ["client_secret"] = options.ClientSecret!,
                    ["refresh_token"] = refreshToken,
                }),
            };

            using var response = await options.Backchannel.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await RejectAsync(context, $"Keycloak отказал в обмене refresh-токена ({(int)response.StatusCode})");
                return;
            }

            using var payload = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));

            StoreRefreshedTokens(context, payload.RootElement);
            context.ShouldRenew = true;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            // Keycloak недоступен или ответил мусором. Разлогинивать за это нельзя: иначе разовый
            // сетевой сбой выкидывает всех пользователей. Сессия доживёт до следующей попытки.
            _logger.LogWarning(exception, "Не удалось обновить сессию, пробуем на следующем запросе");
        }
    }

    /// <summary>
    /// Кладёт обновлённые токены обратно в тикет. <c>StoreTokens</c> заменяет набор целиком,
    /// поэтому старые значения (прежде всего <c>id_token</c> — он нужен для выхода) переносятся.
    /// </summary>
    private static void StoreRefreshedTokens(CookieValidatePrincipalContext context, JsonElement payload)
    {
        var tokens = context.Properties.GetTokens()
            .ToDictionary(token => token.Name, token => token.Value);

        if (payload.TryGetProperty("access_token", out var accessToken))
        {
            tokens[OpenIdConnectParameterNames.AccessToken] = accessToken.GetString()!;
        }

        if (payload.TryGetProperty("refresh_token", out var refreshToken))
        {
            tokens[OpenIdConnectParameterNames.RefreshToken] = refreshToken.GetString()!;
        }

        if (payload.TryGetProperty("id_token", out var idToken))
        {
            tokens[OpenIdConnectParameterNames.IdToken] = idToken.GetString()!;
        }

        if (payload.TryGetProperty("expires_in", out var expiresIn))
        {
            tokens[ExpiresAtTokenName] = DateTimeOffset.UtcNow
                .AddSeconds(expiresIn.GetInt32())
                .ToString("o", CultureInfo.InvariantCulture);
        }

        context.Properties.StoreTokens(tokens
            .Select(token => new AuthenticationToken { Name = token.Key, Value = token.Value }));
    }

    private async Task RejectAsync(CookieValidatePrincipalContext context, string reason)
    {
        _logger.LogInformation("Сессия завершена: {Reason}", reason);

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
