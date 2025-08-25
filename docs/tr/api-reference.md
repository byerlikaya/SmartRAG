---
layout: default
title: API Referansı
description: SmartRAG için örnekler ve kullanım desenleri ile tam API dokümantasyonu
lang: tr
---

# API Referansı

SmartRAG için örnekler ve kullanım desenleri ile tam API dokümantasyonu.

## Temel Arayüzler

### IDocumentService

Belge işlemleri için ana servis.

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

Belgeleri ayrıştırma ve işleme servisi.

```csharp
public interface IDocumentParserService
{
    Task<string> ExtractTextAsync(IFormFile file);
    Task<IEnumerable<DocumentChunk>> ParseDocumentAsync(string text, string documentId);
    Task<IEnumerable<DocumentChunk>> ParseDocumentAsync(Stream stream, string fileName, string documentId);
}
```

### IDocumentRepository

Belge depolama işlemleri için repository.

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

## Modeller

### Document

Sistemdeki bir belgeyi temsil eder.

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

Bir belgenin parçasını temsil eder.

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

SmartRAG için yapılandırma seçenekleri.

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

## Enum'lar

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

## Servis Kaydı

### AddSmartRAG Uzantısı

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
            // ... diğer seçenekler
        });
        
        // Yapılandırmaya göre servisleri kaydet
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentParserService, DocumentParserService>();
        
        // Uygun repository'yi kaydet
        switch (options.StorageProvider)
        {
            case StorageProvider.Qdrant:
                services.AddScoped<IDocumentRepository, QdrantDocumentRepository>();
                break;
            case StorageProvider.Redis:
                services.AddScoped<IDocumentRepository, RedisDocumentRepository>();
                break;
            // ... diğer durumlar
        }
        
        return services;
    }
}
```

## Kullanım Örnekleri

### Temel Belge Yükleme

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

### Belge Arama

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

### Özel Yapılandırma

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

## Hata Yönetimi

### Yaygın İstisnalar

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

### Hata Yanıt Modeli

```csharp
public class ErrorResponse
{
    public string Message { get; set; }
    public string ErrorCode { get; set; }
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; }
}
```

## Günlük Kaydı

### Günlük Mesajları

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

## Performans Hususları

### Parçalama Stratejisi

- **Küçük parçalar**: Daha kesin arama için, daha fazla API çağrısı
- **Büyük parçalar**: Daha iyi bağlam, daha az API çağrısı
- **Örtüşme**: Önemli bilgilerin bölünmemesini sağlar

### Toplu İşlemler

```csharp
public async Task<IEnumerable<Document>> UploadDocumentsAsync(IEnumerable<IFormFile> files)
{
    var tasks = files.Select(file => UploadDocumentAsync(file));
    return await Task.WhenAll(tasks);
}
```

## Yardıma mı ihtiyacınız var?

API konusunda yardıma ihtiyacınız varsa:

- [Ana Dokümantasyona Dön]({{ site.baseurl }}/tr/) - Ana dokümantasyon
- [GitHub'da issue açın](https://github.com/byerlikaya/SmartRAG/issues) - GitHub Issues
- [Destek için iletişime geçin](mailto:b.yerlikaya@outlook.com) - E-posta desteği
