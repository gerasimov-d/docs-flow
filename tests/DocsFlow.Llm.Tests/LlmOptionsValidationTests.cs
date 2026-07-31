using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace DocsFlow.Llm.Tests;

public sealed class LlmOptionsValidationTests
{
    [Fact]
    public void Missing_embeddings_settings_are_rejected()
        => Should.Throw<OptionsValidationException>(
            () => Resolve<LlmEmbeddingsOptions>(new Dictionary<string, string?>()));

    [Fact]
    public void A_malformed_endpoint_is_rejected()
        => Should.Throw<OptionsValidationException>(() => Resolve<LlmEmbeddingsOptions>(new Dictionary<string, string?>
        {
            ["Llm:Embeddings:Endpoint"] = "localhost:11434",
            ["Llm:Embeddings:Model"] = "bge-m3",
        }));

    [Fact]
    public void A_missing_chat_model_is_rejected()
        => Should.Throw<OptionsValidationException>(() => Resolve<LlmChatOptions>(new Dictionary<string, string?>
        {
            ["Llm:Chat:Endpoint"] = "http://localhost:11434/v1",
            ["Llm:Embeddings:Endpoint"] = "http://localhost:11434/v1",
            ["Llm:Embeddings:Model"] = "bge-m3",
        }));

    [Fact]
    public void Timeout_defaults_leave_room_for_a_slow_generation()
    {
        var options = Resolve<LlmChatOptions>(new Dictionary<string, string?>
        {
            ["Llm:Chat:Endpoint"] = "http://localhost:11434/v1",
            ["Llm:Chat:Model"] = "llama3.1",
            ["Llm:Embeddings:Endpoint"] = "http://localhost:11434/v1",
            ["Llm:Embeddings:Model"] = "bge-m3",
        });

        // Дефолты пакета устойчивости (10 с на попытку, 30 с всего) для генерации не годятся.
        options.AttemptTimeoutSeconds.ShouldBeGreaterThan(30);
        options.TotalTimeoutSeconds.ShouldBeGreaterThan(options.AttemptTimeoutSeconds);
    }

    private static TOptions Resolve<TOptions>(Dictionary<string, string?> settings)
        where TOptions : class
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddLogging()
            .AddLlm(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<TOptions>>()
            .Value;
    }
}
