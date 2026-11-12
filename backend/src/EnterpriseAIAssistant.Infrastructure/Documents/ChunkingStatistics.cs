namespace EnterpriseAIAssistant.Infrastructure.Documents;

/// <summary>
/// Describes the size distribution of generated document chunks.
/// </summary>
public sealed record ChunkingStatistics(
    int ChunkCount,
    int MinLength,
    double AverageLength,
    int MaxLength);