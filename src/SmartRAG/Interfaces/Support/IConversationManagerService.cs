namespace SmartRAG.Interfaces.Support;


/// <summary>
/// Service interface for managing conversation sessions and history
/// </summary>
public interface IConversationManagerService
{
    /// <summary>
    /// Gets or creates a session ID automatically for conversation continuity
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Session ID</returns>
    Task<string> GetOrCreateSessionIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a new conversation session
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>New session ID</returns>
    Task<string> StartNewConversationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets conversation history for a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Conversation history</returns>
    Task<string> GetConversationHistoryAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a conversation turn to the session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="question">User question</param>
    /// <param name="answer">Assistant answer</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task AddToConversationAsync(string sessionId, string question, string answer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends sources JSON for the latest assistant turn (one JSON array per turn).
    /// </summary>
    Task AddSourcesForLastTurnAsync(string sessionId, string sourcesJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stored sources for a session as JSON array of arrays, or null if none.
    /// </summary>
    Task<string> GetSourcesForSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Truncates conversation history to keep only the most recent turns
    /// </summary>
    /// <param name="history">Full conversation history</param>
    /// <param name="maxTurns">Maximum number of turns to keep</param>
    /// <returns>Truncated conversation history</returns>
    string TruncateConversationHistory(string history, int maxTurns = 3);

    /// <summary>
    /// Handles general conversation queries with conversation history
    /// </summary>
    /// <param name="query">User query</param>
    /// <param name="conversationHistory">Optional conversation history</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>AI-generated conversation response</returns>
    Task<string> HandleGeneralConversationAsync(string query, string? conversationHistory = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all conversation history from storage
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task ClearAllConversationsAsync(CancellationToken cancellationToken = default);
}


