using System.Text;

namespace EnterpriseAIAssistant.Infrastructure.Documents.Strategies;

internal sealed class RecursiveCharacterChunkingStrategy : IChunkingStrategy
{
    private static readonly string[] Separators =
    {
        "\r\n\r\n",
        "\n\n",
        "\r\n",
        "\n",
        ". ",
        "? ",
        "! ",
        " "
    };

    public IReadOnlyList<string> Split(
        string content,
        int chunkSize,
        int chunkOverlap)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<string>();
        }

        var segments = SplitRecursively(
            content,
            chunkSize,
            separatorIndex: 0);

        return MergeSegments(
            segments,
            chunkSize,
            chunkOverlap);
    }

    private static List<string> SplitRecursively(
        string text,
        int chunkSize,
        int separatorIndex)
    {
        if (text.Length <= chunkSize)
        {
            return new List<string> { text };
        }

        if (separatorIndex >= Separators.Length)
        {
            return SplitByCharacters(text, chunkSize);
        }

        var separator = Separators[separatorIndex];

        var parts = SplitPreservingSeparator(text, separator);

        if (parts.Count <= 1)
        {
            return SplitRecursively(
                text,
                chunkSize,
                separatorIndex + 1);
        }

        var result = new List<string>();

        foreach (var part in parts)
        {
            if (part.Length == 0)
            {
                continue;
            }

            result.AddRange(
                SplitRecursively(
                    part,
                    chunkSize,
                    separatorIndex + 1));
        }

        return result;
    }

    private static List<string> SplitPreservingSeparator(
        string text,
        string separator)
    {
        var parts = new List<string>();
        var start = 0;
        var position = text.IndexOf(
            separator,
            start,
            StringComparison.Ordinal);

        while (position >= 0)
        {
            var end = position + separator.Length;

            parts.Add(text.Substring(start, end - start));

            start = end;

            position = text.IndexOf(
                separator,
                start,
                StringComparison.Ordinal);
        }

        if (start < text.Length)
        {
            parts.Add(text[start..]);
        }

        return parts;
    }

    private static List<string> SplitByCharacters(
        string text,
        int chunkSize)
    {
        var result = new List<string>();

        for (var start = 0; start < text.Length; start += chunkSize)
        {
            var length = Math.Min(
                chunkSize,
                text.Length - start);

            result.Add(text.Substring(start, length));
        }

        return result;
    }

    private static IReadOnlyList<string> MergeSegments(
        IReadOnlyList<string> segments,
        int chunkSize,
        int chunkOverlap)
    {
        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var segment in segments)
        {
            if (current.Length == 0)
            {
                current.Append(segment);
                continue;
            }

            if (current.Length + segment.Length <= chunkSize)
            {
                current.Append(segment);
                continue;
            }

            chunks.Add(current.ToString());

            var overlapLength = Math.Min(
                chunkOverlap,
                current.Length);

            var overlap = overlapLength > 0
                ? current.ToString(
                    current.Length - overlapLength,
                    overlapLength)
                : string.Empty;

            current.Clear();

            if (overlap.Length + segment.Length <= chunkSize)
            {
                current.Append(overlap);
            }

            current.Append(segment);
        }

        if (current.Length > 0)
        {
            chunks.Add(current.ToString());
        }

        return chunks;
    }
}