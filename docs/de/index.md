---
layout: default
title: SmartRAG Dokumentation
description: Enterprise-Grade RAG-Bibliothek für .NET-Anwendungen
lang: de
---

<div class="hero-section text-center py-5 mb-5">
    <div class="hero-content">
        <h1 class="hero-title display-4 fw-bold mb-4">
            <i class="fas fa-brain me-3"></i>
            SmartRAG
        </h1>
        <p class="hero-subtitle lead mb-4">
            Enterprise-Grade RAG-Bibliothek für .NET-Anwendungen
        </p>
        <p class="hero-description mb-5">
            Erstellen Sie intelligente Anwendungen mit fortschrittlicher Dokumentenverarbeitung, KI-gestützten Embeddings und semantischen Suchfunktionen.
        </p>
        <div class="hero-buttons">
            <a href="{{ site.baseurl }}/de/getting-started" class="btn btn-primary btn-lg me-3">
                <i class="fas fa-rocket me-2"></i>Loslegen
            </a>
            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-lg me-3" target="_blank" rel="noopener noreferrer">
                <i class="fab fa-github me-2"></i>Auf GitHub ansehen
            </a>
            <a href="https://www.nuget.org/packages/SmartRAG" class="btn btn-outline-success btn-lg" target="_blank" rel="noopener noreferrer">
                <i class="fas fa-box me-2"></i>NuGet-Paket
            </a>
        </div>
    </div>
</div>

## 🚀 Was ist SmartRAG?

SmartRAG ist eine umfassende .NET-Bibliothek, die intelligente Dokumentenverarbeitung, Embedding-Generierung und semantische Suchfunktionen bietet. Sie wurde entwickelt, um einfach zu verwenden zu sein und gleichzeitig leistungsstarke Funktionen für die Entwicklung von KI-gestützten Anwendungen zu bieten.

<div class="row mt-5 mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body p-4">
                <h5 class="card-title">
                    <div class="feature-icon">
                        <i class="fas fa-file-alt text-primary"></i>
                    </div>
                    Multi-Format-Unterstützung
                </h5>
                <p class="card-text">Verarbeiten Sie Word-, PDF-, Excel- und Textdokumente mit Leichtigkeit. Unsere Bibliothek behandelt alle wichtigen Dokumentformate automatisch.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body p-4">
                <h5 class="card-title">
                    <div class="feature-icon">
                        <i class="fas fa-robot text-success"></i>
                    </div>
                    KI-Anbieter-Integration
                </h5>
                <p class="card-text">Nahtlose Integration mit OpenAI, Anthropic, Azure OpenAI, Gemini und benutzerdefinierten KI-Anbietern für leistungsstarke Embedding-Generierung.</p>
            </div>
        </div>
    </div>
</div>

<div class="row mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body p-4">
                <h5 class="card-title">
                    <div class="feature-icon">
                        <i class="fas fa-database text-warning"></i>
                    </div>
                    Vektor-Speicher
                </h5>
                <p class="card-text">Mehrere Speicher-Backends einschließlich Qdrant, Redis, SQLite, In-Memory und Dateisystem für flexible Bereitstellung.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body p-4">
                <h5 class="card-title">
                    <div class="feature-icon">
                        <i class="fas fa-search text-info"></i>
                    </div>
                    Semantische Suche
                </h5>
                <p class="card-text">Erweiterte Suchfunktionen mit Ähnlichkeitsbewertung und intelligenter Ergebnisrangfolge für bessere Benutzererfahrung.</p>
            </div>
        </div>
    </div>
</div>

## 🌟 Warum SmartRAG?

<div class="alert alert-info">
    <h5><i class="fas fa-star me-2"></i>Enterprise-Ready</h5>
    <p class="mb-0">Für Produktionsumgebungen mit Fokus auf Leistung, Skalierbarkeit und Zuverlässigkeit entwickelt.</p>
</div>

<div class="alert alert-success">
    <h5><i class="fas fa-shield-alt me-2"></i>Produktionsgetestet</h5>
    <p class="mb-0">In realen Anwendungen mit nachgewiesener Erfolgsbilanz und aktiver Wartung verwendet.</p>
</div>

<div class="alert alert-warning">
    <h5><i class="fas fa-code me-2"></i>Open Source</h5>
    <p class="mb-0">MIT-lizenzierte Open-Source-Projekt mit transparenter Entwicklung und regelmäßigen Updates.</p>
</div>

## ⚡ Schnellstart

Starten Sie in wenigen Minuten mit unserem einfachen Einrichtungsprozess:

