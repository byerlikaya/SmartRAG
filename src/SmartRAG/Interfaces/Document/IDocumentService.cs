#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
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
        /// <returns>Created document entity</returns>
        Task<Entities.Document> UploadDocumentAsync(Models.RequestResponse.UploadDocumentRequest request);

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
        /// <returns>Created document entity</returns>
        [Obsolete("Use UploadDocumentAsync(UploadDocumentRequest) instead. This method will be removed in v4.0.0")]
        Task<Entities.Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy, string? language = null, long? fileSize = null, Dictionary<string, object>? additionalMetadata = null);


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
        /// Retrieves all documents filtered by the enabled search options (text, audio, image)
        /// </summary>
        /// <param name="options">Search options to filter documents</param>
        /// <returns>Filtered list of documents</returns>
        Task<List<Entities.Document>> GetAllDocumentsFilteredAsync(Models.SearchOptions? options);

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

        /// <summary>
        /// Determines if a document is an audio document based on content type
        /// </summary>
        /// <param name="doc">Document to check</param>
        /// <returns>True if document is audio</returns>
        bool IsAudioDocument(Entities.Document doc);

        /// <summary>
        /// Determines if a document is an image document based on content type
        /// </summary>
        /// <param name="doc">Document to check</param>
        /// <returns>True if document is image</returns>
        bool IsImageDocument(Entities.Document doc);

        /// <summary>
        /// Determines if a document is a text document (not audio and not image)
        /// </summary>
        /// <param name="doc">Document to check</param>
        /// <returns>True if document is text</returns>
        bool IsTextDocument(Entities.Document doc);
    }
}
