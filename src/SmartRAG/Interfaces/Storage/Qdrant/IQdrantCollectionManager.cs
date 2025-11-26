using System;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Storage.Qdrant
{
    /// <summary>
    /// Interface for managing Qdrant collections and document storage
    /// </summary>
    public interface IQdrantCollectionManager : IDisposable
    {
        /// <summary>
        /// Ensures the main collection exists and is ready for operations
        /// </summary>
        Task EnsureCollectionExistsAsync();

        /// <summary>
        /// Creates a new collection with specified vector parameters
        /// </summary>
        /// <param name="collectionName">Name of the collection to create</param>
        /// <param name="vectorDimension">Dimension of vectors to store</param>
        Task CreateCollectionAsync(string collectionName, int vectorDimension);

        /// <summary>
        /// Ensures a document-specific collection exists
        /// </summary>
        /// <param name="collectionName">Name of the document collection</param>
        /// <param name="document">Document to store</param>
        Task EnsureDocumentCollectionExistsAsync(string collectionName, SmartRAG.Entities.Document document);

        /// <summary>
        /// Gets the vector dimension for collections
        /// </summary>
        Task<int> GetVectorDimensionAsync();

        /// <summary>
        /// Deletes a collection completely
        /// </summary>
        /// <param name="collectionName">Name of the collection to delete</param>
        Task DeleteCollectionAsync(string collectionName);

        /// <summary>
        /// Recreates a collection (deletes and creates anew)
        /// </summary>
        /// <param name="collectionName">Name of the collection to recreate</param>
        Task RecreateCollectionAsync(string collectionName);
    }
}
