using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Infrastructure.Retrieval;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

public sealed class InMemoryVectorStoreTests
{
    [Fact]
    public async Task SearchAsync_RanksByCosineSimilarity_AndReturnsAtMostTopK()
    {
        var store = new InMemoryVectorStore();
        await store.UpsertAsync(new[]
        {
            Embedded("password-reset.md", 0, "Reset a forgotten password.", new float[] { 1, 0 }),
            Embedded("email.md", 0, "Update your email address.", new float[] { 0.8f, 0.6f }),
            Embedded("colors.md", 0, "Change product colors.", new float[] { 0, 1 })
        });

        var results = await store.SearchAsync(new float[] { 1, 0 }, topK: 2);

        Assert.Equal(2, results.Count);
        Assert.Equal("password-reset.md", results[0].Chunk.Source);
        Assert.Equal(1d, results[0].SimilarityScore, precision: 6);
        Assert.Equal("email.md", results[1].Chunk.Source);
        Assert.Equal(0.8d, results[1].SimilarityScore, precision: 5);
    }

    [Fact]
    public async Task SearchAsync_EmptyCorpus_ReturnsEmptyResults()
    {
        var store = new InMemoryVectorStore();

        var results = await store.SearchAsync(new float[] { 1, 0 }, topK: 5);

        Assert.Empty(results);
    }

    [Fact]
    public async Task UpsertAsync_EmptyBatch_DoesNotFail()
    {
        var store = new InMemoryVectorStore();

        await store.UpsertAsync(Array.Empty<EmbeddedChunk>());

        Assert.Empty(await store.SearchAsync(new float[] { 1, 0 }, topK: 1));
    }

    [Fact]
    public async Task UpsertAsync_SameDocumentAndChunkIndex_ReplacesExistingChunk()
    {
        var store = new InMemoryVectorStore();
        await store.UpsertAsync(new[]
        {
            Embedded("guide.md", 0, "old content", new float[] { 1, 0 })
        });
        await store.UpsertAsync(new[]
        {
            Embedded("guide.md", 0, "updated content", new float[] { 0, 1 })
        });

        var results = await store.SearchAsync(new float[] { 0, 1 }, topK: 5);

        var result = Assert.Single(results);
        Assert.Equal("updated content", result.Chunk.Content);
        Assert.Equal(1d, result.SimilarityScore, precision: 6);
    }

    [Fact]
    public async Task UpsertAsync_DimensionMismatchWithExistingStore_Throws()
    {
        var store = new InMemoryVectorStore();
        await store.UpsertAsync(new[]
        {
            Embedded("first.md", 0, "first", new float[] { 1, 0 })
        });

        await Assert.ThrowsAsync<ArgumentException>(() => store.UpsertAsync(new[]
        {
            Embedded("second.md", 0, "second", new float[] { 1, 0, 0 })
        }));
    }

    [Fact]
    public async Task UpsertAsync_InconsistentDimensionsWithinBatch_ThrowsBeforeWriting()
    {
        var store = new InMemoryVectorStore();

        await Assert.ThrowsAsync<ArgumentException>(() => store.UpsertAsync(new[]
        {
            Embedded("first.md", 0, "first", new float[] { 1, 0 }),
            Embedded("second.md", 0, "second", new float[] { 1, 0, 0 })
        }));

        Assert.Empty(await store.SearchAsync(new float[] { 1, 0 }, topK: 5));
    }

    [Fact]
    public async Task SearchAsync_QueryDimensionMismatch_Throws()
    {
        var store = new InMemoryVectorStore();
        await store.UpsertAsync(new[]
        {
            Embedded("first.md", 0, "first", new float[] { 1, 0 })
        });

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.SearchAsync(new float[] { 1, 0, 0 }, topK: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public async Task SearchAsync_InvalidTopK_Throws(int topK)
    {
        var store = new InMemoryVectorStore();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => store.SearchAsync(new float[] { 1, 0 }, topK));
    }

    [Fact]
    public async Task SearchAsync_ZeroVector_Throws()
    {
        var store = new InMemoryVectorStore();

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.SearchAsync(new float[] { 0, 0 }, topK: 1));
    }

    [Fact]
    public async Task SearchAsync_CancelledToken_Throws()
    {
        var store = new InMemoryVectorStore();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.SearchAsync(new float[] { 1, 0 }, topK: 1, cancellation.Token));
    }

    [Fact]
    public void CosineSimilarity_ZeroVector_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => CosineSimilarity.Calculate(new float[] { 0, 0 }, new float[] { 1, 0 }));
    }

    private static EmbeddedChunk Embedded(
        string source,
        int index,
        string content,
        IReadOnlyList<float> embedding)
    {
        var chunk = new DocumentChunk(source, source, index, content);
        return new EmbeddedChunk(chunk, embedding);
    }

    [Fact]
    public async Task SearchAsync_ExcludesChunksBelowMinimumSimilarityScore()
    {
        // Arrange
        var vectorStore = new InMemoryVectorStore();

        var chunks = new[]
        {
        CreateEmbeddedChunk(
            "doc-1", "relevant.md", 0,
            [1f, 0f]),

        CreateEmbeddedChunk(
            "doc-2", "irrelevant.md", 0,
            [0f, 1f])
    };

        await vectorStore.UpsertAsync(chunks);

        var queryEmbedding = new float[] { 1f, 0f };

        // Act
        var results = await vectorStore.SearchAsync(
            queryEmbedding,
            topK: 5);

        // Assert
        Assert.Single(results);
        Assert.Equal("relevant.md", results[0].Chunk.Source);
        Assert.True(
            results[0].SimilarityScore >=
            InMemoryVectorStore.MinimumSimilarityScore);
    }

    [Fact]
    public async Task SearchAsync_ReturnsChunksAboveMinimumSimilarityScore()
    {
        // Arrange
        var vectorStore = new InMemoryVectorStore();

        var chunks = new[]
        {
        CreateEmbeddedChunk(
            "doc-1", "high-relevance.md", 0,
            [1f, 0f]),

        CreateEmbeddedChunk(
            "doc-2", "medium-relevance.md", 0,
            [0.8f, 0.6f])
    };

        await vectorStore.UpsertAsync(chunks);

        var queryEmbedding = new float[] { 1f, 0f };

        // Act
        var results = await vectorStore.SearchAsync(
            queryEmbedding,
            topK: 5);

        // Assert
        Assert.Equal(2, results.Count);

        Assert.All(results, result =>
            Assert.True(
                result.SimilarityScore >=
                InMemoryVectorStore.MinimumSimilarityScore));

        Assert.Equal(
            "high-relevance.md",
            results[0].Chunk.Source);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyList_WhenAllChunksAreBelowMinimumSimilarityScore()
    {
        // Arrange
        var vectorStore = new InMemoryVectorStore();

        var chunks = new[]
        {
        CreateEmbeddedChunk(
            "doc-1", "document-1.md", 0,
            [0f, 1f]),

        CreateEmbeddedChunk(
            "doc-2", "document-2.md", 0,
            [-1f, 0f])
    };

        await vectorStore.UpsertAsync(chunks);

        var queryEmbedding = new float[] { 1f, 0f };

        // Act
        var results = await vectorStore.SearchAsync(
            queryEmbedding,
            topK: 5);

        // Assert
        Assert.Empty(results);
    }

    private static EmbeddedChunk CreateEmbeddedChunk(
        string documentId,
        string source,
        int chunkIndex,
        float[] embedding)
    {
        var chunk = new DocumentChunk(
            documentId,
            source,
            chunkIndex,
            $"Content of {source}");

        return new EmbeddedChunk(chunk, embedding);
    }
}
