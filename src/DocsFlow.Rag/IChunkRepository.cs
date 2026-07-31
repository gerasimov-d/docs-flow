namespace DocsFlow.Rag;

/// <summary>Фрагмент с посчитанным вектором — то, что уходит в хранилище.</summary>
public sealed record ChunkEmbedding(int Ordinal, string Content, ReadOnlyMemory<float> Embedding);

/// <summary>
/// Хранилище фрагментов с эмбеддингами поверх pgvector.
/// </summary>
public interface IChunkRepository
{
    /// <summary>
    /// Заменяет все фрагменты источника новыми — одной транзакцией. Повторная индексация того же
    /// <paramref name="sourceKey"/> не плодит дубли и не оставляет хвостов от прежней версии текста.
    /// </summary>
    /// <param name="embeddingModel">Модель, которой посчитаны вектора: пространства разных моделей несравнимы.</param>
    Task ReplaceAsync(
        string sourceKey,
        IReadOnlyList<ChunkEmbedding> chunks,
        string embeddingModel,
        CancellationToken cancellationToken = default);

    /// <summary>Ищет ближайшие к запросу фрагменты по косинусной близости.</summary>
    /// <param name="minScore">Отсечка близости: всё, что дальше, не возвращается вовсе.</param>
    Task<IReadOnlyList<ChunkMatch>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK,
        double minScore,
        CancellationToken cancellationToken = default);

    /// <summary>Удаляет все фрагменты источника. Идемпотентно.</summary>
    /// <returns>Сколько строк удалено.</returns>
    Task<int> DeleteBySourceAsync(string sourceKey, CancellationToken cancellationToken = default);
}
