using DocsFlow.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace DocsFlow.Database.Tests;

public sealed class PostgresOptionsValidationTests
{
    private static IOptions<PostgresOptions> Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddPostgresDatabase(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<PostgresOptions>>();
    }

    [Fact]
    public void A_missing_connection_string_is_rejected()
        => Should.Throw<OptionsValidationException>(() => Resolve(new Dictionary<string, string?>()).Value);

    [Fact]
    public void A_present_connection_string_is_accepted()
    {
        var options = Resolve(new Dictionary<string, string?>
        {
            ["Database:Postgres:ConnectionString"] = "Host=localhost;Database=docsflow;Username=u;Password=p",
        }).Value;

        options.ConnectionString.ShouldNotBeNullOrEmpty();
    }
}
