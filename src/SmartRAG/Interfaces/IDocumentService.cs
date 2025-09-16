using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces
{

    /// <summary>
    /// Service interface for document CRUD operations
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Upload a single document
        /// </summary>
        Task<Entities.Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy, string language = null);

        /// <summary>
        /// Upload multiple documents
        /// </summary>
        Task<List<Entities.Document>> UploadDocumentsAsync(IEnumerable<Stream> fileStreams, IEnumerable<string> fileNames, IEnumerable<string> contentTypes, string uploadedBy);

        /// <summary>
        /// Get document by ID
        /// </summary>
        Task<Entities.Document> GetDocumentAsync(Guid id);

        /// <summary>
        /// Get all documents
        /// </summary>
        Task<List<Entities.Document>> GetAllDocumentsAsync();

        /// <summary>
        /// Delete document
        /// </summary>
        Task<bool> DeleteDocumentAsync(Guid id);

        /// <summary>
        /// Get storage statistics
        /// </summary>
        Task<Dictionary<string, object>> GetStorageStatisticsAsync();

        /// <summary>
        /// Regenerate all embeddings
        /// </summary>
        Task<bool> RegenerateAllEmbeddingsAsync();

        /// <summary>
        /// Clear all embeddings from all documents
        /// </summary>
        Task<bool> ClearAllEmbeddingsAsync();

        /// <summary>
        /// Clear all documents and their embeddings
        /// </summary>
        Task<bool> ClearAllDocumentsAsync();
    }
}
