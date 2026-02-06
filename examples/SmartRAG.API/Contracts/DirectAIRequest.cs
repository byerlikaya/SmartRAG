
namespace SmartRAG.API.Contracts;


/// <summary>
/// Request model for direct AI text generation (without RAG)
/// </summary>
public class DirectAIRequest
{
    /// <summary>
    /// The prompt/query to send to the AI
    /// </summary>
    /// <example>Explain quantum computing in simple terms</example>
    [Required]
    [MinLength(1)]
    [MaxLength(8000)]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of tokens in the response
    /// </summary>
    /// <example>500</example>
    [Range(1, 4000)]
    [DefaultValue(500)]
    public int MaxTokens { get; set; } = 500;

    /// <summary>
    /// Temperature for response creativity (0.0 = deterministic, 1.0 = very creative)
    /// </summary>
    /// <example>0.7</example>
    [Range(0.0, 1.0)]
    [DefaultValue(0.7)]
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// System message to set AI behavior context
    /// </summary>
    /// <example>You are a helpful assistant that explains complex topics simply.</example>
    public string SystemMessage { get; set; } = string.Empty;

    /// <summary>
    /// Optional conversation history for context
    /// </summary>
    public List<SimpleConversationMessage> ConversationHistory { get; set; } = new List<SimpleConversationMessage>();
}

/// <summary>
/// Simple conversation message for AI context
/// </summary>
public class SimpleConversationMessage
{
    /// <summary>
    /// Role of the message sender
    /// </summary>
    /// <example>user</example>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Content of the message
    /// </summary>
    /// <example>Hello, how are you?</example>
    [Required]
    public string Content { get; set; } = string.Empty;
}

