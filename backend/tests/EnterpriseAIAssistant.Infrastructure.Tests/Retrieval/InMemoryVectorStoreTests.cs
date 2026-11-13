using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Infrastructure.Configuration;
using EnterpriseAIAssistant.Infrastructure.Retrieval;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

public sealed class InMemoryVectorStoreTests
{
    private static InMemoryVectorStore CreateStore(double minimumSimilarityScore = 0.50) =>
        new(NullLogger<InMemoryVectorStore>.Instance, Options.Create(new RetrievalOptions { MinimumSimilarityScore = minimumSimilarityScore }));

    [Fact]
    public async Task SearchAsync_RanksByCosineSimilarity_AndReturnsAtMostTopK()
    {
        var store = CreateStore();
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
        var store = CreateStore();

        var results = await store.SearchAsync(new float[] { 1, 0 }, topK: 5);

        Assert.Empty(results);
    }

    [Fact]
    public async Task UpsertAsync_EmptyBatch_DoesNotFail()
    {
        var store = CreateStore();

        await store.UpsertAsync(Array.Empty<EmbeddedChunk>());

        Assert.Empty(await store.SearchAsync(new float[] { 1, 0 }, topK: 1));
    }

    [Fact]
    public async Task UpsertAsync_SameDocumentAndChunkIndex_ReplacesExistingChunk()
    {
        var store = CreateStore();  
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
        var store = CreateStore();
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
        var store = CreateStore();

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
        var store = CreateStore();
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
        var store = CreateStore();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => store.SearchAsync(new float[] { 1, 0 }, topK));
    }

    [Fact]
    public async Task SearchAsync_ZeroVector_Throws()
    {
        var store = CreateStore();

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.SearchAsync(new float[] { 0, 0 }, topK: 1));
    }

    [Fact]
    public async Task SearchAsync_CancelledToken_Throws()
    {
        var store = CreateStore();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.SearchAsync(new float[] { 1, 0 }, topK: 1, cancellation.Token));
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
        var vectorStore = CreateStore();

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
            0.50);
    }

    [Fact]
    public async Task SearchAsync_ReturnsChunksAboveMinimumSimilarityScore()
    {
        // Arrange
        var vectorStore = CreateStore();

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
                0.50));

        Assert.Equal(
            "high-relevance.md",
            results[0].Chunk.Source);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyList_WhenAllChunksAreBelowMinimumSimilarityScore()
    {
        // Arrange
        var vectorStore = CreateStore();

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


    [Fact]
    public async Task UpsertAsync_InvalidVector_Throws()
    {
        var store = CreateStore();

        var chunks = new[]
        {
        Embedded(
            "invalid.md",
            0,
            "Invalid embedding",
            new float[] { float.NaN, 1 })
    };

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.UpsertAsync(chunks));

        Assert.Empty(
            await store.SearchAsync(new float[] { 1, 0 }, topK: 5));
    }

    [Fact]
    public async Task UpsertAsync_ZeroVector_Throws()
    {
        var store = CreateStore();

        var chunks = new[]
        {
        Embedded(
            "zero.md",
            0,
            "Zero embedding",
            new float[] { 0, 0 })
    };

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.UpsertAsync(chunks));
    }

    [Fact]
    public async Task UpsertAsync_CancelledToken_Throws()
    {
        var store = CreateStore();

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.UpsertAsync(
                new[]
                {
                Embedded(
                    "document.md",
                    0,
                    "Content",
                    new float[] { 1, 0 })
                },
                cancellation.Token));
    }

    [Fact]
    public async Task UpsertAsync_FailedBatch_DoesNotPartiallyModifyStore()
    {
        var store = CreateStore();

        await store.UpsertAsync(new[]
        {
        Embedded("existing.md", 0, "Original content", new float[] { 1, 0 })
    });

        var invalidBatch = new[]
        {
        Embedded("existing.md", 0, "Modified content", new float[] { 0, 1 }),
        Embedded("new.md", 0, "New content", new float[] { 1, 0, 0 })
    };

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.UpsertAsync(invalidBatch));

        var results = await store.SearchAsync(
            new float[] { 1, 0 },
            topK: 5);

        var result = Assert.Single(results);

        Assert.Equal("Original content", result.Chunk.Content);
    }

    [Fact]
    public async Task SearchAsync_ExactMinimumSimilarityScore_IncludesChunk()
    {
        var store = CreateStore();

        await store.UpsertAsync(new[]
        {
        Embedded(
            "boundary.md",
            0,
            "Boundary content",
            new float[] { 0.5f, MathF.Sqrt(0.75f) })
    });

        var results = await store.SearchAsync(
            new float[] { 1, 0 },
            topK: 5);

        var result = Assert.Single(results);

        Assert.Equal(
            0.50,
            result.SimilarityScore,
            precision: 6);
    }

    [Fact]
    public async Task SearchAsync_UsesConfiguredMinimumSimilarityScore()
    {
        // Arrange
        var store = CreateStore(minimumSimilarityScore: 0.80);

        await store.UpsertAsync(
        [
            CreateEmbeddedChunk(
            "doc-1", "high-relevance.md", 0,
            [1f, 0f]),

        CreateEmbeddedChunk(
            "doc-2", "medium-relevance.md", 0,
            [0.8f, 0.6f])
        ]);

        // Act
        var results = await store.SearchAsync(
            new float[] { 1f, 0f },
            topK: 5);

        // Assert
        var result = Assert.Single(results);
        Assert.Equal("high-relevance.md", result.Chunk.Source);
    }


    [Fact]
    public async Task SearchAsync_PreservesChunkMetadata_AfterIndexingAndRetrieval()
    {
        // Arrange
        var store = CreateStore();

        var chunk = new DocumentChunk(
            DocumentId: "support-faq",
            Source: "support-faq.md",
            ChunkIndex: 2,
            Content: "How to reset a password?")
        {
            Section = "Account Management"
        };

        var embeddedChunk = new EmbeddedChunk(
            chunk,
            new float[] { 1f, 0f });

        await store.UpsertAsync(new[] { embeddedChunk });

        // Act
        var results = await store.SearchAsync(
            new float[] { 1f, 0f },
            topK: 5);

        // Assert
        var result = Assert.Single(results);
        var retrievedChunk = result.Chunk;

        Assert.Equal("support-faq", retrievedChunk.DocumentId);
        Assert.Equal("support-faq.md", retrievedChunk.Source);
        Assert.Equal(2, retrievedChunk.ChunkIndex);
        Assert.Equal("support-faq::chunk-0002", retrievedChunk.ChunkId);
        Assert.Equal("Account Management", retrievedChunk.Section);
        Assert.Equal("How to reset a password?", retrievedChunk.Content);
    }

    [Fact]
    public async Task SearchAsync_WithSourceFilter_ReturnsOnlyMatchingSource()
    {
        // Arrange
        var store = CreateStore();

        await store.UpsertAsync(
        [
            CreateEmbeddedChunk(
            "faq",
            "support-faq.md",
            0,
            [1f, 0f]),

        CreateEmbeddedChunk(
            "guide",
            "product-guide.md",
            0,
            [0.8f, 0.6f])
        ]);

        // Act
        var results = await store.SearchAsync(
            [1f, 0f],
            topK: 5,
            sourceFilter: "support-faq.md");

        // Assert
        var result = Assert.Single(results);

        Assert.Equal("support-faq.md", result.Chunk.Source);
    }

    [Fact]
    public async Task SearchAsync_WithUnknownSourceFilter_ReturnsEmptyResults()
    {
        // Arrange
        var store = CreateStore();

        await store.UpsertAsync(
        [
            CreateEmbeddedChunk(
            "faq",
            "support-faq.md",
            0,
            [1f, 0f]),

        CreateEmbeddedChunk(
            "guide",
            "product-guide.md",
            0,
            [0.8f, 0.6f])
        ]);

        // Act
        var results = await store.SearchAsync(
            [1f, 0f],
            topK: 5,
            sourceFilter: "unknown.md");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task UpsertAsync_ChunkWithoutSource_Throws()
    {
        // Arrange
        var store = CreateStore();

        var chunk = new DocumentChunk(
            "document-1",
            "",
            0,
            "Some document content");

        var embeddedChunk = new EmbeddedChunk(
            chunk,
            new float[] { 1f, 0f });

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => store.UpsertAsync([embeddedChunk]));
    }

    [Fact]
    public async Task SearchAsync_WithoutSourceFilter_ReturnsAllMatchingSources()
    {
        // Arrange
        var store = CreateStore();

        await store.UpsertAsync(
        [
            CreateEmbeddedChunk(
            "faq",
            "support-faq.md",
            0,
            [1f, 0f]),

        CreateEmbeddedChunk(
            "guide",
            "product-guide.md",
            0,
            [0.8f, 0.6f])
        ]);

        // Act
        var results = await store.SearchAsync(
            [1f, 0f],
            topK: 5);

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task SearchAsync_WithSourceFilter_IsCaseInsensitive()
    {
        // Arrange
        var store = CreateStore();

        await store.UpsertAsync(
        [
            CreateEmbeddedChunk(
            "faq",
            "support-faq.md",
            0,
            [1f, 0f])
        ]);

        // Act
        var results = await store.SearchAsync(
            [1f, 0f],
            topK: 5,
            sourceFilter: "SUPPORT-FAQ.MD");

        // Assert
        var result = Assert.Single(results);

        Assert.Equal("support-faq.md", result.Chunk.Source);
    }

    [Fact]
    public async Task SearchAsync_WithSourceFilter_AppliesFilterBeforeTopK()
    {
        // Arrange
        var store = CreateStore();

        await store.UpsertAsync(
        [
            CreateEmbeddedChunk(
            "other-1",
            "product-guide.md",
            0,
            [1f, 0f]),

        CreateEmbeddedChunk(
            "other-2",
            "troubleshooting.md",
            0,
            [0.99f, 0.1f]),

        CreateEmbeddedChunk(
            "faq",
            "support-faq.md",
            0,
            [0.8f, 0.6f])
        ]);

        // Act
        var results = await store.SearchAsync(
            [1f, 0f],
            topK: 1,
            sourceFilter: "support-faq.md");

        // Assert
        var result = Assert.Single(results);

        Assert.Equal("support-faq.md", result.Chunk.Source);
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
