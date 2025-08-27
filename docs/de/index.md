---
layout: default
title: SmartRAG Dokumentation
description: Enterprise-Grade RAG-Bibliothek für .NET-Anwendungen
lang: de
hide_title: true
---

<!-- Hero Section -->
<section class="hero-section">
    <div class="hero-background"></div>
    <div class="container">
        <div class="row align-items-center min-vh-100">
            <div class="col-lg-6">
                <div class="hero-content">
                    <div class="hero-badge">
                        <i class="fas fa-star"></i>
                        <span>Enterprise-Ready</span>
                    </div>
                    <h1 class="hero-title">
                        Intelligente Anwendungen mit 
                        <span class="text-gradient">SmartRAG</span> erstellen
                    </h1>
                    <p class="hero-description">
                        Die leistungsstärkste .NET-Bibliothek für Dokumentenverarbeitung, KI-Embeddings und semantische Suche. 
                        Transformieren Sie Ihre Anwendungen mit Enterprise-Grade RAG-Funktionen.
                    </p>
                    <div class="hero-stats">
                        <div class="stat-item">
                            <div class="stat-number">5+</div>
                            <div class="stat-label">KI-Anbieter</div>
                        </div>
                        <div class="stat-item">
                            <div class="stat-number">5+</div>
                            <div class="stat-label">Speicher-Optionen</div>
                        </div>
                        <div class="stat-item">
                            <div class="stat-number">100%</div>
                            <div class="stat-label">Open Source</div>
                        </div>
                    </div>
                    <div class="hero-buttons">
                        <a href="{{ site.baseurl }}/de/getting-started" class="btn btn-primary btn-lg">
                            <i class="fas fa-rocket"></i>
                            Loslegen
                        </a>
                        <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-light btn-lg" target="_blank">
                            <i class="fab fa-github"></i>
                            Auf GitHub ansehen
                        </a>
                    </div>
                </div>
            </div>
            <div class="col-lg-6">
                <div class="hero-visual">
                    <div class="code-window">
                        <div class="code-header">
                            <div class="code-dots">
                                <span></span>
                                <span></span>
                                <span></span>
                            </div>
                            <div class="code-title">SmartRAG.cs</div>
                        </div>
                        <div class="code-content">
                            <pre><code class="language-csharp">// SmartRAG zu Ihrem Projekt hinzufügen
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});

// Dokument hochladen und verarbeiten
var document = await documentService
    .UploadDocumentAsync(file);

// Semantische Suche durchführen
var results = await documentService
    .SearchAsync("your query");</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Features Section -->
<section class="features-section">
    <div class="container">
        <div class="section-header text-center">
            <h2 class="section-title">Hauptfunktionen</h2>
            <p class="section-description">
                Leistungsstarke Funktionen für die Entwicklung intelligenter Anwendungen
            </p>
        </div>
        
        <div class="row g-4">
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-brain"></i>
                    </div>
                    <h3>KI-gestützt</h3>
                    <p>Integration mit führenden KI-Anbietern für leistungsstarke Embeddings und intelligente Verarbeitung.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-file-alt"></i>
                    </div>
                    <h3>Multi-Format-Unterstützung</h3>
                    <p>Verarbeiten Sie Word-, PDF-, Excel- und Textdokumente mit automatischer Format-Erkennung.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-search"></i>
                    </div>
                    <h3>Semantische Suche</h3>
                    <p>Erweiterte Suche mit Ähnlichkeitsbewertung und intelligenter Ergebnisrangfolge.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-database"></i>
                    </div>
                    <h3>Flexible Speicherung</h3>
                    <p>Mehrere Speicher-Backends für flexible Bereitstellungsoptionen.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-rocket"></i>
                    </div>
                    <h3>Einfache Integration</h3>
                    <p>Einfache Einrichtung mit Dependency Injection. Starten Sie in wenigen Minuten.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-magic"></i>
                    </div>
                    <h3>Intelligente Abfrage-Absicht</h3>
                    <p>Leitet Abfragen automatisch zu Chat oder Dokumentsuche basierend auf Absichtserkennung weiter.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-magic"></i>
                    </div>
                    <h3>Intelligente Abfrage-Absicht</h3>
                    <p>Leitet Abfragen automatisch zu Chat oder Dokumentsuche basierend auf Absichtserkennung weiter.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-shield-alt"></i>
                    </div>
                    <h3>Produktionsbereit</h3>
                    <p>Für Enterprise-Umgebungen mit Leistung und Zuverlässigkeit entwickelt.</p>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Providers Section -->
