namespace EnterpriseAIAssistant.Application.DTOs
{
    public sealed record RagResponse(
        string Answer,
        IReadOnlyList<RagSource> Sources);
}
