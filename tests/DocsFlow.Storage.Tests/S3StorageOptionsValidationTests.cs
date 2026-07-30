using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace DocsFlow.Storage.Tests;

public sealed class S3StorageOptionsValidationTests
{
    private static readonly Dictionary<string, string?> ValidSettings = new()
    {
        ["Storage:S3:ServiceUrl"] = "http://localhost:9000",
        ["Storage:S3:AccessKey"] = "key",
        ["Storage:S3:SecretKey"] = "secret",
        ["Storage:S3:BucketName"] = "bucket",
    };

    private static IOptions<S3StorageOptions> Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddS3ObjectStorage(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<S3StorageOptions>>();
    }

    [Theory]
    [InlineData("Storage:S3:ServiceUrl")]
    [InlineData("Storage:S3:AccessKey")]
    [InlineData("Storage:S3:SecretKey")]
    [InlineData("Storage:S3:BucketName")]
    public void A_missing_required_setting_is_rejected(string missingKey)
    {
        var settings = new Dictionary<string, string?>(ValidSettings);
        settings.Remove(missingKey);

        Should.Throw<OptionsValidationException>(() => Resolve(settings).Value);
    }

    [Fact]
    public void Path_style_addressing_is_on_by_default()
    {
        var options = Resolve(new Dictionary<string, string?>(ValidSettings)).Value;

        options.ForcePathStyle.ShouldBeTrue();
        options.Region.ShouldBe("us-east-1");
    }
}
