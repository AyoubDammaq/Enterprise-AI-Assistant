namespace EnterpriseAIAssistant.Infrastructure.Documents;

public sealed class DocumentLoadingOptions
{
    public const string SectionName = "DocumentLoading";

    public string RootDirectory { get; set; } = "Documents";

    public int ChunkSize { get; set; } = 800;

    public int ChunkOverlap { get; set; } = 100;
}