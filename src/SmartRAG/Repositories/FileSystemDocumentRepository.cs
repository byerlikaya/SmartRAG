using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Extensions;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Parser;
using SmartRAG.Interfaces.Search;
using SmartRAG.Interfaces.Storage;
using SmartRAG.Interfaces.Storage.Qdrant;
using SmartRAG.Interfaces.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartRAG.Repositories
{

    /// <summary>
    /// File system document repository implementation
    /// </summary>
    public class FileSystemDocumentRepository : IDocumentRepository
    {
        #region Constants

        // File and path constants
        private const string MetadataFileName = "metadata.json";
        private const string DocumentFileExtension = ".json";

        // Search constants
        private const int DefaultMaxSearchResults = 5;

        // JSON serialization constants
        private const bool WriteIndented = true;
        private const int MaxConversationLength = 2000;

        #endregion

        #region Fields

        private readonly string _basePath;
        private readonly string _metadataFile;
        private readonly string _conversationsPath;
        private readonly object _lock = new object();
        private readonly ILogger<FileSystemDocumentRepository> _logger;

        #endregion

        #region Properties

        protected ILogger Logger => _logger;

        public string StoragePath => _basePath;

        #endregion

        #region Constructor

        /// <summary>
        /// Shared JsonSerializerOptions for consistent serialization
        /// </summary>
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = WriteIndented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Shared JsonSerializerOptions for deserialization
        /// </summary>
        private static readonly JsonSerializerOptions _jsonDeserializeOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public FileSystemDocumentRepository(string basePath, ILogger<FileSystemDocumentRepository> logger)
        {
            _basePath = Path.GetFullPath(basePath);
            _metadataFile = Path.Combine(_basePath, MetadataFileName);
            _conversationsPath = Path.Combine(_basePath, "Conversations");
            _logger = logger;

            Directory.CreateDirectory(_basePath);
            Directory.CreateDirectory(_conversationsPath);

            if (!File.Exists(_metadataFile))
            {
                SaveMetadata(new List<Document>());
            }
        }

        #endregion

        #region Public Methods

        public Task<Document> AddAsync(Document document)
        {
            lock (_lock)
            {
                try
                {
                    var documents = LoadMetadata();

                    if (documents.Any(d => d.Id == document.Id))
                    {
                        RepositoryLogMessages.LogDocumentAlreadyExists(Logger, document.Id, null);
                        throw new InvalidOperationException($"Document with ID {document.Id} already exists");
                    }

                    var documentPath = GetDocumentPath(document.Id);
                    var documentData = CreateDocumentData(document);
                    var json = JsonSerializer.Serialize(documentData, _jsonOptions);

                    File.WriteAllText(documentPath, json);
                    documents.Add(document);
                    SaveMetadata(documents);

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

        public Task<Document> GetByIdAsync(Guid id)
        {
            lock (_lock)
            {
                try
                {
                    var documents = LoadMetadata();
                    var document = documents.FirstOrDefault(d => d.Id == id);

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

        public Task<List<SmartRAG.Entities.Document>> GetAllAsync()
        {
            lock (_lock)
            {
                try
                {
                    var documents = LoadMetadata();
                    RepositoryLogMessages.LogDocumentsRetrieved(Logger, documents.Count, null);
                    return Task.FromResult(documents.ToList());
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
                    var documents = LoadMetadata();
                    var document = documents.FirstOrDefault(d => d.Id == id);

                    if (document == null)
                    {
                        RepositoryLogMessages.LogDocumentDeleteNotFound(Logger, id, null);
                        return Task.FromResult(false);
                    }

                    var documentPath = GetDocumentPath(id);

                    if (File.Exists(documentPath))
                    {
                        File.Delete(documentPath);
                    }

                    documents.Remove(document);
                    SaveMetadata(documents);

                    RepositoryLogMessages.LogDocumentDeleted(Logger, document.FileName, id, null);
                    return Task.FromResult(true);
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
                    var documents = LoadMetadata();
                    var count = documents.Count;
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

        /// <summary>
        /// Loads metadata from file
        /// </summary>
        private List<Document> LoadMetadata()
        {
            try
            {
                if (!File.Exists(_metadataFile))
                    return new List<Document>();

                var json = File.ReadAllText(_metadataFile);
                var documents = JsonSerializer.Deserialize<List<Document>>(json, _jsonDeserializeOptions);

                return documents ?? new List<Document>();
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogMetadataLoadFailed(Logger, ex);
                // If metadata file is corrupted, return empty list
                return new List<Document>();
            }
        }

        /// <summary>
        /// Saves metadata to file
        /// </summary>
        private void SaveMetadata(List<Document> documents)
        {
            try
            {
                var json = JsonSerializer.Serialize(documents, _jsonOptions);
                File.WriteAllText(_metadataFile, json);
                RepositoryLogMessages.LogMetadataSaved(Logger, documents.Count, null);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogMetadataSaveFailed(Logger, ex);
                throw;
            }
        }

        /// <summary>
        /// Gets document file path
        /// </summary>
        private string GetDocumentPath(Guid id)
        {
            return Path.Combine(_basePath, $"{id}{DocumentFileExtension}");
        }

        public long GetTotalSize()
        {
            lock (_lock)
            {
                try
                {
                    var documents = LoadMetadata();
                    var totalSize = documents.Sum(d => d.FileSize);
                    RepositoryLogMessages.LogTotalSizeRetrieved(Logger, totalSize, null);
                    return totalSize;
                }
                catch (Exception ex)
                {
                    RepositoryLogMessages.LogTotalSizeRetrievalFailed(Logger, ex);
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
                    var documents = LoadMetadata();
                    var normalizedQuery = SmartRAG.Extensions.SearchTextExtensions.NormalizeForSearch(query);
                    var relevantChunks = PerformSearch(documents, normalizedQuery, maxResults);

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
        /// Creates document data object for serialization
        /// </summary>
        private static object CreateDocumentData(Entities.Document document)
        {
            return new
            {
                Id = document.Id,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSize = document.FileSize,
                UploadedAt = document.UploadedAt,
                UploadedBy = document.UploadedBy,
                Chunks = document.Chunks
            };
        }

        /// <summary>
        /// Performs search operation on documents
        /// </summary>
        private static List<DocumentChunk> PerformSearch(List<Document> documents, string normalizedQuery, int maxResults)
        {
            var relevantChunks = new List<DocumentChunk>();

            foreach (var document in documents)
            {
                foreach (var chunk in document.Chunks)
                {
                    var normalizedChunk = SearchTextExtensions.NormalizeForSearch(chunk.Content);
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

        public async Task<string> GetConversationHistoryAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return string.Empty;

            try
            {
                var filePath = GetConversationFilePath(sessionId);
                if (!File.Exists(filePath))
                {
                    return string.Empty;
                }

                return await Task.Run(() => File.ReadAllText(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history for session {SessionId}", sessionId);
                return string.Empty;
            }
        }

        public async Task AddToConversationAsync(string sessionId, string question, string answer)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            try
            {
                // If question is empty, this is a special case (like session-id storage)
                if (string.IsNullOrEmpty(question))
                {
                    var sessionFilePath = GetConversationFilePath(sessionId);
                    await Task.Run(() => File.WriteAllText(sessionFilePath, answer));
                    return;
                }

                var currentHistory = await GetConversationHistoryAsync(sessionId);
                var newEntry = string.IsNullOrEmpty(currentHistory) 
                    ? $"User: {question}\nAssistant: {answer}"
                    : $"{currentHistory}\nUser: {question}\nAssistant: {answer}";

                // Limit conversation length to prevent memory issues
                if (newEntry.Length > MaxConversationLength)
                {
                    newEntry = TruncateConversation(newEntry);
                }

                var filePath = GetConversationFilePath(sessionId);
                await Task.Run(() => File.WriteAllText(filePath, newEntry));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to conversation for session {SessionId}", sessionId);
            }
        }

        public async Task ClearConversationAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            try
            {
                var filePath = GetConversationFilePath(sessionId);
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing conversation for session {SessionId}", sessionId);
            }
        }

        public async Task<bool> SessionExistsAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            try
            {
                var filePath = GetConversationFilePath(sessionId);
                return await Task.Run(() => File.Exists(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking session existence for {SessionId}", sessionId);
                return false;
            }
        }

        private string GetConversationFilePath(string sessionId)
        {
            var fileName = $"{sessionId}.txt";
            return Path.Combine(_conversationsPath, fileName);
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
