namespace SmartRAG.Models;

/// <summary>
/// Configuration for AI providers
/// </summary>
public class AIProviderConfig
{
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string? Endpoint { get; set; }

    public string? EmbeddingModel { get; set; }

    public string? EmbeddingApiKey { get; set; }

    public int MaxTokens { get; set; } = 4096;

    public double Temperature { get; set; } = 0.7;

    public string? ApiVersion { get; set; }
}