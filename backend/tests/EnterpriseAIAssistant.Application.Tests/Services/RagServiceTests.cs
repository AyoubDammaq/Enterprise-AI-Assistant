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
        private readonly Mock<IContextRelevanceEvaluator> _contextRelevantEvaluatorMock = new();
        private RagService CreateService()
        {
            return new RagService(
                _retrieverMock.Object,
                _contextRelevantEvaluatorMock.Object,
                _aiChatServiceMock.Object);
        }

        [Fact]
        public async Task AskAsync_WhenNoChunksAreRetrieved_ReturnsNoContextMessageAndDoesNotCallLlm()
        {
            // Arrange
            _retrieverMock
                .Setup(x => x.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<RetrievedChunk>());

            var service = CreateService();

            // Act
            var result = await service.AskAsync(
                "How can I reset my password?",
                "conversation-123",
                Guid.NewGuid());

            // Assert
            Assert.Equal(
                "I couldn't find relevant information in the available documents.",
                result.Answer);

            Assert.Empty(result.Sources);

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
        public async Task AskAsync_WhenChunksAreRetrieved_ReturnsAnswerAndDeduplicatedSources()
        {
            // Arrange
            var userId = Guid.NewGuid();

            const string question = "How can I reset my password?";
            const string conversationId = "conversation-123";

            const string expectedAnswer =
                "Open the sign-in page and select Forgot password.";

            var chunk1 = new DocumentChunk(
                "password-guide",
                "password-reset.md",
                0,
                "Open the sign-in page and select Forgot password.")
            {
                Section = "Account Management"
            };

            var chunk2 = new DocumentChunk(
                "password-guide",
                "password-reset.md",
                1,
                "A reset link will be sent to your email.")
            {
                Section = "Password Recovery"
            };

            var retrievedChunks = new[]
            {
                new RetrievedChunk(chunk1, 0.95),
                new RetrievedChunk(chunk2, 0.90),

                // Duplicate reference to chunk 1.
                new RetrievedChunk(chunk1, 0.85)
            };

            _retrieverMock
                .Setup(x => x.SearchAsync(
                    question,
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(retrievedChunks);

            _contextRelevantEvaluatorMock
                .Setup(x => x.IsContextSufficientAsync(
                    question,
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _aiChatServiceMock
                .Setup(x => x.GenerateGroundedResponseAsync(
                    question,
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    conversationId,
                    userId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedAnswer);

            var service = CreateService();

            // Act
            var result = await service.AskAsync(
                question,
                conversationId,
                userId);

            // Assert
            Assert.Equal(expectedAnswer, result.Answer);

            Assert.Equal(2, result.Sources.Count);

            Assert.Contains(result.Sources, source =>
                source.DocumentId == "password-guide" &&
                source.DocumentName == "password-reset.md" &&
                source.ChunkIndex == 0 &&
                source.ChunkId == "password-guide::chunk-0000" &&
                source.Section == "Account Management");

            Assert.Contains(result.Sources, source =>
                source.DocumentId == "password-guide" &&
                source.DocumentName == "password-reset.md" &&
                source.ChunkIndex == 1 &&
                source.ChunkId == "password-guide::chunk-0001" &&
                source.Section == "Password Recovery");

            _aiChatServiceMock.Verify(
                x => x.GenerateGroundedResponseAsync(
                    question,
                    It.Is<IReadOnlyList<RetrievedChunk>>(
                        chunks => ReferenceEquals(chunks, retrievedChunks)),
                    conversationId,
                    userId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AskAsync_ForwardsCancellationTokenToDependencies()
        {
            // Arrange
            using var cancellationTokenSource =
                new CancellationTokenSource();

            var cancellationToken =
                cancellationTokenSource.Token;

            const string question = "How can I reset my password?";
            const string conversationId = "conversation-123";

            var retrievedChunks = new[]
            {
                new RetrievedChunk(
                    new DocumentChunk(
                        "password-guide",
                        "password-reset.md",
                        0,
                        "Select Forgot password."),
                    0.95)
            };

            _retrieverMock
                .Setup(x => x.SearchAsync(
                    question,
                    It.IsAny<int?>(),
                    cancellationToken))
                .ReturnsAsync(retrievedChunks);

            _contextRelevantEvaluatorMock
                .Setup(x => x.IsContextSufficientAsync(
                    question,
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    cancellationToken))
                .ReturnsAsync(true);

            _aiChatServiceMock
                .Setup(x => x.GenerateGroundedResponseAsync(
                    question,
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    conversationId,
                    null,
                    cancellationToken))
                .ReturnsAsync("Select Forgot password.");

            var service = CreateService();

            // Act
            await service.AskAsync(
                question,
                conversationId,
                null,
                cancellationToken);

            // Assert
            _retrieverMock.Verify(
                x => x.SearchAsync(
                    question,
                    It.IsAny<int?>(),
                    cancellationToken),
                Times.Once);

            _contextRelevantEvaluatorMock.Verify(
                x => x.IsContextSufficientAsync(
                    question,
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    cancellationToken),
                Times.Once);

            _aiChatServiceMock.Verify(
                x => x.GenerateGroundedResponseAsync(
                    question,
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    conversationId,
                    null,
                    cancellationToken),
                Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public async Task AskAsync_WhenQuestionIsEmptyOrWhitespace_ThrowsArgumentException(
            string question)
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AskAsync(
                    question,
                    "conversation-123",
                    null));

            _retrieverMock.Verify(
                x => x.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task AskAsync_WhenRetrievalFails_PropagatesExceptionAndDoesNotCallLlm()
        {
            // Arrange
            _retrieverMock
                .Setup(x => x.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Embedding failed."));

            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                service.AskAsync(
                    "How do I reset my password?",
                    "conversation-1",
                    Guid.NewGuid()));

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
        public async Task AskAsync_WhenLlmFails_PropagatesException()
        {
            // Arrange
            var retrievedChunks = new[]
            {
                new RetrievedChunk(
                    new DocumentChunk(
                        "password-guide",
                        "password.md",
                        0,
                        "Select Forgot password."),
                    0.95)
            };

            _retrieverMock
                .Setup(x => x.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(retrievedChunks);

            _contextRelevantEvaluatorMock
                .Setup(x => x.IsContextSufficientAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _aiChatServiceMock
                .Setup(x => x.GenerateGroundedResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new InvalidOperationException("LLM unavailable."));

            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AskAsync(
                    "How do I reset my password?",
                    "conversation-1",
                    Guid.NewGuid()));
        }

        [Fact]
        public async Task AskAsync_WhenContextIsInsufficient_ReturnsNoContextMessageAndDoesNotCallLlm()
        {
            // Arrange
            var retrievedChunks = new[]
            {
                new RetrievedChunk(
                    new DocumentChunk(
                        "product-guide",
                        "product-guide.md",
                        0,
                        "The application supports inventory management."),
                    0.95)
            };

            _retrieverMock
                .Setup(x => x.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(retrievedChunks);

            _contextRelevantEvaluatorMock
                .Setup(x => x.IsContextSufficientAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var service = CreateService();

            // Act
            var result = await service.AskAsync(
                "What is the monthly subscription price?",
                "conversation-123",
                null);

            // Assert
            Assert.Equal(
                "I couldn't find relevant information in the available documents.",
                result.Answer);

            Assert.Empty(result.Sources);

            _contextRelevantEvaluatorMock.Verify(
                x => x.IsContextSufficientAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

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
        public async Task AskAsync_WhenSectionIsNull_PreservesNullSectionInSources()
        {
            // Arrange
            var chunk = new DocumentChunk(
                "general-guide",
                "general-guide.md",
                0,
                "General information.");

            var retrievedChunks = new[]
            {
                new RetrievedChunk(chunk, 0.95)
            };

            _retrieverMock
                .Setup(x => x.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(retrievedChunks);

            _contextRelevantEvaluatorMock
                .Setup(x => x.IsContextSufficientAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _aiChatServiceMock
                .Setup(x => x.GenerateGroundedResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<RetrievedChunk>>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("General information.");

            var service = CreateService();

            // Act
            var result = await service.AskAsync(
                "Tell me about the guide.",
                "conversation-123",
                Guid.NewGuid());

            // Assert
            var source = Assert.Single(result.Sources);

            Assert.Equal("general-guide", source.DocumentId);
            Assert.Equal("general-guide.md", source.DocumentName);
            Assert.Equal(0, source.ChunkIndex);
            Assert.Equal("general-guide::chunk-0000", source.ChunkId);
            Assert.Null(source.Section);
        }
    }
}
