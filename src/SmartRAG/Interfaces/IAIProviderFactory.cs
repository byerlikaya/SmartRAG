using SmartRAG.Enums;

namespace SmartRAG.Interfaces;

/// <summary>
/// Factory interface for creating AI providers
/// </summary>
public interface IAIProviderFactory
{
    IAIProvider CreateProvider(AIProvider providerType);
}
