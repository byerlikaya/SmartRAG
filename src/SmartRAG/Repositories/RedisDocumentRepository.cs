using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace SmartRAG.Repositories;

/// <summary>
/// Redis document repository implementation
/// </summary>
public class RedisDocumentRepository : IDocumentRepository
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly string _documentsKey;
    private readonly string _documentPrefix;

    public RedisDocumentRepository(RedisConfig config)
    {
        // Configure connection options
        var options = new ConfigurationOptions
        {
            EndPoints = { config.ConnectionString },
            ConnectTimeout = config.ConnectionTimeout * 1000,
            SyncTimeout = config.ConnectionTimeout * 1000,
            ConnectRetry = config.RetryCount,
            ReconnectRetryPolicy = new ExponentialRetry(config.RetryDelay),
            AllowAdmin = true,
            AbortOnConnectFail = false
        };

        // Add authentication if provided
        if (!string.IsNullOrEmpty(config.Username))
        {
            options.User = config.Username;
        }

        if (!string.IsNullOrEmpty(config.Password))
        {
            options.Password = config.Password;
        }

        // Enable SSL if configured
        if (config.EnableSsl)
        {
            options.Ssl = true;
            options.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        }

        _redis = ConnectionMultiplexer.Connect(options);
        _database = _redis.GetDatabase(config.Database);
        _documentsKey = $"{config.KeyPrefix}list";
        _documentPrefix = config.KeyPrefix;

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

        var transaction = _database.CreateTransaction();

        var setTask = transaction.StringSetAsync(documentKey, documentJson);

        var pushTask = transaction.ListRightPushAsync(_documentsKey, document.Id.ToString());

        var metadataKey = $"{_documentPrefix}meta:{document.Id}";

        var metadata = new HashEntry[]
        {
            new("id", document.Id.ToString()),
            new("fileName", document.FileName),
            new("contentType", document.ContentType),
            new("fileSize", document.FileSize.ToString()),
            new("uploadedAt", document.UploadedAt.ToString("O")),
            new("uploadedBy", document.UploadedBy),
            new("chunkCount", document.Chunks.Count.ToString())
        };

        var hashTask = transaction.HashSetAsync(metadataKey, metadata);

        var result = await transaction.ExecuteAsync();

        if (!result)
        {
            throw new InvalidOperationException("Failed to add document to Redis");
        }

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
        var transaction = _database.CreateTransaction();

        var documentKey = $"{_documentPrefix}{id}";

        var deleteDocTask = transaction.KeyDeleteAsync(documentKey);

        var removeFromListTask = transaction.ListRemoveAsync(_documentsKey, id.ToString());

        var metadataKey = $"{_documentPrefix}meta:{id}";
        var deleteMetaTask = transaction.KeyDeleteAsync(metadataKey);

        var result = await transaction.ExecuteAsync();

        await Task.WhenAll(deleteDocTask, removeFromListTask, deleteMetaTask);

        return result;
    }

    public async Task<int> GetCountAsync()
    {
        var count = await _database.ListLengthAsync(_documentsKey);
        return (int)count;
    }

    public Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5)
    {

        var normalizedQuery = Extensions.SearchTextExtensions.NormalizeForSearch(query);
        var relevantChunks = new List<DocumentChunk>();

        var documents = GetAllAsync().Result;
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

        return Task.FromResult(relevantChunks);
    }
}
