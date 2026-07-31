using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector.Dapper;

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

        // Тип vector из pgvector Dapper сам не читает и не пишет. Обработчик регистрируется здесь,
        // рядом с остальными конвенциями маппинга: это ответственность слоя доступа к данным,
        // а не того слайса, которому вектора понадобились.
        SqlMapper.AddTypeHandler(new VectorTypeHandler());

        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<PostgresOptions>>().Value;

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(options.ConnectionString);

            // Без UseVector Npgsql не знает типа vector и отправляет параметр как unknown.
            dataSourceBuilder.UseVector();

            return dataSourceBuilder.Build();
        });

        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddScoped<DemoNoteRepository>();

        return services;
    }
}
