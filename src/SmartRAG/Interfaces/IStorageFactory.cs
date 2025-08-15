using SmartRAG.Enums;
using SmartRAG.Models;

namespace SmartRAG.Interfaces;

/// <summary>
/// Factory interface for creating document storage repositories
/// </summary>
public interface IStorageFactory
{
    IDocumentRepository CreateRepository(StorageConfig config);

    IDocumentRepository CreateRepository(StorageProvider provider);

    StorageProvider GetCurrentProvider();

    IDocumentRepository GetCurrentRepository();

}
