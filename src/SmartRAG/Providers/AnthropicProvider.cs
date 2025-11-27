using Microsoft.Extensions.Logging;
using SmartRAG.Enums;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartRAG.Providers
{

    /// <summary>
    /// Anthropic Claude provider implementation
    /// </summary>
    public class AnthropicProvider : BaseAIProvider
    {
        private readonly ILogger<AnthropicProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the AnthropicProvider
        /// </summary>
        /// <param name="logger">Logger instance for this provider</param>
        public AnthropicProvider(ILogger<AnthropicProvider> logger) : base(logger)
        {
            _logger = logger;
        }

        #region Constants

        private const string AnthropicApiVersion = "2023-06-01";
        private const string DefaultVoyageEndpoint = "https://api.voyageai.com";
        private const string DefaultVoyageModel = "voyage-3.5";
        private const string VoyageInputType = "document";

        #endregion

        #region Properties

        public override AIProvider ProviderType => AIProvider.Anthropic;

        #endregion

        #region Public Methods

        public override async Task<string> GenerateTextAsync(string prompt, AIProviderConfig config)
        {
            var (isValid, errorMessage) = ValidateConfig(config);

            if (!isValid)
                return errorMessage;

            var additionalHeaders = new Dictionary<string, string>
        {
            { "x-api-key", config.ApiKey },
            { "anthropic-version", AnthropicApiVersion }
        };

            using var client = CreateHttpClientWithoutAuth(additionalHeaders);
            var systemMessage = config.SystemMessage;

            var messages = new List<object>();

            if (!string.IsNullOrEmpty(systemMessage))
            {
                messages.Add(new { role = "system", content = systemMessage });
            }

            messages.Add(new { role = "user", content = prompt });

            var payload = new
            {
                model = config.Model,
                max_tokens = config.MaxTokens,
                temperature = config.Temperature,
                messages = messages.ToArray()
            };

            var chatEndpoint = BuildAnthropicUrl(config.Endpoint, "v1/messages");

            var (success, response, error) = await MakeHttpRequestAsync(client, chatEndpoint, payload);

            if (!success)
                return error;

            try
            {
                return ParseAnthropicTextResponse(response);
            }
            catch (Exception ex)
            {
                ProviderLogMessages.LogAnthropicResponseParsingError(Logger, ex.Message, ex);
                return $"Error parsing response: {ex.Message}";
            }
        }

        public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
        {

            var (isValid, errorMessage) = ValidateEmbeddingConfig(config);
            if (!isValid)
            {
                ProviderLogMessages.LogAnthropicEmbeddingValidationError(Logger, errorMessage, null);
                return new List<float>();
            }

            var voyageApiKey = config.EmbeddingApiKey ?? config.ApiKey;
            var voyageModel = config.EmbeddingModel ?? DefaultVoyageModel;

            var additionalHeaders = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {voyageApiKey}" }
            };

            using var client = CreateHttpClientWithoutAuth(additionalHeaders);
            var payload = new
            {
                input = new[] { text },
                model = voyageModel,
                input_type = VoyageInputType
            };

            var embeddingEndpoint = BuildVoyageUrl("v1/embeddings");

            var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint, payload);

            if (!success)
            {
                ProviderLogMessages.LogAnthropicEmbeddingRequestError(Logger, error, null);
                return new List<float>();
            }

            try
            {
                return ParseVoyageEmbeddingResponse(response);
            }
            catch (Exception ex)
            {
                ProviderLogMessages.LogVoyageParsingError(Logger, ex);
                return new List<float>();
            }
        }

        public override async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config)
        {
            var (isValid, errorMessage) = ValidateEmbeddingConfig(config);
            if (!isValid)
            {
                ProviderLogMessages.LogAnthropicEmbeddingValidationError(Logger, errorMessage, null);
                return new List<List<float>>();
            }

            var inputList = texts?.ToList() ?? new List<string>();
            if (inputList.Count == 0)
                return new List<List<float>>();

            var validInputs = inputList.Where(text => !string.IsNullOrWhiteSpace(text)).ToList();

            if (validInputs.Count == 0)
            {
                ProviderLogMessages.LogAnthropicEmbeddingValidationError(Logger, "All input texts are empty or null", null);
                return Enumerable.Range(0, inputList.Count).Select(_ => new List<float>()).ToList();
            }

            var voyageApiKey = config.EmbeddingApiKey ?? config.ApiKey;
            var voyageModel = config.EmbeddingModel ?? DefaultVoyageModel;

            var additionalHeaders = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {voyageApiKey}" }
        };

            using (var client = CreateHttpClientWithoutAuth(additionalHeaders))
            {
                var payload = new
                {
                    input = validInputs.ToArray(),
                    model = voyageModel,
                    input_type = VoyageInputType
                };

                var embeddingEndpoint = BuildVoyageUrl("v1/embeddings");

                var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint, payload);

                if (!success)
                {
                    ProviderLogMessages.LogAnthropicBatchEmbeddingRequestError(Logger, error, null);
                    return ParseVoyageBatchEmbeddingResponse("", inputList.Count);
                }

                try
                {
                    var validEmbeddings = ParseVoyageBatchEmbeddingResponse(response, validInputs.Count);
                    return MapEmbeddingsToOriginalInputs(validEmbeddings, inputList, validInputs);
                }
                catch (Exception ex)
                {
                    ProviderLogMessages.LogVoyageParsingError(Logger, ex);
                    return ParseVoyageBatchEmbeddingResponse("", inputList.Count);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Build Anthropic API URL
        /// </summary>
        private static string BuildAnthropicUrl(string endpoint, string path)
        {
            return $"{endpoint.TrimEnd('/')}/{path}";
        }

        /// <summary>
        /// Build Voyage AI API URL
        /// </summary>
        private static string BuildVoyageUrl(string path)
        {
            return $"{DefaultVoyageEndpoint}/{path}";
        }

        /// <summary>
        /// Validate embedding-specific configuration
        /// </summary>
        private static (bool isValid, string errorMessage) ValidateEmbeddingConfig(AIProviderConfig config)
        {
            var voyageApiKey = config.EmbeddingApiKey ?? config.ApiKey;

            if (string.IsNullOrEmpty(voyageApiKey))
                return (false, "Voyage API key is required for embeddings");

            return (true, string.Empty);
        }

        /// <summary>
        /// Parse Anthropic text response
        /// </summary>
        private static string ParseAnthropicTextResponse(string response)
        {
            using (var doc = JsonDocument.Parse(response))
            {
                if (doc.RootElement.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
                {
                    var firstContent = contentArray.EnumerateArray().FirstOrDefault();

                    if (firstContent.TryGetProperty("text", out var text))
                        return text.GetString() ?? "No response generated";
                }

                return "No response generated";
            }
        }

        /// <summary>
        /// Parse Voyage AI single embedding response
        /// </summary>
        private static List<float> ParseVoyageEmbeddingResponse(string response)
        {
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
            {
                var firstEmbedding = dataArray.EnumerateArray().FirstOrDefault();

                if (firstEmbedding.TryGetProperty("embedding", out var embeddingArray) && embeddingArray.ValueKind == JsonValueKind.Array)
                {
                    return embeddingArray.EnumerateArray()
                        .Select(x => x.GetSingle())
                        .ToList();
                }
            }

            return new List<float>();
        }

        /// <summary>
        /// Parse Voyage AI batch embedding response
        /// </summary>
        private static List<List<float>> ParseVoyageBatchEmbeddingResponse(string response, int expectedCount)
        {
            var results = new List<List<float>>();

            try
            {
                using (var doc = JsonDocument.Parse(response))
                {
                    if (doc.RootElement.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            if (item.TryGetProperty("embedding", out var embeddingArray) && embeddingArray.ValueKind == JsonValueKind.Array)
                            {
                                var embedding = embeddingArray.EnumerateArray()
                                    .Select(x => x.GetSingle())
                                    .ToList();
                                results.Add(embedding);
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
            }

            return Enumerable.Range(0, expectedCount)
                .Select(i => i < results.Count ? results[i] : new List<float>())
                .ToList();
        }

        /// <summary>
        /// Maps valid embeddings back to original input positions, filling empty positions with empty embeddings
        /// </summary>
        private static List<List<float>> MapEmbeddingsToOriginalInputs(List<List<float>> validEmbeddings, List<string> originalInputs, List<string> validInputs)
        {
            var result = new List<List<float>>();
            var validIndex = 0;

            for (int i = 0; i < originalInputs.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(originalInputs[i]))
                {
                    result.Add(new List<float>());
                }
                else
                {
                    if (validIndex < validEmbeddings.Count)
                    {
                        result.Add(validEmbeddings[validIndex]);
                        validIndex++;
                    }
                    else
                    {
                        result.Add(new List<float>());
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
