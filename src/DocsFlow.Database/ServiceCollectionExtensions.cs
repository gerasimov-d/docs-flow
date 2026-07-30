using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DocsFlow.Database;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует доступ к Postgres: пул соединений (<see cref="NpgsqlDataSource"/>),
    /// <see cref="IDbConnectionFactory"/> и репозитории. Настройки читаются из секции
    /// <see cref="PostgresOptions.SectionName"/> и проверяются на старте приложения.
    /// Миграции здесь не применяются — это делает отдельный раннер (DocsFlow.Database.Migrator).
    /// </summary>
    public static IServiceCollection AddPostgresDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection(PostgresOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Dapper сопоставляет snake_case-колонки (created_at) с PascalCase-свойствами (CreatedAt).
        // Настройка глобальная и идемпотентная.
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<PostgresOptions>>().Value;
            return new NpgsqlDataSourceBuilder(options.ConnectionString).Build();
        });

        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddScoped<DemoNoteRepository>();

        return services;
    }
}
