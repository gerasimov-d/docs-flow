using Dapper;
using DocsFlow.Database;
using DocsFlow.Database.Migrator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DocsFlow.Rag.Tests;

/// <summary>
/// Накатывает миграции на отдельную базу тестового класса и собирает пайплайн тем же
/// <c>AddRag</c>, которым его получит приложение. В сеть тесты не ходят: модель эмбеддингов
/// подменена фейком, клиент чата не зарегистрирован вовсе.
/// </summary>
/// <remarks>
/// Контейнер общий на сборку (<see cref="PostgresContainerFixture"/>), а база — своя у каждого
/// класса: классы идут параллельно, и на общей базе чистка индекса в одном классе сносила бы
/// данные другого.
/// </remarks>
public sealed class PgVectorFixture(PostgresContainerFixture postgres) : IAsyncLifetime
{
    /// <summary>Совпадает с размерностью колонки в миграции.</summary>
    public const int Dimensions = 1024;

    private ServiceProvider? _services;

    public IServiceProvider Services => _services
        ?? throw new InvalidOperationException("Фикстура не инициализирована.");

    public async ValueTask InitializeAsync()
    {
        // Имя базы — от класса, который фикстуру запросил: в логах Postgres видно, чьи запросы.
        // В нижнем регистре, потому что Postgres так приводит имена без кавычек.
        var databaseName = (TestContext.Current.TestClass?.TestClassSimpleName ?? "rag").ToLowerInvariant();

        var connectionString = await postgres.CreateDatabaseAsync(databaseName);

        MigrationRunnerFactory.MigrateUp(connectionString);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Postgres:ConnectionString"] = connectionString,
            })
            .Build();

        _services = new ServiceCollection()
            .AddLogging()
            .AddPostgresDatabase(configuration)
            .AddRag(configuration)
            .AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(new FakeEmbeddingGenerator(Dimensions))
            .BuildServiceProvider();
    }

    /// <summary>Чистит индекс: база у тестового класса одна, а поиск идёт по всей таблице.</summary>
    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        var factory = Services.GetRequiredService<IDbConnectionFactory>();

        await using var connection = await factory.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            "TRUNCATE rag_chunks",
            cancellationToken: cancellationToken));
    }

    public async Task<int> CountChunksAsync(string sourceKey, CancellationToken cancellationToken)
    {
        var factory = Services.GetRequiredService<IDbConnectionFactory>();

        await using var connection = await factory.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT count(*) FROM rag_chunks WHERE source_key = @sourceKey",
            new { sourceKey },
            cancellationToken: cancellationToken));
    }

    public async ValueTask DisposeAsync()
    {
        // Инициализация могла оборваться на создании базы — сервисов тогда ещё нет. Падение здесь
        // xUnit пришивает к каждому тесту отдельной записью Test Class Cleanup Failure: счётчик
        // тестов растёт, а настоящая причина сбоя теряется среди них. Базу за собой не убираем —
        // её уносит контейнер.
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }
    }
}
