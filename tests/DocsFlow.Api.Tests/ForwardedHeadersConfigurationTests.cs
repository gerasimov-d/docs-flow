using System.Net;
using DocsFlow.Api.Forwarding;
// ForwardedHeadersOptions лежит в Microsoft.AspNetCore.Builder, флаги — в HttpOverrides.
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace DocsFlow.Api.Tests;

public sealed class ForwardedHeadersConfigurationTests
{
    [Fact]
    public void Trusted_addresses_and_networks_are_parsed()
    {
        var options = Resolve(new Dictionary<string, string?>
        {
            ["Network:TrustedProxies:Addresses:0"] = "10.0.0.7",
            ["Network:TrustedProxies:Networks:0"] = "172.16.0.0/12",
        });

        options.KnownProxies.ShouldContain(IPAddress.Parse("10.0.0.7"));
        options.KnownIPNetworks.ShouldContain(System.Net.IPNetwork.Parse("172.16.0.0/12"));
    }

    [Fact]
    public void Loopback_defaults_are_kept()
    {
        var withTrusted = Resolve(new Dictionary<string, string?>
        {
            ["Network:TrustedProxies:Networks:0"] = "172.16.0.0/12",
        });

        var withoutTrusted = Resolve([]);

        // Свои прокси добавляются к дефолтным, а не заменяют их: приложение, запущенное рядом
        // с прокси на той же машине, должно продолжать работать.
        withTrusted.KnownIPNetworks.Count.ShouldBe(withoutTrusted.KnownIPNetworks.Count + 1);
        withTrusted.KnownProxies.Count.ShouldBe(withoutTrusted.KnownProxies.Count);
    }

    [Fact]
    public void Only_protocol_and_client_address_are_taken_from_headers()
    {
        var options = Resolve([]);

        options.ForwardedHeaders.ShouldBe(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

        // X-Forwarded-Host намеренно не разбирается: nginx передаёт исходный Host как есть,
        // а доверие второму заголовку с тем же смыслом даёт лишний способ подменить адрес.
        options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedHost).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Network:TrustedProxies:Addresses:0", "не-адрес")]
    [InlineData("Network:TrustedProxies:Networks:0", "172.16.0.0")]
    [InlineData("Network:TrustedProxies:Networks:0", "172.16.0.0/999")]
    public void A_malformed_value_fails_the_startup(string key, string value)
    {
        // Падать нужно на старте: молча проигнорированный прокси означает, что приложение
        // тихо перестало доверять заголовкам и собирает ссылки с неверной схемой.
        Should.Throw<InvalidOperationException>(() => Resolve(new Dictionary<string, string?>
        {
            [key] = value,
        }));
    }

    private static ForwardedHeadersOptions Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddForwardedHeaders(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;
    }
}
