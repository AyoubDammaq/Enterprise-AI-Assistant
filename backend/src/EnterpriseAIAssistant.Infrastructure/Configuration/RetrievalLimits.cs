namespace EnterpriseAIAssistant.Infrastructure.Configuration;

/// <summary>
/// Defines shared limits for RAG retrieval operations.
/// </summary>
public static class RetrievalLimits
{
    /// <summary>
    /// Maximum number of chunks that can be retrieved in one search.
    /// </summary>
    public const int MaximumTopK = 20;
}
