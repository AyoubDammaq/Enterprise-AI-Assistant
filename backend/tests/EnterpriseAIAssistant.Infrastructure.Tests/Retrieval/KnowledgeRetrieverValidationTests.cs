using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.Configuration;
using EnterpriseAIAssistant.Infrastructure.Retrieval;
using Microsoft.Extensions.Options;
using Moq;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

public sealed class KnowledgeRetrieverValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task SearchAsync_InvalidPerCallTopK_ThrowsBeforeGeneratingEmbedding(
        int topK)
    {
        var embeddingService = new Mock<IEmbeddingService>(MockBehavior.Strict);
        var vectorStore = new Mock<IVectorStore>(MockBehavior.Strict);

        var retriever = new KnowledgeRetriever(
            embeddingService.Object,
            vectorStore.Object,
            Options.Create(new RetrievalOptions { TopK = 5 }));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => retriever.SearchAsync("test query", topK));

        embeddingService.Verify(
            service => service.GenerateEmbeddingAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        vectorStore.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task SearchAsync_InvalidConfiguredTopK_ThrowsBeforeGeneratingEmbedding(
        int configuredTopK)
    {
        var embeddingService = new Mock<IEmbeddingService>(MockBehavior.Strict);
        var vectorStore = new Mock<IVectorStore>(MockBehavior.Strict);

        var retriever = new KnowledgeRetriever(
            embeddingService.Object,
            vectorStore.Object,
            Options.Create(new RetrievalOptions { TopK = configuredTopK }));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => retriever.SearchAsync("test query"));

        embeddingService.Verify(
            service => service.GenerateEmbeddingAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        vectorStore.VerifyNoOtherCalls();
    }
}
