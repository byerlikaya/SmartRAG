namespace SmartRAG.Enums
{
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
        /// File system storage (JSON files on disk)
        /// </summary>
        FileSystem,
        
        /// <summary>
        /// Redis database for high-performance caching and storage
        /// </summary>
        Redis,
        
        /// <summary>
        /// SQLite database for lightweight local storage
        /// </summary>
        Sqlite,
        
        /// <summary>
        /// Qdrant vector database for advanced vector search capabilities
        /// </summary>
        Qdrant
    }
}
