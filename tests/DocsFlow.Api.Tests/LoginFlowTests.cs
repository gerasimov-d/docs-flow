using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace DocsFlow.Api.Tests;

public sealed class LoginFlowTests(DocsFlowAppFixture fixture) : IClassFixture<DocsFlowAppFixture>
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
    /// Проходит вход так же, как это сделал бы браузер: редирект на Keycloak, отправка формы,
    /// возврат на <c>/api/auth/signin-callback</c>. Единственная проверка, подтверждающая, что
    /// realm, конфиг клиента и настройки cookie согласованы между собой.
    /// </summary>
    private async Task LogInAsync(BrowserClient client)
    {
        using var loginPage = await client.GetAsync("/api/auth/login", Ct);

        var html = await loginPage.Content.ReadAsStringAsync(Ct);

        // Тело в сообщении не роскошь: Keycloak отвечает на неверный запрос авторизации
        // человекочитаемой страницей, и без неё «400» ничего не объясняет.
        loginPage.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            $"ожидалась страница входа Keycloak, а пришло:\n{Excerpt(html)}");

        var action = ExtractLoginFormAction(html);

        using var submitted = await client.PostAsync(
            action,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = DocsFlowAppFixture.UserEmail,
                ["password"] = DocsFlowAppFixture.UserPassword,
            }),
            Ct);

        // Keycloak при неверных данных возвращает ту же страницу входа с ошибкой, без редиректа.
        // Поэтому проверяем не код ответа, а то, что цепочка редиректов вернула нас в приложение.
        client.LastRequestUri!.GetLeftPart(UriPartial.Authority).ShouldBe(
            new Uri(fixture.BaseAddress).GetLeftPart(UriPartial.Authority),
            $"вход не завершился возвратом в приложение (остановились на {client.LastRequestUri}):\n"
            + Excerpt(await submitted.Content.ReadAsStringAsync(Ct)));
    }

    /// <summary>
    /// Достаёт адрес отправки формы со страницы входа Keycloak. Тест зависит от её разметки —
    /// осознанная плата за проверку настоящего flow, поэтому образ Keycloak пинуется точной версией.
    /// </summary>
    private static string ExtractLoginFormAction(string html)
    {
        foreach (var form in Regex.Matches(html, "<form[^>]*>", RegexOptions.IgnoreCase))
        {
            var tag = ((Match)form).Value;

            if (!tag.Contains("kc-form-login", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var action = Regex.Match(tag, "action=\"(?<action>[^\"]+)\"", RegexOptions.IgnoreCase);

            if (action.Success)
            {
                return WebUtility.HtmlDecode(action.Groups["action"].Value);
            }
        }

        throw new InvalidOperationException(
            "На странице Keycloak не нашлась форма входа kc-form-login — вероятно, изменилась её разметка.");
    }

    /// <summary>Срезает разметку до читаемого объёма: в сообщении об ошибке нужна суть, а не вся страница.</summary>
    private static string Excerpt(string html)
    {
        // Скрипты и стили выкидываются целиком: иначе полезный текст не попадает в выжимку.
        var text = Regex.Replace(
            html,
            "<(script|style)[^>]*>.*?</\\1>",
            " ",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        text = Regex.Replace(text, "<[^>]+>", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text.Length > 400 ? text[..400] : text;
    }
}
