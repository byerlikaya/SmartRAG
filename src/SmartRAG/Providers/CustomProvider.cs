using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Providers;

/// <summary>
/// Custom AI provider implementation for custom endpoints
/// </summary>
public class CustomProvider(ILogger<CustomProvider> logger) : BaseAIProvider(logger)
{
    #region Constants

    // Custom provider constants
    private const int DefaultMaxChunkSize = 1000;
    private const string UserRole = "user";
    private const string SystemRole = "system";

    #endregion

    #region Properties

    public override AIProvider ProviderType => AIProvider.Custom;

    #endregion

    #region Public Methods

    public override async Task<string> GenerateTextAsync(string prompt, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: false, requireEndpoint: true, requireModel: true);

        if (!isValid)
            return errorMessage;

        using var client = CreateHttpClient(config.ApiKey);

        bool useMessagesFormat = IsMessagesFormat(config.Endpoint!);

        object payload = CreatePayload(prompt, config, useMessagesFormat);

        var (success, response, error) = await MakeHttpRequestAsync(client, config.Endpoint!, payload);

        if (!success)
            return error;

        try
        {
            return ParseCustomTextResponse(response);
        }
        catch (Exception ex)
        {
            ProviderLogMessages.LogCustomTextParsingError(Logger, ex);
            return $"Error parsing custom response: {ex.Message}";
        }
    }

    public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: false, requireEndpoint: true, requireModel: false);

        if (!isValid)
        {
            ProviderLogMessages.LogCustomEmbeddingValidationError(Logger, errorMessage, null);
            return [];
        }

        if (string.IsNullOrEmpty(config.EmbeddingModel))
        {
            ProviderLogMessages.LogCustomEmbeddingModelMissing(Logger, null);
            return [];
        }

        using var client = CreateHttpClient(config.ApiKey);

        var payload = new
        {
            text = text,
            model = config.EmbeddingModel
        };

        var (success, response, error) = await MakeHttpRequestAsync(client, config.Endpoint!, payload);

        if (!success)
        {
            ProviderLogMessages.LogCustomEmbeddingRequestError(Logger, error, null);
            return [];
        }

        try
        {
            return ParseCustomEmbeddingResponse(response);
        }
        catch (Exception ex)
        {
            ProviderLogMessages.LogCustomEmbeddingParsingError(Logger, ex);
            return [];
        }
    }

    /// <summary>
    /// Override ChunkTextAsync for custom endpoint optimization
    /// Uses string concatenation for better compatibility with various APIs
    /// </summary>
    public override Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = DefaultMaxChunkSize)
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

    #endregion

    #region Private Methods

    /// <summary>
    /// Determine if endpoint uses messages format
    /// </summary>
    private static bool IsMessagesFormat(string endpoint)
    {
        return endpoint.Contains("/chat") || endpoint.Contains("messages") || endpoint.Contains("completions");
    }

    /// <summary>
    /// Create appropriate payload based on format
    /// </summary>
    private static object CreatePayload(string prompt, AIProviderConfig config, bool useMessagesFormat)
    {
        if (useMessagesFormat)
        {
            var messages = new List<object>();

            if (!string.IsNullOrEmpty(config.SystemMessage))
            {
                messages.Add(new { role = SystemRole, content = config.SystemMessage });
            }

            messages.Add(new { role = UserRole, content = prompt });

            return new
            {
                model = config.Model,
                messages = messages.ToArray(),
                max_tokens = config.MaxTokens,
                temperature = config.Temperature
            };
        }
        else
        {
            return new
            {
                prompt = prompt,
                max_tokens = config.MaxTokens,
                temperature = config.Temperature,
                model = config.Model
            };
        }
    }

    /// <summary>
    /// Parse custom AI text response with flexible format support
    /// </summary>
    private static string ParseCustomTextResponse(string response)
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

        return "No response generated";
    }

    /// <summary>
    /// Parse custom AI embedding response with flexible format support
    /// </summary>
    private static List<float> ParseCustomEmbeddingResponse(string response)
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

        return [];
    }

    #endregion
}
