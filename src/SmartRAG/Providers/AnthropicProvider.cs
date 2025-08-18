using SmartRAG.Enums;
using SmartRAG.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmartRAG.Providers;

/// <summary>
/// Anthropic Claude provider implementation
/// </summary>
public class AnthropicProvider : BaseAIProvider
{
    public AnthropicProvider(ILogger<AnthropicProvider> logger) : base(logger)
    {
    }

    public override AIProvider ProviderType => AIProvider.Anthropic;

    public override async Task<string> GenerateTextAsync(string prompt, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config);

        if (!isValid)
            return errorMessage;

        var additionalHeaders = new Dictionary<string, string>
        {
            { "x-api-key", config.ApiKey },
            { "anthropic-version", "2023-06-01" }
        };

        using var client = CreateHttpClientWithoutAuth(additionalHeaders);

        var payload = new
        {
            model = config.Model,
            max_tokens = config.MaxTokens,
            temperature = config.Temperature,
            system = "You are a helpful AI that answers strictly using the provided context. If the context is insufficient, say so.",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        var chatEndpoint = $"{config.Endpoint!.TrimEnd('/')}/v1/messages";

        var (success, response, error) = await MakeHttpRequestAsync(client, chatEndpoint, payload, "Anthropic");

        if (!success)
            return error;

        try
        {
            using var doc = JsonDocument.Parse(response);

            if (doc.RootElement.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
            {
                var firstContent = contentArray.EnumerateArray().FirstOrDefault();

                if (firstContent.TryGetProperty("text", out var text))
                    return text.GetString() ?? "No response generated";
            }
        }
        catch (Exception ex)
        {
            return $"Error parsing response: {ex.Message}";
        }

        return "No response generated";
    }

    public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
    {
        // Anthropic uses Voyage AI for embeddings as per their documentation
        // https://docs.anthropic.com/en/docs/build-with-claude/embeddings#how-to-get-embeddings-with-anthropic

        var voyageApiKey = config.EmbeddingApiKey ?? config.ApiKey; // Separate key for Voyage if needed
        var voyageEndpoint = "https://api.voyageai.com"; // Voyage AI endpoint
        var voyageModel = config.EmbeddingModel ?? "voyage-3.5"; // Default model

        if (string.IsNullOrEmpty(voyageApiKey))
            return [];

        var additionalHeaders = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {voyageApiKey}" }
        };

        using var client = CreateHttpClientWithoutAuth(additionalHeaders);

        var payload = new
        {
            input = new[] { text },
            model = voyageModel,
            input_type = "document" // For RAG documents
        };

        var embeddingEndpoint = $"{voyageEndpoint}/v1/embeddings";

        var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint, payload, "Voyage (Anthropic)");

        if (!success)
            return [];

        return ParseVoyageEmbeddingResponse(response);
    }

    private static List<float> ParseVoyageEmbeddingResponse(string response)
    {
        using var doc = JsonDocument.Parse(response);

        if (doc.RootElement.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
        {
            var firstEmbedding = dataArray.EnumerateArray().FirstOrDefault();

            if (firstEmbedding.TryGetProperty("embedding", out var embeddingArray) && embeddingArray.ValueKind == JsonValueKind.Array)
            {
                return embeddingArray.EnumerateArray()
                    .Select(x => x.GetSingle())
                    .ToList();
            }
        }

        return [];
    }
}