using DocsFlow.Database;
using DocsFlow.Database.Migrator;
using DocsFlow.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

[assembly: AssemblyFixture(typeof(DocsFlow.Spaces.Tests.PostgresFixture))]

namespace DocsFlow.Spaces.Tests;

/// <summary>
/// Поднимает Postgres в контейнере, накатывает миграции тем же кодом, что и мигратор, и собирает
/// сервисы через <c>AddPostgresDatabase</c> + <c>AddUsers</c> + <c>AddSpaces</c> — тем же путём,
/// которым их получит приложение.
/// </summary>
/// <remarks>
/// Фикстура уровня сборки, а не класса: на классе контейнер поднимался бы столько раз, сколько
/// в сборке тестовых классов. База у классов общая — каждый тест заводит собственных пользователей
/// и собственные space, поэтому порядок и параллельность на них не влияют.
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
            .AddSpaces()
            .BuildServiceProvider();
    }

    /// <summary>
    /// Заводит пользователя тем же путём, что и вход через Keycloak. Каждый тест создаёт своих:
    /// база у классов общая.
    /// </summary>
    public async Task<Guid> CreateUserAsync(CancellationToken cancellationToken)
    {
        using var scope = Services.CreateScope();

        var subject = Guid.CreateVersion7().ToString();

        var user = await scope.ServiceProvider
            .GetRequiredService<IUserRepository>()
            .UpsertBySubjectAsync(
                new ExternalIdentity(subject, $"{subject}@docsflow.local", "Тестовый пользователь", true),
                cancellationToken);

        return user.Id;
    }

    /// <summary>Репозиторий в собственном scope — так же, как его берёт приложение на запрос.</summary>
    public ISpaceRepository Spaces(out IServiceScope scope)
    {
        scope = Services.CreateScope();

        return scope.ServiceProvider.GetRequiredService<ISpaceRepository>();
    }

    public IContextRepository Contexts(out IServiceScope scope)
    {
        scope = Services.CreateScope();

        return scope.ServiceProvider.GetRequiredService<IContextRepository>();
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
