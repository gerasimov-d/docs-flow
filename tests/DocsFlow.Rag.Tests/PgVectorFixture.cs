using Dapper;
using DocsFlow.Database;
using DocsFlow.Database.Migrator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace DocsFlow.Rag.Tests;

/// <summary>
/// Поднимает Postgres с pgvector, накатывает миграции тем же кодом, что и мигратор, и собирает
/// пайплайн тем же <c>AddRag</c>, которым его получит приложение. В сеть тесты не ходят: модель
/// эмбеддингов подменена фейком, клиент чата не зарегистрирован вовсе.
/// </summary>
public sealed class PgVectorFixture : IAsyncLifetime
{
    /// <summary>Совпадает с размерностью колонки в миграции.</summary>
    public const int Dimensions = 1024;

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
            .AddLogging()
            .AddPostgresDatabase(configuration)
            .AddRag(configuration)
            .AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(new FakeEmbeddingGenerator(Dimensions))
            .BuildServiceProvider();
    }

    /// <summary>Чистит индекс: база у тестового класса одна, а поиск идёт по всей таблице.</summary>
    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        var factory = _services.GetRequiredService<IDbConnectionFactory>();

        await using var connection = await factory.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            "TRUNCATE rag_chunks",
            cancellationToken: cancellationToken));
    }

    public async Task<int> CountChunksAsync(string sourceKey, CancellationToken cancellationToken)
    {
        var factory = _services.GetRequiredService<IDbConnectionFactory>();

        await using var connection = await factory.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT count(*) FROM rag_chunks WHERE source_key = @sourceKey",
            new { sourceKey },
            cancellationToken: cancellationToken));
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _container.DisposeAsync();
    }
}
