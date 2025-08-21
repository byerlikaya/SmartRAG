using SmartRAG.Enums;
using SmartRAG.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmartRAG.Providers;

/// <summary>
/// Google Gemini AI provider implementation
/// </summary>
public class GeminiProvider(ILogger<GeminiProvider> logger) : BaseAIProvider(logger)
{
    #region Constants

    // Gemini API constants
    private const string GeminiApiKeyHeader = "x-goog-api-key";
    private const int DefaultMaxBatchSize = 50;
    private const int DefaultDelayBetweenBatchesMs = 1000;

    #endregion

    #region Properties

    public override AIProvider ProviderType => AIProvider.Gemini;

    #endregion

    #region Public Methods

    public override async Task<string> GenerateTextAsync(string prompt, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config);

        if (!isValid)
            return errorMessage;

        using var client = CreateGeminiHttpClient(config.ApiKey);

        var payload = CreateGeminiTextPayload(prompt, config);

        var modelEndpoint = BuildGeminiUrl(config.Endpoint!, config.Model!, "generateContent");

        var (success, response, error) = await MakeHttpRequestAsync(client, modelEndpoint, payload);

        if (!success)
            return error;

        try
        {
            return ParseGeminiTextResponse(response);
        }
        catch (Exception ex)
        {
            ProviderLogMessages.LogGeminiTextParsingError(_logger, ex);
            return $"Error parsing Gemini response: {ex.Message}";
        }
    }

    public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid)
        {
            ProviderLogMessages.LogGeminiEmbeddingValidationError(_logger, errorMessage, null);
            return [];
        }

        if (string.IsNullOrEmpty(config.EmbeddingModel))
        {
            ProviderLogMessages.LogGeminiEmbeddingModelMissing(_logger, null);
            return [];
        }

        using var client = CreateGeminiHttpClient(config.ApiKey);

        var payload = CreateGeminiEmbeddingPayload(text, config.EmbeddingModel);

        var embeddingEndpoint = BuildGeminiUrl(config.Endpoint!, config.EmbeddingModel, "embedContent");

        var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint, payload);

        if (!success)
        {
            ProviderLogMessages.LogGeminiEmbeddingRequestError(_logger, error, null);
            return [];
        }

        try
        {
            return ParseGeminiEmbeddingResponse(response);
        }
        catch (Exception ex)
        {
            ProviderLogMessages.LogGeminiEmbeddingParsingError(_logger, ex);
            return [];
        }
    }

    public override async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid)
        {
            ProviderLogMessages.LogGeminiEmbeddingValidationError(_logger, errorMessage, null);
            return [];
        }

        if (string.IsNullOrEmpty(config.EmbeddingModel))
        {
            ProviderLogMessages.LogGeminiEmbeddingModelMissing(_logger, null);
            return [];
        }

        var textList = texts?.ToList() ?? new List<string>();
        if (textList.Count == 0)
            return [];

        var results = new List<List<float>>();
        
        // Process texts in batches
        for (int i = 0; i < textList.Count; i += DefaultMaxBatchSize)
        {
            var batchTexts = textList.Skip(i).Take(DefaultMaxBatchSize).ToList();
            
            try
            {
                var batchResults = await ProcessGeminiBatchAsync(batchTexts, config);
                results.AddRange(batchResults);
                
                // Add delay between batches to respect rate limits
                if (i + DefaultMaxBatchSize < textList.Count)
                {
                    await Task.Delay(DefaultDelayBetweenBatchesMs);
                }
            }
            catch (Exception ex)
            {
                ProviderLogMessages.LogGeminiBatchFailedFallback(_logger, i / DefaultMaxBatchSize, ex.Message, ex);
                
                // Fallback to individual requests for this batch
                var fallbackResults = await base.GenerateEmbeddingsBatchAsync(batchTexts, config);
                results.AddRange(fallbackResults);
            }
        }

        return results;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Create Gemini HTTP client with proper authentication
    /// </summary>
    private HttpClient CreateGeminiHttpClient(string apiKey)
    {
        var client = CreateHttpClient(apiKey);
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add(GeminiApiKeyHeader, apiKey);
        return client;
    }

    /// <summary>
    /// Build Gemini API URL
    /// </summary>
    private static string BuildGeminiUrl(string endpoint, string model, string operation)
    {
        return $"{endpoint.TrimEnd('/')}/models/{model}:{operation}";
    }

    /// <summary>
    /// Create Gemini text generation payload
    /// </summary>
    private static object CreateGeminiTextPayload(string prompt, AIProviderConfig config)
    {
        var contents = new List<object>();
        
        if (!string.IsNullOrEmpty(config.SystemMessage))
        {
            contents.Add(new
            {
                parts = new[] { new { text = config.SystemMessage } }
            });
        }
        
        contents.Add(new
        {
            parts = new[] { new { text = prompt } }
        });

        return new { contents = contents.ToArray() };
    }

    /// <summary>
    /// Create Gemini embedding payload
    /// </summary>
    private static object CreateGeminiEmbeddingPayload(string text, string model)
    {
        return new
        {
            model = $"models/{model}",
            content = new
            {
                parts = new[] { new { text = text } }
            }
        };
    }

    /// <summary>
    /// Parse Gemini text response
    /// </summary>
    private static string ParseGeminiTextResponse(string response)
    {
        using var doc = JsonDocument.Parse(response);
        
        if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
            candidates.ValueKind == JsonValueKind.Array)
        {
            var firstCandidate = candidates.EnumerateArray().FirstOrDefault();

            if (firstCandidate.TryGetProperty("content", out var contentProp) &&
                contentProp.TryGetProperty("parts", out var parts) &&
                parts.ValueKind == JsonValueKind.Array)
            {
                var firstPart = parts.EnumerateArray().FirstOrDefault();
                if (firstPart.TryGetProperty("text", out var text))
                    return text.GetString() ?? "No response generated";
            }
        }

        return "No response generated";
    }

    /// <summary>
    /// Parse Gemini embedding response
    /// </summary>
    private static List<float> ParseGeminiEmbeddingResponse(string response)
    {
        using var doc = JsonDocument.Parse(response);

        if (doc.RootElement.TryGetProperty("embedding", out var embedding) &&
            embedding.TryGetProperty("values", out var values) &&
            values.ValueKind == JsonValueKind.Array)
        {
            var floats = new List<float>();
            foreach (var value in values.EnumerateArray())
            {
                if (value.TryGetSingle(out var f))
                    floats.Add(f);
            }
            return floats;
        }

        return [];
    }

    /// <summary>
    /// Process batch embedding requests
    /// </summary>
    private async Task<List<List<float>>> ProcessGeminiBatchAsync(List<string> batchTexts, AIProviderConfig config)
    {
        using var client = CreateGeminiHttpClient(config.ApiKey);

        var payload = new
        {
            requests = batchTexts.Select(text => new
            {
                model = $"models/{config.EmbeddingModel}",
                content = new
                {
                    parts = new[] { new { text = text } }
                }
            }).ToArray()
        };

        var batchEndpoint = BuildGeminiUrl(config.Endpoint!, config.EmbeddingModel!, "batchEmbedContents");

        var (success, response, error) = await MakeHttpRequestAsync(client, batchEndpoint, payload);

        if (!success)
        {
            ProviderLogMessages.LogGeminiBatchEmbeddingRequestError(_logger, error, null);
            throw new InvalidOperationException($"Batch embedding failed: {error}");
        }

        try
        {
            return ParseGeminiBatchEmbeddingResponse(response);
        }
        catch (Exception ex)
        {
            ProviderLogMessages.LogGeminiBatchEmbeddingParsingError(_logger, ex);
            throw;
        }
    }

    /// <summary>
    /// Parse Gemini batch embedding response
    /// </summary>
    private static List<List<float>> ParseGeminiBatchEmbeddingResponse(string response)
    {
        var results = new List<List<float>>();
        
        using var doc = JsonDocument.Parse(response);

        if (doc.RootElement.TryGetProperty("embeddings", out var embeddings) &&
            embeddings.ValueKind == JsonValueKind.Array)
        {
            foreach (var embedding in embeddings.EnumerateArray())
            {
                if (embedding.TryGetProperty("values", out var values) &&
                    values.ValueKind == JsonValueKind.Array)
                {
                    var floats = new List<float>();
                    foreach (var value in values.EnumerateArray())
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

        return results;
    }

    #endregion
}