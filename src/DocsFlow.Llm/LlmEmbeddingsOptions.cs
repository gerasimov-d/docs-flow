namespace DocsFlow.Llm;

/// <summary>
/// Настройки модели эмбеддингов. Секция <c>Llm:Embeddings</c>.
/// </summary>
/// <remarks>
/// Размерности вектора здесь намеренно нет: её диктует схема БД, поэтому её задаёт и запрашивает
/// у провайдера тот слой, который владеет хранилищем (<c>Rag:EmbeddingDimensions</c>). Две копии
/// одного числа в разных секциях конфига рано или поздно разъезжаются.
/// </remarks>
public sealed class LlmEmbeddingsOptions : LlmEndpointOptions
{
    public const string SectionName = "Llm:Embeddings";

    public LlmEmbeddingsOptions()
    {
        // Эмбеддинги считаются быстро, и ждать их дольше минуты смысла нет: при индексации
        // пачки документов длинный таймаут только растягивает общую деградацию.
        AttemptTimeoutSeconds = 30;
        TotalTimeoutSeconds = 120;
    }
}
