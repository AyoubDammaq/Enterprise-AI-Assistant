using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.Application.Interfaces;

/// <summary>Retrieves the most similar document chunks for a natural-language query.</summary>
public interface IKnowledgeRetriever
{
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
