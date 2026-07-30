# Слой БД: Postgres + Dapper + FluentMigrator

**Дата:** 2026-07-30
**Ветка:** `db/postgres-scaffold` (worktree от `origin/dev`)
**Статус:** одобрено владельцем

## Цель

Собрать полную инфраструктуру работы с Postgres, чтобы последующие продуктовые задачи
делались «на готовом»: подключение, раннер миграций, конвенции доступа к данным, тесты.
Доменной модели пока нет — заводим одну **демонстрационную** таблицу, показывающую сквозной
паттерн «миграция → таблица → репозиторий». Реальная схема появится отдельными задачами.

Решения владельца, зафиксированные на брейншторме:
- Скоуп — полная инфраструктура + одна простая демо-таблица (не доменная модель).
- Миграции применяет **отдельный раннер** (не приложение), по аналогии с `minio-init`.

## Опора на существующие паттерны

- Эталон слайса — `DocsFlow.Storage`: интерфейс + реализация + `XxxOptions` с DataAnnotations
  и `ValidateOnStart` + `ServiceCollectionExtensions.AddXxx(IConfiguration)` +
  `InternalsVisibleTo` для тестов. Секции конфига вложенные.
- Эталон one-shot инфра-контейнера — `minio-init`: инфраструктура выполняет настройку, у
  приложения прав на настройку нет.
- Сборка: .NET 10, CPM (версии только в `Directory.Packages.props`), общие свойства в
  `Directory.Build.props`, решение `DocsFlow.slnx`.
- Тесты: Testcontainers + xunit.v3 + Shouldly; fixture поднимает контейнер и собирает сервис
  тем же `AddXxx`, что и приложение; образ пинуется той же версией, что в compose.
- Комментарии и XML-doc — русские; идентификаторы и имена файлов — английские.

## Архитектура: проекты

| Проект | Тип | Роль | Зависимости (NuGet) | Кто ссылается |
|---|---|---|---|---|
| `src/DocsFlow.Database` | class lib | Runtime-доступ к данным | `Npgsql`, `Dapper`, `Microsoft.Extensions.Options.*` | `DocsFlow.Api` |
| `src/DocsFlow.Database.Migrator` | console (Exe) | Применение миграций FluentMigrator | `FluentMigrator.Runner.Postgres`, `Npgsql`, `Microsoft.Extensions.*` | только compose-контейнер |
| `tests/DocsFlow.Database.Tests` | test | Интеграционные тесты | `Testcontainers.PostgreSql`, `xunit.v3`, `Shouldly` | — |

**Ключевой инвариант разделения:** `DocsFlow.Api` ссылается только на `DocsFlow.Database` и
**не тянет FluentMigrator**. Прав на DDL у приложения нет — схему накатывает мигратор, ровно
как бакет создаёт `minio-init`, а не API.

Рассмотренная и отклонённая альтернатива: вынести классы миграций в отдельную библиотеку
`DocsFlow.Database.Migrations`, чтобы тесты не ссылались на Exe. Отклонено как избыточное:
`ProjectReference` на console-проект в тестах работает штатно, классы миграций публичны.
Вынос тривиален, если позже понадобится.

Все три проекта добавляются в `DocsFlow.slnx` (папки `/src/` и `/tests/`).

## DocsFlow.Database (runtime-доступ)

### Конфигурация

- `PostgresOptions` — секция **`Database:Postgres`**:
  - `ConnectionString` : `string` — `[Required]`.
  - Строка подключения (а не раздельные host/port/...): для Npgsql это натуральнее — SSL,
    пулинг, таймауты живут в самой строке.
- Регистрация зеркалит `AddS3ObjectStorage`:
  ```csharp
  services.AddOptions<PostgresOptions>()
      .Bind(configuration.GetSection(PostgresOptions.SectionName))
      .ValidateDataAnnotations()
      .ValidateOnStart();
  ```

### Подключение

- `NpgsqlDataSource` — singleton, собирается из `NpgsqlDataSourceBuilder(options.ConnectionString)`
  (пулинг и логирование из коробки).
- Фабрика соединений:
  ```csharp
  public interface IDbConnectionFactory
  {
      // Возвращает уже открытое соединение из пула.
      ValueTask<DbConnection> OpenConnectionAsync(CancellationToken ct = default);
  }
  ```
  Реализация открывает соединение из `NpgsqlDataSource` (`OpenConnectionAsync`).

