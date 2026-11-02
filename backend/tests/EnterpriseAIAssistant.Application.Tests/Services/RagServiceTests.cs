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

        [Fact]
        public async Task AskAsync_RetrievalFailure_PropagatesAndDoesNotCallLlm()
        {
            var retriever = new Mock<IKnowledgeRetriever>();
            var chat = new Mock<IAIChatService>();

            retriever
                .Setup(x => x.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Embedding failed."));

            var service = new RagService(retriever.Object, chat.Object);

            await Assert.ThrowsAsync<HttpRequestException>(() =>
                service.AskAsync(
                    "How do I reset my password?",
                    "conversation-1",
                    Guid.NewGuid()));

            chat.Verify(
                x => x.GenerateGroundedResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task AskAsync_LlmFailure_PropagatesException()
        {
            var retriever = new Mock<IKnowledgeRetriever>();
            var chat = new Mock<IAIChatService>();

            var retrieved = new[]
            {
            new RetrievedChunk(
                new DocumentChunk(
                    "password-guide",
                    "password.md",
                    0,
                    "Select Forgot password."),
                0.95)
        };

            retriever
                .Setup(x => x.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(retrieved);

            chat
                .Setup(x => x.GenerateGroundedResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("LLM unavailable."));

            var service = new RagService(retriever.Object, chat.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AskAsync(
                    "How do I reset my password?",
                    "conversation-1",
                    Guid.NewGuid()));
        }

        [Fact]
        public async Task AskAsync_EmptyRetrieval_DoesNotCallLlm()
        {
            var retriever = new Mock<IKnowledgeRetriever>();
            var chat = new Mock<IAIChatService>();

            retriever
                .Setup(x => x.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<RetrievedChunk>());

            var service = new RagService(retriever.Object, chat.Object);

            var response = await service.AskAsync(
                "Question not covered by the corpus",
                "conversation-1",
                Guid.NewGuid());

            Assert.Empty(response.Sources);
            Assert.Contains("couldn't find relevant information", response.Answer);

            chat.Verify(
                x => x.GenerateGroundedResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
