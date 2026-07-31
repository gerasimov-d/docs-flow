using DocsFlow.Database;
using DocsFlow.Database.Migrator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

[assembly: AssemblyFixture(typeof(DocsFlow.Database.Tests.PostgresFixture))]

namespace DocsFlow.Database.Tests;

/// <summary>
/// Поднимает Postgres в контейнере, накатывает миграции тем же кодом, что и мигратор, и собирает
/// сервисы через <c>AddPostgresDatabase</c> — тем же путём, которым их получит приложение.
/// </summary>
/// <remarks>
/// Фикстура уровня сборки, а не класса: на классе контейнер поднимался бы столько раз, сколько
/// в сборке тестовых классов. При полном <c>dotnet test</c> тестовые сборки стартуют параллельно,
/// и лишние контейнеры упирали Docker в таймаут инициализации resource reaper — прогон падал
/// с <c>ResourceReaperException</c> там, где тесты ни при чём.
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    // Образ пинуется той же версией, что и в docker-compose.yml.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("pgvector/pgvector:0.8.6-pg17")
        .Build();

    private ServiceProvider? _services;

    public IServiceProvider Services => _services
        ?? throw new InvalidOperationException("Фикстура не инициализирована.");

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
        // Инициализация могла оборваться на старте контейнера — сервисов тогда ещё нет.
        // Падение здесь xUnit пришивает к каждому тесту отдельной записью Test Class Cleanup
        // Failure: счётчик тестов растёт, а настоящая причина сбоя теряется среди них.
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}
