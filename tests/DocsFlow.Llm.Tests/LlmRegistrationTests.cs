using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace DocsFlow.Llm.Tests;

/// <summary>
/// Проверяет состав контейнера — сети здесь нет: клиенты создаются, но никуда не ходят.
/// </summary>
public sealed class LlmRegistrationTests
{
    [Fact]
    public void Both_clients_are_registered_by_default()
    {
        var services = Build(ValidSettings());

        services.GetService<IEmbeddingGenerator<string, Embedding<float>>>().ShouldNotBeNull();
        services.GetService<IChatClient>().ShouldNotBeNull();
    }

    [Fact]
    public void Disabled_generation_leaves_no_chat_client_in_the_container()
    {
        var settings = ValidSettings();
        settings["Llm:Chat:Enabled"] = "false";

        var services = Build(settings);

        // Именно отсутствие сервиса, а не клиент-заглушка: потребитель обязан увидеть,
        // что генерации нет, и деградировать до поиска.
        services.GetService<IChatClient>().ShouldBeNull();
        services.GetService<IEmbeddingGenerator<string, Embedding<float>>>().ShouldNotBeNull();
    }

    [Fact]
    public void Disabled_generation_does_not_require_chat_settings()
    {
        // Выключенной генерации незачем требовать адрес и модель: конфиг установки без LLM
        // должен быть пустым, а не заполненным заглушками ради валидации.
        var services = Build(new Dictionary<string, string?>
        {
            ["Llm:Chat:Enabled"] = "false",
            ["Llm:Embeddings:Endpoint"] = "http://localhost:11434/v1",
            ["Llm:Embeddings:Model"] = "bge-m3",
        });

        Should.NotThrow(() => services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>());
    }

    [Fact]
    public void Zero_retries_are_rejected_by_configuration_validation()
    {
        var settings = ValidSettings();
        settings["Llm:Chat:MaxRetryAttempts"] = "0";

        var services = Build(settings);

        // Стратегия ретраев требует минимум одной попытки. Без этого ограничения в опциях
        // ноль прошёл бы валидацию конфига и уронил приложение на старте ошибкой из недр Polly.
        Should.Throw<OptionsValidationException>(() => services.GetRequiredService<IChatClient>());
    }

    [Fact]
    public void A_local_endpoint_without_an_api_key_is_accepted()
    {
        var settings = ValidSettings();
        settings.Remove("Llm:Chat:ApiKey");
        settings.Remove("Llm:Embeddings:ApiKey");

        var services = Build(settings);

        // Локальные серверы ключа не спрашивают, а клиент OpenAI не принимает пустую строку —
        // если бы заглушка не подставлялась, создание клиента упало бы здесь.
        Should.NotThrow(() => services.GetRequiredService<IChatClient>());
    }

    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddLogging()
            .AddLlm(configuration)
            .BuildServiceProvider();
    }

    private static Dictionary<string, string?> ValidSettings() => new()
    {
        ["Llm:Chat:Endpoint"] = "http://localhost:11434/v1",
        ["Llm:Chat:Model"] = "llama3.1",
        ["Llm:Embeddings:Endpoint"] = "http://localhost:11434/v1",
        ["Llm:Embeddings:Model"] = "bge-m3",
    };
}
