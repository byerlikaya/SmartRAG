namespace SmartRAG.Interfaces;

/// <summary>
/// Service for parsing different document formats and extracting text content
/// </summary>
public interface IDocumentParserService
{
    /// <summary>
    /// Parses document from file stream and creates document entity
    /// </summary>
    Task<SmartRAG.Entities.Document> ParseDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy);
    
    /// <summary>
    /// Gets list of supported file extensions
    /// </summary>
    IEnumerable<string> GetSupportedFileTypes();
    
    /// <summary>
    /// Gets list of supported MIME content types
    /// </summary>
    IEnumerable<string> GetSupportedContentTypes();
}