```csharp
// SmartRAG zu Ihrem Projekt hinzufügen
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});

// Dokumentenservice verwenden
var documentService = serviceProvider.GetRequiredService<IDocumentService>();
var document = await documentService.UploadDocumentAsync(file);
```

## 🚀 Unterstützte Technologien

SmartRAG integriert sich mit führenden KI-Anbietern und Speicherlösungen, um Ihnen die bestmögliche Erfahrung zu bieten.

### 🤖 KI-Anbieter

<div class="row mt-4 mb-5">
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fab fa-google"></i>
            </div>
            <h6>Gemini</h6>
            <small>Google AI</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-brain"></i>
            </div>
            <h6>OpenAI</h6>
            <small>GPT-Modelle</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-cloud"></i>
            </div>
            <h6>Azure OpenAI</h6>
            <small>Enterprise</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-robot"></i>
            </div>
            <h6>Anthropic</h6>
            <small>Claude-Modelle</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-cogs"></i>
            </div>
            <h6>Benutzerdefiniert</h6>
            <small>Erweiterbar</small>
        </div>
    </div>
</div>

### 🗄️ Speicher-Anbieter

<div class="row mt-4 mb-5">
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-cube"></i>
            </div>
            <h6>Qdrant</h6>
            <small>Vektor-Datenbank</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-database"></i>
            </div>
            <h6>Redis</h6>
            <small>In-Memory-Cache</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-hdd"></i>
            </div>
            <h6>SQLite</h6>
            <small>Lokale Datenbank</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-microchip"></i>
            </div>
            <h6>In-Memory</h6>
            <small>Schnelle Entwicklung</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-folder-open"></i>
            </div>
            <h6>Dateisystem</h6>
            <small>Lokaler Speicher</small>
        </div>
    </div>
</div>

## 📚 Dokumentation

<div class="row mt-4">
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-rocket fa-2x text-primary mb-3"></i>
                <h5 class="card-title">Erste Schritte</h5>
                <p class="card-text">Schnelle Installations- und Einrichtungsanleitung, um Sie zum Laufen zu bringen.</p>
                <a href="{{ site.baseurl }}/de/getting-started" class="btn btn-primary">Loslegen</a>
            </div>
        </div>
    </div>
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-cog fa-2x text-success mb-3"></i>
                <h5 class="card-title">Konfiguration</h5>
                <p class="card-text">Detaillierte Konfigurationsoptionen und bewährte Praktiken.</p>
                <a href="{{ site.baseurl }}/de/configuration" class="btn btn-success">Konfigurieren</a>
            </div>
        </div>
    </div>
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-code fa-2x text-warning mb-3"></i>
                <h5 class="card-title">API-Referenz</h5>
                <p class="card-text">Vollständige API-Dokumentation mit Beispielen und Verwendungsmustern.</p>
                <a href="{{ site.baseurl }}/de/api-reference" class="btn btn-warning">API ansehen</a>
            </div>
        </div>
    </div>
</div>

<div class="row mt-4">
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-lightbulb fa-2x text-info mb-3"></i>
                <h5 class="card-title">Beispiele</h5>
                <p class="card-text">Reale Beispiele und Beispielanwendungen zum Lernen.</p>
                <a href="{{ site.baseurl }}/de/examples" class="btn btn-info">Beispiele ansehen</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-tools fa-2x text-danger mb-3"></i>
                <h5 class="card-title">Fehlerbehebung</h5>
                <p class="card-text">Häufige Probleme und Lösungen zur Problemlösung.</p>
                <a href="{{ site.baseurl }}/de/troubleshooting" class="btn btn-danger">Hilfe erhalten</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-history fa-2x text-secondary mb-3"></i>
                <h5 class="card-title">Änderungsprotokoll</h5>
                <p class="card-text">Verfolgen Sie neue Funktionen, Verbesserungen und Fehlerbehebungen über Versionen hinweg.</p>
                <a href="{{ site.baseurl }}/de/changelog" class="btn btn-secondary">Änderungen ansehen</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-hands-helping fa-2x text-dark mb-3"></i>
                <h5 class="card-title">Beitragen</h5>
                <p class="card-text">Erfahren Sie, wie Sie zur SmartRAG-Entwicklung beitragen können.</p>
                <a href="{{ site.baseurl }}/de/contributing" class="btn btn-dark">Beitragen</a>
            </div>
        </div>
    </div>
</div>

---

<div class="text-center mt-5">
    <p class="text-muted">
        <i class="fas fa-heart text-danger"></i> Mit Liebe von Barış Yerlikaya erstellt
    </p>
</div>