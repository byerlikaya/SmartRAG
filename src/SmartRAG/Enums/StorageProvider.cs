namespace SmartRAG.Enums;


/// <summary>
/// Supported storage providers for document and vector data persistence
/// </summary>
public enum StorageProvider
{
    /// <summary>
    /// In-memory storage (non-persistent, for testing and development)
    /// </summary>
    InMemory,

    /// <summary>
    /// Redis database for high-performance caching and storage
    /// </summary>
    Redis,

    /// <summary>
    /// Qdrant vector database for advanced vector search capabilities
    /// </summary>
    Qdrant
}

