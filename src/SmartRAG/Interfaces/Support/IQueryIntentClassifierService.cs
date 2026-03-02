
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
}