<section class="providers-section">
    <div class="container">
        <div class="section-header text-center">
            <h2 class="section-title">Unterstützte Technologien</h2>
            <p class="section-description">
                Wählen Sie aus führenden KI-Anbietern und Speicherlösungen
            </p>
        </div>
        
        <div class="providers-grid">
            <div class="provider-category">
                <h3>KI-Anbieter</h3>
                <div class="provider-cards">
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fab fa-google"></i>
                        </div>
                        <h4>Gemini</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-brain"></i>
                        </div>
                        <h4>OpenAI</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cloud"></i>
                        </div>
                        <h4>Azure OpenAI</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-robot"></i>
                        </div>
                        <h4>Anthropic</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cogs"></i>
                        </div>
                        <h4>Benutzerdefiniert</h4>
                    </div>
                </div>
            </div>
            
            <div class="provider-category">
                <h3>Speicher-Anbieter</h3>
                <div class="provider-cards">
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cube"></i>
                        </div>
                        <h4>Qdrant</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-database"></i>
                        </div>
                        <h4>Redis</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-hdd"></i>
                        </div>
                        <h4>SQLite</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-microchip"></i>
                        </div>
                        <h4>In-Memory</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-folder-open"></i>
                        </div>
                        <h4>Dateisystem</h4>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Quick Start Section -->
<section class="quick-start-section">
    <div class="container">
        <div class="row align-items-center">
            <div class="col-lg-6">
                <div class="quick-start-content">
                    <h2>In wenigen Minuten starten</h2>
                    <p>Einfache und leistungsstarke Integration für Ihre .NET-Anwendungen.</p>
                    
                    <div class="steps">
                        <div class="step">
                            <div class="step-number">1</div>
                            <div class="step-content">
                                <h4>Paket installieren</h4>
                                <p>SmartRAG über NuGet hinzufügen</p>
                            </div>
                        </div>
                        <div class="step">
                            <div class="step-number">2</div>
                            <div class="step-content">
                                <h4>Dienste konfigurieren</h4>
                                <p>KI- und Speicher-Anbieter einrichten</p>
                            </div>
                        </div>
                        <div class="step">
                            <div class="step-number">3</div>
                            <div class="step-content">
                                <h4>Entwicklung starten</h4>
                                <p>Dokumente hochladen und suchen</p>
                            </div>
                        </div>
                    </div>
                    
                    <a href="{{ site.baseurl }}/de/getting-started" class="btn btn-primary btn-lg">
                        <i class="fas fa-play"></i>
                        Entwicklung starten
                    </a>
                </div>
            </div>
            <div class="col-lg-6">
                <div class="code-example">
                    <div class="code-tabs">
                        <button class="code-tab active" data-tab="install">Installieren</button>
                        <button class="code-tab" data-tab="configure">Konfigurieren</button>
                        <button class="code-tab" data-tab="use">Verwenden</button>
                    </div>
                    <div class="code-content">
                        <div class="code-panel active" id="install">
                            <pre><code class="language-bash">dotnet add package SmartRAG</code></pre>
                        </div>
                        <div class="code-panel" id="configure">
                            <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});</code></pre>
                        </div>
                        <div class="code-panel" id="use">
                            <pre><code class="language-csharp">var documentService = serviceProvider
    .GetRequiredService&lt;IDocumentService&gt;();

var results = await documentService
    .SearchAsync("your query");</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Documentation Section -->
<section class="documentation-section">
    <div class="container">
        <div class="section-header text-center">
            <h2 class="section-title">Dokumentation</h2>
            <p class="section-description">
                Alles was Sie für die Entwicklung mit SmartRAG benötigen
            </p>
        </div>
        
        <div class="row g-4">
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/de/getting-started" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-rocket"></i>
                    </div>
                    <h3>Erste Schritte</h3>
                    <p>Schnelle Installations- und Einrichtungsanleitung</p>
                </a>
            </div>
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/de/configuration" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-cog"></i>
                    </div>
                    <h3>Konfiguration</h3>
                    <p>Detaillierte Konfigurationsoptionen</p>
                </a>
            </div>
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/de/api-reference" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-code"></i>
                    </div>
                    <h3>API-Referenz</h3>
                    <p>Vollständige API-Dokumentation</p>
                </a>
            </div>
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/de/examples" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-lightbulb"></i>
                    </div>
                    <h3>Beispiele</h3>
                    <p>Reale Beispiele und Beispielanwendungen</p>
                </a>
            </div>
        </div>
    </div>
</section>

<!-- CTA Section -->
<section class="cta-section">
    <div class="container">
        <div class="cta-content text-center">
            <h2>Bereit, etwas Erstaunliches zu erstellen?</h2>
            <p>Schließen Sie sich Tausenden von Entwicklern an, die SmartRAG für intelligente Anwendungen verwenden</p>
            <div class="cta-buttons">
                <a href="{{ site.baseurl }}/de/getting-started" class="btn btn-primary btn-lg">
                    <i class="fas fa-rocket"></i>
                    Jetzt starten
                </a>
                <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-light btn-lg" target="_blank">
                    <i class="fab fa-github"></i>
                    Auf GitHub bewerten
                </a>
            </div>
        </div>
    </div>
</section>