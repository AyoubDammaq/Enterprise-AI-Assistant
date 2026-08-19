using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.Application.Tests.DTOs;

public class ChatRequestTests
{
    [Fact]
    public void ChatRequest_DefaultMessage_IsEmptyString()
    {
        // Arrange & Act
        var request = new ChatRequest();

        // Assert
        Assert.Equal(string.Empty, request.Message);
    }

    [Fact]
    public void ChatRequest_SetMessage_ReturnsCorrectValue()
    {
        // Arrange
        const string expected = "Test message";

        // Act
        var request = new ChatRequest { Message = expected };

        // Assert
        Assert.Equal(expected, request.Message);
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("What is the revenue for Q3?")]
    [InlineData("Summarize the last quarterly report.")]
    public void ChatRequest_WithVariousMessages_StoresCorrectly(string message)
    {
        // Arrange & Act
        var request = new ChatRequest { Message = message };

        // Assert
        Assert.Equal(message, request.Message);
    }
}