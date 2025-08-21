using SmartRAG.Enums;
using SmartRAG.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Linq;

namespace SmartRAG.Providers;

/// <summary>
/// OpenAI provider implementation
/// </summary>
public class OpenAIProvider(ILogger<OpenAIProvider> logger) : BaseAIProvider(logger)
{
    #region Constants

    // OpenAI API constants
    private const string ChatCompletionsPath = "chat/completions";
    private const string EmbeddingsPath = "embeddings";
    private const string SystemRole = "system";
    private const string UserRole = "user";

    #endregion

    #region Properties

    public override AIProvider ProviderType => AIProvider.OpenAI;

    #endregion

    #region Public Methods

    public override async Task<string> GenerateTextAsync(string prompt, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config);

        if (!isValid)
            return errorMessage;

        using var client = CreateHttpClient(config.ApiKey);

        var payload = CreateOpenAITextPayload(prompt, config);

        var chatEndpoint = BuildOpenAIUrl(config.Endpoint!, ChatCompletionsPath);

        var (success, response, error) = await MakeHttpRequestAsync(client, chatEndpoint, payload);

        if (!success)
            return error;

        try
        {
            return ParseOpenAITextResponse(response);
        }
        catch (Exception ex)
        {
            ProviderLogMessages.LogOpenAITextParsingError(_logger, ex);
            return $"Error parsing OpenAI response: {ex.Message}";
        }
    }

    public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid)
        {
            ProviderLogMessages.LogOpenAIEmbeddingValidationError(_logger, errorMessage, null);
            return [];
        }

        if (string.IsNullOrEmpty(config.EmbeddingModel))
        {
            ProviderLogMessages.LogOpenAIEmbeddingModelMissing(_logger, null);
            return [];
        }

        using var client = CreateHttpClient(config.ApiKey);

        var payload = CreateOpenAIEmbeddingPayload(text, config.EmbeddingModel);

        var embeddingEndpoint = BuildOpenAIUrl(config.Endpoint!, EmbeddingsPath);

        var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint, payload);

        if (!success)
        {
            ProviderLogMessages.LogOpenAIEmbeddingRequestError(_logger, error, null);
            return [];
        }

        try
        {
            return ParseOpenAIEmbeddingResponse(response);
        }
        catch (Exception ex)
        {
            ProviderLogMessages.LogOpenAIEmbeddingParsingError(_logger, ex);
            return [];
        }
    }

    public override async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid)
        {
            ProviderLogMessages.LogOpenAIEmbeddingValidationError(_logger, errorMessage, null);
            return [];
        }

        if (string.IsNullOrEmpty(config.EmbeddingModel))
        {
            ProviderLogMessages.LogOpenAIEmbeddingModelMissing(_logger, null);
            return [];
        }

        var inputList = texts?.ToList() ?? new List<string>();
        if (inputList.Count == 0)
            return [];

        using var client = CreateHttpClient(config.ApiKey);

        var payload = CreateOpenAIBatchEmbeddingPayload(inputList, config.EmbeddingModel);

        var embeddingEndpoint = BuildOpenAIUrl(config.Endpoint!, EmbeddingsPath);

        var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint, payload);

        if (!success)
        {
            ProviderLogMessages.LogOpenAIBatchEmbeddingRequestError(_logger, error, null);
            return ParseBatchEmbeddingResponse("", inputList.Count);
        }

        try
        {
            return ParseOpenAIBatchEmbeddingResponse(response, inputList.Count);
        }
        catch (Exception ex)
        {
            ProviderLogMessages.LogOpenAIBatchEmbeddingParsingError(_logger, ex);
            return ParseBatchEmbeddingResponse("", inputList.Count);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Build OpenAI API URL
    /// </summary>
    private static string BuildOpenAIUrl(string endpoint, string path)
    {
        return $"{endpoint.TrimEnd('/')}/{path}";
    }

    /// <summary>
    /// Create OpenAI text generation payload
    /// </summary>
    private static object CreateOpenAITextPayload(string prompt, AIProviderConfig config)
    {
        var messages = new List<object>();
        
        if (!string.IsNullOrEmpty(config.SystemMessage))
        {
            messages.Add(new { role = SystemRole, content = config.SystemMessage });
        }
        
        messages.Add(new { role = UserRole, content = prompt });

        return new
        {
            messages = messages.ToArray(),
            model = config.Model,
            max_tokens = config.MaxTokens,
            temperature = config.Temperature,
            stream = false
        };
    }

    /// <summary>
    /// Create OpenAI embedding payload
    /// </summary>
    private static object CreateOpenAIEmbeddingPayload(string text, string model)
    {
        return new
        {
            input = text,
            model = model
        };
    }

    /// <summary>
    /// Create OpenAI batch embedding payload
    /// </summary>
    private static object CreateOpenAIBatchEmbeddingPayload(List<string> texts, string model)
    {
        return new
        {
            input = texts.ToArray(),
            model = model
        };
    }

    /// <summary>
    /// Parse OpenAI text response
    /// </summary>
    private static string ParseOpenAITextResponse(string response)
    {
        using var doc = JsonDocument.Parse(response);

        if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
        {
            var firstChoice = choices.EnumerateArray().FirstOrDefault();

            if (firstChoice.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? "No response generated";
            }
        }

        return "No response generated";
    }

    /// <summary>
    /// Parse OpenAI embedding response
    /// </summary>
    private static List<float> ParseOpenAIEmbeddingResponse(string response)
    {
        using var doc = JsonDocument.Parse(response);

        if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            var firstItem = data.EnumerateArray().FirstOrDefault();

            if (firstItem.TryGetProperty("embedding", out var embedding) && embedding.ValueKind == JsonValueKind.Array)
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
    /// Parse OpenAI batch embedding response
    /// </summary>
    private static List<List<float>> ParseOpenAIBatchEmbeddingResponse(string response, int expectedCount)
    {
        var results = new List<List<float>>();

        try
        {
            using var doc = JsonDocument.Parse(response);

            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in data.EnumerateArray())
                {
                    if (item.TryGetProperty("embedding", out var embedding) && embedding.ValueKind == JsonValueKind.Array)
                    {
                        var floats = new List<float>();
                        foreach (var value in embedding.EnumerateArray())
                        {
                            if (value.TryGetSingle(out var f))
                                floats.Add(f);
                        }
                        results.Add(floats);
                    }
                    else
                    {
                        results.Add([]);
                    }
                }
            }
        }
        catch
        {
            // Return partial results on error
        }

        // Ensure we return exactly expectedCount embeddings
        return Enumerable.Range(0, expectedCount)
            .Select(i => i < results.Count ? results[i] : new List<float>())
            .ToList();
    }

    #endregion
}