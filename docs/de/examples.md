---
layout: default
title: Beispiele
description: Praktische Beispiele und Anwendungsfälle für SmartRAG
lang: de
---

<div class="page-header">
    <div class="container">
        <div class="row">
            <div class="col-lg-8 mx-auto text-center">
                <h1 class="page-title">Beispiele</h1>
                <p class="page-description">
                    Praktische Beispiele und Anwendungsfälle für SmartRAG
                </p>
            </div>
        </div>
    </div>
</div>

<div class="page-content">
    <div class="container">
        <!-- Basic Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Grundlegende Beispiele</h2>
                    <p>Einfache Beispiele für den Einstieg in SmartRAG.</p>
                    
                    <h3>Dokument hochladen und durchsuchen</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger&lt;DocumentController&gt; _logger;

    public DocumentController(IDocumentService documentService, ILogger&lt;DocumentController&gt; logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task&lt;ActionResult&lt;Document&gt;&gt; UploadDocument(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("Keine Datei ausgewählt");

            var document = await _documentService.UploadDocumentAsync(file);
            _logger.LogInformation("Dokument {FileName} erfolgreich hochgeladen", file.FileName);
            
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Hochladen des Dokuments");
            return StatusCode(500, "Interner Serverfehler");
        }
    }

    [HttpGet("search")]
    public async Task&lt;ActionResult&lt;IEnumerable&lt;DocumentChunk&gt;&gt;&gt; SearchDocuments(
        [FromQuery] string query, 
        [FromQuery] int maxResults = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Suchanfrage darf nicht leer sein");

            var results = await _documentService.SearchDocumentsAsync(query, maxResults);
            _logger.LogInformation("Suche nach '{Query}' ergab {Count} Ergebnisse", query, results.Count());
            
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei der Dokumentsuche");
            return StatusCode(500, "Interner Serverfehler");
        }
    }
}</code></pre>
                    </div>

                    <h3>RAG-Antwort generieren</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("ask")]
public async Task&lt;ActionResult&lt;RagResponse&gt;&gt; AskQuestion([FromBody] AskQuestionRequest request)
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Frage darf nicht leer sein");

        var response = await _documentService.GenerateRagAnswerAsync(request.Question, request.MaxResults ?? 5);
        _logger.LogInformation("RAG-Antwort für Frage '{Question}' generiert", request.Question);
        
        return Ok(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Fehler bei der RAG-Antwortgenerierung");
        return StatusCode(500, "Interner Serverfehler");
    }
}

public class AskQuestionRequest
{
    public string Question { get; set; }
    public int? MaxResults { get; set; }
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
                    <p>Komplexere Anwendungsfälle und erweiterte Funktionen.</p>
                    
                    <h3>Intelligente Abfrage-Absichtserkennung</h3>
                    <p>Leiten Sie Abfragen automatisch zu Chat oder Dokumentsuche basierend auf Absichtsanalyse weiter:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task&lt;QueryResult&gt; ProcessQueryAsync(string query)
{
    // Abfrage-Absicht analysieren
    var intent = await _queryIntentService.AnalyzeIntentAsync(query);
    
    switch (intent.Type)
    {
        case QueryIntentType.Chat:
            // Zu konversationeller KI weiterleiten
            return await _chatService.ProcessChatQueryAsync(query);
            
        case QueryIntentType.DocumentSearch:
            // Zu Dokumentsuche weiterleiten
            var searchResults = await _documentService.SearchDocumentsAsync(query);
            return new QueryResult 
            { 
                Type = QueryResultType.DocumentSearch,
                Results = searchResults 
            };
            
        case QueryIntentType.Mixed:
            // Beide Ansätze kombinieren
            var chatResponse = await _chatService.ProcessChatQueryAsync(query);
            var docResults = await _documentService.SearchDocumentsAsync(query);
            
            return new QueryResult 
            { 
                Type = QueryResultType.Mixed,
                ChatResponse = chatResponse,
                DocumentResults = docResults 
            };
            
        default:
            throw new ArgumentException($"Unbekannter Absichtstyp: {intent.Type}");
    }
}</code></pre>
                    </div>

                    <h4>Absichtserkennungs-Konfiguration</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Absichtserkennung konfigurieren
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
    
    // Intelligente Abfrage-Absichtserkennung aktivieren
    options.EnableQueryIntentDetection = true;
    options.IntentDetectionThreshold = 0.7; // Vertrauensschwelle
    options.LanguageAgnostic = true; // Funktioniert mit jeder Sprache
});

// In Ihrem Controller verwenden
[HttpPost("query")]
public async Task&lt;ActionResult&lt;QueryResult&gt;&gt; ProcessQuery([FromBody] QueryRequest request)
{
    var result = await _queryProcessor.ProcessQueryAsync(request.Query);
    return Ok(result);
}</code></pre>
                    </div>

                    <h3>Batch-Dokumentenverarbeitung</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("upload-batch")]
