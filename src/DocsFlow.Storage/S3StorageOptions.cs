using System.ComponentModel.DataAnnotations;

namespace DocsFlow.Storage;

public sealed class S3StorageOptions
{
    public const string SectionName = "Storage:S3";

    /// <summary>Адрес S3-совместимого хранилища, например <c>http://localhost:9000</c>.</summary>
    [Required]
    [Url]
    public string ServiceUrl { get; init; } = null!;

    [Required]
    public string AccessKey { get; init; } = null!;

    [Required]
    public string SecretKey { get; init; } = null!;

    [Required]
    public string BucketName { get; init; } = null!;

    /// <summary>
    /// Адресация бакета путём (<c>host/bucket/key</c>), а не поддоменом. Обязательна для MinIO
    /// и большинства self-hosted реализаций.
    /// </summary>
    public bool ForcePathStyle { get; init; } = true;

    /// <summary>
    /// Регион для подписи SigV4. У S3-совместимых хранилищ регион обычно не значит ничего,
    /// но подпись без него не считается.
    /// </summary>
    public string Region { get; init; } = "us-east-1";
}
