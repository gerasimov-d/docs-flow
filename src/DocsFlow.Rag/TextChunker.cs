namespace DocsFlow.Rag;

/// <summary>
/// Режет текст на перекрывающиеся фрагменты. Размер считается в символах, а не в токенах:
/// точный подсчёт токенов потребовал бы токенайзера под каждую модель, а нужен он для биллинга
/// и лимитов контекста — здесь же важно лишь получать фрагменты сопоставимого размера.
/// </summary>
internal static class TextChunker
{
    public static IReadOnlyList<string> Split(string text, int chunkSize, int overlap)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        // Переводы строк приводим к одному виду: границы абзацев ищутся по "\n\n",
        // и текст из Windows иначе выглядел бы сплошным.
        var normalized = text.Replace("\r\n", "\n").Trim();

        if (normalized.Length <= chunkSize)
        {
            return [normalized];
        }

        var chunks = new List<string>();
        var start = 0;

        while (start < normalized.Length)
        {
            var end = Math.Min(start + chunkSize, normalized.Length);

            if (end < normalized.Length)
            {
                end = FindBreak(normalized, start, end);
            }

            var chunk = normalized[start..end].Trim();

            if (chunk.Length > 0)
            {
                chunks.Add(chunk);
            }

            if (end >= normalized.Length)
            {
                break;
            }

            // Сдвиг минимум на символ: без этого фрагмент короче перекрытия зациклил бы обход.
            start = Math.Max(end - overlap, start + 1);
        }

        return chunks;
    }

    /// <summary>
    /// Ищет естественную границу разреза в дальней половине фрагмента: абзац, конец предложения,
    /// перевод строки, пробел. Ближнюю половину не трогаем — иначе фрагменты выйдут вдвое мельче
    /// заказанного там, где текст плотный.
    /// </summary>
    private static int FindBreak(string text, int start, int end)
    {
        var minBreak = start + (end - start) / 2;
        var window = text.AsSpan(minBreak, end - minBreak);

        var paragraph = window.LastIndexOf("\n\n");

        if (paragraph >= 0)
        {
            return minBreak + paragraph + 2;
        }

        for (var i = window.Length - 1; i >= 0; i--)
        {
            var isSentenceEnd = window[i] is '.' or '!' or '?' or '…';
            var followedBySpace = i + 1 >= window.Length || char.IsWhiteSpace(window[i + 1]);

            if (isSentenceEnd && followedBySpace)
            {
                return minBreak + i + 1;
            }
        }

        var newLine = window.LastIndexOf('\n');

        if (newLine >= 0)
        {
            return minBreak + newLine + 1;
        }

        var space = window.LastIndexOf(' ');

        return space >= 0 ? minBreak + space + 1 : end;
    }
}
