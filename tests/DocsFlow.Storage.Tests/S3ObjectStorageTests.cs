using System.Text;
using Shouldly;
using Xunit;

namespace DocsFlow.Storage.Tests;

public sealed class S3ObjectStorageTests(MinioFixture fixture) : IClassFixture<MinioFixture>
{
    private IObjectStorage Storage => fixture.Storage;

    private static string NewKey(string prefix = "objects") => $"{prefix}/{Guid.NewGuid():N}";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Put_then_get_returns_the_same_bytes()
    {
        var key = NewKey();
        var content = Encoding.UTF8.GetBytes("медицинская справка №42");

        await Storage.PutAsync(key, new MemoryStream(content), "text/plain", Ct);

        await using var stream = await Storage.GetAsync(key, Ct);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, Ct);

        buffer.ToArray().ShouldBe(content);
    }

    [Fact]
    public async Task Put_over_an_existing_key_overwrites_it()
    {
        var key = NewKey();

        await Storage.PutAsync(key, new MemoryStream("первая версия"u8.ToArray()), "text/plain", Ct);
        await Storage.PutAsync(key, new MemoryStream("вторая версия"u8.ToArray()), "text/plain", Ct);

        await using var stream = await Storage.GetAsync(key, Ct);
        using var reader = new StreamReader(stream);

        (await reader.ReadToEndAsync(Ct)).ShouldBe("вторая версия");
    }

    [Fact]
    public async Task Get_of_a_missing_key_throws_object_not_found()
    {
        var key = NewKey();

        var exception = await Should.ThrowAsync<ObjectNotFoundException>(() => Storage.GetAsync(key, Ct));

        exception.Key.ShouldBe(key);
    }

    [Fact]
    public async Task Metadata_reports_size_content_type_and_etag()
    {
        var key = NewKey();
        var content = Encoding.UTF8.GetBytes("выписка");

        await Storage.PutAsync(key, new MemoryStream(content), "application/pdf", Ct);

        var metadata = await Storage.GetMetadataAsync(key, Ct);

        metadata.ShouldNotBeNull();
        metadata.Size.ShouldBe(content.Length);
        metadata.ContentType.ShouldBe("application/pdf");
        metadata.ETag.ShouldNotBeNullOrWhiteSpace();
        metadata.ETag.ShouldNotStartWith("\"");
        metadata.LastModified.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task Metadata_of_a_missing_key_is_null()
        => (await Storage.GetMetadataAsync(NewKey(), Ct)).ShouldBeNull();

    [Fact]
    public async Task Exists_follows_the_lifetime_of_the_object()
    {
        var key = NewKey();

        (await Storage.ExistsAsync(key, Ct)).ShouldBeFalse();

        await Storage.PutAsync(key, new MemoryStream("паспорт"u8.ToArray()), "text/plain", Ct);
        (await Storage.ExistsAsync(key, Ct)).ShouldBeTrue();

        await Storage.DeleteAsync(key, Ct);
        (await Storage.ExistsAsync(key, Ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task Delete_of_a_missing_key_is_not_an_error()
        => await Should.NotThrowAsync(() => Storage.DeleteAsync(NewKey(), Ct));

    [Fact]
    public async Task List_returns_only_the_keys_under_the_prefix()
    {
        var prefix = $"list-scope/{Guid.NewGuid():N}";
        var expected = new[] { $"{prefix}/a", $"{prefix}/b", $"{prefix}/nested/c" };

        foreach (var key in expected.Append(NewKey("list-scope-other")))
        {
            await Storage.PutAsync(key, new MemoryStream("x"u8.ToArray()), "text/plain", Ct);
        }

        var actual = await Storage.ListAsync(prefix, Ct).ToListAsync(Ct);

        actual.ShouldBe(expected, ignoreOrder: true);
    }

    [Fact]
    public async Task List_transparently_walks_past_the_thousand_key_page_limit()
    {
        // S3 отдаёт максимум 1000 ключей за ответ. Меньший объём не проверяет continuation token.
        const int count = 1001;
        var prefix = $"list-paging/{Guid.NewGuid():N}";

        await Parallel.ForAsync(0, count, Ct, async (i, ct) =>
            await Storage.PutAsync($"{prefix}/{i:D5}", new MemoryStream("x"u8.ToArray()), "text/plain", ct));

        var keys = await Storage.ListAsync(prefix, Ct).ToListAsync(Ct);

        keys.Count.ShouldBe(count);
        keys.Distinct().Count().ShouldBe(count);
    }

    [Fact]
    public async Task List_of_an_empty_prefix_yields_nothing()
        => (await Storage.ListAsync($"empty/{Guid.NewGuid():N}", Ct).ToListAsync(Ct)).ShouldBeEmpty();

    [Fact]
    public async Task Presigned_url_is_downloadable_without_credentials()
    {
        var key = NewKey();
        var content = Encoding.UTF8.GetBytes("свидетельство о рождении");

        await Storage.PutAsync(key, new MemoryStream(content), "application/pdf", Ct);

        var url = Storage.GetPresignedUrl(key, TimeSpan.FromMinutes(5));

        using var http = new HttpClient();
        var response = await http.GetAsync(url, Ct);

        response.EnsureSuccessStatusCode();
        (await response.Content.ReadAsByteArrayAsync(Ct)).ShouldBe(content);
    }

    [Fact]
    public async Task Presigned_url_stops_working_once_the_ttl_has_passed()
    {
        var key = NewKey();
        await Storage.PutAsync(key, new MemoryStream("полис"u8.ToArray()), "text/plain", Ct);

        var url = Storage.GetPresignedUrl(key, TimeSpan.FromSeconds(1));
        await Task.Delay(TimeSpan.FromSeconds(2), Ct);

        using var http = new HttpClient();
        var response = await http.GetAsync(url, Ct);

        response.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task Unauthenticated_access_without_a_presigned_url_is_refused()
    {
        var key = NewKey();
        await Storage.PutAsync(key, new MemoryStream("снилс"u8.ToArray()), "text/plain", Ct);

        var signed = Storage.GetPresignedUrl(key, TimeSpan.FromMinutes(5));
        var unsigned = new UriBuilder(signed) { Query = string.Empty }.Uri;

        using var http = new HttpClient();
        var response = await http.GetAsync(unsigned, Ct);

        response.IsSuccessStatusCode.ShouldBeFalse();
    }
}
