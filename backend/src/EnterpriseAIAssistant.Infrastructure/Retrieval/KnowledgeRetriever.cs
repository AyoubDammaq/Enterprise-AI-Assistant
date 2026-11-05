using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Retrieval;

public sealed class KnowledgeRetriever(
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    IOptions<RetrievalOptions> retrievalOptions) : IKnowledgeRetriever
{
    private readonly IEmbeddingService _embeddingService = embeddingService;
    private readonly IVectorStore _vectorStore = vectorStore;
    private readonly RetrievalOptions _options = retrievalOptions.Value;

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string query,
        int? topK = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var effectiveTopK = topK ?? _options.TopK;

        if (effectiveTopK < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topK),
                "topK must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(
            query, cancellationToken);

        return await _vectorStore.SearchAsync(
            queryEmbedding,
            effectiveTopK,
            cancellationToken);
    }
}