### Dapper + snake_case

`Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true` — выставляется один раз внутри
`AddPostgresDatabase`. Маппит `created_at` → `CreatedAt` без атрибутов на POCO.

### DI-расширение

`AddPostgresDatabase(this IServiceCollection services, IConfiguration configuration)`:
1. Регистрирует и валидирует `PostgresOptions` (см. выше).
2. Выставляет конвенцию Dapper.
3. Регистрирует `NpgsqlDataSource` (singleton), `IDbConnectionFactory` (singleton),
   репозитории (`DemoNoteRepository`).

`DocsFlow.Api/Program.cs` дополняется вызовом `builder.Services.AddPostgresDatabase(builder.Configuration);`.

`InternalsVisibleTo("DocsFlow.Database.Tests")` в csproj (по образцу Storage).

### Демо-таблица и репозиторий

POCO:
```csharp
// created_at (timestamptz) Npgsql отдаёт как DateTime (Kind=Utc) — это и есть натуральный CLR-тип.
public sealed record DemoNote(long Id, string Title, DateTime CreatedAt);
```
`DemoNoteRepository` через `IDbConnectionFactory` + Dapper:
- `Task<long> AddAsync(string title, CancellationToken ct)` — `INSERT ... RETURNING id`.
- `Task<DemoNote?> GetAsync(long id, CancellationToken ct)` — `SELECT ... WHERE id = @id`.

Назначение репозитория и таблицы — продемонстрировать сквозной паттерн, а не зафиксировать
домен. Удаляется без последствий, когда появится реальная схема.

## DocsFlow.Database.Migrator (раннер)

- Классы миграций живут здесь. Имена таблиц/колонок задаются **явно в snake_case**.
- Демо-миграция:
  ```csharp
  [Migration(20260730120000)]
  public sealed class CreateDemoNotes : Migration
  {
      public override void Up() =>
          Create.Table("demo_notes")
              .WithColumn("id").AsInt64().PrimaryKey().Identity()
              .WithColumn("title").AsString().NotNullable()
              .WithColumn("created_at").AsDateTimeOffset().NotNullable()
                  .WithDefault(SystemMethods.CurrentDateTimeOffset);

      public override void Down() => Delete.Table("demo_notes");
  }
  ```
- Служебная таблица версий — тоже snake_case через кастомный `IVersionTableMetaData`:
  `version_info(version, applied_on, description)`.
- `Program.cs`:
  ```csharp
  var configuration = new ConfigurationBuilder()
      .AddEnvironmentVariables()
      .Build();
  var connectionString = configuration["Database:Postgres:ConnectionString"]
      ?? throw new InvalidOperationException("Database:Postgres:ConnectionString не задана");

  var services = new ServiceCollection()
      .AddFluentMigratorCore()
      .ConfigureRunner(rb => rb
          .AddPostgres()
          .WithGlobalConnectionString(connectionString)
          .ScanIn(typeof(CreateDemoNotes).Assembly).For.Migrations())
      .BuildServiceProvider(false);

  using var scope = services.CreateScope();
  scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
  ```
  Строка подключения читается из `Database:Postgres:ConnectionString`. `AddEnvironmentVariables`
  превращает env `Database__Postgres__ConnectionString` (заданную в compose) в этот ключ — то же
  имя секции, что у `PostgresOptions` в приложении.

## docker compose

Добавляются два сервиса и один том. Стиль — как у существующих сервисов (env с дефолтами
`${VAR:-...}`, healthcheck, пиновка образа).

```yaml
  postgres:
    image: postgres:17.6
    environment:
      POSTGRES_USER: ${POSTGRES_USER:-docsflow}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-docsflow-secret}
      POSTGRES_DB: ${POSTGRES_DB:-docsflow}
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-docsflow} -d ${POSTGRES_DB:-docsflow}"]
      interval: 5s
      timeout: 3s
      retries: 12
      start_period: 5s

  # Схему накатывает инфраструктура, а не приложение: приложению права на DDL не нужны.
  migrator:
    build:
      context: .
      dockerfile: src/DocsFlow.Database.Migrator/Dockerfile
    depends_on:
      postgres:
        condition: service_healthy
    environment:
      Database__Postgres__ConnectionString: "Host=postgres;Port=5432;Username=${POSTGRES_USER:-docsflow};Password=${POSTGRES_PASSWORD:-docsflow-secret};Database=${POSTGRES_DB:-docsflow}"
```

