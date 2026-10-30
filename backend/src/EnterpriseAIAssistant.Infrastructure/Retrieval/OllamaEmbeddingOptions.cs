namespace EnterpriseAIAssistant.Infrastructure.Retrieval;

public sealed class OllamaEmbeddingOptions
{
    public const string SectionName = "Ollama";

    public string Endpoint { get; set; } = "http://localhost:11434";

    public string EmbeddingModel { get; set; } = "nomic-embed-text";

    public int EmbeddingDimensions { get; set; } = 768;
}
