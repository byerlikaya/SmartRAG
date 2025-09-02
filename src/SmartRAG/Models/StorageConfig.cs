using SmartRAG.Enums;

namespace SmartRAG.Models
{

    /// <summary>
    /// Storage configuration for different storage providers
    /// </summary>
    public class StorageConfig
    {
        public StorageProvider Provider { get; set; } = StorageProvider.InMemory;

        public string FileSystemPath { get; set; } = "Documents";

        public RedisConfig Redis { get; set; } = new RedisConfig();

        public SqliteConfig Sqlite { get; set; } = new SqliteConfig();

        public InMemoryConfig InMemory { get; set; } = new InMemoryConfig();

        public QdrantConfig Qdrant { get; set; } = new QdrantConfig();
    }
}
