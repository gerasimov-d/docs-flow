using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace DocsFlow.Rag.Tests;

public sealed class RagOptionsValidationTests
{
    [Fact]
    public void Defaults_are_valid()
    {
        var options = Resolve(new Dictionary<string, string?>());

        options.ChunkSize.ShouldBeGreaterThan(options.ChunkOverlap);
        options.EmbeddingDimensions.ShouldBe(PgVectorFixture.Dimensions);
    }

    [Fact]
    public void An_overlap_larger_than_the_chunk_is_rejected()
    {
        // Иначе окно не сдвигается вперёд, и нарезка текста не сходится.
        var error = Should.Throw<OptionsValidationException>(() => Resolve(new Dictionary<string, string?>
        {
            ["Rag:ChunkSize"] = "500",
            ["Rag:ChunkOverlap"] = "500",
        }));

        error.Message.ShouldContain(nameof(RagOptions.ChunkOverlap));
    }

    [Fact]
    public void A_score_threshold_outside_the_cosine_range_is_rejected()
        => Should.Throw<OptionsValidationException>(() => Resolve(new Dictionary<string, string?>
        {
            ["Rag:MinScore"] = "1.5",
        }));

    [Fact]
    public void A_zero_top_k_is_rejected()
        => Should.Throw<OptionsValidationException>(() => Resolve(new Dictionary<string, string?>
        {
            ["Rag:TopK"] = "0",
        }));

    private static RagOptions Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(settings)
            {
                ["Database:Postgres:ConnectionString"] = "Host=localhost;Database=docsflow;Username=u;Password=p",
            })
            .Build();

        return new ServiceCollection()
            .AddRag(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<RagOptions>>()
            .Value;
    }
}
