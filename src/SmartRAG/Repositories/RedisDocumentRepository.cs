using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartRAG.Repositories
{

    /// <summary>
    /// Redis document repository implementation
    /// </summary>
    public class RedisDocumentRepository : IDocumentRepository, IDisposable
    {
        #region Constants

        private const int DefaultConnectionTimeoutMs = 1000;
        private const int DefaultKeepAliveSeconds = 180;
        private const int DefaultMaxSearchResults = 5;
        private const string DocumentsListSuffix = "list";
        private const string MetadataKeySuffix = "meta:";
        private const string DateTimeFormat = "O";

        #endregion

        #region Fields

        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly string _documentsKey;
        private readonly string _documentPrefix;
        private readonly ILogger<RedisDocumentRepository> _logger;
        private bool _disposed;

        #endregion

        #region Properties

        protected ILogger Logger => _logger;

        #endregion

        #region Constructor

        public RedisDocumentRepository(IOptions<RedisConfig> config, ILogger<RedisDocumentRepository> logger)
        {
            var redisConfig = config.Value;
            _logger = logger;

            // Configure connection options
            var options = CreateConnectionOptions(redisConfig);

            ConfigureAuthentication(options, redisConfig);
            ConfigureSsl(options, redisConfig);

            try
            {
                _redis = ConnectionMultiplexer.Connect(options);
                _database = _redis.GetDatabase(redisConfig.Database);
                _documentsKey = $"{redisConfig.KeyPrefix}{DocumentsListSuffix}";
                _documentPrefix = redisConfig.KeyPrefix;

                ValidateConnection();
                RepositoryLogMessages.LogRedisConnectionEstablished(Logger, redisConfig.ConnectionString, null);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogRedisConnectionFailed(Logger, redisConfig.ConnectionString, ex);
                throw;
            }
        }

        #endregion

        #region Public Methods

        public async Task<SmartRAG.Entities.Document> AddAsync(SmartRAG.Entities.Document document)
        {
            try
            {
                var documentKey = CreateDocumentKey(document.Id);
                var metadataKey = CreateMetadataKey(document.Id);
                var documentJson = JsonSerializer.Serialize(document);
                var metadata = CreateDocumentMetadata(document);

                await ExecuteDocumentAddBatch(documentKey, metadataKey, documentJson, metadata, document.Id);

                RepositoryLogMessages.LogDocumentAdded(Logger, document.FileName, document.Id, null);
                return document;
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogDocumentAddFailed(Logger, document.FileName, ex);
                throw;
            }
        }

        public async Task<SmartRAG.Entities.Document> GetByIdAsync(Guid id)
        {
            try
            {
                var documentKey = CreateDocumentKey(id);
                var documentJson = await _database.StringGetAsync(documentKey);

                if (documentJson.IsNull)
                {
                    RepositoryLogMessages.LogRedisDocumentNotFound(Logger, id, null);
                    return null;
                }

                var document = JsonSerializer.Deserialize<SmartRAG.Entities.Document>(documentJson);
                RepositoryLogMessages.LogRedisDocumentRetrieved(Logger, id, null);
                return document;
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogRedisDocumentRetrievalFailed(Logger, id, ex);
                return null;
            }
        }

        public async Task<List<SmartRAG.Entities.Document>> GetAllAsync()
        {
            try
            {
                var documentIds = await _database.ListRangeAsync(_documentsKey);
                var documents = new List<SmartRAG.Entities.Document>();

                foreach (var idString in documentIds)
                {
                    if (Guid.TryParse(idString, out var id))
                    {
                        var document = await GetByIdAsync(id);
                        if (document != null)
                        {
                            documents.Add(document);
                        }
                    }
                }

                RepositoryLogMessages.LogRedisDocumentsRetrieved(Logger, documents.Count, null);
                return documents;
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogRedisDocumentsRetrievalFailed(Logger, ex);
                return new List<SmartRAG.Entities.Document>();
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var documentKey = CreateDocumentKey(id);
                var metadataKey = CreateMetadataKey(id);

                await ExecuteDocumentDeleteBatch(documentKey, metadataKey, id);

                RepositoryLogMessages.LogRedisDocumentDeleted(Logger, id, null);
                return true;
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogRedisDocumentDeleteFailed(Logger, id, ex);
                return false;
            }
        }

        public async Task<int> GetCountAsync()
        {
            try
            {
                var count = await _database.ListLengthAsync(_documentsKey);
                var result = (int)count;
                RepositoryLogMessages.LogRedisDocumentCountRetrieved(Logger, result, null);
                return result;
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogRedisDocumentCountRetrievalFailed(Logger, ex);
                return 0;
            }
        }

        public async Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = DefaultMaxSearchResults)
        {
            try
            {
                var normalizedQuery = Extensions.SearchTextExtensions.NormalizeForSearch(query);
                var relevantChunks = new List<DocumentChunk>();

                var documents = await GetAllAsync();

                foreach (var document in documents)
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

                RepositoryLogMessages.LogRedisSearchCompleted(Logger, query, relevantChunks.Count, maxResults, null);
                return relevantChunks;
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogRedisSearchFailed(Logger, query, ex);
                return new List<DocumentChunk>();
            }
        }

        public async Task<string> GetConversationHistoryAsync(string sessionId)
        {
            try
            {
                var conversationKey = $"conversation:{sessionId}";
                var conversationJson = await _database.StringGetAsync(conversationKey);
                
                if (conversationJson.IsNull)
                {
                    RepositoryLogMessages.LogRedisConversationNotFound(Logger, sessionId, null);
                    return string.Empty;
                }

                RepositoryLogMessages.LogRedisConversationRetrieved(Logger, sessionId, null);
                return conversationJson;
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogRedisConversationRetrievalFailed(Logger, sessionId, ex);
                return string.Empty;
            }
        }

        public async Task AddToConversationAsync(string sessionId, string question, string answer)
        {
            try
            {
                var conversationKey = $"conversation:{sessionId}";
                var existingConversation = await GetConversationHistoryAsync(sessionId);
                
                var newEntry = $"\nQ: {question}\nA: {answer}";
                var updatedConversation = existingConversation + newEntry;
                
                await _database.StringSetAsync(conversationKey, updatedConversation);
                RepositoryLogMessages.LogRedisConversationUpdated(Logger, sessionId, null);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogRedisConversationUpdateFailed(Logger, sessionId, ex);
            }
        }

        public async Task ClearConversationAsync(string sessionId)
        {
            try
            {
                var conversationKey = $"conversation:{sessionId}";
                await _database.KeyDeleteAsync(conversationKey);
                RepositoryLogMessages.LogRedisConversationCleared(Logger, sessionId, null);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogRedisConversationClearFailed(Logger, sessionId, ex);
            }
        }

        public async Task<bool> SessionExistsAsync(string sessionId)
        {
            try
            {
                var conversationKey = $"conversation:{sessionId}";
                var exists = await _database.KeyExistsAsync(conversationKey);
                RepositoryLogMessages.LogRedisSessionExistsChecked(Logger, sessionId, exists, null);
                return exists;
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogRedisSessionExistsCheckFailed(Logger, sessionId, ex);
                return false;
            }
        }

        #endregion

        #region Private Helper Methods

        private static ConfigurationOptions CreateConnectionOptions(RedisConfig config)
        {
            return new ConfigurationOptions
            {
                EndPoints = { config.ConnectionString },
                ConnectTimeout = config.ConnectionTimeout * DefaultConnectionTimeoutMs,
                SyncTimeout = config.ConnectionTimeout * DefaultConnectionTimeoutMs,
                ConnectRetry = config.RetryCount,
                ReconnectRetryPolicy = new ExponentialRetry(config.RetryDelay),
                AllowAdmin = true,
                AbortOnConnectFail = false,
                KeepAlive = DefaultKeepAliveSeconds
            };
        }

        private static void ConfigureAuthentication(ConfigurationOptions options, RedisConfig config)
        {
            if (!string.IsNullOrEmpty(config.Username))
            {
                options.User = config.Username;
            }

            if (!string.IsNullOrEmpty(config.Password))
            {
                options.Password = config.Password;
            }
        }

        private static void ConfigureSsl(ConfigurationOptions options, RedisConfig config)
        {
            if (config.EnableSsl)
            {
                options.Ssl = true;
                options.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            }
        }

        private void ValidateConnection()
        {
            if (!_redis.IsConnected)
            {
                throw new InvalidOperationException("Failed to connect to Redis server");
            }
        }

        private string CreateDocumentKey(Guid id) => $"{_documentPrefix}{id}";

        private string CreateMetadataKey(Guid id) => $"{_documentPrefix}{MetadataKeySuffix}{id}";

        private static HashEntry[] CreateDocumentMetadata(SmartRAG.Entities.Document document)
        {
            return new HashEntry[]
            {
            new HashEntry("id", document.Id.ToString()),
            new HashEntry("fileName", document.FileName),
            new HashEntry("contentType", document.ContentType),
            new HashEntry("fileSize", document.FileSize.ToString(CultureInfo.InvariantCulture)),
            new HashEntry("uploadedAt", document.UploadedAt.ToString(DateTimeFormat)),
            new HashEntry("uploadedBy", document.UploadedBy),
            new HashEntry("chunkCount", document.Chunks.Count.ToString(CultureInfo.InvariantCulture))
            };
        }

        private async Task ExecuteDocumentAddBatch(string documentKey, string metadataKey, string documentJson, HashEntry[] metadata, Guid documentId)
        {
            var batch = _database.CreateBatch();

            var setTask = batch.StringSetAsync(documentKey, documentJson);
            var pushTask = batch.ListRightPushAsync(_documentsKey, documentId.ToString());
            var hashTask = batch.HashSetAsync(metadataKey, metadata);

            batch.Execute();
            await Task.WhenAll(setTask, pushTask, hashTask);
        }

        private async Task ExecuteDocumentDeleteBatch(string documentKey, string metadataKey, Guid documentId)
        {
            var batch = _database.CreateBatch();

            var deleteDocTask = batch.KeyDeleteAsync(documentKey);
            var removeFromListTask = batch.ListRemoveAsync(_documentsKey, documentId.ToString());
            var deleteMetaTask = batch.KeyDeleteAsync(metadataKey);

            batch.Execute();
            await Task.WhenAll(deleteDocTask, removeFromListTask, deleteMetaTask);
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _redis?.Close();
                _redis?.Dispose();
                _disposed = true;
            }
        }
    }
}
