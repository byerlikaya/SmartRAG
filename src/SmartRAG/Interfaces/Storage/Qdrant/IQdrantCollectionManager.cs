namespace SmartRAG.Interfaces.Storage.Qdrant;


/// <summary>
/// Interface for managing Qdrant collections and document storage
/// </summary>
public interface IQdrantCollectionManager : IDisposable
{
    /// <summary>
    /// Ensures the main collection exists and is ready for operations
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a document-specific collection exists
    /// </summary>
    /// <param name="collectionName">Name of the document collection</param>
    /// <param name="document">Document to store</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task EnsureDocumentCollectionExistsAsync(string collectionName, SmartRAG.Entities.Document document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a collection completely
    /// </summary>
    /// <param name="collectionName">Name of the collection to delete</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recreates a collection (deletes and creates anew)
    /// </summary>
    /// <param name="collectionName">Name of the collection to recreate</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task RecreateCollectionAsync(string collectionName, CancellationToken cancellationToken = default);
}

