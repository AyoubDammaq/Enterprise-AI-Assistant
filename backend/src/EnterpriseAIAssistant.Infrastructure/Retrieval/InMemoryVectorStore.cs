using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace EnterpriseAIAssistant.Infrastructure.Retrieval;

/// <summary>
/// Process-local vector store. Data is lost when the application stops.
/// The lock protects mutations and snapshots because the service is registered as a singleton.
/// </summary>
public sealed class InMemoryVectorStore(ILogger<InMemoryVectorStore> logger, IOptions<RetrievalOptions> retrievalOptions) : IVectorStore
{
    public const int MaximumTopK = 20;
    private readonly object _gate = new();
    private readonly Dictionary<(string DocumentId, int ChunkIndex), EmbeddedChunk> _chunks = new();
    private int? _vectorDimensions;
    private readonly ILogger<InMemoryVectorStore> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly RetrievalOptions _retrievalOptions = retrievalOptions?.Value ?? throw new ArgumentNullException(nameof(retrievalOptions));

    public Task UpsertAsync(
        IReadOnlyCollection<EmbeddedChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        cancellationToken.ThrowIfCancellationRequested();

        if (chunks.Count == 0)
        {
            return Task.CompletedTask;
        }

        var prepared = new List<EmbeddedChunk>(chunks.Count);
        int? batchDimensions = null;

        foreach (var item in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(item.Chunk);
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Chunk.DocumentId);
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Chunk.Source);
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Chunk.Content);

            ValidateVector(item.Embedding, nameof(chunks));

            batchDimensions ??= item.Embedding.Count;

            if (item.Embedding.Count != batchDimensions.Value)
            {
                _logger.LogWarning(
                    "[RAG] Invalid embedding dimensions in batch. Expected={Expected}, Received={Received}",
                    batchDimensions.Value,
                    item.Embedding.Count);

                throw new ArgumentException(
                    "All vectors in a batch must have the same dimensions.", nameof(chunks));
            }

            // Copy vectors so callers cannot mutate the store after insertion.
            prepared.Add(new EmbeddedChunk(item.Chunk, item.Embedding.ToArray()));
        }

        lock (_gate)
        {
            if (_vectorDimensions.HasValue && _vectorDimensions.Value != batchDimensions!.Value)
            {
                _logger.LogWarning(
                    "[RAG] Vector dimension mismatch during upsert. Expected={Expected}, Received={Received}",
                    _vectorDimensions.Value,
                    batchDimensions.Value);

                throw new ArgumentException(
                    $"Vector dimension mismatch. Store expects {_vectorDimensions.Value}, " +
                    $"received {batchDimensions.Value}.", nameof(chunks));
            }

            // Validate the complete batch before modifying the dictionary.
            foreach (var item in prepared)
            {
                var key = (item.Chunk.DocumentId, item.Chunk.ChunkIndex);
                _chunks[key] = item;
            }

            _vectorDimensions ??= batchDimensions;

            _logger.LogInformation(
                "[RAG] Indexed {ChunkCount} chunks. VectorDimensions={Dimensions}",
                prepared.Count,
                _vectorDimensions);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        IReadOnlyList<float> queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);
        ValidateTopK(topK);
        ValidateVector(queryEmbedding, nameof(queryEmbedding));
        cancellationToken.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();

        List<EmbeddedChunk> snapshot;
        lock (_gate)
        {
            if (_chunks.Count == 0)
            {
                _logger.LogInformation(
                    "[RAG] Search completed. No chunks indexed. TopK={TopK}",
                    topK);

                return Task.FromResult<IReadOnlyList<RetrievedChunk>>(
                    Array.Empty<RetrievedChunk>());
            }

            if (_vectorDimensions != queryEmbedding.Count)
            {
                _logger.LogWarning(
                    "[RAG] Query vector dimension mismatch. Expected={Expected}, Received={Received}",
                    _vectorDimensions,
                    queryEmbedding.Count);

                throw new ArgumentException(
                    $"Query vector dimension mismatch. Store expects {_vectorDimensions}, " +
                    $"received {queryEmbedding.Count}.", nameof(queryEmbedding));
            }

            snapshot = _chunks.Values.ToList();
        }

        var ranked = new List<RetrievedChunk>(snapshot.Count);
        foreach (var item in snapshot)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var score = CosineSimilarity.Calculate(queryEmbedding, item.Embedding);
            _logger.LogDebug(
                "[RAG] Source={Source}, Chunk={Chunk}, Similarity={Similarity:F4}",
                item.Chunk.Source,
                item.Chunk.ChunkIndex,
                score);
            ranked.Add(new RetrievedChunk(item.Chunk, score));
        }

        IReadOnlyList<RetrievedChunk> result = ranked
            .Where(item => item.SimilarityScore >= _retrievalOptions.MinimumSimilarityScore)
            .OrderByDescending(item => item.SimilarityScore)
            .ThenBy(item => item.Chunk.Source, StringComparer.Ordinal)
            .ThenBy(item => item.Chunk.ChunkIndex)
            .Take(topK)
            .ToArray();

        stopwatch.Stop();

        _logger.LogInformation(
            "[RAG] Search completed. IndexedChunks={IndexedChunks}, " +
            "ReturnedResults={ReturnedResults}, TopK={TopK}, " +
            "MinimumSimilarityScore={MinimumSimilarityScore}, DurationMs={DurationMs}",
            snapshot.Count,
            result.Count,
            topK,
            _retrievalOptions.MinimumSimilarityScore,
            stopwatch.ElapsedMilliseconds);

        return Task.FromResult(result);
    }

    private static void ValidateTopK(int topK)
    {
        if (topK is < 1 or > MaximumTopK)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topK), $"topK must be between 1 and {MaximumTopK}.");
        }
    }

    private static void ValidateVector(IReadOnlyList<float> vector, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(vector, parameterName);

        if (vector.Count == 0)
        {
            throw new ArgumentException("Vector must not be empty.", parameterName);
        }

        var normSquared = 0d;
        foreach (var value in vector)
        {
            if (!float.IsFinite(value))
            {
                throw new ArgumentException("Vector values must be finite.", parameterName);
            }

            normSquared += (double)value * value;
        }

        if (normSquared == 0)
        {
            throw new ArgumentException("Zero vectors cannot be used for cosine similarity.", parameterName);
        }
    }
}
