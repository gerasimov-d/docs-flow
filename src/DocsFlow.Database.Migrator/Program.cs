using DocsFlow.Database.Migrator;
using Microsoft.Extensions.Configuration;

// Строку подключения берём из Database:Postgres:ConnectionString. AddEnvironmentVariables
// превращает env Database__Postgres__ConnectionString (её задаёт docker compose) в этот ключ —
// то же имя секции, что у PostgresOptions в приложении.
var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration["Database:Postgres:ConnectionString"]
    ?? throw new InvalidOperationException(
        "Не задана строка подключения Database:Postgres:ConnectionString " +
        "(ожидается переменная окружения Database__Postgres__ConnectionString).");

MigrationRunnerFactory.MigrateUp(connectionString);
