namespace SmartRAG.Models.RequestResponse;



/// <summary>
/// RAG (Retrieval-Augmented Generation) response with metadata
/// </summary>
public class RagResponse
{
    /// <summary>
    /// Original query that was processed
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Generated answer based on retrieved documents
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Sources that were used to generate the answer
    /// </summary>
    public List<SearchSource> Sources { get; set; } = new();

    /// <summary>
    /// Timestamp when the search was performed
    /// </summary>
    public DateTime SearchedAt { get; set; }

    /// <summary>
    /// Configuration information for this RAG response
    /// </summary>
    public RagConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Information about which search operations were performed for this query
    /// </summary>
    public SearchMetadata SearchMetadata { get; set; } = new();
}

/// <summary>
/// Configuration information for RAG response
/// </summary>
public class RagConfiguration
{
    /// <summary>
    /// AI provider used for generating the response
    /// </summary>
    public string AIProvider { get; set; } = string.Empty;

    /// <summary>
    /// Storage provider used for document retrieval
    /// </summary>
    public string StorageProvider { get; set; } = string.Empty;

    /// <summary>
    /// Model name used for text generation
    /// </summary>
    public string Model { get; set; } = string.Empty;
}

/// <summary>
/// Metadata about search operations performed for a query
/// </summary>
public class SearchMetadata
{
    /// <summary>
    /// Whether document search was performed
    /// </summary>
    public bool DocumentSearchPerformed { get; set; }

    /// <summary>
    /// Whether database search was performed
    /// </summary>
    public bool DatabaseSearchPerformed { get; set; }

    /// <summary>
    /// Whether MCP (Model Context Protocol) search was performed
    /// </summary>
    public bool McpSearchPerformed { get; set; }

    /// <summary>
    /// Number of document chunks found (if document search was performed)
    /// </summary>
    public int DocumentChunksFound { get; set; }

    /// <summary>
    /// Number of database results found (if database search was performed)
    /// </summary>
    public int DatabaseResultsFound { get; set; }

    /// <summary>
    /// Number of MCP results found (if MCP search was performed)
    /// </summary>
    public int McpResultsFound { get; set; }
}

