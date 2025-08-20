using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using StackExchange.Redis;
using System.Text.Json;
using System.Globalization;

namespace SmartRAG.Repositories;

/// <summary>
/// Redis document repository implementation
/// </summary>
public class RedisDocumentRepository : IDocumentRepository, IDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly string _documentsKey;
    private readonly string _documentPrefix;
    private bool _disposed;

    public RedisDocumentRepository(IOptions<RedisConfig> config)
    {
        var redisConfig = config.Value;
        
        // Configure connection options
        var options = new ConfigurationOptions
        {
            EndPoints = { redisConfig.ConnectionString },
            ConnectTimeout = redisConfig.ConnectionTimeout * 1000,
            SyncTimeout = redisConfig.ConnectionTimeout * 1000,
            ConnectRetry = redisConfig.RetryCount,
            ReconnectRetryPolicy = new ExponentialRetry(redisConfig.RetryDelay),
            AllowAdmin = true,
            AbortOnConnectFail = false,
            KeepAlive = 180
        };

        // Add authentication if provided
        if (!string.IsNullOrEmpty(redisConfig.Username))
        {
            options.User = redisConfig.Username;
        }

        if (!string.IsNullOrEmpty(redisConfig.Password))
        {
            options.Password = redisConfig.Password;
        }

        // Enable SSL if configured
        if (redisConfig.EnableSsl)
        {
            options.Ssl = true;
            options.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        }

        _redis = ConnectionMultiplexer.Connect(options);
        _database = _redis.GetDatabase(redisConfig.Database);
        _documentsKey = $"{redisConfig.KeyPrefix}list";
        _documentPrefix = redisConfig.KeyPrefix;

        // Test connection
        if (!_redis.IsConnected)
        {
            throw new InvalidOperationException("Failed to connect to Redis server");
        }
    }

    public async Task<Document> AddAsync(Document document)
    {
        var documentKey = $"{_documentPrefix}{document.Id}";
        var documentJson = JsonSerializer.Serialize(document);
        var metadataKey = $"{_documentPrefix}meta:{document.Id}";

        var metadata = new HashEntry[]
        {
            new("id", document.Id.ToString()),
            new("fileName", document.FileName),
            new("contentType", document.ContentType),
            new("fileSize", document.FileSize.ToString(CultureInfo.InvariantCulture)),
            new("uploadedAt", document.UploadedAt.ToString("O")),
            new("uploadedBy", document.UploadedBy),
            new("chunkCount", document.Chunks.Count.ToString(CultureInfo.InvariantCulture))
        };

        // Use pipeline instead of transaction for better performance
        var batch = _database.CreateBatch();
        
        var setTask = batch.StringSetAsync(documentKey, documentJson);
        var pushTask = batch.ListRightPushAsync(_documentsKey, document.Id.ToString());
        var hashTask = batch.HashSetAsync(metadataKey, metadata);

        batch.Execute();

        // Wait for all operations to complete
        await Task.WhenAll(setTask, pushTask, hashTask);

        return document;
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        var documentKey = $"{_documentPrefix}{id}";
        var documentJson = await _database.StringGetAsync(documentKey);

        if (documentJson.IsNull)
            return null;

        try
        {
            return JsonSerializer.Deserialize<Document>(documentJson!);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<Document>> GetAllAsync()
    {
        var documentIds = await _database.ListRangeAsync(_documentsKey);
        var documents = new List<Document>();

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

        return documents;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var documentKey = $"{_documentPrefix}{id}";
        var metadataKey = $"{_documentPrefix}meta:{id}";

        var batch = _database.CreateBatch();
        
        var deleteDocTask = batch.KeyDeleteAsync(documentKey);
        var removeFromListTask = batch.ListRemoveAsync(_documentsKey, id.ToString());
        var deleteMetaTask = batch.KeyDeleteAsync(metadataKey);

        batch.Execute();

        await Task.WhenAll(deleteDocTask, removeFromListTask, deleteMetaTask);

        return true;
    }

    public async Task<int> GetCountAsync()
    {
        var count = await _database.ListLengthAsync(_documentsKey);
        return (int)count;
    }

    public async Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5)
    {
        var normalizedQuery = Extensions.SearchTextExtensions.NormalizeForSearch(query);
        var relevantChunks = new List<DocumentChunk>();

        var documents = await GetAllAsync(); // Fixed: await instead of .Result
        
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
