---
layout: default
title: Fehlerbehebung
description: Häufige Probleme und Lösungen für SmartRAG-Implementierung
lang: de
---

<div class="page-content">
    <div class="container">
        <!-- Common Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Häufige Probleme</h2>
                    <p>Häufige Probleme und Lösungen, die Sie bei der Verwendung von SmartRAG antreffen können.</p>

                    <h3>Service-Registrierungsprobleme</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-exclamation-triangle me-2"></i>Warnung</h4>
                        <p class="mb-0">Stellen Sie immer sicher, dass die Service-Registrierung und Dependency Injection korrekt eingerichtet sind.</p>
                    </div>

                    <h4>Service nicht registriert</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Stellen Sie sicher, dass Services korrekt registriert sind
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// Erforderliche Services abrufen
var documentService = serviceProvider.GetRequiredService<IDocumentService>();
var documentSearchService = serviceProvider.GetRequiredService<IDocumentSearchService>();</code></pre>
                    </div>

                    <h4>Konfigurationsprobleme</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Korrekte Konfiguration sicherstellen
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
});</code></pre>
                    </div>

                    <h3>API-Schlüssel-Konfiguration</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Konfiguration</h4>
                        <p class="mb-0">API-Schlüssel sollten in appsettings.json oder Umgebungsvariablen konfiguriert werden.</p>
                    </div>

                    <h4>Umgebungsvariablen</h4>
                    <div class="code-example">
                        <pre><code class="language-bash"># Umgebungsvariablen setzen
export ANTHROPIC_API_KEY=your-anthropic-api-key
export QDRANT_API_KEY=your-qdrant-api-key</code></pre>
                    </div>

                    <h4>appsettings.json Konfiguration</h4>
                    <div class="code-example">
                        <pre><code class="language-json">{
  "SmartRAG": {
    "AIProvider": "Anthropic",
    "StorageProvider": "Qdrant",
    "MaxChunkSize": 1000,
    "ChunkOverlap": 200
  },
  "Anthropic": {
    "ApiKey": "your-anthropic-api-key"
  },
  "Qdrant": {
    "ApiKey": "your-qdrant-api-key"
  }
}</code></pre>

                    <h3>Leistungsprobleme</h3>
                    <div class="alert alert-success">
                        <h4><i class="fas fa-tachometer-alt me-2"></i>Optimierung</h4>
                        <p class="mb-0">Die Leistung kann durch korrekte Konfiguration verbessert werden.</p>
                    </div>

                    <h4>Langsame Dokumentenverarbeitung</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Chunk-Größe für schnellere Verarbeitung optimieren
services.AddSmartRag(configuration, options =>
{
    options.MaxChunkSize = 500; // Kleinere Chunks für schnellere Verarbeitung
    options.MinChunkSize = 50;
    options.ChunkOverlap = 100;
    options.MaxRetryAttempts = 2; // Retries für schnelleres Fehlschlagen reduzieren
});</code></pre>
                    </div>

                    <h4>Speichernutzung-Optimierung</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Passenden Speicheranbieter verwenden
services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.InMemory; // Für kleine Datensätze
    // oder
    options.StorageProvider = StorageProvider.Qdrant; // Für große Datensätze
    options.EnableFallbackProviders = true; // Fallback für Zuverlässigkeit aktivieren
});</code></pre>
                    </div>

                    <h3>Retry-Konfiguration</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Retry-Richtlinien konfigurieren
services.AddSmartRag(configuration, options =>
{
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new[] { AIProvider.Gemini, AIProvider.OpenAI };
});</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Debugging Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Debugging</h2>
                    <p>Tools und Techniken, die Ihnen beim Debuggen von SmartRAG-Anwendungen helfen.</p>

                    <h3>Logging aktivieren</h3>
                    
                    <h4>Logging-Konfiguration</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Logging konfigurieren
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// SmartRAG-spezifisches Logging hinzufügen
builder.Logging.AddFilter("SmartRAG", LogLevel.Debug);</code></pre>
                    </div>

                    <h4>Service-Implementierung</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">private readonly ILogger<DocumentsController> _logger;

