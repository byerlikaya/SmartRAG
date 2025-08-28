---
layout: default
title: Başlangıç
description: SmartRAG için hızlı kurulum ve kurulum rehberi
lang: tr
---

<div class="page-header">
    <div class="container">
        <div class="row">
            <div class="col-lg-8 mx-auto text-center">
                <h1 class="page-title">SmartRAG ile Başlangıç</h1>
                <p class="page-description">
                    SmartRAG'i .NET uygulamanızda sadece birkaç dakikada kurun ve yapılandırın
                </p>
            </div>
        </div>
    </div>
</div>

<div class="page-content">
    <div class="container">
        <!-- Installation Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Kurulum</h2>
                    <p>SmartRAG NuGet paketi olarak mevcuttur ve .NET Standard 2.0/2.1'i destekler, bu da .NET Framework 4.6.1+, .NET Core 2.0+ ve .NET 5+ uygulamalarıyla uyumlu olmasını sağlar. Tercih ettiğiniz kurulum yöntemini seçin:</p>
                    
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
                    <h2>Yapılandırma</h2>
                    <p>SmartRAG'i <code>Program.cs</code> veya <code>Startup.cs</code> dosyanızda yapılandırın:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
using SmartRAG;

var builder = WebApplication.CreateBuilder(args);

// SmartRAG servislerini ekle
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
                    <h2>Hızlı Örnek</h2>
                    <p>Başlamanız için basit bir örnek:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Document servisini enjekte et
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
                    <h2>Sonraki Adımlar</h2>
                    <p>SmartRAG'i kurduğunuza ve yapılandırdığınıza göre, bu özellikleri keşfedebilirsiniz:</p>
                    
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="feature-card">
                                <div class="feature-icon">
                                    <i class="fas fa-cog"></i>
                                </div>
                                <h3>Yapılandırma</h3>
                                <p>Gelişmiş yapılandırma seçenekleri ve en iyi uygulamalar hakkında bilgi edinin.</p>
                                <a href="{{ site.baseurl }}/tr/configuration" class="btn btn-outline-primary btn-sm">Yapılandır</a>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="feature-card">
                                <div class="feature-icon">
                                    <i class="fas fa-code"></i>
                                </div>
                                <h3>API Referansı</h3>
                                <p>Örneklerle birlikte tam API dokümantasyonunu keşfedin.</p>
                                <a href="{{ site.baseurl }}/tr/api-reference" class="btn btn-outline-primary btn-sm">API'yi Görüntüle</a>
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
                        <h4><i class="fas fa-question-circle me-2"></i>Yardıma mı ihtiyacınız var?</h4>
                        <p class="mb-0">Herhangi bir sorunla karşılaşırsanız veya yardıma ihtiyacınız varsa:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub'da issue açın</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">E-posta ile destek alın</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>