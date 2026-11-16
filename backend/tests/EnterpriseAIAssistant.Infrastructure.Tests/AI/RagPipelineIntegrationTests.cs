using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Application.Services;
using EnterpriseAIAssistant.Infrastructure.AI;
using EnterpriseAIAssistant.Infrastructure.Configuration;
using EnterpriseAIAssistant.Infrastructure.Documents;
using EnterpriseAIAssistant.Infrastructure.Memory;
using EnterpriseAIAssistant.Infrastructure.Retrieval;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Moq;

namespace EnterpriseAIAssistant.Infrastructure.Tests.AI;

[Trait("Category", "Integration")]
public sealed class RagPipelineIntegrationTests
{
    private static readonly string OllamaEndpoint =
        "http://localhost:11434";

    private const string ChatModel = "llama3.2";
    private const string EmbeddingModel = "nomic-embed-text";

    private const string NoContextMessage =
        "I couldn't find relevant information in the available documents.";

    private sealed class RagFixture : IDisposable
    {
        public HttpClient HttpClient { get; }
        public IKnowledgeRetriever Retriever { get; }
        public IContextRelevanceEvaluator Evaluator { get; }
        public Kernel Kernel { get; }

        public RagFixture(
            HttpClient httpClient,
            IKnowledgeRetriever retriever,
            IContextRelevanceEvaluator evaluator,
            Kernel kernel)
        {
            HttpClient = httpClient;
            Retriever = retriever;
            Evaluator = evaluator;
            Kernel = kernel;
        }

        public void Dispose() => HttpClient.Dispose();
    }

