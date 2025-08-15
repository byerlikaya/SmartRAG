using SmartRAG.Enums;
using SmartRAG.Models;

namespace SmartRAG.Providers;

/// <summary>
/// Azure OpenAI provider implementation
/// </summary>
public class AzureOpenAIProvider : BaseAIProvider
{
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

        var (success, response, error) = await MakeHttpRequestAsync(client, url, payload, "Azure OpenAI");

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

        var (success, response, error) = await MakeHttpRequestAsync(client, url, payload, "Azure OpenAI");

        if (!success)
            return [];

        return ParseEmbeddingResponse(response);
    }
}