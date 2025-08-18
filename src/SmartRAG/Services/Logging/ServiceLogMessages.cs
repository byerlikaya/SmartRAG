using Microsoft.Extensions.Logging;

namespace SmartRAG.Services.Logging;

/// <summary>
/// Centralized LoggerMessage delegates for all services
/// </summary>
public static class ServiceLogMessages
{
    #region DocumentService Log Messages

    public static readonly Action<ILogger, string, Exception?> LogDocumentUploaded = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(1001, "DocumentUploaded"),
        "Document uploaded successfully: {FileName}");

    public static readonly Action<ILogger, string, int, Exception?> LogDocumentsUploaded = LoggerMessage.Define<string, int>(
        LogLevel.Information,
        new EventId(1002, "DocumentsUploaded"),
        "Multiple documents uploaded successfully: {FileName} ({Count} total)");

    public static readonly Action<ILogger, string, Exception?> LogDocumentDeleted = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(1003, "DocumentDeleted"),
        "Document deleted successfully: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogEmbeddingsRegenerated = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(1004, "EmbeddingsRegenerated"),
        "Embeddings regenerated for document: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogDocumentNotFound = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1005, "DocumentNotFound"),
        "Document not found: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogDocumentParseError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1006, "DocumentParseError"),
        "Error parsing document: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogEmbeddingGenerationError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1007, "EmbeddingGenerationError"),
        "Error generating embeddings for document: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogDocumentUploadError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1008, "DocumentUploadError"),
        "Error uploading document: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogDocumentDeleteError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1009, "DocumentDeleteError"),
        "Error deleting document: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogEmbeddingsRegenerationError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1010, "EmbeddingsRegenerationError"),
        "Error regenerating embeddings for document: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogEmbeddingsRegenerationStarted = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(1011, "EmbeddingsRegenerationStarted"),
        "Started regenerating embeddings for document: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogEmbeddingsRegenerationCompleted = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(1012, "EmbeddingsRegenerationCompleted"),
        "Completed regenerating embeddings for document: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogEmbeddingsRegenerationSkipped = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1013, "EmbeddingsRegenerationSkipped"),
        "Skipped regenerating embeddings for document: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogEmbeddingsRegenerationFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1014, "EmbeddingsRegenerationFailed"),
        "Failed to regenerate embeddings for document: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogEmbeddingsRegenerationProgress = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(1015, "EmbeddingsRegenerationProgress"),
        "Embeddings regeneration progress: {FileName}");

    public static readonly Action<ILogger, string, Exception?> LogEmbeddingsRegenerationSuccess = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(1016, "EmbeddingsRegenerationSuccess"),
        "Embeddings regeneration successful: {FileName}");

    // Chunk processing log messages
    public static readonly Action<ILogger, int, int, Exception?> LogChunkEmbeddingSuccess = LoggerMessage.Define<int, int>(
        LogLevel.Debug,
        new EventId(1017, "ChunkEmbeddingSuccess"),
        "Chunk {ChunkIndex}: Embedding generated successfully ({Dimensions} dimensions)");

    public static readonly Action<ILogger, int, Exception?> LogChunkBatchEmbeddingFailed = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(1018, "ChunkBatchEmbeddingFailed"),
        "Chunk {ChunkIndex}: Batch embedding failed, trying individual generation");

    public static readonly Action<ILogger, int, int, Exception?> LogChunkIndividualEmbeddingSuccess = LoggerMessage.Define<int, int>(
        LogLevel.Debug,
        new EventId(1019, "ChunkIndividualEmbeddingSuccess"),
        "Chunk {ChunkIndex}: Individual embedding successful ({Dimensions} dimensions)");

    public static readonly Action<ILogger, int, Exception?> LogChunkEmbeddingFailed = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(1020, "ChunkEmbeddingFailed"),
        "Chunk {ChunkIndex}: Failed to generate embedding after retry");

    public static readonly Action<ILogger, int, Exception?> LogChunkProcessingFailed = LoggerMessage.Define<int>(
        LogLevel.Error,
        new EventId(1021, "ChunkProcessingFailed"),
        "Chunk {ChunkIndex}: Failed to process");

    public static readonly Action<ILogger, string, Exception?> LogDocumentUploadFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1022, "DocumentUploadFailed"),
        "Failed to upload document {FileName}");

    public static readonly Action<ILogger, Exception?> LogEmbeddingRegenerationStarted = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(1023, "EmbeddingRegenerationStarted"),
        "Starting embedding regeneration for all documents...");

    public static readonly Action<ILogger, string, int, Exception?> LogDocumentProcessing = LoggerMessage.Define<string, int>(
        LogLevel.Information,
        new EventId(1024, "DocumentProcessing"),
        "Document: {FileName} ({ChunkCount} chunks)");

    public static readonly Action<ILogger, int, int, Exception?> LogTotalChunksToProcess = LoggerMessage.Define<int, int>(
        LogLevel.Information,
        new EventId(1025, "TotalChunksToProcess"),
        "Total chunks to process: {ProcessCount} out of {TotalChunks}");

    public static readonly Action<ILogger, Exception?> LogNoProcessingNeeded = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(1026, "NoProcessingNeeded"),
        "All chunks already have valid embeddings. No processing needed.");

    public static readonly Action<ILogger, int, int, Exception?> LogBatchProcessing = LoggerMessage.Define<int, int>(
        LogLevel.Information,
        new EventId(1027, "BatchProcessing"),
        "Processing in {TotalBatches} batches of {BatchSize} chunks");

    public static readonly Action<ILogger, int, int, int, int, Exception?> LogBatchProgress = LoggerMessage.Define<int, int, int, int>(
        LogLevel.Information,
        new EventId(1028, "BatchProgress"),
        "Processing batch {BatchNumber}/{TotalBatches}: chunks {StartIndex}-{EndIndex}");

    public static readonly Action<ILogger, Guid, int, Exception?> LogChunkBatchEmbeddingSuccess = LoggerMessage.Define<Guid, int>(
        LogLevel.Debug,
        new EventId(1029, "ChunkBatchEmbeddingSuccess"),
        "Chunk {ChunkId}: Batch embedding successful ({Dimensions} dimensions)");

    public static readonly Action<ILogger, Guid, Exception?> LogChunkBatchEmbeddingFailedRetry = LoggerMessage.Define<Guid>(
        LogLevel.Warning,
        new EventId(1030, "ChunkBatchEmbeddingFailedRetry"),
        "Chunk {ChunkId}: Batch embedding failed, trying individual generation");

    public static readonly Action<ILogger, Guid, int, Exception?> LogChunkIndividualEmbeddingSuccessRetry = LoggerMessage.Define<Guid, int>(
        LogLevel.Debug,
        new EventId(1031, "ChunkIndividualEmbeddingSuccessRetry"),
        "Chunk {ChunkId}: Individual embedding successful ({Dimensions} dimensions)");

    public static readonly Action<ILogger, Guid, Exception?> LogChunkAllEmbeddingMethodsFailed = LoggerMessage.Define<Guid>(
        LogLevel.Warning,
        new EventId(1032, "ChunkAllEmbeddingMethodsFailed"),
        "Chunk {ChunkId}: All embedding methods failed");

    public static readonly Action<ILogger, int, Exception?> LogBatchFailed = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(1033, "BatchFailed"),
        "Batch {BatchNumber} failed, processing individually");

    public static readonly Action<ILogger, Guid, int, Exception?> LogChunkIndividualEmbeddingSuccessFinal = LoggerMessage.Define<Guid, int>(
        LogLevel.Debug,
        new EventId(1034, "ChunkIndividualEmbeddingSuccessFinal"),
        "Chunk {ChunkId}: Individual embedding successful ({Dimensions} dimensions)");

    public static readonly Action<ILogger, Guid, Exception?> LogChunkEmbeddingGenerationFailed = LoggerMessage.Define<Guid>(
        LogLevel.Warning,
        new EventId(1035, "ChunkEmbeddingGenerationFailed"),
        "Chunk {ChunkId}: Failed to generate embedding");

    public static readonly Action<ILogger, Guid, Exception?> LogChunkEmbeddingRegenerationFailed = LoggerMessage.Define<Guid>(
        LogLevel.Error,
        new EventId(1036, "ChunkEmbeddingRegenerationFailed"),
        "Chunk {ChunkId}: Failed to regenerate embedding");

    public static readonly Action<ILogger, int, int, int, Exception?> LogProgress = LoggerMessage.Define<int, int, int>(
        LogLevel.Information,
        new EventId(1037, "Progress"),
        "Progress: {ProcessedChunks}/{TotalChunks} chunks processed, {SuccessCount} embeddings generated");

    public static readonly Action<ILogger, int, Exception?> LogSavingDocuments = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(1038, "SavingDocuments"),
        "Saving {DocumentCount} documents with updated embeddings...");

    public static readonly Action<ILogger, int, int, int, Exception?> LogEmbeddingRegenerationCompleted = LoggerMessage.Define<int, int, int>(
        LogLevel.Information,
        new EventId(1039, "EmbeddingRegenerationCompleted"),
        "Embedding regeneration completed. {SuccessCount} embeddings generated for {ProcessedChunks} chunks in {TotalBatches} batches.");

    public static readonly Action<ILogger, Exception?> LogEmbeddingRegenerationFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1040, "EmbeddingRegenerationFailed"),
        "Failed to regenerate embeddings");

    #endregion

    #region DocumentSearchService Log Messages

    public static readonly Action<ILogger, int, int, Exception?> LogSearchResults = LoggerMessage.Define<int, int>(
        LogLevel.Debug,
        new EventId(2001, "SearchResults"),
        "Search returned {ChunkCount} chunks from {DocumentCount} documents");

    public static readonly Action<ILogger, int, int, Exception?> LogDiverseResults = LoggerMessage.Define<int, int>(
        LogLevel.Debug,
        new EventId(2002, "DiverseResults"),
        "Final diverse results: {ResultCount} chunks from {DocumentCount} documents");

    public static readonly Action<ILogger, Exception?> LogGeneralConversationQuery = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(2003, "GeneralConversationQuery"),
        "Detected general conversation query, handling without document search");

    public static readonly Action<ILogger, Exception?> LogPrimaryAIServiceAttempt = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(2004, "PrimaryAIServiceAttempt"),
        "Trying primary AI service for embedding generation");

    public static readonly Action<ILogger, int, Exception?> LogPrimaryAIServiceSuccess = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2005, "PrimaryAIServiceSuccess"),
        "Primary AI service successful: {Dimensions} dimensions");

    public static readonly Action<ILogger, Exception?> LogPrimaryAIServiceNull = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(2006, "PrimaryAIServiceNull"),
        "Primary AI service returned null or empty embedding");

    public static readonly Action<ILogger, Exception?> LogPrimaryAIServiceFailed = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(2007, "PrimaryAIServiceFailed"),
        "Primary AI service failed");

    public static readonly Action<ILogger, string, Exception?> LogFallbackProviderAttempt = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2008, "FallbackProviderAttempt"),
        "Trying fallback provider: {Provider}");

    public static readonly Action<ILogger, string, int, Exception?> LogFallbackProviderSuccess = LoggerMessage.Define<string, int>(
        LogLevel.Debug,
        new EventId(2009, "FallbackProviderSuccess"),
        "Fallback provider {Provider} successful: {Dimensions} dimensions");

    public static readonly Action<ILogger, string, Exception?> LogFallbackProviderFailed = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2010, "FallbackProviderFailed"),
        "Fallback provider {Provider} failed");

    public static readonly Action<ILogger, Exception?> LogAllProvidersFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(2011, "AllProvidersFailed"),
        "All embedding providers failed");

    public static readonly Action<ILogger, int, Exception?> LogBatchEmbeddingGeneration = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2012, "BatchEmbeddingGeneration"),
        "Generating embeddings for {TextCount} texts in batch");

    public static readonly Action<ILogger, int, Exception?> LogBatchEmbeddingSuccess = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2013, "BatchEmbeddingSuccess"),
        "Batch embedding successful: {TextCount} embeddings generated");

    public static readonly Action<ILogger, int, Exception?> LogBatchEmbeddingPartial = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(2014, "BatchEmbeddingPartial"),
        "Batch embedding partially successful: {TextCount} embeddings generated");

    public static readonly Action<ILogger, Exception?> LogBatchEmbeddingFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(2015, "BatchEmbeddingFailed"),
        "Batch embedding failed, falling back to individual generation");

    public static readonly Action<ILogger, int, Exception?> LogIndividualEmbeddingGeneration = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2016, "IndividualEmbeddingGeneration"),
        "Generating individual embeddings for {TextCount} texts");

    public static readonly Action<ILogger, int, Exception?> LogIndividualEmbeddingSuccess = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2017, "IndividualEmbeddingSuccess"),
        "Individual embedding successful: {TextCount} embeddings generated");

    public static readonly Action<ILogger, string, Exception?> LogQueryIntentDetection = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2018, "QueryIntentDetection"),
        "Analyzing query intent for: {Query}");

    public static readonly Action<ILogger, string, Exception?> LogQueryIntentResult = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2019, "QueryIntentResult"),
        "Query intent detected as: {Intent}");

    public static readonly Action<ILogger, string, Exception?> LogGeneralConversationHandling = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2020, "GeneralConversationHandling"),
        "Handling general conversation query: {Query}");

    public static readonly Action<ILogger, string, Exception?> LogGeneralConversationResponse = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2021, "GeneralConversationResponse"),
        "General conversation response generated: {Response}");

    public static readonly Action<ILogger, string, Exception?> LogBasicSearchQuery = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2022, "BasicSearchQuery"),
        "Performing basic search for query: {Query}");

    public static readonly Action<ILogger, int, Exception?> LogBasicSearchResults = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2023, "BasicSearchResults"),
        "Basic search returned {ChunkCount} chunks");

    public static readonly Action<ILogger, string, Exception?> LogBasicRagQuery = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2024, "BasicRagQuery"),
        "Generating basic RAG answer for query: {Query}");

    public static readonly Action<ILogger, int, Exception?> LogBasicRagResults = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2025, "BasicRagResults"),
        "Basic RAG generated answer with {SourceCount} sources");

    public static readonly Action<ILogger, string, Exception?> LogVoyageAIBatchAttempt = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2026, "VoyageAIBatchAttempt"),
        "Attempting VoyageAI batch embedding for {TextCount} texts");

    public static readonly Action<ILogger, int, Exception?> LogVoyageAIBatchSuccess = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2027, "VoyageAIBatchSuccess"),
        "VoyageAI batch embedding successful: {TextCount} embeddings");

    public static readonly Action<ILogger, Exception?> LogVoyageAIBatchFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(2028, "VoyageAIBatchFailed"),
        "VoyageAI batch embedding failed");

    public static readonly Action<ILogger, int, Exception?> LogIndividualEmbeddingAttempt = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2029, "IndividualEmbeddingAttempt"),
        "Attempting individual embedding for {TextCount} texts");

    public static readonly Action<ILogger, int, Exception?> LogIndividualEmbeddingAttemptSuccess = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2030, "IndividualEmbeddingAttemptSuccess"),
        "Individual embedding attempt successful: {TextCount} embeddings");

    public static readonly Action<ILogger, Exception?> LogIndividualEmbeddingAttemptFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(2031, "IndividualEmbeddingAttemptFailed"),
        "Individual embedding attempt failed");

    // Additional logging delegates for remaining calls
    public static readonly Action<ILogger, int, int, Exception?> LogSearchInDocuments = LoggerMessage.Define<int, int>(
        LogLevel.Debug,
        new EventId(2032, "SearchInDocuments"),
        "PerformBasicSearchAsync: Searching in {DocumentCount} documents with {ChunkCount} chunks");

    public static readonly Action<ILogger, int, Exception?> LogEmbeddingSearchSuccessful = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2033, "EmbeddingSearchSuccessful"),
        "PerformBasicSearchAsync: Embedding search successful, found {ChunkCount} chunks");

    public static readonly Action<ILogger, Exception?> LogEmbeddingSearchFailed = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(2034, "EmbeddingSearchFailed"),
        "PerformBasicSearchAsync: Embedding search failed, using keyword search");

    public static readonly Action<ILogger, string, Exception?> LogQueryWords = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2035, "QueryWords"),
        "PerformBasicSearchAsync: Query words: [{QueryWords}]");

    public static readonly Action<ILogger, string, Exception?> LogPotentialNames = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2036, "PotentialNames"),
        "PerformBasicSearchAsync: Potential names: [{PotentialNames}]");

    public static readonly Action<ILogger, string, string, Exception?> LogFullNameMatch = LoggerMessage.Define<string, string>(
        LogLevel.Debug,
        new EventId(2037, "FullNameMatch"),
        "PerformBasicSearchAsync: Found FULL NAME match: '{FullName}' in chunk: {ChunkPreview}...");

    public static readonly Action<ILogger, string, string, Exception?> LogPartialNameMatches = LoggerMessage.Define<string, string>(
        LogLevel.Debug,
        new EventId(2038, "PartialNameMatches"),
        "PerformBasicSearchAsync: Found PARTIAL name matches: [{FoundNames}] in chunk: {ChunkPreview}...");

    public static readonly Action<ILogger, int, Exception?> LogRelevantChunksFound = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2039, "RelevantChunksFound"),
        "PerformBasicSearchAsync: Found {ChunkCount} relevant chunks with enhanced search");

    public static readonly Action<ILogger, int, Exception?> LogNameChunksFound = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2040, "NameChunksFound"),
        "PerformBasicSearchAsync: Found {NameChunkCount} chunks containing names, prioritizing them");

    public static readonly Action<ILogger, Exception?> LogNoVoyageAIKey = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(2041, "NoVoyageAIKey"),
        "Embedding search: No VoyageAI API key found");

    public static readonly Action<ILogger, Exception?> LogFailedQueryEmbedding = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(2042, "FailedQueryEmbedding"),
        "Embedding search: Failed to generate query embedding");

    public static readonly Action<ILogger, int, Exception?> LogChunksContainingQueryTerms = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2043, "ChunksContainingQueryTerms"),
        "Embedding search: Found {ChunkCount} chunks containing query terms");

    public static readonly Action<ILogger, Exception?> LogNoChunksContainQueryTerms = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(2044, "NoChunksContainQueryTerms"),
        "Embedding search: No chunks contain query terms, using similarity only");

    public static readonly Action<ILogger, Exception?> LogEmbeddingSearchFailedError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(2045, "EmbeddingSearchFailedError"),
        "Embedding search failed");

    public static readonly Action<ILogger, int, int, Exception?> LogRateLimitedRetry = LoggerMessage.Define<int, int>(
        LogLevel.Debug,
        new EventId(2046, "RateLimitedRetry"),
        "Embedding generation rate limited, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})");

    public static readonly Action<ILogger, int, Exception?> LogRateLimitedAfterAttempts = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2047, "RateLimitedAfterAttempts"),
        "Embedding generation rate limited after {MaxRetries} attempts");

    public static readonly Action<ILogger, string, Exception?> LogProviderAttempt = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2048, "ProviderAttempt"),
        "Trying {Provider} provider for embedding generation");

    public static readonly Action<ILogger, string, string, Exception?> LogProviderConfigFound = LoggerMessage.Define<string, string>(
        LogLevel.Debug,
        new EventId(2049, "ProviderConfigFound"),
        "{Provider} config found, API key: {ApiKeyPreview}...");

    public static readonly Action<ILogger, string, int, Exception?> LogProviderSuccessful = LoggerMessage.Define<string, int>(
        LogLevel.Debug,
        new EventId(2050, "ProviderSuccessful"),
        "{Provider} successful: {Dimensions} dimensions");

    public static readonly Action<ILogger, string, Exception?> LogProviderReturnedNull = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2051, "ProviderReturnedNull"),
        "{Provider} returned null or empty embedding");

    public static readonly Action<ILogger, string, Exception?> LogProviderConfigNotFound = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2052, "ProviderConfigNotFound"),
        "{Provider} config not found or API key missing");

    public static readonly Action<ILogger, string, Exception?> LogProviderFailed = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2053, "ProviderFailed"),
        "{Provider} provider failed");

    public static readonly Action<ILogger, string, Exception?> LogAllProvidersFailedText = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2054, "AllProvidersFailedText"),
        "All embedding providers failed for text: {TextPreview}...");

    public static readonly Action<ILogger, string, Exception?> LogTestingVoyageAI = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(2055, "TestingVoyageAI"),
        "Testing VoyageAI directly with key: {ApiKeyPreview}...");

    public static readonly Action<ILogger, int, string, Exception?> LogVoyageAITestResponse = LoggerMessage.Define<int, string>(
        LogLevel.Debug,
        new EventId(2056, "VoyageAITestResponse"),
        "VoyageAI test response: {StatusCode} - {Response}");

    public static readonly Action<ILogger, Exception?> LogVoyageAIWorking = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(2057, "VoyageAIWorking"),
        "VoyageAI is working! Trying to parse embedding...");

    public static readonly Action<ILogger, int, Exception?> LogVoyageAITestEmbedding = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(2058, "VoyageAITestEmbedding"),
        "VoyageAI test embedding generated: {Dimensions} dimensions");

    public static readonly Action<ILogger, Exception?> LogFailedParseVoyageAI = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(2059, "FailedParseVoyageAI"),
        "Failed to parse VoyageAI response");

    public static readonly Action<ILogger, Exception?> LogVoyageAIDirectTestFailed = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(2060, "VoyageAIDirectTestFailed"),
        "VoyageAI direct test failed");

    #endregion
}