public async Task&lt;ActionResult&lt;BatchUploadResult&gt;&gt; UploadBatchDocuments(IFormFileCollection files)
{
    try
    {
        var results = new List&lt;Document&gt;();
        var errors = new List&lt;string&gt;();

        foreach (var file in files)
        {
            try
            {
                var document = await _documentService.UploadDocumentAsync(file);
                results.Add(document);
                _logger.LogInformation("Dokument {FileName} erfolgreich verarbeitet", file.FileName);
            }
            catch (Exception ex)
            {
                var error = $"Fehler bei {file.FileName}: {ex.Message}";
                errors.Add(error);
                _logger.LogWarning(ex, "Fehler beim Verarbeiten von {FileName}", file.FileName);
            }
        }

        return Ok(new BatchUploadResult
        {
            SuccessfulUploads = results,
            Errors = errors,
            TotalFiles = files.Count,
            SuccessCount = results.Count,
            ErrorCount = errors.Count
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Fehler bei der Batch-Verarbeitung");
        return StatusCode(500, "Interner Serverfehler");
    }
}

public class BatchUploadResult
{
    public List&lt;Document&gt; SuccessfulUploads { get; set; }
    public List&lt;string&gt; Errors { get; set; }
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
}</code></pre>
                    </div>

                    <h3>Erweiterte Suche mit Filtern</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("advanced-search")]
public async Task&lt;ActionResult&lt;AdvancedSearchResult&gt;&gt; AdvancedSearch([FromBody] AdvancedSearchRequest request)
{
    try
    {
        var searchResults = await _documentService.SearchDocumentsAsync(request.Query, request.MaxResults);
        
        // Zusätzliche Filter anwenden
        var filteredResults = searchResults
            .Where(r => request.MinSimilarityScore == null || r.SimilarityScore >= request.MinSimilarityScore)
            .Where(r => request.ContentTypes == null || !request.ContentTypes.Any() || 
                       request.ContentTypes.Contains(r.Document.ContentType))
            .OrderByDescending(r => r.SimilarityScore)
            .ToList();

        var result = new AdvancedSearchResult
        {
            Query = request.Query,
            Results = filteredResults,
            TotalResults = filteredResults.Count,
            SearchTime = DateTime.UtcNow,
            AppliedFilters = new
            {
                MinSimilarityScore = request.MinSimilarityScore,
                ContentTypes = request.ContentTypes
            }
        };

        _logger.LogInformation("Erweiterte Suche nach '{Query}' ergab {Count} gefilterte Ergebnisse", 
            request.Query, filteredResults.Count);
        
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Fehler bei der erweiterten Suche");
        return StatusCode(500, "Interner Serverfehler");
    }
}

public class AdvancedSearchRequest
{
    public string Query { get; set; }
    public int MaxResults { get; set; } = 10;
    public float? MinSimilarityScore { get; set; }
    public List&lt;string&gt; ContentTypes { get; set; }
}

public class AdvancedSearchResult
{
    public string Query { get; set; }
    public List&lt;DocumentChunk&gt; Results { get; set; }
    public int TotalResults { get; set; }
    public DateTime SearchTime { get; set; }
    public object AppliedFilters { get; set; }
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
                    <p>Vollständige Web API Controller mit allen SmartRAG-Funktionen.</p>
                    
                    <h3>Vollständiger Document Controller</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger&lt;DocumentController&gt; _logger;

    public DocumentController(IDocumentService documentService, ILogger&lt;DocumentController&gt; logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Lädt ein Dokument hoch
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(Document), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task&lt;ActionResult&lt;Document&gt;&gt; UploadDocument(IFormFile file)
    {
        // Implementation siehe oben
    }

    /// <summary>
    /// Ruft ein Dokument anhand der ID ab
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Document), 200)]
    [ProducesResponseType(404)]
    public async Task&lt;ActionResult&lt;Document&gt;&gt; GetDocument(string id)
    {
        try
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound($"Dokument mit ID {id} nicht gefunden");

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen des Dokuments {Id}", id);
            return StatusCode(500, "Interner Serverfehler");
        }
    }

    /// <summary>
    /// Ruft alle Dokumente ab
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable&lt;Document&gt;), 200)]
    public async Task&lt;ActionResult&lt;IEnumerable&lt;Document&gt;&gt;&gt; GetAllDocuments()
    {
        try
        {
            var documents = await _documentService.GetAllDocumentsAsync();
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen aller Dokumente");
            return StatusCode(500, "Interner Serverfehler");
        }
    }

    /// <summary>
    /// Löscht ein Dokument
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task&lt;ActionResult&gt; DeleteDocument(string id)
    {
        try
        {
            var success = await _documentService.DeleteDocumentAsync(id);
            if (!success)
                return NotFound($"Dokument mit ID {id} nicht gefunden");

            _logger.LogInformation("Dokument {Id} erfolgreich gelöscht", id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Löschen des Dokuments {Id}", id);
            return StatusCode(500, "Interner Serverfehler");
        }
    }

    /// <summary>
    /// Sucht in Dokumenten
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable&lt;DocumentChunk&gt;), 200)]
    public async Task&lt;ActionResult&lt;IEnumerable&lt;DocumentChunk&gt;&gt;&gt; SearchDocuments(
        [FromQuery] string query, 
        [FromQuery] int maxResults = 10)
    {
        // Implementation siehe oben
    }

    /// <summary>
    /// Generiert eine RAG-Antwort
    /// </summary>
    [HttpPost("ask")]
    [ProducesResponseType(typeof(RagResponse), 200)]
    public async Task&lt;ActionResult&lt;RagResponse&gt;&gt; AskQuestion([FromBody] AskQuestionRequest request)
    {
        // Implementation siehe oben
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
                    <h2>Konsolenanwendung Beispiel</h2>
                    <p>Einfache Konsolenanwendung mit SmartRAG-Integration.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces;

namespace SmartRAG.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            using var scope = host.Services.CreateScope();
            var documentService = scope.ServiceProvider.GetRequiredService&lt;IDocumentService&gt;();
            var logger = scope.ServiceProvider.GetRequiredService&lt;ILogger&lt;Program&gt;&gt;();

            logger.LogInformation("SmartRAG Konsolenanwendung gestartet");

            while (true)
            {
                Console.WriteLine("\n=== SmartRAG Konsolenanwendung ===");
                Console.WriteLine("1. Dokument hochladen");
                Console.WriteLine("2. Dokumente durchsuchen");
                Console.WriteLine("3. Frage stellen");
                Console.WriteLine("4. Beenden");
                Console.Write("Wählen Sie eine Option (1-4): ");

                var choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await UploadDocument(documentService, logger);
                            break;
                        case "2":
                            await SearchDocuments(documentService, logger);
                            break;
                        case "3":
                            await AskQuestion(documentService, logger);
                            break;
                        case "4":
                            logger.LogInformation("Anwendung wird beendet");
                            return;
                        default:
                            Console.WriteLine("Ungültige Option. Bitte wählen Sie 1-4.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Fehler bei der Ausführung");
                    Console.WriteLine($"Fehler: {ex.Message}");
                }
            }
        }

        static async Task UploadDocument(IDocumentService documentService, ILogger logger)
        {
            Console.Write("Geben Sie den Pfad zur Datei ein: ");
            var filePath = Console.ReadLine();

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Datei nicht gefunden!");
                return;
            }

            var fileInfo = new FileInfo(filePath);
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var fileStream = new MemoryStream(fileBytes);
            var formFile = new FormFile(fileStream, 0, fileBytes.Length, "file", fileInfo.Name);

            var document = await documentService.UploadDocumentAsync(formFile);
            Console.WriteLine($"Dokument erfolgreich hochgeladen: {document.Id}");
        }

        static async Task SearchDocuments(IDocumentService documentService, ILogger logger)
        {
            Console.Write("Geben Sie Ihre Suchanfrage ein: ");
            var query = Console.ReadLine();

            var results = await documentService.SearchDocumentsAsync(query, 5);
            
            Console.WriteLine($"\nGefundene Ergebnisse ({results.Count()}):");
            foreach (var result in results)
            {
                Console.WriteLine($"- {result.Document.FileName} (Ähnlichkeit: {result.SimilarityScore:P2})");
                Console.WriteLine($"  {result.Content.Substring(0, Math.Min(100, result.Content.Length))}...");
            }
        }

        static async Task AskQuestion(IDocumentService documentService, ILogger logger)
        {
            Console.Write("Stellen Sie Ihre Frage: ");
            var question = Console.ReadLine();

            var response = await documentService.GenerateRagAnswerAsync(question, 5);
            
            Console.WriteLine($"\nAntwort: {response.Answer}");
            Console.WriteLine($"\nQuellen ({response.Sources.Count}):");
            foreach (var source in response.Sources)
            {
                Console.WriteLine($"- {source.DocumentName} (Ähnlichkeit: {source.SimilarityScore:P2})");
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSmartRAG(context.Configuration, options =>
                    {
                        options.AIProvider = AIProvider.Anthropic;
                        options.StorageProvider = StorageProvider.Qdrant;
                    });
                });
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Benötigen Sie Hilfe?</h4>
                        <p class="mb-0">Für weitere Beispiele und Unterstützung:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/de/getting-started">Erste Schritte</a></li>
                            <li><a href="{{ site.baseurl }}/de/configuration">Konfiguration</a></li>
                            <li><a href="{{ site.baseurl }}/de/api-reference">API-Referenz</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG" target="_blank">GitHub Repository</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">E-Mail-Support</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>