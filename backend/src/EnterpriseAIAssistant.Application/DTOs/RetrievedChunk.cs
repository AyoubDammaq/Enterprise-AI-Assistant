namespace EnterpriseAIAssistant.Application.DTOs;

/// <summary>A chunk returned by vector search, ranked by cosine similarity.</summary>
public sealed record RetrievedChunk(
    DocumentChunk Chunk,
    double SimilarityScore);
