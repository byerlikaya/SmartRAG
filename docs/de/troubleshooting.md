---
layout: default
title: Fehlerbehebung
nav_order: 5
---

# Fehlerbehebung

Diese Seite bietet Lösungen für häufige Probleme, die bei der Verwendung von SmartRAG auftreten können.

## Konfigurationsprobleme

### API-Schlüssel-Konfiguration

**Problem**: Authentifizierungsfehler bei AI- oder Speicheranbietern.

**Lösung**: Stellen Sie sicher, dass Ihre API-Schlüssel korrekt in `appsettings.json` konfiguriert sind:

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

**Problem**: Dependency Injection-Fehler.

**Lösung**: Stellen Sie sicher, dass SmartRAG-Dienste korrekt in Ihrer `Program.cs` registriert sind:

```csharp
using SmartRAG.Extensions;

var builder = WebApplication.CreateBuilder(args);

// SmartRAG-Dienste hinzufügen
builder.Services.AddSmartRag(builder.Configuration);

var app = builder.Build();
app.UseSmartRag(builder.Configuration, StorageProvider.Qdrant, AIProvider.Anthropic);
app.Run();
```

## Dokument-Upload-Probleme

### Dateigrößenbeschränkungen

**Problem**: Große Dokumente können nicht hochgeladen oder verarbeitet werden.

**Lösung**: 
- Überprüfen Sie die Dateigrößenlimits Ihrer Anwendung in `appsettings.json`
- Erwägen Sie das Aufteilen großer Dokumente in kleinere Chunks
- Stellen Sie sicher, dass ausreichend Speicher für die Verarbeitung verfügbar ist

### Nicht unterstützte Dateitypen

**Problem**: Fehler bei bestimmten Dateiformaten.

**Lösung**: SmartRAG unterstützt gängige Textformate. Stellen Sie sicher, dass Ihre Dateien in unterstützten Formaten sind:
- PDF-Dateien
- Textdateien (.txt)
- Word-Dokumente (.docx)
- Markdown-Dateien (.md)

## Such- und Abrufprobleme

### Keine Suchergebnisse

**Problem**: Suchanfragen liefern keine Ergebnisse.

**Mögliche Lösungen**:
1. **Dokument-Upload überprüfen**: Stellen Sie sicher, dass Dokumente erfolgreich hochgeladen wurden
2. **Embeddings verifizieren**: Überprüfen Sie, ob Embeddings korrekt generiert wurden
3. **Anfragespezifität**: Versuchen Sie spezifischere Suchbegriffe
4. **Speicherverbindung**: Verifizieren Sie, dass Ihr Speicheranbieter zugänglich ist

### Schlechte Suchqualität

**Problem**: Suchergebnisse sind nicht relevant.

**Lösungen**:
- Passen Sie `MaxChunkSize` und `ChunkOverlap`-Einstellungen an
- Verwenden Sie spezifischere Suchanfragen
- Stellen Sie sicher, dass Dokumente korrekt formatiert sind
- Überprüfen Sie, ob Embeddings aktuell sind

## Leistungsprobleme

### Langsame Dokumentverarbeitung

**Problem**: Dokument-Upload und -verarbeitung dauert zu lange.

**Lösungen**:
- Erhöhen Sie `MaxChunkSize`, um die Anzahl der Chunks zu reduzieren
- Verwenden Sie einen leistungsstärkeren AI-Anbieter
- Optimieren Sie Ihre Speicheranbieter-Konfiguration
- Erwägen Sie die Verwendung von async-Operationen in Ihrer Anwendung

### Speicherprobleme

**Problem**: Anwendung läuft während der Verarbeitung aus dem Speicher.

**Lösungen**:
- Reduzieren Sie `MaxChunkSize`, um kleinere Chunks zu erstellen
- Verarbeiten Sie Dokumente in Batches
- Überwachen Sie die Speichernutzung und optimieren Sie entsprechend
- Erwägen Sie Streaming-Operationen für große Dateien

## Speicheranbieter-Probleme

### Qdrant-Verbindungsprobleme

**Problem**: Kann nicht zu Qdrant verbinden.

**Lösungen**:
- Verifizieren Sie, dass der Qdrant-API-Schlüssel korrekt ist
- Überprüfen Sie die Netzwerkverbindung zum Qdrant-Service
- Stellen Sie sicher, dass der Qdrant-Service läuft und zugänglich ist
- Überprüfen Sie Firewall-Einstellungen

### Redis-Verbindungsprobleme

**Problem**: Kann nicht zu Redis verbinden.

**Lösungen**:
- Verifizieren Sie die Redis-Verbindungszeichenfolge
- Stellen Sie sicher, dass der Redis-Server läuft
- Überprüfen Sie die Netzwerkverbindung
- Verifizieren Sie die Redis-Konfiguration in `appsettings.json`

### SQLite-Probleme

**Problem**: SQLite-Datenbankfehler.

**Lösungen**:
- Überprüfen Sie Dateiberechtigungen für das Datenbankverzeichnis
- Stellen Sie sicher, dass ausreichend Festplattenspeicher vorhanden ist
- Verifizieren Sie, dass der Datenbankdateipfad korrekt ist
- Überprüfen Sie auf Datenbankbeschädigung

## AI-Anbieter-Probleme

### Anthropic-API-Fehler

**Problem**: Fehler von der Anthropic-API.

**Lösungen**:
- Verifizieren Sie, dass der API-Schlüssel gültig ist und ausreichend Guthaben hat
- Überprüfen Sie API-Ratenlimits
- Stellen Sie sicher, dass die API-Endpunkt-Konfiguration korrekt ist
- Überwachen Sie API-Nutzung und Kontingente

### OpenAI-API-Fehler

**Problem**: Fehler von der OpenAI-API.

**Lösungen**:
- Verifizieren Sie, dass der API-Schlüssel gültig ist
- Überprüfen Sie API-Ratenlimits und Kontingente
- Stellen Sie sicher, dass die Modell-Konfiguration korrekt ist
- Überwachen Sie die API-Nutzung

## Testen und Debugging

### Unit-Tests

**Problem**: Tests schlagen fehl aufgrund von SmartRAG-Abhängigkeiten.

**Lösung**: Verwenden Sie Mocking für SmartRAG-Dienste in Unit-Tests:

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

**Problem**: Integrationstests schlagen fehl.

**Lösung**: Verwenden Sie Test-Konfiguration und stellen Sie sicher, dass das Setup korrekt ist:

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

## Hilfe erhalten

Wenn Sie immer noch Probleme haben:

1. **Logs überprüfen**: Überprüfen Sie Anwendungslogs für detaillierte Fehlermeldungen
2. **Konfiguration verifizieren**: Überprüfen Sie alle Konfigurationseinstellungen erneut
3. **Mit minimalem Setup testen**: Versuchen Sie es zuerst mit einer einfachen Konfiguration
4. **Abhängigkeiten überprüfen**: Stellen Sie sicher, dass alle erforderlichen Dienste laufen
5. **Dokumentation überprüfen**: Überprüfen Sie andere Dokumentationsseiten für Anleitungen

Für zusätzliche Unterstützung wenden Sie sich bitte an das GitHub-Repository des Projekts oder erstellen Sie ein Issue mit detaillierten Informationen zu Ihrem Problem.
