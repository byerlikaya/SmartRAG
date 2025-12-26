using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Enums;
using SmartRAG.Interfaces.AI;
using SmartRAG.Models;
using SmartRAG.Services.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Services.AI
{
    /// <summary>
    /// AI Service that orchestrates AI requests with retry and fallback logic
    /// </summary>
    public class AIService : IAIService
    {
        private readonly IAIRequestExecutor _requestExecutor;
        private readonly SmartRagOptions _options;
        private readonly ILogger<AIService> _logger;

        private const int MinRetryAttempts = 1;
        private const int MinRetryDelayMs = 0;
        private const string FallbackUnavailableMessage = "I'm sorry, all AI providers are currently unavailable.";
        private const string ErrorMessage = "I encountered an error while processing your request. Please try again later.";

        public AIService(
            IAIRequestExecutor requestExecutor,
            IOptions<SmartRagOptions> options,
            ILogger<AIService> logger)
        {
            _requestExecutor = requestExecutor;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// [AI Query] Generates a response using the configured AI provider with retry logic
        /// </summary>
        public async Task<string> GenerateResponseAsync(string query, IEnumerable<string> context)
        {
            try
            {
                return await GenerateResponseWithRetryAsync(_options.AIProvider, query, context);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogAIServiceGenerateResponseError(_logger, _options.AIProvider.ToString(), ex);

                if (_options.EnableFallbackProviders && _options.FallbackProviders.Count > 0)
                {
                    try
                    {
                        return await TryFallbackProvidersAsync(query, context);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Fallback providers failed");
                    }
                }

                return ErrorMessage;
            }
        }

        /// <summary>
        /// [AI Query] Generates embeddings for a single text
        /// </summary>
        public async Task<List<float>> GenerateEmbeddingsAsync(string text)
        {
            try
            {
                return await _requestExecutor.GenerateEmbeddingsAsync(_options.AIProvider, text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embeddings");
                throw;
            }
        }

        /// <summary>
        /// [AI Query] Generates embeddings for a batch of texts
        /// </summary>
        public async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts)
        {
            try
            {
                return await _requestExecutor.GenerateEmbeddingsBatchAsync(_options.AIProvider, texts);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogAIServiceBatchEmbeddingError(_logger, _options.AIProvider.ToString(), ex);
                return new List<List<float>>();
            }
        }

        private async Task<string> GenerateResponseWithRetryAsync(AIProvider provider, string query, IEnumerable<string> context)
        {
            var attempt = 0;
            var maxAttempts = Math.Max(MinRetryAttempts, _options.MaxRetryAttempts);
            var delayMs = Math.Max(MinRetryDelayMs, _options.RetryDelayMs);

            while (true)
            {
                try
                {
                    return await _requestExecutor.GenerateResponseAsync(provider, query, context);
                }
                catch (Exception ex) when (_options.RetryPolicy != RetryPolicy.None && attempt < maxAttempts - 1)
                {
                    attempt++;
                    var backoff = CalculateRetryDelay(attempt, delayMs);

                    ServiceLogMessages.LogAIServiceRetryAttempt(_logger, attempt, provider.ToString(), backoff, ex);

                    await Task.Delay(backoff);
                }
            }
        }

        private int CalculateRetryDelay(int attempt, int baseDelayMs)
        {
            return _options.RetryPolicy switch
            {
                RetryPolicy.FixedDelay => baseDelayMs,
                RetryPolicy.LinearBackoff => baseDelayMs * attempt,
                RetryPolicy.ExponentialBackoff => baseDelayMs * (int)Math.Pow(2, attempt - 1),
                _ => baseDelayMs,
            };
        }

        private async Task<string> TryFallbackProvidersAsync(string query, IEnumerable<string> context)
        {
            foreach (var fallbackProvider in _options.FallbackProviders)
            {
                try
                {
                    var response = await _requestExecutor.GenerateResponseAsync(fallbackProvider, query, context);

                    if (!string.IsNullOrEmpty(response))
                    {
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    ServiceLogMessages.LogAIServiceFallbackFailed(_logger, fallbackProvider.ToString(), ex);
                    continue;
                }
            }

            _logger.LogWarning("All fallback providers failed");
            return FallbackUnavailableMessage;
        }
    }
}
