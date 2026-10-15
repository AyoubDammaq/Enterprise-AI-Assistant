namespace EnterpriseAIAssistant.Application.Interfaces
{
    public interface IAIChatService
    {
        Task<string> GenerateResponseAsync(string prompt, string conversationId, Guid? userId, CancellationToken cancellationToken = default);
    }
}
