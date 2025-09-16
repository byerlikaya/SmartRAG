---
layout: default
title: Fehlerbehebung
description: Häufige Probleme und Lösungen für SmartRAG
lang: de
---

<div class="page-content">
    <div class="container">
        <!-- Common Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Häufige Probleme</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Schnelle Lösungen für häufige Probleme.</p>
                    
                    <h3>Konfigurationsprobleme</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Ungültiger API-Schlüssel</h5>
                        <p><strong>Problem:</strong> "Unauthorized" oder "Invalid API key" Fehler</p>
                        <p><strong>Lösung:</strong> Überprüfen Sie Ihre API-Schlüssel in appsettings.json</p>
                    </div>
                    
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Fehlende Konfiguration</h5>
                        <p><strong>Problem:</strong> "Configuration not found" Fehler</p>
                        <p><strong>Lösung:</strong> Stellen Sie sicher, dass der SmartRAG-Bereich in appsettings.json existiert</p>
                    </div>

                    <h3>Service-Registrierungsprobleme</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Service nicht registriert</h5>
                        <p><strong>Problem:</strong> "Unable to resolve service" Fehler</p>
                        <p><strong>Lösung:</strong> Fügen Sie SmartRAG-Services in Program.cs hinzu:</p>
                        <div class="code-example">
                            <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
});</code></pre>
                        </div>
                    </div>

                    <h3>Audio-Verarbeitungsprobleme</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Google Speech-to-Text Fehler</h5>
                        <p><strong>Problem:</strong> Audio-Transkription schlägt fehl</p>
                        <p><strong>Lösung:</strong> Überprüfen Sie den Google API-Schlüssel und das unterstützte Audio-Format</p>
                    </div>

                    <h3>Speicherprobleme</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Redis-Verbindung fehlgeschlagen</h5>
                        <p><strong>Problem:</strong> Kann nicht mit Redis verbinden</p>
                        <p><strong>Lösung:</strong> Überprüfen Sie die Redis-Verbindungszeichenfolge und stellen Sie sicher, dass Redis läuft</p>
                    </div>
                    
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Qdrant-Verbindung fehlgeschlagen</h5>
                        <p><strong>Problem:</strong> Kann nicht mit Qdrant verbinden</p>
                        <p><strong>Lösung:</strong> Überprüfen Sie die Qdrant-Host- und API-Schlüssel-Konfiguration</p>
                    </div>
                </div>
            </div>
        </section>

        <!-- Performance Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Leistungsprobleme</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Optimieren Sie die SmartRAG-Leistung.</p>
                    
                    <h3>Langsame Dokumentenverarbeitung</h3>
                    <div class="alert alert-info">
                        <h5><i class="fas fa-info-circle me-2"></i>Optimierungstipps</h5>
                        <ul class="mb-0">
                            <li>Verwenden Sie angemessene Chunk-Größen (500-1000 Zeichen)</li>
                            <li>Aktivieren Sie Redis-Caching für bessere Leistung</li>
                            <li>Verwenden Sie Qdrant für Produktionsvektor-Speicherung</li>
                            <li>Verarbeiten Sie Dokumente in Batches</li>
                        </ul>
                    </div>

                    <h3>Speicherprobleme</h3>
                    <div class="alert alert-info">
                        <h5><i class="fas fa-info-circle me-2"></i>Speicherverwaltung</h5>
                        <ul class="mb-0">
                            <li>Begrenzen Sie die Dokumentengröße für die Verarbeitung</li>
                            <li>Verwenden Sie Streaming für große Dateien</li>
                            <li>Löschen Sie den Embeddings-Cache regelmäßig</li>
                            <li>Überwachen Sie die Speichernutzung in der Produktion</li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>

        <!-- Debugging Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Debugging</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Aktivieren Sie Logging und Debugging.</p>
                    
                    <h3>Debug-Logging aktivieren</h3>
                    <div class="code-example">
                        <pre><code class="language-json">{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SmartRAG": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}</code></pre>
                    </div>

                    <h3>Service-Status überprüfen</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Überprüfen, ob Services registriert sind
var serviceProvider = services.BuildServiceProvider();
var documentService = serviceProvider.GetService<IDocumentService>();
var searchService = serviceProvider.GetService<IDocumentSearchService>();

if (documentService == null || searchService == null)
{
    Console.WriteLine("SmartRAG-Services nicht ordnungsgemäß registriert!");
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
                        <h4><i class="fas fa-question-circle me-2"></i>Benötigen Sie noch Hilfe?</h4>
                        <p class="mb-0">Wenn Sie keine Lösung finden können:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/de/getting-started">Erste Schritte</a></li>
                            <li><a href="{{ site.baseurl }}/de/configuration">Konfigurationsanleitung</a></li>
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