using Microsoft.Extensions.Logging;

namespace SmartRAG.Providers;

/// <summary>
/// Centralized LoggerMessage delegates for AI providers performance optimization
/// </summary>
public static class ProviderLogMessages
{
    #region Gemini Provider

    public static readonly Action<ILogger, string, Exception?> LogGeminiEmbeddingError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(7001, "GeminiEmbeddingError"),
        "Gemini embedding error: {Error}");

    public static readonly Action<ILogger, string, Exception?> LogGeminiBatchEmbeddingError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(7002, "GeminiBatchEmbeddingError"),
        "Gemini batch embedding error: {Error}");

    public static readonly Action<ILogger, int, string, Exception?> LogGeminiBatchFailedFallback = LoggerMessage.Define<int, string>(
        LogLevel.Warning,
        new EventId(7003, "GeminiBatchFailedFallback"),
        "Failed to process batch {BatchIndex}, falling back to individual requests: {Error}");

    public static readonly Action<ILogger, string, Exception?> LogGeminiBatchParsingError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(7004, "GeminiBatchParsingError"),
        "Failed to parse batch embedding response: {Error}");

    #endregion

    #region General Provider Errors

    public static readonly Action<ILogger, string, Exception?> LogProviderRequestFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(7101, "ProviderRequestFailed"),
        "{Provider} request failed after multiple attempts");

    public static readonly Action<ILogger, string, Exception?> LogProviderHttpError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(7102, "ProviderHttpError"),
        "{Provider} HTTP request failed");

    public static readonly Action<ILogger, string, Exception?> LogProviderConfigValidationFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(7103, "ProviderConfigValidationFailed"),
        "{Provider} configuration validation failed");

    #endregion

    #region Rate Limiting

    public static readonly Action<ILogger, int, int, Exception?> LogRateLimitedRetry = LoggerMessage.Define<int, int>(
        LogLevel.Warning,
        new EventId(7201, "RateLimitedRetry"),
        "Rate limited, retrying in {Delay}ms (attempt {Attempt})");

    public static readonly Action<ILogger, int, Exception?> LogRateLimitedMaxAttempts = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(7202, "RateLimitedMaxAttempts"),
        "Rate limited after {MaxAttempts} attempts");

    #endregion

    #region Batch Processing

    public static readonly Action<ILogger, int, int, Exception?> LogBatchProcessingStarted = LoggerMessage.Define<int, int>(
        LogLevel.Information,
        new EventId(7301, "BatchProcessingStarted"),
        "Processing batch {BatchNumber}/{TotalBatches} with {BatchSize} items");

    public static readonly Action<ILogger, int, Exception?> LogBatchProcessingCompleted = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(7302, "BatchProcessingCompleted"),
        "Batch {BatchNumber} completed successfully");

    public static readonly Action<ILogger, int, Exception?> LogBatchProcessingFailed = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(7303, "BatchProcessingFailed"),
        "Batch {BatchNumber} failed, using fallback method");

    #endregion
}
