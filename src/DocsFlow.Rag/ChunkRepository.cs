using Dapper;
using DocsFlow.Database;
using Pgvector;

namespace DocsFlow.Rag;

internal sealed class ChunkRepository : IChunkRepository
{
    // Порог применяется к уже отобранным соседям, а не в WHERE до сортировки: HNSW-индекс
    // работает на паре «ORDER BY <=> ... LIMIT», а фильтр по выражению заставил бы планировщик
    // считать расстояние для всей таблицы.
    private const string SearchSql =
        """
        SELECT source_key, ordinal, content, score
        FROM (
            SELECT source_key,
                   ordinal,
                   content,
                   1 - (embedding <=> @embedding) AS score
            FROM rag_chunks
            ORDER BY embedding <=> @embedding
            LIMIT @topK
        ) AS nearest
        WHERE score >= @minScore
        ORDER BY score DESC
        """;

    private const string InsertSql =
        """
        INSERT INTO rag_chunks (id, source_key, ordinal, content, embedding, embedding_model)
        VALUES (@Id, @SourceKey, @Ordinal, @Content, @Embedding, @EmbeddingModel)
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public ChunkRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task ReplaceAsync(
        string sourceKey,
        IReadOnlyList<ChunkEmbedding> chunks,
        string embeddingModel,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        // Удаление и вставка в одной транзакции: иначе между ними источник виден пустым,
        // и параллельный вопрос получил бы ответ «ничего не найдено» по существующему документу.
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM rag_chunks WHERE source_key = @sourceKey",
            new { sourceKey },
            transaction,
            cancellationToken: cancellationToken));

        if (chunks.Count > 0)
        {
            var rows = chunks
                .Select(chunk => new
                {
                    // UUIDv7 монотонен по времени, поэтому вставки не фрагментируют B-tree.
                    Id = Guid.CreateVersion7(),
                    SourceKey = sourceKey,
                    chunk.Ordinal,
                    chunk.Content,
                    Embedding = new Vector(chunk.Embedding),
                    EmbeddingModel = embeddingModel,
                })
                .ToArray();

            await connection.ExecuteAsync(new CommandDefinition(
                InsertSql,
                rows,
                transaction,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChunkMatch>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK,
        double minScore,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var matches = await connection.QueryAsync<ChunkMatch>(new CommandDefinition(
            SearchSql,
            new { embedding = new Vector(queryEmbedding), topK, minScore },
            cancellationToken: cancellationToken));

        return matches.ToArray();
    }

    public async Task<int> DeleteBySourceAsync(string sourceKey, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM rag_chunks WHERE source_key = @sourceKey",
            new { sourceKey },
            cancellationToken: cancellationToken));
    }
}
