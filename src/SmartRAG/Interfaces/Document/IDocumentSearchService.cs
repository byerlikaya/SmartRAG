
namespace SmartRAG.Interfaces.Document;


/// <summary>
/// Service interface for AI-powered intelligence and RAG operations
/// </summary>
public interface IDocumentSearchService
{
    /// <summary>
    /// Process intelligent query with RAG. When request.SessionId is null, session and history are resolved automatically.
    /// </summary>
    /// <param name="request">Query and session context.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>RAG response with AI-generated answer and relevant sources.</returns>
    Task<RagResponse> QueryIntelligenceAsync(QueryIntelligenceRequest request, CancellationToken cancellationToken = default);
}

