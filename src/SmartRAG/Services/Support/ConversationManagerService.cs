#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Enums;
using SmartRAG.Interfaces.Document;
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

        private readonly IDocumentRepository _documentRepository;
        private readonly SmartRagOptions _options;
        private readonly ILogger<ConversationManagerService> _logger;

        // Conversation management using existing storage
        private readonly ConcurrentDictionary<string, string> _conversationCache = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the ConversationManagerService
        /// </summary>
        /// <param name="documentRepository">Repository for document operations</param>
        /// <param name="options">SmartRAG configuration options</param>
        /// <param name="logger">Logger instance for this service</param>
        public ConversationManagerService(
            IDocumentRepository documentRepository,
            IOptions<SmartRagOptions> options,
            ILogger<ConversationManagerService> logger)
        {
            _documentRepository = documentRepository;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Gets or creates a session ID automatically for conversation continuity
        /// Uses a persistent session key that survives application restarts
        /// </summary>
        public async Task<string> GetOrCreateSessionIdAsync()
        {
            // First, try to get existing session from storage
            try
            {
                var existingSessionData = await _documentRepository.GetConversationHistoryAsync(PersistentSessionKey);
                if (!string.IsNullOrEmpty(existingSessionData))
                {
                    // Extract session ID from stored data (format: "session-id:actual-session-id")
                    var lines = existingSessionData.Split('\n');
                    var sessionLine = lines.FirstOrDefault(l => l.StartsWith("session-id:"));
                    if (sessionLine != null)
                    {
                        var sessionId = sessionLine.Substring("session-id:".Length).Trim();

                        // Verify session still exists and has conversation data
                        var sessionExists = await _documentRepository.SessionExistsAsync(sessionId);
                        if (sessionExists)
                        {
                            // Get the actual conversation history from the session
                            var conversationHistory = await _documentRepository.GetConversationHistoryAsync(sessionId);

                            // Add to cache for faster access
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

            // Create and persist new session
            return await CreateAndPersistSessionAsync();
        }

        /// <summary>
        /// Starts a new conversation session
        /// </summary>
        public async Task<string> StartNewConversationAsync()
        {
            try
            {
                // Clear current session from cache
                var currentSession = _conversationCache.Keys.FirstOrDefault();
                if (!string.IsNullOrEmpty(currentSession))
                {
                    _conversationCache.TryRemove(currentSession, out _);
                }

                // Clear persistent session key
                try
                {
                    await _documentRepository.ClearConversationAsync(PersistentSessionKey);
                }
                catch (Exception ex)
                {
                    ServiceLogMessages.LogConversationStorageFailed(_logger, PersistentSessionKey, ex);
                }

                // Create and persist new session
                return await CreateAndPersistSessionAsync();
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationStorageFailed(_logger, "new-session", ex);
                // Fallback: create session without persistence
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
                // Always get fresh data from storage to ensure conversation continuity
                var history = await GetConversationFromStorageAsync(sessionId);

                // Update cache with fresh data
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
                // Get current history from storage (not cache)
                var currentHistory = await GetConversationFromStorageAsync(sessionId);

                // Build new conversation entry
                var newEntry = string.IsNullOrEmpty(currentHistory)
                    ? $"User: {question}\nAssistant: {answer}"
                    : $"{currentHistory}\nUser: {question}\nAssistant: {answer}";

                // No automatic truncation - keep full conversation history
                // Conversation will only be cleared when user starts a new session

                // Store in persistent storage first
                await StoreConversationToStorageAsync(sessionId, newEntry);

                // Then update cache
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

            // Split by conversation turns (User: ... Assistant: ...)
            var lines = history.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var turns = new System.Collections.Generic.List<string>();
            var currentTurn = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Detect start of a new turn
                if (trimmed.StartsWith("User:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Assistant:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("A:", StringComparison.OrdinalIgnoreCase))
                {
                    // Save previous turn if exists
                    if (currentTurn.Length > 0)
                    {
                        turns.Add(currentTurn.ToString());
                        currentTurn.Clear();
                    }
                }

                currentTurn.AppendLine(trimmed);
            }

            // Add last turn
            if (currentTurn.Length > 0)
            {
                turns.Add(currentTurn.ToString());
            }

            // Keep only last N turns
            var recentTurns = turns.TakeLast(maxTurns * 2).ToList(); // *2 because each turn is User + Assistant

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
                // Store the session ID in persistent storage
                await _documentRepository.AddToConversationAsync(PersistentSessionKey, "", $"session-id:{newSessionId}");
                await _documentRepository.AddToConversationAsync(newSessionId, "", "");

                // Add to cache
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
                // Convert ConversationStorageProvider to StorageProvider
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

            // If not specified, use StorageProvider but exclude Qdrant
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
                    return await _documentRepository.GetConversationHistoryAsync(sessionId);
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
                    await _documentRepository.AddToConversationAsync(sessionId, "", conversation);
                }
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationStorageFailed(_logger, sessionId, ex);
            }
        }
    }
}

