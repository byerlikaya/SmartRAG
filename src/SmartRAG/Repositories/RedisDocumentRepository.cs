using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Document;
using SmartRAG.Models;
using StackExchange.Redis;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Repositories;



/// <summary>
/// Redis document repository implementation with RediSearch vector similarity search
/// </summary>
public class RedisDocumentRepository : IDocumentRepository, IDisposable
{
    private const int DefaultConnectionTimeoutMs = 1000;
    private const int DefaultKeepAliveSeconds = 180;
    private const int DefaultMaxSearchResults = 5;
    private const string DocumentsListSuffix = "list";
    private const string MetadataKeySuffix = "meta:";
    private const string ChunkKeySuffix = "chunk:";
    private const string DateTimeFormat = "O";

    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly string _documentsKey;
    private readonly string _documentPrefix;
    private readonly ILogger<RedisDocumentRepository> _logger;
    private readonly RedisConfig _config;
    private readonly IAIProvider _aiProvider;
    private readonly IAIConfigurationService _aiConfigurationService;
    private readonly SearchCommands _searchCommands;
    private bool _disposed;

    protected ILogger Logger => _logger;

    /// <summary>
    /// Initializes a new instance of RedisDocumentRepository with vector search support
    /// </summary>
    /// <param name="config">Redis configuration</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="aiProvider">AI provider for embedding generation</param>
    /// <param name="aiConfigurationService">AI configuration service for getting provider config</param>
    public RedisDocumentRepository(
        IOptions<RedisConfig> config,
        ILogger<RedisDocumentRepository> logger,
        IAIProvider aiProvider,
        IAIConfigurationService aiConfigurationService)
    {
        var redisConfig = config.Value;
        _logger = logger;
        _config = redisConfig;
        _aiProvider = aiProvider;
        _aiConfigurationService = aiConfigurationService;

        var options = CreateConnectionOptions(redisConfig);

        ConfigureAuthentication(options, redisConfig);
        ConfigureSsl(options, redisConfig);

        try
        {
            _redis = ConnectionMultiplexer.Connect(options);
            _database = _redis.GetDatabase(redisConfig.Database);
            _searchCommands = _database.FT();
            _documentsKey = $"{redisConfig.KeyPrefix}{DocumentsListSuffix}";
            _documentPrefix = redisConfig.KeyPrefix;

            ValidateConnection();

            if (_config.EnableVectorSearch)
            {
                _ = EnsureVectorIndexExistsAsync();
            }

            RepositoryLogMessages.LogRedisConnectionEstablished(Logger, redisConfig.ConnectionString, null);
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogRedisConnectionFailed(Logger, redisConfig.ConnectionString, ex);
            throw;
        }
    }

    public async Task<SmartRAG.Entities.Document> AddAsync(SmartRAG.Entities.Document document, CancellationToken cancellationToken = default)
    {
        try
        {
            SmartRAG.Services.Helpers.DocumentValidator.ValidateDocument(document);
            SmartRAG.Services.Helpers.DocumentValidator.ValidateChunks(document);

            var documentKey = CreateDocumentKey(document.Id);
            var metadataKey = CreateMetadataKey(document.Id);
            var documentJson = JsonSerializer.Serialize(document);
            var metadata = CreateDocumentMetadata(document);

            await ExecuteDocumentAddBatch(documentKey, metadataKey, documentJson, metadata, document.Id);

            if (_config.EnableVectorSearch)
            {
                await EnsureVectorIndexExistsAsync();

                var batch = _database.CreateBatch();
                var tasks = new List<Task>();

                foreach (var chunk in document.Chunks)
                {
                    var chunkKey = $"{_config.KeyPrefix}{ChunkKeySuffix}{chunk.Id}";
                    var embedding = chunk.Embedding;

                    if (embedding == null || embedding.Count == 0)
                    {
                        try
                        {
                            embedding = await _aiProvider.GenerateEmbeddingAsync(chunk.Content, null, cancellationToken);
                            chunk.Embedding = embedding;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate embedding for chunk {ChunkId}", chunk.Id);
                            continue;
                        }
                    }

                    var hashEntries = new HashEntry[]
                    {
                        new HashEntry("documentId", document.Id.ToString()),
                        new HashEntry("fileName", chunk.FileName ?? document.FileName),
                        new HashEntry("content", chunk.Content),
                        new HashEntry("chunkIndex", chunk.ChunkIndex),
                        new HashEntry("embedding", SerializeEmbedding(embedding))
                    };

                    tasks.Add(batch.HashSetAsync(chunkKey, hashEntries));
                }

                batch.Execute();
                await Task.WhenAll(tasks);

                await _database.StringSetAsync(documentKey, JsonSerializer.Serialize(document));
            }

            RepositoryLogMessages.LogDocumentAdded(Logger, document.FileName, document.Id, null);
            return document;
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogDocumentAddFailed(Logger, document.FileName, ex);
            throw;
        }
    }

