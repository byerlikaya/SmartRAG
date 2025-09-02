---
layout: default
title: Fehlerbehebung
description: Häufige Probleme und Lösungen für SmartRAG
lang: de
---

<!-- Page Header -->
<div class="page-header">
    <div class="container">
        <h1 class="page-title">Fehlerbehebung</h1>
        <p class="page-description">
            Lösungen für häufige Probleme, die Sie bei der Verwendung von SmartRAG begegnen können
        </p>
    </div>
</div>

<!-- Main Content -->
<div class="main-content">
    <div class="container">
        
        <!-- Quick Navigation -->
        <div class="content-section">
            <div class="row">
                <div class="col-12">
                    <div class="alert alert-info" role="alert">
                        <i class="fas fa-info-circle me-2"></i>
                        <strong>Brauchen Sie Hilfe?</strong> Wenn Sie hier keine Lösung finden, schauen Sie in unseren 
                        <a href="{{ site.baseurl }}/de/getting-started" class="alert-link">Erste Schritte</a> Guide 
                        oder erstellen Sie ein Issue auf <a href="https://github.com/byerlikaya/SmartRAG" class="alert-link" target="_blank">GitHub</a>.
                    </div>
                </div>
            </div>
        </div>

        <!-- Configuration Issues -->
        <div class="content-section">
            <h2>Konfigurationsprobleme</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-key"></i>
                        </div>
                        <h3>API-Schlüssel-Konfiguration</h3>
                        <p><strong>Problem:</strong> Authentifizierungsfehler mit AI- oder Speicheranbietern.</p>
                        <p><strong>Lösung:</strong> Stellen Sie sicher, dass Ihre API-Schlüssel in <code>appsettings.json</code> korrekt konfiguriert sind:</p>
                        
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
                        </div>
                        
                        <p>Oder setzen Sie Umgebungsvariablen:</p>
                        <div class="code-example">
                            <pre><code class="language-bash"># Umgebungsvariablen setzen
export ANTHROPIC_API_KEY=your-anthropic-api-key
export QDRANT_API_KEY=your-qdrant-api-key</code></pre>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-cogs"></i>
                        </div>
                        <h3>Service-Registrierungsprobleme</h3>
                        <p><strong>Problem:</strong> Dependency Injection-Fehler.</p>
                        <p><strong>Lösung:</strong> Stellen Sie sicher, dass SmartRAG-Services in Ihrer <code>Program.cs</code> korrekt registriert sind:</p>
                        
                        <div class="code-example">
                            <pre><code class="language-csharp">using SmartRAG.Extensions;

var builder = WebApplication.CreateBuilder(args);

// SmartRAG-Services hinzufügen
builder.Services.AddSmartRag(builder.Configuration);

