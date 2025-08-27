using Microsoft.Extensions.Logging;
using System;

namespace SmartRAG.Repositories;

/// <summary>
/// Centralized LoggerMessage delegates for repository operations
/// </summary>
public static class RepositoryLogMessages
{
    #region Document Operations (EventId: 30001-30999)

    public static readonly Action<ILogger, string, Guid, Exception?> LogDocumentAdded = LoggerMessage.Define<string, Guid>(
        LogLevel.Information,
        new EventId(30001, "DocumentAdded"),
        "Document added successfully: {FileName} (ID: {DocumentId})");

    public static readonly Action<ILogger, string, Exception?> LogDocumentAddFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(30002, "DocumentAddFailed"),
        "Failed to add document: {FileName}");

    public static readonly Action<ILogger, Guid, Exception?> LogDocumentAlreadyExists = LoggerMessage.Define<Guid>(
        LogLevel.Warning,
        new EventId(30003, "DocumentAlreadyExists"),
        "Document with ID {DocumentId} already exists");

    public static readonly Action<ILogger, string, Guid, Exception?> LogDocumentRetrieved = LoggerMessage.Define<string, Guid>(
        LogLevel.Debug,
        new EventId(30004, "DocumentRetrieved"),
        "Document retrieved: {FileName} (ID: {DocumentId})");

    public static readonly Action<ILogger, Guid, Exception?> LogDocumentNotFound = LoggerMessage.Define<Guid>(
        LogLevel.Debug,
        new EventId(30005, "DocumentNotFound"),
        "Document not found: {DocumentId}");

    public static readonly Action<ILogger, Guid, Exception?> LogDocumentRetrievalFailed = LoggerMessage.Define<Guid>(
        LogLevel.Error,
        new EventId(30006, "DocumentRetrievalFailed"),
        "Failed to retrieve document: {DocumentId}");

    public static readonly Action<ILogger, int, Exception?> LogDocumentsRetrieved = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(30007, "DocumentsRetrieved"),
        "Retrieved {Count} documents");

    public static readonly Action<ILogger, Exception?> LogDocumentsRetrievalFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(30008, "DocumentsRetrievalFailed"),
        "Failed to retrieve documents");

    public static readonly Action<ILogger, string, Guid, Exception?> LogDocumentDeleted = LoggerMessage.Define<string, Guid>(
        LogLevel.Information,
        new EventId(30009, "DocumentDeleted"),
        "Document deleted: {FileName} (ID: {DocumentId})");

    public static readonly Action<ILogger, Guid, Exception?> LogDocumentDeleteNotFound = LoggerMessage.Define<Guid>(
        LogLevel.Warning,
        new EventId(30010, "DocumentDeleteNotFound"),
        "Document not found for deletion: {DocumentId}");

    public static readonly Action<ILogger, Guid, Exception?> LogDocumentDeleteFailed = LoggerMessage.Define<Guid>(
        LogLevel.Error,
        new EventId(30011, "DocumentDeleteFailed"),
        "Failed to delete document: {DocumentId}");

    public static readonly Action<ILogger, int, Exception?> LogDocumentCountRetrieved = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(30012, "DocumentCountRetrieved"),
        "Document count retrieved: {Count}");

    public static readonly Action<ILogger, Exception?> LogDocumentCountRetrievalFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(30013, "DocumentCountRetrievalFailed"),
        "Failed to retrieve document count");

    public static readonly Action<ILogger, long, Exception?> LogTotalSizeRetrieved = LoggerMessage.Define<long>(
        LogLevel.Debug,
        new EventId(30014, "TotalSizeRetrieved"),
        "Total size retrieved: {TotalSize} bytes");

    public static readonly Action<ILogger, Exception?> LogTotalSizeRetrievalFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(30015, "TotalSizeRetrievalFailed"),
        "Failed to retrieve total size");

    #endregion

    #region Search Operations (EventId: 31001-31999)

    public static readonly Action<ILogger, string, int, int, Exception?> LogSearchCompleted = LoggerMessage.Define<string, int, int>(
        LogLevel.Information,
        new EventId(31001, "SearchCompleted"),
        "Search completed for query '{Query}': {ResultCount}/{MaxResults} results");

    public static readonly Action<ILogger, string, Exception?> LogSearchFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(31002, "SearchFailed"),
        "Search failed for query '{Query}'");

    #endregion

    #region Metadata Operations (EventId: 32001-32999)

    public static readonly Action<ILogger, Exception?> LogMetadataLoadFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(32001, "MetadataLoadFailed"),
        "Failed to load metadata, returning empty list");

    public static readonly Action<ILogger, int, Exception?> LogMetadataSaved = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(32002, "MetadataSaved"),
        "Metadata saved for {Count} documents");

    public static readonly Action<ILogger, Exception?> LogMetadataSaveFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(32003, "MetadataSaveFailed"),
        "Failed to save metadata");

    #endregion

    #region Qdrant Operations (EventId: 34001-34999)

    public static readonly Action<ILogger, Exception?> LogQdrantCollectionInitFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(34001, "QdrantCollectionInitFailed"),
        "Failed to initialize Qdrant collection");

    public static readonly Action<ILogger, string, string, Guid, Exception?> LogQdrantDocumentCollectionCreating = LoggerMessage.Define<string, string, Guid>(
        LogLevel.Information,
        new EventId(34002, "QdrantDocumentCollectionCreating"),
        "Creating Qdrant document collection: {CollectionName} (Base: {BaseCollection}, Document: {DocumentId})");

    public static readonly Action<ILogger, int, Exception?> LogQdrantEmbeddingsGenerationStarted = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(34003, "QdrantEmbeddingsGenerationStarted"),
        "Started generating embeddings for {ChunkCount} chunks");

    public static readonly Action<ILogger, int, int, Exception?> LogQdrantEmbeddingsProgress = LoggerMessage.Define<int, int>(
        LogLevel.Debug,
        new EventId(34004, "QdrantEmbeddingsProgress"),
        "Generated embeddings for {CurrentChunk}/{TotalChunks} chunks");

    public static readonly Action<ILogger, int, Exception?> LogQdrantEmbeddingsGenerationCompleted = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(34005, "QdrantEmbeddingsGenerationCompleted"),
        "Completed generating embeddings for {ChunkCount} chunks");

    public static readonly Action<ILogger, int, Exception?> LogQdrantPointsCreationStarted = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(34006, "QdrantPointsCreationStarted"),
        "Creating {ChunkCount} Qdrant points");

    public static readonly Action<ILogger, int, int, int, Exception?> LogQdrantBatchUploadProgress = LoggerMessage.Define<int, int, int>(
        LogLevel.Information,
        new EventId(34007, "QdrantBatchUploadProgress"),
        "Uploaded batch {BatchNumber}/{TotalBatches} ({PointCount} points)");

    public static readonly Action<ILogger, string, string, Exception?> LogQdrantDocumentUploadSuccess = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(34008, "QdrantDocumentUploadSuccess"),
        "Document '{FileName}' uploaded successfully to Qdrant collection: {CollectionName}");

    public static readonly Action<ILogger, string, Exception?> LogQdrantDocumentUploadFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(34009, "QdrantDocumentUploadFailed"),
        "Failed to upload document: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogQdrantCollectionCreationFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(34010, "QdrantCollectionCreationFailed"),
        "Failed to create Qdrant collection: {CollectionName}");

    public static readonly Action<ILogger, string, int, Exception?> LogQdrantCollectionCreated = LoggerMessage.Define<string, int>(
        LogLevel.Information,
        new EventId(34011, "QdrantCollectionCreated"),
        "Created Qdrant collection: {CollectionName} with vector dimension: {VectorDimension}");

    public static readonly Action<ILogger, string, Exception?> LogQdrantSearchStarted = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(34012, "QdrantSearchStarted"),
        "Started Qdrant search for query: {Query}");

    public static readonly Action<ILogger, string, int, Exception?> LogQdrantSearchResultsFound = LoggerMessage.Define<string, int>(
        LogLevel.Information,
        new EventId(34013, "QdrantSearchResultsFound"),
        "Found {ResultCount} search results in collection: {CollectionName}");

    public static readonly Action<ILogger, string, Exception?> LogQdrantSearchFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(34014, "QdrantSearchFailed"),
        "Qdrant search failed in collection: {CollectionName}");

    public static readonly Action<ILogger, string, Exception?> LogQdrantFallbackSearchStarted = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(34015, "QdrantFallbackSearchStarted"),
        "Started fallback text search for collection: {CollectionName}");

    public static readonly Action<ILogger, int, Exception?> LogQdrantFallbackSearchResults = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(34016, "QdrantFallbackSearchResults"),
        "Fallback text search found {ChunkCount} chunks");

    public static readonly Action<ILogger, string, Exception?> LogQdrantFallbackSearchFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(34017, "QdrantFallbackSearchFailed"),
        "Fallback text search failed for collection: {CollectionId}");

    public static readonly Action<ILogger, int, Exception?> LogQdrantVectorDimensionDetected = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(34018, "QdrantVectorDimensionDetected"),
        "Detected vector dimension: {VectorDimension} from collection");

    public static readonly Action<ILogger, int, Exception?> LogQdrantDefaultVectorDimensionUsed = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(34019, "QdrantDefaultVectorDimensionUsed"),
        "Using default vector dimension: {VectorDimension}");

    public static readonly Action<ILogger, Exception?> LogQdrantVectorDimensionDetectionFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(34020, "QdrantVectorDimensionDetectionFailed"),
        "Failed to detect vector dimension, using default");

    public static readonly Action<ILogger, string, Exception?> LogQdrantHybridSearchStarted = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(34021, "QdrantHybridSearchStarted"),
        "Started hybrid search for query: {Query}");

    public static readonly Action<ILogger, string, double, Exception?> LogQdrantHybridMatchFound = LoggerMessage.Define<string, double>(
        LogLevel.Debug,
        new EventId(34022, "QdrantHybridMatchFound"),
        "Hybrid match found in {CollectionName} with score {Score:F3}");

    public static readonly Action<ILogger, string, Exception?> LogQdrantHybridSearchFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(34023, "QdrantHybridSearchFailed"),
        "Hybrid search failed for collection: {CollectionName}");

    public static readonly Action<ILogger, int, Exception?> LogQdrantTotalChunksFound = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(34024, "QdrantTotalChunksFound"),
        "Total chunks found across all collections: {ChunkCount}");

    public static readonly Action<ILogger, int, Exception?> LogQdrantFinalResultsReturned = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(34025, "QdrantFinalResultsReturned"),
        "Returning {ChunkCount} chunks to DocumentService for final selection");

    public static readonly Action<ILogger, int, Exception?> LogQdrantUniqueDocumentsFound = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(34026, "QdrantUniqueDocumentsFound"),
        "Repository final unique documents: {DocumentCount}");

    public static readonly Action<ILogger, string, int, Exception?> LogQdrantSearchResultsCached = LoggerMessage.Define<string, int>(
        LogLevel.Information,
        new EventId(34027, "QdrantSearchResultsCached"),
        "Cached search results for query: {Query} (Cache size: {CacheSize})");

    public static readonly Action<ILogger, string, Exception?> LogQdrantVectorSearchFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(34028, "QdrantVectorSearchFailed"),
        "Vector search failed: {ErrorMessage}, falling back to text search");

    public static readonly Action<ILogger, string, Exception?> LogQdrantGlobalFallbackStarted = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(34029, "QdrantGlobalFallbackStarted"),
        "Using global fallback text search for query: {Query}");

    public static readonly Action<ILogger, int, Exception?> LogQdrantGlobalFallbackResults = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(34030, "QdrantGlobalFallbackResults"),
        "Global fallback text search found {ChunkCount} chunks");

    public static readonly Action<ILogger, Exception?> LogQdrantGlobalFallbackFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(34031, "QdrantGlobalFallbackFailed"),
        "Global fallback text search failed");

    #endregion

    #region Redis Operations (EventId: 35001-35999)

    public static readonly Action<ILogger, string, Exception?> LogRedisConnectionEstablished = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(35001, "RedisConnectionEstablished"),
        "Redis connection established to {ConnectionString}");

    public static readonly Action<ILogger, string, Exception?> LogRedisConnectionFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(35002, "RedisConnectionFailed"),
        "Failed to connect to Redis server: {ConnectionString}");

    public static readonly Action<ILogger, string, Guid, Exception?> LogRedisDocumentAdded = LoggerMessage.Define<string, Guid>(
        LogLevel.Information,
        new EventId(35003, "RedisDocumentAdded"),
        "Document added to Redis: {FileName} (ID: {DocumentId})");

    public static readonly Action<ILogger, string, Exception?> LogRedisDocumentAddFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(35004, "RedisDocumentAddFailed"),
        "Failed to add document to Redis: {FileName}");

    public static readonly Action<ILogger, Guid, Exception?> LogRedisDocumentRetrieved = LoggerMessage.Define<Guid>(
        LogLevel.Debug,
        new EventId(35005, "RedisDocumentRetrieved"),
        "Document retrieved from Redis: {DocumentId}");

    public static readonly Action<ILogger, Guid, Exception?> LogRedisDocumentNotFound = LoggerMessage.Define<Guid>(
        LogLevel.Debug,
        new EventId(35006, "RedisDocumentNotFound"),
        "Document not found in Redis: {DocumentId}");

    public static readonly Action<ILogger, Guid, Exception?> LogRedisDocumentRetrievalFailed = LoggerMessage.Define<Guid>(
        LogLevel.Error,
        new EventId(35007, "RedisDocumentRetrievalFailed"),
        "Failed to retrieve document from Redis: {DocumentId}");

    public static readonly Action<ILogger, int, Exception?> LogRedisDocumentsRetrieved = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(35008, "RedisDocumentsRetrieved"),
        "Retrieved {DocumentCount} documents from Redis");

    public static readonly Action<ILogger, Exception?> LogRedisDocumentsRetrievalFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(35009, "RedisDocumentsRetrievalFailed"),
        "Failed to retrieve documents from Redis");

    public static readonly Action<ILogger, Guid, Exception?> LogRedisDocumentDeleted = LoggerMessage.Define<Guid>(
        LogLevel.Information,
        new EventId(35010, "RedisDocumentDeleted"),
        "Document deleted from Redis: {DocumentId}");

    public static readonly Action<ILogger, Guid, Exception?> LogRedisDocumentDeleteFailed = LoggerMessage.Define<Guid>(
        LogLevel.Error,
        new EventId(35011, "RedisDocumentDeleteFailed"),
        "Failed to delete document from Redis: {DocumentId}");

    public static readonly Action<ILogger, int, Exception?> LogRedisDocumentCountRetrieved = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(35012, "RedisDocumentCountRetrieved"),
        "Redis document count retrieved: {Count}");

    public static readonly Action<ILogger, Exception?> LogRedisDocumentCountRetrievalFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(35013, "RedisDocumentCountRetrievalFailed"),
        "Failed to retrieve document count from Redis");

    public static readonly Action<ILogger, string, int, int, Exception?> LogRedisSearchCompleted = LoggerMessage.Define<string, int, int>(
        LogLevel.Information,
        new EventId(35014, "RedisSearchCompleted"),
        "Redis search completed for query '{Query}': {ResultCount}/{MaxResults} results");

    public static readonly Action<ILogger, string, Exception?> LogRedisSearchFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(35015, "RedisSearchFailed"),
        "Redis search failed for query '{Query}'");

    #endregion

    #region SQLite Operations (EventId: 36001-36999)
    public static readonly Action<ILogger, string, Exception?> LogSqliteRepositoryInitialized = LoggerMessage.Define<string>(LogLevel.Information, new EventId(36001, "SqliteRepositoryInitialized"), "SQLite repository initialized successfully with database: {DatabasePath}");
    public static readonly Action<ILogger, string, Exception?> LogSqliteRepositoryInitFailed = LoggerMessage.Define<string>(LogLevel.Error, new EventId(36002, "SqliteRepositoryInitFailed"), "Failed to initialize SQLite repository with database: {DatabasePath}");
    public static readonly Action<ILogger, Exception?> LogSqliteDatabaseInitFailed = LoggerMessage.Define(LogLevel.Error, new EventId(36003, "SqliteDatabaseInitFailed"), "Failed to initialize SQLite database");
    public static readonly Action<ILogger, string, Guid, Exception?> LogSqliteDocumentAdded = LoggerMessage.Define<string, Guid>(LogLevel.Information, new EventId(36004, "SqliteDocumentAdded"), "Document added successfully to SQLite: {FileName} (ID: {DocumentId})");
    public static readonly Action<ILogger, string, Exception?> LogSqliteDocumentAddFailed = LoggerMessage.Define<string>(LogLevel.Error, new EventId(36005, "SqliteDocumentAddFailed"), "Failed to add document to SQLite: {FileName}");
    public static readonly Action<ILogger, Guid, Exception?> LogSqliteDocumentNotFound = LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(36006, "SqliteDocumentNotFound"), "Document not found in SQLite: {DocumentId}");
    public static readonly Action<ILogger, Guid, Exception?> LogSqliteDocumentRetrieved = LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(36007, "SqliteDocumentRetrieved"), "Document retrieved from SQLite: {DocumentId}");
    public static readonly Action<ILogger, Guid, Exception?> LogSqliteDocumentRetrievalFailed = LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(36008, "SqliteDocumentRetrievalFailed"), "Failed to retrieve document from SQLite: {DocumentId}");
    public static readonly Action<ILogger, int, Exception?> LogSqliteDocumentsRetrieved = LoggerMessage.Define<int>(LogLevel.Information, new EventId(36009, "SqliteDocumentsRetrieved"), "Retrieved {Count} documents from SQLite");
    public static readonly Action<ILogger, Exception?> LogSqliteDocumentsRetrievalFailed = LoggerMessage.Define(LogLevel.Error, new EventId(36010, "SqliteDocumentsRetrievalFailed"), "Failed to retrieve documents from SQLite");
    public static readonly Action<ILogger, Guid, Exception?> LogSqliteDocumentDeleted = LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(36011, "SqliteDocumentDeleted"), "Document deleted from SQLite: {DocumentId}");
    public static readonly Action<ILogger, Guid, Exception?> LogSqliteDocumentDeleteFailed = LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(36012, "SqliteDocumentDeleteFailed"), "Failed to delete document from SQLite: {DocumentId}");
    public static readonly Action<ILogger, int, Exception?> LogSqliteDocumentCountRetrieved = LoggerMessage.Define<int>(LogLevel.Information, new EventId(36013, "SqliteDocumentCountRetrieved"), "Document count retrieved from SQLite: {Count}");
    public static readonly Action<ILogger, Exception?> LogSqliteDocumentCountRetrievalFailed = LoggerMessage.Define(LogLevel.Error, new EventId(36014, "SqliteDocumentCountRetrievalFailed"), "Failed to retrieve document count from SQLite");
    public static readonly Action<ILogger, string, int, int, Exception?> LogSqliteSearchCompleted = LoggerMessage.Define<string, int, int>(LogLevel.Information, new EventId(36015, "SqliteSearchCompleted"), "Search completed in SQLite for query '{Query}': {ResultCount}/{MaxResults} results");
    public static readonly Action<ILogger, string, Exception?> LogSqliteSearchFailed = LoggerMessage.Define<string>(LogLevel.Error, new EventId(36016, "SqliteSearchFailed"), "Search failed in SQLite for query '{Query}'");
    public static readonly Action<ILogger, Exception?> LogSqliteStatisticsRetrieved = LoggerMessage.Define(LogLevel.Information, new EventId(36017, "SqliteStatisticsRetrieved"), "Statistics retrieved successfully from SQLite");
    public static readonly Action<ILogger, Exception?> LogSqliteStatisticsRetrievalFailed = LoggerMessage.Define(LogLevel.Error, new EventId(36018, "SqliteStatisticsRetrievalFailed"), "Failed to retrieve statistics from SQLite");
    #endregion

    #region InMemory Operations (EventId: 33001-33999)

    public static readonly Action<ILogger, int, int, Exception?> LogOldDocumentsRemoved = LoggerMessage.Define<int, int>(
        LogLevel.Information,
        new EventId(33001, "OldDocumentsRemoved"),
        "Removed {RemovedCount} old documents to maintain capacity limit of {MaxDocuments}");

    #endregion
}
