using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Infrastructure.Documents;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Documents;

public sealed class ChunkingStatisticsCalculatorTests
{
    [Fact]
    public void Calculate_MultipleChunks_ReturnsExpectedStatistics()
    {
        var chunks = new[]
        {
            CreateChunk(0, "abc"),
            CreateChunk(1, "abcde"),
            CreateChunk(2, "abcdefg")
        };

        var result = ChunkingStatisticsCalculator.Calculate(chunks);

        Assert.Equal(3, result.ChunkCount);
        Assert.Equal(3, result.MinLength);
        Assert.Equal(5.0, result.AverageLength);
        Assert.Equal(7, result.MaxLength);
    }

    [Fact]
    public void Calculate_EmptyCollection_ReturnsZeroStatistics()
    {
        var result = ChunkingStatisticsCalculator.Calculate(
            Array.Empty<DocumentChunk>());

        Assert.Equal(0, result.ChunkCount);
        Assert.Equal(0, result.MinLength);
        Assert.Equal(0, result.AverageLength);
        Assert.Equal(0, result.MaxLength);
    }

    [Fact]
    public void Calculate_ChunksWithSameLength_ReturnsSameMinAndMax()
    {
        var chunks = new[]
        {
            CreateChunk(0, "abcde"),
            CreateChunk(1, "fghij")
        };

        var result = ChunkingStatisticsCalculator.Calculate(chunks);

        Assert.Equal(2, result.ChunkCount);
        Assert.Equal(5, result.MinLength);
        Assert.Equal(5.0, result.AverageLength);
        Assert.Equal(5, result.MaxLength);
    }

    [Fact]
    public void Calculate_NullCollection_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => ChunkingStatisticsCalculator.Calculate(null!));
    }

    private static DocumentChunk CreateChunk(
        int index,
        string content)
    {
        return new DocumentChunk(
            "doc-1",
            "sample.md",
            index,
            content);
    }
}