var app = builder.Build();
app.UseSmartRag(builder.Configuration, StorageProvider.Qdrant, AIProvider.Anthropic);
app.Run();</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Document Upload Issues -->
        <div class="content-section">
            <h2>Dokument-Upload-Probleme</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-file-upload"></i>
                        </div>
                        <h3>Dateigrößenbeschränkungen</h3>
                        <p><strong>Problem:</strong> Große Dokumente können nicht hochgeladen oder verarbeitet werden.</p>
                        <p><strong>Lösungen:</strong></p>
                        <ul>
                            <li>Überprüfen Sie die Dateigrößenlimits Ihrer Anwendung in <code>appsettings.json</code></li>
                            <li>Erwägen Sie, große Dokumente in kleinere Chunks zu teilen</li>
                            <li>Stellen Sie sicher, dass genügend Speicher für die Verarbeitung verfügbar ist</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-file-alt"></i>
                        </div>
                        <h3>Nicht unterstützte Dateitypen</h3>
                        <p><strong>Problem:</strong> Fehler bei bestimmten Dateiformaten.</p>
                        <p><strong>Lösung:</strong> SmartRAG unterstützt gängige Textformate:</p>
                        <ul>
                            <li>PDF-Dateien (.pdf)</li>
                            <li>Textdateien (.txt)</li>
                            <li>Word-Dokumente (.docx)</li>
                            <li>Markdown-Dateien (.md)</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Search and Retrieval Issues -->
        <div class="content-section">
            <h2>Such- und Abrufprobleme</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-search"></i>
                        </div>
                        <h3>Keine Suchergebnisse</h3>
                        <p><strong>Problem:</strong> Suchanfragen liefern keine Ergebnisse.</p>
                        <p><strong>Mögliche Lösungen:</strong></p>
                        <ol>
                            <li><strong>Dokument-Upload prüfen:</strong> Stellen Sie sicher, dass Dokumente erfolgreich hochgeladen wurden</li>
                            <li><strong>Embeddings verifizieren:</strong> Prüfen Sie, ob Embeddings korrekt generiert wurden</li>
                            <li><strong>Abfragespezifität:</strong> Versuchen Sie spezifischere Suchbegriffe</li>
                            <li><strong>Speicherverbindung:</strong> Verifizieren Sie, dass Ihr Speicheranbieter zugänglich ist</li>
                        </ol>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-chart-line"></i>
                        </div>
                        <h3>Schlechte Suchqualität</h3>
                        <p><strong>Problem:</strong> Suchergebnisse sind nicht relevant.</p>
                        <p><strong>Lösungen:</strong></p>
                        <ul>
                            <li>Passen Sie <code>MaxChunkSize</code> und <code>ChunkOverlap</code> Einstellungen an</li>
                            <li>Verwenden Sie spezifischere Suchanfragen</li>
                            <li>Stellen Sie sicher, dass Dokumente korrekt formatiert sind</li>
                            <li>Prüfen Sie, ob Embeddings aktuell sind</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Performance Issues -->
        <div class="content-section">
            <h2>Leistungsprobleme</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-tachometer-alt"></i>
                        </div>
                        <h3>Langsame Dokumentverarbeitung</h3>
                        <p><strong>Problem:</strong> Dokument-Upload und -Verarbeitung dauert zu lange.</p>
                        <p><strong>Lösungen:</strong></p>
                        <ul>
                            <li>Erhöhen Sie <code>MaxChunkSize</code>, um die Anzahl der Chunks zu reduzieren</li>
                            <li>Verwenden Sie einen leistungsstärkeren AI-Anbieter</li>
                            <li>Optimieren Sie Ihre Speicheranbieter-Konfiguration</li>
                            <li>Erwägen Sie async-Operationen in Ihrer gesamten Anwendung</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-memory"></i>
                        </div>
                        <h3>Speicherprobleme</h3>
                        <p><strong>Problem:</strong> Anwendung läuft während der Verarbeitung aus dem Speicher.</p>
                        <p><strong>Lösungen:</strong></p>
                        <ul>
                            <li>Reduzieren Sie <code>MaxChunkSize</code>, um kleinere Chunks zu erstellen</li>
                            <li>Verarbeiten Sie Dokumente in Batches</li>
                            <li>Überwachen Sie die Speichernutzung und optimieren Sie entsprechend</li>
                            <li>Erwägen Sie Streaming-Operationen für große Dateien</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Storage Provider Issues -->
        <div class="content-section">
            <h2>Speicheranbieter-Probleme</h2>
            
            <div class="row g-4">
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-database"></i>
                        </div>
                        <h3>Qdrant-Verbindungsprobleme</h3>
                        <p><strong>Problem:</strong> Kann nicht zu Qdrant verbinden.</p>
                        <p><strong>Lösungen:</strong></p>
                        <ul>
                            <li>Verifizieren Sie, dass der Qdrant API-Schlüssel korrekt ist</li>
                            <li>Prüfen Sie die Netzwerkverbindung zum Qdrant-Service</li>
                            <li>Stellen Sie sicher, dass der Qdrant-Service läuft und zugänglich ist</li>
                            <li>Überprüfen Sie Firewall-Einstellungen</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-redis"></i>
                        </div>
                        <h3>Redis-Verbindungsprobleme</h3>
                        <p><strong>Problem:</strong> Kann nicht zu Redis verbinden.</p>
                        <p><strong>Lösungen:</strong></p>
                        <ul>
                            <li>Verifizieren Sie die Redis-Verbindungszeichenkette</li>
                            <li>Stellen Sie sicher, dass der Redis-Server läuft</li>
                            <li>Prüfen Sie die Netzwerkverbindung</li>
                            <li>Verifizieren Sie die Redis-Konfiguration in <code>appsettings.json</code></li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-hdd"></i>
                        </div>
                        <h3>SQLite-Probleme</h3>
                        <p><strong>Problem:</strong> SQLite-Datenbankfehler.</p>
                        <p><strong>Lösungen:</strong></p>
                        <ul>
                            <li>Prüfen Sie Dateiberechtigungen für das Datenbankverzeichnis</li>
                            <li>Stellen Sie sicher, dass genügend Festplattenspeicher vorhanden ist</li>
                            <li>Verifizieren Sie, dass der Datenbankdateipfad korrekt ist</li>
                            <li>Prüfen Sie auf Datenbankkorruption</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- AI Provider Issues -->
        <div class="content-section">
            <h2>AI-Anbieter-Probleme</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-robot"></i>
                        </div>
                        <h3>Anthropic API-Fehler</h3>
                        <p><strong>Problem:</strong> Fehler von der Anthropic API.</p>
                        <p><strong>Lösungen:</strong></p>
                        <ul>
                            <li>Verifizieren Sie, dass der API-Schlüssel gültig ist und genügend Guthaben hat</li>
                            <li>Prüfen Sie API-Ratenlimits</li>
                            <li>Stellen Sie sicher, dass die API-Endpunkt-Konfiguration korrekt ist</li>
                            <li>Überwachen Sie API-Nutzung und Kontingente</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-brain"></i>
                        </div>
                        <h3>OpenAI API-Fehler</h3>
                        <p><strong>Problem:</strong> Fehler von der OpenAI API.</p>
                        <p><strong>Lösungen:</strong></p>
                        <ul>
                            <li>Verifizieren Sie, dass der API-Schlüssel gültig ist</li>
                            <li>Prüfen Sie API-Ratenlimits und Kontingente</li>
                            <li>Stellen Sie sicher, dass die Modell-Konfiguration korrekt ist</li>
                            <li>Überwachen Sie die API-Nutzung</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Testing and Debugging -->
        <div class="content-section">
            <h2>Tests und Debugging</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-vial"></i>
                        </div>
                        <h3>Unit-Tests</h3>
                        <p><strong>Problem:</strong> Tests schlagen aufgrund von SmartRAG-Abhängigkeiten fehl.</p>
                        <p><strong>Lösung:</strong> Verwenden Sie Mocking für SmartRAG-Services in Unit-Tests:</p>
                        
                        <div class="code-example">
                            <pre><code class="language-csharp">[Test]
