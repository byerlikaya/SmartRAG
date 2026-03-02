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

    public static readonly Action<ILogger, string, Exception> LogDocumentUploadFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1002, "DocumentUploadFailed"),
        "Document upload failed for {FileName}");

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

    public static readonly Action<ILogger, Exception> LogQueryIntentAnalysisError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(3021, "QueryIntentAnalysisError"),
        "Error during query intent analysis, falling back to document-only query");

    public static readonly Action<ILogger, Exception> LogContextExpansionError = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(3022, "ContextExpansionError"),
        "Error during context expansion, returning original chunks");

    public static readonly Action<ILogger, Exception> LogMcpQueryError = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(3023, "McpQueryError"),
        "Error querying MCP servers");

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

    public static readonly Action<ILogger, Exception> LogConversationHistoryGetFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(50005, "ConversationHistoryGetFailed"),
        "Error getting conversation history");

    public static readonly Action<ILogger, Exception> LogConversationAddFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(50006, "ConversationAddFailed"),
        "Error adding to conversation");

    public static readonly Action<ILogger, Exception> LogConversationClearedAll = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(50007, "ConversationClearedAll"),
        "Cleared all conversation history");

    public static readonly Action<ILogger, Exception> LogConversationClearAllFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(50008, "ConversationClearAllFailed"),
        "Failed to clear all conversation history");

    public static readonly Action<ILogger, Exception> LogGeneralConversationError = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(50009, "GeneralConversationError"),
        "Error handling general conversation");

    public static readonly Action<ILogger, string, int, Exception> LogDatabaseDocumentUploadSuccess = LoggerMessage.Define<string, int>(
        LogLevel.Information,
        new EventId(70011, "DatabaseDocumentUploadSuccess"),
        "Database document upload successful: {FileName}, Content length: {ContentLength}");

    public static readonly Action<ILogger, Exception> LogDatabaseDocumentParseFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(70012, "DatabaseDocumentParseFailed"),
        "Failed to parse database document");

    public static readonly Action<ILogger, string, Exception> LogMcpConnecting = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(80001, "McpConnecting"),
        "Connecting to MCP server at {Endpoint}");

    public static readonly Action<ILogger, Exception> LogMcpAlreadyConnected = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(80002, "McpAlreadyConnected"),
        "Already connected to server");

    public static readonly Action<ILogger, Exception> LogMcpConnected = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(80003, "McpConnected"),
        "Successfully connected to MCP server");

    public static readonly Action<ILogger, Exception> LogMcpConnectFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(80004, "McpConnectFailed"),
        "Failed to connect to MCP server");

    public static readonly Action<ILogger, string, Exception> LogMcpDiscoverToolsFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(80005, "McpDiscoverToolsFailed"),
        "Failed to discover tools: {Error}");

    public static readonly Action<ILogger, int, Exception> LogMcpDiscoveredTools = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(80006, "McpDiscoveredTools"),
        "Discovered {Count} tools");

    public static readonly Action<ILogger, Exception> LogMcpDiscoverToolsError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(80007, "McpDiscoverToolsError"),
        "Error discovering tools");

    public static readonly Action<ILogger, Exception> LogMcpCallToolError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(80008, "McpCallToolError"),
        "Error calling MCP tool");

    public static readonly Action<ILogger, Exception> LogMcpParseResponseError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(80009, "McpParseResponseError"),
        "Error parsing MCP response");

    public static readonly Action<ILogger, string, Exception> LogFileWatcherCreateDirectoryFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(90001, "FileWatcherCreateDirectoryFailed"),
        "Failed to create directory: {FolderPath}");

    public static readonly Action<ILogger, string, Exception> LogFileWatcherStarted = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(90002, "FileWatcherStarted"),
        "Started watching folder: {FolderPath}");

    public static readonly Action<ILogger, Exception> LogFileWatcherStopped = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(90003, "FileWatcherStopped"),
        "Stopped watching all folders");

    public static readonly Action<ILogger, string, Exception> LogFileWatcherAutoUploading = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(90004, "FileWatcherAutoUploading"),
        "Auto-uploading file: {FilePath}");

    public static readonly Action<ILogger, string, Exception> LogFileWatcherFileCreatedError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(90005, "FileWatcherFileCreatedError"),
        "Error handling file created event for {FilePath}");

    public static readonly Action<ILogger, string, Exception> LogFileWatcherFileChangedError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(90006, "FileWatcherFileChangedError"),
        "Error handling file changed event for {FilePath}");

    public static readonly Action<ILogger, string, Exception> LogFileWatcherFileDeletedError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(90007, "FileWatcherFileDeletedError"),
        "Error handling file deleted event for {FilePath}");

    public static readonly Action<ILogger, Exception> LogFileWatcherError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(90008, "FileWatcherError"),
        "File watcher error occurred");

    public static readonly Action<ILogger, string, Exception> LogFileWatcherFileNoLongerExists = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(90009, "FileWatcherFileNoLongerExists"),
        "File no longer exists: {FilePath}");

    public static readonly Action<ILogger, string, long, string, string, Exception> LogFileWatcherSkippingDuplicate = LoggerMessage.Define<string, long, string, string>(
        LogLevel.Information,
        new EventId(90010, "FileWatcherSkippingDuplicate"),
        "Skipping duplicate file: {FileName} (size: {Size} bytes, hash: {Hash}) - Found duplicate with ID: {DuplicateId}");

    public static readonly Action<ILogger, string, long, string, Exception> LogFileWatcherAutoUploaded = LoggerMessage.Define<string, long, string>(
        LogLevel.Information,
        new EventId(90011, "FileWatcherAutoUploaded"),
        "Auto-uploaded file: {FilePath} (size: {Size} bytes, hash: {Hash})");

    public static readonly Action<ILogger, string, string, Exception> LogFileWatcherSkippingNoContent = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(90012, "FileWatcherSkippingNoContent"),
        "Skipping file (no content to index): {FilePath}. {Message}");

    public static readonly Action<ILogger, int, string, Exception> LogFileWatcherUploadFailedAfterRetries = LoggerMessage.Define<int, string>(
        LogLevel.Error,
        new EventId(90013, "FileWatcherUploadFailedAfterRetries"),
        "Failed to upload file after {RetryCount} attempts: {FilePath}");

    public static readonly Action<ILogger, int, int, string, Exception> LogFileWatcherUploadRetryError = LoggerMessage.Define<int, int, string>(
        LogLevel.Warning,
        new EventId(90014, "FileWatcherUploadRetryError"),
        "Error uploading file (attempt {RetryCount}/{MaxRetries}): {FilePath}");

    public static readonly Action<ILogger, string, long, string, Exception> LogFileWatcherSkippingDuplicateSimple = LoggerMessage.Define<string, long, string>(
        LogLevel.Information,
        new EventId(90015, "FileWatcherSkippingDuplicateSimple"),
        "Skipping duplicate file: {FileName} (size: {Size} bytes, hash: {Hash})");

    public static readonly Action<ILogger, string, Exception> LogFileWatcherProcessExistingFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(90016, "FileWatcherProcessExistingFailed"),
        "Failed to process existing file: {FilePath}");

    public static readonly Action<ILogger, string, int, int, int, Exception> LogFileWatcherInitialScanCompleted = LoggerMessage.Define<string, int, int, int>(
        LogLevel.Information,
        new EventId(90017, "FileWatcherInitialScanCompleted"),
        "Initial scan completed for folder: {FolderPath}. Processed: {ProcessedCount}, Skipped: {SkippedCount}, Duplicates: {DuplicateCount}");

    public static readonly Action<ILogger, string, Exception> LogFileWatcherScanError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(90018, "FileWatcherScanError"),
        "Error scanning existing files in folder: {FolderPath}");

    public static readonly Action<ILogger, Exception> LogQdrantEmbeddingGenerateFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(100001, "QdrantEmbeddingGenerateFailed"),
        "Failed to generate embedding for text");

    public static readonly Action<ILogger, Exception> LogQdrantVectorDimensionFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(100002, "QdrantVectorDimensionFailed"),
        "Failed to get vector dimension, using default");

    public static readonly Action<ILogger, string, Exception> LogAudioConversionStarting = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(110001, "AudioConversionStarting"),
        "Starting audio conversion: {Extension} to Compatible Format");

    public static readonly Action<ILogger, string, long, Exception> LogAudioConversionInputSaved = LoggerMessage.Define<string, long>(
        LogLevel.Debug,
        new EventId(110002, "AudioConversionInputSaved"),
        "Input file saved: {InputFile} ({Size} bytes)");

    public static readonly Action<ILogger, string, string, Exception> LogAudioConversionFfmpegRunning = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(110003, "AudioConversionFfmpegRunning"),
        "Running FFmpeg conversion: {InputFile} to {OutputFile}");

    public static readonly Action<ILogger, string, string, Exception> LogAudioConversionStrategyTrying = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(110004, "AudioConversionStrategyTrying"),
        "Trying conversion strategy: {Strategy} ({Codec})");

    public static readonly Action<ILogger, string, long, Exception> LogAudioConversionStrategySuccess = LoggerMessage.Define<string, long>(
        LogLevel.Information,
        new EventId(110005, "AudioConversionStrategySuccess"),
        "Conversion successful: {Strategy} ({Size} bytes)");

    public static readonly Action<ILogger, string, Exception> LogAudioConversionStrategyEmptyFile = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(110006, "AudioConversionStrategyEmptyFile"),
        "Conversion failed: {Strategy} - Empty or missing file");

    public static readonly Action<ILogger, string, string, Exception> LogAudioConversionStrategyFailed = LoggerMessage.Define<string, string>(
        LogLevel.Warning,
        new EventId(110007, "AudioConversionStrategyFailed"),
        "Conversion strategy {Strategy} failed: {Error}");

    public static readonly Action<ILogger, long, string, Exception> LogAudioConversionConvertedFileCreated = LoggerMessage.Define<long, string>(
        LogLevel.Information,
        new EventId(110008, "AudioConversionConvertedFileCreated"),
        "Converted file created: {Size} bytes, Path: {Path}");

    public static readonly Action<ILogger, Exception> LogAudioConversionFfmpegFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(110009, "AudioConversionFfmpegFailed"),
        "FFmpeg conversion failed. Is FFmpeg installed?");

    public static readonly Action<ILogger, long, long, Exception> LogAudioConversionCompleted = LoggerMessage.Define<long, long>(
        LogLevel.Debug,
        new EventId(110010, "AudioConversionCompleted"),
        "Audio conversion completed: {InputSize} to {OutputSize} bytes");

    public static readonly Action<ILogger, string, Exception> LogAudioConversionFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(110011, "AudioConversionFailed"),
        "Audio format conversion failed for {FileName}");

    public static readonly Action<ILogger, string, Exception> LogFfmpegInitialized = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(110012, "FfmpegInitialized"),
        "FFmpeg initialized successfully at {Path}");

    public static readonly Action<ILogger, Exception> LogFfmpegAutoDownloadFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(110013, "FfmpegAutoDownloadFailed"),
        "FFmpeg auto-download failed. Falling back to system FFmpeg.");

    public static readonly Action<ILogger, Exception> LogWhisperUnavailable = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(110014, "WhisperUnavailable"),
        "Whisper is unavailable; returning empty transcription for audio files. Check native libraries and model path.");

    public static readonly Action<ILogger, string, long, Exception> LogWhisperTranscriptionStarting = LoggerMessage.Define<string, long>(
        LogLevel.Information,
        new EventId(110015, "WhisperTranscriptionStarting"),
        "Starting Whisper transcription for {FileName} ({Size} bytes)");

    public static readonly Action<ILogger, int, double, Exception> LogWhisperTranscriptionCompleted = LoggerMessage.Define<int, double>(
        LogLevel.Information,
        new EventId(110016, "WhisperTranscriptionCompleted"),
        "Whisper transcription completed: {Length} characters with {Confidence} confidence");

    public static readonly Action<ILogger, string, Exception> LogWhisperTranscriptionFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(110017, "WhisperTranscriptionFailed"),
        "Whisper transcription failed for {FileName}");

    public static readonly Action<ILogger, string, Exception> LogWhisperFactoryInitializing = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(110018, "WhisperFactoryInitializing"),
        "Initializing Whisper factory with model: {ModelPath}");

    public static readonly Action<ILogger, Exception> LogWhisperGpuEnabled = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(110019, "WhisperGpuEnabled"),
        "Whisper GPU acceleration enabled. Ensure the host app references the matching runtime (CUDA on Windows/Linux, CoreML on macOS).");

    public static readonly Action<ILogger, Exception> LogWhisperUsingCpu = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(110020, "WhisperUsingCpu"),
        "Whisper using CPU. Set WhisperConfig.UseGpu to true and add the GPU runtime package for acceleration.");

    public static readonly Action<ILogger, long, Exception> LogWhisperModelLoadedToMemory = LoggerMessage.Define<long>(
        LogLevel.Debug,
        new EventId(110021, "WhisperModelLoadedToMemory"),
        "Loaded model into memory ({Size} bytes), initializing Whisper from buffer.");

    public static readonly Action<ILogger, long, Exception> LogWhisperInitializingFromPath = LoggerMessage.Define<long>(
        LogLevel.Debug,
        new EventId(110022, "WhisperInitializingFromPath"),
        "Initializing Whisper from path ({Size} bytes).");

    public static readonly Action<ILogger, string, Exception> LogWhisperModelLoadFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(110023, "WhisperModelLoadFailed"),
        "Failed to load Whisper model from {ModelPath}. Ensure the file exists and Whisper native libraries (e.g. ggml) are available.");

    public static readonly Action<ILogger, string, long, Exception> LogWhisperModelFound = LoggerMessage.Define<string, long>(
        LogLevel.Debug,
        new EventId(110024, "WhisperModelFound"),
        "Whisper model found at {ModelPath} ({Size} bytes)");

    public static readonly Action<ILogger, long, long, Exception> LogWhisperRemovingTruncatedModel = LoggerMessage.Define<long, long>(
        LogLevel.Information,
        new EventId(110025, "WhisperRemovingTruncatedModel"),
        "Removing truncated Whisper model file ({Size} bytes, expected at least {MinSize} bytes). Re-downloading.");

    public static readonly Action<ILogger, Exception> LogWhisperCouldNotRemoveTruncatedModel = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(110026, "WhisperCouldNotRemoveTruncatedModel"),
        "Could not remove truncated model file. Another process may be writing. Will retry on next request.");

    public static readonly Action<ILogger, string, Exception> LogWhisperCreatedModelDirectory = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(110027, "WhisperCreatedModelDirectory"),
        "Created model directory: {Directory}");

    public static readonly Action<ILogger, string, string, Exception> LogWhisperDownloadingModel = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(110028, "WhisperDownloadingModel"),
        "Downloading Whisper model: {ModelType} to {ModelPath}");

    public static readonly Action<ILogger, string, Exception> LogWhisperModelDownloaded = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(110029, "WhisperModelDownloaded"),
        "Whisper model downloaded successfully to {ModelPath}");

    public static readonly Action<ILogger, long, Exception> LogWhisperCreateBuilderFailed = LoggerMessage.Define<long>(
        LogLevel.Error,
        new EventId(110030, "WhisperCreateBuilderFailed"),
        "Whisper CreateBuilder failed. Model file size: {ModelSize} bytes. If this is much smaller than expected for the model type (e.g. large-v3 ~2.9 GB), the file may be corrupted or truncated. Re-download the model or use ggml-base.bin / ggml-tiny.bin for testing. Native libraries may also be missing.");

    public static readonly Action<ILogger, string, Exception> LogWhisperConvertingToWav = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(110031, "WhisperConvertingToWav"),
        "Converting audio file to WAV format: {FileName}");

    public static readonly Action<ILogger, string, Exception> LogWhisperInferenceStarted = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(110032, "WhisperInferenceStarted"),
        "Whisper inference started for {FileName} (large models on CPU may take 1-3 min for ~1 min of audio).");

    public static readonly Action<ILogger, int, int, int, Exception> LogWhisperProcessingCompleted = LoggerMessage.Define<int, int, int>(
        LogLevel.Information,
        new EventId(110033, "WhisperProcessingCompleted"),
        "Whisper processing completed: {SegmentCount} segments processed, {SkippedLowConf} low-confidence skipped, {SkippedDup} duplicates skipped");

    public static readonly Action<ILogger, double, double, Exception> LogWhisperConfidenceBelowThreshold = LoggerMessage.Define<double, double>(
        LogLevel.Warning,
        new EventId(110034, "WhisperConfidenceBelowThreshold"),
        "Transcription confidence {Confidence} below threshold {Threshold}");

    public static readonly Action<ILogger, Exception> LogWhisperTranscriptionProcessingFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(110035, "WhisperTranscriptionProcessingFailed"),
        "Whisper transcription processing failed");

    public static readonly Action<ILogger, Exception> LogAudioFileParserLanguageDetected = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(110036, "AudioFileParserLanguageDetected"),
        "AudioFileParser: Language detected");

    public static readonly Action<ILogger, string, Exception> LogAudioFileParserLanguageFromFilename = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(110037, "AudioFileParserLanguageFromFilename"),
        "Detected language from filename: {Language}");

    public static readonly Action<ILogger, string, Exception> LogAudioFileParserUsingDefaultLanguage = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(110038, "AudioFileParserUsingDefaultLanguage"),
        "Using WhisperConfig.DefaultLanguage for transcription: {Language}");

    public static readonly Action<ILogger, Exception> LogAudioFileParserNoLanguageAutoDetect = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(110039, "AudioFileParserNoLanguageAutoDetect"),
        "No language specified; using auto-detect so Whisper transcribes in the detected language (no translation).");

    public static readonly Action<ILogger, Exception> LogQueryIntentOverrideToInformation = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(120001, "QueryIntentOverrideToInformation"),
        "Overriding LLM CONVERSATION to INFORMATION: query has data-request pattern");

    public static readonly Action<ILogger, int, int, Exception> LogQueryIntentIncompleteTokens = LoggerMessage.Define<int, int>(
        LogLevel.Warning,
        new EventId(120002, "QueryIntentIncompleteTokens"),
        "AI returned only {Count} tokens, expected at least {MinCount}. Response may be incomplete.");

    public static readonly Action<ILogger, string, Exception> LogQueryIntentUnknownType = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(120003, "QueryIntentUnknownType"),
        "AI returned JSON with unknown type value: {Type}");

    public static readonly Action<ILogger, string, Exception> LogQueryIntentParseJsonFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(120004, "QueryIntentParseJsonFailed"),
        "Failed to parse AI response as JSON. Response: {Response}");

    public static readonly Action<ILogger, Exception> LogQueryIntentParseCleanedJsonFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(120005, "QueryIntentParseCleanedJsonFailed"),
        "Failed to parse cleaned JSON, falling back to plain classification");

    public static readonly Action<ILogger, Exception> LogQueryIntentClassificationFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(120006, "QueryIntentClassificationFailed"),
        "AI classification failed; defaulting to conversation.");

    public static readonly Action<ILogger, int, int, string, Exception> LogQueryIntentIncompleteTokensWithPreview = LoggerMessage.Define<int, int, string>(
        LogLevel.Warning,
        new EventId(120007, "QueryIntentIncompleteTokensWithPreview"),
        "AI returned only {Count} tokens, expected at least {MinCount}. Tokens: {Tokens}");

    public static readonly Action<ILogger, Exception> LogMcpConnectionNoServersConfigured = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(130001, "McpConnectionNoServersConfigured"),
        "No MCP servers configured");

    public static readonly Action<ILogger, Exception> LogMcpConnectionNoAutoConnectServers = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(130002, "McpConnectionNoAutoConnectServers"),
        "No MCP servers with AutoConnect enabled");

    public static readonly Action<ILogger, int, Exception> LogMcpConnectionConnectingToServers = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(130003, "McpConnectionConnectingToServers"),
        "Connecting to {Count} MCP servers");

    public static readonly Action<ILogger, Exception> LogMcpConnectionSuccess = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(130004, "McpConnectionSuccess"),
        "Successfully connected to MCP server");

    public static readonly Action<ILogger, Exception> LogMcpConnectionFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130005, "McpConnectionFailed"),
        "Failed to connect to MCP server");

    public static readonly Action<ILogger, Exception> LogMcpConnectionError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(130006, "McpConnectionError"),
        "Error connecting to MCP server");

    public static readonly Action<ILogger, Exception> LogMcpIntegrationNoServersConnected = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(130007, "McpIntegrationNoServersConnected"),
        "No MCP servers connected");

    public static readonly Action<ILogger, Exception> LogMcpIntegrationErrorCallingToolOnServer = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130008, "McpIntegrationErrorCallingToolOnServer"),
        "Error calling tool on server");

    public static readonly Action<ILogger, Exception> LogMcpIntegrationErrorQueryingServer = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(130009, "McpIntegrationErrorQueryingServer"),
        "Error querying MCP server");

    public static readonly Action<ILogger, Exception> LogMcpIntegrationNoServersAttemptingOnDemand = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(130010, "McpIntegrationNoServersAttemptingOnDemand"),
        "No MCP servers connected; attempting on-demand connect for MCP-tagged request.");

    public static readonly Action<ILogger, Exception> LogMcpIntegrationErrorParsingInputSchema = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130011, "McpIntegrationErrorParsingInputSchema"),
        "Error parsing InputSchema for tool, using default parameters");

    public static readonly Action<ILogger, Exception> LogMcpIntegrationErrorDiscoveringTools = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(130012, "McpIntegrationErrorDiscoveringTools"),
        "Error discovering tools");

    public static readonly Action<ILogger, Exception> LogMcpIntegrationErrorCallingMcpTool = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(130013, "McpIntegrationErrorCallingMcpTool"),
        "Error calling MCP tool");

    public static readonly Action<ILogger, string, Exception> LogQdrantSearchVectorSearchFailedForCollection = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(130014, "QdrantSearchVectorSearchFailedForCollection"),
        "Vector search failed for collection: {Collection}");

    public static readonly Action<ILogger, Exception> LogQdrantSearchVectorSearchFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(130015, "QdrantSearchVectorSearchFailed"),
        "Vector search failed");

    public static readonly Action<ILogger, string, Exception> LogQdrantSearchFallbackSearchFailedForCollection = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(130016, "QdrantSearchFallbackSearchFailedForCollection"),
        "Fallback search failed for collection: {Collection}");

    public static readonly Action<ILogger, Exception> LogQdrantSearchFallbackTextSearchFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(130017, "QdrantSearchFallbackTextSearchFailed"),
        "Fallback text search failed");

    public static readonly Action<ILogger, Exception> LogQdrantCollectionInitFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(130018, "QdrantCollectionInitFailed"),
        "Failed to initialize Qdrant collection");

    public static readonly Action<ILogger, string, int, Exception> LogQdrantCollectionCreating = LoggerMessage.Define<string, int>(
        LogLevel.Information,
        new EventId(130019, "QdrantCollectionCreating"),
        "Creating Qdrant collection: {CollectionName} with dimension: {Dimension}");

    public static readonly Action<ILogger, string, Exception> LogQdrantCollectionCreatedTextIndex = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(130020, "QdrantCollectionCreatedTextIndex"),
        "Created text index for 'content' field in collection: {CollectionName}");

    public static readonly Action<ILogger, string, Exception> LogQdrantCollectionCreateFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(130021, "QdrantCollectionCreateFailed"),
        "Failed to create Qdrant collection: {CollectionName}");

    public static readonly Action<ILogger, int, string, Exception> LogQdrantCollectionUsingEmbeddingDimension = LoggerMessage.Define<int, string>(
        LogLevel.Debug,
        new EventId(130022, "QdrantCollectionUsingEmbeddingDimension"),
        "Using embedding dimension {Dimension} from document chunk for collection {CollectionName}");

    public static readonly Action<ILogger, string, Exception> LogQdrantCollectionEnsureFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(130023, "QdrantCollectionEnsureFailed"),
        "Failed to ensure document collection exists: {CollectionName}");

    public static readonly Action<ILogger, Exception> LogQdrantCollectionDetectDimensionFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(130024, "QdrantCollectionDetectDimensionFailed"),
        "Failed to detect vector dimension, using default");

    public static readonly Action<ILogger, string, Exception> LogQdrantCollectionDeleted = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(130025, "QdrantCollectionDeleted"),
        "Deleted Qdrant collection: {CollectionName}");

    public static readonly Action<ILogger, string, Exception> LogQdrantCollectionDeleteFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(130026, "QdrantCollectionDeleteFailed"),
        "Failed to delete collection: {CollectionName}");

    public static readonly Action<ILogger, string, Exception> LogQdrantCollectionRecreated = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(130027, "QdrantCollectionRecreated"),
        "Recreated Qdrant collection: {CollectionName}");

    public static readonly Action<ILogger, string, Exception> LogQdrantCollectionRecreateFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(130028, "QdrantCollectionRecreateFailed"),
        "Failed to recreate collection: {CollectionName}");

    public static readonly Action<ILogger, Exception> LogAIServiceFallbackProvidersFailed = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(130029, "AIServiceFallbackProvidersFailed"),
        "Fallback providers failed");

    public static readonly Action<ILogger, Exception> LogAIServiceErrorGeneratingEmbeddings = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(130030, "AIServiceErrorGeneratingEmbeddings"),
        "Error generating embeddings");

    public static readonly Action<ILogger, Exception> LogAIServiceAllFallbackProvidersFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130031, "AIServiceAllFallbackProvidersFailed"),
        "All fallback providers failed");

    public static readonly Action<ILogger, string, string, Exception> LogDocumentServiceSkippingDuplicateUpload = LoggerMessage.Define<string, string>(
        LogLevel.Information,
        new EventId(130032, "DocumentServiceSkippingDuplicateUpload"),
        "Skipping duplicate upload (same file hash): {FileName} - returning existing document Id: {DocumentId}");

    public static readonly Action<ILogger, Exception> LogQueryStrategyDatabaseQueryFailedFallback = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130033, "QueryStrategyDatabaseQueryFailedFallback"),
        "Database query failed, falling back to document query");

    public static readonly Action<ILogger, Exception> LogQueryStrategyDatabaseQueryFailedInHybridMode = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130034, "QueryStrategyDatabaseQueryFailedInHybridMode"),
        "Database query failed in hybrid mode, continuing with document query only");

    public static readonly Action<ILogger, int, int, Exception> LogDocumentSearchRepositorySearchReturnedResults = LoggerMessage.Define<int, int>(
        LogLevel.Debug,
        new EventId(130035, "DocumentSearchRepositorySearchReturnedResults"),
        "Repository search returned {Count} results for query length {QueryLength}");

    public static readonly Action<ILogger, Exception> LogDocumentSearchRepositorySearchFailedFallback = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130036, "DocumentSearchRepositorySearchFailedFallback"),
        "Repository search failed, falling back to keyword scoring");

    public static readonly Action<ILogger, string, Exception> LogPdfParserFailedToParseDocument = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(130037, "PdfParserFailedToParseDocument"),
        "Failed to parse PDF document: {FileName}");

    public static readonly Action<ILogger, int, int, bool, bool, Exception> LogPdfParserPageSubstantialText = LoggerMessage.Define<int, int, bool, bool>(
        LogLevel.Debug,
        new EventId(130038, "PdfParserPageSubstantialText"),
        "PDF page {PageNumber} has substantial extracted text, using text extraction only (length: {Length}, encodingIssues: {EncodingIssues}, hasImages: {HasImages})");

    public static readonly Action<ILogger, int, int, bool, bool, Exception> LogPdfParserPageNoSubstantialTextOcr = LoggerMessage.Define<int, int, bool, bool>(
        LogLevel.Debug,
        new EventId(130039, "PdfParserPageNoSubstantialTextOcr"),
        "PDF page {PageNumber} has no substantial text; attempting OCR (length: {Length}, encodingIssues: {EncodingIssues}, hasImages: {HasImages})");

    public static readonly Action<ILogger, int, int, Exception> LogPdfParserPageLimitedText = LoggerMessage.Define<int, int>(
        LogLevel.Debug,
        new EventId(130040, "PdfParserPageLimitedText"),
        "PDF page {PageNumber} has limited extracted text, using text extraction (length: {Length})");

    public static readonly Action<ILogger, int, int, Exception> LogPdfParserUsedOcrForPage = LoggerMessage.Define<int, int>(
        LogLevel.Debug,
        new EventId(130041, "PdfParserUsedOcrForPage"),
        "Used OCR for PDF page {PageNumber} from embedded image (extracted text length: {Length} chars)");

    public static readonly Action<ILogger, int, Exception> LogPdfParserOcrFailedEmbeddedImage = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(130042, "PdfParserOcrFailedEmbeddedImage"),
        "OCR failed to extract text from embedded image on PDF page {PageNumber}, using extracted text fallback");

    public static readonly Action<ILogger, int, Exception> LogPdfParserPageExpectedEmbeddedImagesNoneFound = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(130043, "PdfParserPageExpectedEmbeddedImagesNoneFound"),
        "PDF page {PageNumber} was expected to have embedded images but none were found, using extracted text");

    public static readonly Action<ILogger, int, Exception> LogPdfParserFailedToExtractTextViaOcrForPage = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(130044, "PdfParserFailedToExtractTextViaOcrForPage"),
        "Failed to extract text via OCR for PDF page {PageNumber}, using extracted text fallback");

    public static readonly Action<ILogger, Exception> LogPdfParserFailedToCheckEmbeddedImages = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(130045, "PdfParserFailedToCheckEmbeddedImages"),
        "Failed to check for embedded images in PDF page");

    public static readonly Action<ILogger, int, string, string, string, string, int, Exception> LogPdfParserDetectedBrokenSpacing = LoggerMessage.Define<int, string, string, string, string, int>(
        LogLevel.Debug,
        new EventId(130046, "PdfParserDetectedBrokenSpacing"),
        "Detected broken spacing at position {Position}: '{Before}{Char1}{Char2}{After}' (count: {Count})");

    public static readonly Action<ILogger, int, int, Exception> LogPdfParserDetectedConsonantCluster = LoggerMessage.Define<int, int>(
        LogLevel.Debug,
        new EventId(130047, "PdfParserDetectedConsonantCluster"),
        "Detected unusual consonant cluster: {Count} consecutive ASCII consonants at position {Position}");

    public static readonly Action<ILogger, int, int, double, Exception> LogPdfParserDetectedFewNonAscii = LoggerMessage.Define<int, int, double>(
        LogLevel.Debug,
        new EventId(130048, "PdfParserDetectedFewNonAscii"),
        "Detected very few non-ASCII characters with broken words: {NonAscii}/{Total} = {Ratio:P2} (threshold: 1.5%)");

    public static readonly Action<ILogger, int, int, double, Exception> LogPdfParserDetectedSuspiciousChars = LoggerMessage.Define<int, int, double>(
        LogLevel.Debug,
        new EventId(130049, "PdfParserDetectedSuspiciousChars"),
        "Detected high frequency of suspicious characters: {Count}/{Total} = {Ratio:P2} (threshold: 1%)");

    public static readonly Action<ILogger, bool, bool, bool, bool, bool, Exception> LogPdfParserEncodingIssueDetected = LoggerMessage.Define<bool, bool, bool, bool, bool>(
        LogLevel.Debug,
        new EventId(130050, "PdfParserEncodingIssueDetected"),
        "Encoding issue detected - BrokenSpacing: {BrokenSpacing}, ConsonantClusters: {Clusters}, FewSpecialChars: {FewChars}, BrokenWords: {BrokenWords}, SuspiciousChars: {SuspiciousChars}");

    public static readonly Action<ILogger, int, Exception> LogPdfParserExtractedEmbeddedImageForOcr = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(130051, "PdfParserExtractedEmbeddedImageForOcr"),
        "Extracted embedded image from PDF page {PageIndex} for OCR");

    public static readonly Action<ILogger, int, Exception> LogPdfParserRenderedTextBasedPageToBitmap = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(130052, "PdfParserRenderedTextBasedPageToBitmap"),
        "Rendered text-based PDF page {PageIndex} to bitmap for OCR");

    public static readonly Action<ILogger, int, Exception> LogPdfParserNoEmbeddedImagesPageRenderingUnavailable = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(130053, "PdfParserNoEmbeddedImagesPageRenderingUnavailable"),
        "No embedded images found and page rendering unavailable for PDF page {PageIndex}");

    public static readonly Action<ILogger, int, Exception> LogPdfParserFailedToRenderPageToImageForOcr = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(130054, "PdfParserFailedToRenderPageToImageForOcr"),
        "Failed to render PDF page {PageIndex} to image for OCR");

    public static readonly Action<ILogger, Exception> LogPdfParserFailedToRenderPageUsingEmbeddedImages = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(130055, "PdfParserFailedToRenderPageUsingEmbeddedImages"),
        "Failed to render PDF page using embedded images");

    public static readonly Action<ILogger, Exception> LogPdfParserFailedToExtractImageFromPage = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(130056, "PdfParserFailedToExtractImageFromPage"),
        "Failed to extract image from PDF page");

    public static readonly Action<ILogger, Exception> LogPdfParserFailedToExtractImagesFromPage = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(130057, "PdfParserFailedToExtractImagesFromPage"),
        "Failed to extract images from PDF page");

    public static readonly Action<ILogger, int, Exception> LogPdfParserFailedToUpscaleBitmap = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(130058, "PdfParserFailedToUpscaleBitmap"),
        "Failed to upscale bitmap for PDF page {PageIndex}, using original resolution");

    public static readonly Action<ILogger, int, int, int, Exception> LogPdfParserSuccessfullyRenderedPageToBitmap = LoggerMessage.Define<int, int, int>(
        LogLevel.Debug,
        new EventId(130059, "PdfParserSuccessfullyRenderedPageToBitmap"),
        "Successfully rendered PDF page {PageIndex} to bitmap ({Width}x{Height})");

    public static readonly Action<ILogger, int, int, int, int, int, Exception> LogPdfParserSuccessfullyRenderedAndUpscaled = LoggerMessage.Define<int, int, int, int, int>(
        LogLevel.Debug,
        new EventId(130060, "PdfParserSuccessfullyRenderedAndUpscaled"),
        "Successfully rendered and upscaled PDF page {PageIndex} to bitmap ({OriginalWidth}x{OriginalHeight} to {ScaledWidth}x{ScaledHeight})");

    public static readonly Action<ILogger, int, Exception> LogPdfParserFailedToRenderPageToBitmap = LoggerMessage.Define<int>(
        LogLevel.Debug,
        new EventId(130061, "PdfParserFailedToRenderPageToBitmap"),
        "Failed to render PDF page {PageIndex} to bitmap");

    public static readonly Action<ILogger, string, int, int, int, int, Exception> LogPdfParserFixedTextEncoding = LoggerMessage.Define<string, int, int, int, int>(
        LogLevel.Debug,
        new EventId(130062, "PdfParserFixedTextEncoding"),
        "Fixed PDF text encoding using {Encoding}: replacement chars {Original} to {Corrected}, suspicious chars {OriginalSusp} to {CorrectedSusp}");

    public static readonly Action<ILogger, Exception> LogPdfParserFailedToFixTextEncoding = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(130063, "PdfParserFailedToFixTextEncoding"),
        "Failed to fix PDF text encoding, using original text");

    public static readonly Action<ILogger, Exception> LogImageParserFailedToDecodeWebP = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130064, "ImageParserFailedToDecodeWebP"),
        "Failed to decode WebP image with SkiaSharp");

    public static readonly Action<ILogger, Exception> LogImageParserFailedToConvertWebP = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130065, "ImageParserFailedToConvertWebP"),
        "Failed to convert WebP image format with SkiaSharp, using original stream");

    public static readonly Action<ILogger, string, Exception> LogImageParserOcrSystemLocale = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(130066, "ImageParserOcrSystemLocale"),
        "OCR Language Detection: System locale: {Code}");

    public static readonly Action<ILogger, Exception> LogImageParserOcrNoMappingDefaultingEng = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130067, "ImageParserOcrNoMappingDefaultingEng"),
        "OCR Language Detection: No mapping for locale, defaulting to 'eng'");

    public static readonly Action<ILogger, Exception> LogImageParserOcrFailedDetectLocale = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(130068, "ImageParserOcrFailedDetectLocale"),
        "OCR Language Detection: Failed to detect system locale, defaulting to 'eng'");

    public static readonly Action<ILogger, Exception> LogImageParserUnknownLanguageCode = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130069, "ImageParserUnknownLanguageCode"),
        "Unknown language code, falling back to default");

    public static readonly Action<ILogger, string, Exception> LogImageParserFailedToCreateTessdataDirectory = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(130070, "ImageParserFailedToCreateTessdataDirectory"),
        "Failed to create tessdata directory: {Path}");

    public static readonly Action<ILogger, Exception> LogImageParserTesseractDataNotFound = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(130071, "ImageParserTesseractDataNotFound"),
        "Tesseract data not found. Attempting to download...");

    public static readonly Action<ILogger, Exception> LogImageParserSuccessfullyDownloadedTesseractData = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(130072, "ImageParserSuccessfullyDownloadedTesseractData"),
        "Successfully downloaded Tesseract data");

    public static readonly Action<ILogger, Exception> LogImageParserAttemptingDownloadFallbackLanguage = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(130073, "ImageParserAttemptingDownloadFallbackLanguage"),
        "Attempting to download fallback language...");

    public static readonly Action<ILogger, Exception> LogImageParserSuccessfullyDownloadedFallbackTesseractData = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(130074, "ImageParserSuccessfullyDownloadedFallbackTesseractData"),
        "Successfully downloaded fallback Tesseract data");

    public static readonly Action<ILogger, Exception> LogImageParserNoTesseractDataDownloadFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130075, "ImageParserNoTesseractDataDownloadFailed"),
        "No Tesseract language data available and download failed. OCR will be skipped.");

    public static readonly Action<ILogger, string, Exception> LogImageParserDownloadingTesseractDataFrom = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(130076, "ImageParserDownloadingTesseractDataFrom"),
        "Downloading Tesseract data from: {Url}");

    public static readonly Action<ILogger, int, Exception> LogImageParserFailedToDownloadTesseractDataHttp = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(130077, "ImageParserFailedToDownloadTesseractDataHttp"),
        "Failed to download Tesseract data: HTTP {StatusCode}");

    public static readonly Action<ILogger, string, long, Exception> LogImageParserDownloadedTesseractData = LoggerMessage.Define<string, long>(
        LogLevel.Debug,
        new EventId(130078, "ImageParserDownloadedTesseractData"),
        "Downloaded Tesseract data: {File} ({Size} bytes)");

    public static readonly Action<ILogger, Exception> LogImageParserNetworkErrorDownloadingTesseractData = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130079, "ImageParserNetworkErrorDownloadingTesseractData"),
        "Network error while downloading Tesseract data. OCR will use fallback.");

    public static readonly Action<ILogger, Exception> LogImageParserDownloadTimeoutTesseractData = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130080, "ImageParserDownloadTimeoutTesseractData"),
        "Download timeout for Tesseract data. OCR will use fallback.");

    public static readonly Action<ILogger, Exception> LogImageParserFailedToDownloadTesseractData = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130081, "ImageParserFailedToDownloadTesseractData"),
        "Failed to download Tesseract data. OCR will use fallback.");

    public static readonly Action<ILogger, Exception> LogImageParserNoTesseractDataOcrSkipped = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(130082, "ImageParserNoTesseractDataOcrSkipped"),
        "No Tesseract language data available. OCR cannot be performed. Skipping OCR.");

    public static readonly Action<ILogger, Exception> LogImageParserTesseractDataNotFoundUsingFallback = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(130083, "ImageParserTesseractDataNotFoundUsingFallback"),
        "Tesseract data not found. Using fallback instead.");

    public static readonly Action<ILogger, Exception> LogImageParserUsingTesseractLanguage = LoggerMessage.Define(
        LogLevel.Debug,
        new EventId(130084, "ImageParserUsingTesseractLanguage"),
        "Using Tesseract language");
}

