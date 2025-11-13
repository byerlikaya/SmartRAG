using SmartRAG.Enums;

namespace SmartRAG.Models
{

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
        /// File system path for file-based storage
        /// </summary>
        public string FileSystemPath { get; set; } = "Documents";

        /// <summary>
        /// Redis storage configuration
        /// </summary>
        public RedisConfig Redis { get; set; } = new RedisConfig();

        /// <summary>
        /// SQLite storage configuration
        /// </summary>
        public SqliteConfig Sqlite { get; set; } = new SqliteConfig();

        /// <summary>
        /// In-memory storage configuration
        /// </summary>
        public InMemoryConfig InMemory { get; set; } = new InMemoryConfig();

        /// <summary>
        /// Qdrant vector database storage configuration
        /// </summary>
        public QdrantConfig Qdrant { get; set; } = new QdrantConfig();
    }
}
