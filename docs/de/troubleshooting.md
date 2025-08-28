---
layout: default
title: Fehlerbehebung
description: Häufige Probleme und Lösungen für die SmartRAG-Implementierung
lang: de
---

<div class="page-content">
    <div class="container">
        <!-- Common Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Häufige Probleme</h2>
                    <p>Häufige Probleme und Lösungen, die Sie bei der Verwendung von SmartRAG begegnen können.</p>

                    <h3>Kompilierungsprobleme</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-exclamation-triangle me-2"></i>Achtung</h4>
                        <p class="mb-0">Erstellen Sie zuerst eine saubere Lösung, um Kompilierungsfehler zu beheben.</p>
                    </div>

                    <h4>NuGet-Paketfehler</h4>
                    <div class="code-example">
                        <pre><code class="language-bash"># Saubere Lösung
dotnet clean
dotnet restore
dotnet build</code></pre>
                    </div>

                    <h4>Abhängigkeitskonflikt</h4>
                    <div class="code-example">
                        <pre><code class="language-xml">&lt;PackageReference Include="SmartRAG" Version="1.1.0" /&gt;
&lt;PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" /&gt;
&lt;PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" /&gt;</code></pre>
                    </div>

                    <h3>Laufzeitprobleme</h3>
                    
                    <h4>API-Schlüssel-Fehler</h4>
                    <div class="alert alert-danger">
                        <h4><i class="fas fa-times-circle me-2"></i>Fehler</h4>
                        <p class="mb-0">UnauthorizedAccessException: API-Schlüssel ungültig oder fehlend.</p>
                    </div>

                    <div class="code-example">
                        <pre><code class="language-json">// appsettings.json
{
  "SmartRAG": {
    "AIProvider": "Anthropic",
    "Anthropic": {
      "ApiKey": "your-api-key-here",
      "Model": "claude-3-sonnet-20240229"
    }
  }
}</code></pre>
                    </div>

                    <h4>Verbindungs-Timeout</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxRetryAttempts = 5;
    options.RetryDelayMs = 2000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
});</code></pre>
                    </div>

                    <h3>Leistungsprobleme</h3>
                    
                    <h4>Langsame Suche</h4>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Tipp</h4>
                        <p class="mb-0">Optimieren Sie die Chunk-Größen, um die Leistung zu verbessern.</p>
                    </div>

                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(configuration, options =>
{
    options.MaxChunkSize = 1000;  // 1000-1500 empfohlen
    options.ChunkOverlap = 200;   // 200-300 empfohlen
    options.MaxRetryAttempts = 3;
});</code></pre>
                    </div>

                    <h4>Speicherverbrauch</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Verwenden Sie Streaming für große Dateien
public async Task&lt;Document&gt; UploadLargeDocumentAsync(IFormFile file)
{
    using var stream = file.OpenReadStream();
    var chunks = await _documentParserService.ChunkTextAsync(stream, 1000, 200);
    
    var document = new Document
    {
        Id = Guid.NewGuid().ToString(),
        FileName = file.FileName,
        Content = await _documentParserService.ParseDocumentAsync(file),
        ContentType = file.ContentType,
        FileSize = file.Length,
        UploadedAt = DateTime.UtcNow
    };
    
    return document;
}</code></pre>
                    </div>

                    <h3>Konfigurationsprobleme</h3>
                    
                    <h4>Falsche Provider-Auswahl</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Richtige Konfiguration
services.AddSmartRAG(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    
    // Erforderliche Einstellungen für Qdrant
    options.Qdrant = new QdrantOptions
    {
        Host = "localhost",
        Port = 6333,
        CollectionName = "smartrag_documents"
    };
});</code></pre>
                    </div>

                    <h4>Fehlende Abhängigkeiten</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Erforderliche Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SmartRAG Services
builder.Services.AddSmartRAG(builder.Configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
});

var app = builder.Build();

