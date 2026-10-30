namespace EnterpriseAIAssistant.Application.Interfaces;

/// <summary>Converts text into a dense vector without exposing a specific AI provider.</summary>
public interface IEmbeddingService
{
    Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}
