namespace SmartRAG.Models;

/// <summary>
/// RAG (Retrieval-Augmented Generation) response with metadata
/// </summary>
public class RagResponse
{
    public string Query { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<SearchSource> Sources { get; set; } = [];
    public DateTime SearchedAt { get; set; }
    public RagConfiguration Configuration { get; set; } = new();
}

/// <summary>
/// Configuration information for RAG response
/// </summary>
public class RagConfiguration
{
    public string AIProvider { get; set; } = string.Empty;
    public string StorageProvider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}
