using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Repositories;


public class InMemoryConversationRepository : IConversationRepository
{
    private readonly Dictionary<string, string> _conversations = new Dictionary<string, string>();
    private readonly Dictionary<string, List<string>> _sourcesBySession = new Dictionary<string, List<string>>();
    private readonly object _lock = new object();

    private const int MaxConversationLength = 2000;
    private const int MaxSessions = 1000;

    public InMemoryConversationRepository(ILogger<InMemoryConversationRepository> logger)
    {
    }

    public Task<string> GetConversationHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.FromResult(string.Empty);

        lock (_lock)
        {
            return Task.FromResult(_conversations.TryGetValue(sessionId, out var history) ? history : string.Empty);
        }
    }

    public Task AddToConversationAsync(string sessionId, string question, string answer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.CompletedTask;

        lock (_lock)
        {
            if (_conversations.Count >= MaxSessions)
            {
                CleanupOldSessions();
            }

            if (string.IsNullOrEmpty(question))
            {
                _conversations[sessionId] = answer;
                return Task.CompletedTask;
            }

            var currentHistory = _conversations.TryGetValue(sessionId, out var existing) ? existing : string.Empty;
            var newEntry = string.IsNullOrEmpty(currentHistory)
                ? $"User: {question}\nAssistant: {answer}"
                : $"{currentHistory}\nUser: {question}\nAssistant: {answer}";

            if (newEntry.Length > MaxConversationLength)
            {
                newEntry = TruncateConversation(newEntry);
            }

            _conversations[sessionId] = newEntry;
        }

        return Task.CompletedTask;
    }

    public Task ClearConversationAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.CompletedTask;

        lock (_lock)
        {
            _conversations.Remove(sessionId);
            _sourcesBySession.Remove(sessionId);
        }

        return Task.CompletedTask;
    }

    public Task AppendSourcesForTurnAsync(string sessionId, string sourcesJson, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.CompletedTask;

        lock (_lock)
        {
            if (!_sourcesBySession.TryGetValue(sessionId, out var list))
            {
                list = new List<string>();
                _sourcesBySession[sessionId] = list;
            }
            list.Add(sourcesJson);
        }

        return Task.CompletedTask;
    }

    public Task<string> GetSourcesForSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.FromResult(string.Empty);

        lock (_lock)
        {
            if (!_sourcesBySession.TryGetValue(sessionId, out var list) || list.Count == 0)
                return Task.FromResult(string.Empty);
            var arrayOfArrays = list.Select(s => JsonSerializer.Deserialize<JsonElement>(s)).ToList();
            return Task.FromResult(JsonSerializer.Serialize(arrayOfArrays));
        }
    }

    public Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.FromResult(false);

        lock (_lock)
        {
            return Task.FromResult(_conversations.ContainsKey(sessionId));
        }
    }

    public Task SetConversationHistoryAsync(string sessionId, string conversation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.CompletedTask;

        lock (_lock)
        {
            _conversations[sessionId] = conversation;
        }

        return Task.CompletedTask;
    }

    public Task ClearAllConversationsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            _conversations.Clear();
            _sourcesBySession.Clear();
        }
        return Task.CompletedTask;
    }

    public Task<string[]> GetAllSessionIdsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(_conversations.Keys.ToArray());
        }
    }

    private void CleanupOldSessions()
    {
        var sessionsToRemove = _conversations.Count - MaxSessions + 100;
        var keysToRemove = _conversations.Keys.Take(sessionsToRemove).ToList();

        foreach (var key in keysToRemove)
        {
            _conversations.Remove(key);
        }
    }

    private static string TruncateConversation(string conversation)
    {
        var lines = conversation.Split('\n');
        if (lines.Length <= 6)
            return conversation;

        return string.Join("\n", lines.Skip(lines.Length - 6));
    }
}

