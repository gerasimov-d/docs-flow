using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Xunit;

namespace DocsFlow.Api.Tests;

public sealed class LoginFlowTests(DocsFlowAppFixture fixture)
{
    /// <summary>Ответ <c>/api/me</c>. Объявлен здесь, чтобы тест проверял контракт наружу, а не тип из приложения.</summary>
    private sealed record Me(Guid Id, string Email, string? DisplayName);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task An_unauthenticated_api_call_gets_401_and_not_a_redirect()
    {
        using var client = fixture.CreateClient();

        using var response = await client.GetAsync("/api/me", Ct);

        // Дефолтное поведение cookie-схемы — 302 на страницу входа; для API это отдало бы
        // клиенту HTML логин-страницы вместо разбираемого ответа.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("https", "https://")]
    [InlineData("http", "http://")]
    public async Task Addresses_are_built_from_the_protocol_reported_by_the_proxy(
        string forwardedProto,
        string expectedPrefix)
    {
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(fixture.BaseAddress) };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");

        // Так выглядит запрос за прокси, терминирующим TLS: до приложения он дошёл по http,
        // а браузер разговаривал с прокси по https.
        request.Headers.Add("X-Forwarded-Proto", forwardedProto);

        using var response = await client.SendAsync(request, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Found);

        // Проверяем адрес возврата после выхода, а не redirect_uri входа: последний уходит в
        // Keycloak отдельным backchannel-запросом (pushed authorization request) и в ссылке
        // не появляется. Собираются оба из схемы запроса, так что проверяется одно и то же.
        var postLogoutRedirectUri = QueryValue(response.Headers.Location!, "post_logout_redirect_uri");

        postLogoutRedirectUri.ShouldNotBeNull();
        postLogoutRedirectUri.ShouldStartWith(expectedPrefix);
    }

    [Fact]
    public async Task Logging_in_creates_a_session_and_the_user_row()
    {
        using var client = fixture.CreateClient();

        await LogInAsync(client);

        var me = await ReadMeAsync(client);

        me.ShouldNotBeNull();
        me.Email.ShouldBe(DocsFlowAppFixture.UserEmail);
        me.DisplayName.ShouldBe(DocsFlowAppFixture.UserDisplayName);
        // Идентификатор наш, а не sub из Keycloak: /api/me читает запись из базы, значит
        // провижининг при входе отработал.
        me.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task A_second_login_reuses_the_same_user_row()
    {
        using var first = fixture.CreateClient();
        await LogInAsync(first);
        var before = await ReadMeAsync(first);

        // Отдельный клиент — отдельное хранилище cookie, то есть настоящий повторный вход.
        using var second = fixture.CreateClient();
        await LogInAsync(second);
        var after = await ReadMeAsync(second);

        // Вторая строка получила бы другой идентификатор — значит, upsert сработал по sub.
        after!.Id.ShouldBe(before!.Id);
    }

    [Fact]
    public async Task Logging_out_ends_the_session()
    {
        using var client = fixture.CreateClient();
        await LogInAsync(client);

        using (var beforeLogout = await client.GetAsync("/api/me", Ct))
        {
            beforeLogout.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var logout = await client.PostAsync("/api/auth/logout", content: null, Ct);

        using var afterLogout = await client.GetAsync("/api/me", Ct);
        afterLogout.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<Me?> ReadMeAsync(BrowserClient client)
    {
        using var response = await client.GetAsync("/api/me", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<Me>(Ct);
    }

    /// <summary>
    /// Вход настоящим браузерным сценарием — единственная проверка, подтверждающая, что realm,
    /// конфиг клиента и настройки cookie согласованы между собой.
    /// </summary>
    private Task LogInAsync(BrowserClient client) => client.LogInAsync(
        fixture.BaseAddress,
        DocsFlowAppFixture.UserEmail,
        DocsFlowAppFixture.UserPassword,
        Ct);

    /// <summary>Достаёт значение query-параметра. Отдельный метод, чтобы не тянуть в тесты System.Web.</summary>
    private static string? QueryValue(Uri uri, string name)
    {
        foreach (var pair in uri.Query.TrimStart('?').Split('&'))
        {
            var separator = pair.IndexOf('=');

            if (separator > 0 && pair[..separator] == name)
            {
                return Uri.UnescapeDataString(pair[(separator + 1)..]);
            }
        }

        return null;
    }
}
