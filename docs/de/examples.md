---
layout: default
title: Examples
description: Real-world examples and sample applications to learn from SmartRAG
lang: en
---

# Examples

Real-world examples and sample applications to learn from SmartRAG.

## Basic Examples

### Simple Document Upload

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

## Advanced Examples

### Batch Document Processing

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

### Custom Chunking Strategy

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

### Custom AI Provider

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

## Web API Examples

### Complete Controller

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

## Console Application Example

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        
        // Configure services
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
        
        Console.WriteLine("SmartRAG Console Application");
        Console.WriteLine("============================");
        
        while (true)
        {
            Console.WriteLine("\nOptions:");
            Console.WriteLine("1. Upload document");
            Console.WriteLine("2. Search documents");
            Console.WriteLine("3. List all documents");
            Console.WriteLine("4. Exit");
            Console.Write("Choose an option: ");
            
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
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }
    
    static async Task UploadDocument(IDocumentService documentService)
    {
        Console.Write("Enter file path: ");
        var filePath = Console.ReadLine();
        
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File not found.");
            return;
        }
        
        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileStream = File.OpenRead(filePath);
            
            // Create a mock IFormFile
            var formFile = new FormFile(fileStream, 0, fileInfo.Length, 
                fileInfo.Name, fileInfo.Name);
            
            var document = await documentService.UploadDocumentAsync(formFile);
            Console.WriteLine($"Document uploaded successfully. ID: {document.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading document: {ex.Message}");
        }
    }
    
    static async Task SearchDocuments(IDocumentService documentService)
    {
        Console.Write("Enter search query: ");
        var query = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine("Query cannot be empty.");
            return;
        }
        
        try
        {
            var results = await documentService.SearchDocumentsAsync(query, 5);
            Console.WriteLine($"Found {results.Count()} results:");
            
            foreach (var result in results)
            {
                Console.WriteLine($"- {result.Content.Substring(0, Math.Min(100, result.Content.Length))}...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching documents: {ex.Message}");
        }
    }
    
    static async Task ListDocuments(IDocumentService documentService)
    {
        try
        {
            var documents = await documentService.GetAllDocumentsAsync();
            Console.WriteLine($"Total documents: {documents.Count()}");
            
            foreach (var doc in documents)
            {
                Console.WriteLine($"- {doc.FileName} (ID: {doc.Id})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing documents: {ex.Message}");
        }
    }
}
```

## Need Help?

If you need assistance with examples:

- [Back to Documentation]({{ site.baseurl }}/en/) - Main documentation
- [Open an issue](https://github.com/byerlikaya/SmartRAG/issues) - GitHub Issues
- [Contact support](mailto:b.yerlikaya@outlook.com) - Email support

