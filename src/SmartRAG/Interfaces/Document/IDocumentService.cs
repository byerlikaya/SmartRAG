#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Document
{

    /// <summary>
    /// Service interface for document CRUD operations and filtering
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Upload a single document
        /// </summary>
        /// <param name="request">Request containing document upload parameters</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Created document entity</returns>
        Task<Entities.Document> UploadDocumentAsync(Models.RequestResponse.UploadDocumentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upload a single document
        /// </summary>
        /// <param name="fileStream">File stream containing document content</param>
        /// <param name="fileName">Name of the file</param>
        /// <param name="contentType">MIME content type of the file</param>
        /// <param name="uploadedBy">Identifier of the user uploading the document</param>
        /// <param name="language">Language code for document processing (optional)</param>
        /// <param name="fileSize">File size in bytes (optional, will be calculated from stream if not provided)</param>
        /// <param name="additionalMetadata">Additional metadata to add to document (optional)</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Created document entity</returns>
        [Obsolete("Use UploadDocumentAsync(UploadDocumentRequest) instead. This method will be removed in v4.0.0")]
        Task<Entities.Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy, string? language = null, long? fileSize = null, Dictionary<string, object>? additionalMetadata = null, CancellationToken cancellationToken = default);


        /// <summary>
        /// Get document by ID
        /// </summary>
        /// <param name="id">Unique document identifier</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Document entity or null if not found</returns>
        Task<Entities.Document> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all documents
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>List of all document entities</returns>
        Task<List<Entities.Document>> GetAllDocumentsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all documents filtered by the enabled search options (text, audio, image)
        /// </summary>
        /// <param name="options">Search options to filter documents</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Filtered list of documents</returns>
        Task<List<Entities.Document>> GetAllDocumentsFilteredAsync(Models.SearchOptions? options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete document
        /// </summary>
        /// <param name="id">Unique document identifier</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if document was deleted successfully</returns>
        Task<bool> DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get storage statistics
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Dictionary containing storage statistics (document count, total size, etc.)</returns>
        Task<Dictionary<string, object>> GetStorageStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Regenerate all embeddings
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if regeneration completed successfully</returns>
        Task<bool> RegenerateAllEmbeddingsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear all embeddings from all documents
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if clearing completed successfully</returns>
        Task<bool> ClearAllEmbeddingsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear all documents and their embeddings
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if clearing completed successfully</returns>
        Task<bool> ClearAllDocumentsAsync(CancellationToken cancellationToken = default);
    }
}
