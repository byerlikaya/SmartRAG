
namespace SmartRAG.Models.RequestResponse;


/// <summary>
/// Unified request DTO for all query strategy executions (database-only, document-only, hybrid)
/// </summary>
public class QueryStrategyRequest
{
    /// <summary>
    /// The natural language query to execute
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int MaxResults { get; set; }

    /// <summary>
    /// Conversation history for context
    /// </summary>
    public string ConversationHistory { get; set; } = string.Empty;

    /// <summary>
    /// Whether documents can be used to answer the query
    /// </summary>
    public bool? CanAnswerFromDocuments { get; set; }

    /// <summary>
    /// Whether database queries are available (used for hybrid strategy)
    /// </summary>
    public bool? HasDatabaseQueries { get; set; }

    /// <summary>
    /// Query intent analysis result (used for database and hybrid strategies)
    /// </summary>
    public QueryIntent? QueryIntent { get; set; }

    /// <summary>
    /// Preferred language for the response
    /// </summary>
    public string? PreferredLanguage { get; set; }

    /// <summary>
    /// Search options for filtering
    /// </summary>
    public SearchOptions? Options { get; set; }

    /// <summary>
    /// Pre-calculated document chunks (used for document and hybrid strategies)
    /// </summary>
    public List<DocumentChunk>? PreCalculatedResults { get; set; }

    /// <summary>
    /// Pre-tokenized query tokens
    /// </summary>
    public List<string>? QueryTokens { get; set; }
}


