using System.Text.Json.Serialization;

namespace SmartRAG.Dashboard.Models;

/// <summary>
/// Response for the chat configuration endpoint (active provider, model, and feature flags).
/// </summary>
public sealed class ChatConfigResponse
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("maxTokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("features")]
    public ChatFeatureFlags Features { get; set; } = new ChatFeatureFlags();

    [JsonPropertyName("mcpServers")]
    public List<ChatMcpServerInfo> McpServers { get; set; } = new List<ChatMcpServerInfo>();
}

/// <summary>
/// Feature flags exposed to the chat dashboard UI.
/// </summary>
public sealed class ChatFeatureFlags
{
    [JsonPropertyName("enableDatabaseSearch")]
    public bool EnableDatabaseSearch { get; set; }

    [JsonPropertyName("enableDocumentSearch")]
    public bool EnableDocumentSearch { get; set; }

    [JsonPropertyName("enableAudioSearch")]
    public bool EnableAudioSearch { get; set; }

    [JsonPropertyName("enableImageSearch")]
    public bool EnableImageSearch { get; set; }

    [JsonPropertyName("enableMcpSearch")]
    public bool EnableMcpSearch { get; set; }

    [JsonPropertyName("enableFileWatcher")]
    public bool EnableFileWatcher { get; set; }
}

/// <summary>
/// Lightweight MCP server info for display in the chat dashboard.
/// </summary>
public sealed class ChatMcpServerInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [JsonPropertyName("autoConnect")]
    public bool AutoConnect { get; set; }
}

/// <summary>
/// Request body for sending a chat message.
/// </summary>
public sealed class ChatMessageRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}

/// <summary>
/// A single source reference (document/chunk) used to generate the answer.
/// </summary>
public sealed class ChatSourceItem
{
    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = string.Empty;

    [JsonPropertyName("chunkIndex")]
    public int? ChunkIndex { get; set; }

    [JsonPropertyName("relevantContent")]
    public string RelevantContent { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("relevanceScore")]
    public double RelevanceScore { get; set; }
}

/// <summary>
/// Response body for a chat message (answer from the active model).
/// </summary>
public sealed class ChatMessageResponse
{
    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("sources")]
    public List<ChatSourceItem> Sources { get; set; } = new List<ChatSourceItem>();

    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; set; }
}

public sealed class ChatSessionSummaryResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; set; }
}

public sealed class ChatMessageItem
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("sources")]
    public List<ChatSourceItem>? Sources { get; set; }
}

public sealed class ChatSessionDetailResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<ChatMessageItem> Messages { get; set; } = new List<ChatMessageItem>();

    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; set; }
}
