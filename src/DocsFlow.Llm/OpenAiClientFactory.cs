using System.ClientModel;
using System.ClientModel.Primitives;
using OpenAI;

namespace DocsFlow.Llm;

/// <summary>
/// Собирает клиент OpenAI-совместимого API. Единственное место в решении, которое знает про SDK
/// конкретного вендора: наружу отдаются только абстракции Microsoft.Extensions.AI.
/// </summary>
internal static class OpenAiClientFactory
{
    // Клиент OpenAI не принимает пустой ключ, а локальным серверам (Ollama, vLLM, LM Studio)
    // ключ не нужен — подставляем заглушку, чтобы в конфиге локальной установки поле оставалось пустым.
    private const string PlaceholderApiKey = "no-key-required";

    public static OpenAIClient Create(LlmEndpointOptions options, HttpClient httpClient)
    {
        var apiKey = string.IsNullOrWhiteSpace(options.ApiKey) ? PlaceholderApiKey : options.ApiKey;

        return new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri(options.Endpoint),

            // Транспорт поверх HttpClient из фабрики: именно на нём висит пайплайн устойчивости.
            // Со своим внутренним HttpClient SDK ходил бы мимо таймаутов и circuit breaker.
            Transport = new HttpClientPipelineTransport(httpClient),

            // Ретраи оставлены пайплайну. Второй слой ретраев внутри SDK перемножился бы с ним:
            // три попытки там и три здесь — это девять запросов и совсем другое время отказа.
            RetryPolicy = new ClientRetryPolicy(maxRetries: 0),
        });
    }
}
