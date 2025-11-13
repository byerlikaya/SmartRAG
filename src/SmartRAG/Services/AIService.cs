using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services
{

    /// <summary>
    /// AI Service that uses the configured AI provider
    /// </summary>
    public class AIService : IAIService
    {
        private readonly IAIProviderFactory _aiProviderFactory;
        private readonly SmartRagOptions _options;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIService> _logger;

        /// <summary>
        /// Initializes a new instance of the AIService
        /// </summary>
        /// <param name="aiProviderFactory">Factory for creating AI provider instances</param>
        /// <param name="options">SmartRAG configuration options</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger instance for this service</param>
        public AIService(
        IAIProviderFactory aiProviderFactory,
        IOptions<SmartRagOptions> options,
        IConfiguration configuration,
        ILogger<AIService> logger)
        {
            _aiProviderFactory = aiProviderFactory;
            _options = options.Value;
            _configuration = configuration;
            _logger = logger;
        }

        #region Constants

        // Retry and fallback constants
        private const int MinRetryAttempts = 1;
        private const int MinRetryDelayMs = 0;
        private const string ContextPrefix = "Context:\n";
        private const string QuestionPrefix = "\n\nQuestion: ";
        private const string AnswerPrefix = "\n\nAnswer:";
        private const string FallbackUnavailableMessage = "I'm sorry, all AI providers are currently unavailable.";
        private const string NoResponseMessage = "I'm sorry, I couldn't generate a response at this time.";
        private const string ErrorMessage = "I encountered an error while processing your request. Please try again later.";

        #endregion

        #region Fields

        #endregion

        #region Public Methods

        public async Task<string> GenerateResponseAsync(string query, IEnumerable<string> context)
        {
            try
            {
                var aiProvider = _aiProviderFactory.CreateProvider(_options.AIProvider);
                var providerKey = _options.AIProvider.ToString();
                var providerConfig = _configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

                if (providerConfig == null)
                    return $"AI provider configuration not found for '{providerKey}'.";

                var response = await GenerateResponseWithRetryAsync(aiProvider, query, context, providerConfig);

                if (!string.IsNullOrEmpty(response))
                    return response;

                // Try fallback providers if enabled
                if (_options.EnableFallbackProviders && _options.FallbackProviders.Count > 0)
                {
                    return await TryFallbackProvidersAsync(query, context);
                }

                return NoResponseMessage;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogAIServiceGenerateResponseError(_logger, _options.AIProvider.ToString(), ex);

                // Try fallback providers on error if enabled
                if (_options.EnableFallbackProviders && _options.FallbackProviders.Count > 0)
                {
                    try
                    {
                        return await TryFallbackProvidersAsync(query, context);
                    }
                    catch (Exception fallbackEx)
                    {
                        ServiceLogMessages.LogAIServiceFallbackError(_logger, query, fallbackEx);
                    }
                }

                return ErrorMessage;
            }
        }

        public async Task<List<float>> GenerateEmbeddingsAsync(string text)
        {
            try
            {
                var selectedProvider = _options.AIProvider;
                var aiProvider = _aiProviderFactory.CreateProvider(selectedProvider);
                var providerKey = selectedProvider.ToString();
                var providerConfig = _configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

                if (providerConfig == null)
                {
                    ServiceLogMessages.LogAIServiceProviderConfigNotFound(_logger, selectedProvider.ToString(), null);
                    return new List<float>();
                }

                var embeddings = await aiProvider.GenerateEmbeddingAsync(text, providerConfig);
                return embeddings;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogAIServiceEmbeddingError(_logger, text, ex);
                throw;
            }
        }

        public async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts)
        {
            try
            {
                var selectedProvider = _options.AIProvider;
                var aiProvider = _aiProviderFactory.CreateProvider(selectedProvider);
                var providerKey = selectedProvider.ToString();
                var providerConfig = _configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

                if (providerConfig == null)
                {
                    ServiceLogMessages.LogAIServiceProviderConfigNotFound(_logger, selectedProvider.ToString(), null);
                    return new List<List<float>>();
                }

                // Use batch embedding if supported by the provider
                var embeddings = await aiProvider.GenerateEmbeddingsBatchAsync(texts, providerConfig);
                var filteredEmbeddings = embeddings?.Where(e => e != null && e.Count > 0).ToList() ?? new List<List<float>>();

                ServiceLogMessages.LogAIServiceBatchEmbeddingsGenerated(_logger, filteredEmbeddings.Count, selectedProvider.ToString(), null);
                return filteredEmbeddings;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogAIServiceBatchEmbeddingError(_logger, _options.AIProvider.ToString(), ex);
                return new List<List<float>>();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determines whether the AI provider response indicates an error.
        /// </summary>
        private static bool IsErrorResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return true;
            }

            var trimmed = response.TrimStart();
            if (trimmed.StartsWith("error", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (trimmed.StartsWith("gemini error", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("anthropic error", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("openai error", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("azureopenai error", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("custom error", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (trimmed.StartsWith("{\"error\"", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (trimmed.Contains(" error:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains(" error -", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (trimmed.Contains("ServiceUnavailable", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Generate response with retry logic
        /// </summary>
        private async Task<string> GenerateResponseWithRetryAsync(IAIProvider aiProvider, string query, IEnumerable<string> context, AIProviderConfig providerConfig)
        {
            var attempt = 0;
            var maxAttempts = Math.Max(MinRetryAttempts, _options.MaxRetryAttempts);
            var delayMs = Math.Max(MinRetryDelayMs, _options.RetryDelayMs);

            while (true)
            {
                try
                {
                    var prompt = BuildPrompt(query, context);
                    var response = await aiProvider.GenerateTextAsync(prompt, providerConfig) ?? string.Empty;

                    if (IsErrorResponse(response))
                    {
                        throw new InvalidOperationException(string.IsNullOrWhiteSpace(response)
                            ? "Empty response received from AI provider."
                            : response);
                    }

                    return response;
                }
                catch (Exception ex) when (_options.RetryPolicy != RetryPolicy.None && attempt < maxAttempts - 1)
                {
                    attempt++;
                    var backoff = CalculateRetryDelay(attempt, delayMs);

                    ServiceLogMessages.LogAIServiceRetryAttempt(_logger, attempt, _options.AIProvider.ToString(), backoff, ex);

                    await Task.Delay(backoff);
                }
            }
        }

        /// <summary>
        /// Build prompt with context and query
        /// </summary>
        private static string BuildPrompt(string query, IEnumerable<string> context)
        {
            var contextText = string.Join("\n\n", context);
            return $"{ContextPrefix}{contextText}{QuestionPrefix}{query}{AnswerPrefix}";
        }

        /// <summary>
        /// Calculate retry delay based on policy
        /// </summary>
        private int CalculateRetryDelay(int attempt, int baseDelayMs)
        {
            switch (_options.RetryPolicy)
            {
                case RetryPolicy.FixedDelay:
                    return baseDelayMs;
                case RetryPolicy.LinearBackoff:
                    return baseDelayMs * attempt;
                case RetryPolicy.ExponentialBackoff:
                    return baseDelayMs * (int)Math.Pow(2, attempt - 1);
                default:
                    return baseDelayMs;
            }
        }

        /// <summary>
        /// Try fallback providers when primary provider fails
        /// </summary>
        private async Task<string> TryFallbackProvidersAsync(string query, IEnumerable<string> context)
        {
            foreach (var fallbackProvider in _options.FallbackProviders)
            {
                try
                {
                    var aiProvider = _aiProviderFactory.CreateProvider(fallbackProvider);
                    var key = fallbackProvider.ToString();
                    var config = _configuration.GetSection($"AI:{key}").Get<AIProviderConfig>();

                    if (config == null)
                        continue;

                    var prompt = BuildPrompt(query, context);
                    var response = await aiProvider.GenerateTextAsync(prompt, config) ?? string.Empty;

                    if (!IsErrorResponse(response) && !string.IsNullOrEmpty(response))
                    {
                        ServiceLogMessages.LogAIServiceFallbackSuccess(_logger, fallbackProvider.ToString(), null);
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    ServiceLogMessages.LogAIServiceFallbackFailed(_logger, fallbackProvider.ToString(), ex);
                    continue;
                }
            }

            ServiceLogMessages.LogAIServiceAllFallbacksFailed(_logger, query, null);
            return FallbackUnavailableMessage;
        }

        #endregion
    }
}
