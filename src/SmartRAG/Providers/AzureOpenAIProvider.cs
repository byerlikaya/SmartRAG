using Microsoft.Extensions.Logging;
using SmartRAG.Enums;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Providers
{

    /// <summary>
    /// Azure OpenAI provider implementation
    /// </summary>
    public class AzureOpenAIProvider : BaseAIProvider, IDisposable
    {
        private readonly ILogger<AzureOpenAIProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the AzureOpenAIProvider
        /// </summary>
        /// <param name="logger">Logger instance for this provider</param>
        public AzureOpenAIProvider(ILogger<AzureOpenAIProvider> logger) : base(logger)
        {
            _logger = logger;
        }

        #region Constants

        private const int DefaultMaxRetries = 3;
        private const int DefaultMinIntervalMs = 60000; // 60 seconds

        #endregion

        #region Fields

        private readonly SemaphoreSlim _rateLimitSemaphore = new SemaphoreSlim(1, 1);
        private DateTime _lastRequestTime = DateTime.MinValue;

        #endregion

        #region Properties

        public override AIProvider ProviderType => AIProvider.AzureOpenAI;

        #endregion

        #region Public Methods

        public override async Task<string> GenerateTextAsync(string prompt, AIProviderConfig config)
        {
            var (isValid, errorMessage) = ValidateConfig(config);

            if (!isValid)
                return errorMessage;

            using var client = CreateHttpClient(config.ApiKey);
            var messages = new List<object>();

            if (!string.IsNullOrEmpty(config.SystemMessage))
            {
                messages.Add(new { role = "system", content = config.SystemMessage });
            }

            messages.Add(new { role = "user", content = prompt });

            var payload = new
            {
                messages = messages.ToArray(),
                max_tokens = config.MaxTokens,
                temperature = config.Temperature,
                stream = false
            };

            var url = BuildAzureUrl(config.Endpoint, config.Model, "chat/completions", config.ApiVersion);

            var (success, response, error) = await MakeHttpRequestAsyncWithRateLimit(client, url, payload, config);

            if (!success)
                return error;

            try
            {
                return ParseTextResponse(response);
            }
            catch (Exception ex)
            {
                ProviderLogMessages.LogAzureOpenAITextParsingError(Logger, ex);
                return $"Error parsing Azure OpenAI response: {ex.Message}";
            }
        }

        public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
        {
            var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

            if (!isValid)
            {
                ProviderLogMessages.LogAzureOpenAIEmbeddingValidationError(Logger, errorMessage, null);
                return new List<float>();
            }

            if (string.IsNullOrEmpty(config.EmbeddingModel))
            {
                ProviderLogMessages.LogAzureOpenAIEmbeddingModelMissing(Logger, null);
                return new List<float>();
            }

            using var client = CreateHttpClient(config.ApiKey);
            var payload = new
            {
                input = text
            };

            var url = BuildAzureUrl(config.Endpoint, config.EmbeddingModel, "embeddings", config.ApiVersion);

            var (success, response, error) = await MakeHttpRequestAsyncWithRateLimit(client, url, payload, config);

            if (!success)
            {
                ProviderLogMessages.LogAzureOpenAIEmbeddingRequestError(Logger, error, null);
                return new List<float>();
            }

            try
            {
                return ParseEmbeddingResponse(response);
            }
            catch (Exception ex)
            {
                ProviderLogMessages.LogAzureOpenAIEmbeddingParsingError(Logger, ex);
                return new List<float>();
            }
        }

        public override async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config)
        {
            var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

            if (!isValid)
            {
                ProviderLogMessages.LogAzureOpenAIEmbeddingValidationError(Logger, errorMessage, null);
                return new List<List<float>>();
            }

            if (string.IsNullOrEmpty(config.EmbeddingModel))
            {
                ProviderLogMessages.LogAzureOpenAIEmbeddingModelMissing(Logger, null);
                return new List<List<float>>();
            }

            var inputList = texts?.ToList() ?? new List<string>();
            if (inputList.Count == 0)
                return new List<List<float>>();

            using var client = CreateHttpClient(config.ApiKey);
            var payload = new
            {
                input = inputList.ToArray()
            };

            var url = BuildAzureUrl(config.Endpoint, config.EmbeddingModel, "embeddings", config.ApiVersion);

            var (success, response, error) = await MakeHttpRequestAsyncWithRateLimit(client, url, payload, config);

            if (!success)
            {
                ProviderLogMessages.LogAzureOpenAIBatchEmbeddingRequestError(Logger, error, null);
                return ParseBatchEmbeddingResponse("", inputList.Count);
            }

            try
            {
                return ParseBatchEmbeddingResponse(response, inputList.Count);
            }
            catch (Exception ex)
            {
                ProviderLogMessages.LogAzureOpenAIBatchEmbeddingParsingError(Logger, ex);
                return ParseBatchEmbeddingResponse("", inputList.Count);
            }
        }

        public void Dispose()
        {
            _rateLimitSemaphore.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Azure OpenAI URL builder
        /// </summary>
        private static string BuildAzureUrl(string endpoint, string deployment, string operation, string apiVersion)
        {
            return $"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/{operation}?api-version={apiVersion}";
        }

        /// <summary>
        /// Azure OpenAI S0 tier custom rate limiting (3 RPM)
        /// </summary>
        private async Task<(bool success, string response, string error)> MakeHttpRequestAsyncWithRateLimit(
            HttpClient client, string endpoint, object payload, AIProviderConfig config)
        {
            var minIntervalMs = Math.Max(0, config.EmbeddingMinIntervalMs ?? DefaultMinIntervalMs);

            await _rateLimitSemaphore.WaitAsync();
            try
            {
                await WaitForRateLimit(minIntervalMs);
                _lastRequestTime = DateTime.UtcNow;

                return await MakeHttpRequestAsync(client, endpoint, payload, maxRetries: DefaultMaxRetries);
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
        }

        /// <summary>
        /// Calculate wait time for rate limiting
        /// </summary>
        private async Task WaitForRateLimit(int minIntervalMs)
        {
            var now = DateTime.UtcNow;
            var timeSinceLastRequest = now - _lastRequestTime;

            if (timeSinceLastRequest.TotalMilliseconds < minIntervalMs)
            {
                var waitTime = minIntervalMs - (int)timeSinceLastRequest.TotalMilliseconds;
                ProviderLogMessages.LogAzureOpenAIRateLimit(Logger, waitTime, null);
                await Task.Delay(waitTime);
            }
        }

        #endregion
    }
}
