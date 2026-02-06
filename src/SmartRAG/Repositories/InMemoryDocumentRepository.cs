using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Document;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Repositories;



/// <summary>
/// In-memory document repository implementation
/// </summary>
public class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly InMemoryConfig _config;
    private readonly ILogger<InMemoryDocumentRepository> _logger;

    public InMemoryDocumentRepository(
        InMemoryConfig config,
        ILogger<InMemoryDocumentRepository> logger)
    {
        _config = config;
        _logger = logger;
    }

    private const int DefaultMaxSearchResults = 5;
    private const int MinDocumentCapacity = 1;

    private readonly List<SmartRAG.Entities.Document> _documents = new List<SmartRAG.Entities.Document>();
    private readonly object _lock = new object();

    protected ILogger Logger => _logger;

    public Task<SmartRAG.Entities.Document> AddAsync(SmartRAG.Entities.Document document, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            try
            {
                if (_documents.Count >= _config.MaxDocuments)
                {
                    var removedCount = RemoveOldestDocuments();
                    RepositoryLogMessages.LogOldDocumentsRemoved(Logger, removedCount, _config.MaxDocuments, null);
                }

                SmartRAG.Services.Helpers.DocumentValidator.ValidateDocument(document);
                SmartRAG.Services.Helpers.DocumentValidator.ValidateChunks(document);

                _documents.Add(document);
                RepositoryLogMessages.LogDocumentAdded(Logger, document.FileName, document.Id, null);
                return Task.FromResult(document);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentAddFailed(Logger, document.FileName, ex);
                throw;
            }
        }
    }

    public Task<SmartRAG.Entities.Document> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            try
            {
                var document = _documents.FirstOrDefault(d => d.Id == id);
                return Task.FromResult(document);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentRetrievalFailed(Logger, id, ex);
                throw;
            }
        }
    }

    public Task<List<Entities.Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            try
            {
                var documents = _documents.ToList();
                return Task.FromResult(documents);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentsRetrievalFailed(Logger, ex);
                throw;
            }
        }
    }

    public Task<bool> ClearAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _documents.Clear();
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            try
            {
                var document = _documents.FirstOrDefault(d => d.Id == id);

                if (document == null)
                {
                    RepositoryLogMessages.LogDocumentDeleteNotFound(Logger, id, null);
                    return Task.FromResult(false);
                }

                var removed = _documents.Remove(document);
                if (removed)
                {
                    RepositoryLogMessages.LogDocumentDeleted(Logger, document.FileName, id, null);
                }

                return Task.FromResult(removed);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentDeleteFailed(Logger, id, ex);
                throw;
            }
        }
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            try
            {
                var count = _documents.Count;
                return Task.FromResult(count);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentCountRetrievalFailed(Logger, ex);
                throw;
            }
        }
    }

    public Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = DefaultMaxSearchResults, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            try
            {
                var normalizedQuery = SmartRAG.Extensions.SearchTextExtensions.NormalizeForSearch(query);
                var relevantChunks = PerformSearch(normalizedQuery, maxResults);

                return Task.FromResult(relevantChunks);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogSearchFailed(Logger, query, ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Removes oldest documents when capacity limit is reached
    /// </summary>
    private int RemoveOldestDocuments()
    {
        var documentsToRemove = _documents.Count - _config.MaxDocuments + MinDocumentCapacity;
        var oldestDocuments = _documents
            .OrderBy(d => d.UploadedAt)
            .Take(documentsToRemove)
            .ToList();

        foreach (var oldDoc in oldestDocuments)
        {
            _documents.Remove(oldDoc);
        }

        return oldestDocuments.Count;
    }

    /// <summary>
    /// Performs text search on document chunks
    /// </summary>
    private List<DocumentChunk> PerformSearch(string normalizedQuery, int maxResults)
    {
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

        return relevantChunks;
    }
}

