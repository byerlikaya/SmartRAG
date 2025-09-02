using Microsoft.Extensions.Logging;
using System;

namespace SmartRAG.Services
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

        public static readonly Action<ILogger, string, Exception> LogDocumentUploadFailed = LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1002, "DocumentUploadFailed"),
            "Failed to upload document: {FileName}");

        public static readonly Action<ILogger, string, Exception> LogDocumentDeleted = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1003, "DocumentDeleted"),
            "Document deleted: {FileName}");

        #endregion

        #region Embedding Operations

        public static readonly Action<ILogger, int, int, Exception> LogChunkEmbeddingSuccess = LoggerMessage.Define<int, int>(
            LogLevel.Debug,
            new EventId(2001, "ChunkEmbeddingSuccess"),
            "Chunk {Index}: Embedding generated ({Dimensions} dimensions)");

        public static readonly Action<ILogger, int, Exception> LogChunkEmbeddingFailed = LoggerMessage.Define<int>(
            LogLevel.Warning,
            new EventId(2002, "ChunkEmbeddingFailed"),
            "Chunk {Index}: Failed to generate embedding");

        public static readonly Action<ILogger, int, Exception> LogChunkProcessingFailed = LoggerMessage.Define<int>(
            LogLevel.Error,
            new EventId(2003, "ChunkProcessingFailed"),
            "Chunk {Index}: Failed to process");

        public static readonly Action<ILogger, int, int, Exception> LogChunkBatchEmbeddingSuccess = LoggerMessage.Define<int, int>(
            LogLevel.Debug,
            new EventId(2004, "ChunkBatchEmbeddingSuccess"),
            "Chunk {Index}: Batch embedding successful ({Dimensions} dimensions)");

        public static readonly Action<ILogger, int, Exception> LogChunkBatchEmbeddingFailed = LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(2005, "ChunkBatchEmbeddingFailed"),
            "Chunk {Index}: Batch embedding failed, trying individual generation");

        public static readonly Action<ILogger, int, int, Exception> LogChunkIndividualEmbeddingSuccess = LoggerMessage.Define<int, int>(
            LogLevel.Debug,
            new EventId(2006, "ChunkIndividualEmbeddingSuccess"),
            "Chunk {Index}: Individual embedding successful ({Dimensions} dimensions)");

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

        public static readonly Action<ILogger, int, int, Exception> LogSearchResults = LoggerMessage.Define<int, int>(
            LogLevel.Debug,
            new EventId(3001, "SearchResults"),
            "Search returned {ChunkCount} chunks from {DocumentCount} documents");

        public static readonly Action<ILogger, int, int, Exception> LogDiverseResults = LoggerMessage.Define<int, int>(
            LogLevel.Debug,
            new EventId(3002, "DiverseResults"),
            "Final diverse results: {ResultCount} chunks from {DocumentCount} documents");

        public static readonly Action<ILogger, Exception> LogGeneralConversationQuery = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(3003, "GeneralConversationQuery"),
            "Detected general conversation query, handling without document search");

        public static readonly Action<ILogger, int, int, Exception> LogSearchInDocuments = LoggerMessage.Define<int, int>(
            LogLevel.Debug,
            new EventId(3004, "SearchInDocuments"),
            "Searching in {DocumentCount} documents with {ChunkCount} chunks");

        public static readonly Action<ILogger, int, Exception> LogEmbeddingSearchSuccessful = LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(3005, "EmbeddingSearchSuccessful"),
            "Embedding search successful, found {ChunkCount} chunks");

        public static readonly Action<ILogger, Exception> LogEmbeddingSearchFailed = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(3006, "EmbeddingSearchFailed"),
            "Embedding search failed, using keyword search");

        public static readonly Action<ILogger, string, Exception> LogQueryWords = LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(3007, "QueryWords"),
            "Query words: [{QueryWords}]");

        public static readonly Action<ILogger, string, Exception> LogPotentialNames = LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(3008, "PotentialNames"),
            "Potential names: [{PotentialNames}]");

        public static readonly Action<ILogger, string, string, Exception> LogFullNameMatch = LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(3009, "FullNameMatch"),
            "Found FULL NAME match: '{FullName}' in chunk: {ChunkPreview}...");

        public static readonly Action<ILogger, string, string, Exception> LogPartialNameMatches = LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(3010, "PartialNameMatches"),
            "Found PARTIAL name matches: [{FoundNames}] in chunk: {ChunkPreview}...");

        public static readonly Action<ILogger, int, Exception> LogRelevantChunksFound = LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(3011, "RelevantChunksFound"),
            "Found {ChunkCount} relevant chunks with enhanced search");

        public static readonly Action<ILogger, int, Exception> LogNameChunksFound = LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(3012, "NameChunksFound"),
            "Found {NameChunkCount} chunks containing names, prioritizing them");

        public static readonly Action<ILogger, Exception> LogNoVoyageAIKey = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(3013, "NoVoyageAIKey"),
            "Embedding search: No VoyageAI API key found");

        public static readonly Action<ILogger, Exception> LogFailedQueryEmbedding = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(3014, "FailedQueryEmbedding"),
            "Embedding search: Failed to generate query embedding");

        public static readonly Action<ILogger, int, Exception> LogChunksContainingQueryTerms = LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(3015, "ChunksContainingQueryTerms"),
            "Embedding search: Found {ChunkCount} chunks containing query terms");

        public static readonly Action<ILogger, Exception> LogNoChunksContainQueryTerms = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(3016, "NoChunksContainQueryTerms"),
            "Embedding search: No chunks contain query terms, using similarity only");

        public static readonly Action<ILogger, Exception> LogEmbeddingSearchFailedError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(3017, "EmbeddingSearchFailedError"),
            "Embedding search failed");

        public static readonly Action<ILogger, int, int, int, Exception> LogRateLimitedRetry = LoggerMessage.Define<int, int, int>(
            LogLevel.Debug,
            new EventId(3018, "RateLimitedRetry"),
            "Embedding generation rate limited, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})");

        public static readonly Action<ILogger, int, Exception> LogRateLimitedAfterAttempts = LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(3019, "RateLimitedAfterAttempts"),
            "Embedding generation rate limited after {MaxRetries} attempts");

        public static readonly Action<ILogger, Exception> LogCanAnswerFromDocumentsError = LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(3020, "CanAnswerFromDocumentsError"),
            "Error in CanAnswerFromDocumentsAsync, assuming document search for safety");

        #endregion

        #region AI Provider Operations

        public static readonly Action<ILogger, Exception> LogPrimaryAIServiceAttempt = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(4001, "PrimaryAIServiceAttempt"),
            "Trying primary AI service for embedding generation");

        public static readonly Action<ILogger, int, Exception> LogPrimaryAIServiceSuccess = LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(4002, "PrimaryAIServiceSuccess"),
            "Primary AI service successful: {Dimensions} dimensions");

        public static readonly Action<ILogger, Exception> LogPrimaryAIServiceNull = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(4003, "PrimaryAIServiceNull"),
            "Primary AI service returned null or empty embedding");

        public static readonly Action<ILogger, Exception> LogPrimaryAIServiceFailed = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(4004, "PrimaryAIServiceFailed"),
            "Primary AI service failed");

        public static readonly Action<ILogger, string, Exception> LogProviderAttempt = LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(4005, "ProviderAttempt"),
            "Trying {Provider} provider for embedding generation");

        public static readonly Action<ILogger, string, int, Exception> LogProviderSuccessful = LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(4006, "ProviderSuccessful"),
            "{Provider} successful: {Dimensions} dimensions");

        public static readonly Action<ILogger, string, Exception> LogProviderFailed = LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(4007, "ProviderFailed"),
            "{Provider} provider failed");

        public static readonly Action<ILogger, Exception> LogAllProvidersFailed = LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(4008, "AllProvidersFailed"),
            "All embedding providers failed");

        public static readonly Action<ILogger, string, string, Exception> LogProviderConfigFound = LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(4009, "ProviderConfigFound"),
            "{Provider} config found, API key: {ApiKeyPreview}...");

        public static readonly Action<ILogger, string, Exception> LogProviderReturnedNull = LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(4010, "ProviderReturnedNull"),
            "{Provider} returned null or empty embedding");

        public static readonly Action<ILogger, string, Exception> LogProviderConfigNotFound = LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(4011, "ProviderConfigNotFound"),
            "{Provider} config not found or API key missing");

        public static readonly Action<ILogger, string, Exception> LogAllProvidersFailedText = LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(4012, "AllProvidersFailedText"),
            "All embedding providers failed for text: {TextPreview}...");

        public static readonly Action<ILogger, string, Exception> LogTestingVoyageAI = LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(4013, "TestingVoyageAI"),
            "Testing VoyageAI directly with key: {ApiKeyPreview}...");

        public static readonly Action<ILogger, int, string, Exception> LogVoyageAITestResponse = LoggerMessage.Define<int, string>(
            LogLevel.Debug,
            new EventId(4014, "VoyageAITestResponse"),
            "VoyageAI test response: {StatusCode} - {Response}");

        public static readonly Action<ILogger, Exception> LogVoyageAIWorking = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(4015, "VoyageAIWorking"),
            "VoyageAI is working! Trying to parse embedding...");

        public static readonly Action<ILogger, int, Exception> LogVoyageAITestEmbedding = LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(4016, "VoyageAITestEmbedding"),
            "VoyageAI test embedding generated: {Dimensions} dimensions");

        public static readonly Action<ILogger, Exception> LogFailedParseVoyageAI = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(4017, "FailedParseVoyageAI"),
            "Failed to parse VoyageAI response");

        public static readonly Action<ILogger, Exception> LogVoyageAIDirectTestFailed = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(4018, "VoyageAIDirectTestFailed"),
            "VoyageAI direct test failed");

        #endregion

        #region Batch Operations

        public static readonly Action<ILogger, int, Exception> LogBatchProcessing = LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(5001, "BatchProcessing"),
            "Processing {BatchSize} chunks in batch");

        public static readonly Action<ILogger, int, Exception> LogBatchCompleted = LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(5002, "BatchCompleted"),
            "Batch completed: {ProcessedCount} chunks processed");

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

        public static readonly Action<ILogger, int, Exception> LogIndividualEmbeddingGeneration = LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(6005, "IndividualEmbeddingGeneration"),
            "Generating individual embeddings for {TextCount} texts");

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
            new EventId(6006, "DocumentProcessing"),
            "Document: {FileName} ({ChunkCount} chunks)");



        public static readonly Action<ILogger, Guid, Exception> LogChunkBatchEmbeddingFailedRetry = LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(6008, "ChunkBatchEmbeddingFailedRetry"),
            "Chunk {ChunkId}: Batch embedding failed, trying individual generation");

        public static readonly Action<ILogger, Guid, int, Exception> LogChunkIndividualEmbeddingSuccessRetry = LoggerMessage.Define<Guid, int>(
            LogLevel.Debug,
            new EventId(6009, "ChunkIndividualEmbeddingSuccessRetry"),
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

        #region Semantic Search Service (EventId: 21001-21999)

        public static readonly Action<ILogger, Exception> LogSemanticSimilarityCalculationError = LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(21001, "SemanticSimilarityCalculationError"),
            "Failed to calculate enhanced semantic similarity");

        #endregion
    }
}
