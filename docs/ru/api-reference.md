---
layout: default
title: API Reference
description: Complete API documentation with examples and usage patterns for SmartRAG
lang: en
---

# API Reference

Complete API documentation with examples and usage patterns for SmartRAG.

## Core Interfaces

### IDocumentService

The main service for document operations.

```csharp
public interface IDocumentService
{
    Task<Document> UploadDocumentAsync(IFormFile file);
    Task<IEnumerable<Document>> GetAllDocumentsAsync();
    Task<Document> GetDocumentByIdAsync(string id);
    Task<bool> DeleteDocumentAsync(string id);
    Task<IEnumerable<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 10);
}
```

### IDocumentParserService

Service for parsing and processing documents.

```csharp
public interface IDocumentParserService
{
    Task<string> ExtractTextAsync(IFormFile file);
    Task<IEnumerable<DocumentChunk>> ParseDocumentAsync(string text, string documentId);
    Task<IEnumerable<DocumentChunk>> ParseDocumentAsync(Stream stream, string fileName, string documentId);
}
```

### IDocumentRepository

Repository for document storage operations.

```csharp
public interface IDocumentRepository
{
    Task<Document> AddAsync(Document document);
    Task<Document> GetByIdAsync(string id);
    Task<IEnumerable<Document>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
    Task<IEnumerable<DocumentChunk>> SearchAsync(string query, int maxResults = 10);
}
```

## Models

### Document

Represents a document in the system.

```csharp
public class Document
{
    public string Id { get; set; }
    public string FileName { get; set; }
    public string FileType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadDate { get; set; }
    public string Content { get; set; }
    public IEnumerable<DocumentChunk> Chunks { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

### DocumentChunk

Represents a chunk of a document.

```csharp
public class DocumentChunk
{
    public string Id { get; set; }
    public string DocumentId { get; set; }
    public string Content { get; set; }
    public int ChunkIndex { get; set; }
    public float[] Embedding { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

### SmartRagOptions

Configuration options for SmartRAG.

```csharp
public class SmartRagOptions
{
    public AIProvider AIProvider { get; set; }
    public StorageProvider StorageProvider { get; set; }
    public string ApiKey { get; set; }
    public string ModelName { get; set; }
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 200;
    public string QdrantUrl { get; set; }
    public string CollectionName { get; set; }
    public string RedisConnectionString { get; set; }
    public int DatabaseId { get; set; }
    public string ConnectionString { get; set; }
}
```

## Enums

### AIProvider

```csharp
public enum AIProvider
{
    Anthropic,
    OpenAI,
    AzureOpenAI,
    Gemini,
    Custom
}
```

### StorageProvider

```csharp
public enum StorageProvider
{
    Qdrant,
    Redis,
    Sqlite,
    InMemory,
    FileSystem,
    Custom
}
```

## Service Registration

### AddSmartRAG Extension

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmartRAG(
        this IServiceCollection services,
        Action<SmartRagOptions> configureOptions)
    {
        var options = new SmartRagOptions();
        configureOptions(options);
        
        services.Configure<SmartRagOptions>(opt => 
        {
            opt.AIProvider = options.AIProvider;
            opt.StorageProvider = options.StorageProvider;
            opt.ApiKey = options.ApiKey;
            // ... other options
        });
        
        // Register services based on configuration
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentParserService, DocumentParserService>();
        
        // Register appropriate repository
        switch (options.StorageProvider)
        {
            case StorageProvider.Qdrant:
                services.AddScoped<IDocumentRepository, QdrantDocumentRepository>();
                break;
            case StorageProvider.Redis:
                services.AddScoped<IDocumentRepository, RedisDocumentRepository>();
                break;
            // ... other cases
        }
        
        return services;
    }
}
```

## Usage Examples

### Basic Document Upload

```csharp
[HttpPost("upload")]
public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
{
    try
    {
        var document = await _documentService.UploadDocumentAsync(file);
        return Ok(document);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}
```

### Document Search

```csharp
[HttpGet("search")]
public async Task<ActionResult<IEnumerable<DocumentChunk>>> SearchDocuments(
    [FromQuery] string query, 
    [FromQuery] int maxResults = 10)
{
    try
    {
        var results = await _documentService.SearchDocumentsAsync(query, maxResults);
        return Ok(results);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}
```

### Custom Configuration

```csharp
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = Configuration["SmartRAG:ApiKey"];
    options.ChunkSize = 800;
    options.ChunkOverlap = 150;
    options.QdrantUrl = "http://localhost:6333";
    options.CollectionName = "my_documents";
});
```

## Error Handling

### Common Exceptions

```csharp
public class SmartRagException : Exception
{
    public SmartRagException(string message) : base(message) { }
    public SmartRagException(string message, Exception innerException) 
        : base(message, innerException) { }
}

public class DocumentProcessingException : SmartRagException
{
    public DocumentProcessingException(string message) : base(message) { }
}

public class StorageException : SmartRagException
{
    public StorageException(string message) : base(message) { }
}
```

### Error Response Model

```csharp
public class ErrorResponse
{
    public string Message { get; set; }
    public string ErrorCode { get; set; }
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; }
}
```

## Logging

### Logger Messages

```csharp
public static class ServiceLogMessages
{
    public static readonly Action<ILogger, string, Exception> DocumentUploadStarted = 
        LoggerMessage.Define<string>(LogLevel.Information, 
            new EventId(1001, nameof(DocumentUploadStarted)), 
            "Document upload started for file: {FileName}");
            
    public static readonly Action<ILogger, string, Exception> DocumentUploadCompleted = 
        LoggerMessage.Define<string>(LogLevel.Information, 
            new EventId(1002, nameof(DocumentUploadCompleted)), 
            "Document upload completed for file: {FileName}");
}
```

## Performance Considerations

### Chunking Strategy

- **Small chunks**: Better for precise search, more API calls
- **Large chunks**: Better context, fewer API calls
- **Overlap**: Ensures important information isn't split

### Batch Operations

```csharp
public async Task<IEnumerable<Document>> UploadDocumentsAsync(IEnumerable<IFormFile> files)
{
    var tasks = files.Select(file => UploadDocumentAsync(file));
    return await Task.WhenAll(tasks);
}
```

## Need Help?

If you need assistance with the API:

- [Back to Documentation]({{ site.baseurl }}/en/) - Main documentation
- [Open an issue](https://github.com/byerlikaya/SmartRAG/issues) - GitHub Issues
- [Contact support](mailto:b.yerlikaya@outlook.com) - Email support
