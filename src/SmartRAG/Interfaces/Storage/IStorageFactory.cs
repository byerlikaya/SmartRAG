using SmartRAG.Enums;
using SmartRAG.Models;
using SmartRAG.Interfaces.Document;

namespace SmartRAG.Interfaces.Storage
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

        /// <summary>
        /// Creates conversation repository using storage configuration
        /// </summary>
        /// <param name="config">Storage configuration settings</param>
        /// <returns>Conversation repository instance</returns>
        IConversationRepository CreateConversationRepository(StorageConfig config);

        /// <summary>
        /// Creates conversation repository using storage provider type
        /// </summary>
        /// <param name="provider">Storage provider type</param>
        /// <returns>Conversation repository instance</returns>
        IConversationRepository CreateConversationRepository(StorageProvider provider);

        /// <summary>
        /// Gets the currently active conversation repository instance
        /// </summary>
        /// <returns>Currently active conversation repository instance</returns>
        IConversationRepository GetCurrentConversationRepository();
    }
}