    public async Task<SmartRAG.Entities.Document> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var documentKey = CreateDocumentKey(id);
            var documentJson = await _database.StringGetAsync(documentKey);

            if (documentJson.IsNull)
            {
                return null;
            }

            var document = JsonSerializer.Deserialize<SmartRAG.Entities.Document>(documentJson);
            return document;
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogRedisDocumentRetrievalFailed(Logger, id, ex);
            return null;
        }
    }

    public async Task<List<SmartRAG.Entities.Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var documentIds = await _database.ListRangeAsync(_documentsKey);
            var documents = new List<SmartRAG.Entities.Document>();
            var processedDocumentIds = new HashSet<Guid>();

            foreach (var idString in documentIds)
            {
                if (Guid.TryParse(idString, out var id))
                {
                    var document = await GetByIdAsync(id, cancellationToken);
                    if (document != null)
                    {
                        documents.Add(document);
                        processedDocumentIds.Add(id);
                    }
                }
            }

            if (_config.EnableVectorSearch && documents.Count == 0)
            {
                try
                {
                    var chunkKeys = await GetAllChunkKeysAsync(cancellationToken);
                    var documentMap = new Dictionary<Guid, DocumentData>();

                    foreach (var chunkKey in chunkKeys)
                    {
                        var chunkData = await _database.HashGetAllAsync(chunkKey);
                        if (chunkData == null || chunkData.Length == 0) continue;

                        var docIdEntry = chunkData.FirstOrDefault(h => h.Name == "documentId");
                        if (docIdEntry.Equals(default(HashEntry)) || docIdEntry.Value.IsNull ||
                            !Guid.TryParse(docIdEntry.Value.ToString(), out var docId)) continue;

                        var contentEntry = chunkData.FirstOrDefault(h => h.Name == "content");
                        var chunkIndexEntry = chunkData.FirstOrDefault(h => h.Name == "chunkIndex");
                        var fileNameEntry = chunkData.FirstOrDefault(h => h.Name == "fileName");

                        if (!documentMap.ContainsKey(docId))
                        {
                            documentMap[docId] = new DocumentData
                            {
                                Id = docId,
                                FileName = fileNameEntry.Equals(default(HashEntry)) || fileNameEntry.Value.IsNull
                                    ? string.Empty
                                    : fileNameEntry.Value.ToString()
                            };
                        }

                        if (!contentEntry.Equals(default(HashEntry)) && !contentEntry.Value.IsNull &&
                            int.TryParse(chunkIndexEntry.Equals(default(HashEntry)) || chunkIndexEntry.Value.IsNull
                                ? "0"
                                : chunkIndexEntry.Value.ToString(), out var chunkIndex))
                        {
                            var fileName = fileNameEntry.Equals(default(HashEntry)) || fileNameEntry.Value.IsNull
                                ? documentMap[docId].FileName
                                : fileNameEntry.Value.ToString();

                            documentMap[docId].Chunks.Add(new DocumentChunk
                            {
                                Id = Guid.Parse(chunkKey.Replace($"{_config.KeyPrefix}{ChunkKeySuffix}", "")),
                                DocumentId = docId,
                                FileName = fileName,
                                Content = contentEntry.Value.ToString(),
                                ChunkIndex = chunkIndex
                            });
                        }
                    }

                    foreach (var docData in documentMap.Values)
                    {
                        if (!processedDocumentIds.Contains(docData.Id))
                        {
                            var orderedChunks = docData.Chunks.OrderBy(c => c.ChunkIndex).ToList();
                            documents.Add(new SmartRAG.Entities.Document
                            {
                                Id = docData.Id,
                                FileName = docData.FileName,
                                ContentType = "application/octet-stream",
                                Content = string.Join("\n", orderedChunks.Select(c => c.Content)),
                                UploadedBy = "system",
                                UploadedAt = DateTime.UtcNow,
                                Chunks = orderedChunks,
                                FileSize = 0
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve documents from chunks");
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

    private async Task<List<string>> GetAllChunkKeysAsync(CancellationToken cancellationToken = default)
    {
        var chunkKeys = new List<string>();
        var pattern = $"{_config.KeyPrefix}{ChunkKeySuffix}*";
        var endpoints = _redis.GetEndPoints();
        
        if (endpoints == null || endpoints.Length == 0)
        {
            _logger.LogWarning("No Redis endpoints available for chunk key retrieval");
            return chunkKeys;
        }

        var server = _redis.GetServer(endpoints.First());
        
        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            cancellationToken.ThrowIfCancellationRequested();
            chunkKeys.Add(key);
        }

        return chunkKeys;
    }

    public async Task<bool> ClearAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await GetAllAsync(cancellationToken);
            foreach (var doc in documents)
            {
                await DeleteAsync(doc.Id, cancellationToken);
            }

            if (_config.EnableVectorSearch)
            {
                try
                {
                    var chunkKeys = await GetAllChunkKeysAsync(cancellationToken);
                    if (chunkKeys.Count > 0)
                    {
                        var batch = _database.CreateBatch();
                        var tasks = new List<Task>();
                        
                        foreach (var chunkKey in chunkKeys)
                        {
                            tasks.Add(batch.KeyDeleteAsync(chunkKey));
                        }
                        
                        batch.Execute();
                        await Task.WhenAll(tasks);
                        
                        _logger.LogInformation("Cleared {Count} chunks from Redis", chunkKeys.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clear chunks from Redis");
                }
            }

            await _database.KeyDeleteAsync(_documentsKey);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear all documents from Redis");
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
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

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _database.ListLengthAsync(_documentsKey);
            var result = (int)count;
            return result;
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogRedisDocumentCountRetrievalFailed(Logger, ex);
            return 0;
        }
    }

    public async Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = DefaultMaxSearchResults, CancellationToken cancellationToken = default)
    {
        if (!_config.EnableVectorSearch)
        {
            return await FallbackTextSearchAsync(query, maxResults, cancellationToken);
        }

        try
        {
            var aiConfig = _aiConfigurationService.GetAIProviderConfig();
            if (aiConfig == null)
            {
                RepositoryLogMessages.LogRedisVectorSearchFailed(Logger, query, new InvalidOperationException("AI provider configuration not available"));
                return await FallbackTextSearchAsync(query, maxResults, cancellationToken);
            }

            var queryEmbedding = await _aiProvider.GenerateEmbeddingAsync(query, aiConfig, cancellationToken);

            // KNN search syntax: *=>[KNN {k} @vector_field $query_vector AS score]
            var searchQuery = new Query($"*=>[KNN {maxResults} @embedding $vec AS score]")
                .AddParam("vec", SerializeEmbedding(queryEmbedding))
                .SetSortBy("score")
                .ReturnFields("documentId", "fileName", "content", "chunkIndex", "score")
                .Dialect(2); // Dialect 2 is required for vector search

            var searchResult = await _searchCommands.SearchAsync(_config.VectorIndexName, searchQuery);

            var results = new List<DocumentChunk>();

            foreach (var doc in searchResult.Documents)
            {
                var properties = doc.GetProperties().ToDictionary(x => x.Key, x => x.Value);

                if (properties.TryGetValue("content", out var content) &&
                    properties.TryGetValue("documentId", out var docIdStr) &&
                    properties.TryGetValue("chunkIndex", out var chunkIndexStr) &&
                    properties.TryGetValue("score", out var scoreStr))
                {
                    if (Guid.TryParse(docIdStr.ToString(), out var docId) &&
                        int.TryParse(chunkIndexStr.ToString(), out var chunkIndex))
                    {
                        double score = 0.0;
                        if (scoreStr.HasValue && !scoreStr.IsNull && double.TryParse(scoreStr.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedScore))
                        {
                            score = parsedScore;
                        }

                        var distanceMetric = _config.DistanceMetric?.ToUpperInvariant() ?? "COSINE";
                        var similarity = distanceMetric switch
                        {
                            "COSINE" => (float)Math.Max(0.0, Math.Min(1.0, 1.0 - (score / 2.0))),
                            "L2" => (float)(1.0 / (1.0 + score)),
                            "IP" => (float)Math.Max(0.0, Math.Min(1.0, (score + 1.0) / 2.0)),
                            _ => (float)Math.Max(0.0, Math.Min(1.0, 1.0 - score)),
                        };
                        var relevanceScore = similarity * 100.0;

                        var fileName = properties.TryGetValue("fileName", out var fileNameValue) 
                            ? fileNameValue.ToString() 
                            : string.Empty;

                        results.Add(new DocumentChunk
                        {
                            Id = Guid.Parse(doc.Id.Replace($"{_config.KeyPrefix}{ChunkKeySuffix}", "")),
                            DocumentId = docId,
                            FileName = fileName,
                            Content = content.ToString(),
                            ChunkIndex = chunkIndex,
                            Embedding = new List<float>(),
                            RelevanceScore = relevanceScore
                        });
                    }
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogRedisVectorSearchFailed(Logger, query, ex);
            return await FallbackTextSearchAsync(query, maxResults, cancellationToken);
        }
    }

    private async Task<List<DocumentChunk>> FallbackTextSearchAsync(string query, int maxResults, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedQuery = Extensions.SearchTextExtensions.NormalizeForSearch(query);
            var relevantChunks = new List<DocumentChunk>();

            var documents = await GetAllAsync(cancellationToken);

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

            return relevantChunks;
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogRedisSearchFailed(Logger, query, ex);
            return new List<DocumentChunk>();
        }
    }

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
        if (config.UseSsl)
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
        var entries = new List<HashEntry>
        {
        new HashEntry("id", document.Id.ToString()),
        new HashEntry("fileName", document.FileName),
        new HashEntry("contentType", document.ContentType),
        new HashEntry("fileSize", document.FileSize.ToString(CultureInfo.InvariantCulture)),
        new HashEntry("uploadedAt", document.UploadedAt.ToString(DateTimeFormat)),
        new HashEntry("uploadedBy", document.UploadedBy),
        new HashEntry("chunkCount", document.Chunks.Count.ToString(CultureInfo.InvariantCulture))
        };

        if (document.Metadata != null)
        {
            foreach (var metadataItem in document.Metadata)
            {
                if (metadataItem.Value != null)
                {
                    entries.Add(new HashEntry($"metadata_{metadataItem.Key}", metadataItem.Value.ToString()));
                }
            }
        }

        return entries.ToArray();
    }

    private async Task EnsureVectorIndexExistsAsync()
    {
        try
        {
            await _searchCommands.InfoAsync(_config.VectorIndexName);
            RepositoryLogMessages.LogRedisVectorIndexExists(Logger, _config.VectorIndexName, null);
        }
        catch (Exception)
        {
            await CreateVectorIndexAsync();
        }
    }

    private async Task CreateVectorIndexAsync()
    {
        try
        {
            var schema = new Schema()
                .AddTextField("content", 1.0)
                .AddTagField("documentId")
                .AddTagField("fileName")
                .AddNumericField("chunkIndex")
                .AddVectorField("embedding",
                    Schema.VectorField.VectorAlgo.HNSW,
                    new Dictionary<string, object>
                    {
                        ["TYPE"] = "FLOAT32",
                        ["DIM"] = _config.VectorDimension,
                        ["DISTANCE_METRIC"] = _config.DistanceMetric,
                        ["INITIAL_CAP"] = 1000
                    });

            var parameters = new FTCreateParams()
                .On(IndexDataType.HASH)
                .Prefix($"{_config.KeyPrefix}{ChunkKeySuffix}");

            await _searchCommands.CreateAsync(
                _config.VectorIndexName,
                parameters,
                schema);

            RepositoryLogMessages.LogRedisVectorIndexCreated(Logger, _config.VectorIndexName, null);
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogRedisVectorIndexCreationFailure(Logger, _config.VectorIndexName, ex);

            var errorMessage = ex.Message ?? string.Empty;
            if (errorMessage.Contains("unknown command 'FT.CREATE'", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("FT.CREATE", StringComparison.OrdinalIgnoreCase))
            {
                RepositoryLogMessages.LogRedisRediSearchModuleMissing(Logger, null);
            }
        }
    }

    private static byte[] SerializeEmbedding(List<float> embedding)
    {
        var bytes = new byte[embedding.Count * 4];
        Buffer.BlockCopy(embedding.ToArray(), 0, bytes, 0, bytes.Length);
        return bytes;
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
        var allTasks = new List<Task>
        {
            batch.KeyDeleteAsync(documentKey),
            batch.ListRemoveAsync(_documentsKey, documentId.ToString()),
            batch.KeyDeleteAsync(metadataKey)
        };

        if (_config.EnableVectorSearch)
        {
            try
            {
                var query = new Query($"@documentId:{{{documentId}}}").ReturnFields("id");
                var result = await _searchCommands.SearchAsync(_config.VectorIndexName, query);

                foreach (var doc in result.Documents)
                {
                    var chunkKey = doc.Id;
                    allTasks.Add(batch.KeyDeleteAsync(chunkKey));
                }
            }
            catch
            {
            }
        }

        batch.Execute();
        await Task.WhenAll(allTasks);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _redis?.Close();
            _redis?.Dispose();
            _disposed = true;
        }
    }

    private class DocumentData
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public List<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }
}

