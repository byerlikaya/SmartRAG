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

        var (success, response, error) = await MakeHttpRequestAsync(client, modelEndpoint!, payload, "Gemini");

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

        var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint!, payload, "Gemini");

        if (!success)
        {
            // Log detailed error for debugging
            _logger.LogError("Gemini embedding error: {Error}", error);
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

        // Gemini has a limit of 50 requests per batch for Free Tier (to stay under 100 RPM)
        const int maxBatchSize = 50;
        const int delayBetweenBatchesMs = 600; // 600ms to stay under 100 RPM limit
        
        // Process texts in batches of maxBatchSize
        for (int i = 0; i < textList.Count; i += maxBatchSize)
        {
            var batchTexts = textList.Skip(i).Take(maxBatchSize).ToList();
            
            try
            {
                var batchResults = await ProcessBatchAsync(batchTexts, config);
                results.AddRange(batchResults);
                
                // Add delay between batches to respect Free Tier rate limits (100 RPM = 600ms between requests)
                if (i + maxBatchSize < textList.Count)
                {
                    await Task.Delay(delayBetweenBatchesMs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to process batch {BatchIndex}, falling back to individual requests: {Error}", 
                    i / maxBatchSize, ex.Message);
                
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

        var (success, response, error) = await MakeHttpRequestAsync(client, batchEndpoint, payload, "Gemini");

        if (!success)
        {
            _logger.LogError("Gemini batch embedding error: {Error}", error);
            throw new Exception($"Batch embedding failed: {error}");
        }

        var results = new List<List<float>>();

        try
        {
            using var doc = JsonDocument.Parse(response);

            if (doc.RootElement.TryGetProperty("embeddings", out var embeddings) &&
                embeddings.ValueKind == JsonValueKind.Array)
            {
                foreach (var embedding in embeddings.EnumerateArray())
                {
                    if (embedding.TryGetProperty("embedding", out var embeddingProp) &&
                        embeddingProp.TryGetProperty("values", out var values) &&
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
                        results.Add(new List<float>());
                    }
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to parse batch embedding response: {Error}", ex.Message);
            throw;
        }
    }
}