namespace EnterpriseAIAssistant.Application.Interfaces
{
    public interface IAIChatService
    {
        Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
    }
}
