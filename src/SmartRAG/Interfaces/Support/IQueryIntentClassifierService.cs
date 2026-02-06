
namespace SmartRAG.Interfaces.Support;


/// <summary>
/// Service interface for classifying query intent (conversation vs information)
/// </summary>
public interface IQueryIntentClassifierService
{
    /// <summary>
    /// Analyzes the query intent and returns both conversation classification and tokenized query terms.
    /// </summary>
    /// <param name="query">User query to analyze.</param>
    /// <param name="conversationHistory">Optional conversation history for context.</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Analysis result containing conversation flag and normalized tokens for non-conversational queries.</returns>
    Task<QueryIntentAnalysisResult> AnalyzeQueryAsync(string query, string? conversationHistory = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses command from user input and extracts payload if available
    /// </summary>
    /// <param name="input">User input to parse</param>
    /// <param name="commandType">Type of command detected</param>
    /// <param name="payload">Extracted payload after command</param>
    /// <returns>True if a command was detected, false otherwise</returns>
    bool TryParseCommand(string input, out QueryCommandType commandType, out string payload);
}

