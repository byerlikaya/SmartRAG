using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Interfaces.Storage;
using SmartRAG.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Repositories
{
    public class SqliteConversationRepository : IConversationRepository, IDisposable
    {
        private readonly string _connectionString;
        private readonly ILogger<SqliteConversationRepository> _logger;
        private SqliteConnection _connection;
        private readonly object _lock = new object();

        private const int MaxConversationLength = 2000;

        public SqliteConversationRepository(IOptions<SqliteConfig> config, ILogger<SqliteConversationRepository> logger)
        {
            var sqliteConfig = config.Value;
            _connectionString = $"Data Source={sqliteConfig.DatabasePath};";
            _logger = logger;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            lock (_lock)
            {
                if (_connection == null)
                {
                    _connection = new SqliteConnection(_connectionString);
                    _connection.Open();

                    var command = _connection.CreateCommand();
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Conversations (
                            SessionId TEXT PRIMARY KEY,
                            History TEXT,
                            LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
                        );";
                    command.ExecuteNonQuery();
                }
            }
        }

        public Task<string> GetConversationHistoryAsync(string sessionId)
        {
            lock (_lock)
            {
                try
                {
                    var command = _connection.CreateCommand();
                    command.CommandText = "SELECT History FROM Conversations WHERE SessionId = @sessionId";
                    command.Parameters.AddWithValue("@sessionId", sessionId);

                    var result = command.ExecuteScalar();
                    return Task.FromResult(result?.ToString() ?? string.Empty);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting conversation history for session {SessionId}", sessionId);
                    return Task.FromResult(string.Empty);
                }
            }
        }

        public Task AddToConversationAsync(string sessionId, string question, string answer)
        {
            lock (_lock)
            {
                try
                {
                    // If question is empty, this is a special case (like session-id storage)
                    if (string.IsNullOrEmpty(question))
                    {
                        var updateCmd = _connection.CreateCommand();
                        updateCmd.CommandText = @"
                            INSERT INTO Conversations (SessionId, History, LastUpdated) 
                            VALUES (@sessionId, @history, CURRENT_TIMESTAMP)
                            ON CONFLICT(SessionId) DO UPDATE SET History = @history, LastUpdated = CURRENT_TIMESTAMP";
                        updateCmd.Parameters.AddWithValue("@sessionId", sessionId);
                        updateCmd.Parameters.AddWithValue("@history", answer);
                        updateCmd.ExecuteNonQuery();
                        return Task.CompletedTask;
                    }

                    var currentHistory = GetConversationHistoryAsync(sessionId).Result;
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
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding to conversation for session {SessionId}", sessionId);
                }
                return Task.CompletedTask;
            }
        }

        public Task ClearConversationAsync(string sessionId)
        {
            lock (_lock)
            {
                try
                {
                    var command = _connection.CreateCommand();
                    command.CommandText = "DELETE FROM Conversations WHERE SessionId = @sessionId";
                    command.Parameters.AddWithValue("@sessionId", sessionId);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error clearing conversation for session {SessionId}", sessionId);
                }
                return Task.CompletedTask;
            }
        }

        public Task<bool> SessionExistsAsync(string sessionId)
        {
            lock (_lock)
            {
                try
                {
                    var command = _connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(1) FROM Conversations WHERE SessionId = @sessionId";
                    command.Parameters.AddWithValue("@sessionId", sessionId);
                    var count = Convert.ToInt32(command.ExecuteScalar());
                    return Task.FromResult(count > 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking session existence for {SessionId}", sessionId);
                    return Task.FromResult(false);
                }
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
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
