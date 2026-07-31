using Microsoft.Extensions.AI;

namespace DocsFlow.Rag.Tests;

/// <summary>
/// Детерминированный генератор эмбеддингов: слова текста раскладываются по измерениям, вектор
/// нормируется. Тексты с общими словами оказываются близки по косинусу — этого хватает, чтобы
/// проверять ранжирование и пороги, не выходя в сеть.
/// </summary>
public sealed class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private static readonly char[] Separators = [' ', '\n', '\t', '.', ',', ':', ';', '!', '?', '(', ')', '"'];

    private readonly int _dimensions;
    private readonly bool _honorRequestedDimensions;

    /// <param name="honorRequestedDimensions">
    /// <c>false</c> имитирует провайдера, который не умеет усекать вектор и всегда отдаёт свою длину.
    /// </param>
    public FakeEmbeddingGenerator(int dimensions, bool honorRequestedDimensions = true)
    {
        _dimensions = dimensions;
        _honorRequestedDimensions = honorRequestedDimensions;
    }

    public string ModelId { get; init; } = "fake-embed";

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var dimensions = _honorRequestedDimensions ? options?.Dimensions ?? _dimensions : _dimensions;

        var embeddings = values
            .Select(value => new Embedding<float>(Vectorize(value, dimensions)) { ModelId = ModelId })
            .ToList();

        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    public static ReadOnlyMemory<float> Vectorize(string text, int dimensions)
    {
        var vector = new float[dimensions];

        foreach (var word in text.ToLowerInvariant().Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            vector[(int)(Hash(word) % (uint)dimensions)] += 1;
        }

        var norm = MathF.Sqrt(vector.Sum(value => value * value));

        // Нулевой вектор в pgvector даёт NaN при косинусном расстоянии, поэтому пустой текст
        // получает орт первого измерения.
        if (norm == 0)
        {
            vector[0] = 1;

            return vector;
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= norm;
        }

        return vector;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType == typeof(EmbeddingGeneratorMetadata)
            ? new EmbeddingGeneratorMetadata("fake", defaultModelId: ModelId)
            : null;

    public void Dispose()
    {
    }

    /// FNV-1a: в отличие от string.GetHashCode он одинаков между процессами, поэтому вектора
    /// воспроизводимы от прогона к прогону.
    private static uint Hash(string word)
    {
        var hash = 2166136261;

        foreach (var symbol in word)
        {
            hash = (hash ^ symbol) * 16777619;
        }

        return hash;
    }
}