// Middleware-Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Debugging Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Debugging</h2>
                    <p>Debugging-Techniken für Ihre SmartRAG-Anwendung.</p>
                    
                    <h3>Protokollierungskonfiguration</h3>
                    <div class="code-example">
                        <pre><code class="language-json">// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SmartRAG": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}</code></pre>
                    </div>

                    <h3>Detailliertes Debugging</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class DocumentController : ControllerBase
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
        _logger.LogInformation("Dokument-Upload gestartet: {FileName}, Größe: {Size}", 
            file?.FileName, file?.Length);

        try
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Datei ist null oder leer");
                return BadRequest("Keine Datei ausgewählt");
            }

            _logger.LogDebug("Datei validiert, Verarbeitung beginnt");
            var document = await _documentService.UploadDocumentAsync(file);
            
            _logger.LogInformation("Dokument erfolgreich hochgeladen: {DocumentId}", document.Id);
            return Ok(document);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Ungültiges Dateiformat: {FileName}", file?.FileName);
            return BadRequest($"Ungültiges Dateiformat: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "API-Schlüssel-Fehler");
            return Unauthorized("Ungültiger API-Schlüssel");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Netzwerkverbindungsfehler");
            return StatusCode(503, "Service vorübergehend nicht verfügbar");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unerwarteter Fehler aufgetreten");
            return StatusCode(500, "Interner Serverfehler");
        }
    }
}</code></pre>
                    </div>

                    <h3>Leistungsüberwachung</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("search")]
