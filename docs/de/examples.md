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
                    <h2>Schnelle Beispiele</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Starten Sie in Minuten mit SmartRAG.</p>
                    
                    <h3>Grundlegende Verwendung</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// 1. Dokument hochladen
var document = await _documentService.UploadDocumentAsync(file);

// 2. Dokumente durchsuchen
var results = await _searchService.SearchDocumentsAsync(query, 10);

// 3. RAG-Antwort mit Gesprächshistorie generieren
var response = await _searchService.GenerateRagAnswerAsync(question);</code></pre>
                    </div>

                    <h3>Controller-Beispiel</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    [HttpPost("search")]
    public async Task<ActionResult> Search([FromBody] SearchRequest request)
    {
        var response = await _searchService.GenerateRagAnswerAsync(
            request.Query, request.MaxResults);
        return Ok(response);
    }
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 5;
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Advanced Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Erweiterte Verwendung</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Erweiterte Beispiele für Produktionsumgebungen.</p>
                    
                    <h3>Batch-Verarbeitung</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Mehrere Dokumente hochladen
var documents = await _documentService.UploadDocumentsAsync(files);

// Speicherstatistiken abrufen
var stats = await _documentService.GetStorageStatisticsAsync();

// Dokumente verwalten
var allDocs = await _documentService.GetAllDocumentsAsync();
await _documentService.DeleteDocumentAsync(documentId);</code></pre>
                    </div>

                    <h3>Wartungsoperationen</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Embeddings regenerieren
await _documentService.RegenerateAllEmbeddingsAsync();

// Daten löschen
await _documentService.ClearAllEmbeddingsAsync();
await _documentService.ClearAllDocumentsAsync();</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Konfiguration</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Konfigurieren Sie SmartRAG für Ihre Bedürfnisse.</p>
                    
                    <h3>Service-Registrierung</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
services.AddSmartRag(configuration, options =>
                    {
                        options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});</code></pre>
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
                        <p class="mb-0">Wenn Sie Hilfe mit Beispielen benötigen:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/de/getting-started">Erste Schritte</a></li>
                            <li><a href="{{ site.baseurl }}/de/api-reference">API-Referenz</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub-Issue erstellen</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">E-Mail-Support kontaktieren</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>