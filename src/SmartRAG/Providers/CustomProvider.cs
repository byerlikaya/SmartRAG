using Microsoft.Extensions.Logging;
using SmartRAG.Enums;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
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

        /// <summary>
        /// Override GenerateEmbeddingsBatchAsync with OPTIMIZED approach:
        /// - Single HttpClient reuse (reduces connection overhead)
        /// - keep_alive parameter (keeps model loaded in memory)
        /// - Small concurrency (2) for stability
        /// </summary>
        public override async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config)
        {
            var textList = texts.ToList();
            if (textList.Count == 0)
                return new List<List<float>>();

            var results = new List<List<float>>(new List<float>[textList.Count]);
            var startTime = DateTime.UtcNow;
            
            var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: false, requireEndpoint: true, requireModel: false);
            if (!isValid)
            {
                ProviderLogMessages.LogCustomEmbeddingValidationError(Logger, errorMessage, null);
                return textList.Select(_ => new List<float>()).ToList();
            }

            if (string.IsNullOrEmpty(config.EmbeddingModel))
            {
                ProviderLogMessages.LogCustomEmbeddingModelMissing(Logger, null);
                return textList.Select(_ => new List<float>()).ToList();
            }

            var embeddingEndpoint = GetEmbeddingEndpoint(config);
            
            // REUSE single HttpClient for entire batch with LONG timeout
            var handler = CreateHttpClientHandler();
            using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(10) })
            {
                if (!string.IsNullOrEmpty(config.ApiKey))
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
                }
                
                // Use Ollama's NATIVE BATCH API - send large batches (200 chunks at a time for maximum speed)
                // 200 is optimal for speed: with aggressive sanitization, problematic texts are cleaned
                // Failed batches are automatically retried individually to avoid data loss
                const int BatchSize = 200;
                const int MaxConcurrentBatches = 3; // Process 3 batches in parallel for 2-3x speedup
                
                Logger.LogInformation("Processing {Count} embeddings in BATCHES of {BatchSize} using Ollama native batch API (parallel: {Concurrency})", 
                    textList.Count, BatchSize, MaxConcurrentBatches);
                
                // Sanitize all texts first - AGGRESSIVE sanitization
                var sanitizedTexts = textList.Select(t => 
                {
                    if (string.IsNullOrEmpty(t))
                        return "";
                    
                    // Remove null bytes
                    var cleaned = t.Replace("\0", "").Trim();
                    
                    // Replace multiple dots/periods with single dot (common in PDFs)
                    cleaned = Regex.Replace(cleaned, @"\.{3,}", "...");
                    
                    // Replace multiple spaces/tabs/newlines with single space
                    cleaned = Regex.Replace(cleaned, @"\s+", " ");
                    
                    // Remove any remaining control characters except newline and tab
                    cleaned = new string(cleaned.Where(c => !char.IsControl(c) || c == '\n' || c == '\t').ToArray());
                    
                    // Limit text length to prevent Ollama crashes (max 8000 characters)
                    if (cleaned.Length > 8000)
                    {
                        Logger.LogWarning("Text truncated from {OriginalLength} to 8000 characters to prevent Ollama crash", cleaned.Length);
                        cleaned = cleaned.Substring(0, 8000);
                    }
                    
                    return cleaned;
                }).ToList();
                
                // Prepare batch tasks for parallel processing
                var batchTasks = new List<Task>();
                var semaphore = new SemaphoreSlim(MaxConcurrentBatches, MaxConcurrentBatches);
                var lockObject = new object();
                
                // Process batches in parallel (controlled by semaphore)
                for (int batchStart = 0; batchStart < textList.Count; batchStart += BatchSize)
                {
                    var currentBatchStart = batchStart; // Capture for closure
                    var batchEnd = Math.Min(batchStart + BatchSize, textList.Count);
                    var batch = sanitizedTexts.Skip(batchStart).Take(BatchSize).ToList();
                    var batchIndices = Enumerable.Range(batchStart, batch.Count).ToList();
                    var batchNum = (batchStart / BatchSize) + 1;
                    
                    // Create parallel task for this batch
                    var batchTask = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            await ProcessBatchAsync(client, embeddingEndpoint, config, batch, batchIndices, batchNum, 
                                batchStart, batchEnd, textList.Count, results, lockObject);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                    
                    batchTasks.Add(batchTask);
                }
                
                // Wait for all batches to complete
                await Task.WhenAll(batchTasks);
            }

            var successCount = results.Count(r => r != null && r.Count > 0);
            var totalTime = DateTime.UtcNow - startTime;
            Logger.LogInformation("Batch embedding completed: {Success}/{Total} successful ({SuccessRate:F1}%) | Total time: {TotalTime:F1}s", 
                successCount, textList.Count, (successCount * 100.0) / textList.Count, totalTime.TotalSeconds);

            return results;
        }

        private async Task ProcessBatchAsync(HttpClient client, string embeddingEndpoint, AIProviderConfig config,
            List<string> batch, List<int> batchIndices, int batchNum, int batchStart, int batchEnd, int totalCount,
            List<List<float>> results, object lockObject)
        {
            var batchStartTime = DateTime.UtcNow;
            bool shouldLogBatch = batchNum <= 5 || batchNum % 10 == 0;
            
            try
            {
                if (shouldLogBatch)
                {
                    Logger.LogInformation("Sending BATCH {BatchNum} ({Start}-{End}/{Total}) - {Count} texts", 
                        batchNum, batchStart + 1, batchEnd, totalCount, batch.Count);
                }
                
                var payload = new Dictionary<string, object>
                {
                    ["model"] = config.EmbeddingModel,
                    ["input"] = batch,  // ARRAY of strings - Ollama native batch!
                    ["keep_alive"] = "10m"  // Keep model loaded in memory longer for faster processing!
                };
                
                // Use Task with timeout wrapper (15 seconds per batch - should be enough for 200 texts)
                var requestTask = MakeHttpRequestAsync(client, embeddingEndpoint, payload, maxRetries: 2);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15));
                var completedTask = await Task.WhenAny(requestTask, timeoutTask);
                
                var batchDuration = (DateTime.UtcNow - batchStartTime).TotalSeconds;
                
                if (completedTask == timeoutTask)
                {
                    Logger.LogWarning("BATCH {BatchNum} TIMEOUT after {Duration:F2}s - Retrying each text individually", batchNum, batchDuration);
                    await ProcessIndividualTextsOnFailure(client, embeddingEndpoint, config, batch, batchIndices, results, batchNum);
                }
                else
                {
                    var (success, response, error) = await requestTask;
                    batchDuration = (DateTime.UtcNow - batchStartTime).TotalSeconds;
                    
                    if (success && !string.IsNullOrEmpty(response))
                    {
                        try
                        {
                            var batchEmbeddings = ParseOllamaBatchEmbeddingResponse(response);
                            
                            if (batchEmbeddings.Count == batch.Count)
                            {
                                lock (lockObject)
                                {
                                    for (int i = 0; i < batch.Count; i++)
                                    {
                                        results[batchIndices[i]] = batchEmbeddings[i];
                                    }
                                }
                                
                                if (shouldLogBatch)
                                {
                                    Logger.LogInformation("BATCH {BatchNum} completed in {Duration:F2}s ({Count} embeddings)", 
                                        batchNum, batchDuration, batch.Count);
                                }
                            }
                            else
                            {
                                Logger.LogWarning("BATCH {BatchNum} returned {Returned} embeddings but expected {Expected} - Retrying individually", 
                                    batchNum, batchEmbeddings.Count, batch.Count);
                                await ProcessIndividualTextsOnFailure(client, embeddingEndpoint, config, batch, batchIndices, results, batchNum);
                            }
                        }
                        catch (Exception parseEx)
                        {
                            Logger.LogError(parseEx, "Failed to parse batch response for batch {BatchNum} - Retrying individually", batchNum);
                            await ProcessIndividualTextsOnFailure(client, embeddingEndpoint, config, batch, batchIndices, results, batchNum);
                        }
                    }
                    else
                    {
                        Logger.LogWarning("BATCH {BatchNum} failed: {Error} - Retrying each text individually", batchNum, error ?? "Unknown error");
                        await ProcessIndividualTextsOnFailure(client, embeddingEndpoint, config, batch, batchIndices, results, batchNum);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An unexpected error occurred during BATCH {BatchNum} processing", batchNum);
                await ProcessIndividualTextsOnFailure(client, embeddingEndpoint, config, batch, batchIndices, results, batchNum);
            }
        }

        private async Task ProcessIndividualTextsOnFailure(HttpClient client, string embeddingEndpoint, AIProviderConfig config,
            List<string> batch, List<int> batchIndices, List<List<float>> results, int batchNum)
        {
            for (int i = 0; i < batch.Count; i++)
            {
                try
                {
                    var text = batch[i];
                    var textLength = text?.Length ?? 0;
                    var textPreview = textLength > 100 ? text.Substring(0, 100) + "..." : text;
                    
                    var singlePayload = new Dictionary<string, object>
                    {
                        ["model"] = config.EmbeddingModel,
                        ["input"] = text,
                        ["keep_alive"] = "10m"
                    };
                    
                    var (singleSuccess, singleResponse, singleError) = await MakeHttpRequestAsync(client, embeddingEndpoint, singlePayload, maxRetries: 3);
                    
                    if (singleSuccess && !string.IsNullOrEmpty(singleResponse))
                    {
                        try
                        {
                            var singleEmbeddings = ParseOllamaBatchEmbeddingResponse(singleResponse);
                            if (singleEmbeddings.Count > 0)
                            {
                                results[batchIndices[i]] = singleEmbeddings[0];
                            }
                            else
                            {
                                results[batchIndices[i]] = new List<float>();
                            }
                        }
                        catch
                        {
                            results[batchIndices[i]] = new List<float>();
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Individual retry for text {Index} in BATCH {BatchNum} failed after 4 attempts. Text length: {Length}, Preview: {Preview}, Error: {Error}", 
                            batchIndices[i] + 1, batchNum, textLength, textPreview, singleError ?? "Unknown");
                        results[batchIndices[i]] = new List<float>();
                    }
                    
                    // Delay between individual retries to give Ollama time to recover
                    if (i < batch.Count - 1)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(1000));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An unexpected error occurred during individual retry for text {Index} in BATCH {BatchNum}", 
                        batchIndices[i] + 1, batchNum);
                    results[batchIndices[i]] = new List<float>();
                }
            }
        }

        // Override to sanitize text and use custom endpoint for Ollama
        public override async Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
        {
            // Sanitize input text to remove null bytes that might break Ollama
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Replace("\0", "").Trim();
            }

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

            var embeddingEndpoint = GetEmbeddingEndpoint(config);
            
            using (var client = CreateHttpClient(config.ApiKey))
            {
                string paramName = embeddingEndpoint.Contains("/api/embeddings") ? "prompt" : "input";

                var payload = new Dictionary<string, object>
                {
                    ["model"] = config.EmbeddingModel,
                    [paramName] = text ?? ""
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
        /// Creates HttpClient with configurable timeout for localhost Ollama
        /// </summary>
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

            try
            {
                // Parse URL to get base URL
                var uri = new Uri(endpoint);
                var baseUrl = $"{uri.Scheme}://{uri.Authority}";

                // Ollama pattern: http://localhost:11434/v1/chat/completions → http://localhost:11434/v1/embeddings
                if (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "::1")
                {
                    // Use OpenAI-compatible endpoint for better stability with "input" parameter
                    return $"{baseUrl}/v1/embeddings";
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
                    var v1Index = endpoint.IndexOf("/v1/");
                    var baseUrlFromEndpoint = endpoint.Substring(0, v1Index + 4);
                    return $"{baseUrlFromEndpoint}embeddings";
                }

                // Fallback: append /api/embeddings to base URL
                return $"{baseUrl}/api/embeddings";
            }
            catch
            {
                // If URL parsing fails, try simple string replacement
                if (endpoint.Contains("/chat/completions"))
                {
                    return endpoint.Replace("/chat/completions", "/embeddings");
                }

                // Last resort: append /api/embeddings
                if (!endpoint.EndsWith("/"))
                {
                    return endpoint + "/api/embeddings";
                }
                return endpoint + "api/embeddings";
            }
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
        private static List<List<float>> ParseOllamaBatchEmbeddingResponse(string response)
        {
            try
            {
                using var document = JsonDocument.Parse(response);
                var root = document.RootElement;

                // Ollama native /api/embed response: { "embeddings": [[...], [...]] }
                if (root.TryGetProperty("embeddings", out var embeddingsArray))
                {
                    var result = new List<List<float>>();
                    foreach (var embedding in embeddingsArray.EnumerateArray())
                    {
                        var vector = new List<float>();
                        foreach (var value in embedding.EnumerateArray())
                        {
                            vector.Add((float)value.GetDouble());
                        }
                        result.Add(vector);
                    }
                    return result;
                }

                return new List<List<float>>();
            }
            catch
            {
                return new List<List<float>>();
            }
        }

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
