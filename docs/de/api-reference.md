---
layout: default
title: API-Referenz
description: Vollständige API-Dokumentation für SmartRAG-Dienste und -Schnittstellen
lang: de
---

<div class="page-content">
    <div class="container">
        <!-- Core Interfaces Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Kern-Interfaces</h2>
                    <p>Die grundlegenden Interfaces und Services von SmartRAG.</p>
                    
                    <h3>IDocumentService</h3>
                    <p>Hauptservice-Interface für Dokumentoperationen.</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentService
{
    Task&lt;Document&gt; UploadDocumentAsync(IFormFile file);
    Task&lt;Document&gt; GetDocumentByIdAsync(string id);
    Task&lt;IEnumerable&lt;Document&gt;&gt; GetAllDocumentsAsync();
    Task&lt;bool&gt; DeleteDocumentAsync(string id);
    Task&lt;IEnumerable&lt;DocumentChunk&gt;&gt; SearchDocumentsAsync(string query, int maxResults = 5);
    Task&lt;RagResponse&gt; GenerateRagAnswerAsync(string query, int maxResults = 5);
}</code></pre>
                    </div>

                    <h3>IDocumentParserService</h3>
                    <p>Service zum Parsen verschiedener Dateiformate.</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentParserService
{
    Task&lt;string&gt; ParseDocumentAsync(IFormFile file);
    bool CanParse(string fileName);
    Task&lt;IEnumerable&lt;string&gt;&gt; ChunkTextAsync(string text, int chunkSize = 1000, int overlap = 200);
}</code></pre>
                    </div>

                    <h3>IDocumentSearchService</h3>
                    <p>Service für Dokumentensuche und RAG-Operationen.</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentSearchService
{
    Task&lt;List&lt;DocumentChunk&gt;&gt; SearchDocumentsAsync(string query, int maxResults = 5);
    Task&lt;RagResponse&gt; GenerateRagAnswerAsync(string query, int maxResults = 5);
}</code></pre>
                    </div>

                    <h3>IAIService</h3>
                    <p>Service für die Interaktion mit AI-Providern.</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IAIService
{
    Task&lt;float[]&gt; GenerateEmbeddingAsync(string text);
    Task&lt;string&gt; GenerateTextAsync(string prompt);
    Task&lt;string&gt; GenerateTextAsync(string prompt, string context);
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Models Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Modelle</h2>
                    <p>Die grundlegenden Datenmodelle, die in SmartRAG verwendet werden.</p>
                    
                    <h3>Document</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class Document
{
    public string Id { get; set; }
    public string FileName { get; set; }
    public string Content { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public List&lt;DocumentChunk&gt; Chunks { get; set; }
}</code></pre>
                    </div>

                    <h3>DocumentChunk</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class DocumentChunk
{
    public string Id { get; set; }
    public string DocumentId { get; set; }
    public string Content { get; set; }
    public float[] Embedding { get; set; }
    public int ChunkIndex { get; set; }
    public DateTime CreatedAt { get; set; }
}</code></pre>
                    </div>

                    <h3>RagResponse</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class RagResponse
{
    public string Answer { get; set; }
    public List&lt;SearchSource&gt; Sources { get; set; }
    public DateTime SearchedAt { get; set; }
    public RagConfiguration Configuration { get; set; }
}</code></pre>
                    </div>

                    <h3>SearchSource</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class SearchSource
{
    public string DocumentId { get; set; }
    public string DocumentName { get; set; }
    public string Content { get; set; }
    public float SimilarityScore { get; set; }
    public int ChunkIndex { get; set; }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Enums Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Enums</h2>
                    <p>Die in SmartRAG verwendeten Enum-Werte.</p>
                    
                    <h3>AIProvider</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public enum AIProvider
{
    OpenAI,
    Anthropic,
    Gemini,
    AzureOpenAI,
    Custom
}</code></pre>
                    </div>

                    <h3>StorageProvider</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public enum StorageProvider
{
    Qdrant,
    Redis,
    SQLite,
    InMemory,
    FileSystem
}</code></pre>
                    </div>

                    <h3>RetryPolicy</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public enum RetryPolicy
{
    None,
    FixedDelay,
    ExponentialBackoff,
    LinearBackoff
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Service Registration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Service-Registrierung</h2>
                    <p>Wie Sie SmartRAG-Services in Ihrer Anwendung registrieren.</p>
                    
                    <h3>Grundlegende Registrierung</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs oder Startup.cs
services.AddSmartRAG(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
});</code></pre>
                    </div>

                    <h3>Erweiterte Konfiguration</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Redis;
    options.MaxChunkSize = 1500;
    options.MinChunkSize = 100;
    options.ChunkOverlap = 300;
    options.MaxRetryAttempts = 5;
    options.RetryDelayMs = 2000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List&lt;AIProvider&gt; 
    { 
        AIProvider.Anthropic, 
        AIProvider.Gemini 
    };
});</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Usage Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Verwendungsbeispiele</h2>
                    <p>Wie Sie die SmartRAG-API verwenden.</p>
                    
                    <h3>Dokument hochladen</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("upload")]
