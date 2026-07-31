using DocsFlow.Api.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace DocsFlow.Api.Tests;

public sealed class KeycloakOptionsValidationTests
{
    private static readonly Dictionary<string, string?> ValidSettings = new()
    {
        ["Authentication:Keycloak:Authority"] = "http://localhost:8080/realms/docsflow",
        ["Authentication:Keycloak:ClientId"] = "docsflow-web",
        ["Authentication:Keycloak:ClientSecret"] = "secret",
    };

    [Fact]
    public void An_empty_configuration_is_rejected()
        => Should.Throw<OptionsValidationException>(() => Resolve([]).Value);

    [Theory]
    [InlineData("Authentication:Keycloak:Authority")]
    [InlineData("Authentication:Keycloak:ClientId")]
    [InlineData("Authentication:Keycloak:ClientSecret")]
    public void A_missing_required_setting_is_rejected(string missingKey)
    {
        var settings = new Dictionary<string, string?>(ValidSettings);
        settings.Remove(missingKey);

        Should.Throw<OptionsValidationException>(() => Resolve(settings).Value);
    }

    [Fact]
    public void A_non_url_authority_is_rejected()
    {
        var settings = new Dictionary<string, string?>(ValidSettings)
        {
            ["Authentication:Keycloak:Authority"] = "не-адрес",
        };

        Should.Throw<OptionsValidationException>(() => Resolve(settings).Value);
    }

    [Fact]
    public void A_complete_configuration_is_accepted()
    {
        var options = Resolve(ValidSettings).Value;

        options.ClientId.ShouldBe("docsflow-web");
        // HTTPS требуется по умолчанию: ослабление защиты сессии должно быть записано в конфиге,
        // а не получаться само по себе там, где настройку забыли.
        options.RequireHttps.ShouldBeTrue();
    }

    private static IOptions<KeycloakOptions> Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddKeycloakAuthentication(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<KeycloakOptions>>();
    }
}
