using System.Globalization;

namespace EnterpriseAIAssistant.Application.DTOs;

/// <summary>
/// Represents a text fragment extracted from a source document.
/// </summary>
public sealed record DocumentChunk(
    string DocumentId,
    string Source,
    int ChunkIndex,
    string Content)
{
    /// <summary>
    /// Unique, deterministic identifier for this chunk.
    /// Derived from the document identifier and chunk index.
    /// </summary>
    public string ChunkId =>
        $"{DocumentId}::chunk-{ChunkIndex.ToString("D4", CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Optional section or heading associated with this chunk.
    /// Null when no section information is available.
    /// </summary>
    public string? Section { get; init; }
}
