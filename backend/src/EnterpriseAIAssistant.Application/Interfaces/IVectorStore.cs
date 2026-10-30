using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.Application.Interfaces;

/// <summary>Stores embedded chunks and searches them by vector similarity.</summary>
public interface IVectorStore
{
    Task UpsertAsync(
        IReadOnlyCollection<EmbeddedChunk> chunks,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        IReadOnlyList<float> queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default);
}
