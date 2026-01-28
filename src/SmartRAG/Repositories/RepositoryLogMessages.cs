using Microsoft.Extensions.Logging;
using System;

namespace SmartRAG.Repositories
{
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
    }
}
