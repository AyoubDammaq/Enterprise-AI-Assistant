using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.Configuration;
using EnterpriseAIAssistant.Infrastructure.Documents;
using EnterpriseAIAssistant.Infrastructure.Retrieval;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

[Trait("Category", "Integration")]
public sealed class RagPipelineIntegrationTests
{
    private static InMemoryVectorStore CreateStore(double minimumSimilarityScore = 0.50) =>
        new(NullLogger<InMemoryVectorStore>.Instance, Options.Create(new RetrievalOptions { MinimumSimilarityScore = minimumSimilarityScore }));

    [Fact]
    public async Task RagPipeline_WithRealOllamaEmbeddings_RetrievesRelevantDocument()
    {
        // Arrange: create an isolated demo corpus.
        var rootDirectory = Path.Combine(
            Path.GetTempPath(),
            $"EnterpriseAIAssistant-RAG-{Guid.NewGuid():N}");

        Directory.CreateDirectory(rootDirectory);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(rootDirectory, "password-reset.md"),
                """
                Password reset instructions

                If you forget your password, open the sign-in page.
                Select Forgot password, enter your account email address,
                and follow the password reset link sent to your inbox.
                Never share your password with support.
                """);

            await File.WriteAllTextAsync(
                Path.Combine(rootDirectory, "dashboard.md"),
                """
                Dashboard documentation

                The dashboard displays monthly revenue, recent transactions,
                account balances, and spending charts.
                Use the reporting page to export monthly financial reports.
                """);

            var documentLoader = new FileSystemDocumentLoader(
                Options.Create(new DocumentLoadingOptions
                {
                    RootDirectory = rootDirectory,
                    ChunkSize = 1200,
                    ChunkOverlap = 100
                }));

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            var embeddingService = new OllamaEmbeddingService(
                httpClient,
                Options.Create(new OllamaEmbeddingOptions
                {
                    Endpoint = "http://localhost:11434",
                    EmbeddingModel = "nomic-embed-text",
                    EmbeddingDimensions = 768
                }));

            IVectorStore vectorStore = CreateStore();

            var indexer = new KnowledgeIndexer(
                embeddingService,
                vectorStore);

            var retriever = new KnowledgeRetriever(
                embeddingService,
                vectorStore,
                Options.Create(new RetrievalOptions { TopK = 5 }));

            // Act: load documents, generate embeddings, and index them.
            var chunks = await documentLoader.LoadAsync();

            Assert.NotEmpty(chunks);

            var indexedCount = await indexer.IndexAsync(chunks);

            Assert.Equal(chunks.Count, indexedCount);

            // Search with a real query embedding and cosine similarity.
            var results = await retriever.SearchAsync(
                "How can I reset my forgotten password?",
                topK: 2);

            // Assert: only relevant documents should be retrieved.
            Assert.NotEmpty(results);

            Assert.Contains(
                results,
                result => result.Chunk.Source == "password-reset.md");

            Assert.DoesNotContain(
                results,
                result => result.Chunk.Source == "dashboard.md");

            Assert.All(results, result =>
                Assert.True(
                    result.SimilarityScore >=
                    0.50));

            Assert.True(
                double.IsFinite(results[0].SimilarityScore));
        }
        finally
        {
            // Remove only the temporary test documents.
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }
}
