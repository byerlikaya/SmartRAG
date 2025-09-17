---
layout: default
title: Başlangıç
description: SmartRAG'i .NET uygulamanızda sadece birkaç dakikada kurun ve yapılandırın
lang: tr
---

<div class="page-content">
    <div class="container">
        <!-- Installation Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Kurulum</h2>
                    <p>SmartRAG NuGet paketi olarak mevcuttur ve .NET Standard 2.0/2.1'i destekler, bu da .NET Framework 4.6.1+, .NET Core 2.0+ ve .NET 5+ uygulamalarıyla uyumlu olmasını sağlar. Tercih ettiğiniz kurulum yöntemini seçin:</p>
                    
                    <div class="code-example">
                        <div class="code-tabs">
                            <button class="code-tab active" data-tab="cli">.NET CLI</button>
                            <button class="code-tab" data-tab="pm">Package Manager</button>
                            <button class="code-tab" data-tab="xml">Package Reference</button>
                        </div>
                        
                        <div class="code-panel active" data-tab="cli">
                            <pre><code class="language-bash">dotnet add package SmartRAG</code></pre>
                        </div>
                        <div class="code-panel" data-tab="pm">
                            <pre><code class="language-bash">Install-Package SmartRAG</code></pre>
                        </div>
                        <div class="code-panel" data-tab="xml">
                            <pre><code class="language-xml">&lt;PackageReference Include="SmartRAG" Version="2.3.0" /&gt;</code></pre>
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
                    <!-- Updated for v2.3.0 -->
                    <p>Konuşma geçmişi ile gerçek SmartRAG implementasyonunu kullanan basit bir örnek:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Document search servisini enjekte et
public class SearchController : ControllerBase
{
    private readonly IDocumentSearchService _documentSearchService;
    
    public SearchController(IDocumentSearchService documentSearchService)
    {
        _documentSearchService = documentSearchService;
    }
    
    [HttpPost("search")]
    public async Task<ActionResult<object>> Search([FromBody] SearchRequest request)
    {
        string query = request?.Query ?? string.Empty;
        int maxResults = request?.MaxResults ?? 5;
        string sessionId = request?.SessionId ?? Guid.NewGuid().ToString();

        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query cannot be empty");

        try
        {
            var response = await _documentSearchService.QueryIntelligenceAsync(query, maxResults);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}

public class SearchRequest
{
    [Required]
    public string Query { get; set; } = string.Empty;

    [Range(1, 50)]
    [DefaultValue(5)]
    public int MaxResults { get; set; } = 5;

    /// <summary>
    /// Session ID for conversation history
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Conversation History Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>💬 Konuşma Geçmişi</h2>
                    <p>SmartRAG oturum tabanlı bağlam farkındalığı kullanarak konuşma geçmişini otomatik olarak yönetir. Her konuşma oturumu birden fazla soru ve cevap arasında bağlamı korur.</p>
                    
                    <h3>Nasıl Çalışır</h3>
                    <ul>
                        <li><strong>Oturum Yönetimi</strong>: Her konuşma benzersiz bir oturum kimliği kullanır</li>
                        <li><strong>Otomatik Bağlam</strong>: Önceki sorular ve cevaplar otomatik olarak bağlama dahil edilir</li>
                        <li><strong>Akıllı Kısaltma</strong>: Konuşma geçmişi optimal performansı korumak için akıllıca kısaltılır</li>
                        <li><strong>Depolama Entegrasyonu</strong>: Konuşma verileri yapılandırılan depolama sağlayıcısı kullanılarak saklanır</li>
                    </ul>

                    <h3>Kullanım Örneği</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// İlk soru
var firstRequest = new SearchRequest
{
    Query = "Makine öğrenmesi nedir?",
    SessionId = "user-session-123",
    MaxResults = 5
};

// Takip sorusu (önceki bağlamı hatırlar)
var followUpRequest = new SearchRequest
{
    Query = "Denetimli öğrenmeyi daha detaylı açıklayabilir misin?",
    SessionId = "user-session-123",  // Aynı oturum kimliği
    MaxResults = 5
};

// Başka bir takip sorusu
var anotherRequest = new SearchRequest
{
    Query = "Derin öğrenmenin avantajları nelerdir?",
    SessionId = "user-session-123",  // Aynı oturum kimliği
    MaxResults = 5
};</code></pre>
                    </div>

                    <div class="alert alert-success">
                        <h4><i class="fas fa-lightbulb me-2"></i>Pro İpucu</h4>
                        <p class="mb-0">Farklı kullanıcı oturumları veya konuşma thread'leri arasında bağlamı korumak için anlamlı oturum kimlikleri (kullanıcı kimlikleri veya konuşma kimlikleri gibi) kullanın.</p>
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
        <section class="content-section help-section">
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