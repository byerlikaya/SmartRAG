using System.IO;

namespace SmartRAG.Interfaces.Document;



/// <summary>
/// Service for parsing different document formats and extracting text content
/// </summary>
public interface IDocumentParserService
{
    /// <summary>
    /// Parses document from file stream and creates document entity
    /// </summary>
    /// <param name="fileStream">File stream containing document content</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="contentType">MIME content type of the file</param>
    /// <param name="uploadedBy">Identifier of the user uploading the document</param>
    /// <param name="language">Language code for document processing (optional)</param>
    /// <returns>Parsed document entity with extracted content</returns>
    Task<Entities.Document> ParseDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy, string language = null);

    /// <summary>
    /// Gets list of supported file extensions
    /// </summary>
    /// <returns>Collection of supported file extensions (e.g., ".pdf", ".docx")</returns>
    IEnumerable<string> GetSupportedFileTypes();

    /// <summary>
    /// Gets list of supported MIME content types
    /// </summary>
    /// <returns>Collection of supported MIME types (e.g., "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")</returns>
    IEnumerable<string> GetSupportedContentTypes();
}

