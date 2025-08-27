using SmartRAG.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces;

/// <summary>
/// Repository interface for document storage operations
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Adds a new document to storage
    /// </summary>
    Task<SmartRAG.Entities.Document> AddAsync(SmartRAG.Entities.Document document);

    /// <summary>
    /// Retrieves document by unique identifier
    /// </summary>
    Task<SmartRAG.Entities.Document?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all documents from storage
    /// </summary>
    Task<List<SmartRAG.Entities.Document>> GetAllAsync();

    /// <summary>
    /// Removes document from storage by ID
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Gets total count of documents in storage
    /// </summary>
    Task<int> GetCountAsync();

    /// <summary>
    /// Searches documents using query string
    /// </summary>
    Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5);
}
