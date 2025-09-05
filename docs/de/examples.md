---
layout: default
title: Beispiele
description: Praktische Beispiele und Code-Samples für SmartRAG-Integration
lang: de
---

<div class="page-content">
    <div class="container">
        <!-- Basic Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Grundlegende Beispiele</h2>
                    <p>Einfache Beispiele zum Einstieg in SmartRAG.</p>
                    
                    <h3>Einfaches Dokument-Upload</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("upload")]
    public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
    {
        try
        {
        using var stream = file.OpenReadStream();
        var document = await _documentService.UploadDocumentAsync(
            stream, file.FileName, file.ContentType, "user123");
            return Ok(document);
        }
        catch (Exception ex)
        {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>Dokument-Suche</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpGet("search")]
    public async Task<ActionResult<IEnumerable<DocumentChunk>>> SearchDocuments(
        [FromQuery] string query, 
        [FromQuery] int maxResults = 10)
    {
        try
        {
        var results = await _documentSearchService.SearchDocumentsAsync(query, maxResults);
            return Ok(results);
        }
        catch (Exception ex)
        {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>RAG-Antwort-Generierung (mit Gesprächsverlauf)</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("search")]
public async Task<ActionResult<object>> Search([FromBody] SearchRequest request)
{
    string query = request?.Query ?? string.Empty;
    int maxResults = request?.MaxResults ?? 5;

    if (string.IsNullOrWhiteSpace(query))
        return BadRequest("Query cannot be empty");

    try
    {
        var response = await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);
        return Ok(response);
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Internal server error: {ex.Message}");
    }
}

public class SearchRequest
{
    [Required]
    public string Query { get; set; } = string.Empty;

    [Range(1, 50)]
    [DefaultValue(5)]
    public int MaxResults { get; set; } = 5;

    /// <summary>
    /// Session ID for conversation history
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Advanced Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Erweiterte Beispiele</h2>
                    <p>Komplexere Beispiele für erweiterte Anwendungsfälle.</p>
                    
                    <h3>Batch-Dokument-Verarbeitung</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("upload-multiple")]
public async Task<ActionResult<List<Document>>> UploadMultipleDocuments(
    IEnumerable<IFormFile> files)
{
    try
    {
        var streams = new List<Stream>();
        var fileNames = new List<string>();
        var contentTypes = new List<string>();

        foreach (var file in files)
        {
            streams.Add(file.OpenReadStream());
            fileNames.Add(file.FileName);
            contentTypes.Add(file.ContentType);
        }

        var documents = await _documentService.UploadDocumentsAsync(
            streams, fileNames, contentTypes, "user123");
        
        return Ok(documents);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>Dokument-Verwaltung</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Alle Dokumente abrufen
[HttpGet]
public async Task<ActionResult<List<Document>>> GetAllDocuments()
{
    var documents = await _documentService.GetAllDocumentsAsync();
    return Ok(documents);
}

// Bestimmtes Dokument abrufen
[HttpGet("{id}")]
public async Task<ActionResult<Document>> GetDocument(Guid id)
{
    var document = await _documentService.GetDocumentAsync(id);
    if (document == null)
        return NotFound();
    
    return Ok(document);
}

// Dokument löschen
[HttpDelete("{id}")]
public async Task<ActionResult> DeleteDocument(Guid id)
{
    var success = await _documentService.DeleteDocumentAsync(id);
    if (!success)
        return NotFound();
    
    return NoContent();
}</code></pre>
                    </div>

                    <h3>Speicher-Statistiken</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpGet("statistics")]
public async Task<ActionResult<Dictionary<string, object>>> GetStorageStatistics()
{
    var stats = await _documentService.GetStorageStatisticsAsync();
    return Ok(stats);
}</code></pre>
                    </div>

                    <h3>Embedding-Operationen</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Alle Embeddings regenerieren
[HttpPost("regenerate-embeddings")]
public async Task<ActionResult> RegenerateAllEmbeddings()
{
    var success = await _documentService.RegenerateAllEmbeddingsAsync();
    if (success)
        return Ok("Alle Embeddings erfolgreich regeneriert");
    else
        return BadRequest("Embeddings konnten nicht regeneriert werden");
}

// Alle Embeddings löschen
[HttpPost("clear-embeddings")]
public async Task<ActionResult> ClearAllEmbeddings()
{
    var success = await _documentService.ClearAllEmbeddingsAsync();
    if (success)
        return Ok("Alle Embeddings erfolgreich gelöscht");
    else
        return BadRequest("Embeddings konnten nicht gelöscht werden");
}

// Alle Dokumente löschen
[HttpPost("clear-all")]
public async Task<ActionResult> ClearAllDocuments()
{
    var success = await _documentService.ClearAllDocumentsAsync();
    if (success)
        return Ok("Alle Dokumente erfolgreich gelöscht");
    else
        return BadRequest("Dokumente konnten nicht gelöscht werden");
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Web API Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Web API Beispiele</h2>
                    <p>Vollständige Controller-Beispiele für Web-Anwendungen.</p>
                    
                    <h3>Vollständiger Controller</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _documentSearchService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        IDocumentSearchService documentSearchService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _documentSearchService = documentSearchService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Keine Datei bereitgestellt");
            
        try
        {
            using var stream = file.OpenReadStream();
            var document = await _documentService.UploadDocumentAsync(
                stream, file.FileName, file.ContentType, "user123");
            _logger.LogInformation("Dokument hochgeladen: {DocumentId}", document.Id);
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dokument-Upload fehlgeschlagen: {FileName}", file.FileName);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<DocumentChunk>>> SearchDocuments(
        [FromQuery] string query, 
        [FromQuery] int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Abfrage-Parameter erforderlich");
            
        try
        {
            var results = await _documentSearchService.SearchDocumentsAsync(query, maxResults);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Suche fehlgeschlagen für Abfrage: {Query}", query);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("chat")]
    public async Task<ActionResult<RagResponse>> ChatWithDocuments(
        [FromBody] string query,
        [FromQuery] int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Abfrage-Parameter erforderlich");
            
        try
        {
            var response = await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat fehlgeschlagen für Abfrage: {Query}", query);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Document>> GetDocument(Guid id)
    {
        try
        {
            var document = await _documentService.GetDocumentAsync(id);
            if (document == null)
                return NotFound();

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dokument abrufen fehlgeschlagen: {DocumentId}", id);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<Document>>> GetAllDocuments()
    {
        try
        {
            var documents = await _documentService.GetAllDocumentsAsync();
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alle Dokumente abrufen fehlgeschlagen");
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDocument(Guid id)
    {
        try
        {
            var success = await _documentService.DeleteDocumentAsync(id);
            if (!success)
                return NotFound();

            _logger.LogInformation("Dokument gelöscht: {DocumentId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dokument löschen fehlgeschlagen: {DocumentId}", id);
            return BadRequest(ex.Message);
        }
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Console Application Example Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Konsolen-Anwendungsbeispiel</h2>
                    <p>Ein vollständiges Konsolen-Anwendungsbeispiel.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">class Program
{
    static async Task Main(string[] args)
    {
        // Create configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
            
        var services = new ServiceCollection();
        
        // Services konfigurieren
        services.AddSmartRag(configuration, options =>
        {
            options.AIProvider = AIProvider.Anthropic;
            options.StorageProvider = StorageProvider.Qdrant;
            options.MaxChunkSize = 1000;
            options.ChunkOverlap = 200;
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentService>();
        var documentSearchService = serviceProvider.GetRequiredService<IDocumentSearchService>();
        
        Console.WriteLine("SmartRAG Konsolen-Anwendung");
        Console.WriteLine("============================");

            while (true)
            {
            Console.WriteLine("\nOptionen:");
                Console.WriteLine("1. Dokument hochladen");
                Console.WriteLine("2. Dokumente durchsuchen");
            Console.WriteLine("3. Mit Dokumenten chatten");
            Console.WriteLine("4. Alle Dokumente auflisten");
            Console.WriteLine("5. Beenden");
            Console.Write("Option wählen: ");

                var choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                    await UploadDocument(documentService);
                            break;
                        case "2":
                    await SearchDocuments(documentSearchService);
                            break;
                        case "3":
                    await ChatWithDocuments(documentSearchService);
                            break;
                        case "4":
                    await ListDocuments(documentService);
                    break;
                case "5":
                            return;
                        default:
                    Console.WriteLine("Ungültige Option. Bitte versuchen Sie es erneut.");
                            break;
            }
        }
    }
    
    static async Task UploadDocument(IDocumentService documentService)
    {
        Console.Write("Dateipfad eingeben: ");
            var filePath = Console.ReadLine();

            if (!File.Exists(filePath))
            {
            Console.WriteLine("Datei nicht gefunden.");
                return;
            }

        try
        {
            var fileInfo = new FileInfo(filePath);
            using var fileStream = File.OpenRead(filePath);
            
            var document = await documentService.UploadDocumentAsync(
                fileStream, fileInfo.Name, "application/octet-stream", "console-user");
            Console.WriteLine($"Dokument erfolgreich hochgeladen. ID: {document.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Hochladen des Dokuments: {ex.Message}");
        }
    }
    
    static async Task SearchDocuments(IDocumentSearchService documentSearchService)
    {
        Console.Write("Suchabfrage eingeben: ");
            var query = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine("Abfrage darf nicht leer sein.");
            return;
        }
        
        try
        {
            var results = await documentSearchService.SearchDocumentsAsync(query, 5);
            Console.WriteLine($"{results.Count} Ergebnisse gefunden:");
            
            foreach (var result in results)
            {
                Console.WriteLine($"- {result.Content.Substring(0, Math.Min(100, result.Content.Length))}...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Durchsuchen der Dokumente: {ex.Message}");
        }
    }
    
    static async Task ChatWithDocuments(IDocumentSearchService documentSearchService)
    {
        Console.Write("Ihre Frage eingeben: ");
        var query = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine("Frage darf nicht leer sein.");
            return;
        }
        
        try
        {
            var response = await documentSearchService.GenerateRagAnswerAsync(query, 5);
            Console.WriteLine($"KI-Antwort: {response.Answer}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Chatten mit Dokumenten: {ex.Message}");
        }
    }
    
    static async Task ListDocuments(IDocumentService documentService)
    {
        try
        {
            var documents = await documentService.GetAllDocumentsAsync();
            Console.WriteLine($"Gesamtzahl der Dokumente: {documents.Count}");
            
            foreach (var doc in documents)
            {
                Console.WriteLine($"- {doc.FileName} (ID: {doc.Id})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Auflisten der Dokumente: {ex.Message}");
        }
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Configuration Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Konfigurationsbeispiele</h2>
                    <p>Verschiedene Möglichkeiten, SmartRAG-Services zu konfigurieren.</p>
                    
                    <h3>Grundlegende Konfiguration</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
services.UseSmartRag(configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.Gemini
);</code></pre>
                    </div>

                    <h3>Erweiterte Konfiguration</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
services.AddSmartRag(configuration, options =>
                    {
                        options.AIProvider = AIProvider.Anthropic;
                        options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.MinChunkSize = 50;
    options.ChunkOverlap = 200;
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new[] { AIProvider.Gemini, AIProvider.OpenAI };
});</code></pre>
                    </div>

                    <h3>appsettings.json Konfiguration</h3>
                    <div class="code-example">
                        <pre><code class="language-json">{
  "SmartRAG": {
    "AIProvider": "Anthropic",
    "StorageProvider": "Qdrant",
    "MaxChunkSize": 1000,
    "MinChunkSize": 50,
    "ChunkOverlap": 200,
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000,
    "RetryPolicy": "ExponentialBackoff",
    "EnableFallbackProviders": true,
    "FallbackProviders": ["Gemini", "OpenAI"]
  },
  "Anthropic": {
    "ApiKey": "your-anthropic-api-key"
  },
  "Qdrant": {
    "ApiKey": "your-qdrant-api-key"
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Need Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Brauchen Sie Hilfe?</h4>
                        <p class="mb-2">Wenn Sie Hilfe mit Beispielen benötigen:</p>
                        <ul class="mb-0">
                            <li><a href="{{ site.baseurl }}/de/getting-started">Erste Schritte Anleitung</a></li>
                            <li><a href="{{ site.baseurl }}/de/api-reference">API-Referenz</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues">Issue auf GitHub öffnen</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Support per E-Mail kontaktieren</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
