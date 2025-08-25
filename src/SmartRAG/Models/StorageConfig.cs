namespace SmartRAG.Models;

/// <summary>
/// Storage configuration for different storage providers
/// </summary>
public class StorageConfig
{
    public StorageProvider Provider { get; set; } = StorageProvider.InMemory;

    public string FileSystemPath { get; set; } = "Documents";

    public RedisConfig Redis { get; set; } = new();

    public SqliteConfig Sqlite { get; set; } = new();

    public InMemoryConfig InMemory { get; set; } = new();

    public QdrantConfig Qdrant { get; set; } = new();
}
