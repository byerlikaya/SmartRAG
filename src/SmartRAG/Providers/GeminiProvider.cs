using SmartRAG.Enums;
using SmartRAG.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SmartRAG.Providers;

/// <summary>
/// Google Gemini AI provider implementation
/// </summary>
public class GeminiProvider : BaseAIProvider
{
    public GeminiProvider(ILogger<GeminiProvider> logger) : base(logger)
    {
    }

    public override AIProvider ProviderType => AIProvider.Gemini;

    public override async Task<string> GenerateTextAsync(string prompt, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config);

        if (!isValid)
            return errorMessage;

        using var client = CreateHttpClient(config.ApiKey);
        // Override auth header for Gemini
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("x-goog-api-key", config.ApiKey);

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var modelEndpoint = $"{config.Endpoint!.TrimEnd('/')}/models/{config.Model}:generateContent";

        var (success, response, error) = await MakeHttpRequestAsync(client, modelEndpoint!, payload);

        if (!success)
            return error;

        // Gemini has different response format
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

    public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid) return [];

        if (string.IsNullOrEmpty(config.EmbeddingModel))
            return [];

        using var client = CreateHttpClient(config.ApiKey);
        // Override auth header for Gemini
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("x-goog-api-key", config.ApiKey);

        var payload = new
        {
            model = $"models/{config.EmbeddingModel}",
            content = new
            {
                parts = new[]
                {
                    new { text = text }
                }
            }
        };

        var embeddingEndpoint = $"{config.Endpoint!.TrimEnd('/')}/models/{config.EmbeddingModel}:embedContent";

        var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint!, payload);

        if (!success)
        {
            // Log detailed error for debugging
            ProviderLogMessages.LogGeminiEmbeddingError(_logger, error, null);
            return [];
        }

        // Gemini has different embedding response format
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

    public override async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid) return [];

        if (string.IsNullOrEmpty(config.EmbeddingModel))
            return [];

        var textList = texts.ToList();
        var results = new List<List<float>>();

        // Generic batch processing with configurable limits
        const int maxBatchSize = 50; // Configurable batch size
        const int delayBetweenBatchesMs = 1000; // Configurable delay between batches
        
        // Process texts in batches of maxBatchSize
        for (int i = 0; i < textList.Count; i += maxBatchSize)
        {
            var batchTexts = textList.Skip(i).Take(maxBatchSize).ToList();
            
            try
            {
                var batchResults = await ProcessBatchAsync(batchTexts, config);
                results.AddRange(batchResults);
                
                // Add delay between batches to respect rate limits
                if (i + maxBatchSize < textList.Count)
                {
                    await Task.Delay(delayBetweenBatchesMs);
                }
            }
            catch (Exception ex)
            {
                ProviderLogMessages.LogGeminiBatchFailedFallback(_logger, i / maxBatchSize, ex.Message, ex);
                
                // Fallback to individual requests for this batch
                var fallbackResults = await base.GenerateEmbeddingsBatchAsync(batchTexts, config);
                results.AddRange(fallbackResults);
            }
        }

        return results;
    }

    private async Task<List<List<float>>> ProcessBatchAsync(List<string> batchTexts, AIProviderConfig config)
    {
        using var client = CreateHttpClient(config.ApiKey);
        // Override auth header for Gemini
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("x-goog-api-key", config.ApiKey);

        var payload = new
        {
            requests = batchTexts.Select(text => new
            {
                model = $"models/{config.EmbeddingModel}",
                content = new
                {
                    parts = new[]
                    {
                        new { text = text }
                    }
                }
            }).ToArray()
        };

        var batchEndpoint = $"{config.Endpoint!.TrimEnd('/')}/models/{config.EmbeddingModel}:batchEmbedContents";

        var (success, response, error) = await MakeHttpRequestAsync(client, batchEndpoint, payload);

        if (!success)
        {
            // Log detailed error for debugging
            ProviderLogMessages.LogGeminiBatchEmbeddingError(_logger, error, null);
            throw new InvalidOperationException($"Batch embedding failed: {error}");
        }

        var results = new List<List<float>>();

        try
        {
            Console.WriteLine($"[DEBUG] Gemini Response: {response}");
            
            using var doc = JsonDocument.Parse(response);

            if (doc.RootElement.TryGetProperty("embeddings", out var embeddings) &&
                embeddings.ValueKind == JsonValueKind.Array)
            {
                Console.WriteLine($"[DEBUG] Found embeddings array with {embeddings.GetArrayLength()} items");
                
                foreach (var embedding in embeddings.EnumerateArray())
                {
                    Console.WriteLine($"[DEBUG] Processing embedding: {embedding}");
                    
                    // Gemini response format: {"embeddings": [{"values": [0.1, 0.2, ...]}]}
                    if (embedding.TryGetProperty("values", out var values) &&
                        values.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"[DEBUG] Found values array with {values.GetArrayLength()} items");
                        
                        var floats = new List<float>();
                        foreach (var value in values.EnumerateArray())
                        {
                            if (value.TryGetSingle(out var f))
                                floats.Add(f);
                        }
                        
                        Console.WriteLine($"[DEBUG] Parsed {floats.Count} float values");
                        results.Add(floats);
                    }
                    else
                    {
                        Console.WriteLine("[DEBUG] Missing values property, adding empty list");
                        results.Add(new List<float>());
                    }
                }
            }
            else
            {
                Console.WriteLine("[DEBUG] No embeddings property found in response");
            }

            Console.WriteLine($"[DEBUG] Returning {results.Count} embedding results");
            return results;
        }
        catch (Exception ex)
        {
            ProviderLogMessages.LogGeminiBatchParsingError(_logger, ex.Message, ex);
            throw;
        }
    }
}