    private static async Task<RagFixture> CreateFixtureAsync()
    {
        var documentsPath =
            Environment.GetEnvironmentVariable("RAG_DOCUMENTS_PATH");

        Assert.False(
            string.IsNullOrWhiteSpace(documentsPath),
            "Set the RAG_DOCUMENTS_PATH environment variable.");

        Assert.True(
            Directory.Exists(documentsPath),
            $"Documents directory not found: {documentsPath}");

        var loader = new FileSystemDocumentLoader(
            Options.Create(new DocumentLoadingOptions
            {
                RootDirectory = documentsPath!,
                ChunkSize = 800,
                ChunkOverlap = 100
            }));

        var loadedChunks = await loader.LoadAsync(documentsPath!);

        var chunks = loadedChunks
            .Where(chunk =>
                !string.Equals(
                    Path.GetFileName(chunk.Source),
                    "evaluation-questions.md",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(chunks);

        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        try
        {
            IEmbeddingService embeddingService =
                new OllamaEmbeddingService(
                    httpClient,
                    Options.Create(new OllamaEmbeddingOptions
                    {
                        Endpoint = OllamaEndpoint,
                        EmbeddingModel = EmbeddingModel,
                        EmbeddingDimensions = 768
                    }));

            IVectorStore vectorStore =
                new InMemoryVectorStore(
                    NullLogger<InMemoryVectorStore>.Instance,
                    Options.Create(new RetrievalOptions
                    {
                        MinimumSimilarityScore = 0.50
                    }));

            IKnowledgeIndexer indexer =
                new KnowledgeIndexer(
                    embeddingService,
                    vectorStore);

            IKnowledgeRetriever retriever =
                new KnowledgeRetriever(
                    embeddingService,
                    vectorStore,
                    Options.Create(new RetrievalOptions
                    {
                        TopK = 5
                    }));

            var indexedCount =
                await indexer.IndexAsync(chunks);

            Assert.Equal(chunks.Count, indexedCount);

            var kernelBuilder = Kernel.CreateBuilder();

            kernelBuilder.AddOllamaChatCompletion(
                modelId: ChatModel,
                endpoint: new Uri(OllamaEndpoint),
                serviceId: "ollama");

            var kernel = kernelBuilder.Build();

            IContextRelevanceEvaluator evaluator =
                new SemanticKernelContextRelevanceEvaluator(
                    kernel,
                    NullLogger<SemanticKernelContextRelevanceEvaluator>.Instance);

            return new RagFixture(
                httpClient,
                retriever,
                evaluator,
                kernel);
        }
        catch
        {
            httpClient.Dispose();
            throw;
        }
    }

    [Fact]
    public async Task RagService_ShouldGenerateOnlyWhenContextIsSufficient()
    {
        using var fixture = await CreateFixtureAsync();

        var chatServiceMock = new Mock<IAIChatService>();

        chatServiceMock
            .Setup(service => service.GenerateGroundedResponseAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test grounded response.");

        var ragService = new RagService(
            fixture.Retriever,
            fixture.Evaluator,
            chatServiceMock.Object);

        var testCases = new[]
        {
            (
                Id: "Q1",
                Question: "How can a user reset their Demo Portal password?",
                ExpectedSufficient: true,
                ExpectedSource: "support-faq.md"
            ),
            (
                Id: "Q2",
                Question: "How long is a Demo Portal account locked after failed login attempts?",
                ExpectedSufficient: true,
                ExpectedSource: "support-faq.md"
            ),
            (
                Id: "Q3",
                Question: "What is the default low-stock threshold in Atlas Inventory?",
                ExpectedSufficient: true,
                ExpectedSource: "product-guide.md"
            ),
            (
                Id: "Q4",
                Question: "Which user role can modify inventory settings and manage users?",
                ExpectedSufficient: true,
                ExpectedSource: "product-guide.md"
            ),
            (
                Id: "Q5",
                Question: "How should a user resolve error INV-001?",
                ExpectedSufficient: true,
                ExpectedSource: "troubleshooting.md"
            ),
            (
                Id: "Q6",
                Question: "What is the monthly subscription price of Atlas Inventory?",
                ExpectedSufficient: false,
                ExpectedSource: ""
            ),
            (
                Id: "Q7",
                Question: "Which database engine does Atlas Inventory use in production?",
                ExpectedSufficient: false,
                ExpectedSource: ""
            )
        };

        var generationCount = 0;

        foreach (var testCase in testCases)
        {
            var response = await ragService.AskAsync(
                testCase.Question,
                $"rag-test-{testCase.Id}",
                Guid.NewGuid());

            if (testCase.ExpectedSufficient)
            {
                generationCount++;

                Assert.Equal(
                    "Test grounded response.",
                    response.Answer);

                Assert.NotEmpty(response.Sources);

                Assert.Contains(
                    response.Sources,
                    source => string.Equals(
                        Path.GetFileName(source.DocumentName),
                        testCase.ExpectedSource,
                        StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                Assert.Equal(
                    NoContextMessage,
                    response.Answer);

                Assert.Empty(response.Sources);
            }
        }

        chatServiceMock.Verify(
            service => service.GenerateGroundedResponseAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(generationCount));

        Assert.Equal(5, generationCount);
    }

    [Fact]
    public async Task RagPipeline_ShouldGenerateGroundedAnswersAndRejectUnsupportedQuestions()
    {
        using var fixture = await CreateFixtureAsync();

        var conversationStore = new InMemoryConversationStore();
        var memoryService = new InMemoryMemoryService();
        var memoryUserContext = new MemoryUserContext();

        IAIChatService chatService =
            new SemanticKernelChatService(
                fixture.Kernel,
                conversationStore,
                memoryService,
                memoryUserContext,
                NullLogger<SemanticKernelChatService>.Instance);

        var ragService = new RagService(
            fixture.Retriever,
            fixture.Evaluator,
            chatService);

        var supportedQuestions = new[]
        {
            (
                Id: "Q1",
                Question: "How can a user reset their Demo Portal password?",
                ExpectedSource: "support-faq.md"
            ),
            (
                Id: "Q3",
                Question: "What is the default low-stock threshold in Atlas Inventory?",
                ExpectedSource: "product-guide.md"
            ),
            (
                Id: "Q5",
                Question: "How should a user resolve error INV-001?",
                ExpectedSource: "troubleshooting.md"
            )
        };

        foreach (var testCase in supportedQuestions)
        {
            var response = await ragService.AskAsync(
                testCase.Question,
                $"rag-e2e-{testCase.Id}",
                Guid.NewGuid());

            Assert.False(
                string.IsNullOrWhiteSpace(response.Answer),
                $"{testCase.Id}: generated answer is empty.");

            Assert.NotEqual(NoContextMessage, response.Answer);

            Assert.NotEmpty(response.Sources);

            Assert.Contains(
                response.Sources,
                source => string.Equals(
                    Path.GetFileName(source.DocumentName),
                    testCase.ExpectedSource,
                    StringComparison.OrdinalIgnoreCase));

            var distinctSources = response.Sources
                .Select(source => new
                {
                    source.DocumentId,
                    source.ChunkIndex
                })
                .Distinct()
                .Count();

            Assert.Equal(
                response.Sources.Count,
                distinctSources);
        }

        var unsupportedQuestions = new[]
        {
            "What is the monthly subscription price of Atlas Inventory?",
            "Which database engine does Atlas Inventory use in production?"
        };

        foreach (var question in unsupportedQuestions)
        {
            var response = await ragService.AskAsync(
                question,
                $"rag-e2e-negative-{Guid.NewGuid()}",
                Guid.NewGuid());

            Assert.Equal(NoContextMessage, response.Answer);
            Assert.Empty(response.Sources);
        }
    }
}
