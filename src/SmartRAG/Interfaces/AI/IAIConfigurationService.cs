using SmartRAG.Enums;
using SmartRAG.Models;

namespace SmartRAG.Interfaces.AI;

/// <summary>
/// Service interface for AI provider configuration
/// </summary>
public interface IAIConfigurationService
{
    /// <summary>
    /// Gets AI provider configuration for the currently configured provider
    /// </summary>
    AIProviderConfig? GetAIProviderConfig();

    /// <summary>
    /// Gets AI provider configuration for a specific provider
    /// </summary>
    AIProviderConfig? GetProviderConfig(AIProvider provider);
}