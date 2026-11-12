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


        [Fact]
        public async Task DiagnoseChunkingStrategies_WithSameCorpus()
        {
            // Arrange: load the same knowledge corpus for every strategy.
            var documentsPath =
                Environment.GetEnvironmentVariable("RAG_DOCUMENTS_PATH");

            Assert.False(
                string.IsNullOrWhiteSpace(documentsPath),
                "Set the RAG_DOCUMENTS_PATH environment variable.");

            Assert.True(
                Directory.Exists(documentsPath),
                $"Documents directory not found: {documentsPath}");

            var expectedFiles = new[]
            {
                "support-faq.md",
                "product-guide.md",
                "troubleshooting.md"
            };

            _logger.LogInformation(
                "========== CHUNKING STRATEGIES COMPARISON ==========");

            _logger.LogInformation("Documents path: {DocumentsPath}", documentsPath);
            _logger.LogInformation("Chunk size: 800");
            _logger.LogInformation("Chunk overlap: 100");

            _logger.LogInformation(
                "{Strategy,-22} | {Count,8} | {Min,8} | {Average,10} | {Max,8}",
                "Strategy",
                "Chunks",
                "Min",
                "Average",
                "Max");

            _logger.LogInformation(
                new string('-', 75));

            // Act: evaluate each strategy using identical input documents.
            foreach (var strategy in Enum.GetValues<ChunkingStrategy>())
            {
                var documentLoader = new FileSystemDocumentLoader(
                    Options.Create(new DocumentLoadingOptions
                    {
                        RootDirectory = documentsPath!,
                        Strategy = strategy,
                        ChunkSize = 800,
                        ChunkOverlap = 100
                    }));

                var chunks = (await documentLoader.LoadAsync(documentsPath!))
                    .ToList();

                // Ensure that the same three knowledge documents are loaded.
                Assert.NotEmpty(chunks);

                var loadedSources = chunks
                    .Select(chunk => Path.GetFileName(chunk.Source))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                Assert.Equal(expectedFiles.Length, loadedSources.Count);

                foreach (var expectedFile in expectedFiles)
                {
                    Assert.Contains(
                        loadedSources,
                        source => string.Equals(
                            source,
                            expectedFile,
                            StringComparison.OrdinalIgnoreCase));
                }

                // Calculate chunking statistics.
                var statistics = ChunkingStatisticsCalculator.Calculate(chunks);

                // Log results.
                _logger.LogInformation(
                    "{Strategy,-22} | {Count,8} | {Min,8} | {Average,10:F2} | {Max,8}",
                    strategy,
                    statistics.ChunkCount,
                    statistics.MinLength,
                    statistics.AverageLength,
                    statistics.MaxLength);

                Assert.Equal(chunks.Count, statistics.ChunkCount);
            }

            _logger.LogInformation(
                "=====================================================");
        }

        [Fact]
        public async Task DiagnoseRetrievalAcrossChunkingStrategies()
        {
            // 1. Validate the evaluation corpus.
            var documentsPath =
                Environment.GetEnvironmentVariable("RAG_DOCUMENTS_PATH");

            Assert.False(
                string.IsNullOrWhiteSpace(documentsPath),
                "Set the RAG_DOCUMENTS_PATH environment variable.");

            Assert.True(
                Directory.Exists(documentsPath),
                $"Documents directory not found: {documentsPath}");

            // 2. Keep the same questions for every strategy.
            var testCases = new[]
            {
                (
                    Id: "Q1",
                    Question: "How can I reset my Demo Portal password?",
                    ExpectedSource: (string?)"support-faq.md"
                ),
                (
                    Id: "Q2",
                    Question: "How long does an account remain locked after too many failed login attempts?",
                    ExpectedSource: (string?)"support-faq.md"
                ),
                (
                    Id: "Q3",
                    Question: "What is the default low-stock threshold?",
                    ExpectedSource: (string?)"product-guide.md"
                ),
                (
                    Id: "Q4",
                    Question: "Which role can modify inventory settings and manage users?",
                    ExpectedSource: (string?)"product-guide.md"
                ),
                (
                    Id: "Q5",
                    Question: "How can I resolve error INV-001?",
                    ExpectedSource: (string?)"troubleshooting.md"
                ),
                (
                    Id: "Q6",
                    Question: "What is the monthly subscription price?",
                    ExpectedSource: (string?)null
                ),
                (
                    Id: "Q7",
                    Question: "Which database engine is used in production?",
                    ExpectedSource: (string?)null
                )
            };

            const int chunkSize = 800;
            const int chunkOverlap = 100;
            const int topK = 5;
            const double minimumSimilarityScore = 0.50;

            // 3. Reuse the same embedding model and HTTP client.
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

            var reports = new List<(
                ChunkingStrategy Strategy,
                int ChunkCount,
                double HitRateAt1,
                double HitRateAt2,
                double Mrr,
                int NegativeQueriesWithResults)>();

            _logger.LogInformation(
                "========== RETRIEVAL STRATEGIES COMPARISON ==========");

            foreach (var strategy in Enum.GetValues<ChunkingStrategy>())
            {
                _logger.LogInformation(
                    "Evaluating strategy: {Strategy}", strategy);

                // 4. Load documents using the current strategy.
                var documentLoader = new FileSystemDocumentLoader(
                    Options.Create(new DocumentLoadingOptions
                    {
                        RootDirectory = documentsPath!,
                        Strategy = strategy,
                        ChunkSize = chunkSize,
                        ChunkOverlap = chunkOverlap
                    }));

                var loadedChunks =
                    await documentLoader.LoadAsync(documentsPath!);

                Assert.NotEmpty(loadedChunks);

                // Exclude evaluation questions from the knowledge index.
                var knowledgeChunks = loadedChunks
                    .Where(chunk =>
                        !string.Equals(
                            Path.GetFileName(chunk.Source),
                            "evaluation-questions.md",
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

                Assert.NotEmpty(knowledgeChunks);

                // 5. Create a completely independent vector store.
                IVectorStore vectorStore = CreateStore(
                    minimumSimilarityScore);

                IKnowledgeIndexer indexer = new KnowledgeIndexer(
                    embeddingService,
                    vectorStore);

                IKnowledgeRetriever retriever = new KnowledgeRetriever(
                    embeddingService,
                    vectorStore,
                    Options.Create(new RetrievalOptions
                    {
                        TopK = topK
                    }));

                // 6. Index chunks generated by this strategy only.
                var indexedCount = await indexer.IndexAsync(
                    knowledgeChunks);

                Assert.Equal(knowledgeChunks.Count, indexedCount);

                _logger.LogInformation(
                    "Strategy={Strategy}, IndexedChunks={ChunkCount}",
                    strategy,
                    indexedCount);

                var evaluationSamples =
                    new List<RetrievalEvaluationSample>();

                var negativeQueriesWithResults = 0;

                // 7. Execute exactly the same evaluation questions.
                foreach (var testCase in testCases)
                {
                    var results = await retriever.SearchAsync(
                        testCase.Question,
                        topK: topK);

                    var retrievedSources = results
                        .Select(result =>
                            Path.GetFileName(result.Chunk.Source))
                        .ToList();

                    _logger.LogInformation(
                        "{QuestionId} | Results={ResultCount}",
                        testCase.Id,
                        results.Count);


                    foreach (var result in results)
                    {
                        _logger.LogInformation(
                            "Source={Source}, Chunk={ChunkIndex}, Score={Score:F4}",
                            Path.GetFileName(result.Chunk.Source),
                            result.Chunk.ChunkIndex,
                            result.SimilarityScore);

                        // TEMPORARY: Display complete Q7 chunks for ParagraphAware.
                        if (strategy == ChunkingStrategy.RecursiveCharacter &&
                            testCase.Id == "Q7")
                        {
                            _logger.LogInformation(
                                """
                                ===== Q7 RETRIEVED CHUNK =====
                                Strategy: {Strategy}
                                Source: {Source}
                                ChunkIndex: {ChunkIndex}
                                Similarity: {Score:F4}
                                Content:
                                {Content}
                                =============================
                                """,
                                strategy,
                                Path.GetFileName(result.Chunk.Source),
                                result.Chunk.ChunkIndex,
                                result.SimilarityScore,
                                result.Chunk.Content);
                        }
                    }


                    if (testCase.ExpectedSource is not null)
                    {
                        evaluationSamples.Add(
                            new RetrievalEvaluationSample(
                                testCase.ExpectedSource,
                                retrievedSources));
                    }
                    else if (results.Count > 0)
                    {
                        // This is not automatically a false positive.
                        // Retrieved chunks still require relevance review.
                        negativeQueriesWithResults++;
                    }
                }

                // 8. Calculate metrics for positive queries only.
                Assert.NotEmpty(evaluationSamples);

                var hitRateAt1 =
                    RetrievalMetrics.CalculateHitRateAtK(
                        evaluationSamples,
                        k: 1);

                var hitRateAt2 =
                    RetrievalMetrics.CalculateHitRateAtK(
                        evaluationSamples,
                        k: 2);

                var mrr =
                    RetrievalMetrics.CalculateMeanReciprocalRank(
                        evaluationSamples);

                reports.Add((
                    strategy,
                    indexedCount,
                    hitRateAt1,
                    hitRateAt2,
                    mrr,
                    negativeQueriesWithResults));
            }

            // 9. Display the final comparison.
            _logger.LogInformation("");
            _logger.LogInformation(
                "================ RETRIEVAL COMPARISON ================");

            _logger.LogInformation(
                "{Strategy,-22} | {Chunks,6} | {Hit1,8} | {Hit2,8} | {Mrr,8} | {Negative,8}",
                "Strategy",
                "Chunks",
                "Hit@1",
                "Hit@2",
                "MRR",
                "Neg.Results");

            _logger.LogInformation(new string('-', 85));

            foreach (var report in reports)
            {
                _logger.LogInformation(
                    "{Strategy,-22} | {Chunks,6} | {Hit1,7:P1} | {Hit2,7:P1} | {Mrr,8:F4} | {Negative,8}",
                    report.Strategy,
                    report.ChunkCount,
                    report.HitRateAt1,
                    report.HitRateAt2,
                    report.Mrr,
                    report.NegativeQueriesWithResults);
            }

            _logger.LogInformation(
                "=======================================================");
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
