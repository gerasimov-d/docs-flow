using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.DependencyInjection;

namespace DocsFlow.Database.Migrator;

/// <summary>
/// Единая точка сборки раннера FluentMigrator. Один и тот же код используют консольный мигратор
/// и интеграционные тесты — так тесты проверяют ровно ту схему, что накатывается в проде.
/// </summary>
public static class MigrationRunnerFactory
{
    /// <summary>Накатывает все pending-миграции на указанную строку подключения.</summary>
    public static void MigrateUp(string connectionString)
    {
        using var serviceProvider = Build(connectionString);
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
    }

    private static ServiceProvider Build(string connectionString) =>
        new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(CreateDemoNotes).Assembly).For.Migrations())
            // Служебная таблица версий — в snake_case (см. SnakeCaseVersionTableMetaData).
            .AddScoped<IVersionTableMetaData, SnakeCaseVersionTableMetaData>()
            // Прогресс миграций в консоль — чтобы в логах one-shot контейнера было видно, что накатилось.
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(validateScopes: false);
}