Том `postgres-data` добавляется в секцию `volumes`.

**Новый артефакт — первый Dockerfile в репо** (следствие выбора «раннер как compose-сервис»):
- Multi-stage: `mcr.microsoft.com/dotnet/sdk:10.0` (build) → `mcr.microsoft.com/dotnet/runtime:10.0` (run).
- Контекст сборки — корень репозитория (нужны `Directory.Build.props`, `Directory.Packages.props`).
- Добавить `.dockerignore` (исключить `bin/`, `obj/`, `.git/`, `.idea/`).

## Тесты (DocsFlow.Database.Tests)

- `PostgresFixture : IAsyncLifetime` (по образцу `MinioFixture`):
  - Поднимает `PostgreSqlContainer` (`Testcontainers.PostgreSql`), образ пинуется = `postgres:17.6`.
  - Накатывает миграции тем же кодом раннера, что и мигратор (через `ProjectReference` на Migrator).
  - Собирает сервисы через `AddPostgresDatabase` с in-memory конфигом (как `MinioFixture`).
- Проверки:
  1. Миграции применяются: таблица `demo_notes` существует, `version_info` содержит запись.
  2. Round-trip `DemoNoteRepository`: `AddAsync` → `GetAsync` возвращает запись; `created_at`
     смаплен в `CreatedAt` (проверка snake_case-конвенции).
  3. Валидация опций падает без `ConnectionString` (зеркало `S3StorageOptionsValidationTests`).

## Пакеты (CPM → `Directory.Packages.props`)

Добавить `PackageVersion` для: `Npgsql`, `Dapper`, `FluentMigrator.Runner.Postgres`,
`Testcontainers.PostgreSql`. Точные версии пинуются при реализации (последние стабильные под
.NET 10); `Testcontainers.PostgreSql` выравнивается к уже стоящей `Testcontainers.Minio` `4.13.0`.

## Воркфлоу

- Вся работа — в worktree `db/postgres-scaffold` от `origin/dev`; основной репозиторий не трогаем.
- Регулярный `git fetch origin && git merge origin/dev`; интеграция в `dev` — по правилам CLAUDE.md
  (зелёные `dotnet build` и `dotnet test`, `push origin HEAD:dev`, без форса).

## Замечания по реализации (по итогам прогона)

Некритичные, но неочевидные вещи, на которые наткнётся следующая задача:

- **Мигратор напрямую ссылается на `Npgsql`.** `FluentMigrator.Runner.Postgres` не тянет Npgsql
  транзитивно — он грузит ADO.NET-драйвер рефлексией и ждёт, что `Npgsql.dll` окажется в output
  приложения. Без прямой ссылки контейнер-мигратор падает на `ValidateConnection`
  («Could not load file or assembly 'Npgsql'»). В тестах это не всплывает: там Npgsql приходит
  через `DocsFlow.Database`.
- **`timestamptz` ↔ `DateTime` (Kind=Utc).** Натуральный CLR-тип Npgsql для `timestamp with time
  zone` — `DateTime`, а не `DateTimeOffset`; Dapper подбирает конструктор записи по типам колонок,
  поэтому `DemoNote.CreatedAt` — `DateTime`.
- **`libgssapi-krb5-2` в runtime-образе мигратора.** Иначе Npgsql на старте пишет пугающую (но
  безобидную) ошибку про попытку подгрузить GSSAPI. Авторизация у нас по паролю, GSS не нужен.
- **Логи FluentMigrator в консоль** (`AddFluentMigratorConsole`) — чтобы в логах one-shot
  контейнера было видно, какие миграции применились.

## Вне скоупа

- Реальная доменная модель и её таблицы.
- Эндпоинты API поверх БД.
- Отдельная ограниченная роль БД для приложения (least-privilege разделение ролей мигратора и
  приложения) — осознанно отложено, сейчас YAGNI. Отметить как возможное будущее ужесточение.
```
