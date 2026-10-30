using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.Retrieval;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

public sealed class KnowledgeServicesTests
{
    [Fact]
    public async Task Indexer_EmbedsChunksAndStoresThem()
    {
        var embeddings = new FakeEmbeddingService(text => text.Contains("password", StringComparison.OrdinalIgnoreCase)
            ? new float[] { 1, 0 }
            : new float[] { 0, 1 });
        var store = new InMemoryVectorStore();
        var indexer = new KnowledgeIndexer(embeddings, store);
        var chunks = new[]
        {
            new DocumentChunk("password", "password.md", 0, "Password reset instructions"),
            new DocumentChunk("colors", "colors.md", 0, "Product color guide")
        };

        var indexedCount = await indexer.IndexAsync(chunks);
        var results = await store.SearchAsync(new float[] { 1, 0 }, topK: 2);

        Assert.Equal(2, indexedCount);
        Assert.Equal(2, embeddings.Calls);
        Assert.Equal("password.md", results[0].Chunk.Source);
        Assert.Equal("colors.md", results[1].Chunk.Source);
    }

    [Fact]
    public async Task Indexer_EmptyInput_ReturnsZeroWithoutCallingEmbeddingService()
    {
        var embeddings = new FakeEmbeddingService(_ => new float[] { 1, 0 });
        var indexer = new KnowledgeIndexer(embeddings, new InMemoryVectorStore());

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
        var store = new InMemoryVectorStore();
        await store.UpsertAsync(new[]
        {
            Embedded("colors.md", "Product color guide", new float[] { 0, 1 }),
            Embedded("password.md", "Password reset guide", new float[] { 0.9f, 0.1f })
        });
        var retriever = new KnowledgeRetriever(embeddings, store);

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
        var retriever = new KnowledgeRetriever(
            new FakeEmbeddingService(_ => new float[] { 1, 0 }),
            new InMemoryVectorStore());

        await Assert.ThrowsAsync<ArgumentException>(() => retriever.SearchAsync(query));
    }

    [Fact]
    public async Task Retriever_InvalidTopK_ThrowsBeforeEmbeddingCall()
    {
        var embeddings = new FakeEmbeddingService(_ => new float[] { 1, 0 });
        var retriever = new KnowledgeRetriever(embeddings, new InMemoryVectorStore());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => retriever.SearchAsync("test", topK: 0));
        Assert.Equal(0, embeddings.Calls);
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
