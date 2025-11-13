using SmartRAG.Enums;
using SmartRAG.Models;

namespace SmartRAG.Interfaces
{

    /// <summary>
    /// Factory interface for creating document storage repositories
    /// </summary>
    public interface IStorageFactory
    {
        /// <summary>
        /// Creates repository using storage configuration
        /// </summary>
        /// <param name="config">Storage configuration settings</param>
        /// <returns>Document repository instance</returns>
        IDocumentRepository CreateRepository(StorageConfig config);

        /// <summary>
        /// Creates repository using storage provider type
        /// </summary>
        /// <param name="provider">Storage provider type</param>
        /// <returns>Document repository instance</returns>
        IDocumentRepository CreateRepository(StorageProvider provider);

        /// <summary>
        /// Gets the currently active storage provider
        /// </summary>
        /// <returns>Currently active storage provider</returns>
        StorageProvider GetCurrentProvider();

        /// <summary>
        /// Gets the currently active repository instance
        /// </summary>
        /// <returns>Currently active document repository instance</returns>
        IDocumentRepository GetCurrentRepository();
    }
}
