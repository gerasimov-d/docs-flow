using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace DocsFlow.Api.Forwarding;

public static class ForwardedHeadersServiceCollectionExtensions
{
    /// <summary>
    /// Настраивает разбор заголовков <c>X-Forwarded-*</c> от доверенных прокси. Нужен потому, что
    /// приложение работает за nginx: без этого оно видит схему и адрес клиента такими, какими их
    /// подставил прокси-хоп, и собирает <c>redirect_uri</c> с <c>http</c> там, где браузер пришёл
    /// по <c>https</c>. Доверенные прокси читаются из секции
    /// <see cref="TrustedProxyOptions.SectionName"/>.
    /// </summary>
    public static IServiceCollection AddForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var trusted = configuration.GetSection(TrustedProxyOptions.SectionName).Get<TrustedProxyOptions>()
            ?? new TrustedProxyOptions();

        // Разбираем сразу, а не внутри Configure: неверный адрес в конфиге должен ронять запуск
        // с понятным сообщением, а не всплывать на первом запросе.
        var addresses = trusted.Addresses.Select(ParseAddress).ToList();
        var networks = trusted.Networks.Select(ParseNetwork).ToList();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            // X-Forwarded-Host не берём: nginx передаёт исходный Host как есть
            // (proxy_set_header Host $host), поэтому доверять ещё одному заголовку с тем же
            // смыслом незачем — это лишняя возможность подменить адрес.
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Значения добавляются к дефолтным (loopback), а не заменяют их: приложение,
            // запущенное рядом с прокси на той же машине, продолжает работать.
            foreach (var address in addresses)
            {
                options.KnownProxies.Add(address);
            }

            foreach (var network in networks)
            {
                // KnownIPNetworks, а не устаревший KnownNetworks (ASPDEPR005).
                options.KnownIPNetworks.Add(network);
            }
        });

        return services;
    }

    private static IPAddress ParseAddress(string value) =>
        IPAddress.TryParse(value, out var address)
            ? address
            : throw new InvalidOperationException(
                $"В {TrustedProxyOptions.SectionName}:Addresses значение «{value}» не является IP-адресом.");

    // Разбор строгий: адрес обязан быть началом диапазона, а длина префикса — в допустимых пределах.
    private static System.Net.IPNetwork ParseNetwork(string value) =>
        System.Net.IPNetwork.TryParse(value, out var network)
            ? network
            : throw new InvalidOperationException(
                $"В {TrustedProxyOptions.SectionName}:Networks значение «{value}» не является сетью в нотации CIDR.");
}
