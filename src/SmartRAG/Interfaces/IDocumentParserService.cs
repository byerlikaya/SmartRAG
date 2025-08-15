namespace SmartRAG.Interfaces;

/// <summary>
/// Service for parsing different document formats and extracting text content
/// </summary>
public interface IDocumentParserService
{
    Task<Entities.Document> ParseDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy);
    IEnumerable<string> GetSupportedFileTypes();
    IEnumerable<string> GetSupportedContentTypes();
}
