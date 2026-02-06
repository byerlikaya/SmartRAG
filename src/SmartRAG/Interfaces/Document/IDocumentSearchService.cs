
namespace SmartRAG.Interfaces.Document;


/// <summary>
/// Service interface for AI-powered intelligence and RAG operations
/// </summary>
public interface IDocumentSearchService
{
    /// <summary>
    /// Process intelligent query with RAG and automatic session management
    /// </summary>
    /// <param name="query">Natural language query to process (supports tags: -d, -db, -i, -a, -mcp, -lang:xx)</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="startNewConversation">Whether to start a new conversation session</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>RAG response with AI-generated answer and relevant sources</returns>
    Task<RagResponse> QueryIntelligenceAsync(string query, int maxResults = 5, bool startNewConversation = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process intelligent query with RAG using explicit session context
    /// </summary>
    /// <param name="query">Natural language query to process</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="sessionId">Session ID for conversation continuity</param>
    /// <param name="conversationHistory">Conversation history for the session</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>RAG response with AI-generated answer and relevant sources</returns>
    Task<RagResponse> QueryIntelligenceAsync(string query, int maxResults, string sessionId, string conversationHistory, CancellationToken cancellationToken = default);
}

