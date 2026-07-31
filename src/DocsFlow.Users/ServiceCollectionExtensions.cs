using Microsoft.Extensions.DependencyInjection;

namespace DocsFlow.Users;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует доступ к пользователям. Требует уже зарегистрированного
    /// <c>AddPostgresDatabase</c>: репозиторий работает поверх его <c>IDbConnectionFactory</c>
    /// и полагается на выставленную им snake_case-конвенцию Dapper.
    /// </summary>
    public static IServiceCollection AddUsers(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
