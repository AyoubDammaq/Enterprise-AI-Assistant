namespace EnterpriseAIAssistant.Application.Interfaces
{
    public interface IAIChatService
    {
        Task<string> GetResponseAsync(string message, CancellationToken cancellationToken = default);
    }
}
