---
layout: default
title: Beitragen
description: Wie Sie zu SmartRAG beitragen können
lang: de
---

<div class="page-content">
    <div class="container">
        <!-- How to Contribute Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Wie man beiträgt</h2>
                    <p>SmartRAG ist ein Open-Source-Projekt und begrüßt Community-Beiträge. Hier sind verschiedene Wege, wie Sie zum Projekt beitragen können:</p>
                    
                    <h3>🐛 Fehler melden</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-bug me-2"></i>Haben Sie einen Fehler gefunden?</h4>
                        <p class="mb-0">Bitte erstellen Sie einen detaillierten Fehlerbericht in GitHub Issues.</p>
                    </div>
                    
                    <h3>✨ Feature-Anfrage</h3>
                    <div class="alert alert-success">
                        <h4><i class="fas fa-lightbulb me-2"></i>Haben Sie neue Ideen?</h4>
                        <p class="mb-0">Teilen Sie Ihre Feature-Anfragen in GitHub Discussions.</p>
                    </div>
                    
                    <h3>📝 Dokumentation</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-book me-2"></i>Verbessern Sie die Dokumentation</h4>
                        <p class="mb-0">Korrigieren Sie fehlende oder falsche Informationen, fügen Sie neue Beispiele hinzu.</p>
                    </div>
                    
                    <h3>💻 Code-Beitrag</h3>
                    <div class="alert alert-primary">
                        <h4><i class="fas fa-code me-2"></i>Code schreiben</h4>
                        <p class="mb-0">Fügen Sie neue Features hinzu, beheben Sie Fehler, verbessern Sie die Leistung.</p>
                    </div>
                </div>
            </div>
        </section>

        <!-- Prerequisites Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Voraussetzungen</h2>
                    <p>Tools, die Sie benötigen, bevor Sie zu SmartRAG beitragen:</p>
                    
                    <h3>Erforderliche Tools</h3>
                    <ul>
                        <li><strong>.NET 9.0 SDK</strong> oder höher</li>
                        <li><strong>Git</strong> für Versionskontrolle</li>
                        <li><strong>Visual Studio 2022</strong> oder <strong>VS Code</strong></li>
                        <li><strong>Docker</strong> (für Qdrant, Redis Tests)</li>
                    </ul>
                    
                    <h3>Konto-Anforderungen</h3>
                    <ul>
                        <li><strong>GitHub-Konto</strong></li>
                        <li><strong>NuGet-Konto</strong> (für Paketveröffentlichung)</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Development Workflow Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Entwicklungsworkflow</h2>
                    <p>Schritte, die Sie befolgen müssen, um zu SmartRAG beizutragen:</p>
                    
                    <h3>1. Repository forken</h3>
                    <div class="code-example">
                        <pre><code class="language-bash"># Forken Sie das Repository auf GitHub
# Dann klonen Sie es in Ihrem eigenen Konto
git clone https://github.com/IHR_BENUTZERNAME/SmartRAG.git
cd SmartRAG</code></pre>
                    </div>
                    
                    <h3>2. Upstream Remote hinzufügen</h3>
                    <div class="code-example">
                        <pre><code class="language-bash">git remote add upstream https://github.com/byerlikaya/SmartRAG.git
git fetch upstream</code></pre>
                    </div>
                    
                    <h3>3. Neuen Branch erstellen</h3>
                    <div class="code-example">
                        <pre><code class="language-bash"># Feature-Branch erstellen
git checkout -b feature/ihr-feature-name

# Bug-Fix-Branch erstellen
git checkout -b fix/issue-nummer-beschreibung</code></pre>
                    </div>
                    
                    <h3>4. Ihre Änderungen vornehmen</h3>
                    <p>Schreiben Sie Ihren Code, testen Sie ihn und committen Sie:</p>
                    <div class="code-example">
                        <pre><code class="language-bash"># Änderungen stagen
git add .

# Committen
git commit -m "feat: neue Feature-Beschreibung hinzufügen"

