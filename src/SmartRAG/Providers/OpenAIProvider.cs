using SmartRAG.Enums;
using SmartRAG.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Linq;

namespace SmartRAG.Providers;

/// <summary>
/// OpenAI provider implementation
/// </summary>
public class OpenAIProvider : BaseAIProvider
{
    public OpenAIProvider(ILogger<OpenAIProvider> logger) : base(logger)
    {
    }

    public override AIProvider ProviderType => AIProvider.OpenAI;

    public override async Task<string> GenerateTextAsync(string prompt, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config);

        if (!isValid)
            return errorMessage;

        using var client = CreateHttpClient(config.ApiKey);

        var payload = new
        {
            messages = new[]
            {
                new { role = "system", content = "You are a helpful AI assistant that answers questions based on provided context. Always base your answers on the context information provided. If the context doesn't contain enough information, say so clearly." },
                new { role = "user", content = prompt }
            },
            model = config.Model,
            max_tokens = config.MaxTokens,
            temperature = config.Temperature,
            stream = false
        };

        var chatEndpoint = $"{config.Endpoint!.TrimEnd('/')}/chat/completions";

        var (success, response, error) = await MakeHttpRequestAsync(client, chatEndpoint!, payload, "OpenAI");

        if (!success)
            return error;

        return ParseTextResponse(response);
    }

    public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid)
            return [];

        if (string.IsNullOrEmpty(config.EmbeddingModel))
            return [];

        using var client = CreateHttpClient(config.ApiKey);

        var payload = new
        {
            input = text,
            model = config.EmbeddingModel
        };

        var embeddingEndpoint = $"{config.Endpoint!.TrimEnd('/')}/embeddings";

        var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint!, payload, "OpenAI");

        if (!success)
            return [];

        return ParseEmbeddingResponse(response);
    }

    public override async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config)
    {
        var (isValid, _) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid)
            return [];

        if (string.IsNullOrEmpty(config.EmbeddingModel))
            return [];

        var inputList = texts?.ToList() ?? new List<string>();
        if (inputList.Count == 0)
            return [];

        using var client = CreateHttpClient(config.ApiKey);

        var payload = new
        {
            input = inputList.ToArray(),
            model = config.EmbeddingModel
        };

        var embeddingEndpoint = $"{config.Endpoint!.TrimEnd('/')}/embeddings";

        var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint!, payload, "OpenAI");

        if (!success)
        {
            // Preserve order and size with empty embeddings on failure
            return Enumerable.Repeat(new List<float>(), inputList.Count).ToList();
        }

        var results = new List<List<float>>(capacity: inputList.Count);
        try
        {
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in data.EnumerateArray())
                {
                    if (item.TryGetProperty("embedding", out var embedding) && embedding.ValueKind == JsonValueKind.Array)
                    {
                        var floats = new List<float>(embedding.GetArrayLength());
                        foreach (var value in embedding.EnumerateArray())
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
        catch
        {
            return Enumerable.Repeat(new List<float>(), inputList.Count).ToList();
        }
    }
}