public async Task&lt;ActionResult&lt;IEnumerable&lt;DocumentChunk&gt;&gt;&gt; SearchDocuments(
    [FromQuery] string query, 
    [FromQuery] int maxResults = 10)
{
    var stopwatch = Stopwatch.StartNew();
    _logger.LogInformation("Suche gestartet: {Query}", query);

    try
    {
        var results = await _documentService.SearchDocumentsAsync(query, maxResults);
        stopwatch.Stop();
        
        _logger.LogInformation("Suche abgeschlossen: {Query}, Ergebnisse: {Count}, Dauer: {Duration}ms", 
            query, results.Count(), stopwatch.ElapsedMilliseconds);
        
        return Ok(results);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger.LogError(ex, "Suchfehler: {Query}, Dauer: {Duration}ms", 
            query, stopwatch.ElapsedMilliseconds);
        throw;
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Testing Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Testen</h2>
                    <p>Methoden zum Testen Ihrer SmartRAG-Anwendung.</p>
                    
                    <h3>Unit-Tests</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task UploadDocument_ValidFile_ReturnsDocument()
{
    // Arrange
    var mockFile = new Mock&lt;IFormFile&gt;();
    mockFile.Setup(f => f.FileName).Returns("test.pdf");
    mockFile.Setup(f => f.Length).Returns(1024);
    mockFile.Setup(f => f.ContentType).Returns("application/pdf");
    
    var mockDocumentService = new Mock&lt;IDocumentService&gt;();
    var expectedDocument = new Document { Id = "test-id", FileName = "test.pdf" };
    mockDocumentService.Setup(s => s.UploadDocumentAsync(It.IsAny&lt;IFormFile&gt;()))
                      .ReturnsAsync(expectedDocument);
    
    var controller = new DocumentController(mockDocumentService.Object, Mock.Of&lt;ILogger&lt;DocumentController&gt;&gt;());
    
    // Act
    var result = await controller.UploadDocument(mockFile.Object);
    
    // Assert
    Assert.IsInstanceOf&lt;OkObjectResult&gt;(result);
    var okResult = result as OkObjectResult;
    Assert.AreEqual(expectedDocument, okResult.Value);
}</code></pre>
                    </div>

                    <h3>Integrationstests</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task SearchDocuments_IntegrationTest()
{
    // Arrange
    var host = CreateTestHost();
    using var scope = host.Services.CreateScope();
    var documentService = scope.ServiceProvider.GetRequiredService&lt;IDocumentService&gt;();
    
    // Testdaten laden
    var testFile = CreateTestFile("test-document.txt", "Dies ist ein Testdokument.");
    await documentService.UploadDocumentAsync(testFile);
    
    // Act
    var results = await documentService.SearchDocumentsAsync("test", 5);
    
    // Assert
    Assert.IsNotNull(results);
    Assert.IsTrue(results.Any());
    Assert.IsTrue(results.First().Content.Contains("test"));
}

private IHost CreateTestHost()
{
    return Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            services.AddSmartRAG(context.Configuration, options =>
            {
                options.AIProvider = AIProvider.Anthropic;
                options.StorageProvider = StorageProvider.InMemory; // InMemory für Tests verwenden
            });
        })
        .Build();
}</code></pre>
                    </div>

                    <h3>API-Tests</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task UploadDocument_ApiTest()
{
    // Arrange
    var client = _factory.CreateClient();
    var content = new MultipartFormDataContent();
    var fileContent = new ByteArrayContent(File.ReadAllBytes("test-file.pdf"));
    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
    content.Add(fileContent, "file", "test-file.pdf");
    
    // Act
    var response = await client.PostAsync("/api/document/upload", content);
    
    // Assert
    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    var document = JsonSerializer.Deserialize&lt;Document&gt;(responseContent);
    Assert.IsNotNull(document);
    Assert.AreEqual("test-file.pdf", document.FileName);
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Getting Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Hilfe erhalten</h2>
                    <p>Methoden, um Hilfe bei SmartRAG-Problemen zu erhalten.</p>
                    
                    <h3>GitHub Issues</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-github me-2"></i>GitHub</h4>
                        <p class="mb-0">Melden Sie Probleme auf GitHub und erhalten Sie Community-Unterstützung.</p>
                    </div>

                    <h3>E-Mail-Support</h3>
                    <div class="alert alert-success">
                        <h4><i class="fas fa-envelope me-2"></i>E-Mail</h4>
                        <p class="mb-0">Erhalten Sie direkten E-Mail-Support: <a href="mailto:b.yerlikaya@outlook.com">b.yerlikaya@outlook.com</a></p>
                    </div>

                    <h3>Dokumentation</h3>
                    <ul>
                        <li><a href="{{ site.baseurl }}/de/getting-started">Erste Schritte</a></li>
                        <li><a href="{{ site.baseurl }}/de/configuration">Konfiguration</a></li>
                        <li><a href="{{ site.baseurl }}/de/api-reference">API-Referenz</a></li>
                        <li><a href="{{ site.baseurl }}/de/examples">Beispiele</a></li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Prevention Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Prävention</h2>
                    <p>Methoden zur Vermeidung von Problemen in Ihrer SmartRAG-Anwendung.</p>
                    
                    <h3>Beste Praktiken</h3>
                    <ul>
                        <li><strong>Fehlerbehandlung</strong>: Wickeln Sie alle API-Aufrufe in try-catch-Blöcke ein</li>
                        <li><strong>Protokollierung</strong>: Führen Sie detaillierte Protokollierung durch und konfigurieren Sie Log-Levels angemessen</li>
                        <li><strong>Leistung</strong>: Optimieren Sie Chunk-Größen und verwenden Sie Streaming für große Dateien</li>
                        <li><strong>Sicherheit</strong>: Speichern Sie API-Schlüssel sicher</li>
                        <li><strong>Tests</strong>: Schreiben Sie umfassende Tests und führen Sie sie in CI/CD-Pipelines aus</li>
                    </ul>

                    <h3>Konfigurationsprüfung</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class SmartRAGHealthCheck : IHealthCheck
{
    private readonly IDocumentService _documentService;
    private readonly ILogger&lt;SmartRAGHealthCheck&gt; _logger;

    public SmartRAGHealthCheck(IDocumentService documentService, ILogger&lt;SmartRAGHealthCheck&gt; logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    public async Task&lt;HealthCheckResult&gt; CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Einfache Testabfrage
            var results = await _documentService.SearchDocumentsAsync("health check", 1);
            
            return HealthCheckResult.Healthy("SmartRAG-Service läuft");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SmartRAG-Gesundheitsprüfung fehlgeschlagen");
            return HealthCheckResult.Unhealthy("SmartRAG-Service läuft nicht", ex);
        }
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
