using SmartRAG.Entities;

namespace SmartRAG.Interfaces;

public interface IDocumentRepository
{
    Task<Document> AddAsync(Document document);
    Task<Document?> GetByIdAsync(Guid id);
    Task<List<Document>> GetAllAsync();
    Task<bool> DeleteAsync(Guid id);
    Task<int> GetCountAsync();
    Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5);
}
