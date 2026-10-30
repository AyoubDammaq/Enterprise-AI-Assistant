using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EnterpriseAIAssistant.API.Controllers;
using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EnterpriseAIAssistant.API.Tests
{
    public class ChatControllerTests
    {
        private readonly Mock<IAIChatService> _mockChatService;
        private readonly Mock<IRagService> _mockRagService;
        private readonly ChatController _controller;

        public ChatControllerTests()
        {
            _mockChatService = new Mock<IAIChatService>();
            _mockRagService = new Mock<IRagService>();
            _controller = new ChatController(_mockChatService.Object, _mockRagService.Object);
        }

        private void AuthenticateAs(Guid userId)
        {
            var claims = new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }

        [Fact]
        public async Task Chat_WithValidMessage_ReturnsOkWithResponse()
        {
            // Arrange
            const string userMessage = "What is AI?";
            const string aiResponse =
                "AI stands for Artificial Intelligence.";

            var userId = Guid.NewGuid();
            AuthenticateAs(userId);

            _mockChatService
                .Setup(s => s.GenerateResponseAsync(
                    userMessage,
                    It.IsAny<string>(),
                    userId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            var request = new ChatRequest
            {
                Message = userMessage
            };

            // Act
            var result = await _controller.Chat(
                request,
                CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);

            Assert.Equal(aiResponse, response.Response);
            Assert.False(string.IsNullOrWhiteSpace(response.ConversationId));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null!)]
        public async Task Chat_WithEmptyOrWhitespaceMessage_ReturnsBadRequest(
            string? message)
        {
            // Arrange
            AuthenticateAs(Guid.NewGuid());

            var request = new ChatRequest
            {
                Message = message!
            };

            // Act
            var result = await _controller.Chat(
                request,
                CancellationToken.None);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(
                "Message cannot be empty.",
                badRequest.Value);

            _mockChatService.Verify(
                s => s.GenerateResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Chat_WithValidMessage_CallsServiceExactlyOnce()
        {
            // Arrange
            const string userMessage = "Hello AI";
            var userId = Guid.NewGuid();

            AuthenticateAs(userId);

            _mockChatService
                .Setup(s => s.GenerateResponseAsync(
                    userMessage,
                    It.IsAny<string>(),
                    userId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("Hello!");

            var request = new ChatRequest
            {
                Message = userMessage
            };

            // Act
            await _controller.Chat(request, CancellationToken.None);

            // Assert
            _mockChatService.Verify(
                s => s.GenerateResponseAsync(
                    userMessage,
                    It.IsAny<string>(),
                    userId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Chat_WhenServiceThrows_PropagatesException()
        {
            // Arrange
            AuthenticateAs(Guid.NewGuid());

            _mockChatService
                .Setup(s => s.GenerateResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new InvalidOperationException(
                        "AI service unavailable."));

            var request = new ChatRequest
            {
                Message = "Hello"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _controller.Chat(
                    request,
                    CancellationToken.None));
        }

        [Fact]
        public async Task Chat_WithValidMessage_ResponseContainsAIContent()
        {
            // Arrange
            const string expected = "Enterprise AI answer.";

            AuthenticateAs(Guid.NewGuid());

            _mockChatService
                .Setup(s => s.GenerateResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var request = new ChatRequest
            {
                Message = "Tell me something."
            };

            // Act
            var result = await _controller.Chat(
                request,
                CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);

            Assert.False(string.IsNullOrWhiteSpace(response.Response));
            Assert.Equal(expected, response.Response);
        }

        [Fact]
        public async Task Chat_WhenConversationBelongsToAnotherUser_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            AuthenticateAs(userId);

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
                ConversationId = "conv-1"
            };

            // Act
            var result = await _controller.Chat(
                request,
                CancellationToken.None);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);

            Assert.Equal(
                "Conversation not found.",
                notFound.Value);
        }

        [Fact]
        public async Task Chat_UsesUserIdFromClaims_NotFromRequestBody()
        {
            // Arrange
            var authenticatedUserId = Guid.NewGuid();
            AuthenticateAs(authenticatedUserId);

            _mockChatService
                .Setup(s => s.GenerateResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    authenticatedUserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("Authenticated user response.");

            var request = new ChatRequest
            {
                Message = "Hello"
            };

            // Act
            var result = await _controller.Chat(
                request,
                CancellationToken.None);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _mockChatService.Verify(
                s => s.GenerateResponseAsync(
                    "Hello",
                    It.IsAny<string>(),
                    authenticatedUserId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Chat_WithoutValidSubClaim_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[]
                            {
                                new Claim(
                                    JwtRegisteredClaimNames.Sub,
                                    "not-a-guid")
                            },
                            "TestAuth"))
                }
            };

            var request = new ChatRequest
            {
                Message = "Hello"
            };

            // Act
            var result = await _controller.Chat(
                request,
                CancellationToken.None);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);

            _mockChatService.Verify(
                s => s.GenerateResponseAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
