
namespace SmartRAG.API.Contracts;


/// <summary>
/// Request model for switching AI provider
/// </summary>
public class AIProviderSwitchRequest
{
    /// <summary>
    /// Target AI provider to switch to
    /// </summary>
    /// <example>OpenAI</example>
    [Required]
    [DefaultValue(AIProvider.OpenAI)]
    public AIProvider Provider { get; set; } = AIProvider.OpenAI;

    /// <summary>
    /// API key for the target provider (optional if already configured)
    /// </summary>
    /// <example>sk-1234567890abcdef...</example>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model name to use with the provider (optional, uses default if not specified)
    /// </summary>
    /// <example>gpt-5.1</example>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for custom providers (optional)
    /// </summary>
    /// <example>https://api.openai.com/v1</example>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether to validate the provider configuration before switching
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ValidateBeforeSwitch { get; set; } = true;
}

