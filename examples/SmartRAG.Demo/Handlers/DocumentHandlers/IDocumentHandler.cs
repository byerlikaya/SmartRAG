namespace SmartRAG.Demo.Handlers.DocumentHandlers;

/// <summary>
/// Interface for document operation handlers
/// </summary>
public interface IDocumentHandler
{
    Task UploadDocumentsAsync(string language);
    Task ListDocumentsAsync();
    Task ClearAllDocumentsAsync();
}

