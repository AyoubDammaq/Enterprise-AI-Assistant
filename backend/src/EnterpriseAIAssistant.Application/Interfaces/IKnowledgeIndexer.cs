using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.Application.Interfaces;

/// <summary>Creates embeddings for document chunks and writes them to the vector store.</summary>
public interface IKnowledgeIndexer
{
    Task<int> IndexAsync(
        IEnumerable<DocumentChunk> chunks,
        CancellationToken cancellationToken = default);
}
