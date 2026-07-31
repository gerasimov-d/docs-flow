using Dapper;
using DocsFlow.Database;
using Pgvector;

namespace DocsFlow.Rag;

internal sealed class ChunkRepository : IChunkRepository
{
    // Порог применяется к уже отобранным соседям, а не в WHERE до сортировки: HNSW-индекс
    // работает на паре «ORDER BY <=> ... LIMIT», а фильтр по выражению заставил бы планировщик
    // считать расстояние для всей таблицы. С фильтром по space так поступить нельзя: чужой
    // фрагмент не должен доходить даже до ранжирования, поэтому он стоит до сортировки.
    private const string SearchSql =
        """
        SELECT source_key, ordinal, content, score
        FROM (
            SELECT source_key,
                   ordinal,
                   content,
                   1 - (embedding <=> @embedding) AS score
            FROM rag_chunks
            WHERE space_id = @spaceId
            ORDER BY embedding <=> @embedding
            LIMIT @topK
        ) AS nearest
        WHERE score >= @minScore
        ORDER BY score DESC
        """;

    private const string InsertSql =
        """
        INSERT INTO rag_chunks (id, space_id, source_key, ordinal, content, embedding, embedding_model)
        VALUES (@Id, @SpaceId, @SourceKey, @Ordinal, @Content, @Embedding, @EmbeddingModel)
        """;

    private const string DeleteSql =
        "DELETE FROM rag_chunks WHERE space_id = @spaceId AND source_key = @sourceKey";

    private readonly IDbConnectionFactory _connectionFactory;

    public ChunkRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task ReplaceAsync(
        Guid spaceId,
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
            DeleteSql,
            new { spaceId, sourceKey },
            transaction,
            cancellationToken: cancellationToken));

        if (chunks.Count > 0)
        {
            var rows = chunks
                .Select(chunk => new
                {
                    // UUIDv7 монотонен по времени, поэтому вставки не фрагментируют B-tree.
                    Id = Guid.CreateVersion7(),
                    SpaceId = spaceId,
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
        Guid spaceId,
        ReadOnlyMemory<float> queryEmbedding,
        int topK,
        double minScore,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        // Транзакция здесь не ради атомарности одного SELECT, а ради SET LOCAL: настройка обязана
        // умереть вместе с запросом, иначе уедет в пул вместе с соединением.
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Без итеративного обхода HNSW сначала берёт topK ближайших по всему индексу, и фильтр
        // по space выбрасывает из них чужие уже после отбора: space с небольшой долей корпуса
        // получал бы пустую выдачу при полном индексе. relaxed_order — потому что внешний запрос
        // всё равно пересортировывает по score.
        await connection.ExecuteAsync(new CommandDefinition(
            "SET LOCAL hnsw.iterative_scan = relaxed_order",
            transaction: transaction,
            cancellationToken: cancellationToken));

        var matches = await connection.QueryAsync<ChunkMatch>(new CommandDefinition(
            SearchSql,
            new { spaceId, embedding = new Vector(queryEmbedding), topK, minScore },
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return matches.ToArray();
    }

    public async Task<int> DeleteBySourceAsync(
        Guid spaceId,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteAsync(new CommandDefinition(
            DeleteSql,
            new { spaceId, sourceKey },
            cancellationToken: cancellationToken));
    }
}
