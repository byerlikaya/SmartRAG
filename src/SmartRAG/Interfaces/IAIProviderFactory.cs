using SmartRAG.Enums;

namespace SmartRAG.Interfaces;

/// <summary>
/// Factory interface for creating AI providers
/// </summary>
public interface IAIProviderFactory
{
    /// <summary>
    /// Creates an AI provider instance of the specified type
    /// </summary>
    IAIProvider CreateProvider(AIProvider providerType);
}
