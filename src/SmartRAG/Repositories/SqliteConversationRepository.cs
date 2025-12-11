using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Interfaces.Storage;
using SmartRAG.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Repositories
{
    public class SqliteConversationRepository : IConversationRepository, IDisposable
    {
        private readonly string _connectionString;
        private readonly ILogger<SqliteConversationRepository> _logger;
        private SqliteConnection _connection;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);
        private bool _initialized = false;

        private const int MaxConversationLength = 2000;

        public SqliteConversationRepository(IOptions<SqliteConfig> config, ILogger<SqliteConversationRepository> logger)
        {
            var sqliteConfig = config.Value;
            _connectionString = $"Data Source={sqliteConfig.DatabasePath};";
            _logger = logger;
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
                return;

            await _initSemaphore.WaitAsync();
            try
            {
                if (_initialized)
                    return;

                if (_connection == null)
                {
                    _connection = new SqliteConnection(_connectionString);
                    await _connection.OpenAsync();

                    var command = _connection.CreateCommand();
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Conversations (
                            SessionId TEXT PRIMARY KEY,
                            History TEXT,
                            LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
                        );";
                    await command.ExecuteNonQueryAsync();
                }

                _initialized = true;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        public async Task<string> GetConversationHistoryAsync(string sessionId)
        {
            await EnsureInitializedAsync();
            await _semaphore.WaitAsync();
            try
            {
                var command = _connection.CreateCommand();
                command.CommandText = "SELECT History FROM Conversations WHERE SessionId = @sessionId";
                command.Parameters.AddWithValue("@sessionId", sessionId);

                var result = await command.ExecuteScalarAsync();
                return result?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history for session {SessionId}", sessionId);
                return string.Empty;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task AddToConversationAsync(string sessionId, string question, string answer)
        {
            await EnsureInitializedAsync();
            await _semaphore.WaitAsync();
            try
            {
                if (string.IsNullOrEmpty(question))
                {
                    var updateCmd = _connection.CreateCommand();
                    updateCmd.CommandText = @"
                        INSERT INTO Conversations (SessionId, History, LastUpdated) 
                        VALUES (@sessionId, @history, CURRENT_TIMESTAMP)
                        ON CONFLICT(SessionId) DO UPDATE SET History = @history, LastUpdated = CURRENT_TIMESTAMP";
                    updateCmd.Parameters.AddWithValue("@sessionId", sessionId);
                    updateCmd.Parameters.AddWithValue("@history", answer);
                    await updateCmd.ExecuteNonQueryAsync();
                    return;
                }

                var commandHistory = _connection.CreateCommand();
                commandHistory.CommandText = "SELECT History FROM Conversations WHERE SessionId = @sessionId";
                commandHistory.Parameters.AddWithValue("@sessionId", sessionId);
                var result = await commandHistory.ExecuteScalarAsync();
                var currentHistory = result?.ToString() ?? string.Empty;

                var newEntry = string.IsNullOrEmpty(currentHistory)
                    ? $"User: {question}\nAssistant: {answer}"
                    : $"{currentHistory}\nUser: {question}\nAssistant: {answer}";

                if (newEntry.Length > MaxConversationLength)
                {
                    newEntry = TruncateConversation(newEntry);
                }

                var command = _connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Conversations (SessionId, History, LastUpdated) 
                    VALUES (@sessionId, @history, CURRENT_TIMESTAMP)
                    ON CONFLICT(SessionId) DO UPDATE SET History = @history, LastUpdated = CURRENT_TIMESTAMP";
                command.Parameters.AddWithValue("@sessionId", sessionId);
                command.Parameters.AddWithValue("@history", newEntry);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to conversation for session {SessionId}", sessionId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ClearConversationAsync(string sessionId)
        {
            await EnsureInitializedAsync();
            await _semaphore.WaitAsync();
            try
            {
                var command = _connection.CreateCommand();
                command.CommandText = "DELETE FROM Conversations WHERE SessionId = @sessionId";
                command.Parameters.AddWithValue("@sessionId", sessionId);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing conversation for session {SessionId}", sessionId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> SessionExistsAsync(string sessionId)
        {
            await EnsureInitializedAsync();
            await _semaphore.WaitAsync();
            try
            {
                var command = _connection.CreateCommand();
                command.CommandText = "SELECT COUNT(1) FROM Conversations WHERE SessionId = @sessionId";
                command.Parameters.AddWithValue("@sessionId", sessionId);
                var result = await command.ExecuteScalarAsync();
                var count = Convert.ToInt32(result);
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking session existence for {SessionId}", sessionId);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task SetConversationHistoryAsync(string sessionId, string conversation)
        {
            await EnsureInitializedAsync();
            await _semaphore.WaitAsync();
            try
            {
                var command = _connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Conversations (SessionId, History, LastUpdated) 
                    VALUES (@sessionId, @history, CURRENT_TIMESTAMP)
                    ON CONFLICT(SessionId) DO UPDATE SET History = @history, LastUpdated = CURRENT_TIMESTAMP";
                command.Parameters.AddWithValue("@sessionId", sessionId);
                command.Parameters.AddWithValue("@history", conversation);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting conversation history for session {SessionId}", sessionId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ClearAllConversationsAsync()
        {
            await EnsureInitializedAsync();
            await _semaphore.WaitAsync();
            try
            {
                var command = _connection.CreateCommand();
                command.CommandText = "DELETE FROM Conversations";
                await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Cleared all conversations from SQLite");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all conversations from SQLite");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static string TruncateConversation(string conversation)
        {
            var lines = conversation.Split('\n');
            if (lines.Length <= 6)
                return conversation;

            return string.Join("\n", lines.Skip(lines.Length - 6));
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
            _initSemaphore?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
