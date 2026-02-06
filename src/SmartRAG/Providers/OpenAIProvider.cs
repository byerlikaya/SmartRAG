using Microsoft.Extensions.Logging;
using System.Net.Http;
using SmartRAG.Enums;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Providers;



/// <summary>
/// OpenAI provider implementation
/// </summary>
public class OpenAIProvider : BaseAIProvider
{
    /// <summary>
    /// Initializes a new instance of the OpenAIProvider
    /// </summary>
    /// <param name="logger">Logger instance for this provider</param>
    /// <param name="httpClientFactory">HTTP client factory for creating HTTP clients</param>
    public OpenAIProvider(ILogger<OpenAIProvider> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }

    private const string ChatCompletionsPath = "chat/completions";
    private const string EmbeddingsPath = "embeddings";
    private const string SystemRole = "system";
    private const string UserRole = "user";

    public override AIProvider ProviderType => AIProvider.OpenAI;

    public override async Task<string> GenerateTextAsync(string prompt, AIProviderConfig config, CancellationToken cancellationToken = default)
    {
        var (isValid, errorMessage) = ValidateConfig(config);

        if (!isValid)
            return errorMessage;

        using var client = CreateHttpClient(config.ApiKey);
        var payload = CreateOpenAITextPayload(prompt, config);

        var chatEndpoint = BuildOpenAIUrl(config.Endpoint, ChatCompletionsPath);

        var (success, response, error) = await MakeHttpRequestAsync(client, chatEndpoint, payload, cancellationToken: cancellationToken);

        if (!success)
            return error;

        try
        {
            return ParseOpenAITextResponse(response);
        }
        catch (Exception ex)
        {
            ProviderLogMessages.LogOpenAITextParsingError(Logger, ex);
            return $"Error parsing OpenAI response: {ex.Message}";
        }
    }

    public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config, CancellationToken cancellationToken = default)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid)
        {
            ProviderLogMessages.LogOpenAIEmbeddingValidationError(Logger, errorMessage, null);
            return new List<float>();
        }

        if (string.IsNullOrEmpty(config.EmbeddingModel))
        {
            ProviderLogMessages.LogOpenAIEmbeddingModelMissing(Logger, null);
            return new List<float>();
        }

        using var client = CreateHttpClient(config.ApiKey);

        var payload = new { input = text, model = config.EmbeddingModel };

        var embeddingEndpoint = BuildOpenAIUrl(config.Endpoint, EmbeddingsPath);

        var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint, payload, cancellationToken: cancellationToken);

        if (!success)
        {
            ProviderLogMessages.LogOpenAIEmbeddingRequestError(Logger, error, null);
            return new List<float>();
        }

        try
        {
            return ParseOpenAIEmbeddingResponse(response);
        }
        catch (Exception ex)
        {
            ProviderLogMessages.LogOpenAIEmbeddingParsingError(Logger, ex);
            return new List<float>();
        }
    }

    public override async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config, CancellationToken cancellationToken = default)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid)
        {
            ProviderLogMessages.LogOpenAIEmbeddingValidationError(Logger, errorMessage, null);
            return new List<List<float>>();
        }

        if (string.IsNullOrEmpty(config.EmbeddingModel))
        {
            ProviderLogMessages.LogOpenAIEmbeddingModelMissing(Logger, null);
            return new List<List<float>>();
        }

        var inputList = texts?.ToList() ?? new List<string>();
        if (inputList.Count == 0)
            return new List<List<float>>();

        using var client = CreateHttpClient(config.ApiKey);
        var payload = new { input = inputList.ToArray(), model = config.EmbeddingModel };

        var embeddingEndpoint = BuildOpenAIUrl(config.Endpoint, EmbeddingsPath);

        var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint, payload, cancellationToken: cancellationToken);

        if (!success)
        {
            ProviderLogMessages.LogOpenAIBatchEmbeddingRequestError(Logger, error, null);
            return ParseBatchEmbeddingResponse("", inputList.Count);
        }

        try
        {
            return ParseOpenAIBatchEmbeddingResponse(response, inputList.Count);
        }
        catch (Exception ex)
        {
            ProviderLogMessages.LogOpenAIBatchEmbeddingParsingError(Logger, ex);
            return ParseBatchEmbeddingResponse("", inputList.Count);
        }
    }

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

        return new List<float>();
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
                        results.Add(new List<float>());
                    }
                }
            }
        }
        catch
        {
        }

        return Enumerable.Range(0, expectedCount)
            .Select(i => i < results.Count ? results[i] : new List<float>())
            .ToList();
    }
}

