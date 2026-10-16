using EnterpriseAIAssistant.API.Controllers;
using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EnterpriseAIAssistant.API.Tests
{
    public class ChatControllerTests
    {
        private readonly Mock<IAIChatService> _mockChatService;
        private readonly ChatController _controller;

        public ChatControllerTests()
        {
            _mockChatService = new Mock<IAIChatService>();
            _controller = new ChatController(_mockChatService.Object);
        }

        [Fact]
        public async Task Chat_WithValidMessage_ReturnsOkWithResponse()
        {
            // Arrange
            const string userMessage = "What is AI?";
            const string aiResponse = "AI stands for Artificial Intelligence.";

            _mockChatService
                .Setup(s => s.GenerateResponseAsync(userMessage, It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            var request = new ChatRequest
            {
                Message = userMessage,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = await _controller.Chat(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal(aiResponse, response.Response);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null!)]
        public async Task Chat_WithEmptyOrWhitespaceMessage_ReturnsBadRequest(string? message)
        {
            // Arrange
            var request = new ChatRequest
            {
                Message = message,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = await _controller.Chat(request, CancellationToken.None);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Message cannot be empty.", badRequest.Value);
        }

        [Fact]
        public async Task Chat_WithValidMessage_CallsServiceExactlyOnce()
        {
            // Arrange
            const string userMessage = "Hello AI";
            _mockChatService
                .Setup(s => s.GenerateResponseAsync(userMessage, It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Hello!");

            var request = new ChatRequest
            {
                Message = userMessage,
                UserId = Guid.NewGuid()
            };

            // Act
            await _controller.Chat(request, CancellationToken.None);

            // Assert
            _mockChatService.Verify(
                s => s.GenerateResponseAsync(userMessage, It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Chat_WhenServiceThrows_PropagatesException()
        {
            // Arrange
            _mockChatService
                .Setup(s => s.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("AI service unavailable."));

            var request = new ChatRequest
            {
                Message = "Hello",
                UserId = Guid.NewGuid()
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _controller.Chat(request, CancellationToken.None));
        }

        [Fact]
        public async Task Chat_WithValidMessage_ResponseContainsAIContent()
        {
            // Arrange
            const string expected = "Enterprise AI answer.";
            _mockChatService
                .Setup(s => s.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var request = new ChatRequest
            {
                Message = "Tell me something.",
                UserId = Guid.NewGuid()
            };

            // Act
            var result = await _controller.Chat(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.False(string.IsNullOrWhiteSpace(response.Response));
            Assert.Equal(expected, response.Response);
        }

        [Fact]
        public async Task Chat_WhenConversationBelongsToAnotherUser_ReturnsNotFound()
        {
            var userId = Guid.NewGuid();

            _mockChatService
                .Setup(s => s.GenerateResponseAsync(
                    "Hello",
                    "conv-1",
                    userId,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            var request = new ChatRequest
            {
                Message = "Hello",
                ConversationId = "conv-1",
                UserId = userId
            };

            var result = await _controller.Chat(
                request,
                CancellationToken.None);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Conversation not found.", notFound.Value);
        }
    }
}
