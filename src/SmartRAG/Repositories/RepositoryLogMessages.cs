namespace SmartRAG.Repositories;


/// <summary>
/// Centralized LoggerMessage delegates for repository operations
/// </summary>
public static class RepositoryLogMessages
{
    public static readonly Action<ILogger, string, Guid, Exception> LogDocumentAdded = LoggerMessage.Define<string, Guid>(
        LogLevel.Information,
        new EventId(30001, "DocumentAdded"),
        "Document added successfully: {FileName} (ID: {DocumentId})");

    public static readonly Action<ILogger, string, Exception> LogDocumentAddFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(30002, "DocumentAddFailed"),
        "Failed to add document: {FileName}");

    public static readonly Action<ILogger, Guid, Exception> LogDocumentRetrievalFailed = LoggerMessage.Define<Guid>(
        LogLevel.Error,
        new EventId(30006, "DocumentRetrievalFailed"),
        "Failed to retrieve document: {DocumentId}");

    public static readonly Action<ILogger, Exception> LogDocumentsRetrievalFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(30008, "DocumentsRetrievalFailed"),
        "Failed to retrieve documents");

    public static readonly Action<ILogger, string, Guid, Exception> LogDocumentDeleted = LoggerMessage.Define<string, Guid>(
        LogLevel.Information,
        new EventId(30009, "DocumentDeleted"),
        "Document deleted: {FileName} (ID: {DocumentId})");

    public static readonly Action<ILogger, Guid, Exception> LogDocumentDeleteNotFound = LoggerMessage.Define<Guid>(
        LogLevel.Warning,
        new EventId(30010, "DocumentDeleteNotFound"),
        "Document not found for deletion: {DocumentId}");

    public static readonly Action<ILogger, Guid, Exception> LogDocumentDeleteFailed = LoggerMessage.Define<Guid>(
        LogLevel.Error,
        new EventId(30011, "DocumentDeleteFailed"),
        "Failed to delete document: {DocumentId}");

    public static readonly Action<ILogger, Exception> LogDocumentCountRetrievalFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(30013, "DocumentCountRetrievalFailed"),
        "Failed to retrieve document count");

    public static readonly Action<ILogger, Exception> LogSearchFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(31002, "SearchFailed"),
        "Search failed during search operation");

    public static readonly Action<ILogger, Exception> LogQdrantCollectionInitFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(34001, "QdrantCollectionInitFailed"),
        "Failed to initialize Qdrant collection");

    public static readonly Action<ILogger, string, string, Guid, Exception> LogQdrantDocumentCollectionCreating = LoggerMessage.Define<string, string, Guid>(
        LogLevel.Information,
        new EventId(34002, "QdrantDocumentCollectionCreating"),
        "Creating Qdrant document collection: {CollectionName} (Base: {BaseCollection}, Document: {DocumentId})");

    public static readonly Action<ILogger, int, Exception> LogQdrantEmbeddingsGenerationStarted = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(34003, "QdrantEmbeddingsGenerationStarted"),
        "Started generating embeddings for {ChunkCount} chunks");

    public static readonly Action<ILogger, int, Exception> LogQdrantEmbeddingsGenerationCompleted = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(34005, "QdrantEmbeddingsGenerationCompleted"),
        "Completed generating embeddings for {ChunkCount} chunks");

    public static readonly Action<ILogger, int, Exception> LogQdrantPointsCreationStarted = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(34006, "QdrantPointsCreationStarted"),
        "Creating {ChunkCount} Qdrant points");

    public static readonly Action<ILogger, int, int, int, Exception> LogQdrantBatchUploadProgress = LoggerMessage.Define<int, int, int>(
        LogLevel.Information,
        new EventId(34007, "QdrantBatchUploadProgress"),
        "Uploaded batch {BatchNumber}/{TotalBatches} ({PointCount} points)");

    public static readonly Action<ILogger, string, string, Exception> LogQdrantDocumentUploadSuccess = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(34008, "QdrantDocumentUploadSuccess"),
        "Document '{FileName}' uploaded successfully to Qdrant collection: {CollectionName}");

    public static readonly Action<ILogger, string, Exception> LogQdrantDocumentUploadFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(34009, "QdrantDocumentUploadFailed"),
        "Failed to upload document: {FileName}");

    public static readonly Action<ILogger, int, Exception> LogQdrantFinalResultsReturned = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(34025, "QdrantFinalResultsReturned"),
        "Returning {ChunkCount} chunks to DocumentService for final selection");

    public static readonly Action<ILogger, string, Exception> LogQdrantVectorSearchFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(34028, "QdrantVectorSearchFailed"),
        "Vector search failed: {ErrorMessage}, falling back to text search");

    public static readonly Action<ILogger, string, Exception> LogRedisConnectionEstablished = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(35001, "RedisConnectionEstablished"),
        "Redis connection established to {ConnectionString}");

    public static readonly Action<ILogger, string, Exception> LogRedisConnectionFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(35002, "RedisConnectionFailed"),
        "Failed to connect to Redis server: {ConnectionString}");

    public static readonly Action<ILogger, Guid, Exception> LogRedisDocumentRetrievalFailed = LoggerMessage.Define<Guid>(
        LogLevel.Error,
        new EventId(35007, "RedisDocumentRetrievalFailed"),
        "Failed to retrieve document from Redis: {DocumentId}");

    public static readonly Action<ILogger, int, Exception> LogRedisDocumentsRetrieved = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(35008, "RedisDocumentsRetrieved"),
        "Retrieved {DocumentCount} documents from Redis");

    public static readonly Action<ILogger, Exception> LogRedisDocumentsRetrievalFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(35009, "RedisDocumentsRetrievalFailed"),
        "Failed to retrieve documents from Redis");

    public static readonly Action<ILogger, Guid, Exception> LogRedisDocumentDeleted = LoggerMessage.Define<Guid>(
        LogLevel.Information,
        new EventId(35010, "RedisDocumentDeleted"),
        "Document deleted from Redis: {DocumentId}");

    public static readonly Action<ILogger, Guid, Exception> LogRedisDocumentDeleteFailed = LoggerMessage.Define<Guid>(
        LogLevel.Error,
        new EventId(35011, "RedisDocumentDeleteFailed"),
        "Failed to delete document from Redis: {DocumentId}");

    public static readonly Action<ILogger, Exception> LogRedisDocumentCountRetrievalFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(35013, "RedisDocumentCountRetrievalFailed"),
        "Failed to retrieve document count from Redis");

    public static readonly Action<ILogger, Exception> LogRedisSearchFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(35015, "RedisSearchFailed"),
        "Redis search failed during search operation");

    public static readonly Action<ILogger, int, int, Exception> LogOldDocumentsRemoved = LoggerMessage.Define<int, int>(
        LogLevel.Information,
        new EventId(33001, "OldDocumentsRemoved"),
        "Removed {RemovedCount} old documents to maintain capacity limit of {MaxDocuments}");

    public static readonly Action<ILogger, string, Exception> LogRedisVectorIndexExists = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(35209, "RedisVectorIndexExists"),
        "Redis vector index '{IndexName}' already exists");

    public static readonly Action<ILogger, string, Exception> LogRedisVectorIndexCreated = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(35210, "RedisVectorIndexCreated"),
        "Created Redis vector index '{IndexName}'");

    public static readonly Action<ILogger, string, Exception> LogRedisVectorIndexCreationFailure = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(35211, "RedisVectorIndexCreationFailure"),
        "Failed to create Redis vector index '{IndexName}'");

    public static readonly Action<ILogger, Exception> LogRedisVectorSearchFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(35212, "RedisVectorSearchFailed"),
        "Redis vector search failed. Falling back to text search.");

    public static readonly Action<ILogger, Exception> LogRedisRediSearchModuleMissing = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(35213, "RedisRediSearchModuleMissing"),
        "Redis RediSearch module is not available. Vector similarity search will be disabled. " +
        "Redis will still work for document storage and text search. " +
        "To enable vector search, install Redis with RediSearch module: " +
        "docker run -d -p 6379:6379 redis/redis-stack-server:latest");

    public static readonly Action<ILogger, Exception> LogConversationGetHistoryFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36001, "ConversationGetHistoryFailed"),
        "Error getting conversation history");

    public static readonly Action<ILogger, Exception> LogConversationAddFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36002, "ConversationAddFailed"),
        "Error adding to conversation");

    public static readonly Action<ILogger, Exception> LogConversationClearFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36003, "ConversationClearFailed"),
        "Error clearing conversation");

    public static readonly Action<ILogger, Exception> LogConversationAppendSourcesFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36004, "ConversationAppendSourcesFailed"),
        "Error appending sources for turn");

    public static readonly Action<ILogger, Exception> LogConversationGetSourcesFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36005, "ConversationGetSourcesFailed"),
        "Error getting sources for session");

    public static readonly Action<ILogger, Exception> LogConversationCheckSessionFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36006, "ConversationCheckSessionFailed"),
        "Error checking session existence");

    public static readonly Action<ILogger, Exception> LogConversationSetHistoryFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36007, "ConversationSetHistoryFailed"),
        "Error setting conversation history");

    public static readonly Action<ILogger, Exception> LogConversationGetTimestampsFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36008, "ConversationGetTimestampsFailed"),
        "Error getting session timestamps");

    public static readonly Action<ILogger, string, Exception> LogConversationListSessionsFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(36009, "ConversationListSessionsFailed"),
        "Error listing conversation sessions from {Source}");

    public static readonly Action<ILogger, Exception?> LogSqliteConversationsCleared = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36101, "SqliteConversationsCleared"),
        "Cleared all conversations from SQLite");

    public static readonly Action<ILogger, Exception> LogSqliteConversationsClearFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36102, "SqliteConversationsClearFailed"),
        "Error clearing all conversations from SQLite");

    public static readonly Action<ILogger, Exception?> LogRedisNoEndpointsClear = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36201, "RedisNoEndpointsClear"),
        "No Redis endpoints available for clearing conversations");

    public static readonly Action<ILogger, Exception?> LogRedisConversationsCleared = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(36202, "RedisConversationsCleared"),
        "Cleared all conversation history from Redis");

    public static readonly Action<ILogger, Exception> LogRedisConversationsClearFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36203, "RedisConversationsClearFailed"),
        "Error clearing all conversations from Redis");

    public static readonly Action<ILogger, Exception?> LogRedisNoEndpointsList = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(36204, "RedisNoEndpointsList"),
        "No Redis endpoints available for listing conversations");

    public static readonly Action<ILogger, Exception> LogFileSystemConversationsClearFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(36301, "FileSystemConversationsClearFailed"),
        "Error clearing all conversations from file system");

    public static readonly Action<ILogger, Guid, Exception> LogRedisChunkEmbeddingFailed = LoggerMessage.Define<Guid>(
        LogLevel.Warning,
        new EventId(35020, "RedisChunkEmbeddingFailed"),
        "Failed to generate embedding for chunk {ChunkId}");

    public static readonly Action<ILogger, Exception> LogRedisDocumentsFromChunksRetrievalFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(35021, "RedisDocumentsFromChunksRetrievalFailed"),
        "Failed to retrieve documents from chunks");

    public static readonly Action<ILogger, Exception?> LogRedisNoEndpointsChunkRetrieval = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(35022, "RedisNoEndpointsChunkRetrieval"),
        "No Redis endpoints available for chunk key retrieval");

    public static readonly Action<ILogger, int, Exception> LogRedisChunksCleared = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(35023, "RedisChunksCleared"),
        "Cleared {Count} chunks from Redis");

    public static readonly Action<ILogger, Exception> LogRedisChunksClearFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(35024, "RedisChunksClearFailed"),
        "Failed to clear chunks from Redis");

    public static readonly Action<ILogger, Exception> LogRedisClearAllFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(35025, "RedisClearAllFailed"),
        "Failed to clear all documents from Redis");

    public static readonly Action<ILogger, Guid, Exception> LogQdrantInvalidMetadata = LoggerMessage.Define<Guid>(
        LogLevel.Warning,
        new EventId(34030, "QdrantInvalidMetadata"),
        "Invalid metadata for document {DocumentId}");

    public static readonly Action<ILogger, string, Exception> LogQdrantDocumentRetrieveFromCollectionFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(34031, "QdrantDocumentRetrieveFromCollectionFailed"),
        "Failed to retrieve document from collection: {Collection}");

    public static readonly Action<ILogger, string, Exception> LogQdrantDocumentCollectionDeleteFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(34032, "QdrantDocumentCollectionDeleteFailed"),
        "Failed to delete document collection: {Collection}");

    public static readonly Action<ILogger, Exception> LogQdrantClearAllFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(34033, "QdrantClearAllFailed"),
        "Failed to clear all documents from Qdrant");

    public static readonly Action<ILogger, Exception?> LogQdrantEmbeddingFallback = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(34034, "QdrantEmbeddingFallback"),
        "AI embedding generation failed, falling back to text search");

    public static readonly Action<ILogger, Exception> LogQdrantVectorSearchFallback = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(34035, "QdrantVectorSearchFallback"),
        "Vector search failed, falling back to text search");
}

