
namespace SmartRAG.Providers;


/// <summary>
/// Base class for AI providers with common implementations
/// </summary>
public abstract class BaseAIProvider : IAIProvider
{
    private const int DefaultMaxRetries = 3;
    private const int BaseDelayMs = 1000;
    private const int MinRetryDelayMs = 60000;

    private const string DefaultDataProperty = "data";
    private const string DefaultEmbeddingProperty = "embedding";
    private const string DefaultChoicesProperty = "choices";
    private const string DefaultMessageProperty = "message";
    private const string DefaultContentProperty = "content";

    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Logger instance for derived classes
    /// </summary>
    protected ILogger Logger => _logger;

    protected BaseAIProvider(ILogger logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Gets the type of AI provider this implementation represents
    /// </summary>
    public abstract AIProvider ProviderType { get; }

    /// <summary>
    /// Generates text response from the AI provider
    /// </summary>
    /// <param name="prompt">The input prompt for text generation</param>
    /// <param name="config">AI provider configuration settings</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Generated text response</returns>
    public abstract Task<string> GenerateTextAsync(string prompt, AIProviderConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates vector embedding for the given text
    /// </summary>
    /// <param name="text">The text to generate embedding for</param>
    /// <param name="config">AI provider configuration settings</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Vector embedding as list of floats</returns>
    public abstract Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Default batch embedding implementation with parallel processing
    /// Providers can override this for better performance if they support true batch operations
    /// </summary>
    public virtual async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config, CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        var results = new List<List<float>>(new List<float>[textList.Count]);

        if (textList.Count == 0)
            return results;

        Logger.LogInformation("Starting batch embedding generation: {Total} texts", textList.Count);

        using (var semaphore = new System.Threading.SemaphoreSlim(3))
        {
            var tasks = textList.Select(async (text, index) =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var embedding = await GenerateEmbeddingAsync(text, config, cancellationToken);
                    results[index] = embedding;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to generate embedding for text at index {Index}", index);
                    results[index] = new List<float>();
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        var successCount = results.Count(r => r != null && r.Count > 0);
        Logger.LogInformation("Batch embedding generation completed: {Success}/{Total} successful",
            successCount, textList.Count);

        return results;
    }

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
    /// Creates HttpClient with common headers using IHttpClientFactory
    /// </summary>
    protected HttpClient CreateHttpClient(string apiKey = null, Dictionary<string, string> additionalHeaders = null)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(180);

        if (!string.IsNullOrEmpty(apiKey))
        {
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
    protected HttpClient CreateHttpClientWithoutAuth(Dictionary<string, string> additionalHeaders)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(180);

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
        HttpClient client, string endpoint, object payload, int maxRetries = DefaultMaxRetries, CancellationToken cancellationToken = default)
    {
        var attempt = 0;

        while (attempt < maxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var (success, response, error) = await ExecuteHttpRequestAsync(client, endpoint, payload, cancellationToken);

                if (success)
                    return (true, response, string.Empty);

                bool shouldRetry = error.Contains("429") || error.Contains("TooManyRequests") ||
                    error.Contains("529") || error.Contains("Overloaded") ||
                    error.Contains("EOF") ||  // Retry EOF errors (Ollama runner crashes)
                    error.Contains("llama runner process no longer running") ||  // Ollama crash
                    error.Contains("InternalServerError");  // General server errors

                if (shouldRetry)
                {
                    attempt++;
                    if (attempt < maxRetries)
                    {
                        int delayMs = error.Contains("EOF") ? 1000 : MinRetryDelayMs * attempt;
                        await Task.Delay(delayMs, cancellationToken);
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
                    var delay = BaseDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
                return (false, string.Empty, $"{ProviderType} request failed: {ex.Message}");
            }
        }

        return (false, string.Empty, $"{ProviderType} request failed after {maxRetries} attempts");
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
        HttpClient client, string endpoint, object payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(endpoint, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            return (true, responseBody, string.Empty);
        }

        var errorBody = await response.Content.ReadAsStringAsync();

        return (false, string.Empty, $"{ProviderType} error: {response.StatusCode} - {errorBody}");
    }

    /// <summary>
    /// Common embedding response parsing
    /// </summary>
    protected static List<float> ParseEmbeddingResponse(string responseBody, string dataProperty = DefaultDataProperty, string embeddingProperty = DefaultEmbeddingProperty)
    {
        using var doc = JsonDocument.Parse(responseBody);
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

    /// <summary>
    /// Common batch embedding response parsing for OpenAI-like APIs
    /// </summary>
    protected static List<List<float>> ParseBatchEmbeddingResponse(string responseBody, int expectedCount, string dataProperty = DefaultDataProperty, string embeddingProperty = DefaultEmbeddingProperty)
    {
        var results = new List<List<float>>();

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
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
        catch
        {
        }

        return Enumerable.Range(0, expectedCount)
            .Select(i => i < results.Count ? results[i] : new List<float>())
            .ToList();
    }

    /// <summary>
    /// Common text generation response parsing for OpenAI-like APIs
    /// </summary>
    protected static string ParseTextResponse(string responseBody, string choicesProperty = DefaultChoicesProperty, string messageProperty = DefaultMessageProperty, string contentProperty = DefaultContentProperty)
    {
        using var doc = JsonDocument.Parse(responseBody);
        if (doc.RootElement.TryGetProperty(choicesProperty, out var choices) && choices.ValueKind == JsonValueKind.Array)
        {
            var firstChoice = choices.EnumerateArray().FirstOrDefault();

            if (firstChoice.TryGetProperty(messageProperty, out var message) && message.TryGetProperty(contentProperty, out var contentProp))
                return contentProp.GetString() ?? "No response generated";
        }

        return "No response generated";
    }

    /// <summary>
    /// Shared JsonSerializerOptions for consistent serialization
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}

