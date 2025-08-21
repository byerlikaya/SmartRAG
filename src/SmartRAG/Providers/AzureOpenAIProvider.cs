using SmartRAG.Enums;
using SmartRAG.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Linq;

namespace SmartRAG.Providers;

/// <summary>
/// Azure OpenAI provider implementation
/// </summary>
public class AzureOpenAIProvider : BaseAIProvider
{
    public AzureOpenAIProvider(ILogger<AzureOpenAIProvider> logger) : base(logger)
    {
    }

    public override AIProvider ProviderType => AIProvider.AzureOpenAI;

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
            max_tokens = config.MaxTokens,
            temperature = config.Temperature,
            stream = false
        };

        var url = $"{config.Endpoint!.TrimEnd('/')}/openai/deployments/{config.Model}/chat/completions?api-version={config.ApiVersion}";

        var (success, response, error) = await MakeHttpRequestAsync(client, url, payload);

        if (!success)
            return error;

        return ParseTextResponse(response);
    }

    public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
    {
        var (isValid, _) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid)
            return [];

        if (string.IsNullOrEmpty(config.EmbeddingModel))
            return [];

        using var client = CreateHttpClient(config.ApiKey);

        var payload = new
        {
            input = text
        };

        var url = $"{config.Endpoint!.TrimEnd('/')}/openai/deployments/{config.EmbeddingModel}/embeddings?api-version={config.ApiVersion}";

        // Azure OpenAI S0 tier için özel rate limiting (3 RPM)
        var (success, response, error) = await MakeHttpRequestAsyncWithRateLimit(client, url, payload, config);

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
            input = inputList.ToArray()
        };

        var url = $"{config.Endpoint!.TrimEnd('/')}/openai/deployments/{config.EmbeddingModel}/embeddings?api-version={config.ApiVersion}";

        // Azure OpenAI S0 tier için özel rate limiting (3 RPM)
        var (success, response, error) = await MakeHttpRequestAsyncWithRateLimit(client, url, payload, config);

        if (!success)
        {
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

    // Azure OpenAI S0 tier için rate limiting (3 RPM)
    private static readonly SemaphoreSlim _rateLimitSemaphore = new SemaphoreSlim(1, 1);
    private static DateTime _lastRequestTime = DateTime.MinValue;

    /// <summary>
    /// Azure OpenAI S0 tier için özel rate limiting (3 RPM)
    /// </summary>
    private async Task<(bool success, string response, string error)> MakeHttpRequestAsyncWithRateLimit(
        HttpClient client, string endpoint, object payload, AIProviderConfig config)
    {
        // S0 tier: 3 RPM - configurable minimum interval (default 60s)
        var minIntervalMs = Math.Max(0, config.EmbeddingMinIntervalMs ?? 60000);

        await _rateLimitSemaphore.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            var timeSinceLastRequest = now - _lastRequestTime;
            
            if (timeSinceLastRequest.TotalMilliseconds < minIntervalMs)
            {
                var waitTime = minIntervalMs - (int)timeSinceLastRequest.TotalMilliseconds;
                _logger.LogWarning("Azure OpenAI rate limit: waiting {WaitTime}ms", waitTime);
                await Task.Delay(waitTime);
            }
            
            _lastRequestTime = DateTime.UtcNow;
            
            // Normal request with retry logic
            return await MakeHttpRequestAsync(client, endpoint, payload, maxRetries: 5);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }
}