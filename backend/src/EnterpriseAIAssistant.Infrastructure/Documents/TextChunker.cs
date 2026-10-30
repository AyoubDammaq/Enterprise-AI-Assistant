using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.Infrastructure.Documents;

/// <summary>
/// Splits document text into chunks while preserving paragraph boundaries when possible.
/// </summary>
public sealed class TextChunker
{
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;

    public TextChunker(int chunkSize = 800, int chunkOverlap = 100)
    {
        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chunkSize), "Chunk size must be greater than zero.");
        }

        if (chunkOverlap < 0 || chunkOverlap >= chunkSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chunkOverlap),
                "Chunk overlap must be non-negative and smaller than chunk size.");
        }

        _chunkSize = chunkSize;
        _chunkOverlap = chunkOverlap;
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

        var paragraphs = content
            .Split(
                new[] { "\r\n\r\n", "\n\n", "\r\r" },
                StringSplitOptions.None)
            .Select(paragraph => paragraph.Trim())
            .Where(paragraph => paragraph.Length > 0)
            .ToList();

        var chunks = new List<DocumentChunk>();
        var current = new System.Text.StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length > _chunkSize)
            {
                FlushCurrentChunk(chunks, current, documentId, source);

                AddLongParagraphChunks(
                    chunks, paragraph, documentId, source);

                continue;
            }

            var requiredLength = current.Length == 0
                ? paragraph.Length
                : current.Length + 2 + paragraph.Length;

            if (requiredLength <= _chunkSize)
            {
                if (current.Length > 0)
                {
                    current.Append("\n\n");
                }

                current.Append(paragraph);
                continue;
            }

            FlushCurrentChunk(chunks, current, documentId, source);

            AppendWithOverlap(current, paragraph);
        }

        FlushCurrentChunk(chunks, current, documentId, source);

        return chunks;
    }

    private void AddLongParagraphChunks(
        ICollection<DocumentChunk> chunks,
        string paragraph,
        string documentId,
        string source)
    {
        var start = 0;

        while (start < paragraph.Length)
        {
            var length = Math.Min(_chunkSize, paragraph.Length - start);
            var piece = paragraph.Substring(start, length);

            chunks.Add(new DocumentChunk(
                documentId,
                source,
                chunks.Count,
                piece));

            if (start + length >= paragraph.Length)
            {
                break;
            }

            start += _chunkSize - _chunkOverlap;
        }
    }

    private void FlushCurrentChunk(
        ICollection<DocumentChunk> chunks,
        System.Text.StringBuilder current,
        string documentId,
        string source)
    {
        if (current.Length == 0)
        {
            return;
        }

        chunks.Add(new DocumentChunk(
            documentId,
            source,
            chunks.Count,
            current.ToString()));

        current.Clear();
    }

    private void AppendWithOverlap(
        System.Text.StringBuilder current,
        string paragraph)
    {
        var overlapLength = Math.Min(_chunkOverlap, current.Length);
        var overlap = overlapLength > 0
            ? current.ToString(current.Length - overlapLength, overlapLength)
            : string.Empty;

        current.Clear();

        if (overlap.Length > 0)
        {
            current.Append(overlap);
        }

        if (current.Length > 0 && paragraph.Length > 0)
        {
            current.Append("\n\n");
        }

        current.Append(paragraph);

        // A paragraph can fit by itself, but not with the overlap.
        // Keep the chunk within the configured maximum size.
        if (current.Length > _chunkSize)
        {
            current.Clear();
            current.Append(paragraph);
        }
    }
}
