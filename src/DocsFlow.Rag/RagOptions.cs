using System.ComponentModel.DataAnnotations;

namespace DocsFlow.Rag;

/// <summary>
/// Настройки пайплайна RAG. Секция <c>Rag</c>.
/// </summary>
public sealed class RagOptions : IValidatableObject
{
    public const string SectionName = "Rag";

    /// <summary>Целевой размер фрагмента в символах.</summary>
    [Range(200, 20000)]
    public int ChunkSize { get; init; } = 1000;

    /// <summary>
    /// Перекрытие соседних фрагментов в символах. Без него предложение, разрезанное границей
    /// фрагмента, теряется для поиска: ни одна из половин не содержит мысль целиком.
    /// </summary>
    [Range(0, 5000)]
    public int ChunkOverlap { get; init; } = 150;

    /// <summary>Сколько фрагментов уходит в один запрос эмбеддингов.</summary>
    [Range(1, 256)]
    public int EmbeddingBatchSize { get; init; } = 16;

    /// <summary>
    /// Размерность вектора. Обязана совпадать с размерностью колонки <c>rag_chunks.embedding</c>:
    /// её задаёт миграция, а здесь она запрашивается у провайдера и проверяется перед записью.
    /// </summary>
    [Range(1, 16000)]
    public int EmbeddingDimensions { get; init; } = 1024;

    /// <summary>Сколько фрагментов попадает в контекст ответа.</summary>
    [Range(1, 100)]
    public int TopK { get; init; } = 5;

    /// <summary>
    /// Минимальная косинусная близость фрагмента к вопросу. Отсекает случайные попадания:
    /// без отсечки поиск всегда возвращает <see cref="TopK"/> строк, даже когда в базе нет
    /// ничего похожего.
    /// </summary>
    [Range(0, 1)]
    public double MinScore { get; init; } = 0.2;

    /// <summary>
    /// Просить ли у провайдера нативный структурированный вывод (<c>response_format=json_schema</c>).
    /// Часть OpenAI-совместимых серверов его не поддерживает — тогда <c>false</c> переводит
    /// требование формата в текст промпта.
    /// </summary>
    public bool UseJsonSchemaResponseFormat { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Перекрытие не меньше размера фрагмента означало бы, что окно не сдвигается вперёд.
        if (ChunkOverlap >= ChunkSize)
        {
            yield return new ValidationResult(
                $"{nameof(ChunkOverlap)} ({ChunkOverlap}) должен быть меньше {nameof(ChunkSize)} ({ChunkSize}).",
                [nameof(ChunkOverlap)]);
        }
    }
}
