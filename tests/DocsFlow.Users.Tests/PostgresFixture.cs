using DocsFlow.Database;
using DocsFlow.Database.Migrator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace DocsFlow.Users.Tests;

/// <summary>
/// Поднимает Postgres в контейнере, накатывает миграции тем же кодом, что и мигратор, и собирает
/// сервисы через <c>AddPostgresDatabase</c> + <c>AddUsers</c> — тем же путём, которым их получит
/// приложение.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    // Образ пинуется той же версией, что и в docker-compose.yml.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("pgvector/pgvector:0.8.6-pg17")
        .Build();

    private ServiceProvider _services = null!;

    public IServiceProvider Services => _services;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();

        MigrationRunnerFactory.MigrateUp(connectionString);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Postgres:ConnectionString"] = connectionString,
            })
            .Build();

        _services = new ServiceCollection()
            .AddPostgresDatabase(configuration)
            .AddUsers()
            .BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _container.DisposeAsync();
    }
}
