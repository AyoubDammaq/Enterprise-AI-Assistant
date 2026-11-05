using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.Application.Interfaces
{
    public interface IContextRelevanceEvaluator
    {
        Task<bool> IsContextSufficientAsync(
            string question,
            IReadOnlyList<RetrievedChunk> context,
            CancellationToken cancellationToken = default);
    }
}