public async Task TestDocumentUpload()
{
    // Arrange
    var mockDocumentService = new Mock<IDocumentService>();
    var mockSearchService = new Mock<IDocumentSearchService>();
    
    var controller = new DocumentsController(
        mockDocumentService.Object, 
        mockSearchService.Object, 
        Mock.Of<ILogger<DocumentsController>>());

    // Act & Assert
    // Ihre Testlogik hier
}</code></pre>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-cogs"></i>
                        </div>
                        <h3>Integrationstests</h3>
                        <p><strong>Problem:</strong> Integrationstests schlagen fehl.</p>
                        <p><strong>Lösung:</strong> Verwenden Sie Test-Konfiguration und stellen Sie sicher, dass das Setup korrekt ist:</p>
                        
                        <div class="code-example">
                            <pre><code class="language-csharp">[Test]
public async Task TestEndToEndWorkflow()
{
    // Arrange
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.test.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();
    services.AddSmartRag(configuration);
    
    var serviceProvider = services.BuildServiceProvider();
    
    // Act & Assert
    // Ihre Integrationstest-Logik hier
}</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Common Error Messages -->
        <div class="content-section">
            <h2>Häufige Fehlermeldungen</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-exclamation-triangle"></i>
                        </div>
                        <h3>Häufige Fehler</h3>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"Document not found"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Verifizieren Sie, dass die Dokument-ID korrekt ist</li>
                                <li>Prüfen Sie, ob das Dokument erfolgreich hochgeladen wurde</li>
                                <li>Stellen Sie sicher, dass das Dokument nicht gelöscht wurde</li>
                            </ul>
                        </div>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"Storage provider not configured"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Verifizieren Sie die <code>StorageProvider</code>-Einstellung in der Konfiguration</li>
                                <li>Stellen Sie sicher, dass alle erforderlichen Speichereinstellungen bereitgestellt werden</li>
                                <li>Prüfen Sie die Service-Registrierung</li>
                            </ul>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-exclamation-circle"></i>
                        </div>
                        <h3>Weitere Fehler</h3>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"AI provider not configured"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Verifizieren Sie die <code>AIProvider</code>-Einstellung in der Konfiguration</li>
                                <li>Stellen Sie sicher, dass ein API-Schlüssel für den ausgewählten Anbieter bereitgestellt wird</li>
                                <li>Prüfen Sie die Service-Registrierung</li>
                            </ul>
                        </div>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"Invalid file format"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Stellen Sie sicher, dass die Datei in einem unterstützten Format ist</li>
                                <li>Prüfen Sie die Dateierweiterung und den Inhalt</li>
                                <li>Verifizieren Sie, dass die Datei nicht beschädigt ist</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Getting Help -->
        <div class="content-section">
            <h2>Hilfe bekommen</h2>
            
            <div class="row g-4">
                <div class="col-lg-8">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-question-circle"></i>
                        </div>
                        <h3>Brauchen Sie noch Hilfe?</h3>
                        <p>Wenn Sie immer noch Probleme haben, folgen Sie diesen Schritten:</p>
                        
                        <div class="row g-3">
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-file-alt"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Logs prüfen</h5>
                                        <p class="text-muted">Überprüfen Sie Anwendungslogs für detaillierte Fehlermeldungen</p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-cog"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Konfiguration verifizieren</h5>
                                        <p class="text-muted">Überprüfen Sie alle Konfigurationseinstellungen erneut</p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-play"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Mit minimalem Setup testen</h5>
                                        <p class="text-muted">Versuchen Sie es zuerst mit einer einfachen Konfiguration</p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-book"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Dokumentation überprüfen</h5>
                                        <p class="text-muted">Prüfen Sie andere Dokumentationsseiten für Anleitung</p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fab fa-github"></i>
                        </div>
                        <h3>Zusätzliche Unterstützung</h3>
                        <p>Für zusätzliche Unterstützung wenden Sie sich an:</p>
                        
                        <div class="d-grid gap-3">
                            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-primary" target="_blank">
                                <i class="fab fa-github me-2"></i>
                                GitHub Repository
                            </a>
                            <a href="https://github.com/byerlikaya/SmartRAG/issues" class="btn btn-outline-primary" target="_blank">
                                <i class="fas fa-bug me-2"></i>
                                Issue erstellen
                            </a>
                            <a href="{{ site.baseurl }}/de/getting-started" class="btn btn-outline-primary">
                                <i class="fas fa-rocket me-2"></i>
                                Erste Schritte
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>

    </div>
</div>