public async Task&lt;ActionResult&lt;Document&gt;&gt; UploadDocument(IFormFile file)
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
}</code></pre>
                    </div>

                    <h3>Dokumente suchen</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpGet("search")]
public async Task&lt;ActionResult&lt;IEnumerable&lt;DocumentChunk&gt;&gt;&gt; SearchDocuments(
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
}</code></pre>
                    </div>

                    <h3>RAG-Antwort generieren</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("ask")]
public async Task&lt;ActionResult&lt;RagResponse&gt;&gt; AskQuestion([FromBody] string question)
{
    try
    {
        var response = await _documentService.GenerateRagAnswerAsync(question, 5);
        return Ok(response);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Error Handling Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Fehlerbehandlung</h2>
                    <p>Fehlerbehandlung und Ausnahmetypen in SmartRAG.</p>
                    
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-exclamation-triangle me-2"></i>Wichtig</h4>
                        <p class="mb-0">Alle SmartRAG-Services sind mit angemessener Fehlerbehandlung entworfen. Wickeln Sie Ihre API-Aufrufe in try-catch-Blöcke ein.</p>
                    </div>

                    <h3>Häufige Fehler</h3>
                    <ul>
                        <li><strong>ArgumentException</strong>: Ungültige Parameter</li>
                        <li><strong>FileNotFoundException</strong>: Datei nicht gefunden</li>
                        <li><strong>UnauthorizedAccessException</strong>: Ungültiger API-Schlüssel</li>
                        <li><strong>HttpRequestException</strong>: Netzwerkverbindungsprobleme</li>
                        <li><strong>TimeoutException</strong>: Anforderungs-Timeout</li>
                    </ul>

                    <h3>Fehlerbehandlungsbeispiel</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">try
{
    var document = await _documentService.UploadDocumentAsync(file);
    return Ok(document);
}
catch (ArgumentException ex)
{
    _logger.LogWarning("Invalid argument: {Message}", ex.Message);
    return BadRequest("Invalid file or parameters");
}
catch (UnauthorizedAccessException ex)
{
    _logger.LogError("Authentication failed: {Message}", ex.Message);
    return Unauthorized("Invalid API key");
}
catch (HttpRequestException ex)
{
    _logger.LogError("Network error: {Message}", ex.Message);
    return StatusCode(503, "Service temporarily unavailable");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error occurred");
    return StatusCode(500, "Internal server error");
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Logging Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Protokollierung</h2>
                    <p>Protokollierung und Überwachung in SmartRAG.</p>
                    
                    <h3>Protokollstufen</h3>
                    <ul>
                        <li><strong>Information</strong>: Normale Operationen</li>
                        <li><strong>Warning</strong>: Warnungen und unerwartete Situationen</li>
                        <li><strong>Error</strong>: Fehler und Ausnahmen</li>
                        <li><strong>Debug</strong>: Detaillierte Debugging-Informationen</li>
                    </ul>

                    <h3>Protokollierungskonfiguration</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SmartRAG": "Debug",
      "Microsoft": "Warning"
    }
  }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Performance Considerations Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Leistungsüberlegungen</h2>
                    <p>Leistungsoptimierung in SmartRAG.</p>
                    
                    <h3>Empfohlene Einstellungen</h3>
                    <ul>
                        <li><strong>Chunk Size</strong>: 1000-1500 Zeichen</li>
                        <li><strong>Chunk Overlap</strong>: 200-300 Zeichen</li>
                        <li><strong>Max Results</strong>: 5-10 Ergebnisse</li>
                        <li><strong>Retry Attempts</strong>: 3-5 Versuche</li>
                    </ul>

                    <h3>Leistungstipps</h3>
                    <ul>
                        <li>Verarbeiten Sie große Dateien im Voraus</li>
                        <li>Verwenden Sie angemessene Chunk-Größen</li>
                        <li>Aktivieren Sie Cache-Mechanismen</li>
                        <li>Bevorzugen Sie asynchrone Operationen</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Benötigen Sie Hilfe?</h4>
                        <p class="mb-0">Für Fragen zur API:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/de/getting-started">Erste Schritte</a></li>
                            <li><a href="{{ site.baseurl }}/de/examples">Beispiele</a></li>
                            <li><a href="{{ site.baseurl }}/de/configuration">Konfiguration</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub Issue erstellen</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">E-Mail-Support</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>