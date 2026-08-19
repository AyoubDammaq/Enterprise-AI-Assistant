using EnterpriseAIAssistant.Application.DTOs;

namespace EnterpriseAIAssistant.Application.Tests.DTOs;

public class ChatResponseTests
{
    [Fact]
    public void ChatResponse_Constructor_SetsResponseProperty()
    {
        // Arrange
        const string expected = "This is the AI response.";

        // Act
        var response = new ChatResponse(expected);

        // Assert
        Assert.Equal(expected, response.Response);
    }

    [Fact]
    public void ChatResponse_WithEmptyString_SetsEmptyResponse()
    {
        // Act
        var response = new ChatResponse(string.Empty);

        // Assert
        Assert.Equal(string.Empty, response.Response);
    }

    [Theory]
    [InlineData("Short reply.")]
    [InlineData("A longer enterprise-grade AI response with technical content.")]
    public void ChatResponse_WithVariousContent_StoresCorrectly(string content)
    {
        // Act
        var response = new ChatResponse(content);

        // Assert
        Assert.Equal(content, response.Response);
    }
}