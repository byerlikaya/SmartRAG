---
layout: default
title: Fehlerbehebung
nav_order: 5
---

# Fehlerbehebung

Diese Seite bietet Lösungen für häufige Probleme, die bei der Verwendung von SmartRAG auftreten können.

<div class="troubleshooting-section">
## Konfigurationsprobleme

### API-Schlüssel-Konfiguration

<div class="problem-solution">
**Problem**: Authentifizierungsfehler bei AI- oder Speicheranbietern.

**Lösung**: Stellen Sie sicher, dass Ihre API-Schlüssel korrekt in `appsettings.json` konfiguriert sind:
</div>

```json
{
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
}
```

Oder setzen Sie Umgebungsvariablen:

```bash
# Umgebungsvariablen setzen
export ANTHROPIC_API_KEY=your-anthropic-api-key
export QDRANT_API_KEY=your-qdrant-api-key
```

### Dienstregistrierungsprobleme

<div class="problem-solution">
**Problem**: Dependency Injection-Fehler.

**Lösung**: Stellen Sie sicher, dass SmartRAG-Dienste korrekt in Ihrer `Program.cs` registriert sind:
</div>

```csharp
using SmartRAG.Extensions;

var builder = WebApplication.CreateBuilder(args);

// SmartRAG-Dienste hinzufügen
builder.Services.AddSmartRag(builder.Configuration);

var app = builder.Build();
app.UseSmartRag(builder.Configuration, StorageProvider.Qdrant, AIProvider.Anthropic);
app.Run();
```
</div>

<div class="troubleshooting-section">
## Dokument-Upload-Probleme

### Dateigrößenbeschränkungen

<div class="problem-solution">
**Problem**: Große Dokumente können nicht hochgeladen oder verarbeitet werden.

**Lösung**: 
</div>

- Überprüfen Sie die Dateigrößenlimits Ihrer Anwendung in `appsettings.json`
- Erwägen Sie das Aufteilen großer Dokumente in kleinere Chunks
- Stellen Sie sicher, dass ausreichend Speicher für die Verarbeitung verfügbar ist

### Nicht unterstützte Dateitypen

<div class="problem-solution">
**Problem**: Fehler bei bestimmten Dateiformaten.

**Lösung**: SmartRAG unterstützt gängige Textformate. Stellen Sie sicher, dass Ihre Dateien in unterstützten Formaten sind:
</div>

- PDF-Dateien
- Textdateien (.txt)
- Word-Dokumente (.docx)
- Markdown-Dateien (.md)
</div>

<div class="troubleshooting-section">
## Such- und Abrufprobleme

### Keine Suchergebnisse

<div class="problem-solution">
**Problem**: Suchanfragen liefern keine Ergebnisse.

**Mögliche Lösungen**:
</div>

1. **Dokument-Upload überprüfen**: Stellen Sie sicher, dass Dokumente erfolgreich hochgeladen wurden
2. **Embeddings verifizieren**: Überprüfen Sie, ob Embeddings korrekt generiert wurden
3. **Anfragespezifität**: Versuchen Sie spezifischere Suchbegriffe
4. **Speicherverbindung**: Verifizieren Sie, dass Ihr Speicheranbieter zugänglich ist

### Schlechte Suchqualität

<div class="problem-solution">
**Problem**: Suchergebnisse sind nicht relevant.

**Lösungen**:
</div>

- Passen Sie `MaxChunkSize` und `ChunkOverlap`-Einstellungen an
- Verwenden Sie spezifischere Suchanfragen
- Stellen Sie sicher, dass Dokumente korrekt formatiert sind
- Überprüfen Sie, ob Embeddings aktuell sind
</div>

<div class="troubleshooting-section">
## Leistungsprobleme

### Langsame Dokumentverarbeitung

<div class="problem-solution">
**Problem**: Dokument-Upload und -verarbeitung dauert zu lange.

**Lösungen**:
</div>

- Erhöhen Sie `MaxChunkSize`, um die Anzahl der Chunks zu reduzieren
- Verwenden Sie einen leistungsstärkeren AI-Anbieter
- Optimieren Sie Ihre Speicheranbieter-Konfiguration
- Erwägen Sie die Verwendung von async-Operationen in Ihrer Anwendung

### Speicherprobleme

<div class="problem-solution">
**Problem**: Anwendung läuft während der Verarbeitung aus dem Speicher.

**Lösungen**:
</div>

- Reduzieren Sie `MaxChunkSize`, um kleinere Chunks zu erstellen
- Verarbeiten Sie Dokumente in Batches
- Überwachen Sie die Speichernutzung und optimieren Sie entsprechend
- Erwägen Sie Streaming-Operationen für große Dateien
</div>

<div class="troubleshooting-section">
## Speicheranbieter-Probleme

### Qdrant-Verbindungsprobleme

<div class="problem-solution">
**Problem**: Kann nicht zu Qdrant verbinden.

