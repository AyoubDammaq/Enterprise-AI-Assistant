namespace EnterpriseAIAssistant.Infrastructure.Documents.Strategies;

internal interface IChunkingStrategy
{
    IReadOnlyList<string> Split(
        string content,
        int chunkSize,
        int chunkOverlap);
}