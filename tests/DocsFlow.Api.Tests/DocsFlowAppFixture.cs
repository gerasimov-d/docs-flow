using DocsFlow.Database.Migrator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;
using Xunit;

namespace DocsFlow.Api.Tests;

/// <summary>
/// Поднимает Postgres и настоящий Keycloak в контейнерах и запускает приложение тем же
/// <c>Program</c>, что и в проде. Проверяет связку realm ↔ конфиг клиента ↔ настройки cookie —
/// то, что не ловится ни одним изолированным тестом.
/// </summary>
public sealed class DocsFlowAppFixture : IAsyncLifetime
{
    // Значения обязаны совпадать с infra/keycloak/realm-export.json.
    private const string Realm = "docsflow";
    private const string ClientId = "docsflow-web";
    private const string ClientSecret = "docsflow-web-dev-secret";

    private const string AdminUsername = "admin";
    private const string AdminPassword = "admin";

    public const string UserEmail = "tester@docsflow.local";
    public const string UserPassword = "test-password";

    private const string UserFirstName = "Тест";
    private const string UserLastName = "Тестировщик";

    /// <summary>Так Keycloak составит claim <c>name</c>, из которого берётся отображаемое имя.</summary>
    public const string UserDisplayName = UserFirstName + " " + UserLastName;

    // Образы пинуются теми же версиями, что и в docker-compose.yml. Точная версия, а не 26.7:
    // сквозной тест разбирает HTML страницы входа, и её разметка может измениться в патче.
    private readonly KeycloakContainer _keycloak = new KeycloakBuilder("quay.io/keycloak/keycloak:26.7.0")
        .WithUsername(AdminUsername)
        .WithPassword(AdminPassword)
        .WithResourceMapping(
            new FileInfo(Path.Combine(AppContext.BaseDirectory, "realm-export.json")),
            "/opt/keycloak/data/import/")
        .WithCommand("--import-realm")
        .Build();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17.6").Build();

    private DocsFlowApp _app = null!;

    /// <summary>Адрес приложения на реальном Kestrel.</summary>
    public string BaseAddress { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _keycloak.StartAsync());

        var connectionString = _postgres.GetConnectionString();
        MigrationRunnerFactory.MigrateUp(connectionString);

        var keycloakAddress = _keycloak.GetBaseAddress().TrimEnd('/');

        using var admin = new KeycloakAdminClient(keycloakAddress, Realm);
        await admin.AuthenticateAsync(AdminUsername, AdminPassword, TestContext.Current.CancellationToken);
        await admin.CreateUserAsync(
            UserEmail,
            UserPassword,
            UserFirstName,
            UserLastName,
            TestContext.Current.CancellationToken);

        _app = new DocsFlowApp(new Dictionary<string, string?>
        {
            ["Database:Postgres:ConnectionString"] = connectionString,
            ["Authentication:Keycloak:Authority"] = $"{keycloakAddress}/realms/{Realm}",
            ["Authentication:Keycloak:ClientId"] = ClientId,
            ["Authentication:Keycloak:ClientSecret"] = ClientSecret,
            ["Authentication:Keycloak:RequireHttps"] = "false",
            // Хранилище в этих тестах не участвует, но опции валидируются на старте.
            ["Storage:S3:ServiceUrl"] = "http://localhost:9000",
            ["Storage:S3:AccessKey"] = "unused",
            ["Storage:S3:SecretKey"] = "unused",
            ["Storage:S3:BucketName"] = "unused",
        });

        // Обращение к Services поднимает хост — только после этого известен порт.
        _ = _app.Services;
        BaseAddress = _app.BaseAddress;

        await admin.AllowRedirectsToAsync(ClientId, BaseAddress, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Клиент с собственным хранилищем cookie: сессия живёт в cookie, поэтому каждый тест,
    /// которому нужен отдельный вход, берёт отдельный клиент.
    /// </summary>
    public BrowserClient CreateClient() => new(BaseAddress);

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        await Task.WhenAll(_keycloak.DisposeAsync().AsTask(), _postgres.DisposeAsync().AsTask());
    }

    private sealed class DocsFlowApp : WebApplicationFactory<Program>
    {
        private readonly Dictionary<string, string?> _settings;

        public DocsFlowApp(Dictionary<string, string?> settings) => _settings = settings;

        private IHost? _kestrelHost;

        public string BaseAddress { get; private set; } = null!;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Не Development: иначе загрузится appsettings.Development.json со «своими» адресами
            // Keycloak и Postgres, а тестам нужны адреса контейнеров.
            builder.UseEnvironment("Testing");

            // UseSetting, а не ConfigureAppConfiguration: колбэки отложенного хост-билдера
            // применяются только к первому builder.Build(), а тесты ходят по второму хосту
            // (см. CreateHost). Настройки хоста переживают оба.
            foreach (var (key, value) in _settings)
            {
                builder.UseSetting(key, value);
            }
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Хост с TestServer нужен самой фабрике: она сразу приводит его IServer к TestServer
            // и без этого падает. Тесты по нему не ходят. Собирается первым — до того, как в
            // builder попадёт Kestrel.
            var testHost = builder.Build();

            // Рабочий хост — на настоящем Kestrel. TestServer здесь не годится: вход уводит
            // клиента редиректами на Keycloak в контейнере, а один HttpClient не может ходить
            // и в TestServer (он живёт только в памяти), и по сети.
            builder.ConfigureWebHost(web => web.UseKestrel().UseUrls("http://127.0.0.1:0"));

            _kestrelHost = builder.Build();
            _kestrelHost.Start();

            BaseAddress = _kestrelHost.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!
                .Addresses
                .First()
                .TrimEnd('/');

            testHost.Start();

            return testHost;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _kestrelHost?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