**Lösungen**:
</div>

- Verifizieren Sie, dass der Qdrant-API-Schlüssel korrekt ist
- Überprüfen Sie die Netzwerkverbindung zum Qdrant-Service
- Stellen Sie sicher, dass der Qdrant-Service läuft und zugänglich ist
- Überprüfen Sie Firewall-Einstellungen

### Redis-Verbindungsprobleme

<div class="problem-solution">
**Problem**: Kann nicht zu Redis verbinden.

**Lösungen**:
</div>

- Verifizieren Sie die Redis-Verbindungszeichenfolge
- Stellen Sie sicher, dass der Redis-Server läuft
- Überprüfen Sie die Netzwerkverbindung
- Verifizieren Sie die Redis-Konfiguration in `appsettings.json`

### SQLite-Probleme

<div class="problem-solution">
**Problem**: SQLite-Datenbankfehler.

**Lösungen**:
</div>

- Überprüfen Sie Dateiberechtigungen für das Datenbankverzeichnis
- Stellen Sie sicher, dass ausreichend Festplattenspeicher vorhanden ist
- Verifizieren Sie, dass der Datenbankdateipfad korrekt ist
- Überprüfen Sie auf Datenbankbeschädigung
</div>

<div class="troubleshooting-section">
## AI-Anbieter-Probleme

### Anthropic-API-Fehler

<div class="problem-solution">
**Problem**: Fehler von der Anthropic-API.

**Lösungen**:
</div>

- Verifizieren Sie, dass der API-Schlüssel gültig ist und ausreichend Guthaben hat
- Überprüfen Sie API-Ratenlimits
- Stellen Sie sicher, dass die API-Endpunkt-Konfiguration korrekt ist
- Überwachen Sie API-Nutzung und Kontingente

### OpenAI-API-Fehler

<div class="problem-solution">
**Problem**: Fehler von der OpenAI-API.

**Lösungen**:
</div>

- Verifizieren Sie, dass der API-Schlüssel gültig ist
- Überprüfen Sie API-Ratenlimits und Kontingente
- Stellen Sie sicher, dass die Modell-Konfiguration korrekt ist
- Überwachen Sie die API-Nutzung
</div>

<div class="troubleshooting-section">
## Testen und Debugging

### Unit-Tests

<div class="problem-solution">
**Problem**: Tests schlagen fehl aufgrund von SmartRAG-Abhängigkeiten.

**Lösung**: Verwenden Sie Mocking für SmartRAG-Dienste in Unit-Tests:
</div>

```csharp
[Test]
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
}
```

### Integrationstests

<div class="problem-solution">
**Problem**: Integrationstests schlagen fehl.

**Lösung**: Verwenden Sie Test-Konfiguration und stellen Sie sicher, dass das Setup korrekt ist:
</div>

```csharp
[Test]
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
}
```
</div>

<div class="troubleshooting-section">
## Häufige Fehlermeldungen

### "Document not found"
- Verifizieren Sie, dass die Dokument-ID korrekt ist
- Überprüfen Sie, ob das Dokument erfolgreich hochgeladen wurde
- Stellen Sie sicher, dass das Dokument nicht gelöscht wurde

### "Storage provider not configured"
- Verifizieren Sie die `StorageProvider`-Einstellung in der Konfiguration
- Stellen Sie sicher, dass alle erforderlichen Speichereinstellungen bereitgestellt werden
- Überprüfen Sie die Dienstregistrierung

### "AI provider not configured"
- Verifizieren Sie die `AIProvider`-Einstellung in der Konfiguration
- Stellen Sie sicher, dass ein API-Schlüssel für den ausgewählten Anbieter bereitgestellt wird
- Überprüfen Sie die Dienstregistrierung

### "Invalid file format"
- Stellen Sie sicher, dass die Datei in einem unterstützten Format ist
- Überprüfen Sie die Dateierweiterung und den Inhalt
- Verifizieren Sie, dass die Datei nicht beschädigt ist
</div>

<div class="troubleshooting-section">
## Hilfe erhalten

Wenn Sie immer noch Probleme haben:

1. **Logs überprüfen**: Überprüfen Sie Anwendungslogs für detaillierte Fehlermeldungen
2. **Konfiguration verifizieren**: Überprüfen Sie alle Konfigurationseinstellungen erneut
3. **Mit minimalem Setup testen**: Versuchen Sie es zuerst mit einer einfachen Konfiguration
4. **Abhängigkeiten überprüfen**: Stellen Sie sicher, dass alle erforderlichen Dienste laufen
5. **Dokumentation überprüfen**: Überprüfen Sie andere Dokumentationsseiten für Anleitungen

Für zusätzliche Unterstützung wenden Sie sich bitte an das GitHub-Repository des Projekts oder erstellen Sie ein Issue mit detaillierten Informationen zu Ihrem Problem.
</div>
