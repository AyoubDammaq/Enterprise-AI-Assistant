
using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.Retrieval;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

public sealed class RetrievalEvaluationTests
{
    [Fact]
    public async Task SearchAsync_RanksRelevantDocumentFirst()
    {
        // Arrange
        var embeddings = new FakeEmbeddingService(_ => [1f, 0f]);

        var store = new InMemoryVectorStore();

        await store.UpsertAsync(
        [
            Embedded("products.txt", "Product information", [0.8f, 0.6f]),
            Embedded("company-info.md", "Company architecture", [1f, 0f])
        ]);

        var retriever = new KnowledgeRetriever(embeddings, store);

        // Act
        var results = await retriever.SearchAsync(
            "Which architecture does the company use?",
            topK: 2);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("company-info.md", results[0].Chunk.Source);
        Assert.True(
            results[0].SimilarityScore > results[1].SimilarityScore);
        Assert.Equal(1, embeddings.Calls);
    }

    [Fact]
    public async Task SearchAsync_RelevantDocumentInSecondPosition_HitRateAtTwoIsOne()
    {
        // Arrange
        var embeddings = new FakeEmbeddingService(_ => [1f, 0f]);

        var store = new InMemoryVectorStore();

        await store.UpsertAsync(
        [
            Embedded("company-info.md", "Company information", [1f, 0f]),
            Embedded("products.txt", "Product information", [0.8f, 0.6f])
        ]);

        var retriever = new KnowledgeRetriever(embeddings, store);

        // Act
        var results = await retriever.SearchAsync("Product information", topK: 2);

        var sample = new RetrievalEvaluationSample(
            "products.txt",
            results.Select(result => result.Chunk.Source).ToList());

        var hitRateAtOne = RetrievalMetrics.CalculateHitRateAtK(
            [sample], k: 1);

        var hitRateAtTwo = RetrievalMetrics.CalculateHitRateAtK(
            [sample], k: 2);

        // Assert
        Assert.Equal(0d, hitRateAtOne);
        Assert.Equal(1d, hitRateAtTwo);
        Assert.Equal("products.txt", results[1].Chunk.Source);
    }

    [Fact]
    public async Task SearchAsync_ExcludesDocumentBelowSimilarityThreshold()
    {
        // Arrange
        var embeddings = new FakeEmbeddingService(_ => [1f, 0f]);

        var store = new InMemoryVectorStore();

        await store.UpsertAsync(
        [
            Embedded("relevant.md", "Relevant document", [1f, 0f]),
            Embedded("irrelevant.md", "Irrelevant document", [0f, 1f])
        ]);

        var retriever = new KnowledgeRetriever(embeddings, store);

        // Act
        var results = await retriever.SearchAsync("Relevant document");

        // Assert
        var result = Assert.Single(results);

        Assert.Equal("relevant.md", result.Chunk.Source);
        Assert.True(
            result.SimilarityScore >=
            InMemoryVectorStore.MinimumSimilarityScore);
    }

    [Fact]
    public async Task SearchAsync_QueryWithoutRelevantDocument_ReturnsEmptyResults()
    {
        // Arrange
        var embeddings = new FakeEmbeddingService(_ => [1f, 0f]);

        var store = new InMemoryVectorStore();

        await store.UpsertAsync(
        [
            Embedded("company-info.md", "Company information", [0f, 1f]),
            Embedded("products.txt", "Product information", [-1f, 0f])
        ]);

        var retriever = new KnowledgeRetriever(embeddings, store);

        // Act
        var results = await retriever.SearchAsync(
            "Information about an unrelated subject");

        // Assert
        Assert.Empty(results);
        Assert.Equal(1, embeddings.Calls);
    }

    private static EmbeddedChunk Embedded(
        string source,
        string content,
        IReadOnlyList<float> embedding)
    {
        var chunk = new DocumentChunk(
            source,
            source,
            0,
            content);

        return new EmbeddedChunk(chunk, embedding);
    }

    private sealed class FakeEmbeddingService(
        Func<string, IReadOnlyList<float>> createEmbedding)
        : IEmbeddingService
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Calls++;

            return Task.FromResult(createEmbedding(text));
        }
    }
}
