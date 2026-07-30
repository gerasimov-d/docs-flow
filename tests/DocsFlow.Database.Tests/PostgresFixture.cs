using DocsFlow.Database;
using DocsFlow.Database.Migrator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace DocsFlow.Database.Tests;

/// <summary>
/// Поднимает Postgres в контейнере, накатывает миграции тем же кодом, что и мигратор, и собирает
/// сервисы через <c>AddPostgresDatabase</c> — тем же путём, которым их получит приложение.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    // Образ пинуется той же версией, что и в docker-compose.yml.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17.6")
        .Build();

    private ServiceProvider _services = null!;

    public IServiceProvider Services => _services;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();

        // Схему накатывает тот же раннер, что и в docker compose.
        MigrationRunnerFactory.MigrateUp(connectionString);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Postgres:ConnectionString"] = connectionString,
            })
            .Build();

        _services = new ServiceCollection()
            .AddPostgresDatabase(configuration)
            .BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _container.DisposeAsync();
    }
}
