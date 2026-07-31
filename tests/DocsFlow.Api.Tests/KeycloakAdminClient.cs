using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DocsFlow.Api.Tests;

/// <summary>
/// Тонкая обёртка над Admin API Keycloak — только для подготовки тестового окружения.
/// Приложение к Admin API не обращается: регистрацию и учётные данные Keycloak ведёт сам.
/// </summary>
internal sealed class KeycloakAdminClient : IDisposable
{
    private readonly HttpClient _http = new();
    private readonly string _baseAddress;
    private readonly string _realm;

    public KeycloakAdminClient(string baseAddress, string realm)
    {
        _baseAddress = baseAddress.TrimEnd('/');
        _realm = realm;
    }

    public async Task AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsync(
            $"{_baseAddress}/realms/master/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "admin-cli",
                ["username"] = username,
                ["password"] = password,
            }),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            payload.RootElement.GetProperty("access_token").GetString());
    }

    /// <summary>
    /// Прописывает клиенту адреса возврата уже запущенного приложения. В realm-файле лежат порты
    /// из <c>launchSettings.json</c>, а тест поднимает приложение на свободном порту — Keycloak же
    /// сверяет <c>redirect_uri</c> буквально.
    /// </summary>
    public async Task AllowRedirectsToAsync(
        string clientId,
        string appBaseAddress,
        CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(clientId, cancellationToken);
        var internalId = client.GetProperty("id").GetString();

        var update = new Dictionary<string, object?>
        {
            ["redirectUris"] = new[] { $"{appBaseAddress}/signin-oidc" },
            ["webOrigins"] = new[] { appBaseAddress },
            ["attributes"] = new Dictionary<string, string>
            {
                ["pkce.code.challenge.method"] = "S256",
                ["post.logout.redirect.uris"] = $"{appBaseAddress}/*",
            },
        };

        using var response = await _http.PutAsJsonAsync(
            $"{_baseAddress}/admin/realms/{_realm}/clients/{internalId}",
            update,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Создаёт пользователя с уже подтверждённым email — подтверждение письмом в тесте не пройти,
    /// а провижининг требует <c>email_verified</c>.
    /// </summary>
    public async Task CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken)
    {
        var user = new Dictionary<string, object?>
        {
            ["username"] = email,
            ["email"] = email,
            ["emailVerified"] = true,
            ["enabled"] = true,
            // Имя и фамилия обязательны: без них Keycloak не пускает дальше и требует
            // дозаполнить профиль (обязательное действие VERIFY_PROFILE).
            ["firstName"] = firstName,
            ["lastName"] = lastName,
            ["credentials"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["type"] = "password",
                    ["value"] = password,
                    ["temporary"] = false,
                },
            },
        };

        using var response = await _http.PostAsJsonAsync(
            $"{_baseAddress}/admin/realms/{_realm}/users",
            user,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private async Task<JsonElement> GetClientAsync(string clientId, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(
            $"{_baseAddress}/admin/realms/{_realm}/clients?clientId={clientId}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var clients = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        return clients.RootElement.EnumerateArray().FirstOrDefault() is { ValueKind: JsonValueKind.Object } client
            ? client.Clone()
            : throw new InvalidOperationException(
                $"В realm {_realm} нет клиента {clientId} — realm импортировался не из ожидаемого файла.");
    }

    public void Dispose() => _http.Dispose();
}
