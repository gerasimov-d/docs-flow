using Microsoft.Extensions.DependencyInjection;

namespace DocsFlow.Spaces;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует доступ к space и контекстам. Требует уже зарегистрированного
    /// <c>AddPostgresDatabase</c>: репозитории работают поверх его <c>IDbConnectionFactory</c>
    /// и полагаются на выставленную им snake_case-конвенцию Dapper.
    /// </summary>
    public static IServiceCollection AddSpaces(this IServiceCollection services)
    {
        services.AddScoped<ISpaceRepository, SpaceRepository>();
        services.AddScoped<IContextRepository, ContextRepository>();

        return services;
    }
}
