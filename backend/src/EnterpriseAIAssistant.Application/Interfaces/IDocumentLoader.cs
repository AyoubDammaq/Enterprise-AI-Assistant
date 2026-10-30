using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.Application.Interfaces
{
    public interface IDocumentLoader
    {
        Task<IReadOnlyList<DocumentChunk>> LoadAsync(
            string? path = null,
            CancellationToken cancellationToken = default);
    }
}