using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocsFlow.Rag;

internal sealed class DocumentIndexer : IDocumentIndexer
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IChunkRepository _repository;
    private readonly RagOptions _options;
    private readonly ILogger<DocumentIndexer> _logger;

    public DocumentIndexer(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IChunkRepository repository,
        IOptions<RagOptions> options,
        ILogger<DocumentIndexer> logger)
    {
        _embeddingGenerator = embeddingGenerator;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> IndexAsync(string sourceKey, string text, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);

        var model = ResolveModelName();
        var chunks = TextChunker.Split(text, _options.ChunkSize, _options.ChunkOverlap);

        if (chunks.Count == 0)
        {
            await _repository.ReplaceAsync(sourceKey, [], model, cancellationToken);
            _logger.LogInformation("Источник {SourceKey} пуст, индекс очищен.", sourceKey);

            return 0;
        }

        var embedded = new List<ChunkEmbedding>(chunks.Count);

        // Фрагменты уходят пачками: провайдеры ограничивают и число строк в запросе, и его размер,
        // а по одному запросу на фрагмент индексация большого документа превращается в сотни round-trip.
        foreach (var batch in chunks.Chunk(_options.EmbeddingBatchSize))
        {
            var embeddings = await GenerateAsync(batch, cancellationToken);

            for (var i = 0; i < batch.Length; i++)
            {
                var vector = embeddings[i].Vector;

                if (vector.Length != _options.EmbeddingDimensions)
                {
                    throw new RagException(
                        $"Модель '{model}' вернула вектор размерности {vector.Length}, а колонка " +
                        $"rag_chunks.embedding рассчитана на {_options.EmbeddingDimensions}. " +
                        "Настройте Rag:EmbeddingDimensions под модель либо смените модель эмбеддингов; " +
                        "смена размерности схемы требует миграции и переиндексации.");
                }

                embedded.Add(new ChunkEmbedding(embedded.Count, batch[i], vector));
            }
        }

        await _repository.ReplaceAsync(sourceKey, embedded, model, cancellationToken);

        _logger.LogInformation(
            "Источник {SourceKey} проиндексирован: {ChunkCount} фрагментов, модель {Model}.",
            sourceKey,
            embedded.Count,
            model);

        return embedded.Count;
    }

    private async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        string[] batch,
        CancellationToken cancellationToken)
    {
        // Размерность просим явно: модели семейства text-embedding-3 умеют её усекать, и без
        // запроса вернули бы свою полную длину, которая в колонку уже не влезет.
        var options = new EmbeddingGenerationOptions { Dimensions = _options.EmbeddingDimensions };

        try
        {
            return await _embeddingGenerator.GenerateAsync(batch, options, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Деградировать здесь нечем: без эмбеддингов документ в индекс не попадёт, и тихий
            // «успех» с пустым индексом был бы хуже явной ошибки.
            throw new RagException($"Не удалось получить эмбеддинги: {e.Message}", e);
        }
    }

    private string ResolveModelName()
        => _embeddingGenerator.GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId ?? "unknown";
}
