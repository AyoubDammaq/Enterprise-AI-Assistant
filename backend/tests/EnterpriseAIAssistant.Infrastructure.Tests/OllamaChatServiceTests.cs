using EnterpriseAIAssistant.Infrastructure.AI.Ollama;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;

namespace EnterpriseAIAssistant.Infrastructure.Tests.AI.Ollama;

public class OllamaChatServiceTests
{
    private static Kernel BuildKernelWithFakeService(IChatCompletionService fakeChatService)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(fakeChatService);
        return builder.Build();
    }

    [Fact]
    public async Task GenerateResponseAsync_WithValidPrompt_ReturnsNonEmptyString()
    {
        // Arrange
        const string expectedContent = "Ollama AI response.";

        var fakeChatService = new Mock<IChatCompletionService>();
        fakeChatService
            .Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ChatMessageContent(AuthorRole.Assistant, expectedContent)]);

        var kernel = BuildKernelWithFakeService(fakeChatService.Object);
        var service = new OllamaChatService(kernel);

        // Act
        var result = await service.GenerateResponseAsync("What is Ollama?");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public async Task GenerateResponseAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var fakeChatService = new Mock<IChatCompletionService>();
        fakeChatService
            .Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var kernel = BuildKernelWithFakeService(fakeChatService.Object);
        var service = new OllamaChatService(kernel);

        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.GenerateResponseAsync("Hello", cts.Token));
    }
}