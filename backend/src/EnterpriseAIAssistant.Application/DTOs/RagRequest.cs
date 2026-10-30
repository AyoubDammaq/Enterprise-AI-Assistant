namespace EnterpriseAIAssistant.Application.DTOs
{
    public class RagRequest
    {
        public string Question { get; set; } = string.Empty;

        public string? ConversationId { get; set; }
    }
}