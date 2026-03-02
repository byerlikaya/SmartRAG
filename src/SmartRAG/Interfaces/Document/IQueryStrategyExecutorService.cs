
namespace SmartRAG.Interfaces.Document;


/// <summary>
/// Service interface for executing query strategies
/// </summary>
public interface IQueryStrategyExecutorService
{
    /// <summary>
    /// Executes a database-only query strategy with fallback to document query
    /// </summary>
    /// <param name="request">Request containing query parameters</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>RAG response with answer and sources</returns>
    Task<RagResponse> ExecuteDatabaseOnlyStrategyAsync(QueryStrategyRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a hybrid query strategy combining both database and document queries
    /// </summary>
    /// <param name="request">Request containing query parameters</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Merged RAG response with answer and sources from both database and documents</returns>
    Task<RagResponse> ExecuteHybridStrategyAsync(QueryStrategyRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a document-only query strategy
    /// </summary>
    /// <param name="request">Request containing query parameters</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>RAG response with answer and sources</returns>
    Task<RagResponse> ExecuteDocumentOnlyStrategyAsync(QueryStrategyRequest request, CancellationToken cancellationToken = default);
}


