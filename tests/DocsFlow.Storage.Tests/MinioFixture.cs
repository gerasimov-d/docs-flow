using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Minio;
using Xunit;

namespace DocsFlow.Storage.Tests;

/// <summary>
/// Поднимает MinIO в контейнере и собирает <see cref="IObjectStorage"/> тем же путём,
/// которым его получит приложение — через <c>AddS3ObjectStorage</c>.
/// </summary>
public sealed class MinioFixture : IAsyncLifetime
{
    private const string Username = "docsflow";
    private const string Password = "docsflow-secret";
    private const string BucketName = "docsflow-test";

    // Образ пинуется той же версией, что и в docker-compose.yml.
    private readonly MinioContainer _container = new MinioBuilder("minio/minio:RELEASE.2025-09-07T16-13-09Z")
        .WithUsername(Username)
        .WithPassword(Password)
        .Build();

    private ServiceProvider? _services;

    public IObjectStorage Storage { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:S3:ServiceUrl"] = _container.GetConnectionString(),
                ["Storage:S3:AccessKey"] = Username,
                ["Storage:S3:SecretKey"] = Password,
                ["Storage:S3:BucketName"] = BucketName,
            })
            .Build();

        _services = new ServiceCollection()
            .AddS3ObjectStorage(configuration)
            .BuildServiceProvider();

        await _services.GetRequiredService<IAmazonS3>().PutBucketAsync(BucketName, TestContext.Current.CancellationToken);

        Storage = _services.GetRequiredService<IObjectStorage>();
    }

    public async ValueTask DisposeAsync()
    {
        // Инициализация могла оборваться на старте контейнера — сервисов тогда ещё нет.
        // Падение здесь xUnit пришивает к каждому тесту отдельной записью Test Class Cleanup
        // Failure: счётчик тестов растёт, а настоящая причина сбоя теряется среди них.
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}
