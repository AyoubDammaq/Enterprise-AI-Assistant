namespace EnterpriseAIAssistant.Application.DTOs
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? ConversationId { get; set; }
        public Guid? UserId { get; set; } = null;
    }
}
