
namespace SmartRAG.Models.Configuration;



/// <summary>
/// Storage configuration for different storage providers
/// </summary>
public class StorageConfig
{
    /// <summary>
    /// Selected storage provider
    /// </summary>
    public StorageProvider Provider { get; set; } = StorageProvider.InMemory;

    /// <summary>
    /// Redis storage configuration
    /// </summary>
    public RedisConfig Redis { get; set; } = new();

    /// <summary>
    /// In-memory storage configuration
    /// </summary>
    public InMemoryConfig InMemory { get; set; } = new();

    /// <summary>
    /// Qdrant vector database storage configuration
    /// </summary>
    public QdrantConfig Qdrant { get; set; } = new();
}

