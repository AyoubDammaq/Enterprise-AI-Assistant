namespace EnterpriseAIAssistant.Application.DTOs
{
    public sealed record RagSource(
        string DocumentId,
        string DocumentName,
        int ChunkIndex);
}
