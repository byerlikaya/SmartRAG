---
layout: default
title: Getting Started
description: Install and configure SmartRAG in your .NET application in just a few minutes
lang: en
---

<div class="page-content">
    <div class="container">
        <!-- Installation Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Installation</h2>
                    <p>SmartRAG is available as a NuGet package and supports .NET Standard 2.0/2.1, making it compatible with .NET Framework 4.6.1+, .NET Core 2.0+, and .NET 5+ applications. Choose your preferred installation method:</p>
                    
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
                            <pre><code class="language-xml">&lt;PackageReference Include="SmartRAG" Version="2.0.0" /&gt;</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Configuration</h2>
                    <p>Configure SmartRAG in your <code>Program.cs</code> or <code>Startup.cs</code>:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
using SmartRAG;

var builder = WebApplication.CreateBuilder(args);

// Add SmartRAG services
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
                    <h2>Quick Example</h2>
                    <p>Here's a simple example to get you started:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Inject the document service
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
                    <h2>Next Steps</h2>
                    <p>Now that you have SmartRAG installed and configured, explore these features:</p>
                    
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="feature-card">
                                <div class="feature-icon">
                                    <i class="fas fa-cog"></i>
                                </div>
                                <h3>Configuration</h3>
                                <p>Learn about advanced configuration options and best practices.</p>
                                <a href="{{ site.baseurl }}/en/configuration" class="btn btn-outline-primary btn-sm">Configure</a>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="feature-card">
                                <div class="feature-icon">
                                    <i class="fas fa-code"></i>
                                </div>
                                <h3>API Reference</h3>
                                <p>Explore the complete API documentation with examples.</p>
                                <a href="{{ site.baseurl }}/en/api-reference" class="btn btn-outline-primary btn-sm">View API</a>
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
                        <h4><i class="fas fa-question-circle me-2"></i>Need Help?</h4>
                        <p class="mb-0">If you encounter any issues or need assistance:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">Open an issue on GitHub</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Contact support via email</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>