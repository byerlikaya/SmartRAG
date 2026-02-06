using Microsoft.Data.Sqlite;

namespace SmartRAG.Repositories;


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
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE TABLE IF NOT EXISTS ConversationSources (
                        SessionId TEXT PRIMARY KEY,
                        SourcesJson TEXT
                    );";
                await command.ExecuteNonQueryAsync();

                try
                {
                    var alterCmd = _connection.CreateCommand();
                    alterCmd.CommandText = "ALTER TABLE Conversations ADD COLUMN CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP";
                    await alterCmd.ExecuteNonQueryAsync();
                    var backfillCmd = _connection.CreateCommand();
                    backfillCmd.CommandText = "UPDATE Conversations SET CreatedAt = LastUpdated WHERE CreatedAt IS NULL";
                    await backfillCmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    /* Column may already exist */
                }
            }

            _initialized = true;
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    public async Task<string> GetConversationHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT History FROM Conversations WHERE SessionId = @sessionId";
            command.Parameters.AddWithValue("@sessionId", sessionId);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation history");
            return string.Empty;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AddToConversationAsync(string sessionId, string question, string answer, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (string.IsNullOrEmpty(question))
            {
                var updateCmd = _connection.CreateCommand();
                updateCmd.CommandText = @"
                    INSERT INTO Conversations (SessionId, History, CreatedAt, LastUpdated) 
                    VALUES (@sessionId, @history, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                    ON CONFLICT(SessionId) DO UPDATE SET History = @history, LastUpdated = CURRENT_TIMESTAMP";
                updateCmd.Parameters.AddWithValue("@sessionId", sessionId);
                updateCmd.Parameters.AddWithValue("@history", answer);
                await updateCmd.ExecuteNonQueryAsync(cancellationToken);
                return;
            }

            var commandHistory = _connection.CreateCommand();
            commandHistory.CommandText = "SELECT History FROM Conversations WHERE SessionId = @sessionId";
            commandHistory.Parameters.AddWithValue("@sessionId", sessionId);
            var result = await commandHistory.ExecuteScalarAsync(cancellationToken);
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
                INSERT INTO Conversations (SessionId, History, CreatedAt, LastUpdated) 
                VALUES (@sessionId, @history, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                ON CONFLICT(SessionId) DO UPDATE SET History = @history, LastUpdated = CURRENT_TIMESTAMP";
            command.Parameters.AddWithValue("@sessionId", sessionId);
            command.Parameters.AddWithValue("@history", newEntry);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to conversation");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearConversationAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Conversations WHERE SessionId = @sessionId";
            cmd.Parameters.AddWithValue("@sessionId", sessionId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM ConversationSources WHERE SessionId = @sessionId";
            cmd.Parameters.AddWithValue("@sessionId", sessionId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing conversation");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AppendSourcesForTurnAsync(string sessionId, string sourcesJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return;
        await EnsureInitializedAsync();
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = "SELECT SourcesJson FROM ConversationSources WHERE SessionId = @sessionId";
            selectCmd.Parameters.AddWithValue("@sessionId", sessionId);
            var existing = await selectCmd.ExecuteScalarAsync(cancellationToken) as string;
            var list = new List<JsonElement>();
            if (!string.IsNullOrWhiteSpace(existing))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<JsonElement>>(existing);
                    if (parsed != null)
                        list = parsed;
                }
                catch
                {
                    list = new List<JsonElement>();
                }
            }
            list.Add(JsonSerializer.Deserialize<JsonElement>(sourcesJson));
            var newJson = JsonSerializer.Serialize(list);
            var upsertCmd = _connection.CreateCommand();
            upsertCmd.CommandText = @"
                INSERT INTO ConversationSources (SessionId, SourcesJson) VALUES (@sessionId, @json)
                ON CONFLICT(SessionId) DO UPDATE SET SourcesJson = @json";
            upsertCmd.Parameters.AddWithValue("@sessionId", sessionId);
            upsertCmd.Parameters.AddWithValue("@json", newJson);
            await upsertCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error appending sources for turn");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string> GetSourcesForSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return string.Empty;
        await EnsureInitializedAsync();
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT SourcesJson FROM ConversationSources WHERE SessionId = @sessionId";
            cmd.Parameters.AddWithValue("@sessionId", sessionId);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sources for session");
            return string.Empty;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM Conversations WHERE SessionId = @sessionId";
            command.Parameters.AddWithValue("@sessionId", sessionId);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var count = Convert.ToInt32(result);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session existence");
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetConversationHistoryAsync(string sessionId, string conversation, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Conversations (SessionId, History, CreatedAt, LastUpdated) 
                VALUES (@sessionId, @history, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                ON CONFLICT(SessionId) DO UPDATE SET History = @history, LastUpdated = CURRENT_TIMESTAMP";
            command.Parameters.AddWithValue("@sessionId", sessionId);
            command.Parameters.AddWithValue("@history", conversation);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting conversation history");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearAllConversationsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Conversations";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM ConversationSources";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
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

    public async Task<(DateTime? CreatedAt, DateTime? LastUpdated)> GetSessionTimestampsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT CreatedAt, LastUpdated FROM Conversations WHERE SessionId = @sessionId";
            command.Parameters.AddWithValue("@sessionId", sessionId);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var createdAt = reader.IsDBNull(0) ? (DateTime?)null : reader.GetDateTime(0);
                var lastUpdated = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
                return (createdAt, lastUpdated);
            }
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session timestamps");
            return (null, null);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string[]> GetAllSessionIdsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT SessionId FROM Conversations ORDER BY LastUpdated DESC";
            var result = await command.ExecuteReaderAsync(cancellationToken);

            var ids = new System.Collections.Generic.List<string>();
            while (await result.ReadAsync(cancellationToken))
            {
                if (!result.IsDBNull(0))
                {
                    ids.Add(result.GetString(0));
                }
            }

            return ids.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing conversation sessions from SQLite");
            return Array.Empty<string>();
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

