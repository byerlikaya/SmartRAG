using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Repositories
{

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
        #region Constants

        // Search constants  
        private const int DefaultMaxSearchResults = 5;

        // Collection constants
        private const int MinDocumentCapacity = 1;
        private const int MaxConversationLength = 2000;
        private const int MaxSessions = 1000;

        #endregion

        #region Fields

        private readonly List<SmartRAG.Entities.Document> _documents = new List<SmartRAG.Entities.Document>();
        private readonly Dictionary<string, string> _conversations = new Dictionary<string, string>();
        private readonly object _lock = new object();


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

        public Task<SmartRAG.Entities.Document> GetByIdAsync(Guid id)
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

        #region Conversation Methods

        public Task<string> GetConversationHistoryAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Task.FromResult(string.Empty);

            lock (_lock)
            {
                return Task.FromResult(_conversations.TryGetValue(sessionId, out var history) ? history : string.Empty);
            }
        }

        public Task AddToConversationAsync(string sessionId, string question, string answer)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Task.CompletedTask;

            lock (_lock)
            {
                // Clean up old sessions if we have too many
                if (_conversations.Count >= MaxSessions)
                {
                    CleanupOldSessions();
                }

                // If question is empty, this is a special case (like session-id storage)
                if (string.IsNullOrEmpty(question))
                {
                    _conversations[sessionId] = answer;
                    return Task.CompletedTask;
                }

                var currentHistory = _conversations.TryGetValue(sessionId, out var existing) ? existing : string.Empty;
                var newEntry = string.IsNullOrEmpty(currentHistory) 
                    ? $"User: {question}\nAssistant: {answer}"
                    : $"{currentHistory}\nUser: {question}\nAssistant: {answer}";

                // Limit conversation length to prevent memory issues
                if (newEntry.Length > MaxConversationLength)
                {
                    newEntry = TruncateConversation(newEntry);
                }

                _conversations[sessionId] = newEntry;
            }
            
            return Task.CompletedTask;
        }

        public Task ClearConversationAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Task.CompletedTask;

            lock (_lock)
            {
                _conversations.Remove(sessionId);
            }
            
            return Task.CompletedTask;
        }

        public Task<bool> SessionExistsAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Task.FromResult(false);

            lock (_lock)
            {
                return Task.FromResult(_conversations.ContainsKey(sessionId));
            }
        }

        private void CleanupOldSessions()
        {
            // Simple cleanup: remove oldest sessions
            var sessionsToRemove = _conversations.Count - MaxSessions + 100;
            var keysToRemove = _conversations.Keys.Take(sessionsToRemove).ToList();

            foreach (var key in keysToRemove)
            {
                _conversations.Remove(key);
            }
        }

        private static string TruncateConversation(string conversation)
        {
            // Keep only the last few exchanges
            var lines = conversation.Split('\n');
            if (lines.Length <= 6) // Keep at least 3 exchanges (6 lines)
                return conversation;

            // Keep last 6 lines (3 exchanges)
            return string.Join("\n", lines.Skip(lines.Length - 6));
        }

        #endregion
    }
}
