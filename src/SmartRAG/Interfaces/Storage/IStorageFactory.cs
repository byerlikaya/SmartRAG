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
        /// Gets the currently active conversation repository instance
        /// </summary>
        /// <returns>Currently active conversation repository instance</returns>
        IConversationRepository GetCurrentConversationRepository();
    }
}
