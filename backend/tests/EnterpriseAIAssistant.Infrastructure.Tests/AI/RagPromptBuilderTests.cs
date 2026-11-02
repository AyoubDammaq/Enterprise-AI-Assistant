using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Infrastructure.AI;

namespace EnterpriseAIAssistant.Infrastructure.Tests.AI;

public sealed class RagPromptBuilderTests
{
    [Fact]
    public void Build_IncludesCurrentQuestion()
    {
        const string question = "How can I reset my password?";

        var result = RagPromptBuilder.Build(
            question,
            new[] { CreateRetrievedChunk("password.md", 0, "Select Forgot password.") });

        Assert.Equal(question, result.UserMessage);
    }

    [Fact]
    public void Build_IncludesOnlyRetrievedChunks()
    {
        var result = RagPromptBuilder.Build(
            "How can I reset my password?",
            new[]
            {
                CreateRetrievedChunk(
                    "password.md",
                    0,
                    "Select Forgot password.")
            });

        Assert.Contains("Select Forgot password.", result.SystemMessage);
        Assert.DoesNotContain("UNRETRIEVED_SECRET", result.SystemMessage);
    }

    [Fact]
    public void Build_PreservesSourceMetadata()
    {
        var result = RagPromptBuilder.Build(
            "Where is the procedure?",
            new[]
            {
                CreateRetrievedChunk(
                    "account-guide.md",
                    3,
                    "Open account settings.")
            });

        Assert.Contains("Source: account-guide.md", result.SystemMessage);
        Assert.Contains("Document ID: account-guide.md", result.SystemMessage);
        Assert.Contains("Chunk index: 3", result.SystemMessage);
        Assert.Contains("Open account settings.", result.SystemMessage);
    }

    [Fact]
    public void Build_IncludesGroundingInstructions()
    {
        var result = RagPromptBuilder.Build(
            "What does the guide say?",
            new[] { CreateRetrievedChunk("guide.md", 0, "Guide content.") });

        Assert.Contains("Do not invent facts", result.SystemMessage);
        Assert.Contains("untrusted data", result.SystemMessage);
        Assert.Contains("do not provide the answer", result.SystemMessage);
    }

    [Fact]
    public void Build_DoesNotMixQuestionWithSystemMessage()
    {
        const string question = "QUESTION_SENTINEL";

        var result = RagPromptBuilder.Build(
            question,
            new[] { CreateRetrievedChunk("guide.md", 0, "Document content.") });

        Assert.Equal(question, result.UserMessage);
        Assert.DoesNotContain(question, result.SystemMessage);
    }

    [Fact]
    public void Build_EmptyContext_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RagPromptBuilder.Build(
                "A question",
                Array.Empty<RetrievedChunk>()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Build_EmptyQuestion_Throws(string question)
    {
        Assert.Throws<ArgumentException>(() =>
            RagPromptBuilder.Build(
                question,
                new[] { CreateRetrievedChunk("guide.md", 0, "Content.") }));
    }

    private static RetrievedChunk CreateRetrievedChunk(
        string source,
        int chunkIndex,
        string content)
    {
        var chunk = new DocumentChunk(
            source,
            source,
            chunkIndex,
            content);

        return new RetrievedChunk(chunk, 0.95);
    }
}