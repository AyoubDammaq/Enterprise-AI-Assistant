using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.Configuration;
using EnterpriseAIAssistant.Infrastructure.Retrieval;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

public sealed class KnowledgeServicesTests
{
    private static InMemoryVectorStore CreateStore(double minimumSimilarityScore = 0.50) =>
        new(NullLogger<InMemoryVectorStore>.Instance, Options.Create(new RetrievalOptions { MinimumSimilarityScore = minimumSimilarityScore }));

    [Fact]
    public async Task Indexer_EmbedsChunksAndStoresThem()
    {
        // Arrange
        var embeddings = new FakeEmbeddingService(
            text => text.Contains("password", StringComparison.OrdinalIgnoreCase)
                ? new float[] { 1, 0 }
                : new float[] { 0, 1 });

        var store = CreateStore();
        var indexer = new KnowledgeIndexer(embeddings, store);

        var chunks = new[]
        {
        new DocumentChunk(
            "password", "password.md", 0,
            "Password reset instructions"),

        new DocumentChunk(
            "colors", "colors.md", 0,
            "Product color guide")
    };

        // Act
        var indexedCount = await indexer.IndexAsync(chunks);

        // Assert: indexing behavior
        Assert.Equal(2, indexedCount);
        Assert.Equal(2, embeddings.Calls);

        // Assert: both documents were stored.
        // Search independently using each document's embedding.
        var passwordResults = await store.SearchAsync(
            new float[] { 1, 0 }, topK: 2);

        var colorsResults = await store.SearchAsync(
            new float[] { 0, 1 }, topK: 2);

        Assert.Contains(
            passwordResults,
            result => result.Chunk.Source == "password.md");

        Assert.Contains(
            colorsResults,
            result => result.Chunk.Source == "colors.md");
    }

    [Fact]
    public async Task Indexer_EmptyInput_ReturnsZeroWithoutCallingEmbeddingService()
    {
        var embeddings = new FakeEmbeddingService(_ => new float[] { 1, 0 });
        var indexer = new KnowledgeIndexer(embeddings, CreateStore());

        var result = await indexer.IndexAsync(Array.Empty<DocumentChunk>());

        Assert.Equal(0, result);
        Assert.Equal(0, embeddings.Calls);
    }

    [Fact]
    public async Task Retriever_EmbedsQueryAndReturnsRankedChunks()
    {
        var embeddings = new FakeEmbeddingService(text => text == "password reset"
            ? new float[] { 1, 0 }
            : text.Contains("password", StringComparison.OrdinalIgnoreCase)
                ? new float[] { 0.9f, 0.1f }
                : new float[] { 0, 1 });
        var store = CreateStore();
        await store.UpsertAsync(new[]
        {
            Embedded("colors.md", "Product color guide", new float[] { 0, 1 }),
            Embedded("password.md", "Password reset guide", new float[] { 0.9f, 0.1f })
        });
        var options = Options.Create(new RetrievalOptions { TopK = 5 });
        var retriever = new KnowledgeRetriever(embeddings, store, options);

        var results = await retriever.SearchAsync("password reset", topK: 1);

        var result = Assert.Single(results);
        Assert.Equal("password.md", result.Chunk.Source);
        Assert.Equal(1, embeddings.Calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Retriever_EmptyQuery_Throws(string query)
    {
        var options = Options.Create(new RetrievalOptions { TopK = 5 });
        var retriever = new KnowledgeRetriever(
            new FakeEmbeddingService(_ => new float[] { 1, 0 }),
            CreateStore(),
            options);

        await Assert.ThrowsAsync<ArgumentException>(() => retriever.SearchAsync(query));
    }

    [Fact]
    public async Task Retriever_InvalidTopK_ThrowsBeforeEmbeddingCall()
    {
        var embeddings = new FakeEmbeddingService(_ => new float[] { 1, 0 });
        var options = Options.Create(new RetrievalOptions { TopK = 5 });
        var retriever = new KnowledgeRetriever(embeddings, CreateStore(), options);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => retriever.SearchAsync("test", topK: 0));
        Assert.Equal(0, embeddings.Calls);
    }


    [Fact]
    public async Task Indexer_PreservesChunkMetadata_WhenStoringChunks()
    {
        // Arrange
        var embeddings = new FakeEmbeddingService(
            _ => new float[] { 1f, 0f });

        var store = CreateStore();
        var indexer = new KnowledgeIndexer(embeddings, store);

        var chunk = new DocumentChunk(
            DocumentId: "support-faq",
            Source: "support-faq.md",
            ChunkIndex: 3,
            Content: "How to reset a password?")
        {
            Section = "Account Management"
        };

        // Act
        var indexedCount = await indexer.IndexAsync(new[] { chunk });

        var results = await store.SearchAsync(
            new float[] { 1f, 0f },
            topK: 5);

        // Assert
        Assert.Equal(1, indexedCount);

        var result = Assert.Single(results);
        var indexedChunk = result.Chunk;

        Assert.Equal("support-faq", indexedChunk.DocumentId);
        Assert.Equal("support-faq.md", indexedChunk.Source);
        Assert.Equal(3, indexedChunk.ChunkIndex);
        Assert.Equal("support-faq::chunk-0003", indexedChunk.ChunkId);
        Assert.Equal("Account Management", indexedChunk.Section);
        Assert.Equal("How to reset a password?", indexedChunk.Content);
    }

    [Fact]
    public async Task Retriever_PreservesChunkMetadata_WhenReturningResults()
    {
        // Arrange
        var embeddings = new FakeEmbeddingService(
            _ => new float[] { 1f, 0f });

        var store = CreateStore();

        var chunk = new DocumentChunk(
            DocumentId: "support-faq",
            Source: "support-faq.md",
            ChunkIndex: 4,
            Content: "How to reset a password?")
        {
            Section = "Account Management"
        };

        await store.UpsertAsync(new[]
        {
        new EmbeddedChunk(chunk, new float[] { 1f, 0f })
    });

        var options = Options.Create(
            new RetrievalOptions { TopK = 5 });

        var retriever = new KnowledgeRetriever(
            embeddings,
            store,
            options);

        // Act
        var results = await retriever.SearchAsync("password reset");

        // Assert
        var result = Assert.Single(results);
        var retrievedChunk = result.Chunk;

        Assert.Equal("support-faq", retrievedChunk.DocumentId);
        Assert.Equal("support-faq.md", retrievedChunk.Source);
        Assert.Equal(4, retrievedChunk.ChunkIndex);
        Assert.Equal("support-faq::chunk-0004", retrievedChunk.ChunkId);
        Assert.Equal("Account Management", retrievedChunk.Section);
        Assert.Equal("How to reset a password?", retrievedChunk.Content);

        Assert.Equal(1, embeddings.Calls);
    }

    private static EmbeddedChunk Embedded(string source, string content, IReadOnlyList<float> vector)
    {
        var chunk = new DocumentChunk(source, source, 0, content);
        return new EmbeddedChunk(chunk, vector);
    }

    private sealed class FakeEmbeddingService(Func<string, IReadOnlyList<float>> createEmbedding)
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
