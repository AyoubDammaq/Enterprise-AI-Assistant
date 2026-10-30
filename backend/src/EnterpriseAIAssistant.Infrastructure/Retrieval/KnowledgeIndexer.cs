using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;

namespace EnterpriseAIAssistant.Infrastructure.Retrieval;

public sealed class KnowledgeIndexer : IKnowledgeIndexer
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;

    public KnowledgeIndexer(IEmbeddingService embeddingService, IVectorStore vectorStore)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
    }

    public async Task<int> IndexAsync(
        IEnumerable<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        cancellationToken.ThrowIfCancellationRequested();

        var materialized = chunks.ToArray();
        if (materialized.Length == 0)
        {
            return 0;
        }

        var embeddedChunks = new List<EmbeddedChunk>(materialized.Length);
        foreach (var chunk in materialized)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(chunk);
            ArgumentException.ThrowIfNullOrWhiteSpace(chunk.Content);

            var embedding = await _embeddingService.GenerateEmbeddingAsync(
                chunk.Content, cancellationToken);

            embeddedChunks.Add(new EmbeddedChunk(chunk, embedding));
        }

        // One upsert after embeddings are prepared avoids partially writing a batch on an
        // embedding-provider failure. The store validates dimensions before mutating state.
        await _vectorStore.UpsertAsync(embeddedChunks, cancellationToken);
        return embeddedChunks.Count;
    }
}
