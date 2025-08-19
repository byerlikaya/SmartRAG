using SmartRAG.Enums;
using SmartRAG.Models;
using System.Text.Json;

namespace SmartRAG.Providers;

/// <summary>
/// Google Gemini AI provider implementation
/// </summary>
public class GeminiProvider : BaseAIProvider
{
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
            return [];

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

    public override async Task<List<List<float>>?> GenerateEmbeddingsBatchAsync(List<string> texts, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid || texts == null || texts.Count == 0)
            return null;

        if (string.IsNullOrEmpty(config.EmbeddingModel))
            return null;

        using var client = CreateHttpClient(config.ApiKey);

        var payload = new
        {
            model = $"models/{config.EmbeddingModel}",
            requests = texts.Select(text => new
            {
                content = new
                {
                    parts = new[]
                    {
                        new { text = text }
                    }
                }
            }).ToArray()
        };

        var embeddingEndpoint = $"{config.Endpoint!.TrimEnd('/')}/models/{config.EmbeddingModel}:batchEmbedContents";

        var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint!, payload, "Gemini");

        if (!success)
            return null;

        // Parse batch Gemini embedding response
        using var doc = JsonDocument.Parse(response);
        if (doc.RootElement.TryGetProperty("embeddings", out var embeddings) && embeddings.ValueKind == JsonValueKind.Array)
        {
            var results = new List<List<float>>();
            foreach (var item in embeddings.EnumerateArray())
            {
                if (item.TryGetProperty("embedding", out var embedding) && 
                    embedding.TryGetProperty("values", out var values) && 
                    values.ValueKind == JsonValueKind.Array)
                {
                    var embeddingList = values.EnumerateArray()
                        .Select(x => x.GetSingle())
                        .ToList();
                    results.Add(embeddingList);
                }
                else
                {
                    results.Add(new List<float>());
                }
            }
            return results.Count == texts.Count ? results : null;
        }

        return null;
    }
}