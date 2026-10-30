using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Application.Services;
using Moq;

namespace EnterpriseAIAssistant.Application.Tests.Services
{
    public sealed class RagServiceTests
    {
        private readonly Mock<IKnowledgeRetriever> _retrieverMock = new();
        private readonly Mock<IAIChatService> _aiChatServiceMock = new();

        private RagService CreateService()
        {
            return new RagService(
                _retrieverMock.Object,
                _aiChatServiceMock.Object);
        }

        [Fact]
        public async Task AskAsync_WhenNoChunksAreRetrieved_DoesNotCallLlm()
        {
            // Arrange
            _retrieverMock
                .Setup(x => x.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<RetrievedChunk>());

            var service = CreateService();

            // Act
            var result = await service.AskAsync(
                "How can I reset my password?",
                "conversation-123",
                Guid.NewGuid());

            // Assert
            Assert.Empty(result.Sources);
            Assert.Contains("couldn't find relevant information", result.Answer);

            _aiChatServiceMock.Verify(
                x => x.GenerateGroundedResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task AskAsync_WhenChunksAreRetrieved_ReturnsAnswerAndSources()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var chunk = new DocumentChunk(
                "password-guide",
                "password-reset.md",
                0,
                "Open the sign-in page and select Forgot password.");

            var retrievedChunks = new[]
            {
            new RetrievedChunk(chunk, 0.95)
        };

            _retrieverMock
                .Setup(x => x.SearchAsync(
                    "How can I reset my password?",
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(retrievedChunks);

            _aiChatServiceMock
                .Setup(x => x.GenerateGroundedResponseAsync(
                    "How can I reset my password?",
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    "conversation-123",
                    userId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("Open the sign-in page and select Forgot password.");

            var service = CreateService();

            // Act
            var result = await service.AskAsync(
                "How can I reset my password?",
                "conversation-123",
                userId);

            // Assert
            Assert.Equal(
                "Open the sign-in page and select Forgot password.",
                result.Answer);

            var source = Assert.Single(result.Sources);

            Assert.Equal("password-guide", source.DocumentId);
            Assert.Equal("password-reset.md", source.DocumentName);
            Assert.Equal(0, source.ChunkIndex);

            _aiChatServiceMock.Verify(
                x => x.GenerateGroundedResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
