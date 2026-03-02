
namespace SmartRAG.Interfaces.AI;

/// <summary>
/// Factory interface for creating AI providers
/// </summary>
public interface IAIProviderFactory
{
    /// <summary>
    /// Creates an AI provider instance of the specified type
    /// </summary>
    /// <param name="providerType">Type of AI provider to create</param>
    /// <returns>AI provider instance</returns>
    IAIProvider CreateProvider(AIProvider providerType);
}