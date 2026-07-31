using Shouldly;
using Xunit;

namespace DocsFlow.Rag.Tests;

public sealed class TextChunkerTests
{
    [Fact]
    public void A_short_text_stays_one_chunk()
    {
        var chunks = TextChunker.Split("Паспорт выдан в 2019 году.", chunkSize: 1000, overlap: 150);

        chunks.Count.ShouldBe(1);
        chunks[0].ShouldBe("Паспорт выдан в 2019 году.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n\n  ")]
    public void An_empty_text_gives_no_chunks(string text)
        => TextChunker.Split(text, chunkSize: 1000, overlap: 150).ShouldBeEmpty();

    [Fact]
    public void A_long_text_is_split_and_covered_completely()
    {
        var text = string.Join(" ", Enumerable.Range(0, 400).Select(i => $"слово{i}"));

        var chunks = TextChunker.Split(text, chunkSize: 500, overlap: 100);

        chunks.Count.ShouldBeGreaterThan(1);
        chunks.ShouldAllBe(chunk => chunk.Length <= 500);

        // Ни одно слово не потеряно на границах — иначе поиск не найдёт то, что есть в документе.
        foreach (var word in new[] { "слово0", "слово199", "слово399" })
        {
            chunks.ShouldContain(chunk => chunk.Contains(word));
        }
    }

    [Fact]
    public void Neighbouring_chunks_overlap()
    {
        var text = string.Join(" ", Enumerable.Range(0, 200).Select(i => $"слово{i}"));

        var chunks = TextChunker.Split(text, chunkSize: 400, overlap: 120);

        // Хвост первого фрагмента обязан встретиться в начале второго: ради этого перекрытие
        // и нужно — предложение, разрезанное границей, остаётся целым хотя бы в одном фрагменте.
        var tail = chunks[0][^60..];

        chunks[1].ShouldContain(tail[(tail.IndexOf(' ') + 1)..]);
    }

    [Fact]
    public void A_paragraph_boundary_is_preferred_over_a_mid_sentence_cut()
    {
        var first = new string('а', 300);
        var second = new string('б', 300);

        var chunks = TextChunker.Split($"{first}\n\n{second}", chunkSize: 400, overlap: 50);

        // Разрез приходится на границу абзацев, а не на 400-й символ.
        chunks[0].ShouldBe(first);
    }

    [Fact]
    public void A_sentence_boundary_is_preferred_over_a_mid_word_cut()
    {
        var text = $"{new string('а', 250)}. {new string('б', 250)}";

        var chunks = TextChunker.Split(text, chunkSize: 400, overlap: 50);

        chunks[0].ShouldEndWith(".");
    }
}
