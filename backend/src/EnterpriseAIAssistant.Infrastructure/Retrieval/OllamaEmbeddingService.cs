using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Retrieval;

/// <summary>Calls Ollama's POST /api/embed endpoint to generate a single text embedding.</summary>
public sealed class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaEmbeddingOptions _options;
    private readonly Uri _embedUri;

    public OllamaEmbeddingService(
        HttpClient httpClient,
        IOptions<OllamaEmbeddingOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        ArgumentException.ThrowIfNullOrWhiteSpace(_options.Endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.EmbeddingModel);

        if (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "Ollama:Endpoint must be an absolute HTTP or HTTPS URL.",
                nameof(options));
        }

        if (_options.EmbeddingDimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(_options.EmbeddingDimensions),
                "Embedding dimensions must be greater than zero.");
        }

        _embedUri = new Uri(endpoint.ToString().TrimEnd('/') + "/api/embed");
    }

    public async Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        cancellationToken.ThrowIfCancellationRequested();

        using var response = await _httpClient.PostAsJsonAsync(
            _embedUri,
            new OllamaEmbedRequest(_options.EmbeddingModel, text),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Ollama embedding request failed with HTTP {(int)response.StatusCode} " +
                $"({response.StatusCode}). Response: {errorBody}",
                inner: null,
                response.StatusCode);
        }

        OllamaEmbedResponse? payload;
        try
        {
            payload = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(
                cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Ollama returned an invalid JSON response for /api/embed.", ex);
        }

        var embedding = payload?.Embeddings?.FirstOrDefault();
        if (embedding is null || embedding.Length == 0)
        {
            throw new InvalidOperationException(
                "Ollama's /api/embed response did not contain an embedding.");
        }

        if (embedding.Length != _options.EmbeddingDimensions)
        {
            throw new InvalidOperationException(
                $"Embedding dimension mismatch. Expected {_options.EmbeddingDimensions}, " +
                $"received {embedding.Length}. Check Ollama:EmbeddingModel and " +
                "Ollama:EmbeddingDimensions.");
        }

        if (embedding.Any(value => !float.IsFinite(value)))
        {
            throw new InvalidOperationException(
                "Ollama returned an embedding containing a non-finite value.");
        }

        return embedding;
    }

    private sealed record OllamaEmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input);

    private sealed record OllamaEmbedResponse(
        [property: JsonPropertyName("embeddings")] float[][]? Embeddings);
}
