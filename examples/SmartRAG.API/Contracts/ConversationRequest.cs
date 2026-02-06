using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SmartRAG.API.Contracts;


/// <summary>
/// Request model for creating a new conversation session
/// </summary>
public class CreateConversationRequest
{
    /// <summary>
    /// Optional title for the conversation
    /// </summary>
    /// <example>AI Discussion about Machine Learning</example>
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// User identifier for the conversation
    /// </summary>
    /// <example>user123</example>
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Optional metadata for the conversation
    /// </summary>
    /// <example>{"source": "web", "language": "en"}</example>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Initial system message or context
    /// </summary>
    /// <example>You are a helpful AI assistant specializing in technical topics.</example>
    [MaxLength(2000)]
    public string SystemMessage { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of messages to keep in history
    /// </summary>
    /// <example>50</example>
    [Range(1, 1000)]
    [DefaultValue(50)]
    public int MaxHistoryLength { get; set; } = 50;

    /// <summary>
    /// Whether to enable conversation analytics
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool EnableAnalytics { get; set; } = true;
}

/// <summary>
/// Request model for adding a message to a conversation
/// </summary>
public class AddMessageRequest
{
    /// <summary>
    /// The message content
    /// </summary>
    /// <example>What is machine learning?</example>
    [Required]
    [MinLength(1)]
    [MaxLength(8000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Role of the message sender (user, assistant, system)
    /// </summary>
    /// <example>user</example>
    [Required]
    public string Role { get; set; } = "user";

    /// <summary>
    /// Optional message metadata
    /// </summary>
    /// <example>{"source": "web", "confidence": "high"}</example>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Whether to generate an AI response to this message
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool GenerateResponse { get; set; } = true;

    /// <summary>
    /// Whether to include document context in the response
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool UseRAG { get; set; } = true;

    /// <summary>
    /// Maximum number of tokens for the AI response
    /// </summary>
    /// <example>500</example>
    [Range(1, 4000)]
    [DefaultValue(500)]
    public int MaxTokens { get; set; } = 500;

    /// <summary>
    /// Temperature for AI response creativity (0.0 = deterministic, 1.0 = very creative)
    /// </summary>
    /// <example>0.7</example>
    [Range(0.0, 1.0)]
    [DefaultValue(0.7)]
    public double Temperature { get; set; } = 0.7;
}

/// <summary>
/// Request model for conversation search and filtering
/// </summary>
public class ConversationSearchRequest
{
    /// <summary>
    /// User ID to filter conversations
    /// </summary>
    /// <example>user123</example>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Search term to find in conversation titles or messages
    /// </summary>
    /// <example>machine learning</example>
    [MaxLength(200)]
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Start date for conversation filtering
    /// </summary>
    /// <example>2024-01-01T00:00:00Z</example>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for conversation filtering
    /// </summary>
    /// <example>2024-01-31T23:59:59Z</example>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Maximum number of conversations to return
    /// </summary>
    /// <example>20</example>
    [Range(1, 100)]
    [DefaultValue(20)]
    public int Limit { get; set; } = 20;

    /// <summary>
    /// Number of conversations to skip (for pagination)
    /// </summary>
    /// <example>0</example>
    [Range(0, int.MaxValue)]
    [DefaultValue(0)]
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Sort order: created_asc, created_desc, updated_asc, updated_desc, title_asc, title_desc
    /// </summary>
    /// <example>created_desc</example>
    [DefaultValue("created_desc")]
    public string SortBy { get; set; } = "created_desc";

    /// <summary>
    /// Whether to include message content in results
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool IncludeMessages { get; set; } = false;
}

/// <summary>
/// Request model for conversation export
/// </summary>
public class ConversationExportRequest
{
    /// <summary>
    /// Conversation ID to export
    /// </summary>
    /// <example>123e4567-e89b-12d3-a456-426614174000</example>
    [Required]
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Export format (json, txt, pdf, html)
    /// </summary>
    /// <example>json</example>
    [Required]
    [DefaultValue("json")]
    public string Format { get; set; } = "json";

    /// <summary>
    /// Whether to include metadata in export
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Whether to include timestamps
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool IncludeTimestamps { get; set; } = true;

    /// <summary>
    /// Whether to include analytics data
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool IncludeAnalytics { get; set; } = false;

    /// <summary>
    /// Date range for message filtering (optional)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Date range for message filtering (optional)
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Request model for updating conversation settings
/// </summary>
public class UpdateConversationRequest
{
    /// <summary>
    /// New title for the conversation
    /// </summary>
    /// <example>Updated AI Discussion</example>
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Updated metadata
    /// </summary>
    /// <example>{"updated": "2024-01-15", "category": "technical"}</example>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Whether the conversation is archived
    /// </summary>
    /// <example>false</example>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// Whether analytics are enabled
    /// </summary>
    /// <example>true</example>
    public bool EnableAnalytics { get; set; } = true;

    /// <summary>
    /// Maximum history length
    /// </summary>
    /// <example>100</example>
    [Range(1, 1000)]
    public int MaxHistoryLength { get; set; } = 50;
}

