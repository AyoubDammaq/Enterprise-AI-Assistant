using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.Infrastructure.Documents;

/// <summary>
/// Calculates statistics from generated document chunks.
/// </summary>
public static class ChunkingStatisticsCalculator
{
    public static ChunkingStatistics Calculate(
        IEnumerable<DocumentChunk> chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        var lengths = chunks
            .Select(chunk => chunk.Content.Length)
            .ToArray();

        if (lengths.Length == 0)
        {
            return new ChunkingStatistics(
                ChunkCount: 0,
                MinLength: 0,
                AverageLength: 0,
                MaxLength: 0);
        }

        return new ChunkingStatistics(
            ChunkCount: lengths.Length,
            MinLength: lengths.Min(),
            AverageLength: lengths.Average(),
            MaxLength: lengths.Max());
    }
}