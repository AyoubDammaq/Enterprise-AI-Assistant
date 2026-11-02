using System.Net;
using System.Text;
using EnterpriseAIAssistant.Infrastructure.Retrieval;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

public sealed class OllamaEmbeddingServiceReliabilityTests
{
    [Fact]
    public async Task GenerateEmbeddingAsync_HttpFailure_Throws()
    {
        using var client = CreateClient(_ =>
            Task.FromResult(new HttpResponseMessage(
                HttpStatusCode.ServiceUnavailable)));

        var service = CreateService(client);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GenerateEmbeddingAsync("hello"));

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            exception.StatusCode);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_InvalidJson_Throws()
    {
        using var client = CreateClient(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{ invalid json",
                    Encoding.UTF8,
                    "application/json")
            }));

        var service = CreateService(client);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateEmbeddingAsync("hello"));

        Assert.Contains("invalid JSON", exception.Message);
        Assert.IsType<System.Text.Json.JsonException>(
            exception.InnerException);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_EmptyEmbeddingResponse_Throws()
    {
        using var client = CreateClient(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"model":"test","embeddings":[]}""",
                    Encoding.UTF8,
                    "application/json")
            }));

        var service = CreateService(client);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateEmbeddingAsync("hello"));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_NullPayload_Throws()
    {
        using var client = CreateClient(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "null",
                    Encoding.UTF8,
                    "application/json")
            }));

        var service = CreateService(client);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateEmbeddingAsync("hello"));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_CancelledRequest_PropagatesCancellation()
    {
        using var client = CreateClient(async cancellationToken =>
        {
            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var service = CreateService(client);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GenerateEmbeddingAsync(
                "hello",
                cancellation.Token));
    }

    private static OllamaEmbeddingService CreateService(HttpClient client)
    {
        return new OllamaEmbeddingService(
            client,
            Options.Create(new OllamaEmbeddingOptions
            {
                Endpoint = "http://localhost:11434",
                EmbeddingModel = "nomic-embed-text",
                EmbeddingDimensions = 2
            }));
    }

    private static HttpClient CreateClient(
        Func<CancellationToken, Task<HttpResponseMessage>> response)
    {
        return new HttpClient(new StubHttpMessageHandler(response));
    }

    private sealed class StubHttpMessageHandler(
        Func<CancellationToken, Task<HttpResponseMessage>> response)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return response(cancellationToken);
        }
    }
}