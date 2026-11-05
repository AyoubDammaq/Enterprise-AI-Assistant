using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.Configuration;
using EnterpriseAIAssistant.Infrastructure.Retrieval;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

public sealed class KnowledgeRetrieverFailureTests
{
    private static InMemoryVectorStore CreateStore(double minimumSimilarityScore = 0.50) =>
        new(NullLogger<InMemoryVectorStore>.Instance, Options.Create(new RetrievalOptions { MinimumSimilarityScore = minimumSimilarityScore }));

    [Fact]
    public async Task SearchAsync_EmbeddingFailure_PropagatesException()
    {
        var embeddingService = new FailingEmbeddingService(
            new HttpRequestException("Embedding provider unavailable."));

        var retriever = new KnowledgeRetriever(
            embeddingService,
            CreateStore(),
            Options.Create(new RetrievalOptions { TopK = 5 }));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => retriever.SearchAsync("password reset"));
    }

    [Fact]
    public async Task SearchAsync_VectorStoreFailure_PropagatesException()
    {
        var embeddingService = new FixedEmbeddingService(
            new float[] { 1, 0 });

        var retriever = new KnowledgeRetriever(
            embeddingService,
            new FailingSearchVectorStore(),
            Options.Create(new RetrievalOptions { TopK = 5 }));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => retriever.SearchAsync("password reset"));
    }

    private sealed class FixedEmbeddingService(
        IReadOnlyList<float> vector) : IEmbeddingService
    {
        public Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(vector);
        }
    }

    private sealed class FailingEmbeddingService(Exception exception)
        : IEmbeddingService
    {
        public Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<IReadOnlyList<float>>(exception);
        }
    }

    private sealed class FailingSearchVectorStore : IVectorStore
    {
        public Task UpsertAsync(
            IReadOnlyCollection<EmbeddedChunk> chunks,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
            IReadOnlyList<float> queryEmbedding,
            int topK,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<IReadOnlyList<RetrievedChunk>>(
                new InvalidOperationException("Vector store unavailable."));
        }
    }
}