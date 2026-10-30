using EnterpriseAIAssistant.Infrastructure.Documents;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Documents;

public sealed class TextChunkerTests
{
    [Fact]
    public void Chunk_EmptyContent_ReturnsNoChunks()
    {
        var chunker = new TextChunker();

        var result = chunker.Chunk("doc-1", "sample.txt", "");

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\n\n")]
    [InlineData("\r\n\r\n")]
    public void Chunk_WhitespaceContent_ReturnsNoChunks(string content)
    {
        var chunker = new TextChunker();

        var result = chunker.Chunk("doc-1", "sample.txt", content);

        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_ShortContent_ReturnsOneChunk()
    {
        var chunker = new TextChunker(chunkSize: 20, chunkOverlap: 5);

        var result = chunker.Chunk("doc-1", "sample.txt", "Hello world");

        var chunk = Assert.Single(result);
        Assert.Equal("Hello world", chunk.Content);
        Assert.Equal("doc-1", chunk.DocumentId);
        Assert.Equal("sample.txt", chunk.Source);
        Assert.Equal(0, chunk.ChunkIndex);
    }

    [Fact]
    public void Chunk_LongParagraph_RespectsMaximumSizeAndOverlap()
    {
        const int chunkSize = 20;
        const int overlap = 5;
        var content = new string('a', 45);
        var chunker = new TextChunker(chunkSize, overlap);

        var result = chunker.Chunk("doc-1", "long.txt", content);

        Assert.Equal(3, result.Count);
        Assert.All(result, chunk =>
        {
            Assert.NotEmpty(chunk.Content);
            Assert.True(chunk.Content.Length <= chunkSize);
        });

        Assert.Equal(new string('a', 20), result[0].Content);
        Assert.Equal(new string('a', 5) + new string('a', 15), result[1].Content);
        Assert.Equal(new string('a', 15), result[2].Content);
    }

    [Fact]
    public void Chunk_MultipleParagraphs_CombinesThemWhenTheyFit()
    {
        var chunker = new TextChunker(chunkSize: 40, chunkOverlap: 5);

        var result = chunker.Chunk(
            "doc-1",
            "paragraphs.md",
            "First paragraph.\n\n\n\nSecond paragraph.");

        var chunk = Assert.Single(result);
        Assert.Equal("First paragraph.\n\nSecond paragraph.", chunk.Content);
    }

    [Fact]
    public void Chunk_MultipleChunks_UsesSequentialIndexes()
    {
        var chunker = new TextChunker(chunkSize: 10, chunkOverlap: 2);

        var result = chunker.Chunk(
            "doc-1",
            "sample.txt",
            new string('x', 25));

        Assert.Equal(new[] { 0, 1, 2 }, result.Select(chunk => chunk.ChunkIndex));
    }

    [Fact]
    public void Chunk_ParagraphsWithOverlap_NeverExceedsMaximumSize()
    {
        const int chunkSize = 20;
        const int overlap = 5;

        var chunker = new TextChunker(chunkSize, overlap);

        var content =
            "First paragraph\n\nSecond paragraph\n\nThird paragraph";

        var result = chunker.Chunk("doc-1", "paragraphs.md", content);

        Assert.NotEmpty(result);

        Assert.All(result, chunk =>
        {
            Assert.True(
                chunk.Content.Length <= chunkSize,
                $"Chunk {chunk.ChunkIndex} exceeds the maximum size: " +
                $"{chunk.Content.Length} > {chunkSize}");
        });

        Assert.Equal(
            Enumerable.Range(0, result.Count),
            result.Select(chunk => chunk.ChunkIndex));
    }

    [Fact]
    public void Chunk_LongParagraph_PreservesConfiguredOverlap()
    {
        const int chunkSize = 20;
        const int overlap = 5;

        var content = new string('a', 30);
        var chunker = new TextChunker(chunkSize, overlap);

        var result = chunker.Chunk("doc-1", "long.txt", content);

        Assert.Equal(2, result.Count);

        Assert.Equal(new string('a', 20), result[0].Content);
        Assert.Equal(new string('a', 15), result[1].Content);

        Assert.True(result.All(chunk => chunk.Content.Length <= chunkSize));

        Assert.Equal(
            result[0].Content[^overlap..],
            result[1].Content[..overlap]);
    }

    [Fact]
    public void Chunk_ContentExactlyAtMaximumSize_ReturnsOneChunk()
    {
        const int chunkSize = 20;
        var content = new string('a', chunkSize);
        var chunker = new TextChunker(chunkSize, chunkOverlap: 5);

        var result = chunker.Chunk("doc-1", "exact.txt", content);

        var chunk = Assert.Single(result);
        Assert.Equal(content, chunk.Content);
        Assert.Equal(chunkSize, chunk.Content.Length);
    }

    [Fact]
    public void Chunk_ParagraphsNearMaximumSize_RespectsMaximumSize()
    {
        const int chunkSize = 20;
        var chunker = new TextChunker(chunkSize, chunkOverlap: 5);

        var content = "1234567890\n\nabcdefghij";

        var result = chunker.Chunk("doc-1", "paragraphs.txt", content);

        Assert.Equal(2, result.Count);

        Assert.All(result, chunk =>
            Assert.True(chunk.Content.Length <= chunkSize));

        Assert.Equal("1234567890", result[0].Content);
        Assert.Equal("abcdefghij", result[1].Content);
    }

    [Fact]
    public void Chunk_NullDocumentId_Throws()
    {
        var chunker = new TextChunker();

        Assert.Throws<ArgumentNullException>(
            () => chunker.Chunk(null!, "sample.txt", "content"));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    [InlineData(10, -1)]
    [InlineData(10, 10)]
    [InlineData(10, 11)]
    public void Constructor_InvalidConfiguration_Throws(
        int chunkSize,
        int chunkOverlap)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TextChunker(chunkSize, chunkOverlap));
    }

    [Fact]
    public void Chunk_WhitespaceDocumentId_Throws()
    {
        var chunker = new TextChunker();

        Assert.Throws<ArgumentException>(
            () => chunker.Chunk(" ", "sample.txt", "content"));
    }

    [Fact]
    public void Chunk_NullContent_Throws()
    {
        var chunker = new TextChunker();

        Assert.Throws<ArgumentNullException>(
            () => chunker.Chunk("doc-1", "sample.txt", null!));
    }
}
