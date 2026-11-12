using System.Text;

namespace EnterpriseAIAssistant.Infrastructure.Documents.Strategies;

internal sealed class ParagraphAwareChunkingStrategy : IChunkingStrategy
{
    public IReadOnlyList<string> Split(
        string content,
        int chunkSize,
        int chunkOverlap)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<string>();
        }

        var paragraphs = content
            .Split(
                new[] { "\r\n\r\n", "\n\n", "\r\r" },
                StringSplitOptions.None)
            .Select(paragraph => paragraph.Trim())
            .Where(paragraph => paragraph.Length > 0)
            .ToList();

        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length > chunkSize)
            {
                FlushCurrentChunk(chunks, current);

                AddLongParagraphChunks(
                    chunks,
                    paragraph,
                    chunkSize,
                    chunkOverlap);

                continue;
            }

            var requiredLength = current.Length == 0
                ? paragraph.Length
                : current.Length + 2 + paragraph.Length;

            if (requiredLength <= chunkSize)
            {
                if (current.Length > 0)
                {
                    current.Append("\n\n");
                }

                current.Append(paragraph);
                continue;
            }

            FlushCurrentChunk(chunks, current);

            AppendWithOverlap(
                current,
                paragraph,
                chunkSize,
                chunkOverlap);
        }

        FlushCurrentChunk(chunks, current);

        return chunks;
    }

    private static void AddLongParagraphChunks(
        ICollection<string> chunks,
        string paragraph,
        int chunkSize,
        int chunkOverlap)
    {
        var start = 0;

        while (start < paragraph.Length)
        {
            var length = Math.Min(
                chunkSize,
                paragraph.Length - start);

            chunks.Add(paragraph.Substring(start, length));

            if (start + length >= paragraph.Length)
            {
                break;
            }

            start += chunkSize - chunkOverlap;
        }
    }

    private static void FlushCurrentChunk(
        ICollection<string> chunks,
        StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        chunks.Add(current.ToString());
        current.Clear();
    }

    private static void AppendWithOverlap(
        StringBuilder current,
        string paragraph,
        int chunkSize,
        int chunkOverlap)
    {
        var overlapLength = Math.Min(
            chunkOverlap,
            current.Length);

        var overlap = overlapLength > 0
            ? current.ToString(
                current.Length - overlapLength,
                overlapLength)
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

        // Preserve the maximum chunk-size constraint.
        // If overlap does not fit, it is discarded.
        if (current.Length > chunkSize)
        {
            current.Clear();
            current.Append(paragraph);
        }
    }
}