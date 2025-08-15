using SmartRAG.Enums;
using SmartRAG.Models;
using System.Text.Json;

namespace SmartRAG.Providers;

/// <summary>
/// Custom AI provider implementation for custom endpoints
/// </summary>
public class CustomProvider : BaseAIProvider
{
    public override AIProvider ProviderType => AIProvider.Custom;

    public override async Task<string> GenerateTextAsync(string prompt, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: false, requireEndpoint: true, requireModel: true);

        if (!isValid)
            return errorMessage;

        using var client = CreateHttpClient(config.ApiKey);

        bool useMessagesFormat = config.Endpoint!.Contains("/chat") || config.Endpoint.Contains("messages") || config.Endpoint.Contains("completions");

        object payload;

        if (useMessagesFormat)
        {
            payload = new
            {
                model = config.Model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = config.MaxTokens,
                temperature = config.Temperature
            };
        }
        else
        {
            payload = new
            {
                prompt = prompt,
                max_tokens = config.MaxTokens,
                temperature = config.Temperature,
                model = config.Model
            };
        }

        var (success, response, error) = await MakeHttpRequestAsync(client, config.Endpoint!, payload, "Custom AI");

        if (!success)
            return error;

        // Custom AI has flexible response format
        try
        {
            var responseData = JsonSerializer.Deserialize<JsonElement>(response);

            // Try OpenAI-style response format first (for OpenRouter, etc.)
            if (responseData.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
            {
                var firstChoice = choices.EnumerateArray().FirstOrDefault();
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? "No response generated";
                }
            }

            // Try message.content[].text format (some APIs use array-based content)
            if (responseData.TryGetProperty("message", out var messageWithArrayContent) &&
                messageWithArrayContent.TryGetProperty("content", out var contentArray) &&
                contentArray.ValueKind == JsonValueKind.Array)
            {
                var firstContentItem = contentArray.EnumerateArray().FirstOrDefault();
                if (firstContentItem.TryGetProperty("text", out var textContent))
                {
                    return textContent.GetString() ?? "No response generated";
                }
            }

            // Try other common response field names
            if (responseData.TryGetProperty("text", out var text))
                return text.GetString() ?? "No response generated";
            if (responseData.TryGetProperty("response", out var responseText))
                return responseText.GetString() ?? "No response generated";
            if (responseData.TryGetProperty("content", out var contentProp))
                return contentProp.GetString() ?? "No response generated";
            if (responseData.TryGetProperty("message", out var messageProp))
                return messageProp.GetString() ?? "No response generated";
        }
        catch
        {
            return response;
        }

        return "No response generated";
    }

    public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: false, requireEndpoint: true, requireModel: false);

        if (!isValid)
            return [];

        if (string.IsNullOrEmpty(config.EmbeddingModel))
            return [];

        using var client = CreateHttpClient(config.ApiKey);

        var payload = new
        {
            text = text,
            model = config.EmbeddingModel
        };

        var (success, response, error) = await MakeHttpRequestAsync(client, config.Endpoint!, payload, "Custom AI");

        if (!success)
            return [];

        // Custom AI has flexible embedding response format
        try
        {
            var responseData = JsonSerializer.Deserialize<JsonElement>(response);

            // Try different embedding field names
            if (responseData.TryGetProperty("embedding", out var embedding) && embedding.ValueKind == JsonValueKind.Array)
            {
                var floats = new List<float>();

                foreach (var value in embedding.EnumerateArray())
                {
                    if (value.TryGetSingle(out var f))
                        floats.Add(f);
                }

                return floats;
            }

            if (responseData.TryGetProperty("embeddings", out var embeddings) && embeddings.ValueKind == JsonValueKind.Array)
            {
                var firstEmbedding = embeddings.EnumerateArray().FirstOrDefault();

                if (firstEmbedding.ValueKind == JsonValueKind.Array)
                {
                    var floats = new List<float>();

                    foreach (var value in firstEmbedding.EnumerateArray())
                    {
                        if (value.TryGetSingle(out var f))
                            floats.Add(f);
                    }
                    return floats;
                }
            }
        }
        catch
        {
            return [];
        }

        return [];
    }

    /// <summary>
    /// Override ChunkTextAsync for custom endpoint optimization
    /// Uses string concatenation for better compatibility with various APIs
    /// </summary>
    public override Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(new List<string>());

        var chunks = new List<string>();

        var sentences = text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = "";

        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();

            if (string.IsNullOrEmpty(trimmedSentence))
                continue;

            if (currentChunk.Length + trimmedSentence.Length > maxChunkSize && !string.IsNullOrEmpty(currentChunk))
            {
                chunks.Add(currentChunk.Trim());
                currentChunk = trimmedSentence + ".";
            }
            else
            {
                currentChunk += (currentChunk.Length > 0 ? " " : "") + trimmedSentence + ".";
            }
        }

        if (!string.IsNullOrEmpty(currentChunk))
        {
            chunks.Add(currentChunk.Trim());
        }

        return Task.FromResult(chunks);
    }
}
