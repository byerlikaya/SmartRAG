using SmartRAG.Entities;
using SmartRAG.Models;
using SmartRAG.Interfaces.Document;

namespace SmartRAG.Interfaces.Search;


/// <summary>
/// Service interface for building search sources from document chunks
/// </summary>
public interface ISourceBuilderService
{
    /// <summary>
    /// Builds search sources from document chunks
    /// </summary>
    /// <param name="chunks">Document chunks to build sources from</param>
    /// <param name="documentRepository">Repository for document operations</param>
    /// <returns>List of search sources</returns>
    Task<List<SearchSource>> BuildSourcesAsync(List<DocumentChunk> chunks, IDocumentRepository documentRepository);
}


