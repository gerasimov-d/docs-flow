using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocsFlow.Rag;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует пайплайн RAG: хранилище фрагментов, индексатор и сервис вопросов.
    /// Настройки читаются из секции <see cref="RagOptions.SectionName"/> и проверяются на старте.
    /// </summary>
    /// <remarks>
    /// Требует уже зарегистрированных <c>AddPostgresDatabase</c> (соединения и маппинг типа
    /// <c>vector</c>) и генератора эмбеддингов — его даёт <c>AddLlm</c>. Клиент чата опционален:
    /// без него сервис деградирует до поиска по смыслу.
    /// </remarks>
    public static IServiceCollection AddRag(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RagOptions>()
            .Bind(configuration.GetSection(RagOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IChunkRepository, ChunkRepository>();
        services.AddScoped<IDocumentIndexer, DocumentIndexer>();
        services.AddScoped<IRagService, RagService>();

        return services;
    }
}
