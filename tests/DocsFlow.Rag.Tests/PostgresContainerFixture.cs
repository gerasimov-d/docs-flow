using System.Data.Common;
using Testcontainers.PostgreSql;
using Xunit;

[assembly: AssemblyFixture(typeof(DocsFlow.Rag.Tests.PostgresContainerFixture))]

namespace DocsFlow.Rag.Tests;

/// <summary>
/// Один Postgres на всю сборку. Базы внутри него раздаются по одной на тестовый класс — см.
/// <see cref="CreateDatabaseAsync"/>.
/// </summary>
/// <remarks>
/// Контейнер на класс, а не на сборку, обходился в четыре Postgres на эту сборку. При полном
/// <c>dotnet test</c> тестовые сборки стартуют параллельно, и лишние контейнеры упирали Docker
/// в таймаут инициализации resource reaper — прогон падал с <c>ResourceReaperException</c> там,
/// где тесты ни при чём.
/// </remarks>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    // Образ пинуется той же версией, что и в docker-compose.yml.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("pgvector/pgvector:0.8.6-pg17")
        .Build();

    public ValueTask InitializeAsync() => new(_container.StartAsync());

    /// <summary>
    /// Заводит пустую базу и отдаёт строку подключения к ней. Своя база у каждого класса нужна
    /// затем, что классы в сборке идут параллельно, а тесты чистят индекс целиком (TRUNCATE):
    /// на общей базе такая чистка сносила бы данные соседнего класса.
    /// </summary>
    public async Task<string> CreateDatabaseAsync(string name)
    {
        // Имя приходит из имени тестового класса, но в SQL оно всё равно попадает как идентификатор:
        // кавычки экранируются, чтобы имя не могло разорвать запрос.
        var quoted = name.Replace("\"", "\"\"");

        var result = await _container.ExecScriptAsync($"CREATE DATABASE \"{quoted}\"");

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Не удалось создать базу {name}: {result.Stderr}");
        }

        var builder = new DbConnectionStringBuilder { ConnectionString = _container.GetConnectionString() };
        builder["Database"] = name;

        return builder.ConnectionString;
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
