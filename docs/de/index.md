---
layout: default
title: SmartRAG Dokumentation
description: Unternehmensreife RAG-Bibliothek für .NET-Anwendungen
lang: de
---

<div class="hero-section text-center py-5 mb-5">
    <div class="hero-content">
        <h1 class="hero-title display-4 fw-bold mb-4">
            <i class="fas fa-brain me-3"></i>
            SmartRAG
        </h1>
        <p class="hero-subtitle lead mb-4">
            Unternehmensreife RAG-Bibliothek für .NET-Anwendungen
        </p>
        <p class="hero-description mb-5">
            Entwickeln Sie intelligente Anwendungen mit fortschrittlicher Dokumentenverarbeitung, KI-gestützten Embeddings und semantischen Suchfunktionen.
        </p>
        <div class="hero-buttons">
            <a href="{{ site.baseurl }}/de/getting-started" class="btn btn-primary btn-lg me-3">
                <i class="fas fa-rocket me-2"></i>Loslegen
            </a>
            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-lg me-3" target="_blank" rel="noopener noreferrer">
                <i class="fab fa-github me-2"></i>Auf GitHub anzeigen
            </a>
            <a href="https://www.nuget.org/packages/SmartRAG" class="btn btn-outline-success btn-lg" target="_blank" rel="noopener noreferrer">
                <i class="fas fa-box me-2"></i>NuGet-Paket
            </a>
        </div>
    </div>
</div>

## 🚀 Was ist SmartRAG?

SmartRAG ist eine umfassende .NET-Bibliothek, die intelligente Dokumentenverarbeitung, Embedding-Generierung und semantische Suchfunktionen bereitstellt. Sie ist so konzipiert, dass sie einfach zu verwenden ist und gleichzeitig leistungsstarke Funktionen für den Aufbau KI-gestützter Anwendungen bietet.

<div class="row mt-5 mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-file-alt fa-3x text-primary"></i>
                </div>
                <h5 class="card-title">Multi-Format-Unterstützung</h5>
                <p class="card-text">Verarbeiten Sie Word-, PDF-, Excel- und Textdokumente mühelos. Unsere Bibliothek behandelt alle wichtigen Dokumentenformate automatisch.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-robot fa-3x text-success"></i>
                </div>
                <h5 class="card-title">KI-Anbieter-Integration</h5>
                <p class="card-text">Nahtlose Integration mit OpenAI, Anthropic, Azure OpenAI, Gemini und benutzerdefinierten KI-Anbietern für leistungsstarke Embedding-Generierung.</p>
            </div>
        </div>
    </div>
</div>

<div class="row mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-database fa-3x text-warning"></i>
                </div>
                <h5 class="card-title">Vektor-Speicherung</h5>
                <p class="card-text">Mehrere Speicher-Backends einschließlich Qdrant, Redis, SQLite, In-Memory, Dateisystem und benutzerdefinierte Speicherung für flexible Bereitstellung.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-search fa-3x text-info"></i>
                </div>
                <h5 class="card-title">Semantische Suche</h5>
                <p class="card-text">Fortschrittliche Suchfunktionen mit Ähnlichkeitsbewertung und intelligenter Ergebnisrangfolge für bessere Benutzererfahrung.</p>
            </div>
        </div>
    </div>
</div>

## ⚡ Schnellstart

Starten Sie in wenigen Minuten mit unserem einfachen Einrichtungsprozess:

```csharp
// Fügen Sie SmartRAG zu Ihrem Projekt hinzu
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});

// Verwenden Sie den Dokumentenservice
var documentService = serviceProvider.GetRequiredService<IDocumentService>();
var document = await documentService.UploadDocumentAsync(file);
```

## 🚀 Unterstützte Technologien

SmartRAG integriert sich mit führenden KI-Anbietern und Speicherlösungen, um Ihnen die bestmögliche Erfahrung zu bieten.

### 🤖 KI-Anbieter

