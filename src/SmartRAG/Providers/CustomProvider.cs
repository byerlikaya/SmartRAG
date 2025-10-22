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
    /// Custom AI provider implementation for custom endpoints
    /// </summary>
    public class CustomProvider : BaseAIProvider
    {
        private readonly ILogger<CustomProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the CustomProvider
        /// </summary>
        /// <param name="logger">Logger instance for this provider</param>
        public CustomProvider(ILogger<CustomProvider> logger) : base(logger)
        {
            _logger = logger;
        }

        #region Constants

        // Custom provider constants
        private const int DefaultMaxChunkSize = 1000;
        private const string UserRole = "user";
        private const string SystemRole = "system";

        #endregion

        #region Properties

        public override AIProvider ProviderType => AIProvider.Custom;

        #endregion

        #region Public Methods

        public override async Task<string> GenerateTextAsync(string prompt, AIProviderConfig config)
        {
            var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: false, requireEndpoint: true, requireModel: true);

            if (!isValid)
                return errorMessage;

            using (var client = CreateHttpClient(config.ApiKey))
            {
                bool useMessagesFormat = IsMessagesFormat(config.Endpoint);

                object payload = CreatePayload(prompt, config, useMessagesFormat);

                var (success, response, error) = await MakeHttpRequestAsync(client, config.Endpoint, payload);

                if (!success)
                    return error;

                try
                {
                    return ParseCustomTextResponse(response);
                }
                catch (Exception ex)
                {
                    ProviderLogMessages.LogCustomTextParsingError(Logger, ex);
                    return $"Error parsing custom response: {ex.Message}";
                }
            }
        }

        public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
        {
            var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: false, requireEndpoint: true, requireModel: false);

            if (!isValid)
            {
                ProviderLogMessages.LogCustomEmbeddingValidationError(Logger, errorMessage, null);
                return new List<float>();
            }

            if (string.IsNullOrEmpty(config.EmbeddingModel))
            {
                ProviderLogMessages.LogCustomEmbeddingModelMissing(Logger, null);
                return new List<float>();
            }

            using (var client = CreateHttpClient(config.ApiKey))
            {
                // Determine embedding endpoint
                var embeddingEndpoint = GetEmbeddingEndpoint(config);

                Logger.LogDebug("Ollama embedding request: Endpoint={Endpoint}, Model={Model}, TextLength={Length}",
                    embeddingEndpoint, config.EmbeddingModel, text?.Length ?? 0);

                // Ollama-style payload (uses "prompt" not "input")
                var payload = new
                {
                    model = config.EmbeddingModel,
                    prompt = text
                };

                Logger.LogDebug("Ollama embedding payload: {Payload}",
                    JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false }));

                var (success, response, error) = await MakeHttpRequestAsync(client, embeddingEndpoint, payload);

                Logger.LogDebug("Ollama embedding response: Success={Success}, ResponseLength={Length}, Error={Error}",
                    success, response?.Length ?? 0, error ?? "None");

                if (success && !string.IsNullOrEmpty(response))
                {
                    Logger.LogDebug("Ollama embedding response content: {Response}", response);
                }

                if (!success)
                {
                    ProviderLogMessages.LogCustomEmbeddingRequestError(Logger, error, null);
                    return new List<float>();
                }

                try
                {
                    return ParseCustomEmbeddingResponse(response);
                }
                catch (Exception ex)
                {
                    ProviderLogMessages.LogCustomEmbeddingParsingError(Logger, ex);
                    return new List<float>();
                }
            }
        }

        /// <summary>
        /// Override ChunkTextAsync for custom endpoint optimization
        /// Uses string concatenation for better compatibility with various APIs
        /// </summary>
        public override Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = DefaultMaxChunkSize)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Task.FromResult(new List<string>());

            var chunks = new List<string>();
            var sentences = text.Split(new char[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var currentChunk = "";

            foreach (var sentence in sentences)
            {
                var trimmedSentence = sentence.Trim();

                if (string.IsNullOrEmpty(trimmedSentence))
                    continue;

                if (currentChunk.Length + trimmedSentence.Length > maxChunkSize && !string.IsNullOrEmpty(currentChunk))
                {
                    chunks.Add(currentChunk.Trim());
                    currentChunk = trimmedSentence + ".";
                }
                else
                {
                    currentChunk += (currentChunk.Length > 0 ? " " : "") + trimmedSentence + ".";
                }
            }

            if (!string.IsNullOrEmpty(currentChunk))
            {
                chunks.Add(currentChunk.Trim());
            }

            return Task.FromResult(chunks);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determine if endpoint uses messages format
        /// </summary>
        private static bool IsMessagesFormat(string endpoint)
        {
            return endpoint.Contains("/chat") || endpoint.Contains("messages") || endpoint.Contains("completions");
        }

        /// <summary>
        /// Gets the embedding endpoint, either from config or derived from main endpoint
        /// </summary>
        private static string GetEmbeddingEndpoint(AIProviderConfig config)
        {
            // If explicit embedding endpoint provided, use it
            if (!string.IsNullOrEmpty(config.EmbeddingEndpoint))
            {
                return config.EmbeddingEndpoint;
            }

            // Derive from main endpoint
            var endpoint = config.Endpoint;

            // Ollama pattern: /v1/chat/completions → /api/embeddings or /v1/embeddings
            if (endpoint.Contains("localhost") || endpoint.Contains("127.0.0.1"))
            {
                // Likely Ollama - use /api/embeddings
                var baseUrl = endpoint.Substring(0, endpoint.IndexOf("/v1"));
                return baseUrl + "/api/embeddings";
            }

            // OpenRouter, Groq, etc: usually same endpoint pattern
            // /v1/chat/completions → /v1/embeddings
            if (endpoint.Contains("/chat/completions"))
            {
                return endpoint.Replace("/chat/completions", "/embeddings");
            }

            // Default: try /v1/embeddings
            if (endpoint.Contains("/v1/"))
            {
                var baseUrl = endpoint.Substring(0, endpoint.IndexOf("/v1/") + 4);
                return baseUrl + "embeddings";
            }

            // Fallback: use main endpoint
            return endpoint;
        }

        /// <summary>
        /// Create appropriate payload based on format
        /// </summary>
        private static object CreatePayload(string prompt, AIProviderConfig config, bool useMessagesFormat)
        {
            if (useMessagesFormat)
            {
                var messages = new List<object>();

                if (!string.IsNullOrEmpty(config.SystemMessage))
                {
                    messages.Add(new { role = SystemRole, content = config.SystemMessage });
                }

                messages.Add(new { role = UserRole, content = prompt });

                return new
                {
                    model = config.Model,
                    messages = messages.ToArray(),
                    max_tokens = config.MaxTokens,
                    temperature = config.Temperature
                };
            }
            else
            {
                return new
                {
                    prompt = prompt,
                    max_tokens = config.MaxTokens,
                    temperature = config.Temperature,
                    model = config.Model
                };
            }
        }

        /// <summary>
        /// Parse custom AI text response with flexible format support
        /// </summary>
        private static string ParseCustomTextResponse(string response)
        {
            var responseData = JsonSerializer.Deserialize<JsonElement>(response);

            // Try OpenAI-style response format first (for OpenRouter, etc.)
            if (responseData.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
            {
                var firstChoice = choices.EnumerateArray().FirstOrDefault();
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? "No response generated";
                }
            }

            // Try message.content[].text format (some APIs use array-based content)
            if (responseData.TryGetProperty("message", out var messageWithArrayContent) &&
                messageWithArrayContent.TryGetProperty("content", out var contentArray) &&
                contentArray.ValueKind == JsonValueKind.Array)
            {
                var firstContentItem = contentArray.EnumerateArray().FirstOrDefault();
                if (firstContentItem.TryGetProperty("text", out var textContent))
                {
                    return textContent.GetString() ?? "No response generated";
                }
            }

            // Try other common response field names
            if (responseData.TryGetProperty("text", out var text))
                return text.GetString() ?? "No response generated";
            if (responseData.TryGetProperty("response", out var responseText))
                return responseText.GetString() ?? "No response generated";
            if (responseData.TryGetProperty("content", out var contentProp))
                return contentProp.GetString() ?? "No response generated";
            if (responseData.TryGetProperty("message", out var messageProp))
                return messageProp.GetString() ?? "No response generated";

            return "No response generated";
        }

        /// <summary>
        /// Parse custom AI embedding response with flexible format support
        /// </summary>
        private static List<float> ParseCustomEmbeddingResponse(string response)
        {
            var responseData = JsonSerializer.Deserialize<JsonElement>(response);

            // Try Ollama format: { "embedding": [0.1, 0.2, ...] }
            if (responseData.TryGetProperty("embedding", out var embedding) && embedding.ValueKind == JsonValueKind.Array)
            {
                var floats = new List<float>();
                foreach (var value in embedding.EnumerateArray())
                {
                    if (value.TryGetSingle(out var f))
                        floats.Add(f);
                    else if (value.TryGetDouble(out var d))
                        floats.Add((float)d);
                }
                return floats;
            }

            // Try OpenAI/Ollama format: { "embeddings": [[0.1, 0.2, ...]] }
            if (responseData.TryGetProperty("embeddings", out var embeddings) && embeddings.ValueKind == JsonValueKind.Array)
            {
                var firstEmbedding = embeddings.EnumerateArray().FirstOrDefault();

                if (firstEmbedding.ValueKind == JsonValueKind.Array)
                {
                    var floats = new List<float>();
                    foreach (var value in firstEmbedding.EnumerateArray())
                    {
                        if (value.TryGetSingle(out var f))
                            floats.Add(f);
                        else if (value.TryGetDouble(out var d))
                            floats.Add((float)d);
                    }
                    return floats;
                }
            }

            // Try OpenAI format: { "data": [{ "embedding": [0.1, 0.2, ...] }] }
            if (responseData.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                var firstData = data.EnumerateArray().FirstOrDefault();
                if (firstData.TryGetProperty("embedding", out var dataEmbedding) && dataEmbedding.ValueKind == JsonValueKind.Array)
                {
                    var floats = new List<float>();
                    foreach (var value in dataEmbedding.EnumerateArray())
                    {
                        if (value.TryGetSingle(out var f))
                            floats.Add(f);
                        else if (value.TryGetDouble(out var d))
                            floats.Add((float)d);
                    }
                    return floats;
                }
            }

            return new List<float>();
        }

        #endregion
    }
}
