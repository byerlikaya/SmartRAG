#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SmartRAG.Interfaces.AI;
using SmartRAG.Enums;
using SmartRAG.Models;

namespace SmartRAG.Services.AI;


/// <summary>
/// Service for AI provider configuration
/// </summary>
public class AIConfigurationService : IAIConfigurationService
{
    private readonly SmartRagOptions _options;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the AIConfigurationService
    /// </summary>
    /// <param name="options">SmartRAG configuration options</param>
    /// <param name="configuration">Application configuration</param>
    public AIConfigurationService(
        IOptions<SmartRagOptions> options,
        IConfiguration configuration)
    {
        _options = options.Value;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets AI provider configuration
    /// </summary>
    public AIProviderConfig? GetAIProviderConfig()
    {
        return GetProviderConfig(_options.AIProvider);
    }

    /// <summary>
    /// Gets AI provider configuration for a specific provider
    /// </summary>
    public AIProviderConfig? GetProviderConfig(AIProvider provider)
    {
        var providerKey = provider.ToString();
        var providerConfig = _configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

        if (providerConfig == null || (provider != Enums.AIProvider.Custom && string.IsNullOrEmpty(providerConfig.ApiKey)))
        {
            return null;
        }

        return providerConfig;
    }
}


