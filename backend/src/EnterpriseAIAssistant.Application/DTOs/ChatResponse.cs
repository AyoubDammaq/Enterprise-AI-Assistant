namespace EnterpriseAIAssistant.Application.DTOs
{
    public class ChatResponse(string response, string? conversationId = null)
    {
        public string Response { get; set; } = response;
        public string? ConversationId { get; set; } = conversationId;
    }
}
