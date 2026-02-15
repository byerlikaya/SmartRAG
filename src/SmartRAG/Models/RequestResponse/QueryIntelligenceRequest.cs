namespace SmartRAG.Models.RequestResponse;

/// <summary>
/// Request for processing an intelligent query with RAG. When SessionId is null, session and history are resolved automatically.
/// </summary>
public class QueryIntelligenceRequest
{
    /// <summary>
    /// Natural language query to process (supports tags: -d, -db, -i, -a, -mcp, -lang:xx).
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    public int MaxResults { get; set; } = 5;

    /// <summary>
    /// Session ID for conversation continuity; null for automatic session management.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Conversation history for the session; ignored when SessionId is null.
    /// </summary>
    public string? ConversationHistory { get; set; }
}
