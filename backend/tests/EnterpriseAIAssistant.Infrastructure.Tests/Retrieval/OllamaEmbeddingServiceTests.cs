using System.Net;
using System.Net.Http.Json;
using EnterpriseAIAssistant.Infrastructure.Retrieval;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

public sealed class OllamaEmbeddingServiceTests
{
    [Fact]
    public async Task GenerateEmbeddingAsync_PostsToApiEmbedAndReturnsVector()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    model = "nomic-embed-text",
                    embeddings = new[] { new[] { 0.25f, 0.75f } }
                })
            };
        });
        using var client = new HttpClient(handler);
        var service = new OllamaEmbeddingService(client, Options.Create(new OllamaEmbeddingOptions
        {
            Endpoint = "http://localhost:11434",
            EmbeddingModel = "nomic-embed-text",
            EmbeddingDimensions = 2
        }));

        var result = await service.GenerateEmbeddingAsync("test text");

        Assert.Equal(new[] { 0.25f, 0.75f }, result);
        Assert.NotNull(capturedRequest);
        Assert.Equal("http://localhost:11434/api/embed", capturedRequest!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WrongDimension_ThrowsHelpfulError()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { embeddings = new[] { new[] { 1f, 2f, 3f } } })
        });
        using var client = new HttpClient(handler);
        var service = new OllamaEmbeddingService(client, Options.Create(new OllamaEmbeddingOptions
        {
            Endpoint = "http://localhost:11434",
            EmbeddingModel = "nomic-embed-text",
            EmbeddingDimensions = 2
        }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateEmbeddingAsync("test"));

        Assert.Contains("Expected 2", exception.Message);
        Assert.Contains("received 3", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GenerateEmbeddingAsync_EmptyText_Throws(string text)
    {
        using var client = new HttpClient(new StubHttpMessageHandler(_ =>
            throw new InvalidOperationException("The handler should not be called.")));
        var service = new OllamaEmbeddingService(client, Options.Create(new OllamaEmbeddingOptions
        {
            Endpoint = "http://localhost:11434",
            EmbeddingModel = "nomic-embed-text",
            EmbeddingDimensions = 2
        }));

        await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateEmbeddingAsync(text));
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
