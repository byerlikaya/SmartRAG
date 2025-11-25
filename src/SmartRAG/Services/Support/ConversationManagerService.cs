#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Enums;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Storage;
using SmartRAG.Interfaces.Support;
using SmartRAG.Models;
using SmartRAG.Services.Shared;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartRAG.Services.Support
{
    /// <summary>
    /// Service for managing conversation sessions and history
    /// </summary>
    public class ConversationManagerService : IConversationManagerService
    {
        private const string PersistentSessionKey = "smartrag-current-session";

        private readonly IConversationRepository _conversationRepository;
        private readonly SmartRagOptions _options;
        private readonly ILogger<ConversationManagerService> _logger;

        private readonly ConcurrentDictionary<string, string> _conversationCache = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the ConversationManagerService
        /// </summary>
        /// <param name="conversationRepository">Repository for conversation operations</param>
        /// <param name="options">SmartRAG configuration options</param>
        /// <param name="logger">Logger instance for this service</param>
        public ConversationManagerService(
            IConversationRepository conversationRepository,
            IOptions<SmartRagOptions> options,
            ILogger<ConversationManagerService> logger)
        {
            _conversationRepository = conversationRepository;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Gets or creates a session ID automatically for conversation continuity
        /// Uses a persistent session key that survives application restarts
        /// </summary>
        public async Task<string> GetOrCreateSessionIdAsync()
        {
            try
            {
                var existingSessionData = await _conversationRepository.GetConversationHistoryAsync(PersistentSessionKey);
                if (!string.IsNullOrEmpty(existingSessionData))
                {
                    var lines = existingSessionData.Split('\n');
                    var sessionLine = lines.FirstOrDefault(l => l.StartsWith("session-id:"));
                    if (sessionLine != null)
                    {
                        var sessionId = sessionLine.Substring("session-id:".Length).Trim();

                        var sessionExists = await _conversationRepository.SessionExistsAsync(sessionId);
                        if (sessionExists)
                        {
                            var conversationHistory = await _conversationRepository.GetConversationHistoryAsync(sessionId);

                            _conversationCache.TryAdd(sessionId, conversationHistory ?? string.Empty);
                            ServiceLogMessages.LogSessionRetrieved(_logger, sessionId, null);
                            return sessionId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationRetrievalFailed(_logger, PersistentSessionKey, ex);
            }

            return await CreateAndPersistSessionAsync();
        }

        /// <summary>
        /// Starts a new conversation session
        /// </summary>
        public async Task<string> StartNewConversationAsync()
        {
            try
            {
                var currentSession = _conversationCache.Keys.FirstOrDefault();
                if (!string.IsNullOrEmpty(currentSession))
                {
                    _conversationCache.TryRemove(currentSession, out _);
                }

                try
                {
                    await _conversationRepository.ClearConversationAsync(PersistentSessionKey);
                }
                catch (Exception ex)
                {
                    ServiceLogMessages.LogConversationStorageFailed(_logger, PersistentSessionKey, ex);
                }

                return await CreateAndPersistSessionAsync();
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationStorageFailed(_logger, "new-session", ex);
                return CreateFallbackSession();
            }
        }

        /// <summary>
        /// Gets conversation history for a session using existing storage provider
        /// </summary>
        public async Task<string> GetConversationHistoryAsync(string sessionId)
        {
            try
            {
                var history = await GetConversationFromStorageAsync(sessionId);

                _conversationCache.AddOrUpdate(sessionId, history, (key, oldValue) => history);

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history for session {SessionId}", sessionId);
                return string.Empty;
            }
        }

        /// <summary>
        /// Adds a conversation turn to the session
        /// </summary>
        public async Task AddToConversationAsync(string sessionId, string question, string answer)
        {
            try
            {
                var currentHistory = await GetConversationFromStorageAsync(sessionId);

                var newEntry = string.IsNullOrEmpty(currentHistory)
                    ? $"User: {question}\nAssistant: {answer}"
                    : $"{currentHistory}\nUser: {question}\nAssistant: {answer}";


                await StoreConversationToStorageAsync(sessionId, newEntry);

                _conversationCache.AddOrUpdate(sessionId, newEntry, (key, oldValue) => newEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to conversation for session {SessionId}", sessionId);
            }
        }

        /// <summary>
        /// Truncates conversation history to keep only the most recent turns
        /// </summary>
        public string TruncateConversationHistory(string history, int maxTurns = 3)
        {
            if (string.IsNullOrWhiteSpace(history))
            {
                return string.Empty;
            }

            var lines = history.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var turns = new System.Collections.Generic.List<string>();
            var currentTurn = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("User:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Assistant:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("A:", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentTurn.Length > 0)
                    {
                        turns.Add(currentTurn.ToString());
                        currentTurn.Clear();
                    }
                }

                currentTurn.AppendLine(trimmed);
            }

            if (currentTurn.Length > 0)
            {
                turns.Add(currentTurn.ToString());
            }

            var recentTurns = turns.TakeLast(maxTurns * 2).ToList();

            if (recentTurns.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("\n", recentTurns);
        }

        /// <summary>
        /// Creates a new session ID and persists it to storage and cache
        /// </summary>
        private async Task<string> CreateAndPersistSessionAsync()
        {
            var newSessionId = GenerateSessionId();

            try
            {
                await _conversationRepository.AddToConversationAsync(PersistentSessionKey, "", $"session-id:{newSessionId}");
                await _conversationRepository.AddToConversationAsync(newSessionId, "", "");

                _conversationCache.TryAdd(newSessionId, string.Empty);

                ServiceLogMessages.LogSessionCreated(_logger, newSessionId, null);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationStorageFailed(_logger, newSessionId, ex);
            }

            return newSessionId;
        }

        /// <summary>
        /// Creates a fallback session ID without persistence (cache only)
        /// </summary>
        private string CreateFallbackSession()
        {
            var fallbackSessionId = GenerateSessionId();
            _conversationCache.TryAdd(fallbackSessionId, string.Empty);
            return fallbackSessionId;
        }

        /// <summary>
        /// Generates a new session ID
        /// </summary>
        private static string GenerateSessionId()
        {
            return $"session-{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Gets the conversation storage provider (uses ConversationStorageProvider if specified, otherwise StorageProvider with fallback)
        /// </summary>
        private StorageProvider GetConversationStorageProvider()
        {
            if (_options.ConversationStorageProvider.HasValue)
            {
                switch (_options.ConversationStorageProvider.Value)
                {
                    case ConversationStorageProvider.Redis:
                        return StorageProvider.Redis;
                    case ConversationStorageProvider.SQLite:
                        return StorageProvider.SQLite;
                    case ConversationStorageProvider.FileSystem:
                        return StorageProvider.FileSystem;
                    case ConversationStorageProvider.InMemory:
                        return StorageProvider.InMemory;
                    default:
                        return StorageProvider.InMemory; // Fallback
                }
            }

            switch (_options.StorageProvider)
            {
                case StorageProvider.Qdrant:
                    return StorageProvider.InMemory; // Fallback for Qdrant
                default:
                    return _options.StorageProvider;
            }
        }

        /// <summary>
        /// Checks if the storage provider supports conversation history
        /// </summary>
        private static bool SupportsConversationHistory(StorageProvider provider)
        {
            return provider switch
            {
                StorageProvider.Redis => true,
                StorageProvider.SQLite => true,
                StorageProvider.InMemory => true,
                StorageProvider.FileSystem => true,
                StorageProvider.Qdrant => false,
                _ => false
            };
        }

        /// <summary>
        /// Get conversation from storage based on conversation storage provider
        /// </summary>
        private async Task<string> GetConversationFromStorageAsync(string sessionId)
        {
            try
            {
                var conversationStorageProvider = GetConversationStorageProvider();

                if (SupportsConversationHistory(conversationStorageProvider))
                {
                    return await _conversationRepository.GetConversationHistoryAsync(sessionId);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationRetrievalFailed(_logger, sessionId, ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Store conversation to storage based on conversation storage provider
        /// </summary>
        private async Task StoreConversationToStorageAsync(string sessionId, string conversation)
        {
            try
            {
                var conversationStorageProvider = GetConversationStorageProvider();

                if (SupportsConversationHistory(conversationStorageProvider))
                {
                    await _conversationRepository.AddToConversationAsync(sessionId, "", conversation);
                }
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationStorageFailed(_logger, sessionId, ex);
            }
        }
    }
}

