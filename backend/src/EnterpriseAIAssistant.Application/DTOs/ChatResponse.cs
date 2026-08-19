namespace EnterpriseAIAssistant.Application.DTOs
{
    public class ChatResponse
    {
        public string Response { get; set; } = string.Empty;
        public ChatResponse(string response)
        {
            Response = response;
        }
    }
}
