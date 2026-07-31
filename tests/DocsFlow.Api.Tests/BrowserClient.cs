using System.Net;
using System.Net.Http.Json;

namespace DocsFlow.Api.Tests;

/// <summary>
/// Клиент, который ведёт себя как браузер на переходах вход → Keycloak → возврат: сам идёт по
/// редиректам и хранит cookie отдельно для каждого хоста.
/// </summary>
/// <remarks>
/// Штатная связка <c>HttpClientHandler</c> + <c>CookieContainer</c> здесь не работает. Keycloak
/// помечает свои cookie (<c>KC_RESTART</c>, <c>AUTH_SESSION_ID</c>) флагом <c>Secure</c> даже когда
/// realm настроен без TLS. Браузеры считают <c>http://localhost</c> доверенным источником и такие
/// cookie всё равно возвращают, а <c>CookieContainer</c> исключения не делает: он их сохраняет, но
/// по http не отправляет — и Keycloak отвечает «Restart login cookie not found». Перехватить это
/// снаружи нельзя, cookie обрабатываются внутри самого обработчика, поэтому храним их сами.
/// </remarks>
public sealed class BrowserClient : IDisposable
{
    private const int MaxHops = 20;

    private readonly HttpClient _http = new(new HttpClientHandler
    {
        UseCookies = false,
        AllowAutoRedirect = false,
    });

    // (хост, имя) → значение. Путь не учитываем: в этом сценарии он ничего не различает.
    private readonly Dictionary<(string Host, string Name), string> _cookies = [];

    private readonly Uri _baseAddress;

    public BrowserClient(string baseAddress) => _baseAddress = new Uri(baseAddress);

    /// <summary>Адрес, на котором остановилась цепочка редиректов.</summary>
    public Uri? LastRequestUri { get; private set; }

    public Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, url, content: null, cancellationToken);

    public Task<HttpResponseMessage> PostAsync(
        string url,
        HttpContent? content,
        CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, url, content, cancellationToken);

    public Task<HttpResponseMessage> DeleteAsync(string url, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Delete, url, content: null, cancellationToken);

    /// <summary>Запрос с телом в JSON — так с API разговаривает веб-клиент.</summary>
    public Task<HttpResponseMessage> SendJsonAsync<T>(
        HttpMethod method,
        string url,
        T payload,
        CancellationToken cancellationToken) =>
        SendAsync(method, url, JsonContent.Create(payload), cancellationToken);

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string url,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        var uri = new Uri(_baseAddress, url);

        for (var hop = 0; hop < MaxHops; hop++)
        {
            using var request = new HttpRequestMessage(method, uri);

            if (content is not null)
            {
                request.Content = content;
            }

            ApplyCookies(request, uri);

            var response = await _http.SendAsync(request, cancellationToken);

            StoreCookies(response, uri);
            LastRequestUri = uri;

            if (!IsRedirect(response.StatusCode) || response.Headers.Location is null)
            {
                return response;
            }

            uri = new Uri(uri, response.Headers.Location);

            // Редирект после POST браузер выполняет как GET и без тела.
            method = HttpMethod.Get;
            content = null;

            response.Dispose();
        }

        throw new InvalidOperationException(
            $"Цепочка редиректов не закончилась за {MaxHops} переходов, последний адрес: {uri}.");
    }

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.MovedPermanently or
        HttpStatusCode.Found or
        HttpStatusCode.SeeOther or
        HttpStatusCode.TemporaryRedirect or
        HttpStatusCode.PermanentRedirect;

    private void ApplyCookies(HttpRequestMessage request, Uri uri)
    {
        var cookies = _cookies
            .Where(cookie => cookie.Key.Host == uri.Host)
            .Select(cookie => $"{cookie.Key.Name}={cookie.Value}")
            .ToList();

        if (cookies.Count > 0)
        {
            request.Headers.Add("Cookie", string.Join("; ", cookies));
        }
    }

    private void StoreCookies(HttpResponseMessage response, Uri uri)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var headers))
        {
            return;
        }

        foreach (var header in headers)
        {
            var pair = header.Split(';', 2)[0];
            var separator = pair.IndexOf('=');

            if (separator <= 0)
            {
                continue;
            }

            var name = pair[..separator].Trim();
            var value = pair[(separator + 1)..].Trim();
            var key = (uri.Host, name);

            // Пустое значение — это удаление cookie: так гасится сессия при выходе.
            if (value.Length == 0)
            {
                _cookies.Remove(key);
            }
            else
            {
                _cookies[key] = value;
            }
        }
    }

    public void Dispose() => _http.Dispose();
}
