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
        /// <param name="fileStream">File stream containing document content</param>
        /// <param name="fileName">Name of the file</param>
        /// <param name="contentType">MIME content type of the file</param>
        /// <param name="uploadedBy">Identifier of the user uploading the document</param>
        /// <param name="language">Language code for document processing (optional)</param>
        /// <returns>Created document entity</returns>
        Task<Entities.Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy, string language = null);

        /// <summary>
        /// Upload multiple documents
        /// </summary>
        /// <param name="fileStreams">Collection of file streams containing document content</param>
        /// <param name="fileNames">Collection of file names corresponding to streams</param>
        /// <param name="contentTypes">Collection of MIME content types corresponding to streams</param>
        /// <param name="uploadedBy">Identifier of the user uploading the documents</param>
        /// <returns>List of created document entities</returns>
        Task<List<Entities.Document>> UploadDocumentsAsync(IEnumerable<Stream> fileStreams, IEnumerable<string> fileNames, IEnumerable<string> contentTypes, string uploadedBy);

        /// <summary>
        /// Get document by ID
        /// </summary>
        /// <param name="id">Unique document identifier</param>
        /// <returns>Document entity or null if not found</returns>
        Task<Entities.Document> GetDocumentAsync(Guid id);

        /// <summary>
        /// Get all documents
        /// </summary>
        /// <returns>List of all document entities</returns>
        Task<List<Entities.Document>> GetAllDocumentsAsync();

        /// <summary>
        /// Delete document
        /// </summary>
        /// <param name="id">Unique document identifier</param>
        /// <returns>True if document was deleted successfully</returns>
        Task<bool> DeleteDocumentAsync(Guid id);

        /// <summary>
        /// Get storage statistics
        /// </summary>
        /// <returns>Dictionary containing storage statistics (document count, total size, etc.)</returns>
        Task<Dictionary<string, object>> GetStorageStatisticsAsync();

        /// <summary>
        /// Regenerate all embeddings
        /// </summary>
        /// <returns>True if regeneration completed successfully</returns>
        Task<bool> RegenerateAllEmbeddingsAsync();

        /// <summary>
        /// Clear all embeddings from all documents
        /// </summary>
        /// <returns>True if clearing completed successfully</returns>
        Task<bool> ClearAllEmbeddingsAsync();

        /// <summary>
        /// Clear all documents and their embeddings
        /// </summary>
        /// <returns>True if clearing completed successfully</returns>
        Task<bool> ClearAllDocumentsAsync();
    }
}
