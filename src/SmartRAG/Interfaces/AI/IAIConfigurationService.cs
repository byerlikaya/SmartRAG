#nullable enable

using SmartRAG.Models;

namespace SmartRAG.Interfaces.AI
{
    /// <summary>
    /// Service interface for AI provider configuration
    /// </summary>
    public interface IAIConfigurationService
    {
        /// <summary>
        /// Gets AI provider configuration
        /// </summary>
        /// <returns>AI provider configuration or null if not available</returns>
        AIProviderConfig? GetAIProviderConfig();
    }
}

