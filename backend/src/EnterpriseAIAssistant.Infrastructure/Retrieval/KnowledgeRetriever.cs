using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;

namespace EnterpriseAIAssistant.Infrastructure.Retrieval;

public sealed class KnowledgeRetriever : IKnowledgeRetriever
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;

    public KnowledgeRetriever(IEmbeddingService embeddingService, IVectorStore vectorStore)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
    }

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        if (topK is < 1 or > InMemoryVectorStore.MaximumTopK)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topK), $"topK must be between 1 and {InMemoryVectorStore.MaximumTopK}.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(
            query, cancellationToken);

        return await _vectorStore.SearchAsync(queryEmbedding, topK, cancellationToken);
    }
}
