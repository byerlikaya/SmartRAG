namespace SmartRAG.Services.Shared;


/// <summary>
/// Centralized LoggerMessage delegates for performance optimization
/// </summary>
public static class ServiceLogMessages
{
    public static readonly Action<ILogger, string, Exception> LogDocumentUploaded = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(1001, "DocumentUploaded"),
        "Document uploaded successfully: {FileName}");

    public static readonly Action<ILogger, int, Exception> LogChunkEmbeddingFailed = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(2002, "ChunkEmbeddingFailed"),
        "Chunk {Index}: Failed to generate embedding");

    public static readonly Action<ILogger, Exception> LogEmbeddingRegenerationStarted = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(2007, "EmbeddingRegenerationStarted"),
        "Starting embedding regeneration for all documents");

    public static readonly Action<ILogger, int, int, Exception> LogEmbeddingRegenerationCompleted = LoggerMessage.Define<int, int>(
        LogLevel.Information,
        new EventId(2008, "EmbeddingRegenerationCompleted"),
        "Embedding regeneration completed: {SuccessCount}/{TotalCount} chunks");

    public static readonly Action<ILogger, Exception> LogEmbeddingRegenerationFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(2009, "EmbeddingRegenerationFailed"),
        "Failed to regenerate embeddings");

    public static readonly Action<ILogger, Exception> LogCanAnswerFromDocumentsError = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(3020, "CanAnswerFromDocumentsError"),
        "Error in CanAnswerFromDocumentsAsync, assuming document search for safety");

    public static readonly Action<ILogger, Exception> LogContextExpansionError = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(3022, "ContextExpansionError"),
        "Error during context expansion, returning original chunks");

    public static readonly Action<ILogger, int, Exception> LogBatchFailed = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(5004, "BatchFailed"),
        "Batch {BatchNumber} failed, processing individually");

    public static readonly Action<ILogger, int, int, Exception> LogBatchEmbeddingIncomplete = LoggerMessage.Define<int, int>(
        LogLevel.Warning,
        new EventId(6008, "BatchEmbeddingIncomplete"),
        "Batch embedding incomplete: got {ActualCount}/{ExpectedCount} embeddings");

    public static readonly Action<ILogger, string, Exception> LogBatchEmbeddingFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(6009, "BatchEmbeddingFailed"),
        "Batch embedding failed: {ErrorMessage}");

    public static readonly Action<ILogger, Guid, Exception> LogChunkEmbeddingRegenerationFailed = LoggerMessage.Define<Guid>(
        LogLevel.Error,
        new EventId(6013, "ChunkEmbeddingRegenerationFailed"),
        "Chunk {ChunkId}: Failed to regenerate embedding");

    public static readonly Action<ILogger, Exception> LogDocumentDeletionStarted = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(6014, "DocumentDeletionStarted"),
        "Starting deletion of all documents");

    public static readonly Action<ILogger, int, int, Exception> LogDocumentDeletionCompleted = LoggerMessage.Define<int, int>(
        LogLevel.Information,
        new EventId(6015, "DocumentDeletionCompleted"),
        "Document deletion completed: {DeletedCount}/{TotalCount} documents deleted");

    public static readonly Action<ILogger, Exception> LogDocumentDeletionFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(6016, "DocumentDeletionFailed"),
        "Failed to delete documents");

    public static readonly Action<ILogger, Exception> LogEmbeddingClearingStarted = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(6017, "EmbeddingClearingStarted"),
        "Starting clearing of all embeddings");

    public static readonly Action<ILogger, int, Exception> LogEmbeddingClearingCompleted = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(6018, "EmbeddingClearingCompleted"),
        "Embedding clearing completed: {ProcessedCount} documents processed");

    public static readonly Action<ILogger, Exception> LogEmbeddingClearingFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(6019, "EmbeddingClearingFailed"),
        "Failed to clear embeddings");

    public static readonly Action<ILogger, string, Exception> LogAIServiceGenerateResponseError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(20001, "AIServiceGenerateResponseError"),
        "Error in GenerateResponseAsync for provider {Provider}");

    public static readonly Action<ILogger, string, Exception> LogAIServiceProviderConfigNotFound = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(20003, "AIServiceProviderConfigNotFound"),
        "Provider config not found for {Provider}");

    public static readonly Action<ILogger, string, Exception> LogAIServiceBatchEmbeddingError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(20006, "AIServiceBatchEmbeddingError"),
        "Error generating batch embeddings from {Provider}");

    public static readonly Action<ILogger, int, string, int, Exception> LogAIServiceRetryAttempt = LoggerMessage.Define<int, string, int>(
        LogLevel.Warning,
        new EventId(20007, "AIServiceRetryAttempt"),
        "Attempt {Attempt} failed for provider {Provider}, retrying in {Delay}ms");

    public static readonly Action<ILogger, string, Exception> LogAIServiceFallbackFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(20009, "AIServiceFallbackFailed"),
        "Fallback provider {Provider} failed");

    public static readonly Action<ILogger, string, Exception> LogConversationRetrievalFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(50003, "ConversationRetrievalFailed"),
        "Failed to retrieve conversation for session: {SessionId}");

    public static readonly Action<ILogger, string, Exception> LogConversationStorageFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(50004, "ConversationStorageFailed"),
        "Failed to store conversation for session: {SessionId}");

    public static readonly Action<ILogger, Exception> LogImageOcrFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(70003, "ImageOcrFailed"),
        "OCR processing failed");

    public static readonly Action<ILogger, string, Exception> LogOcrDataPathNotFound = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(70004, "OcrDataPathNotFound"),
        "OCR engine data path not found at: {Path}");

    public static readonly Action<ILogger, Exception> LogImageProcessingFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(70010, "ImageProcessingFailed"),
        "Image processing failed");
}

