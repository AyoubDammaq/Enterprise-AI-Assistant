namespace EnterpriseAIAssistant.Application.DTOs;

/// <summary>
/// Represents a text fragment extracted from a source document.
/// </summary>
public sealed record DocumentChunk(
    string DocumentId,
    string Source,
    int ChunkIndex,
    string Content);
