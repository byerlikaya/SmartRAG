namespace SmartRAG.Interfaces.Document;



/// <summary>
/// Repository interface for document storage operations
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Adds a new document to storage
    /// </summary>
    /// <param name="document">Document entity to add</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Added document entity</returns>
    Task<SmartRAG.Entities.Document> AddAsync(SmartRAG.Entities.Document document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves document by unique identifier
    /// </summary>
    /// <param name="id">Unique document identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Document entity or null if not found</returns>
    Task<SmartRAG.Entities.Document> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all documents from storage
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>List of all document entities</returns>
    Task<List<SmartRAG.Entities.Document>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes document from storage by ID
    /// </summary>
    /// <param name="id">Unique document identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if document was deleted successfully</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total count of documents in storage
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Total number of documents</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches documents using query string
    /// </summary>
    /// <param name="query">Search query string</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>List of relevant document chunks</returns>
    Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all documents from storage (efficient bulk delete)
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if all documents were cleared successfully</returns>
    Task<bool> ClearAllAsync(CancellationToken cancellationToken = default);

}

