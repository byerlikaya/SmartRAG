using Microsoft.Extensions.Logging;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartRAG.Providers
{
    /// <summary>
    /// Base class for AI providers with common implementations
    /// </summary>
    public abstract class BaseAIProvider : IAIProvider
    {
        #region Constants

        // HTTP request constants
        private const int DefaultMaxRetries = 3;
        private const int BaseDelayMs = 1000;
        private const int MinRetryDelayMs = 60000;
        private const int MaxRetryDelayMs = int.MaxValue;

        // JSON parsing constants
        private const string DefaultDataProperty = "data";
        private const string DefaultEmbeddingProperty = "embedding";
        private const string DefaultChoicesProperty = "choices";
        private const string DefaultMessageProperty = "message";
        private const string DefaultContentProperty = "content";

        #endregion

        #region Fields

        private readonly ILogger _logger;

        #endregion

        #region Protected Properties

        /// <summary>
        /// Logger instance for derived classes
        /// </summary>
        protected ILogger Logger => _logger;

        #endregion

        #region Constructor

        protected BaseAIProvider(ILogger logger)
        {
            _logger = logger;
        }

        #endregion

        #region Abstract Properties

        /// <summary>
        /// Gets the type of AI provider this implementation represents
        /// </summary>
        public abstract AIProvider ProviderType { get; }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Generates text response from the AI provider
        /// </summary>
        /// <param name="prompt">The input prompt for text generation</param>
        /// <param name="config">AI provider configuration settings</param>
        /// <returns>Generated text response</returns>
        public abstract Task<string> GenerateTextAsync(string prompt, AIProviderConfig config);

        /// <summary>
        /// Generates vector embedding for the given text
        /// </summary>
        /// <param name="text">The text to generate embedding for</param>
        /// <param name="config">AI provider configuration settings</param>
        /// <returns>Vector embedding as list of floats</returns>
        public abstract Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config);

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Default batch embedding implementation that falls back to individual calls
        /// Providers can override this for better performance if they support batch operations
        /// </summary>
        public virtual async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config)
        {
            var results = new List<List<float>>();

            foreach (var text in texts)
            {
                try
                {
                    var embedding = await GenerateEmbeddingAsync(text, config);
                    results.Add(embedding);
                }
                catch
                {
                    // Add empty embedding on error to maintain index consistency
                    results.Add(new List<float>());
                }
            }

            return results;
        }

        /// <summary>
        /// Common text chunking implementation for all providers
        /// Uses StringBuilder for better performance
        /// </summary>
        public virtual Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Task.FromResult(new List<string>());

            var chunks = new List<string>();
            var sentences = text.Split(new char[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();

            foreach (var sentence in sentences)
            {
                var trimmedSentence = sentence.Trim();

                if (string.IsNullOrEmpty(trimmedSentence))
                    continue;

                if (current.Length + trimmedSentence.Length + 1 > maxChunkSize && current.Length > 0)
                {
                    chunks.Add(current.ToString().Trim());
                    current.Clear();
                }

                if (current.Length > 0)
                    current.Append(' ');

                current.Append(trimmedSentence).Append('.');
            }

            if (current.Length > 0)
                chunks.Add(current.ToString().Trim());

            return Task.FromResult(chunks);
        }

        #endregion

        #region Protected Helper Methods

        /// <summary>
        /// Validates common configuration requirements
        /// </summary>
        protected (bool isValid, string errorMessage) ValidateConfig(AIProviderConfig config, bool requireApiKey = true, bool requireEndpoint = true, bool requireModel = true)
        {
            if (requireApiKey && string.IsNullOrEmpty(config.ApiKey))
                return (false, $"{ProviderType} API key missing. Configure {ProviderType}:ApiKey.");

            if (requireEndpoint && string.IsNullOrEmpty(config.Endpoint))
                return (false, $"{ProviderType} endpoint missing. Configure {ProviderType}:Endpoint.");

            if (requireModel && string.IsNullOrEmpty(config.Model))
                return (false, $"{ProviderType} model missing. Configure {ProviderType}:Model.");

            return (true, string.Empty);
        }

        /// <summary>
        /// Creates HttpClient with common headers
        /// </summary>
        protected static HttpClient CreateHttpClient(string apiKey = null, Dictionary<string, string> additionalHeaders = null)
        {
            var handler = CreateHttpClientHandler();
            var client = new HttpClient(handler);

            if (!string.IsNullOrEmpty(apiKey))
            {
                // Generic auth header - providers can override if needed
                var authHeader = "Authorization";
                var authValue = $"Bearer {apiKey}";
                client.DefaultRequestHeaders.Add(authHeader, authValue);
            }

            if (additionalHeaders != null)
            {
                AddAdditionalHeaders(client, additionalHeaders);
            }

            return client;
        }

        /// <summary>
        /// Creates HTTP client without automatic Authorization header (for providers like Anthropic that use custom headers)
        /// </summary>
        protected static HttpClient CreateHttpClientWithoutAuth(Dictionary<string, string> additionalHeaders)
        {
            var handler = CreateHttpClientHandler();
            var client = new HttpClient(handler);

            if (additionalHeaders != null)
            {
                AddAdditionalHeaders(client, additionalHeaders);
            }

            return client;
        }

        /// <summary>
        /// Common HTTP POST request with error handling and retry logic
        /// </summary>
        protected async Task<(bool success, string response, string errorMessage)> MakeHttpRequestAsync(
            HttpClient client, string endpoint, object payload, int maxRetries = DefaultMaxRetries)
        {
            var attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    var (success, response, error) = await ExecuteHttpRequestAsync(client, endpoint, payload);

                    if (success)
                        return (true, response, string.Empty);

                    // Handle rate limiting and server overload
                    if (error.Contains("429") || error.Contains("TooManyRequests") ||
                        error.Contains("529") || error.Contains("Overloaded"))
                    {
                        attempt++;
                        if (attempt < maxRetries)
                        {
                            var delayMs = CalculateRetryDelay(attempt);
                            await Task.Delay(delayMs);
                            continue;
                        }
                    }

                    return (false, string.Empty, error);
                }
                catch (Exception ex)
                {
                    attempt++;
                    if (attempt < maxRetries)
                    {
                        var delay = CalculateExponentialBackoffDelay(attempt);
                        await Task.Delay(delay);
                        continue;
                    }
                    return (false, string.Empty, $"{ProviderType} request failed: {ex.Message}");
                }
            }

            return (false, string.Empty, $"{ProviderType} request failed after {maxRetries} attempts");
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Creates HttpClientHandler with SSL/TLS configuration
        /// </summary>
        private static HttpClientHandler CreateHttpClientHandler()
        {
            var handler = new HttpClientHandler();

            // Handle SSL/TLS issues for all providers
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                // For development/testing, you might want to bypass certificate validation
                // In production, this should be more restrictive
                return true;
            };

            return handler;
        }

        /// <summary>
        /// Adds additional headers to HttpClient
        /// </summary>
        private static void AddAdditionalHeaders(HttpClient client, Dictionary<string, string> headers)
        {
            foreach (var header in headers)
            {
                if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    continue;

                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        /// <summary>
        /// Executes a single HTTP request
        /// </summary>
        private async Task<(bool success, string response, string error)> ExecuteHttpRequestAsync(
            HttpClient client, string endpoint, object payload)
        {
            var options = GetJsonSerializerOptions();
            var json = JsonSerializer.Serialize(payload, options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return (true, responseBody, string.Empty);
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            return (false, string.Empty, $"{ProviderType} error: {response.StatusCode} - {errorBody}");
        }

        /// <summary>
        /// Calculates retry delay for rate limiting and server overload
        /// </summary>
        private static int CalculateRetryDelay(int attempt)
        {
            // For server overload (529), use exponential backoff starting from 60 seconds
            return MinRetryDelayMs * attempt;
        }

        /// <summary>
        /// Calculates exponential backoff delay for retries
        /// </summary>
        private static int CalculateExponentialBackoffDelay(int attempt)
        {
            return BaseDelayMs * (int)Math.Pow(2, attempt - 1);
        }

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// Common embedding response parsing
        /// </summary>
        protected static List<float> ParseEmbeddingResponse(string responseBody, string dataProperty = DefaultDataProperty, string embeddingProperty = DefaultEmbeddingProperty)
        {
            using (var doc = JsonDocument.Parse(responseBody))
            {
                if (doc.RootElement.TryGetProperty(dataProperty, out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    var firstData = data.EnumerateArray().FirstOrDefault();

                    if (firstData.TryGetProperty(embeddingProperty, out var embedding) && embedding.ValueKind == JsonValueKind.Array)
                    {
                        var floats = new List<float>();

                        foreach (var value in embedding.EnumerateArray())
                        {
                            if (value.TryGetSingle(out var f))
                                floats.Add(f);
                        }

                        return floats;
                    }
                }

                return new List<float>();
            }
        }

        /// <summary>
        /// Common batch embedding response parsing for OpenAI-like APIs
        /// </summary>
        protected static List<List<float>> ParseBatchEmbeddingResponse(string responseBody, int expectedCount, string dataProperty = DefaultDataProperty, string embeddingProperty = DefaultEmbeddingProperty)
        {
            var results = new List<List<float>>();

            try
            {
                using (var doc = JsonDocument.Parse(responseBody))
                {
                    if (doc.RootElement.TryGetProperty(dataProperty, out var data) && data.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in data.EnumerateArray())
                        {
                            if (item.TryGetProperty(embeddingProperty, out var embedding) && embedding.ValueKind == JsonValueKind.Array)
                            {
                                var floats = new List<float>(embedding.GetArrayLength());
                                foreach (var value in embedding.EnumerateArray())
                                {
                                    if (value.TryGetSingle(out var f))
                                        floats.Add(f);
                                }
                                results.Add(floats);
                            }
                            else
                            {
                                results.Add(new List<float>());
                            }
                        }
                    }
                }
            }
            catch
            {
                // Return partial results on error
            }

            // Ensure we return the expected number of embeddings
            return Enumerable.Range(0, expectedCount)
                .Select(i => i < results.Count ? results[i] : new List<float>())
                .ToList();
        }

        /// <summary>
        /// Common text generation response parsing for OpenAI-like APIs
        /// </summary>
        protected static string ParseTextResponse(string responseBody, string choicesProperty = DefaultChoicesProperty, string messageProperty = DefaultMessageProperty, string contentProperty = DefaultContentProperty)
        {
            using (var doc = JsonDocument.Parse(responseBody))
            {
                if (doc.RootElement.TryGetProperty(choicesProperty, out var choices) && choices.ValueKind == JsonValueKind.Array)
                {
                    var firstChoice = choices.EnumerateArray().FirstOrDefault();

                    if (firstChoice.TryGetProperty(messageProperty, out var message) && message.TryGetProperty(contentProperty, out var contentProp))
                        return contentProp.GetString() ?? "No response generated";
                }

                return "No response generated";
            }
        }

        /// <summary>
        /// Shared JsonSerializerOptions for consistent serialization
        /// </summary>
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Get shared JsonSerializerOptions instance
        /// </summary>
        private static JsonSerializerOptions GetJsonSerializerOptions() => _jsonOptions;

        #endregion
    }
}
