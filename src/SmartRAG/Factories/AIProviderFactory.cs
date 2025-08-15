using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Providers;

namespace SmartRAG.Factories;

/// <summary>
/// Factory for creating AI providers based on configuration
/// </summary>
public class AIProviderFactory : IAIProviderFactory
{
    private readonly Dictionary<AIProvider, IAIProvider> _providers;

    public AIProviderFactory()
    {
        _providers = [];
        InitializeProviders();
    }

    private void InitializeProviders()
    {
        _providers[AIProvider.Gemini] = new GeminiProvider();
        _providers[AIProvider.OpenAI] = new OpenAIProvider();
        _providers[AIProvider.Anthropic] = new AnthropicProvider();
        _providers[AIProvider.AzureOpenAI] = new AzureOpenAIProvider();
        _providers[AIProvider.Custom] = new CustomProvider();
    }

    public IAIProvider CreateProvider(AIProvider providerType)
    {
        if (_providers.TryGetValue(providerType, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException($"AI Provider '{providerType}' is not supported or not implemented.");
    }
}