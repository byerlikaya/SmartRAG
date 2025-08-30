---
layout: default
title: Erste Schritte
description: Installieren und konfigurieren Sie SmartRAG in Ihrer .NET-Anwendung in nur wenigen Minuten
lang: de
---

<div class="page-content">
    <div class="container">
        <!-- Installation Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Installation</h2>
                    <p>SmartRAG ist als NuGet-Paket verfügbar. Wählen Sie Ihre bevorzugte Installationsmethode:</p>
                    
                    <div class="code-tabs">
                        <div class="code-tab active" data-tab="cli">.NET CLI</div>
                        <div class="code-tab" data-tab="pm">Package Manager</div>
                        <div class="code-tab" data-tab="xml">Package Reference</div>
                    </div>
                    
                    <div class="code-content">
                        <div class="code-panel active" id="cli">
                            <pre><code class="language-bash">dotnet add package SmartRAG</code></pre>
                        </div>
                        <div class="code-panel" id="pm">
                            <pre><code class="language-bash">Install-Package SmartRAG</code></pre>
                        </div>
                        <div class="code-panel" id="xml">
                            <pre><code class="language-xml">&lt;PackageReference Include="SmartRAG" Version="1.0.0" /&gt;</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Konfiguration</h2>
                    <p>Konfigurieren Sie SmartRAG in Ihrer <code>Program.cs</code> oder <code>Startup.cs</code>:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
using SmartRAG;

var builder = WebApplication.CreateBuilder(args);

// SmartRAG-Services hinzufügen
builder.Services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});

var app = builder.Build();</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Quick Example Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Schnelles Beispiel</h2>
                    <p>Hier ist ein einfaches Beispiel zum Einstieg:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Document-Service injizieren
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    
    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }
    
    [HttpPost("upload")]
    public async Task&lt;IActionResult&gt; UploadDocument(IFormFile file)
    {
        var document = await _documentService.UploadDocumentAsync(file);
        return Ok(document);
    }
    
    [HttpPost("search")]
    public async Task&lt;IActionResult&gt; Search([FromBody] string query)
    {
        var results = await _documentService.SearchAsync(query);
        return Ok(results);
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Next Steps Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Nächste Schritte</h2>
                    <p>Jetzt, da Sie SmartRAG installiert und konfiguriert haben, erkunden Sie diese Funktionen:</p>
                    
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="feature-card">
                                <div class="feature-icon">
                                    <i class="fas fa-cog"></i>
                                </div>
                                <h3>Konfiguration</h3>
                                <p>Erfahren Sie mehr über erweiterte Konfigurationsoptionen und bewährte Praktiken.</p>
                                <a href="{{ site.baseurl }}/de/configuration" class="btn btn-outline-primary btn-sm">Konfigurieren</a>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="feature-card">
                                <div class="feature-icon">
                                    <i class="fas fa-code"></i>
                                </div>
                                <h3>API-Referenz</h3>
                                <p>Erkunden Sie die vollständige API-Dokumentation mit Beispielen.</p>
                                <a href="{{ site.baseurl }}/de/api-reference" class="btn btn-outline-primary btn-sm">API ansehen</a>
                            </div>
                        </div>
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
                        <p class="mb-0">Wenn Sie auf Probleme stoßen oder Unterstützung benötigen:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">Öffnen Sie ein Issue auf GitHub</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Kontaktieren Sie den Support per E-Mail</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>