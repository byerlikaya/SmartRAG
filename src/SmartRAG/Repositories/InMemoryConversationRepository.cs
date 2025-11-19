using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.Storage;
using SmartRAG.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Repositories
{
    public class InMemoryConversationRepository : IConversationRepository
    {
        private readonly Dictionary<string, string> _conversations = new Dictionary<string, string>();
        private readonly object _lock = new object();
        private readonly ILogger<InMemoryConversationRepository> _logger;
        
        // Default constants if config is not available or for simplicity
        private const int MaxConversationLength = 2000;
        private const int MaxSessions = 1000;

        public InMemoryConversationRepository(ILogger<InMemoryConversationRepository> logger)
        {
            _logger = logger;
        }

        public Task<string> GetConversationHistoryAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Task.FromResult(string.Empty);

            lock (_lock)
            {
                return Task.FromResult(_conversations.TryGetValue(sessionId, out var history) ? history : string.Empty);
            }
        }

        public Task AddToConversationAsync(string sessionId, string question, string answer)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Task.CompletedTask;

            lock (_lock)
            {
                // Clean up old sessions if we have too many
                if (_conversations.Count >= MaxSessions)
                {
                    CleanupOldSessions();
                }

                // If question is empty, this is a special case (like session-id storage)
                if (string.IsNullOrEmpty(question))
                {
                    _conversations[sessionId] = answer;
                    return Task.CompletedTask;
                }

                var currentHistory = _conversations.TryGetValue(sessionId, out var existing) ? existing : string.Empty;
                var newEntry = string.IsNullOrEmpty(currentHistory)
                    ? $"User: {question}\nAssistant: {answer}"
                    : $"{currentHistory}\nUser: {question}\nAssistant: {answer}";

                // Limit conversation length to prevent memory issues
                if (newEntry.Length > MaxConversationLength)
                {
                    newEntry = TruncateConversation(newEntry);
                }

                _conversations[sessionId] = newEntry;
            }

            return Task.CompletedTask;
        }

        public Task ClearConversationAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Task.CompletedTask;

            lock (_lock)
            {
                _conversations.Remove(sessionId);
            }

            return Task.CompletedTask;
        }

        public Task<bool> SessionExistsAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Task.FromResult(false);

            lock (_lock)
            {
                return Task.FromResult(_conversations.ContainsKey(sessionId));
            }
        }

        private void CleanupOldSessions()
        {
            // Simple cleanup: remove oldest sessions
            // Since Dictionary doesn't maintain order, we just remove arbitrary ones or we could track timestamps
            // For simplicity matching the original implementation which likely just removed some
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
}
