using SmartRAG.Entities;

namespace SmartRAG.Interfaces;

/// <summary>
/// Interface for semantic search providers using embeddings and cosine similarity
/// </summary>
public interface ISemanticSearchProvider
{
    /// <summary>
    /// Search documents using semantic understanding
    /// </summary>
    Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5);
}
