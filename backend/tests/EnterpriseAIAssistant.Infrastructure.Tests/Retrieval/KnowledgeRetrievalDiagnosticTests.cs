using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.Configuration;
using EnterpriseAIAssistant.Infrastructure.Documents;
using EnterpriseAIAssistant.Infrastructure.Retrieval;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval
{
    [Trait("Category", "Integration")]
    public sealed class KnowledgeRetrievalDiagnosticTests(ITestOutputHelper output)
    {
        private readonly ILogger<KnowledgeRetrievalDiagnosticTests> _logger = new XunitLogger<KnowledgeRetrievalDiagnosticTests>(output);

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


            // Load only the knowledge documents from the Corpus directory.
            var loadedChunks =
                await documentLoader.LoadAsync(documentsPath!);

            Assert.NotEmpty(loadedChunks);

            // Verify that exactly three knowledge documents are loaded.
            var expectedFiles = new[]
            {
                "support-faq.md",
                "product-guide.md",
                "troubleshooting.md"
            };

            var loadedSources = loadedChunks
                .Select(chunk => chunk.Source)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Assert.Equal(3, loadedSources.Count);

            foreach (var expectedFile in expectedFiles)
            {
                Assert.Contains(
                    loadedSources,
                    source => string.Equals(
                        Path.GetFileName(source),
                        expectedFile,
                        StringComparison.OrdinalIgnoreCase));
            }

            // All loaded chunks belong to the knowledge corpus.
            var chunks = loadedChunks.ToList();

            Assert.NotEmpty(chunks);

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

            _logger.LogInformation("========== RAG RETRIEVAL DIAGNOSTIC ==========");

            _logger.LogInformation("Documents path: {DocumentsPath}", documentsPath);

            _logger.LogInformation(
                "Knowledge documents loaded: {DocumentCount}",
                knowledgeSources.Count);

            _logger.LogInformation("Evaluation questions loaded as documents: 0");

            _logger.LogInformation(
                "Knowledge chunks loaded: {ChunkCount}",
                chunks.Count);

            _logger.LogInformation(
                "Chunks indexed: {IndexedCount}",
                indexedCount);

            // Positive questions have an expected source.
            // Negative questions intentionally have no expected source.

            var testCases = new[]
            {
                (
                    Id: "Q1",
                    Question: "How can a user reset their Demo Portal password?",
                    ExpectedSource: (string?)"support-faq.md"
                ),
                (
                    Id: "Q2",
                    Question: "How long is a Demo Portal account locked after failed login attempts?",
                    ExpectedSource: (string?)"support-faq.md"
                ),
                (
                    Id: "Q3",
                    Question: "What is the default low-stock threshold in Atlas Inventory?",
                    ExpectedSource: (string?)"product-guide.md"
                ),
                (
                    Id: "Q4",
                    Question: "Which user role can modify inventory settings and manage users?",
                    ExpectedSource: (string?)"product-guide.md"
                ),
                (
                    Id: "Q5",
                    Question: "How should a user resolve error INV-001?",
                    ExpectedSource: (string?)"troubleshooting.md"
                ),
                (
                    Id: "Q6",
                    Question: "What is the monthly subscription price of Atlas Inventory?",
                    ExpectedSource: (string?)null
                ),
                (
                    Id: "Q7",
                    Question: "Which database engine does Atlas Inventory use in production?",
                    ExpectedSource: (string?)null
                )
            };

            // Validate the evaluation dataset structure.
            Assert.Equal(7, testCases.Length);

            Assert.Equal(
                5,
                testCases.Count(testCase => testCase.ExpectedSource is not null));

            Assert.Equal(
                2,
                testCases.Count(testCase => testCase.ExpectedSource is null));

            // Only positive questions contribute to Hit Rate and MRR.
            var evaluationSamples =
                new List<RetrievalEvaluationSample>();

            var positiveQueriesWithExpectedSourceFound = 0;
            var negativeQueriesWithRetrievedChunks = 0;
            var negativeQueriesWithoutRetrievedChunks = 0;

            foreach (var testCase in testCases)
            {
                _logger.LogInformation("--- {QuestionId} ---", testCase.Id);

                _logger.LogInformation(
                    "Question: {Question}",
                    testCase.Question);

                _logger.LogInformation(
                    "Expected source: {ExpectedSource}",
                    testCase.ExpectedSource ?? "NONE (negative query)");

                var results = await retriever.SearchAsync(
                    testCase.Question,
                    topK: 5);

                var retrievedSources = results
                    .Select(result => result.Chunk.Source)
                    .ToList();

                if (results.Count == 0)
                {
                    _logger.LogInformation("Retrieved chunks: NONE");

                    if (testCase.ExpectedSource is not null)
                    {
                        evaluationSamples.Add(
                            new RetrievalEvaluationSample(
                                testCase.ExpectedSource,
                                retrievedSources));

                        _logger.LogInformation("Expected source found: NO");
                    }
                    else
                    {
                        negativeQueriesWithoutRetrievedChunks++;

                        _logger.LogInformation("Negative query: no chunks retrieved. Abstention expected.");
                    }

                    continue;
                }

                foreach (var result in results)
                {
                    _logger.LogInformation(
                        "Retrieved chunk | Source: {Source} | Chunk: {ChunkIndex} | Score: {Score:F4}",
                        Path.GetFileName(result.Chunk.Source),
                        result.Chunk.ChunkIndex,
                        result.SimilarityScore);
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

                    _logger.LogInformation(
                        "Expected source found: {Found}",
                        found ? "YES" : "NO");
                }
                else
                {
                    negativeQueriesWithRetrievedChunks++;

                    _logger.LogInformation(
                        "Negative query: potentially irrelevant chunks retrieved.");

                    _logger.LogInformation(
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
            _logger.LogInformation("========== POSITIVE RETRIEVAL EVALUATION ==========");

            _logger.LogInformation("Total queries: {TotalQueries}", testCases.Length);

            _logger.LogInformation("Positive queries: {PositiveQueries}", positiveQueryCount);

            _logger.LogInformation("Negative queries: {NegativeQueries}", negativeQueryCount);

            _logger.LogInformation(
                "Positive queries with expected source found: {Found}/{Total}",
                positiveQueriesWithExpectedSourceFound,
                positiveQueryCount);

            _logger.LogInformation("Hit Rate @1: {HitRateAt1:P2}", hitRateAt1);

            _logger.LogInformation("Hit Rate @2: {HitRateAt2:P2}", hitRateAt2);

            _logger.LogInformation("MRR: {MRR:F4}", meanReciprocalRank);

            // Display the negative-query and abstention evaluation report.
            _logger.LogInformation("========== NEGATIVE QUERY / ABSTENTION EVALUATION ==========");

            _logger.LogInformation("Negative queries: {NegativeQueries}", negativeQueryCount);

            _logger.LogInformation(
                "Negative queries with no retrieved chunks: {NegativeQueriesWithNoRetrievedChunks}",
                negativeQueriesWithoutRetrievedChunks);

            _logger.LogInformation(
                "Negative queries with retrieved chunks: {NegativeQueriesWithRetrievedChunks}",
                negativeQueriesWithRetrievedChunks);

            _logger.LogInformation(
                "Negative queries with retrieved chunks require relevance validation.");

            _logger.LogInformation(
                "Retrieval results alone do not prove that an answer is supported.");

            _logger.LogInformation("============================================================");
        }
    }

    internal sealed class XunitLogger<T> : ILogger<T>
    {
        private readonly ITestOutputHelper _output;

        public XunitLogger(ITestOutputHelper output)
        {
            _output = output;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            try
            {
                _output.WriteLine(
                    $"[{logLevel}] {formatter(state, exception)}");

                if (exception is not null)
                {
                    _output.WriteLine(exception.ToString());
                }
            }
            catch (InvalidOperationException)
            {
                // xUnit output may be unavailable outside the test execution context.
            }
        }
    }
}
