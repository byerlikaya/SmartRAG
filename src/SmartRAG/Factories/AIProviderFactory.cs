using Microsoft.Extensions.Logging;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Providers;
using System;
using System.Collections.Generic;

namespace SmartRAG.Factories
{

    /// <summary>
    /// Factory for creating AI providers based on configuration
    /// </summary>
    public class AIProviderFactory : IAIProviderFactory
    {
        private readonly Dictionary<AIProvider, IAIProvider> _providers;
        private readonly ILoggerFactory _loggerFactory;

        public AIProviderFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _providers = new Dictionary<AIProvider, IAIProvider>();
            InitializeProviders();
        }

        private void InitializeProviders()
        {
            _providers[AIProvider.Gemini] = new GeminiProvider(_loggerFactory.CreateLogger<GeminiProvider>());
            _providers[AIProvider.OpenAI] = new OpenAIProvider(_loggerFactory.CreateLogger<OpenAIProvider>());
            _providers[AIProvider.Anthropic] = new AnthropicProvider(_loggerFactory.CreateLogger<AnthropicProvider>());
            _providers[AIProvider.AzureOpenAI] = new AzureOpenAIProvider(_loggerFactory.CreateLogger<AzureOpenAIProvider>());
            _providers[AIProvider.Custom] = new CustomProvider(_loggerFactory.CreateLogger<CustomProvider>());
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
}
