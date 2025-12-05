using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Interfaces.Storage;
using SmartRAG.Models;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace SmartRAG.Repositories
{
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
                _logger.LogError(ex, "Failed to connect to Redis server at {ConnectionString}", redisConfig.ConnectionString);
                throw;
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
                    return string.Empty;
                }

                return conversationJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history for session {SessionId}", sessionId);
                return string.Empty;
            }
        }

        public async Task AddToConversationAsync(string sessionId, string question, string answer)
        {
            try
            {
                var conversationKey = $"conversation:{sessionId}";

                if (string.IsNullOrEmpty(question))
                {
                    await _database.StringSetAsync(conversationKey, answer);
                    return;
                }

                var existingConversation = await GetConversationHistoryAsync(sessionId);

                var newEntry = string.IsNullOrEmpty(existingConversation)
                    ? $"User: {question}\nAssistant: {answer}"
                    : $"{existingConversation}\nUser: {question}\nAssistant: {answer}";

                await _database.StringSetAsync(conversationKey, newEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to conversation for session {SessionId}", sessionId);
            }
        }

        public async Task ClearConversationAsync(string sessionId)
        {
            try
            {
                var conversationKey = $"conversation:{sessionId}";
                await _database.KeyDeleteAsync(conversationKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing conversation for session {SessionId}", sessionId);
            }
        }

        public async Task<bool> SessionExistsAsync(string sessionId)
        {
            try
            {
                var conversationKey = $"conversation:{sessionId}";
                return await _database.KeyExistsAsync(conversationKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking session existence for {SessionId}", sessionId);
                return false;
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

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _redis?.Close();
                _redis?.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}
