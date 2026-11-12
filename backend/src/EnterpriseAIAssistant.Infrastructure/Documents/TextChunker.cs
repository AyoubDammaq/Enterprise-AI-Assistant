using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Infrastructure.Documents.Strategies;

namespace EnterpriseAIAssistant.Infrastructure.Documents;

/// <summary>
/// Facade responsible for selecting a chunking strategy
/// and converting its output into DocumentChunk objects.
/// </summary>
public sealed class TextChunker
{
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;
    private readonly IChunkingStrategy _strategy;

    public TextChunker(
        int chunkSize = 800,
        int chunkOverlap = 100)
        : this(new ChunkingOptions
        {
            ChunkSize = chunkSize,
            ChunkOverlap = chunkOverlap,
            Strategy = ChunkingStrategy.ParagraphAware
        })
    {
    }

    public TextChunker(ChunkingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ChunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.ChunkSize),
                "Chunk size must be greater than zero.");
        }

        if (options.ChunkOverlap < 0 ||
            options.ChunkOverlap >= options.ChunkSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.ChunkOverlap),
                "Chunk overlap must be non-negative and smaller than chunk size.");
        }

        if (!Enum.IsDefined(options.Strategy))
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.Strategy),
                "Unknown chunking strategy.");
        }

        _chunkSize = options.ChunkSize;
        _chunkOverlap = options.ChunkOverlap;

        _strategy = options.Strategy switch
        {
            ChunkingStrategy.FixedSize =>
                new FixedSizeChunkingStrategy(),

            ChunkingStrategy.ParagraphAware =>
                new ParagraphAwareChunkingStrategy(),

            ChunkingStrategy.RecursiveCharacter =>
                new RecursiveCharacterChunkingStrategy(),

            _ => throw new ArgumentOutOfRangeException(
                nameof(options.Strategy))
        };
    }

    public IReadOnlyList<DocumentChunk> Chunk(
        string documentId,
        string source,
        string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<DocumentChunk>();
        }

        var chunkContents = _strategy.Split(
            content,
            _chunkSize,
            _chunkOverlap);

        return chunkContents
            .Select((chunkContent, index) =>
                new DocumentChunk(
                    documentId,
                    source,
                    index,
                    chunkContent))
            .ToList();
    }
}