public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
{
    _logger.LogInformation("Dokument wird hochgeladen: {FileName}", file.FileName);
    try
    {
        using var stream = file.OpenReadStream();
        var document = await _documentService.UploadDocumentAsync(
            stream, file.FileName, file.ContentType, "user123");
        _logger.LogInformation("Dokument erfolgreich hochgeladen: {DocumentId}", document.Id);
        return Ok(document);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Dokument-Upload fehlgeschlagen: {FileName}", file.FileName);
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>Exception-Behandlung</h3>
                    
                    <h4>Grundlegende Fehlerbehandlung</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">try
{
    using var stream = file.OpenReadStream();
    var document = await _documentService.UploadDocumentAsync(
        stream, file.FileName, file.ContentType, "user123");
    return Ok(document);
}
catch (ArgumentException ex)
{
    _logger.LogError(ex, "Ungültiges Dateiformat: {FileName}", file.FileName);
    return BadRequest("Ungültiges Dateiformat");
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "AI-Provider-Fehler: {Message}", ex.Message);
    return StatusCode(503, "Service vorübergehend nicht verfügbar");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unerwarteter Fehler beim Upload: {FileName}", file.FileName);
    return StatusCode(500, "Interner Serverfehler");
}</code></pre>
                    </div>

                    <h4>Service-Level-Fehlerbehandlung</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy)
{
    try
    {
        _logger.LogInformation("Dokument-Upload beginnt: {FileName}", fileName);
        
        // Eingabe validieren
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("Dateistream ist null oder leer");
        
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Dateiname ist erforderlich");
        
        // Dokument verarbeiten
        var document = await ProcessDocumentAsync(fileStream, fileName, contentType, uploadedBy);
        
        _logger.LogInformation("Dokument erfolgreich hochgeladen: {DocumentId}", document.Id);
        return document;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Dokument-Upload fehlgeschlagen: {FileName}", fileName);
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
                    <p>Wie Sie Ihre SmartRAG-Implementierung testen können.</p>

                    <h3>Unit-Tests</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task UploadDocument_ValidFile_ReturnsDocument()
{
    // Arrange
    var mockDocumentService = new Mock<IDocumentService>();
    var mockDocumentSearchService = new Mock<IDocumentSearchService>();
    var mockLogger = new Mock<ILogger<DocumentsController>>();
    
    var controller = new DocumentsController(
        mockDocumentService.Object, 
        mockDocumentSearchService.Object, 
        mockLogger.Object);
    
    var mockFile = new Mock<IFormFile>();
    mockFile.Setup(f => f.FileName).Returns("test.pdf");
    mockFile.Setup(f => f.ContentType).Returns("application/pdf");
    mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
    
    var expectedDocument = new Document 
    { 
        Id = Guid.NewGuid(), 
        FileName = "test.pdf" 
    };
    
    mockDocumentService.Setup(s => s.UploadDocumentAsync(
        It.IsAny<Stream>(), 
        It.IsAny<string>(), 
        It.IsAny<string>(), 
        It.IsAny<string>()))
        .ReturnsAsync(expectedDocument);
    
    // Act
    var result = await controller.UploadDocument(mockFile.Object);
    
    // Assert
    var okResult = result as OkObjectResult;
    Assert.IsNotNull(okResult);
    Assert.AreEqual(expectedDocument, okResult.Value);
}</code></pre>
                    </div>

                    <h3>Integrationstests</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task SearchDocuments_ReturnsRelevantResults()
{
    // Arrange
    var mockDocumentSearchService = new Mock<IDocumentSearchService>();
    var mockDocumentService = new Mock<IDocumentService>();
    var mockLogger = new Mock<ILogger<DocumentsController>>();
    
    var controller = new DocumentsController(
        mockDocumentService.Object,
        mockDocumentSearchService.Object,
        mockLogger.Object);
    
    var testQuery = "test query";
    var expectedResults = new List<DocumentChunk>
    {
        new DocumentChunk { Content = "Test-Inhalt 1" },
        new DocumentChunk { Content = "Test-Inhalt 2" }
    };
    
    mockDocumentSearchService.Setup(s => s.SearchDocumentsAsync(testQuery, 10))
        .ReturnsAsync(expectedResults);
    
    // Act
    var result = await controller.SearchDocuments(testQuery);
    
    // Assert
    var okResult = result as OkObjectResult;
    Assert.IsNotNull(okResult);
    var results = okResult.Value as IEnumerable<DocumentChunk>;
    Assert.IsNotNull(results);
    Assert.AreEqual(expectedResults.Count, results.Count());
}</code></pre>
                    </div>

                    <h3>End-to-End-Tests</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task CompleteWorkflow_UploadSearchChat_WorksCorrectly()
{
    // Arrange
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
        
    var services = new ServiceCollection();
    services.AddSmartRag(configuration, options =>
    {
        options.AIProvider = AIProvider.Anthropic;
        options.StorageProvider = StorageProvider.InMemory;
        options.MaxChunkSize = 1000;
        options.ChunkOverlap = 200;
    });
    
    var serviceProvider = services.BuildServiceProvider();
    var documentService = serviceProvider.GetRequiredService<IDocumentService>();
    var documentSearchService = serviceProvider.GetRequiredService<IDocumentSearchService>();
    
    // Testdatei erstellen
    var testContent = "Dies ist ein Testdokument über künstliche Intelligenz.";
    var testStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testContent));
    
    // Act - Upload
    var document = await documentService.UploadDocumentAsync(
        testStream, "test.txt", "text/plain", "test-user");
    
    // Assert - Upload
    Assert.IsNotNull(document);
    Assert.AreEqual("test.txt", document.FileName);
    
    // Act - Suche
    var searchResults = await documentSearchService.SearchDocumentsAsync("künstliche Intelligenz", 5);
    
    // Assert - Suche
    Assert.IsNotNull(searchResults);
    Assert.IsTrue(searchResults.Count > 0);
    
    // Act - Chat
    var chatResponse = await documentSearchService.GenerateRagAnswerAsync("Worum geht es in diesem Dokument?", 5);
    
    // Assert - Chat
    Assert.IsNotNull(chatResponse);
    Assert.IsFalse(string.IsNullOrWhiteSpace(chatResponse.Answer));
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Getting Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Hilfe bekommen</h2>
                    <p>Wenn Sie immer noch Probleme haben, hier ist, wie Sie Hilfe bekommen können.</p>

                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-github me-2"></i>GitHub Issues</h4>
                                <p class="mb-0">Melden Sie Bugs und fordern Sie Features auf GitHub an.</p>
                                <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank" class="btn btn-sm btn-outline-info mt-2">Issue öffnen</a>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-envelope me-2"></i>E-Mail-Support</h4>
                                <p class="mb-0">Erhalten Sie direkte Hilfe per E-Mail.</p>
                                <a href="mailto:b.yerlikaya@outlook.com" class="btn btn-sm btn-outline-success mt-2">Kontakt</a>
                            </div>
                        </div>
                    </div>

                    <h3>Bevor Sie um Hilfe bitten</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-list me-2"></i>Checkliste</h4>
                        <ul class="mb-0">
                            <li>Überprüfen Sie die <a href="{{ site.baseurl }}/de/getting-started">Erste Schritte</a>-Anleitung</li>
                            <li>Überprüfen Sie die <a href="{{ site.baseurl }}/de/configuration">Konfigurations</a>-Dokumentation</li>
                            <li>Durchsuchen Sie bestehende <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub Issues</a></li>
                            <li>Fügen Sie Fehlermeldungen und Konfigurationsdetails hinzu</li>
                            <li>Überprüfen Sie die <a href="{{ site.baseurl }}/de/api-reference">API-Referenz</a> für korrekte Methodensignaturen</li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>

        <!-- Prevention Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Prävention</h2>
                    <p>Best Practices, um häufige Probleme zu vermeiden.</p>

                    <h3>Konfigurations-Best Practices</h3>
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="alert alert-primary">
                                <h4><i class="fas fa-key me-2"></i>API-Schlüssel</h4>
                                <p class="mb-0">API-Schlüssel niemals hardcoden. Verwenden Sie Umgebungsvariablen oder sichere Konfiguration.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-database me-2"></i>Speicher</h4>
                                <p class="mb-0">Wählen Sie den richtigen Speicheranbieter für Ihren Anwendungsfall.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-shield-alt me-2"></i>Fehlerbehandlung</h4>
                                <p class="mb-0">Implementieren Sie ordnungsgemäße Fehlerbehandlung und Logging.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-warning">
                                <h4><i class="fas fa-balance-scale me-2"></i>Leistung</h4>
                                <p class="mb-0">Überwachen Sie die Leistung und optimieren Sie Chunk-Größen.</p>
                            </div>
                        </div>
                    </div>

                    <h3>Entwicklungs- vs Produktionskonfiguration</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Entwicklungs-Konfiguration
if (builder.Environment.IsDevelopment())
{
    services.AddSmartRag(configuration, options =>
    {
        options.AIProvider = AIProvider.Gemini; // Kostenlose Stufe für Entwicklung
        options.StorageProvider = StorageProvider.InMemory; // Schnell für Entwicklung
        options.MaxChunkSize = 500;
        options.ChunkOverlap = 100;
        options.MaxRetryAttempts = 1; // Schnelles Fehlschlagen in Entwicklung
    });
}
else
{
    // Produktions-Konfiguration
    services.AddSmartRag(configuration, options =>
    {
        options.AIProvider = AIProvider.Anthropic; // Bessere Qualität für Produktion
        options.StorageProvider = StorageProvider.Qdrant; // Persistenter Speicher
        options.MaxChunkSize = 1000;
        options.ChunkOverlap = 200;
        options.MaxRetryAttempts = 3;
        options.RetryDelayMs = 1000;
        options.RetryPolicy = RetryPolicy.ExponentialBackoff;
        options.EnableFallbackProviders = true;
    });
}</code></pre>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
