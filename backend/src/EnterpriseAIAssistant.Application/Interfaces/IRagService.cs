using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.Application.Interfaces
{
    public interface IRagService
    {
        Task<RagResponse> AskAsync(
            string question,
            string conversationId,
            Guid? userId,
            CancellationToken cancellationToken = default);
    }
}
