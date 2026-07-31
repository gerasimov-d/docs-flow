using System.Text;

namespace DocsFlow.Rag;

/// <summary>
/// Сборка промпта. Инструкции здесь — просьба, а не гарантия: то, что ответ опирается на реальные
/// фрагменты, проверяет <see cref="RagService"/> кодом.
/// </summary>
internal static class RagPrompt
{
    public const string System =
        """
        Ты помощник по личному архиву документов.

        Правила:
        1. Отвечай только на основе приведённых фрагментов. Ничего не додумывай и не дополняй
           общими знаниями.
        2. Если во фрагментах нет ответа, прямо скажи об этом и оставь список citations пустым.
        3. В citations перечисли номера фрагментов, на которых основан ответ.
        4. Отвечай на языке вопроса, кратко и по существу.
        5. Не пересказывай фрагменты, не относящиеся к вопросу.
        """;

    public static string BuildUserMessage(string question, IReadOnlyList<ChunkMatch> matches)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Фрагменты:");
        builder.AppendLine();

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];

            // Номер фрагмента — это и есть то, чем модель ссылается на первоисточник:
            // по нему ответ сопоставляется с source_key и порядковым номером в документе.
            builder.AppendLine($"[{i + 1}] источник: {match.SourceKey}, фрагмент {match.Ordinal}");
            builder.AppendLine(match.Content);
            builder.AppendLine();
        }

        builder.AppendLine($"Вопрос: {question}");

        return builder.ToString();
    }
}
