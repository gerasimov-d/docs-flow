namespace DocsFlow.Rag;

/// <summary>
/// Найденный фрагмент. <paramref name="SourceKey"/> и <paramref name="Ordinal"/> вместе образуют
/// локатор первоисточника — то, без чего ответ показывать нельзя.
/// </summary>
/// <param name="Score">Косинусная близость к запросу, 1 — совпадение, 0 — ортогональные векторы.</param>
public sealed record ChunkMatch(string SourceKey, int Ordinal, string Content, double Score);
