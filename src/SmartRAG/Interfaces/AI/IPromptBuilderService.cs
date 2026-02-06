#nullable enable

namespace SmartRAG.Interfaces.AI;


/// <summary>
/// Service interface for building AI prompts
/// </summary>
public interface IPromptBuilderService
{
    /// <summary>
    /// Builds a prompt for document-based RAG answer generation
    /// </summary>
    /// <param name="query">User query</param>
    /// <param name="context">Document context</param>
    /// <param name="conversationHistory">Optional conversation history</param>
    /// <param name="extractionRetryMode">When true, uses stronger extraction instructions for retry when sources contain data but initial response indicated missing</param>
    /// <returns>Built prompt</returns>
    string BuildDocumentRagPrompt(string query, string context, string? conversationHistory = null, bool extractionRetryMode = false);

    /// <summary>
    /// Builds a prompt for merging hybrid results (database + documents)
    /// </summary>
    /// <param name="query">User query</param>
    /// <param name="databaseContext">Database context</param>
    /// <param name="documentContext">Document context</param>
    /// <param name="conversationHistory">Optional conversation history</param>
    /// <returns>Built prompt</returns>
    string BuildHybridMergePrompt(string query, string? databaseContext, string? documentContext, string? conversationHistory = null);

    /// <summary>
    /// Builds a prompt for general conversation
    /// </summary>
    /// <param name="query">User query</param>
    /// <param name="conversationHistory">Optional conversation history</param>
    /// <returns>Built prompt</returns>
    string BuildConversationPrompt(string query, string? conversationHistory = null);
}


