using System.Net;
using System.Runtime.CompilerServices;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace DocsFlow.Storage;

internal sealed class S3ObjectStorage : IObjectStorage
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;
    private readonly Protocol _protocol;

    public S3ObjectStorage(IAmazonS3 client, IOptions<S3StorageOptions> options)
    {
        _client = client;
        _bucket = options.Value.BucketName;

        // GetPreSignedUrlRequest.Protocol по умолчанию HTTPS и не наследуется от ServiceURL:
        // без этого presigned-ссылка на http-эндпоинт (MinIO локально) уедет на https.
        _protocol = new Uri(options.Value.ServiceUrl).Scheme == Uri.UriSchemeHttp
            ? Protocol.HTTP
            : Protocol.HTTPS;
    }

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        };

        try
        {
            await _client.PutObjectAsync(request, ct);
        }
        catch (AmazonS3Exception e)
        {
            throw Wrap(e, key, "записать");
        }
    }

    public async Task<Stream> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(_bucket, key, ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception e) when (IsNotFound(e))
        {
            throw new ObjectNotFoundException(key, e);
        }
        catch (AmazonS3Exception e)
        {
            throw Wrap(e, key, "прочитать");
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _client.DeleteObjectAsync(_bucket, key, ct);
        }
        catch (AmazonS3Exception e)
        {
            throw Wrap(e, key, "удалить");
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => await GetMetadataAsync(key, ct) is not null;

    public async Task<ObjectMetadata?> GetMetadataAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetObjectMetadataAsync(_bucket, key, ct);

            return new ObjectMetadata(
                Size: response.ContentLength,
                ContentType: response.Headers.ContentType ?? "application/octet-stream",
                ETag: response.ETag?.Trim('"') ?? string.Empty,
                LastModified: response.LastModified ?? default);
        }
        catch (AmazonS3Exception e) when (IsNotFound(e))
        {
            return null;
        }
        catch (AmazonS3Exception e)
        {
            throw Wrap(e, key, "получить метаданные");
        }
    }

    public async IAsyncEnumerable<string> ListAsync(
        string prefix,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix };

        while (true)
        {
            // Запрос вынесен в отдельный метод: try/catch не оборачивает yield return.
            var response = await ListPageAsync(request, ct);

            foreach (var s3Object in response.S3Objects ?? [])
            {
                yield return s3Object.Key;
            }

            if (response.IsTruncated is not true)
            {
                yield break;
            }

            request.ContinuationToken = response.NextContinuationToken;
        }
    }

    public Uri GetPresignedUrl(string key, TimeSpan ttl)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Protocol = _protocol,
            Expires = DateTime.UtcNow.Add(ttl),
        };

        try
        {
            return new Uri(_client.GetPreSignedURL(request));
        }
        catch (AmazonS3Exception e)
        {
            throw Wrap(e, key, "подписать ссылку на");
        }
    }

    private async Task<ListObjectsV2Response> ListPageAsync(ListObjectsV2Request request, CancellationToken ct)
    {
        try
        {
            return await _client.ListObjectsV2Async(request, ct);
        }
        catch (AmazonS3Exception e)
        {
            throw Wrap(e, request.Prefix ?? string.Empty, "перечислить объекты по префиксу");
        }
    }

    private static bool IsNotFound(AmazonS3Exception e) => e.StatusCode == HttpStatusCode.NotFound;

    private ObjectStorageException Wrap(AmazonS3Exception e, string key, string action)
        => new($"Не удалось {action} '{key}' в бакете '{_bucket}': {e.Message}", e);
}
