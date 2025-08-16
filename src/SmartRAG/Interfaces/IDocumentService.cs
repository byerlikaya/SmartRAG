using SmartRAG.Entities;
using SmartRAG.Models;

namespace SmartRAG.Interfaces;

/// <summary>
/// Service interface for document operations
/// </summary>
public interface IDocumentService
{
    Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy);
    Task<Document?> GetDocumentAsync(Guid id);
    Task<List<Document>> GetAllDocumentsAsync();
    Task<bool> DeleteDocumentAsync(Guid id);
    Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5);
    Task<Dictionary<string, object>> GetStorageStatisticsAsync();
    Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5);
    Task<bool> RegenerateAllEmbeddingsAsync();
}
