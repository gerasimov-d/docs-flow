using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DocsFlow.Storage;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует <see cref="IObjectStorage"/> поверх S3-совместимого хранилища.
    /// Настройки читаются из секции <see cref="S3StorageOptions.SectionName"/>
    /// и проверяются на старте приложения.
    /// </summary>
    public static IServiceCollection AddS3ObjectStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<S3StorageOptions>()
            .Bind(configuration.GetSection(S3StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<S3StorageOptions>>().Value;

            var config = new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                ForcePathStyle = options.ForcePathStyle,
                AuthenticationRegion = options.Region,
            };

            return new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
        });

        services.AddSingleton<IObjectStorage, S3ObjectStorage>();

        return services;
    }
}