# Pushen
git push origin feature/ihr-feature-name</code></pre>
                    </div>
                    
                    <h3>5. Pull Request erstellen</h3>
                    <p>Erstellen Sie einen Pull Request auf GitHub und beschreiben Sie Ihre Änderungen.</p>
                </div>
            </div>
        </section>

        <!-- Code Style Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Code-Stil</h2>
                    <p>Das SmartRAG-Projekt folgt bestimmten Code-Standards:</p>
                    
                    <h3>C# Code-Standards</h3>
                    <ul>
                        <li><strong>PascalCase</strong>: Klassen-, Methoden- und Eigenschaftsnamen</li>
                        <li><strong>camelCase</strong>: Lokale Variablen- und Parameternamen</li>
                        <li><strong>UPPER_CASE</strong>: Konstantenwerte</li>
                        <li><strong>Async/Await</strong>: Für asynchrone Operationen</li>
                    </ul>
                    
                    <h3>Dateiorganisation</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Using-Statements am Anfang der Datei
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// Namespace
namespace SmartRAG.Services
{
    // Klassendefinition
    public class ExampleService : IExampleService
    {
        // Felder
        private readonly ILogger<ExampleService> _logger;
        
        // Konstruktor
        public ExampleService(ILogger<ExampleService> logger)
        {
            _logger = logger;
        }
        
        // Öffentliche Methoden
        public async Task<string> DoSomethingAsync()
        {
            // Implementierung
        }
        
        // Private Methoden
        private void HelperMethod()
        {
            // Implementierung
        }
    }
}</code></pre>
                    </div>
                    
                    <h3>XML-Dokumentation</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">/// <summary>
