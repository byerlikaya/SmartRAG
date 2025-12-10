using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using System.Net.Http;
using SmartRAG.Enums;
using SmartRAG.Interfaces.AI;
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
        private readonly IHttpClientFactory _httpClientFactory;

        public AIProviderFactory(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
        {
            _loggerFactory = loggerFactory;
            _httpClientFactory = httpClientFactory;
            _providers = new Dictionary<AIProvider, IAIProvider>();
            InitializeProviders();
        }

        private void InitializeProviders()
        {
            _providers[AIProvider.Gemini] = new GeminiProvider(_loggerFactory.CreateLogger<GeminiProvider>(), _httpClientFactory);
            _providers[AIProvider.OpenAI] = new OpenAIProvider(_loggerFactory.CreateLogger<OpenAIProvider>(), _httpClientFactory);
            _providers[AIProvider.Anthropic] = new AnthropicProvider(_loggerFactory.CreateLogger<AnthropicProvider>(), _httpClientFactory);
            _providers[AIProvider.AzureOpenAI] = new AzureOpenAIProvider(_loggerFactory.CreateLogger<AzureOpenAIProvider>(), _httpClientFactory);
            _providers[AIProvider.Custom] = new CustomProvider(_loggerFactory.CreateLogger<CustomProvider>(), _httpClientFactory);
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
