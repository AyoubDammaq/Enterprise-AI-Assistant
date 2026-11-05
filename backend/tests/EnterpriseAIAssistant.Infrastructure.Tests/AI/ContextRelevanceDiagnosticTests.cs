using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.AI;
using EnterpriseAIAssistant.Infrastructure.Documents;
using EnterpriseAIAssistant.Infrastructure.Retrieval;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace EnterpriseAIAssistant.Infrastructure.Tests.AI
{
    [Trait("Category", "Integration")]
    public sealed class ContextRelevanceDiagnosticTests
    {
        private static InMemoryVectorStore CreateStore() =>
            new(NullLogger<InMemoryVectorStore>.Instance);

        [Fact]
        public async Task DiagnoseContextRelevance_WithRealOllama()
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
                vectorStore);

            // Load documents.
            var loadedChunks =
                await documentLoader.LoadAsync(documentsPath!);

            Assert.NotEmpty(loadedChunks);

            // Exclude evaluation questions from the knowledge corpus.
            var chunks = loadedChunks
                .Where(chunk =>
                    !string.Equals(
                        Path.GetFileName(chunk.Source),
                        "evaluation-questions.md",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

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

            // Index the knowledge documents.
            var indexedCount = await indexer.IndexAsync(chunks);

            Assert.Equal(chunks.Count, indexedCount);

            // Build a Kernel using the same Ollama configuration as the API.
            var kernelBuilder = Kernel.CreateBuilder();

            kernelBuilder.AddOllamaChatCompletion(
                modelId: "llama3.2",
                endpoint: new Uri("http://localhost:11434"),
                serviceId: "ollama");

            var kernel = kernelBuilder.Build();

            // Use the actual context relevance evaluator.
            IContextRelevanceEvaluator evaluator =
                new SemanticKernelContextRelevanceEvaluator(kernel);

            // Positive questions must have sufficient context.
            // Negative questions must be rejected.
            var testCases = new[]
            {
                (
                    Id: "Q1",
                    Question: "How can I reset my password?",
                    ExpectedSufficient: true
                ),
                (
                    Id: "Q2",
                    Question: "After how many failed attempts is an account locked, and when is it unlocked?",
                    ExpectedSufficient: true
                ),
                (
                    Id: "Q3",
                    Question: "What is the low stock threshold?",
                    ExpectedSufficient: true
                ),
                (
                    Id: "Q4",
                    Question: "What role and permissions does the Administrator have?",
                    ExpectedSufficient: true
                ),
                (
                    Id: "Q5",
                    Question: "How can I resolve error INV-001?",
                    ExpectedSufficient: true
                ),
                (
                    Id: "Q6",
                    Question: "What is the monthly price?",
                    ExpectedSufficient: false
                ),
                (
                    Id: "Q7",
                    Question: "Which database engine is used in production?",
                    ExpectedSufficient: false
                )
            };

            var truePositives = 0;
            var falsePositives = 0;
            var trueNegatives = 0;
            var falseNegatives = 0;

            Console.WriteLine();
            Console.WriteLine("========== CONTEXT RELEVANCE DIAGNOSTIC ==========");
            Console.WriteLine($"Documents path: {documentsPath}");
            Console.WriteLine($"Knowledge documents: {knowledgeSources.Count}");
            Console.WriteLine($"Chunks indexed: {indexedCount}");
            Console.WriteLine("Chat model: llama3.2");
            Console.WriteLine("Embedding model: nomic-embed-text");

            foreach (var testCase in testCases)
            {
                Console.WriteLine();
                Console.WriteLine($"--- {testCase.Id} ---");
                Console.WriteLine($"Question: {testCase.Question}");

                // Retrieve the context using the real retriever.
                var retrievedChunks = await retriever.SearchAsync(
                    testCase.Question,
                    topK: 5);

                Console.WriteLine(
                    $"Retrieved chunks: {retrievedChunks.Count}");

                foreach (var result in retrievedChunks)
                {
                    Console.WriteLine(
                        $"Source: {Path.GetFileName(result.Chunk.Source)} | " +
                        $"Chunk: {result.Chunk.ChunkIndex} | " +
                        $"Score: {result.SimilarityScore:F4}");

                    Console.WriteLine("----- CHUNK CONTENT BEGIN -----");
                    Console.WriteLine(result.Chunk.Content);
                    Console.WriteLine("----- CHUNK CONTENT END -----");
                }

                // Evaluate whether the retrieved context contains
                // sufficient explicit evidence to answer the question.
                var actualSufficient =
                    await evaluator.IsContextSufficientAsync(
                        testCase.Question,
                        retrievedChunks);

                var expectedLabel = testCase.ExpectedSufficient
                    ? "SUPPORTED"
                    : "INSUFFICIENT";

                var actualLabel = actualSufficient
                    ? "SUPPORTED"
                    : "INSUFFICIENT";

                var correct =
                    testCase.ExpectedSufficient == actualSufficient;

                Console.WriteLine($"Expected: {expectedLabel}");
                Console.WriteLine($"Actual:   {actualLabel}");
                Console.WriteLine(
                    $"Result:   {(correct ? "CORRECT" : "INCORRECT")}");

                // Update the confusion matrix.
                if (testCase.ExpectedSufficient && actualSufficient)
                {
                    truePositives++;
                }
                else if (!testCase.ExpectedSufficient && actualSufficient)
                {
                    falsePositives++;
                }
                else if (!testCase.ExpectedSufficient && !actualSufficient)
                {
                    trueNegatives++;
                }
                else
                {
                    falseNegatives++;
                }
            }

            // Calculate classification metrics.
            var total = testCases.Length;

            var correctPredictions =
                truePositives + trueNegatives;

            var accuracy =
                (double)correctPredictions / total;

            var precision =
                truePositives + falsePositives == 0
                    ? 0
                    : (double)truePositives /
                      (truePositives + falsePositives);

            var recall =
                truePositives + falseNegatives == 0
                    ? 0
                    : (double)truePositives /
                      (truePositives + falseNegatives);

            var specificity =
                trueNegatives + falsePositives == 0
                    ? 0
                    : (double)trueNegatives /
                      (trueNegatives + falsePositives);

            Console.WriteLine();
            Console.WriteLine("========== CONFUSION MATRIX ==========");
            Console.WriteLine($"True Positives:  {truePositives}");
            Console.WriteLine($"False Positives: {falsePositives}");
            Console.WriteLine($"True Negatives:  {trueNegatives}");
            Console.WriteLine($"False Negatives: {falseNegatives}");

            Console.WriteLine();
            Console.WriteLine("========== CLASSIFICATION METRICS ==========");
            Console.WriteLine($"Accuracy:    {accuracy:P2}");
            Console.WriteLine($"Precision:   {precision:P2}");
            Console.WriteLine($"Recall:      {recall:P2}");
            Console.WriteLine($"Specificity: {specificity:P2}");

            Console.WriteLine("============================================");

            Console.WriteLine();
            Console.WriteLine(
                "Diagnostic completed. Review classification errors " +
                "before considering the evaluator validated.");
        }
    }
}
