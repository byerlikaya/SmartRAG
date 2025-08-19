using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System.Text;
using System.Text.Json;

namespace SmartRAG.Providers;

/// <summary>
/// Base class for AI providers with common implementations
/// </summary>
public abstract class BaseAIProvider : IAIProvider
{
    public abstract AIProvider ProviderType { get; }

    public abstract Task<string> GenerateTextAsync(string prompt, AIProviderConfig config);

    public abstract Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config);

    /// <summary>
    /// Generates embeddings for multiple texts in batch
    /// </summary>
    public abstract Task<List<List<float>>?> GenerateEmbeddingsBatchAsync(List<string> texts, AIProviderConfig config);

    /// <summary>
    /// Common text chunking implementation for all providers
    /// Uses StringBuilder for better performance
    /// </summary>
    public virtual Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(new List<string>());

        var chunks = new List<string>();

        var sentences = text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);

        var current = new StringBuilder();

        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();

            if (string.IsNullOrEmpty(trimmedSentence))
                continue;

            if (current.Length + trimmedSentence.Length + 1 > maxChunkSize && current.Length > 0)
            {
                chunks.Add(current.ToString().Trim());
                current.Clear();
            }

            if (current.Length > 0)
                current.Append(' ');

            current.Append(trimmedSentence).Append('.');
        }

        if (current.Length > 0)
            chunks.Add(current.ToString().Trim());

        return Task.FromResult(chunks);
    }

    #region Common Helper Methods

    /// <summary>
    /// Validates common configuration requirements
    /// </summary>
    protected (bool isValid, string errorMessage) ValidateConfig(AIProviderConfig config, bool requireApiKey = true, bool requireEndpoint = true, bool requireModel = true)
    {
        if (requireApiKey && string.IsNullOrEmpty(config.ApiKey))
            return (false, $"{ProviderType} API key missing. Configure {ProviderType}:ApiKey.");

        if (requireEndpoint && string.IsNullOrEmpty(config.Endpoint))
            return (false, $"{ProviderType} endpoint missing. Configure {ProviderType}:Endpoint.");

        if (requireModel && string.IsNullOrEmpty(config.Model))
            return (false, $"{ProviderType} model missing. Configure {ProviderType}:Model.");

        return (true, string.Empty);
    }

    /// <summary>
    /// Creates HttpClient with common headers
    /// </summary>
    protected HttpClient CreateHttpClient(string? apiKey = null, Dictionary<string, string>? additionalHeaders = null)
    {
        var client = new HttpClient();

        if (!string.IsNullOrEmpty(apiKey))
        {
            // Determine auth header type based on provider
            var authHeader = ProviderType switch
            {
                AIProvider.OpenAI or AIProvider.AzureOpenAI => "Authorization",
                AIProvider.Anthropic => "x-api-key",
                AIProvider.Gemini => "x-goog-api-key",
                _ => "Authorization"
            };

            var authValue = ProviderType switch
            {
                AIProvider.OpenAI or AIProvider.AzureOpenAI => $"Bearer {apiKey}",
                AIProvider.Anthropic => apiKey,
                AIProvider.Gemini => apiKey,
                _ => $"Bearer {apiKey}"
            };

            client.DefaultRequestHeaders.Add(authHeader, authValue);
        }

        if (additionalHeaders != null)
        {
            foreach (var header in additionalHeaders)
            {
                if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return client;
    }

    /// <summary>
    /// Creates HTTP client without automatic Authorization header (for providers like Anthropic that use custom headers)
    /// </summary>
    protected static HttpClient CreateHttpClientWithoutAuth(Dictionary<string, string>? additionalHeaders)
    {
        var client = new HttpClient();

        if (additionalHeaders != null)
        {
            foreach (var header in additionalHeaders)
            {
                if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return client;
    }

    /// <summary>
    /// Common HTTP POST request with error handling
    /// </summary>
    protected static async Task<(bool success, string response, string errorMessage)> MakeHttpRequestAsync(
        HttpClient client, string endpoint, object payload, string providerName)
    {
        try
        {
            var options = GetJsonSerializerOptions();
            var json = JsonSerializer.Serialize(payload, options);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint, content);

            if (!response.IsSuccessStatusCode)
                return (false, string.Empty, $"{providerName} error: {response.StatusCode}");

            var responseBody = await response.Content.ReadAsStringAsync();

            return (true, responseBody, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"{providerName} request failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Common embedding response parsing
    /// </summary>
    protected static List<float> ParseEmbeddingResponse(string responseBody, string dataProperty = "data", string embeddingProperty = "embedding")
    {
        using var doc = JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty(dataProperty, out var data) && data.ValueKind == JsonValueKind.Array)
        {
            var firstData = data.EnumerateArray().FirstOrDefault();

            if (firstData.TryGetProperty(embeddingProperty, out var embedding) && embedding.ValueKind == JsonValueKind.Array)
            {
                var floats = new List<float>();

                foreach (var value in embedding.EnumerateArray())
                {
                    if (value.TryGetSingle(out var f))
                        floats.Add(f);
                }

                return floats;
            }
        }

        return [];
    }

    /// <summary>
    /// Common text generation response parsing for OpenAI-like APIs
    /// </summary>
    protected static string ParseTextResponse(string responseBody, string choicesProperty = "choices", string messageProperty = "message", string contentProperty = "content")
    {
        using var doc = JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty(choicesProperty, out var choices) && choices.ValueKind == JsonValueKind.Array)
        {
            var firstChoice = choices.EnumerateArray().FirstOrDefault();

            if (firstChoice.TryGetProperty(messageProperty, out var message) && message.TryGetProperty(contentProperty, out var contentProp))
                return contentProp.GetString() ?? "No response generated";
        }

        return "No response generated";
    }

    /// <summary>
    /// Shared JsonSerializerOptions for consistent serialization
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Get shared JsonSerializerOptions instance
    /// </summary>
    private static JsonSerializerOptions GetJsonSerializerOptions() => _jsonOptions;

    #endregion
}
