using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;

namespace EnterpriseAIAssistant.Infrastructure.Retrieval;

public sealed class KnowledgeRetriever(IEmbeddingService embeddingService, IVectorStore vectorStore) : IKnowledgeRetriever
{
    private readonly IEmbeddingService _embeddingService = embeddingService;
    private readonly IVectorStore _vectorStore = vectorStore;

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        if (topK < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topK),
                "topK must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(
            query, cancellationToken);

        return await _vectorStore.SearchAsync(queryEmbedding, topK, cancellationToken);
    }
}
