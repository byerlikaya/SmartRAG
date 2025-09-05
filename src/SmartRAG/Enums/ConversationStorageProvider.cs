namespace SmartRAG.Enums
{
    /// <summary>
    /// Available storage providers for conversation history
    /// </summary>
    public enum ConversationStorageProvider
    {
        /// <summary>
        /// Store conversations in Redis
        /// </summary>
        Redis,

        /// <summary>
        /// Store conversations in SQLite database
        /// </summary>
        Sqlite,

        /// <summary>
        /// Store conversations in file system
        /// </summary>
        FileSystem,

        /// <summary>
        /// Store conversations in memory (not persistent)
        /// </summary>
        InMemory
    }
}
