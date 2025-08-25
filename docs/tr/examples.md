---
layout: default
title: Örnekler
description: SmartRAG'dan öğrenmek için gerçek dünya örnekleri ve örnek uygulamalar
lang: tr
---

# Örnekler

SmartRAG'dan öğrenmek için gerçek dünya örnekleri ve örnek uygulamalar.

## Temel Örnekler

### Basit Belge Yükleme

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

## Gelişmiş Örnekler

### Toplu Belge İşleme

```csharp
public async Task<IEnumerable<Document>> ProcessMultipleDocumentsAsync(
    IEnumerable<IFormFile> files)
{
    var tasks = files.Select(async file =>
    {
        try
        {
            return await _documentService.UploadDocumentAsync(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process file: {FileName}", file.FileName);
            return null;
        }
    });

    var results = await Task.WhenAll(tasks);
    return results.Where(d => d != null);
}
```

### Özel Parçalama Stratejisi

```csharp
public class CustomChunkingStrategy : IChunkingStrategy
{
    public IEnumerable<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        var sentences = text.Split(new[] { '.', '!', '?' }, 
            StringSplitOptions.RemoveEmptyEntries);
        
        var currentChunk = new StringBuilder();
        
        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > chunkSize)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }
            }
            currentChunk.AppendLine(sentence.Trim() + ".");
        }
        
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }
        
        return chunks;
    }
}
```

### Özel AI Provider

```csharp
public class CustomAIProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    public CustomAIProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["CustomAI:ApiKey"];
    }
    
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var request = new
        {
            text = text,
            model = "custom-embedding-model"
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            "https://api.customai.com/embeddings", request);
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        return result.Embedding;
    }
}
```

## Web API Örnekleri

### Tam Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;
    
    public DocumentsController(
        IDocumentService documentService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }
    
    [HttpPost("upload")]
    public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");
            
        try
        {
            var document = await _documentService.UploadDocumentAsync(file);
            _logger.LogInformation("Document uploaded: {DocumentId}", document.Id);
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document: {FileName}", file.FileName);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<DocumentChunk>>> SearchDocuments(
        [FromQuery] string query, 
        [FromQuery] int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query parameter is required");
            
        try
        {
            var results = await _documentService.SearchDocumentsAsync(query, maxResults);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query: {Query}", query);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Document>> GetDocument(string id)
    {
        try
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound();
                
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document: {DocumentId}", id);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDocument(string id)
    {
        try
        {
            var success = await _documentService.DeleteDocumentAsync(id);
            if (!success)
                return NotFound();
                
            _logger.LogInformation("Document deleted: {DocumentId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document: {DocumentId}", id);
            return BadRequest(ex.Message);
        }
    }
}
```

## Konsol Uygulaması Örneği

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        
        // Servisleri yapılandır
        services.AddSmartRAG(options =>
        {
            options.AIProvider = AIProvider.Anthropic;
            options.StorageProvider = StorageProvider.Qdrant;
            options.ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            options.ChunkSize = 1000;
            options.ChunkOverlap = 200;
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentService>();
        
        Console.WriteLine("SmartRAG Konsol Uygulaması");
        Console.WriteLine("============================");
        
        while (true)
        {
            Console.WriteLine("\nSeçenekler:");
            Console.WriteLine("1. Belge yükle");
            Console.WriteLine("2. Belgelerde ara");
            Console.WriteLine("3. Tüm belgeleri listele");
            Console.WriteLine("4. Çıkış");
            Console.Write("Bir seçenek seçin: ");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    await UploadDocument(documentService);
                    break;
                case "2":
                    await SearchDocuments(documentService);
                    break;
                case "3":
                    await ListDocuments(documentService);
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("Geçersiz seçenek. Lütfen tekrar deneyin.");
                    break;
            }
        }
    }
    
    static async Task UploadDocument(IDocumentService documentService)
    {
        Console.Write("Dosya yolunu girin: ");
        var filePath = Console.ReadLine();
        
        if (!File.Exists(filePath))
        {
            Console.WriteLine("Dosya bulunamadı.");
            return;
        }
        
        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileStream = File.OpenRead(filePath);
            
            // Mock IFormFile oluştur
            var formFile = new FormFile(fileStream, 0, fileInfo.Length, 
                fileInfo.Name, fileInfo.Name);
            
            var document = await documentService.UploadDocumentAsync(formFile);
            Console.WriteLine($"Belge başarıyla yüklendi. ID: {document.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Belge yükleme hatası: {ex.Message}");
        }
    }
    
    static async Task SearchDocuments(IDocumentService documentService)
    {
        Console.Write("Arama sorgusunu girin: ");
        var query = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine("Sorgu boş olamaz.");
            return;
        }
        
        try
        {
            var results = await documentService.SearchDocumentsAsync(query, 5);
            Console.WriteLine($"{results.Count()} sonuç bulundu:");
            
            foreach (var result in results)
            {
                Console.WriteLine($"- {result.Content.Substring(0, Math.Min(100, result.Content.Length))}...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Belge arama hatası: {ex.Message}");
        }
    }
    
    static async Task ListDocuments(IDocumentService documentService)
    {
        try
        {
            var documents = await documentService.GetAllDocumentsAsync();
            Console.WriteLine($"Toplam belge sayısı: {documents.Count()}");
            
            foreach (var doc in documents)
            {
                Console.WriteLine($"- {doc.FileName} (ID: {doc.Id})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Belge listeleme hatası: {ex.Message}");
        }
    }
}
```

## Yardıma mı ihtiyacınız var?

Örnekler konusunda yardıma ihtiyacınız varsa:

- [Ana Dokümantasyona Dön]({{ site.baseurl }}/tr/) - Ana dokümantasyon
- [GitHub'da issue açın](https://github.com/byerlikaya/SmartRAG/issues) - GitHub Issues
- [Destek için iletişime geçin](mailto:b.yerlikaya@outlook.com) - E-posta desteği
