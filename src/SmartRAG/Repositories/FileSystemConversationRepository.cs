
namespace SmartRAG.Repositories;


public class FileSystemConversationRepository : IConversationRepository
{
    private readonly string _conversationsPath;
    private readonly ILogger<FileSystemConversationRepository> _logger;
    private const int MaxConversationLength = 2000;

    public FileSystemConversationRepository(string basePath, ILogger<FileSystemConversationRepository> logger)
    {
        _conversationsPath = Path.Combine(basePath, "Conversations");
        _logger = logger;
        Directory.CreateDirectory(_conversationsPath);
    }

    public async Task<string> GetConversationHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
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

            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation history");
            return string.Empty;
        }
    }

    public async Task AddToConversationAsync(string sessionId, string question, string answer, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return;

        try
        {
            if (string.IsNullOrEmpty(question))
            {
                var sessionFilePath = GetConversationFilePath(sessionId);
                await File.WriteAllTextAsync(sessionFilePath, answer, cancellationToken);
                return;
            }

            var currentHistory = await GetConversationHistoryAsync(sessionId, cancellationToken);
            var newEntry = string.IsNullOrEmpty(currentHistory)
                ? $"User: {question}\nAssistant: {answer}"
                : $"{currentHistory}\nUser: {question}\nAssistant: {answer}";

            if (newEntry.Length > MaxConversationLength)
            {
                newEntry = TruncateConversation(newEntry);
            }

            var filePath = GetConversationFilePath(sessionId);
            await File.WriteAllTextAsync(filePath, newEntry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to conversation");
        }
    }

    public Task ClearConversationAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.CompletedTask;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filePath = GetConversationFilePath(sessionId);
            if (File.Exists(filePath))
                File.Delete(filePath);
            var sourcesPath = GetSourcesFilePath(sessionId);
            if (File.Exists(sourcesPath))
                File.Delete(sourcesPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing conversation");
        }

        return Task.CompletedTask;
    }

    public async Task AppendSourcesForTurnAsync(string sessionId, string sourcesJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = GetSourcesFilePath(sessionId);
            var list = new List<JsonElement>();
            if (File.Exists(path))
            {
                var content = await File.ReadAllTextAsync(path, cancellationToken);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        var existing = JsonSerializer.Deserialize<List<JsonElement>>(content);
                        if (existing != null)
                            list = existing;
                    }
                    catch
                    {
                        list = new List<JsonElement>();
                    }
                }
            }
            list.Add(JsonSerializer.Deserialize<JsonElement>(sourcesJson));
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(list), cancellationToken);
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

        try
        {
            var path = GetSourcesFilePath(sessionId);
            if (!File.Exists(path))
                return string.Empty;
            return await File.ReadAllTextAsync(path, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sources for session");
            return string.Empty;
        }
    }

    public Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.FromResult(false);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filePath = GetConversationFilePath(sessionId);
            return Task.FromResult(File.Exists(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session existence");
            return Task.FromResult(false);
        }
    }

    public async Task SetConversationHistoryAsync(string sessionId, string conversation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return;

        try
        {
            var filePath = GetConversationFilePath(sessionId);
            await File.WriteAllTextAsync(filePath, conversation, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting conversation history");
        }
    }

    public Task ClearAllConversationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Directory.Exists(_conversationsPath))
            {
                foreach (var file in Directory.GetFiles(_conversationsPath, "*.txt"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    File.Delete(file);
                }
                foreach (var file in Directory.GetFiles(_conversationsPath, "*.sources.json"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all conversations from file system");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task<(DateTime? CreatedAt, DateTime? LastUpdated)> GetSessionTimestampsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.FromResult<(DateTime? CreatedAt, DateTime? LastUpdated)>((null, null));

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filePath = GetConversationFilePath(sessionId);
            if (!File.Exists(filePath))
                return Task.FromResult<(DateTime? CreatedAt, DateTime? LastUpdated)>((null, null));

            var info = new FileInfo(filePath);
            return Task.FromResult<(DateTime? CreatedAt, DateTime? LastUpdated)>((info.CreationTimeUtc, info.LastWriteTimeUtc));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session timestamps");
            return Task.FromResult<(DateTime? CreatedAt, DateTime? LastUpdated)>((null, null));
        }
    }

    public Task<string[]> GetAllSessionIdsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(_conversationsPath))
            {
                return Task.FromResult(Array.Empty<string>());
            }

            var files = Directory.GetFiles(_conversationsPath, "*.txt");
            var ids = files
                .Select(Path.GetFileNameWithoutExtension)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToArray()!;

            return Task.FromResult(ids);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing conversation sessions from file system");
            return Task.FromResult(Array.Empty<string>());
        }
    }

    private string GetConversationFilePath(string sessionId)
    {
        return Path.Combine(_conversationsPath, $"{sessionId}.txt");
    }

    private string GetSourcesFilePath(string sessionId)
    {
        return Path.Combine(_conversationsPath, $"{sessionId}.sources.json");
    }

    private static string TruncateConversation(string conversation)
    {
        var lines = conversation.Split('\n');
        if (lines.Length <= 6)
            return conversation;

        return string.Join("\n", lines.Skip(lines.Length - 6));
    }
}

