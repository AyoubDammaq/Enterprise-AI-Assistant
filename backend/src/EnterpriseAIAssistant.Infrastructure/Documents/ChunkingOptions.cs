namespace EnterpriseAIAssistant.Infrastructure.Documents
{
    public sealed class ChunkingOptions
    {
        public ChunkingStrategy Strategy { get; set; }
            = ChunkingStrategy.ParagraphAware;

        public int ChunkSize { get; set; } = 800;

        public int ChunkOverlap { get; set; } = 100;
    }
}
