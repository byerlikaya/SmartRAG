
namespace SmartRAG.Interfaces.Storage;



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
    /// Gets the document repository for the current storage provider using the given scoped service provider.
    /// Use this overload when resolving from a request scope so that scoped dependencies (e.g. IAIConfigurationService) can be resolved.
    /// </summary>
    /// <param name="scopedProvider">The request/scoped service provider.</param>
    /// <returns>Document repository instance.</returns>
    IDocumentRepository GetCurrentRepository(IServiceProvider scopedProvider);

    /// <summary>
    /// Gets the currently active conversation repository instance
    /// </summary>
    /// <returns>Currently active conversation repository instance</returns>
    IConversationRepository GetCurrentConversationRepository();
}

