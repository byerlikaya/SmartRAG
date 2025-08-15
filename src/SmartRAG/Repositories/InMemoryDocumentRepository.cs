using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;

namespace SmartRAG.Repositories;

/// <summary>
/// In-memory document repository implementation
/// </summary>
public class InMemoryDocumentRepository(InMemoryConfig config) : IDocumentRepository
{
    private readonly List<Document> _documents = [];
    private readonly Lock _lock = new();

    public Task<Document> AddAsync(Document document)
    {
        lock (_lock)
        {
            if (_documents.Count >= config.MaxDocuments)
            {
                var oldestDocuments = _documents
                    .OrderBy(d => d.UploadedAt)
                    .Take(_documents.Count - config.MaxDocuments + 1)
                    .ToList();

                foreach (var oldDoc in oldestDocuments)
                {
                    _documents.Remove(oldDoc);
                }
            }

            _documents.Add(document);
            return Task.FromResult(document);
        }
    }

    public Task<Document?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            var document = _documents.FirstOrDefault(d => d.Id == id);
            return Task.FromResult(document);
        }
    }

    public Task<List<Document>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_documents.ToList());
        }
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        lock (_lock)
        {
            var document = _documents.FirstOrDefault(d => d.Id == id);
            if (document == null)
                return Task.FromResult(false);

            return Task.FromResult(_documents.Remove(document));
        }
    }

    public Task<int> GetCountAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_documents.Count);
        }
    }

    public int CurrentCount => _documents.Count;

    public int MaxDocuments => config.MaxDocuments;

    public Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5)
    {
        lock (_lock)
        {
            var normalizedQuery = SmartRAG.Extensions.SearchTextExtensions.NormalizeForSearch(query);
            var relevantChunks = new List<DocumentChunk>();

            foreach (var document in _documents)
            {
                foreach (var chunk in document.Chunks)
                {
                    var normalizedChunk = SmartRAG.Extensions.SearchTextExtensions.NormalizeForSearch(chunk.Content);
                    if (normalizedChunk.Contains(normalizedQuery))
                    {
                        relevantChunks.Add(chunk);
                        if (relevantChunks.Count >= maxResults)
                            break;
                    }
                }
                if (relevantChunks.Count >= maxResults)
                    break;
            }

            return Task.FromResult(relevantChunks);
        }
    }
}