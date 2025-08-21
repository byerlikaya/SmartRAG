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

    #region Anthropic/Voyage Provider

    public static readonly Action<ILogger, Exception?> LogVoyageParsingError = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(6001, "VoyageParsingError"),
        "Failed to parse Voyage embedding response, returning partial results");

    public static readonly Action<ILogger, string, Exception?> LogAnthropicResponseParsingError = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(6002, "AnthropicResponseParsingError"),
        "Failed to parse Anthropic response: {Error}");

    public static readonly Action<ILogger, string, Exception?> LogAnthropicEmbeddingValidationError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(6003, "AnthropicEmbeddingValidationError"),
        "Anthropic embedding validation failed: {ErrorMessage}");

    public static readonly Action<ILogger, string, Exception?> LogAnthropicEmbeddingRequestError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(6004, "AnthropicEmbeddingRequestError"),
        "Voyage embedding request failed: {Error}");

    public static readonly Action<ILogger, string, Exception?> LogAnthropicBatchEmbeddingRequestError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(6005, "AnthropicBatchEmbeddingRequestError"),
        "Voyage batch embedding request failed: {Error}");

    public static readonly Action<ILogger, Exception?> LogCustomTextParsingError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(6006, "CustomTextParsingError"),
        "Custom provider text response parsing failed");

    public static readonly Action<ILogger, string, Exception?> LogCustomEmbeddingValidationError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(6007, "CustomEmbeddingValidationError"),
        "Custom provider embedding validation failed: {ErrorMessage}");

    public static readonly Action<ILogger, Exception?> LogCustomEmbeddingModelMissing = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(6008, "CustomEmbeddingModelMissing"),
        "Custom provider embedding model is required but not provided");

    public static readonly Action<ILogger, string, Exception?> LogCustomEmbeddingRequestError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(6009, "CustomEmbeddingRequestError"),
        "Custom provider embedding request failed: {Error}");

    public static readonly Action<ILogger, Exception?> LogCustomEmbeddingParsingError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(6010, "CustomEmbeddingParsingError"),
        "Custom provider embedding response parsing failed");

    #endregion

    #region Azure OpenAI Provider

    public static readonly Action<ILogger, int, Exception?> LogAzureOpenAIRateLimit = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(7001, "AzureOpenAIRateLimit"),
        "Azure OpenAI rate limit: waiting {WaitTime}ms");

    public static readonly Action<ILogger, Exception?> LogAzureOpenAITextParsingError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(7002, "AzureOpenAITextParsingError"),
        "Azure OpenAI text response parsing failed");

    public static readonly Action<ILogger, string, Exception?> LogAzureOpenAIEmbeddingValidationError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(7003, "AzureOpenAIEmbeddingValidationError"),
        "Azure OpenAI embedding validation failed: {ErrorMessage}");

    public static readonly Action<ILogger, Exception?> LogAzureOpenAIEmbeddingModelMissing = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(7004, "AzureOpenAIEmbeddingModelMissing"),
        "Azure OpenAI embedding model is required but not provided");

    public static readonly Action<ILogger, string, Exception?> LogAzureOpenAIEmbeddingRequestError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(7005, "AzureOpenAIEmbeddingRequestError"),
        "Azure OpenAI embedding request failed: {Error}");

    public static readonly Action<ILogger, Exception?> LogAzureOpenAIEmbeddingParsingError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(7006, "AzureOpenAIEmbeddingParsingError"),
        "Azure OpenAI embedding response parsing failed");

    public static readonly Action<ILogger, string, Exception?> LogAzureOpenAIBatchEmbeddingRequestError = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(7007, "AzureOpenAIBatchEmbeddingRequestError"),
        "Azure OpenAI batch embedding request failed: {Error}");

    public static readonly Action<ILogger, Exception?> LogAzureOpenAIBatchEmbeddingParsingError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(7008, "AzureOpenAIBatchEmbeddingParsingError"),
        "Azure OpenAI batch embedding response parsing failed");

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
