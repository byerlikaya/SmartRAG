using Microsoft.Extensions.Logging;
using System;

namespace SmartRAG.Services.Shared
{
    /// <summary>
    /// Centralized LoggerMessage delegates for performance optimization
    /// </summary>
    public static class ServiceLogMessages
    {
        #region Document Operations

        public static readonly Action<ILogger, string, Exception> LogDocumentUploaded = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1001, "DocumentUploaded"),
            "Document uploaded successfully: {FileName}");

        #endregion

        #region Embedding Operations

        public static readonly Action<ILogger, int, Exception> LogChunkEmbeddingFailed = LoggerMessage.Define<int>(
            LogLevel.Warning,
            new EventId(2002, "ChunkEmbeddingFailed"),
            "Chunk {Index}: Failed to generate embedding");

        public static readonly Action<ILogger, int, int, Exception> LogChunkBatchEmbeddingSuccess = LoggerMessage.Define<int, int>(
            LogLevel.Debug,
            new EventId(2004, "ChunkBatchEmbeddingSuccess"),
            "Chunk {Index}: Batch embedding successful ({Dimensions} dimensions)");

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

        #endregion

        #region Search Operations

        public static readonly Action<ILogger, Exception> LogGeneralConversationQuery = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(3003, "GeneralConversationQuery"),
            "Detected general conversation query, handling without document search");

        public static readonly Action<ILogger, string, string, Exception> LogFullNameMatch = LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(3009, "FullNameMatch"),
            "Found FULL NAME match: '{FullName}' in chunk: {ChunkPreview}...");

        public static readonly Action<ILogger, string, string, Exception> LogPartialNameMatches = LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(3010, "PartialNameMatches"),
            "Found PARTIAL name matches: [{FoundNames}] in chunk: {ChunkPreview}...");

        public static readonly Action<ILogger, Exception> LogEmbeddingSearchFailedError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(3017, "EmbeddingSearchFailedError"),
            "Embedding search failed");

        public static readonly Action<ILogger, Exception> LogCanAnswerFromDocumentsError = LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(3020, "CanAnswerFromDocumentsError"),
            "Error in CanAnswerFromDocumentsAsync, assuming document search for safety");

        public static readonly Action<ILogger, int, int, Exception> LogContextExpansionCompleted = LoggerMessage.Define<int, int>(
            LogLevel.Debug,
            new EventId(3021, "ContextExpansionCompleted"),
            "Context expansion completed: {OriginalCount} chunks expanded to {ExpandedCount} chunks");

        public static readonly Action<ILogger, Exception> LogContextExpansionError = LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(3022, "ContextExpansionError"),
            "Error during context expansion, returning original chunks");

        public static readonly Action<ILogger, int, Exception> LogContextExpansionLimited = LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(3023, "ContextExpansionLimited"),
            "Context expansion limited to {MaxChunks} chunks to prevent timeout");

        public static readonly Action<ILogger, int, int, int, Exception> LogContextSizeLimited = LoggerMessage.Define<int, int, int>(
            LogLevel.Information,
            new EventId(3024, "ContextSizeLimited"),
            "Context size limited: {ChunkCount} chunks, {ActualSize} chars (max: {MaxSize} chars)");

        public static readonly Action<ILogger, int, Exception> LogFallbackSearchUsed = LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(3025, "FallbackSearchUsed"),
            "Fallback keyword search used, found {ChunkCount} chunks");

        #endregion

        #region Batch Operations

        public static readonly Action<ILogger, int, Exception> LogBatchProcessing = LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(5001, "BatchProcessing"),
            "Processing {BatchSize} chunks in batch");

        public static readonly Action<ILogger, int, int, Exception> LogBatchProgress = LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(5003, "BatchProgress"),
            "Processing batch {BatchNumber}/{TotalBatches}");

        public static readonly Action<ILogger, int, Exception> LogBatchFailed = LoggerMessage.Define<int>(
            LogLevel.Warning,
            new EventId(5004, "BatchFailed"),
            "Batch {BatchNumber} failed, processing individually");

        #endregion

        #region Progress and Status

        public static readonly Action<ILogger, int, int, int, Exception> LogProgress = LoggerMessage.Define<int, int, int>(
            LogLevel.Information,
            new EventId(6001, "Progress"),
            "Progress: {ProcessedChunks}/{TotalChunks} chunks processed, {SuccessCount} embeddings generated");

        public static readonly Action<ILogger, int, Exception> LogSavingDocuments = LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(6002, "SavingDocuments"),
            "Saving {DocumentCount} documents with updated embeddings");

        public static readonly Action<ILogger, int, int, Exception> LogTotalChunksToProcess = LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(6003, "TotalChunksToProcess"),
            "Total chunks to process: {ProcessCount} out of {TotalChunks}");

        public static readonly Action<ILogger, Exception> LogNoProcessingNeeded = LoggerMessage.Define(
            LogLevel.Information,
            new EventId(6004, "NoProcessingNeeded"),
            "All chunks already have valid embeddings. No processing needed.");

        public static readonly Action<ILogger, int, Exception> LogBatchEmbeddingAttempt = LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(6006, "BatchEmbeddingAttempt"),
            "Attempting batch embedding generation for {Count} texts");

        public static readonly Action<ILogger, int, Exception> LogBatchEmbeddingSuccess = LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(6007, "BatchEmbeddingSuccess"),
            "Batch embedding successful for {Count} texts");

        public static readonly Action<ILogger, int, int, Exception> LogBatchEmbeddingIncomplete = LoggerMessage.Define<int, int>(
            LogLevel.Warning,
            new EventId(6008, "BatchEmbeddingIncomplete"),
            "Batch embedding incomplete: got {ActualCount}/{ExpectedCount} embeddings");

        public static readonly Action<ILogger, string, Exception> LogBatchEmbeddingFailed = LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(6009, "BatchEmbeddingFailed"),
            "Batch embedding failed: {ErrorMessage}");

        public static readonly Action<ILogger, string, int, Exception> LogDocumentProcessing = LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(6020, "DocumentProcessing"),
            "Document: {FileName} ({ChunkCount} chunks)");

        public static readonly Action<ILogger, Guid, Exception> LogChunkBatchEmbeddingFailedRetry = LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(6021, "ChunkBatchEmbeddingFailedRetry"),
            "Chunk {ChunkId}: Batch embedding failed, trying individual generation");

        public static readonly Action<ILogger, Guid, int, Exception> LogChunkIndividualEmbeddingSuccessRetry = LoggerMessage.Define<Guid, int>(
            LogLevel.Debug,
            new EventId(6022, "ChunkIndividualEmbeddingSuccessRetry"),
            "Chunk {ChunkId}: Individual embedding successful ({Dimensions} dimensions)");

        public static readonly Action<ILogger, Guid, Exception> LogChunkAllEmbeddingMethodsFailed = LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(6010, "ChunkAllEmbeddingMethodsFailed"),
            "Chunk {ChunkId}: All embedding methods failed");

        public static readonly Action<ILogger, Guid, int, Exception> LogChunkIndividualEmbeddingSuccessFinal = LoggerMessage.Define<Guid, int>(
            LogLevel.Debug,
            new EventId(6011, "ChunkIndividualEmbeddingSuccessFinal"),
            "Chunk {ChunkId}: Individual embedding successful ({Dimensions} dimensions)");

        public static readonly Action<ILogger, Guid, Exception> LogChunkEmbeddingGenerationFailed = LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(6012, "ChunkEmbeddingGenerationFailed"),
            "Chunk {ChunkId}: Failed to generate embedding");

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

        #endregion

        #region AI Service (EventId: 20001-20999)

        public static readonly Action<ILogger, string, Exception> LogAIServiceGenerateResponseError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(20001, "AIServiceGenerateResponseError"),
            "Error in GenerateResponseAsync for provider {Provider}");

        public static readonly Action<ILogger, string, Exception> LogAIServiceFallbackError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(20002, "AIServiceFallbackError"),
            "Fallback providers also failed for query: {Query}");

        public static readonly Action<ILogger, string, Exception> LogAIServiceProviderConfigNotFound = LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(20003, "AIServiceProviderConfigNotFound"),
            "Provider config not found for {Provider}");

        public static readonly Action<ILogger, string, Exception> LogAIServiceEmbeddingError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(20004, "AIServiceEmbeddingError"),
            "Error generating embeddings for text: {Text}");

        public static readonly Action<ILogger, int, string, Exception> LogAIServiceBatchEmbeddingsGenerated = LoggerMessage.Define<int, string>(
            LogLevel.Information,
            new EventId(20005, "AIServiceBatchEmbeddingsGenerated"),
            "Generated {Count} valid embeddings from {Provider}");

        public static readonly Action<ILogger, string, Exception> LogAIServiceBatchEmbeddingError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(20006, "AIServiceBatchEmbeddingError"),
            "Error generating batch embeddings from {Provider}");

        public static readonly Action<ILogger, int, string, int, Exception> LogAIServiceRetryAttempt = LoggerMessage.Define<int, string, int>(
            LogLevel.Warning,
            new EventId(20007, "AIServiceRetryAttempt"),
            "Attempt {Attempt} failed for provider {Provider}, retrying in {Delay}ms");

        public static readonly Action<ILogger, string, Exception> LogAIServiceFallbackSuccess = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(20008, "AIServiceFallbackSuccess"),
            "Fallback provider {Provider} succeeded");

        public static readonly Action<ILogger, string, Exception> LogAIServiceFallbackFailed = LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(20009, "AIServiceFallbackFailed"),
            "Fallback provider {Provider} failed");

        public static readonly Action<ILogger, string, Exception> LogAIServiceAllFallbacksFailed = LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(20010, "AIServiceAllFallbacksFailed"),
            "All fallback providers failed for query: {Query}");

        #endregion

        #region Session Management (EventId: 50001-50999)

        public static readonly Action<ILogger, string, Exception> LogSessionCreated = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(50001, "SessionCreated"),
            "New session created: {SessionId}");

        public static readonly Action<ILogger, string, Exception> LogSessionRetrieved = LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(50002, "SessionRetrieved"),
            "Session retrieved: {SessionId}");

        public static readonly Action<ILogger, string, Exception> LogConversationRetrievalFailed = LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(50003, "ConversationRetrievalFailed"),
            "Failed to retrieve conversation for session: {SessionId}");

        public static readonly Action<ILogger, string, Exception> LogConversationStorageFailed = LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(50004, "ConversationStorageFailed"),
            "Failed to store conversation for session: {SessionId}");

        #endregion

        #region Semantic Search Service (EventId: 21001-21999)

        public static readonly Action<ILogger, Exception> LogSemanticSimilarityCalculationError = LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(21001, "SemanticSimilarityCalculationError"),
            "Failed to calculate enhanced semantic similarity");

        #endregion

        #region Image Processing and OCR Operations (EventId: 70001-70999)

        public static readonly Action<ILogger, int, Exception> LogImageOcrSuccess = LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(70002, "ImageOcrSuccess"),
            "OCR completed successfully, extracted {TextLength} characters");

        public static readonly Action<ILogger, Exception> LogImageOcrFailed = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(70003, "ImageOcrFailed"),
            "OCR processing failed");

        public static readonly Action<ILogger, string, Exception> LogOcrDataPathNotFound = LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(70004, "OcrDataPathNotFound"),
            "OCR engine data path not found at: {Path}");

        public static readonly Action<ILogger, string, Exception> LogOcrDataPathFound = LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(70005, "OcrDataPathFound"),
            "OCR engine data path found at: {Path}");

        public static readonly Action<ILogger, int, Exception> LogImageProcessingStarted = LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(70008, "ImageProcessingStarted"),
            "Starting image processing for {ImageCount} images");

        public static readonly Action<ILogger, int, int, Exception> LogImageProcessingCompleted = LoggerMessage.Define<int, int>(
            LogLevel.Debug,
            new EventId(70009, "ImageProcessingCompleted"),
            "Image processing completed: {SuccessCount}/{TotalCount} images processed successfully");

        public static readonly Action<ILogger, Exception> LogImageProcessingFailed = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(70010, "ImageProcessingFailed"),
            "Image processing failed");

        #endregion

    }
}
