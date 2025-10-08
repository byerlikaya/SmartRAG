---
layout: default
title: API-Referenz
description: Vollständige API-Dokumentation für SmartRAG-Services und -Schnittstellen
lang: de
---

<div class="page-content">
    <div class="container">
        <!-- Core Interfaces Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Kern-Schnittstellen</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>SmartRAG bietet mehrere Kern-Schnittstellen für die Dokumentenverarbeitung und -verwaltung.</p>
                    
                    <h3>IDocumentSearchService</h3>
                    <p>Dokumentsuche und KI-gestützte Antwortgenerierung mit RAG (Retrieval-Augmented Generation).</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentSearchService
{
    Task&lt;List&lt;DocumentChunk&gt;&gt; SearchDocumentsAsync(string query, int maxResults = 5);
    Task&lt;RagResponse&gt; GenerateRagAnswerAsync(string query, int maxResults = 5, bool startNewConversation = false);
}</code></pre>
                    </div>

                    <h4>GenerateRagAnswerAsync</h4>
                    <p>Generiert KI-gestützte Antworten mit automatischer Sitzungsverwaltung und Gesprächshistorie.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">Task&lt;RagResponse&gt; GenerateRagAnswerAsync(string query, int maxResults = 5, bool startNewConversation = false)</code></pre>
                    </div>

                    <p><strong>Parameter:</strong></p>
                    <ul>
                        <li><code>query</code> (string): Die Frage des Benutzers</li>
                        <li><code>maxResults</code> (int): Maximale Anzahl der abzurufenden Dokumentfragmente (Standard: 5)</li>
                        <li><code>startNewConversation</code> (bool): Neue Gesprächssitzung starten (Standard: false)</li>
                    </ul>
                    
                    <p><strong>Gibt zurück:</strong> <code>RagResponse</code> mit KI-Antwort, Quellen und Metadaten</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Grundlegende Verwendung
var response = await documentSearchService.GenerateRagAnswerAsync("Wie ist das Wetter?");

// Neues Gespräch starten
var response = await documentSearchService.GenerateRagAnswerAsync("/new");</code></pre>
                    </div>

                    <h3>Weitere wichtige Schnittstellen</h3>
                    <p>Zusätzliche Services für Dokumentenverarbeitung und -speicherung.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Dokumentenverarbeitung und -analyse
IDocumentParserService - Dokumente analysieren und Text extrahieren
IDocumentRepository - Dokumentenspeicher-Operationen
IAIService - KI-Anbieter-Kommunikation
IAudioParserService - Audio-Transkription (Google Speech-to-Text)</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Models Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Hauptmodelle</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Wesentliche Datenmodelle für SmartRAG-Operationen.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Hauptantwortmodell
public class RagResponse
{
    public string Query { get; set; }
    public string Answer { get; set; }
    public List&lt;SearchSource&gt; Sources { get; set; }
    public DateTime SearchedAt { get; set; }
}

// Dokumentfragment für Suchergebnisse
public class DocumentChunk
{
    public string Id { get; set; }
    public string DocumentId { get; set; }
    public string Content { get; set; }
    public double RelevanceScore { get; set; }
}</code></pre>
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
                    <p>Wichtige Konfigurationsoptionen für SmartRAG.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// KI-Anbieter
AIProvider.Anthropic    // Claude-Modelle
AIProvider.OpenAI       // GPT-Modelle
AIProvider.Gemini       // Google-Modelle

// Speicheranbieter  
StorageProvider.Qdrant  // Vektordatenbank
StorageProvider.Redis   // Hochleistungs-Cache
StorageProvider.Sqlite  // Lokale Datenbank</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Quick Start Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Schnellstart</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Starten Sie in Minuten mit SmartRAG.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// 1. Services registrieren
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
});

// 2. Injizieren und verwenden
public class MyController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    public async Task<ActionResult> Ask(string question)
    {
        var response = await _searchService.GenerateRagAnswerAsync(question);
        return Ok(response);
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Common Patterns Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Häufige Muster</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Häufig verwendete Muster und Konfigurationen.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Dokument hochladen
        var document = await _documentService.UploadDocumentAsync(file);

// Dokumente durchsuchen  
var results = await _searchService.SearchDocumentsAsync(query, 10);

// RAG-Gespräch
var response = await _searchService.GenerateRagAnswerAsync(question);

// Konfiguration
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
});</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Error Handling Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Fehlerbehandlung</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Häufige Ausnahmen und Fehlerbehandlungsmuster.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">try
{
    var response = await _searchService.GenerateRagAnswerAsync(query);
    return Ok(response);
}
catch (SmartRagException ex)
{
    return BadRequest(ex.Message);
}
catch (Exception ex)
{
    return StatusCode(500, "Internal server error");
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Performance Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Leistungstipps</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Optimieren Sie die SmartRAG-Leistung mit diesen Tipps.</p>
                    
                    <div class="alert alert-info">
                        <ul class="mb-0">
                            <li><strong>Chunk-Größe</strong>: 500-1000 Zeichen für optimales Gleichgewicht</li>
                            <li><strong>Batch-Operationen</strong>: Verarbeiten Sie mehrere Dokumente zusammen</li>
                            <li><strong>Caching</strong>: Verwenden Sie Redis für bessere Leistung</li>
                            <li><strong>Vektorspeicher</strong>: Qdrant für Produktionsumgebungen</li>
                    </ul>
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
                        <p class="mb-0">Wenn Sie Hilfe mit der API benötigen:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/de/getting-started">Erste Schritte</a></li>
                            <li><a href="{{ site.baseurl }}/de/configuration">Konfigurationsoptionen</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub-Issue erstellen</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">E-Mail-Support kontaktieren</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>