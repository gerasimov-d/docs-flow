using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace DocsFlow.Llm;

public static class ServiceCollectionExtensions
{
    private const string ChatHttpClientName = "llm-chat";
    private const string EmbeddingsHttpClientName = "llm-embeddings";

    /// <summary>
    /// Регистрирует клиентов LLM поверх OpenAI-совместимого провайдера: генератор эмбеддингов —
    /// всегда, <see cref="IChatClient"/> — только если генерация включена (<c>Llm:Chat:Enabled</c>).
    /// Настройки читаются из секций <c>Llm:Chat</c> и <c>Llm:Embeddings</c> и проверяются
    /// на старте приложения.
    /// </summary>
    public static IServiceCollection AddLlm(this IServiceCollection services, IConfiguration configuration)
    {
        AddEmbeddingGenerator(services, configuration);

        // Выключенная генерация — это отсутствующий в контейнере IChatClient, а не клиент,
        // падающий при вызове: потребитель видит отсутствие зависимости и деградирует штатно.
        // Читаем флаг напрямую из конфигурации, потому что от него зависит сам состав DI.
        if (configuration.GetSection(LlmChatOptions.SectionName).GetValue("Enabled", true))
        {
            AddChatClient(services, configuration);
        }

        return services;
    }

    private static void AddChatClient(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LlmChatOptions>()
            .Bind(configuration.GetSection(LlmChatOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        AddResilientHttpClient<LlmChatOptions>(services, ChatHttpClientName);

        services.AddChatClient(sp =>
            {
                var options = sp.GetRequiredService<IOptions<LlmChatOptions>>().Value;

                return OpenAiClientFactory
                    .Create(options, CreateHttpClient(sp, ChatHttpClientName))
                    .GetChatClient(options.Model)
                    .AsIChatClient();
            })
            .UseLogging();
    }

    private static void AddEmbeddingGenerator(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LlmEmbeddingsOptions>()
            .Bind(configuration.GetSection(LlmEmbeddingsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        AddResilientHttpClient<LlmEmbeddingsOptions>(services, EmbeddingsHttpClientName);

        services.AddEmbeddingGenerator(sp =>
            {
                var options = sp.GetRequiredService<IOptions<LlmEmbeddingsOptions>>().Value;

                return OpenAiClientFactory
                    .Create(options, CreateHttpClient(sp, EmbeddingsHttpClientName))
                    .GetEmbeddingClient(options.Model)
                    .AsIEmbeddingGenerator();
            })
            .UseLogging();
    }

    /// <summary>
    /// Именованный HttpClient со стандартным пайплайном устойчивости: таймаут на попытку и общий,
    /// ретраи с экспоненциальной задержкой и джиттером, circuit breaker.
    /// </summary>
    private static void AddResilientHttpClient<TOptions>(IServiceCollection services, string name)
        where TOptions : LlmEndpointOptions
    {
        services.AddHttpClient(name)
            // Собственный таймаут HttpClient (100 с по умолчанию) обрубил бы запрос раньше
            // пайплайна и мимо его логики ретраев — временем управляет только пайплайн.
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .AddStandardResilienceHandler()
            .Configure((resilience, sp) =>
            {
                var options = sp.GetRequiredService<IOptions<TOptions>>().Value;

                resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(options.AttemptTimeoutSeconds);
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(options.TotalTimeoutSeconds);
                resilience.Retry.MaxRetryAttempts = options.MaxRetryAttempts;

                // Окно наблюдения circuit breaker обязано быть не меньше двух таймаутов попытки,
                // иначе пакет заваливает валидацию настроек на старте.
                resilience.CircuitBreaker.SamplingDuration =
                    TimeSpan.FromSeconds(options.AttemptTimeoutSeconds * 2);
            });
    }

    private static HttpClient CreateHttpClient(IServiceProvider serviceProvider, string name)
        => serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(name);
}
