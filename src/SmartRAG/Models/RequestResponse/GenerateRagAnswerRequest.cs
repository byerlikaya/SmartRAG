using SmartRAG.Entities;

namespace SmartRAG.Models.RequestResponse;


/// <summary>
/// Request DTO for generating RAG answers
/// </summary>
public class GenerateRagAnswerRequest
{
    /// <summary>
    /// The natural language query to answer
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
    /// Preferred language for the response
    /// </summary>
    public string? PreferredLanguage { get; set; }

    /// <summary>
    /// Search options for filtering
    /// </summary>
    public SearchOptions? Options { get; set; }

    /// <summary>
    /// Pre-calculated document chunks
    /// </summary>
    public List<DocumentChunk>? PreCalculatedResults { get; set; }

    /// <summary>
    /// Pre-tokenized query tokens
    /// </summary>
    public List<string>? QueryTokens { get; set; }
}

