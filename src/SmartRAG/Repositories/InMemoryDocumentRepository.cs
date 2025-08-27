using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Repositories;

/// <summary>
/// In-memory document repository implementation
/// </summary>
public class InMemoryDocumentRepository(
    InMemoryConfig config,
    ILogger<InMemoryDocumentRepository> logger) : IDocumentRepository
{
    #region Constants

    // Search constants  
    private const int DefaultMaxSearchResults = 5;

    // Collection constants
    private const int MinDocumentCapacity = 1;

    #endregion

    #region Fields

    private readonly List<SmartRAG.Entities.Document> _documents = new List<SmartRAG.Entities.Document>();
    private readonly System.Threading.Lock _lock = new();
    private readonly InMemoryConfig _config = config;
    private readonly ILogger<InMemoryDocumentRepository> _logger = logger;

    #endregion

    #region Properties

    protected ILogger Logger => _logger;

    public int CurrentCount => _documents.Count;

    public int MaxDocuments => _config.MaxDocuments;

    #endregion

    #region Public Methods

    public Task<SmartRAG.Entities.Document> AddAsync(SmartRAG.Entities.Document document)
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

    public Task<SmartRAG.Entities.Document?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            try
            {
                var document = _documents.FirstOrDefault(d => d.Id == id);

                if (document != null)
                {
                    RepositoryLogMessages.LogDocumentRetrieved(Logger, document.FileName, id, null);
                }
                else
                {
                    RepositoryLogMessages.LogDocumentNotFound(Logger, id, null);
                }

                return Task.FromResult(document);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentRetrievalFailed(Logger, id, ex);
                throw;
            }
        }
    }

    public Task<List<Entities.Document>> GetAllAsync()
    {
        lock (_lock)
        {
            try
            {
                var documents = _documents.ToList();
                RepositoryLogMessages.LogDocumentsRetrieved(Logger, documents.Count, null);
                return Task.FromResult(documents);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentsRetrievalFailed(Logger, ex);
                throw;
            }
        }
    }

    public Task<bool> DeleteAsync(Guid id)
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

    public Task<int> GetCountAsync()
    {
        lock (_lock)
        {
            try
            {
                var count = _documents.Count;
                RepositoryLogMessages.LogDocumentCountRetrieved(Logger, count, null);
                return Task.FromResult(count);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentCountRetrievalFailed(Logger, ex);
                throw;
            }
        }
    }

    public Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = DefaultMaxSearchResults)
    {
        lock (_lock)
        {
            try
            {
                var normalizedQuery = SmartRAG.Extensions.SearchTextExtensions.NormalizeForSearch(query);
                var relevantChunks = PerformSearch(normalizedQuery, maxResults);

                RepositoryLogMessages.LogSearchCompleted(Logger, query, relevantChunks.Count, maxResults, null);
                return Task.FromResult(relevantChunks);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogSearchFailed(Logger, query, ex);
                throw;
            }
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Removes oldest documents when capacity is exceeded
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
    /// Performs search operation on documents
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

    #endregion
}