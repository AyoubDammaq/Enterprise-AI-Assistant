namespace EnterpriseAIAssistant.Application.DTOs;

/// <summary>A document chunk together with the embedding generated for its content.</summary>
public sealed record EmbeddedChunk(
    DocumentChunk Chunk,
    IReadOnlyList<float> Embedding);
