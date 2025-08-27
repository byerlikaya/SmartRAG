using SmartRAG.Enums;
using SmartRAG.Models;

namespace SmartRAG.Interfaces {

/// <summary>
/// Factory interface for creating document storage repositories
/// </summary>
public interface IStorageFactory
{
    /// <summary>
    /// Creates repository using storage configuration
    /// </summary>
    IDocumentRepository CreateRepository(StorageConfig config);

    /// <summary>
    /// Creates repository using storage provider type
    /// </summary>
    IDocumentRepository CreateRepository(StorageProvider provider);

    /// <summary>
    /// Gets the currently active storage provider
    /// </summary>
    StorageProvider GetCurrentProvider();

    /// <summary>
    /// Gets the currently active repository instance
    /// </summary>
    IDocumentRepository GetCurrentRepository();
}
}
