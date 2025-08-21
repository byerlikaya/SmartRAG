using Microsoft.Extensions.Logging;

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
}