<div class="row mt-4 mb-5">
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fab fa-google fa-3x text-warning"></i>
            </div>
            <h6 class="mb-1">Gemini</h6>
            <small class="text-muted">Google AI</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fab fa-openai fa-3x text-primary"></i>
            </div>
            <h6 class="mb-1">OpenAI</h6>
            <small class="text-muted">GPT-Modelle</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cloud fa-3x text-secondary"></i>
            </div>
            <h6 class="mb-1">Azure OpenAI</h6>
            <small class="text-muted">Unternehmen</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-robot fa-3x text-success"></i>
            </div>
            <h6 class="mb-1">Anthropic</h6>
            <small class="text-muted">Claude-Modelle</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cogs fa-3x text-dark"></i>
            </div>
            <h6 class="mb-1">Benutzerdefiniert</h6>
            <small class="text-muted">Erweiterbar</small>
        </div>
    </div>
</div>

### 🗄️ Speicheranbieter

<div class="row mt-4 mb-5">
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cube fa-3x text-primary"></i>
            </div>
            <h6 class="mb-1">Qdrant</h6>
            <small class="text-muted">Vektordatenbank</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fab fa-redis fa-3x text-success"></i>
            </div>
            <h6 class="mb-1">Redis</h6>
            <small class="text-muted">In-Memory-Cache</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-hdd fa-3x text-info"></i>
            </div>
            <h6 class="mb-1">SQLite</h6>
            <small class="text-muted">Lokale Datenbank</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-microchip fa-3x text-warning"></i>
            </div>
            <h6 class="mb-1">In-Memory</h6>
            <small class="text-muted">Schnelle Entwicklung</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-folder-open fa-3x text-secondary"></i>
            </div>
            <h6 class="mb-1">Dateisystem</h6>
            <small class="text-muted">Lokaler Speicher</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cogs fa-3x text-dark"></i>
            </div>
            <h6 class="mb-1">Benutzerdefiniert</h6>
            <small class="text-muted">Erweiterbarer Speicher</small>
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
                <a href="{{ site.baseurl }}/de/api-reference" class="btn btn-warning">API anzeigen</a>
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
                <p class="card-text">Echte Beispiele und Beispielanwendungen zum Lernen.</p>
                <a href="{{ site.baseurl }}/de/examples" class="btn btn-info">Beispiele anzeigen</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-tools fa-2x text-danger mb-3"></i>
                <h5 class="card-title">Fehlerbehebung</h5>
                <p class="card-text">Häufige Probleme und Lösungen, um Ihnen bei der Problemlösung zu helfen.</p>
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
                <a href="{{ site.baseurl }}/de/changelog" class="btn btn-secondary">Änderungen anzeigen</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-hands-helping fa-2x text-dark mb-3"></i>
                <h5 class="card-title">Mitwirken</h5>
                <p class="card-text">Erfahren Sie, wie Sie zur SmartRAG-Entwicklung beitragen können.</p>
                <a href="{{ site.baseurl }}/de/contributing" class="btn btn-dark">Beitragen</a>
            </div>
        </div>
    </div>
</div>

## 🌟 Warum SmartRAG?

<div class="alert alert-info">
    <h5><i class="fas fa-star me-2"></i>Unternehmensbereit</h5>
    <p class="mb-0">Für Produktionsumgebungen mit Fokus auf Leistung, Skalierbarkeit und Zuverlässigkeit entwickelt.</p>
</div>

<div class="alert alert-success">
    <h5><i class="fas fa-shield-alt me-2"></i>Produktionsgetestet</h5>
    <p class="mb-0">In echten Anwendungen verwendet mit bewährtem Track Record und aktiver Wartung.</p>
</div>

<div class="alert alert-warning">
    <h5><i class="fas fa-code me-2"></i>Open Source</h5>
    <p class="mb-0">MIT-lizenziertes Open-Source-Projekt mit transparenter Entwicklung und regelmäßigen Updates.</p>
</div>

## 📦 Installation

Installieren Sie SmartRAG über NuGet:

```bash
dotnet add package SmartRAG
```

Oder mit dem Package Manager:

```bash
Install-Package SmartRAG
```

## 🤝 Mitwirken

Wir freuen uns über Beiträge! Weitere Details finden Sie in unserem [Beitragsleitfaden]({{ site.baseurl }}/de/contributing).

## 📄 Lizenz

Dieses Projekt steht unter der MIT-Lizenz - siehe [LICENSE](https://github.com/byerlikaya/SmartRAG/blob/main/LICENSE) für Details.

---

<div class="text-center mt-5">
    <p class="text-muted">
        <i class="fas fa-heart text-danger"></i> Mit Liebe entwickelt von Barış Yerlikaya
    </p>
</div>
