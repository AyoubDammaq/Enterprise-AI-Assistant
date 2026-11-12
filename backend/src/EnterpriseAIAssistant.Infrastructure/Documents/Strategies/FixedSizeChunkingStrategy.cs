namespace EnterpriseAIAssistant.Infrastructure.Documents.Strategies;

internal sealed class FixedSizeChunkingStrategy : IChunkingStrategy
{
    public IReadOnlyList<string> Split(
        string content,
        int chunkSize,
        int chunkOverlap)
    {
        var chunks = new List<string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            return chunks;
        }

        var start = 0;
        var step = chunkSize - chunkOverlap;

        while (start < content.Length)
        {
            var length = Math.Min(
                chunkSize,
                content.Length - start);

            chunks.Add(content.Substring(start, length));

            if (start + length >= content.Length)
            {
                break;
            }

            start += step;
        }

        return chunks;
    }
}