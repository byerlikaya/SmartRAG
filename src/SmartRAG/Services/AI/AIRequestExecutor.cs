
namespace SmartRAG.Services.AI;

public class AIRequestExecutor : IAIRequestExecutor
{
    private readonly IAIProviderFactory _aiProviderFactory;
    private readonly IAIConfigurationService _configService;
    private readonly ILogger<AIRequestExecutor> _logger;

    private const string ContextPrefix = "Context:\n";
    private const string QuestionPrefix = "\n\nQuestion: ";
    private const string AnswerPrefix = "\n\nAnswer:";

    public AIRequestExecutor(
        IAIProviderFactory aiProviderFactory,
        IAIConfigurationService configService,
        ILogger<AIRequestExecutor> logger)
    {
        _aiProviderFactory = aiProviderFactory;
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// [AI Query] Generates a response using the specified AI provider
    /// </summary>
    public async Task<string> GenerateResponseAsync(AIProvider provider, string query, IEnumerable<string> context, CancellationToken cancellationToken = default)
    {
        var providerConfig = _configService.GetProviderConfig(provider) ?? throw new InvalidOperationException($"AI provider configuration not found for '{provider}'");
        var aiProvider = _aiProviderFactory.CreateProvider(provider);
        var prompt = BuildPrompt(query, context);

        var response = await aiProvider.GenerateTextAsync(prompt, providerConfig, cancellationToken);

        if (IsErrorResponse(response))
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(response)
                ? "Empty response received from AI provider."
                : response);
        }

        return response ?? string.Empty;
    }

    /// <summary>
    /// [AI Query] Generates embeddings for a single text
    /// </summary>
    public async Task<List<float>> GenerateEmbeddingsAsync(AIProvider provider, string text, CancellationToken cancellationToken = default)
    {
        var providerConfig = _configService.GetProviderConfig(provider);
        if (providerConfig == null)
        {
            ServiceLogMessages.LogAIServiceProviderConfigNotFound(_logger, provider.ToString(), null);
            return new List<float>();
        }

        var aiProvider = _aiProviderFactory.CreateProvider(provider);
        return await aiProvider.GenerateEmbeddingAsync(text, providerConfig, cancellationToken);
    }

    /// <summary>
    /// [AI Query] Generates embeddings for a batch of texts
    /// </summary>
    public async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(AIProvider provider, IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var providerConfig = _configService.GetProviderConfig(provider);
        if (providerConfig == null)
        {
            ServiceLogMessages.LogAIServiceProviderConfigNotFound(_logger, provider.ToString(), null);
            return new List<List<float>>();
        }

        var aiProvider = _aiProviderFactory.CreateProvider(provider);
        var embeddings = await aiProvider.GenerateEmbeddingsBatchAsync(texts, providerConfig, cancellationToken);

        var filteredEmbeddings = embeddings?.Where(e => e != null && e.Count > 0).ToList() ?? new List<List<float>>();

        return filteredEmbeddings;
    }

    private static string BuildPrompt(string query, IEnumerable<string> context)
    {
        var contextText = string.Join("\n\n", context);
        return $"{ContextPrefix}{contextText}{QuestionPrefix}{query}{AnswerPrefix}";
    }

    private static bool IsErrorResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return true;

        var trimmed = response.TrimStart();
        if (trimmed.StartsWith("error", StringComparison.OrdinalIgnoreCase)) return true;

        if (trimmed.StartsWith("gemini error", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("anthropic error", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("openai error", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("azureopenai error", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("custom error", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (trimmed.StartsWith("{\"error\"", StringComparison.OrdinalIgnoreCase)) return true;

        if (trimmed.Contains(" error:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains(" error -", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (trimmed.Contains("ServiceUnavailable", StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }
}
