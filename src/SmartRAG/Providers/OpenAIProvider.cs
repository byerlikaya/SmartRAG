using SmartRAG.Enums;
using SmartRAG.Models;
using Microsoft.Extensions.Logging;

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
}