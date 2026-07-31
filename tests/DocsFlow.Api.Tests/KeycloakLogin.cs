using System.Net;
using System.Text.RegularExpressions;
using Shouldly;

namespace DocsFlow.Api.Tests;

/// <summary>
/// Проход входа так же, как это сделал бы браузер: редирект на Keycloak, отправка формы, возврат
/// на <c>/api/auth/signin-callback</c>. Вынесено из тестов входа, потому что нужно всем сценариям,
/// где требуется настоящая сессия, — включая сценарии с двумя разными пользователями.
/// </summary>
internal static class KeycloakLogin
{
    public static async Task LogInAsync(
        this BrowserClient client,
        string appBaseAddress,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        using var loginPage = await client.GetAsync("/api/auth/login", cancellationToken);

        var html = await loginPage.Content.ReadAsStringAsync(cancellationToken);

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
                ["username"] = email,
                ["password"] = password,
            }),
            cancellationToken);

        // Keycloak при неверных данных возвращает ту же страницу входа с ошибкой, без редиректа.
        // Поэтому проверяем не код ответа, а то, что цепочка редиректов вернула нас в приложение.
        client.LastRequestUri!.GetLeftPart(UriPartial.Authority).ShouldBe(
            new Uri(appBaseAddress).GetLeftPart(UriPartial.Authority),
            $"вход не завершился возвратом в приложение (остановились на {client.LastRequestUri}):\n"
            + Excerpt(await submitted.Content.ReadAsStringAsync(cancellationToken)));
    }

    /// <summary>Срезает разметку до читаемого объёма: в сообщении об ошибке нужна суть, а не вся страница.</summary>
    public static string Excerpt(string html)
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
}
