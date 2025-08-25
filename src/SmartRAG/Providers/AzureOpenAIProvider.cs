namespace SmartRAG.Providers;

/// <summary>
/// Azure OpenAI provider implementation
/// </summary>
public class AzureOpenAIProvider(ILogger<AzureOpenAIProvider> logger) : BaseAIProvider(logger), IDisposable
{
    #region Constants

    // Rate limiting constants
    private const int DefaultMaxRetries = 3;
    private const int DefaultMinIntervalMs = 60000; // 60 seconds

    #endregion

    #region Fields

    // Azure OpenAI S0 tier rate limiting (3 RPM)
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

        var url = BuildAzureUrl(config.Endpoint!, config.Model!, "chat/completions", config.ApiVersion!);

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
            return [];
        }

        if (string.IsNullOrEmpty(config.EmbeddingModel))
        {
            ProviderLogMessages.LogAzureOpenAIEmbeddingModelMissing(Logger, null);
            return [];
        }

        using var client = CreateHttpClient(config.ApiKey);

        var payload = new
        {
            input = text
        };

        var url = BuildAzureUrl(config.Endpoint!, config.EmbeddingModel!, "embeddings", config.ApiVersion!);

        var (success, response, error) = await MakeHttpRequestAsyncWithRateLimit(client, url, payload, config);

        if (!success)
        {
            ProviderLogMessages.LogAzureOpenAIEmbeddingRequestError(Logger, error, null);
            return [];
        }

        try
        {
            return ParseEmbeddingResponse(response);
        }
        catch (Exception ex)
        {
            ProviderLogMessages.LogAzureOpenAIEmbeddingParsingError(Logger, ex);
            return [];
        }
    }

    public override async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config)
    {
        var (isValid, errorMessage) = ValidateConfig(config, requireApiKey: true, requireEndpoint: true, requireModel: false);

        if (!isValid)
        {
            ProviderLogMessages.LogAzureOpenAIEmbeddingValidationError(Logger, errorMessage, null);
            return [];
        }

        if (string.IsNullOrEmpty(config.EmbeddingModel))
        {
            ProviderLogMessages.LogAzureOpenAIEmbeddingModelMissing(Logger, null);
            return [];
        }

        var inputList = texts?.ToList() ?? new List<string>();
        if (inputList.Count == 0)
            return [];

        using var client = CreateHttpClient(config.ApiKey);

        var payload = new
        {
            input = inputList.ToArray()
        };

        var url = BuildAzureUrl(config.Endpoint!, config.EmbeddingModel!, "embeddings", config.ApiVersion!);

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
        // S0 tier: 3 RPM - configurable minimum interval
        var minIntervalMs = Math.Max(0, config.EmbeddingMinIntervalMs ?? DefaultMinIntervalMs);

        await _rateLimitSemaphore.WaitAsync();
        try
        {
            await WaitForRateLimit(minIntervalMs);
            _lastRequestTime = DateTime.UtcNow;

            // Normal request with retry logic
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