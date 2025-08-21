namespace SmartRAG.Models;

/// <summary>
/// RAG (Retrieval-Augmented Generation) response with metadata
/// </summary>
public class RagResponse
{
    #region Response Data

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
    public List<SearchSource> Sources { get; set; } = [];

    #endregion

    #region Metadata

    /// <summary>
    /// Timestamp when the search was performed
    /// </summary>
    public DateTime SearchedAt { get; set; }

    /// <summary>
    /// Configuration information for this RAG response
    /// </summary>
    public RagConfiguration Configuration { get; set; } = new();

    #endregion
}

/// <summary>
/// Configuration information for RAG response
/// </summary>
public class RagConfiguration
{
    #region Provider Information

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

    #endregion
}
