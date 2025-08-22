namespace SmartRAG.Models;

/// <summary>
/// Configuration for AI providers
/// </summary>
public class AIProviderConfig
{
    #region Authentication Properties

    /// <summary>
    /// API key for the AI provider
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional separate API key for embedding operations
    /// </summary>
    public string? EmbeddingApiKey { get; set; }

    #endregion

    #region Endpoint Configuration

    /// <summary>
    /// Optional custom endpoint URL for the AI provider
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// API version identifier for versioned APIs
    /// </summary>
    public string? ApiVersion { get; set; }

    #endregion

    #region Model Configuration

    /// <summary>
    /// Model name to use for text generation
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Optional separate model for embedding generation
    /// </summary>
    public string? EmbeddingModel { get; set; }

    #endregion

    #region Generation Parameters

    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Temperature parameter for controlling randomness (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Optional system message for chat completions.
    /// If null, provider defaults will be used.
    /// </summary>
    public string? SystemMessage { get; set; }

    #endregion

    #region Rate Limiting

    /// <summary>
    /// Optional minimum interval between embedding requests in milliseconds.
    /// If null, provider defaults will be used (e.g., 60000 ms for Azure S0).
    /// </summary>
    public int? EmbeddingMinIntervalMs { get; set; }

    #endregion
}