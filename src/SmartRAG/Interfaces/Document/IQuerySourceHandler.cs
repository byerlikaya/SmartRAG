namespace SmartRAG.Interfaces.Document;


/// <summary>
/// Handler for executing a specific query source type (database, document, MCP).
/// Used by the query intelligence orchestrator to delegate by SearchOptions.
/// </summary>
public interface IQuerySourceHandler
{
    /// <summary>
    /// Whether this handler can process the request for the given search options.
    /// </summary>
    bool CanHandle(SearchOptions options);

    /// <summary>
    /// Executes the search for this source type and returns a RAG response, or null if no result.
    /// </summary>
    Task<RagResponse?> ExecuteAsync(QuerySourceHandlerRequest request, CancellationToken cancellationToken = default);
}
