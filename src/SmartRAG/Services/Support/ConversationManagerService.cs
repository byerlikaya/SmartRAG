#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Storage;
using SmartRAG.Interfaces.Support;
using SmartRAG.Models;
using SmartRAG.Services.Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private const string ChatUnavailableMessage = "Sorry, I cannot chat right now. Please try again later.";

        private readonly IConversationRepository _conversationRepository;
        private readonly SmartRagOptions _options;
        private readonly ILogger<ConversationManagerService> _logger;
        private readonly IAIConfigurationService? _aiConfiguration;
        private readonly IAIProviderFactory? _aiProviderFactory;
        private readonly IPromptBuilderService? _promptBuilder;

        private readonly ConcurrentDictionary<string, string> _conversationCache = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the ConversationManagerService
        /// </summary>
        /// <param name="conversationRepository">Repository for conversation operations</param>
        /// <param name="options">SmartRAG configuration options</param>
        /// <param name="logger">Logger instance for this service</param>
        /// <param name="aiConfiguration">Optional service for AI provider configuration</param>
        /// <param name="aiProviderFactory">Optional factory for creating AI providers</param>
        /// <param name="promptBuilder">Optional service for building AI prompts</param>
        public ConversationManagerService(
            IConversationRepository conversationRepository,
            IOptions<SmartRagOptions> options,
            ILogger<ConversationManagerService> logger,
            IAIConfigurationService? aiConfiguration = null,
            IAIProviderFactory? aiProviderFactory = null,
            IPromptBuilderService? promptBuilder = null)
        {
            _conversationRepository = conversationRepository;
            _options = options.Value;
            _logger = logger;
            _aiConfiguration = aiConfiguration;
            _aiProviderFactory = aiProviderFactory;
            _promptBuilder = promptBuilder;
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
                        var sessionId = sessionLine["session-id:".Length..].Trim();

                        var sessionExists = await _conversationRepository.SessionExistsAsync(sessionId);
                        if (sessionExists)
                        {
                            var conversationHistory = await _conversationRepository.GetConversationHistoryAsync(sessionId);

                            _conversationCache.TryAdd(sessionId, conversationHistory ?? string.Empty);
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
                _logger.LogError(ex, "Error getting conversation history");
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
                string currentHistory;
                if (_conversationCache.TryGetValue(sessionId, out var cachedHistory))
                {
                    currentHistory = RemoveDuplicateEntries(cachedHistory ?? string.Empty);
                }
                else
                {
                    currentHistory = await GetConversationFromStorageAsync(sessionId);
                }

                var newTurn = $"User: {question}\nAssistant: {answer}";

                if (!string.IsNullOrEmpty(currentHistory))
                {
                    var lines = currentHistory.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("User: ", StringComparison.OrdinalIgnoreCase) &&
                            lines[i].Equals($"User: {question}", StringComparison.OrdinalIgnoreCase) &&
                            i + 1 < lines.Length &&
                            lines[i + 1].StartsWith("Assistant: ", StringComparison.OrdinalIgnoreCase) &&
                            lines[i + 1].Equals($"Assistant: {answer}", StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                    }
                }

                var newEntry = string.IsNullOrEmpty(currentHistory)
                    ? newTurn
                    : $"{currentHistory}\n{newTurn}";

                await StoreConversationToStorageAsync(sessionId, newEntry);

                _conversationCache.AddOrUpdate(sessionId, newEntry, (key, oldValue) => newEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to conversation");
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

            var recentTurns = turns.TakeLast(maxTurns).ToList();

            if (recentTurns.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("\n", recentTurns);
        }

        /// <summary>
        /// Clears all conversation history from storage
        /// </summary>
        public async Task ClearAllConversationsAsync()
        {
            try
            {
                _conversationCache.Clear();
                await _conversationRepository.ClearAllConversationsAsync();
                _logger.LogInformation("Cleared all conversation history");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear all conversation history");
                throw;
            }
        }

        /// <summary>
        /// Handles general conversation queries with conversation history
        /// </summary>
        public async Task<string> HandleGeneralConversationAsync(string query, string? conversationHistory = null)
        {
            try
            {
                if (_aiConfiguration == null || _aiProviderFactory == null || _promptBuilder == null)
                {
                    return ChatUnavailableMessage;
                }

                var providerConfig = _aiConfiguration.GetProviderConfig(_options.AIProvider);

                if (providerConfig == null)
                {
                    return ChatUnavailableMessage;
                }

                var aiProvider = _aiProviderFactory.CreateProvider(_options.AIProvider);

                var prompt = _promptBuilder.BuildConversationPrompt(query, conversationHistory);

                return await aiProvider.GenerateTextAsync(prompt, providerConfig);
            }
            catch (Exception)
            {
                return ChatUnavailableMessage;
            }
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
        /// Get conversation from storage based on conversation storage provider
        /// </summary>
        private async Task<string> GetConversationFromStorageAsync(string sessionId)
        {
            try
            {
                var history = await _conversationRepository.GetConversationHistoryAsync(sessionId);
                return RemoveDuplicateEntries(history);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationRetrievalFailed(_logger, sessionId, ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Removes duplicate conversation entries from history
        /// </summary>
        private string RemoveDuplicateEntries(string history)
        {
            if (string.IsNullOrWhiteSpace(history))
                return string.Empty;

            var lines = history.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var seenEntries = new HashSet<string>();
            var uniqueLines = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("User: ", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < lines.Length && lines[i + 1].StartsWith("Assistant: ", StringComparison.OrdinalIgnoreCase))
                    {
                        var userLine = line;
                        var assistantLine = lines[i + 1];
                        var entry = $"{userLine}\n{assistantLine}";

                        if (!seenEntries.Contains(entry))
                        {
                            seenEntries.Add(entry);
                            uniqueLines.Add(userLine);
                            uniqueLines.Add(assistantLine);
                            i++;
                        }
                        else
                        {
                            i++;
                        }
                    }
                    else
                    {
                        uniqueLines.Add(line);
                    }
                }
                else if (line.StartsWith("Assistant: ", StringComparison.OrdinalIgnoreCase))
                {
                    if (i > 0 && lines[i - 1].StartsWith("User: ", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    uniqueLines.Add(line);
                }
                else
                {
                    uniqueLines.Add(line);
                }
            }

            return string.Join("\n", uniqueLines);
        }

        /// <summary>
        /// Store conversation to storage based on conversation storage provider
        /// </summary>
        private async Task StoreConversationToStorageAsync(string sessionId, string conversation)
        {
            try
            {
                await _conversationRepository.SetConversationHistoryAsync(sessionId, conversation);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationStorageFailed(_logger, sessionId, ex);
            }
        }
    }
}

