namespace EnterpriseAIAssistant.Infrastructure.Configuration;

public sealed class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    public int TopK { get; set; } = 5;

    public double MinimumSimilarityScore { get; set; } = 0.50;
}