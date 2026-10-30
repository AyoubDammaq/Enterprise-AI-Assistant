using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.Application.Interfaces
{
    public interface IAIChatService
    {
        Task<string> GenerateResponseAsync(
            string prompt,
            string conversationId,
            Guid? userId,
            CancellationToken cancellationToken = default);

        Task<string> GenerateGroundedResponseAsync(
            string question,
            IReadOnlyList<RetrievedChunk> context,
            string conversationId,
            Guid? userId,
            CancellationToken cancellationToken = default);
    }
}