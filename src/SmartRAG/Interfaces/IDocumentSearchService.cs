using SmartRAG.Entities;
using SmartRAG.Models;

namespace SmartRAG.Interfaces;

/// <summary>
/// Service interface for AI-powered RAG operations
/// </summary>
public interface IDocumentSearchService
{
    /// <summary>
    /// Generate RAG answer using semantic search
    /// </summary>
    Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5);
}
