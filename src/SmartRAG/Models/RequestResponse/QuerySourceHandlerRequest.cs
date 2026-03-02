using SmartRAG.Entities;
using SmartRAG.Models.Schema;

namespace SmartRAG.Models.RequestResponse;


/// <summary>
/// Request DTO for query source handlers (database, document, MCP).
/// Aligns with orchestrator needs and can be mapped to QueryStrategyRequest or GenerateRagAnswerRequest.
/// </summary>
public class QuerySourceHandlerRequest
{
    /// <summary>
    /// The natural language query.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    public int MaxResults { get; set; }

    /// <summary>
    /// Conversation history for context.
    /// </summary>
    public string? ConversationHistory { get; set; }

    /// <summary>
    /// Session identifier for conversation storage (optional; used by orchestrator).
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Search options that determined which handler(s) to invoke.
    /// </summary>
    public SearchOptions Options { get; set; } = new();

    /// <summary>
    /// Pre-tokenized query tokens.
    /// </summary>
    public List<string>? QueryTokens { get; set; }

    /// <summary>
    /// Pre-calculated document chunks (for document path when CanAnswerFromDocuments was already run).
    /// </summary>
    public List<DocumentChunk>? PreCalculatedResults { get; set; }

    /// <summary>
    /// Query intent analysis result (for database strategy).
    /// </summary>
    public QueryIntent? QueryIntent { get; set; }

    /// <summary>
    /// Whether database queries are available (for hybrid strategy).
    /// </summary>
    public bool? HasDatabaseQueries { get; set; }

    /// <summary>
    /// Whether documents can answer the query.
    /// </summary>
    public bool? CanAnswerFromDocuments { get; set; }

    /// <summary>
    /// Search metadata to update (e.g. MCP sets McpSearchPerformed).
    /// </summary>
    public SearchMetadata? SearchMetadata { get; set; }

    /// <summary>
    /// Existing response to merge with (e.g. MCP merges into document/DB response).
    /// </summary>
    public RagResponse? ExistingResponse { get; set; }

    /// <summary>
    /// Preferred language for the response.
    /// </summary>
    public string? PreferredLanguage { get; set; }
}
