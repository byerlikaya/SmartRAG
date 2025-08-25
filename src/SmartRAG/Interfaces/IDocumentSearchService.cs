namespace SmartRAG.Interfaces;

/// <summary>
/// Service interface for AI-powered search and RAG operations
/// </summary>
public interface IDocumentSearchService
{
    /// <summary>
    /// Search documents semantically
    /// </summary>
    Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5);

    /// <summary>
    /// Generate RAG answer
    /// </summary>
    Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5);
}
