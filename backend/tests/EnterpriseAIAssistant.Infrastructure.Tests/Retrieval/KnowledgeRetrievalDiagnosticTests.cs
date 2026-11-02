
using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.Documents;
using EnterpriseAIAssistant.Infrastructure.Retrieval;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval
{
    [Trait("Category", "Integration")]
    public sealed class KnowledgeRetrievalDiagnosticTests
    {
        [Fact]
        public async Task DiagnoseRetrieval_WithRealOllamaEmbeddings()
        {
            // Arrange: use the actual project document directory.
            var documentsPath =
                Environment.GetEnvironmentVariable("RAG_DOCUMENTS_PATH");

            Assert.False(
                string.IsNullOrWhiteSpace(documentsPath),
                "Set the RAG_DOCUMENTS_PATH environment variable.");

            Assert.True(
                Directory.Exists(documentsPath),
                $"Documents directory not found: {documentsPath}");

            var documentLoader = new FileSystemDocumentLoader(
                Options.Create(new DocumentLoadingOptions
                {
                    RootDirectory = documentsPath!,
                    ChunkSize = 800,
                    ChunkOverlap = 100
                }));

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            IEmbeddingService embeddingService =
                new OllamaEmbeddingService(
                    httpClient,
                    Options.Create(new OllamaEmbeddingOptions
                    {
                        Endpoint = "http://localhost:11434",
                        EmbeddingModel = "nomic-embed-text",
                        EmbeddingDimensions = 768
                    }));

            IVectorStore vectorStore = new InMemoryVectorStore();

            IKnowledgeIndexer indexer = new KnowledgeIndexer(
                embeddingService,
                vectorStore);

            IKnowledgeRetriever retriever = new KnowledgeRetriever(
                embeddingService,
                vectorStore);

            // Load and index the real documents.
            var chunks = await documentLoader.LoadAsync(documentsPath);

            Assert.NotEmpty(chunks);

            var indexedCount = await indexer.IndexAsync(chunks);

            Assert.Equal(chunks.Count, indexedCount);

            Console.WriteLine();
            Console.WriteLine("========== RAG RETRIEVAL DIAGNOSTIC ==========");
            Console.WriteLine($"Documents path: {documentsPath}");
            Console.WriteLine($"Chunks indexed: {indexedCount}");

            var testCases = new[]
            {
                (
                    Id: "Q1",
                    Question: "Which architectural layers does the backend follow?",
                    ExpectedSource: "company-info.md"
                ),
                (
                    Id: "Q2",
                    Question: "Which technologies are used by the backend?",
                    ExpectedSource: "company-info.md"
                ),
                (
                    Id: "Q3",
                    Question: "What AI capabilities does the application support?",
                    ExpectedSource: "company-info.md"
                ),
                (
                    Id: "Q4",
                    Question: "What type of internal information does the assistant use to answer questions?",
                    ExpectedSource: "products.txt"
                ),
                (
                    Id: "Q5",
                    Question: "Which embedding model and chat model does the application use?",
                    ExpectedSource: "products.txt"
                )
            };

            // Collect retrieval results for metric calculation.
            var evaluationSamples =
                new List<RetrievalEvaluationSample>();

            foreach (var testCase in testCases)
            {
                Console.WriteLine();
                Console.WriteLine($"--- {testCase.Id} ---");
                Console.WriteLine($"Question: {testCase.Question}");
                Console.WriteLine($"Expected source: {testCase.ExpectedSource}");

                var results = await retriever.SearchAsync(
                    testCase.Question,
                    topK: 5);

                var retrievedSources = results
                    .Select(result => result.Chunk.Source)
                    .ToList();

                // Save the evaluation sample, including empty results.
                evaluationSamples.Add(
                    new RetrievalEvaluationSample(
                        testCase.ExpectedSource,
                        retrievedSources));

                if (results.Count == 0)
                {
                    Console.WriteLine("Retrieved chunks: NONE");
                    Console.WriteLine("Expected source found: NO");
                    continue;
                }

                foreach (var result in results)
                {
                    Console.WriteLine(
                        $"Source: {result.Chunk.Source} | " +
                        $"Chunk: {result.Chunk.ChunkIndex} | " +
                        $"Score: {result.SimilarityScore:F4}");

                    Console.WriteLine($"Content: {result.Chunk.Content}");
                }

                var found = retrievedSources.Contains(
                    testCase.ExpectedSource,
                    StringComparer.OrdinalIgnoreCase);

                Console.WriteLine(
                    $"Expected source found: {(found ? "YES" : "NO")}");
            }

            // Calculate retrieval evaluation metrics.
            var hitRateAt1 =
                RetrievalMetrics.CalculateHitRateAtK(
                    evaluationSamples, k: 1);

            var hitRateAt2 =
                RetrievalMetrics.CalculateHitRateAtK(
                    evaluationSamples, k: 2);

            var meanReciprocalRank =
                RetrievalMetrics.CalculateMeanReciprocalRank(
                    evaluationSamples);

            // Display the final evaluation report.
            Console.WriteLine();
            Console.WriteLine("========== RAG EVALUATION REPORT ==========");
            Console.WriteLine($"Total queries: {evaluationSamples.Count}");
            Console.WriteLine($"Chunks indexed: {indexedCount}");
            Console.WriteLine($"Hit Rate @1: {hitRateAt1:P2}");
            Console.WriteLine($"Hit Rate @2: {hitRateAt2:P2}");
            Console.WriteLine($"MRR: {meanReciprocalRank:F4}");
            Console.WriteLine("============================================");
        }
    }
}
