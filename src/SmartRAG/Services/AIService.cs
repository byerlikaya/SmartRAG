using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services;

/// <summary>
/// AI Service that uses the configured AI provider
/// </summary>
public class AIService(
    IAIProviderFactory aiProviderFactory,
    IOptions<SmartRagOptions> options,
    IConfiguration configuration,
    ILogger<AIService> logger) : IAIService
{
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

    private readonly SmartRagOptions _options = options.Value;

    #endregion

    #region Public Methods

    public async Task<string> GenerateResponseAsync(string query, IEnumerable<string> context)
    {
        try
        {
            var aiProvider = aiProviderFactory.CreateProvider(_options.AIProvider);
            var providerKey = _options.AIProvider.ToString();
            var providerConfig = configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

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
            ServiceLogMessages.LogAIServiceGenerateResponseError(logger, _options.AIProvider.ToString(), ex);

            // Try fallback providers on error if enabled
            if (_options.EnableFallbackProviders && _options.FallbackProviders.Count > 0)
            {
                try
                {
                    return await TryFallbackProvidersAsync(query, context);
                }
                catch (Exception fallbackEx)
                {
                    ServiceLogMessages.LogAIServiceFallbackError(logger, query, fallbackEx);
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
            var aiProvider = aiProviderFactory.CreateProvider(selectedProvider);
            var providerKey = selectedProvider.ToString();
            var providerConfig = configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

            if (providerConfig == null)
            {
                ServiceLogMessages.LogAIServiceProviderConfigNotFound(logger, selectedProvider.ToString(), null);
                return [];
            }

            var embeddings = await aiProvider.GenerateEmbeddingAsync(text, providerConfig);
            return embeddings;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogAIServiceEmbeddingError(logger, text, ex);
            throw;
        }
    }

    public async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts)
    {
        try
        {
            var selectedProvider = _options.AIProvider;
            var aiProvider = aiProviderFactory.CreateProvider(selectedProvider);
            var providerKey = selectedProvider.ToString();
            var providerConfig = configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

            if (providerConfig == null)
            {
                ServiceLogMessages.LogAIServiceProviderConfigNotFound(logger, selectedProvider.ToString(), null);
                return [];
            }

            // Use batch embedding if supported by the provider
            var embeddings = await aiProvider.GenerateEmbeddingsBatchAsync(texts, providerConfig);
            var filteredEmbeddings = embeddings?.Where(e => e != null && e.Count > 0).ToList() ?? new List<List<float>>();

            ServiceLogMessages.LogAIServiceBatchEmbeddingsGenerated(logger, filteredEmbeddings.Count, selectedProvider.ToString(), null);
            return filteredEmbeddings;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogAIServiceBatchEmbeddingError(logger, _options.AIProvider.ToString(), ex);
            return [];
        }
    }

    #endregion

    #region Private Methods

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
                var response = await aiProvider.GenerateTextAsync(prompt, providerConfig);
                return response;
            }
            catch (Exception ex) when (_options.RetryPolicy != RetryPolicy.None && attempt < maxAttempts - 1)
            {
                attempt++;
                var backoff = CalculateRetryDelay(attempt, delayMs);

                ServiceLogMessages.LogAIServiceRetryAttempt(logger, attempt, _options.AIProvider.ToString(), backoff, ex);

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
        return _options.RetryPolicy switch
        {
            RetryPolicy.FixedDelay => baseDelayMs,
            RetryPolicy.LinearBackoff => baseDelayMs * attempt,
            RetryPolicy.ExponentialBackoff => baseDelayMs * (int)Math.Pow(2, attempt - 1),
            _ => baseDelayMs
        };
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
                var aiProvider = aiProviderFactory.CreateProvider(fallbackProvider);
                var key = fallbackProvider.ToString();
                var config = configuration.GetSection($"AI:{key}").Get<AIProviderConfig>();

                if (config == null)
                    continue;

                var prompt = BuildPrompt(query, context);
                var response = await aiProvider.GenerateTextAsync(prompt, config);

                if (!string.IsNullOrEmpty(response))
                {
                    ServiceLogMessages.LogAIServiceFallbackSuccess(logger, fallbackProvider.ToString(), null);
                    return response;
                }
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogAIServiceFallbackFailed(logger, fallbackProvider.ToString(), ex);
                continue;
            }
        }

        ServiceLogMessages.LogAIServiceAllFallbacksFailed(logger, query, null);
        return FallbackUnavailableMessage;
    }

    #endregion
}
