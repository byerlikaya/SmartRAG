namespace SmartRAG.Demo.Handlers.OllamaHandlers;

/// <summary>
/// Interface for Ollama operation handlers
/// </summary>
public interface IOllamaHandler
{
    Task SetupModelsAsync();
    Task TestVectorStoreAsync(string storageProvider);
}

