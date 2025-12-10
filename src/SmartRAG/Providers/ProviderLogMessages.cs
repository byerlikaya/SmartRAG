using Microsoft.Extensions.Logging;
using System;

namespace SmartRAG.Providers
{
    /// <summary>
    /// Centralized LoggerMessage delegates for AI providers performance optimization
    /// </summary>
    public static class ProviderLogMessages
    {
        public static readonly Action<ILogger, Exception> LogVoyageParsingError = LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(6001, "VoyageParsingError"),
            "Failed to parse Voyage embedding response, returning partial results");

        public static readonly Action<ILogger, string, Exception> LogAnthropicResponseParsingError = LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(6002, "AnthropicResponseParsingError"),
            "Failed to parse Anthropic response: {Error}");

        public static readonly Action<ILogger, string, Exception> LogAnthropicEmbeddingValidationError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(6003, "AnthropicEmbeddingValidationError"),
            "Anthropic embedding validation failed: {ErrorMessage}");

        public static readonly Action<ILogger, string, Exception> LogAnthropicEmbeddingRequestError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(6004, "AnthropicEmbeddingRequestError"),
            "Voyage embedding request failed: {Error}");

        public static readonly Action<ILogger, string, Exception> LogAnthropicBatchEmbeddingRequestError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(6005, "AnthropicBatchEmbeddingRequestError"),
            "Voyage batch embedding request failed: {Error}");

        public static readonly Action<ILogger, int, Exception> LogAzureOpenAIRateLimit = LoggerMessage.Define<int>(
            LogLevel.Warning,
            new EventId(7001, "AzureOpenAIRateLimit"),
            "Azure OpenAI rate limit: waiting {WaitTime}ms");

        public static readonly Action<ILogger, Exception> LogAzureOpenAITextParsingError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(7002, "AzureOpenAITextParsingError"),
            "Azure OpenAI text response parsing failed");

        public static readonly Action<ILogger, string, Exception> LogAzureOpenAIEmbeddingValidationError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(7003, "AzureOpenAIEmbeddingValidationError"),
            "Azure OpenAI embedding validation failed: {ErrorMessage}");

        public static readonly Action<ILogger, Exception> LogAzureOpenAIEmbeddingModelMissing = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(7004, "AzureOpenAIEmbeddingModelMissing"),
            "Azure OpenAI embedding model is required but not provided");

        public static readonly Action<ILogger, string, Exception> LogAzureOpenAIEmbeddingRequestError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(7005, "AzureOpenAIEmbeddingRequestError"),
            "Azure OpenAI embedding request failed: {Error}");

        public static readonly Action<ILogger, Exception> LogAzureOpenAIEmbeddingParsingError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(7006, "AzureOpenAIEmbeddingParsingError"),
            "Azure OpenAI embedding response parsing failed");

        public static readonly Action<ILogger, string, Exception> LogAzureOpenAIBatchEmbeddingRequestError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(7007, "AzureOpenAIBatchEmbeddingRequestError"),
            "Azure OpenAI batch embedding request failed: {Error}");

        public static readonly Action<ILogger, Exception> LogAzureOpenAIBatchEmbeddingParsingError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(7008, "AzureOpenAIBatchEmbeddingParsingError"),
            "Azure OpenAI batch embedding response parsing failed");

        public static readonly Action<ILogger, Exception> LogOpenAITextParsingError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(8001, "OpenAITextParsingError"),
            "OpenAI text response parsing failed");

        public static readonly Action<ILogger, string, Exception> LogOpenAIEmbeddingValidationError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(8002, "OpenAIEmbeddingValidationError"),
            "OpenAI embedding validation failed: {ErrorMessage}");

        public static readonly Action<ILogger, Exception> LogOpenAIEmbeddingModelMissing = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(8003, "OpenAIEmbeddingModelMissing"),
            "OpenAI embedding model is required but not provided");

        public static readonly Action<ILogger, string, Exception> LogOpenAIEmbeddingRequestError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(8004, "OpenAIEmbeddingRequestError"),
            "OpenAI embedding request failed: {Error}");

        public static readonly Action<ILogger, Exception> LogOpenAIEmbeddingParsingError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(8005, "OpenAIEmbeddingParsingError"),
            "OpenAI embedding response parsing failed");

        public static readonly Action<ILogger, string, Exception> LogOpenAIBatchEmbeddingRequestError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(8006, "OpenAIBatchEmbeddingRequestError"),
            "OpenAI batch embedding request failed: {Error}");

        public static readonly Action<ILogger, Exception> LogOpenAIBatchEmbeddingParsingError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(8007, "OpenAIBatchEmbeddingParsingError"),
            "OpenAI batch embedding response parsing failed");

        public static readonly Action<ILogger, Exception> LogGeminiTextParsingError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(9001, "GeminiTextParsingError"),
            "Gemini text response parsing failed");

        public static readonly Action<ILogger, string, Exception> LogGeminiEmbeddingValidationError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(9002, "GeminiEmbeddingValidationError"),
            "Gemini embedding validation failed: {ErrorMessage}");

        public static readonly Action<ILogger, Exception> LogGeminiEmbeddingModelMissing = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(9003, "GeminiEmbeddingModelMissing"),
            "Gemini embedding model is required but not provided");

        public static readonly Action<ILogger, string, Exception> LogGeminiEmbeddingRequestError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(9004, "GeminiEmbeddingRequestError"),
            "Gemini embedding request failed: {Error}");

        public static readonly Action<ILogger, Exception> LogGeminiEmbeddingParsingError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(9005, "GeminiEmbeddingParsingError"),
            "Gemini embedding response parsing failed");

        public static readonly Action<ILogger, string, Exception> LogGeminiBatchEmbeddingRequestError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(9006, "GeminiBatchEmbeddingRequestError"),
            "Gemini batch embedding request failed: {Error}");

        public static readonly Action<ILogger, Exception> LogGeminiBatchEmbeddingParsingError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(9007, "GeminiBatchEmbeddingParsingError"),
            "Gemini batch embedding response parsing failed");

        public static readonly Action<ILogger, int, string, Exception> LogGeminiBatchFailedFallback = LoggerMessage.Define<int, string>(
            LogLevel.Warning,
            new EventId(9008, "GeminiBatchFailedFallback"),
            "Gemini batch {BatchIndex} failed, falling back to individual requests: {ErrorMessage}");

        public static readonly Action<ILogger, Exception> LogCustomTextParsingError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(10001, "CustomTextParsingError"),
            "Custom provider text response parsing failed");

        public static readonly Action<ILogger, string, Exception> LogCustomEmbeddingValidationError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(10002, "CustomEmbeddingValidationError"),
            "Custom provider embedding validation failed: {ErrorMessage}");

        public static readonly Action<ILogger, Exception> LogCustomEmbeddingModelMissing = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(10003, "CustomEmbeddingModelMissing"),
            "Custom provider embedding model is required but not provided");

        public static readonly Action<ILogger, string, Exception> LogCustomEmbeddingRequestError = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(10004, "CustomEmbeddingRequestError"),
            "Custom provider embedding request failed: {Error}");

        public static readonly Action<ILogger, Exception> LogCustomEmbeddingParsingError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(10005, "CustomEmbeddingParsingError"),
            "Custom provider embedding response parsing failed");
    }
}
