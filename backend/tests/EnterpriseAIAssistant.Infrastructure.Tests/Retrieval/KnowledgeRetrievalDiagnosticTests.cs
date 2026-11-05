using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.Configuration;
using EnterpriseAIAssistant.Infrastructure.Documents;
using EnterpriseAIAssistant.Infrastructure.Retrieval;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval
{
    [Trait("Category", "Integration")]
    public sealed class KnowledgeRetrievalDiagnosticTests
    {
        private static InMemoryVectorStore CreateStore(double minimumSimilarityScore = 0.50) =>
            new(NullLogger<InMemoryVectorStore>.Instance, Options.Create(new RetrievalOptions { MinimumSimilarityScore = minimumSimilarityScore }));

        [Fact]
        public async Task DiagnoseRetrieval_WithRealOllamaEmbeddings()
        {
            // Arrange: load the evaluation corpus.
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

            IVectorStore vectorStore = CreateStore();

            IKnowledgeIndexer indexer = new KnowledgeIndexer(
                embeddingService,
                vectorStore);

            IKnowledgeRetriever retriever = new KnowledgeRetriever(
                embeddingService,
                vectorStore,
                Options.Create(new RetrievalOptions { TopK = 5 }));

            // Load all files from the RAG directory.
            var loadedChunks =
                await documentLoader.LoadAsync(documentsPath!);

            Assert.NotEmpty(loadedChunks);

            // Verify that all four expected files exist.
            var expectedFiles = new[]
            {
                "support-faq.md",
                "product-guide.md",
                "troubleshooting.md",
                "evaluation-questions.md"
            };

            var loadedSources = loadedChunks
                .Select(chunk => chunk.Source)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var expectedFile in expectedFiles)
            {
                Assert.Contains(
                    loadedSources,
                    source => string.Equals(
                        Path.GetFileName(source),
                        expectedFile,
                        StringComparison.OrdinalIgnoreCase));
            }

            // Exclude evaluation-questions.md from the knowledge corpus.
            var chunks = loadedChunks
                .Where(chunk =>
                    !string.Equals(
                        Path.GetFileName(chunk.Source),
                        "evaluation-questions.md",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.NotEmpty(chunks);

            // Verify that evaluation questions are not indexed.
            Assert.DoesNotContain(
                chunks,
                chunk => string.Equals(
                    Path.GetFileName(chunk.Source),
                    "evaluation-questions.md",
                    StringComparison.OrdinalIgnoreCase));

            var knowledgeSources = chunks
                .Select(chunk => chunk.Source)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Assert.Equal(3, knowledgeSources.Count);

            // Index only the three knowledge documents.
            var indexedCount = await indexer.IndexAsync(chunks);

            Assert.Equal(chunks.Count, indexedCount);

            Console.WriteLine();
            Console.WriteLine("========== RAG RETRIEVAL DIAGNOSTIC ==========");
            Console.WriteLine($"Documents path: {documentsPath}");
            Console.WriteLine($"Total files loaded: {loadedSources.Count}");
            Console.WriteLine($"Knowledge documents: {knowledgeSources.Count}");
            Console.WriteLine($"Evaluation files excluded: 1");
            Console.WriteLine($"Knowledge chunks loaded: {chunks.Count}");
            Console.WriteLine($"Chunks indexed: {indexedCount}");

            // Positive questions have an expected source.
            // Negative questions intentionally have no expected source.
            var testCases = new[]
            {
                (
                    Id: "Q1",
                    Question: "How can I reset my password?",
                    ExpectedSource: (string?)"support-faq.md"
                ),
                (
                    Id: "Q2",
                    Question: "After how many failed attempts is an account locked, and when is it unlocked?",
                    ExpectedSource: (string?)"support-faq.md"
                ),
                (
                    Id: "Q3",
                    Question: "What is the low stock threshold?",
                    ExpectedSource: (string?)"product-guide.md"
                ),
                (
                    Id: "Q4",
                    Question: "What role and permissions does the Administrator have?",
                    ExpectedSource: (string?)"product-guide.md"
                ),
                (
                    Id: "Q5",
                    Question: "How can I resolve error INV-001?",
                    ExpectedSource: (string?)"troubleshooting.md"
                ),
                (
                    Id: "Q6",
                    Question: "What is the monthly price?",
                    ExpectedSource: (string?)null
                ),
                (
                    Id: "Q7",
                    Question: "Which database engine is used in production?",
                    ExpectedSource: (string?)null
                )
            };

            // Only positive questions contribute to Hit Rate and MRR.
            var evaluationSamples =
                new List<RetrievalEvaluationSample>();

            var positiveQueriesWithExpectedSourceFound = 0;
            var negativeQueriesWithRetrievedChunks = 0;
            var negativeQueriesWithoutRetrievedChunks = 0;

            foreach (var testCase in testCases)
            {
                Console.WriteLine();
                Console.WriteLine($"--- {testCase.Id} ---");
                Console.WriteLine($"Question: {testCase.Question}");

                Console.WriteLine(
                    $"Expected source: {testCase.ExpectedSource ?? "NONE (negative query)"}");

                var results = await retriever.SearchAsync(
                    testCase.Question,
                    topK: 5);

                var retrievedSources = results
                    .Select(result => result.Chunk.Source)
                    .ToList();

                if (results.Count == 0)
                {
                    Console.WriteLine("Retrieved chunks: NONE");

                    if (testCase.ExpectedSource is not null)
                    {
                        evaluationSamples.Add(
                            new RetrievalEvaluationSample(
                                testCase.ExpectedSource,
                                retrievedSources));

                        Console.WriteLine("Expected source found: NO");
                    }
                    else
                    {
                        negativeQueriesWithoutRetrievedChunks++;

                        Console.WriteLine(
                            "Negative query: no chunks retrieved. Abstention expected.");
                    }

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

                if (testCase.ExpectedSource is not null)
                {
                    var found = retrievedSources.Contains(
                        testCase.ExpectedSource,
                        StringComparer.OrdinalIgnoreCase);

                    evaluationSamples.Add(
                        new RetrievalEvaluationSample(
                            testCase.ExpectedSource,
                            retrievedSources));

                    if (found)
                    {
                        positiveQueriesWithExpectedSourceFound++;
                    }

                    Console.WriteLine(
                        $"Expected source found: {(found ? "YES" : "NO")}");
                }
                else
                {
                    negativeQueriesWithRetrievedChunks++;

                    Console.WriteLine(
                        "Negative query: potentially irrelevant chunks retrieved.");

                    Console.WriteLine(
                        "Manual relevance review required. No answer should be generated " +
                        "without admissible evidence.");
                }
            }

            // Calculate metrics using positive queries only.
            var hitRateAt1 =
                RetrievalMetrics.CalculateHitRateAtK(
                    evaluationSamples, k: 1);

            var hitRateAt2 =
                RetrievalMetrics.CalculateHitRateAtK(
                    evaluationSamples, k: 2);

            var meanReciprocalRank =
                RetrievalMetrics.CalculateMeanReciprocalRank(
                    evaluationSamples);

            // Separate positive and negative test cases.
            var positiveQueryCount = testCases.Count(
                testCase => testCase.ExpectedSource is not null);

            var negativeQueryCount = testCases.Count(
                testCase => testCase.ExpectedSource is null);

            // Display the positive retrieval evaluation report.
            Console.WriteLine();
            Console.WriteLine("========== POSITIVE RETRIEVAL EVALUATION ==========");

            Console.WriteLine($"Total queries: {testCases.Length}");
            Console.WriteLine($"Positive queries: {positiveQueryCount}");
            Console.WriteLine($"Negative queries: {negativeQueryCount}");

            Console.WriteLine($"Knowledge documents: {knowledgeSources.Count}");
            Console.WriteLine($"Chunks indexed: {indexedCount}");

            Console.WriteLine(
                $"Positive queries with expected source found: " +
                $"{positiveQueriesWithExpectedSourceFound}/{positiveQueryCount}");

            Console.WriteLine($"Hit Rate @1: {hitRateAt1:P2}");
            Console.WriteLine($"Hit Rate @2: {hitRateAt2:P2}");
            Console.WriteLine($"MRR: {meanReciprocalRank:F4}");

            Console.WriteLine("====================================================");

            // Display the negative-query and abstention evaluation report.
            Console.WriteLine();
            Console.WriteLine("========== NEGATIVE QUERY / ABSTENTION EVALUATION ==========");

            Console.WriteLine($"Negative queries: {negativeQueryCount}");

            Console.WriteLine(
                $"Negative queries with no retrieved chunks: " +
                $"{negativeQueriesWithoutRetrievedChunks}");

            Console.WriteLine(
                $"Negative queries with retrieved chunks: " +
                $"{negativeQueriesWithRetrievedChunks}");

            Console.WriteLine(
                "Negative queries with retrieved chunks require relevance validation.");

            Console.WriteLine(
                "Retrieval results alone do not prove that an answer is supported.");

            Console.WriteLine("============================================================");
        }
    }
}
