#nullable enable

using SmartRAG.Enums;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Support
{
    /// <summary>
    /// Service interface for classifying query intent (conversation vs information)
    /// </summary>
    public interface IQueryIntentClassifierService
    {
        /// <summary>
        /// Determines whether the query should be treated as general conversation
        /// </summary>
        /// <param name="query">User query to classify</param>
        /// <param name="conversationHistory">Optional conversation history for context</param>
        /// <returns>True if query is conversation, false if information query</returns>
        Task<bool> IsGeneralConversationAsync(string query, string? conversationHistory = null);

        /// <summary>
        /// Parses command from user input and extracts payload if available
        /// </summary>
        /// <param name="input">User input to parse</param>
        /// <param name="commandType">Type of command detected</param>
        /// <param name="payload">Extracted payload after command</param>
        /// <returns>True if a command was detected, false otherwise</returns>
        bool TryParseCommand(string input, out QueryCommandType commandType, out string payload);
    }
}

