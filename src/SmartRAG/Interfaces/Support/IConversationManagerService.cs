#nullable enable

using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Support
{
    /// <summary>
    /// Service interface for managing conversation sessions and history
    /// </summary>
    public interface IConversationManagerService
    {
        /// <summary>
        /// Gets or creates a session ID automatically for conversation continuity
        /// </summary>
        /// <returns>Session ID</returns>
        Task<string> GetOrCreateSessionIdAsync();

        /// <summary>
        /// Starts a new conversation session
        /// </summary>
        /// <returns>New session ID</returns>
        Task<string> StartNewConversationAsync();

        /// <summary>
        /// Gets conversation history for a session
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <returns>Conversation history</returns>
        Task<string> GetConversationHistoryAsync(string sessionId);

        /// <summary>
        /// Adds a conversation turn to the session
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="question">User question</param>
        /// <param name="answer">Assistant answer</param>
        Task AddToConversationAsync(string sessionId, string question, string answer);

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
        /// <param name="preferredLanguage">Optional preferred language code for AI response</param>
        /// <returns>AI-generated conversation response</returns>
        Task<string> HandleGeneralConversationAsync(string query, string? conversationHistory = null, string? preferredLanguage = null);

        /// <summary>
        /// Clears all conversation history from storage
        /// </summary>
        Task ClearAllConversationsAsync();
    }
}

