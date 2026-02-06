using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Storage;
using SmartRAG.Interfaces.Support;
using SmartRAG.Services.Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Services.Support;


/// <summary>
/// Service for managing conversation sessions and history
/// </summary>
public class ConversationManagerService : IConversationManagerService
{
    private const string PersistentSessionKey = "smartrag-current-session";
    private const string ChatUnavailableMessage = "Sorry, I cannot chat right now. Please try again later.";

    private readonly IConversationRepository _conversationRepository;
    private readonly ILogger<ConversationManagerService> _logger;
    private readonly IAIService? _aiService;
    private readonly IPromptBuilderService? _promptBuilder;

    private readonly ConcurrentDictionary<string, string> _conversationCache = new ConcurrentDictionary<string, string>();

    /// <summary>
    /// Initializes a new instance of the ConversationManagerService
    /// </summary>
    /// <param name="conversationRepository">Repository for conversation operations</param>
    /// <param name="logger">Logger instance for this service</param>
    /// <param name="aiService">Optional AI service for generating responses (uses AIService with retry and fallback logic)</param>
    /// <param name="promptBuilder">Optional service for building AI prompts</param>
    public ConversationManagerService(
        IConversationRepository conversationRepository,
        ILogger<ConversationManagerService> logger,
        IAIService? aiService = null,
        IPromptBuilderService? promptBuilder = null)
    {
        _conversationRepository = conversationRepository;
        _logger = logger;
        _aiService = aiService;
        _promptBuilder = promptBuilder;
    }

    /// <summary>
    /// Gets or creates a session ID automatically for conversation continuity
    /// Uses a persistent session key that survives application restarts
    /// </summary>
    public async Task<string> GetOrCreateSessionIdAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var existingSessionData = await _conversationRepository.GetConversationHistoryAsync(PersistentSessionKey, cancellationToken);
            if (!string.IsNullOrEmpty(existingSessionData))
            {
                var lines = existingSessionData.Split('\n');
                var sessionLine = lines.FirstOrDefault(l => l.StartsWith("session-id:"));
                if (sessionLine != null)
                {
                    var sessionId = sessionLine["session-id:".Length..].Trim();

                    var sessionExists = await _conversationRepository.SessionExistsAsync(sessionId, cancellationToken);
                    if (sessionExists)
                    {
                        var conversationHistory = await _conversationRepository.GetConversationHistoryAsync(sessionId, cancellationToken);

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

        return await CreateAndPersistSessionAsync(cancellationToken);
    }

    /// <summary>
    /// Starts a new conversation session
    /// </summary>
    public async Task<string> StartNewConversationAsync(CancellationToken cancellationToken = default)
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
                await _conversationRepository.ClearConversationAsync(PersistentSessionKey, cancellationToken);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationStorageFailed(_logger, PersistentSessionKey, ex);
            }

            return await CreateAndPersistSessionAsync(cancellationToken);
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
    public async Task<string> GetConversationHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await GetConversationFromStorageAsync(sessionId, cancellationToken);

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
    public async Task AddToConversationAsync(string sessionId, string question, string answer, CancellationToken cancellationToken = default)
    {
        try
        {
            string currentHistory;
            if (_conversationCache.TryGetValue(sessionId, out var cachedHistory))
            {
                currentHistory = cachedHistory ?? string.Empty;
            }
            else
            {
                currentHistory = await GetConversationFromStorageAsync(sessionId, cancellationToken);
            }

            var newTurn = $"User: {question}\nAssistant: {answer}";
            var newEntry = string.IsNullOrEmpty(currentHistory)
                ? newTurn
                : $"{currentHistory}\n{newTurn}";

            await StoreConversationToStorageAsync(sessionId, newEntry, cancellationToken);

            _conversationCache.AddOrUpdate(sessionId, newEntry, (key, oldValue) => newEntry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to conversation");
        }
    }

    /// <inheritdoc />
    public Task AddSourcesForLastTurnAsync(string sessionId, string sourcesJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.CompletedTask;
        return _conversationRepository.AppendSourcesForTurnAsync(sessionId, sourcesJson, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> GetSourcesForSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.FromResult(string.Empty);
        return _conversationRepository.GetSourcesForSessionAsync(sessionId, cancellationToken);
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
    public async Task ClearAllConversationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _conversationCache.Clear();
            await _conversationRepository.ClearAllConversationsAsync(cancellationToken);
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
    public async Task<string> HandleGeneralConversationAsync(string query, string? conversationHistory = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_aiService == null || _promptBuilder == null)
            {
                return ChatUnavailableMessage;
            }

            var prompt = _promptBuilder.BuildConversationPrompt(query, conversationHistory);

            return await _aiService.GenerateResponseAsync(prompt, new List<string>(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error handling general conversation");
            return ChatUnavailableMessage;
        }
    }

    /// <summary>
    /// Creates a new session ID and persists it to storage and cache
    /// </summary>
    private async Task<string> CreateAndPersistSessionAsync(CancellationToken cancellationToken = default)
    {
        var newSessionId = GenerateSessionId();

        try
        {
            await _conversationRepository.AddToConversationAsync(PersistentSessionKey, "", $"session-id:{newSessionId}", cancellationToken);
            await _conversationRepository.AddToConversationAsync(newSessionId, "", "", cancellationToken);

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
    private async Task<string> GetConversationFromStorageAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _conversationRepository.GetConversationHistoryAsync(sessionId, cancellationToken);
            return history ?? string.Empty;
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
    private async Task StoreConversationToStorageAsync(string sessionId, string conversation, CancellationToken cancellationToken = default)
    {
        try
        {
            await _conversationRepository.SetConversationHistoryAsync(sessionId, conversation, cancellationToken);
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogConversationStorageFailed(_logger, sessionId, ex);
        }
    }
}