/// Lädt und verarbeitet ein Dokument
/// </summary>
/// <param name="file">Die zu ladende Datei</param>
/// <returns>Das verarbeitete Dokument</returns>
/// <exception cref="ArgumentException">Ungültiges Dateiformat</exception>
public async Task<Document> UploadDocumentAsync(IFormFile file)
{
    // Implementierung
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Testing Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Testen</h2>
                    <p>Alle Beiträge müssen getestet werden:</p>
                    
                    <h3>Unit-Tests</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task UploadDocument_ValidFile_ReturnsDocument()
{
    // Arrange
    var mockFile = new Mock<IFormFile>();
    var service = new DocumentService(mockLogger.Object);
    
    // Act
    var result = await service.UploadDocumentAsync(mockFile.Object);
    
    // Assert
    Assert.IsNotNull(result);
    Assert.IsNotEmpty(result.Id);
}</code></pre>
                    </div>
                    
                    <h3>Tests ausführen</h3>
                    <div class="code-example">
                        <pre><code class="language-bash"># Alle Tests ausführen
dotnet test

# Bestimmtes Projekt testen
dotnet test tests/SmartRAG.Tests/

# Coverage-Bericht erhalten
dotnet test --collect:"XPlat Code Coverage"</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Documentation Standards Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Dokumentationsstandards</h2>
                    <p>Standards für Dokumentationsbeiträge:</p>
                    
                    <h3>Markdown-Format</h3>
                    <ul>
                        <li><strong>Überschriften</strong>: Verwenden Sie hierarchische Struktur (H1, H2, H3)</li>
                        <li><strong>Code-Blöcke</strong>: Geben Sie die Sprache an (```csharp, ```bash)</li>
                        <li><strong>Links</strong>: Verwenden Sie beschreibende Link-Texte</li>
                        <li><strong>Listen</strong>: Verwenden Sie konsistentes Format</li>
                    </ul>
                    
                    <h3>Mehrsprachige Unterstützung</h3>
                    <p>Alle Dokumentation muss in 4 Sprachen verfügbar sein:</p>
                    <ul>
                        <li><strong>Englisch</strong> (en) - Hauptsprache</li>
                        <li><strong>Türkisch</strong> (tr) - Lokale Sprache</li>
                        <li><strong>Deutsch</strong> (de) - International</li>
                        <li><strong>Russisch</strong> (ru) - International</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Issue Reporting Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Fehler melden</h2>
                    <p>Bitte fügen Sie beim Melden von Fehlern folgende Informationen hinzu:</p>
                    
                    <h3>Erforderliche Informationen</h3>
                    <ul>
                        <li><strong>SmartRAG-Version</strong>: Welche Version verwenden Sie?</li>
                        <li><strong>.NET-Version</strong>: .NET 9.0</li>
                        <li><strong>Betriebssystem</strong>: Windows, Linux, macOS?</li>
                        <li><strong>Fehlermeldung</strong>: Vollständige Fehlermeldung und Stack-Trace</li>
                        <li><strong>Schritte</strong>: Schritte zur Reproduktion des Fehlers</li>
                        <li><strong>Erwartetes Verhalten</strong>: Was haben Sie erwartet?</li>
                        <li><strong>Tatsächliches Verhalten</strong>: Was ist passiert?</li>
                    </ul>
                    
                    <h3>Fehler-Vorlage</h3>
                    <div class="code-example">
                        <pre><code class="language-markdown">## Fehlerbeschreibung
Kurze und klare Fehlerbeschreibung

## Reproduktionsschritte
1. Gehen Sie zu '...'
2. Klicken Sie auf '...'
3. Fehler '...' erscheint

## Erwartetes Verhalten
Was Sie erwartet haben

## Tatsächliches Verhalten
Was passiert ist

## Screenshots
Fügen Sie Screenshots hinzu, falls vorhanden

## Umgebungsinformationen
- SmartRAG-Version: 1.1.0
- .NET-Version: 9.0
- Betriebssystem: Windows 11
- Browser: Chrome 120</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- PR Process Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Pull-Request-Prozess</h2>
                    <p>Punkte, die beim Erstellen von Pull Requests zu beachten sind:</p>
                    
                    <h3>PR-Vorlage</h3>
                    <div class="code-example">
                        <pre><code class="language-markdown">## Änderungsbeschreibung
Was macht dieser PR?

## Änderungstyp
- [ ] Bug-Fix
- [ ] Neues Feature
- [ ] Breaking Change
- [ ] Dokumentationsupdate

## Getestet?
- [ ] Unit-Tests bestehen
- [ ] Integration-Tests bestehen
- [ ] Manueller Test durchgeführt

## Checkliste
- [ ] Code-Standards eingehalten
- [ ] Dokumentation aktualisiert
- [ ] Tests hinzugefügt
- [ ] Kein Breaking Change</code></pre>
                    </div>
                    
                    <h3>Review-Prozess</h3>
                    <ul>
                        <li><strong>Automatische Tests</strong>: CI/CD-Pipeline muss bestehen</li>
                        <li><strong>Code-Review</strong>: Mindestens 1 Genehmigung erforderlich</li>
                        <li><strong>Dokumentation</strong>: Muss bei Bedarf aktualisiert werden</li>
                        <li><strong>Breaking Changes</strong>: Erfordern besondere Aufmerksamkeit</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Release Process Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Release-Prozess</h2>
                    <p>Wie SmartRAG-Versionen veröffentlicht werden:</p>
                    
                    <h3>Versionsarten</h3>
                    <ul>
                        <li><strong>Patch (1.0.1)</strong>: Fehlerbehebungen</li>
                        <li><strong>Minor (1.1.0)</strong>: Neue Features</li>
                        <li><strong>Major (2.0.0)</strong>: Breaking Changes</li>
                    </ul>
                    
                    <h3>Release-Schritte</h3>
                    <ol>
                        <li><strong>Changelog aktualisieren</strong>: Alle Änderungen auflisten</li>
                        <li><strong>Version erhöhen</strong>: .csproj-Dateien aktualisieren</li>
                        <li><strong>Git-Tag</strong>: Tag für neue Version erstellen</li>
                        <li><strong>NuGet-Veröffentlichung</strong>: Paket auf NuGet hochladen</li>
                        <li><strong>GitHub-Release</strong>: Mit Release-Notes veröffentlichen</li>
                    </ol>
                </div>
            </div>
        </section>

        <!-- Community Guidelines Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Community-Richtlinien</h2>
                    <p>Regeln, die in der SmartRAG-Community befolgt werden müssen:</p>
                    
                    <h3>Verhaltensregeln</h3>
                    <ul>
                        <li><strong>Respektvoll sein</strong>: Zeigen Sie Respekt gegenüber allen</li>
                        <li><strong>Konstruktiv sein</strong>: Geben Sie konstruktives Feedback</li>
                        <li><strong>Lernbereit sein</strong>: Akzeptieren Sie neue Ideen</li>
                        <li><strong>Professionell sein</strong>: Verwenden Sie professionelle Sprache</li>
                    </ul>
                    
                    <h3>Kommunikationskanäle</h3>
                    <ul>
                        <li><strong>GitHub Issues</strong>: Fehlermeldungen und Feature-Anfragen</li>
                        <li><strong>GitHub Discussions</strong>: Allgemeine Diskussionen</li>
                        <li><strong>E-Mail</strong>: b.yerlikaya@outlook.com</li>
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
                        <p class="mb-0">Wenn Sie Hilfe beim Beitragen benötigen:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/de/getting-started">Erste Schritte</a></li>
                            <li><a href="{{ site.baseurl }}/de/api-reference">API-Referenz</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub Issues</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">E-Mail-Support</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
