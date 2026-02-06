using StackExchange.Redis;

namespace SmartRAG.Repositories;


public class RedisConversationRepository : IConversationRepository, IDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisConversationRepository> _logger;
    private bool _disposed;

    private const int DefaultConnectionTimeoutMs = 1000;
    private const int DefaultKeepAliveSeconds = 180;

    public RedisConversationRepository(IOptions<RedisConfig> config, ILogger<RedisConversationRepository> logger)
    {
        var redisConfig = config.Value;
        _logger = logger;

        var options = CreateConnectionOptions(redisConfig);
        ConfigureAuthentication(options, redisConfig);
        ConfigureSsl(options, redisConfig);

        try
        {
            _redis = ConnectionMultiplexer.Connect(options);
            _database = _redis.GetDatabase(redisConfig.Database);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Redis server");
            throw;
        }
    }

    public async Task<string> GetConversationHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversationKey = $"conversation:{sessionId}";
            var conversationJson = await _database.StringGetAsync(conversationKey);

            if (conversationJson.IsNull)
            {
                return string.Empty;
            }

            return conversationJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation history");
            return string.Empty;
        }
    }

    public async Task AddToConversationAsync(string sessionId, string question, string answer, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversationKey = $"conversation:{sessionId}";
            var metaKey = $"conversation:meta:{sessionId}";
            var now = DateTime.UtcNow.ToString("o");

            if (string.IsNullOrEmpty(question))
            {
                await _database.StringSetAsync(conversationKey, answer);
                await _database.HashSetAsync(metaKey, new[] { new HashEntry("CreatedAt", now), new HashEntry("LastUpdated", now) });
                return;
            }

            var existingConversation = await GetConversationHistoryAsync(sessionId, cancellationToken);
            var isNew = string.IsNullOrEmpty(existingConversation);

            var newEntry = isNew
                ? $"User: {question}\nAssistant: {answer}"
                : $"{existingConversation}\nUser: {question}\nAssistant: {answer}";

            await _database.StringSetAsync(conversationKey, newEntry);
            if (isNew)
                await _database.HashSetAsync(metaKey, new[] { new HashEntry("CreatedAt", now), new HashEntry("LastUpdated", now) });
            else
                await _database.HashSetAsync(metaKey, "LastUpdated", now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to conversation");
        }
    }

    public async Task SetConversationHistoryAsync(string sessionId, string conversation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var conversationKey = $"conversation:{sessionId}";
            var metaKey = $"conversation:meta:{sessionId}";
            var now = DateTime.UtcNow.ToString("o");
            var metaExists = await _database.KeyExistsAsync(metaKey);
            await _database.StringSetAsync(conversationKey, conversation);
            if (metaExists)
                await _database.HashSetAsync(metaKey, "LastUpdated", now);
            else
                await _database.HashSetAsync(metaKey, new[] { new HashEntry("CreatedAt", now), new HashEntry("LastUpdated", now) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting conversation history");
        }
    }


    public async Task ClearConversationAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _database.KeyDeleteAsync($"conversation:{sessionId}");
            await _database.KeyDeleteAsync($"conversation:sources:{sessionId}");
            await _database.KeyDeleteAsync($"conversation:meta:{sessionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing conversation");
        }
    }

    public async Task AppendSourcesForTurnAsync(string sessionId, string sourcesJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return;
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var key = $"conversation:sources:{sessionId}";
            var existing = await _database.StringGetAsync(key);
            var list = new List<JsonElement>();
            if (existing.HasValue && !existing.IsNullOrEmpty)
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<JsonElement>>(existing!);
                    if (parsed != null)
                        list = parsed;
                }
                catch
                {
                    list = new List<JsonElement>();
                }
            }
            list.Add(JsonSerializer.Deserialize<JsonElement>(sourcesJson));
            await _database.StringSetAsync(key, JsonSerializer.Serialize(list));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error appending sources for turn");
        }
    }

    public async Task<string> GetSourcesForSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return null;
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var key = $"conversation:sources:{sessionId}";
            var value = await _database.StringGetAsync(key);
            return value.IsNullOrEmpty ? string.Empty : value.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sources for session");
            return null;
        }
    }

    public async Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var conversationKey = $"conversation:{sessionId}";
            return await _database.KeyExistsAsync(conversationKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session existence");
            return false;
        }
    }

    public async Task ClearAllConversationsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var endpoints = _redis.GetEndPoints();
            if (endpoints == null || endpoints.Length == 0)
            {
                _logger.LogWarning("No Redis endpoints available for clearing conversations");
                return;
            }

            var server = _redis.GetServer(endpoints.First());
            var pattern = "conversation:*";

            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _database.KeyDeleteAsync(key);
            }

            _logger.LogInformation("Cleared all conversation history from Redis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all conversations from Redis");
            throw;
        }
    }

    public async Task<(DateTime? CreatedAt, DateTime? LastUpdated)> GetSessionTimestampsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return (null, null);

        try
        {
            var metaKey = $"conversation:meta:{sessionId}";
            var entries = await _database.HashGetAllAsync(metaKey);
            if (entries == null || entries.Length == 0)
                return (null, null);

            DateTime? createdAt = null;
            DateTime? lastUpdated = null;
            foreach (var e in entries)
            {
                var val = e.Value.ToString();
                if (string.IsNullOrEmpty(val)) continue;
                if (string.Equals(e.Name, "CreatedAt", StringComparison.OrdinalIgnoreCase) && DateTime.TryParse(val, null, System.Globalization.DateTimeStyles.RoundtripKind, out var ca))
                    createdAt = ca;
                else if (string.Equals(e.Name, "LastUpdated", StringComparison.OrdinalIgnoreCase) && DateTime.TryParse(val, null, System.Globalization.DateTimeStyles.RoundtripKind, out var lu))
                    lastUpdated = lu;
            }
            return (createdAt, lastUpdated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session timestamps");
            return (null, null);
        }
    }

    public async Task<string[]> GetAllSessionIdsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            if (endpoints == null || endpoints.Length == 0)
            {
                _logger.LogWarning("No Redis endpoints available for listing conversations");
                return Array.Empty<string>();
            }

            var server = _redis.GetServer(endpoints.First());
            var pattern = "conversation:*";
            var ids = new System.Collections.Generic.List<string>();

            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var keyStr = (string)key;
                if (keyStr.StartsWith("conversation:sources:", StringComparison.Ordinal) || keyStr.StartsWith("conversation:meta:", StringComparison.Ordinal))
                    continue;
                if (keyStr.StartsWith("conversation:", StringComparison.Ordinal))
                {
                    ids.Add(keyStr.Substring("conversation:".Length));
                }
            }

            return ids.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing conversation sessions from Redis");
            return Array.Empty<string>();
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
        if (config.EnableSsl)
        {
            options.Ssl = true;
            options.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        }
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
}

