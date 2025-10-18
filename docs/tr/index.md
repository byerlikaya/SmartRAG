---
layout: default
title: SmartRAG Dokümantasyon
description: .NET için Kurumsal Düzeyde RAG Kütüphanesi - Çok Veritabanlı + Çok Modlu Yapay Zeka Platformu
lang: tr
hide_title: true
---

<section class="hero-section">
    <div class="hero-background"></div>
    <div class="container">
        <div class="row align-items-center">
            <div class="col-lg-6">
                <div class="hero-content">
                    <div class="hero-badge">
                        <i class="fas fa-star"></i>
                        <span>.NET Standard 2.0/2.1</span>
                    </div>
                    <h1 class="hero-title">
                        <span class="text-gradient">SmartRAG</span> ile Akıllı Uygulamalar Geliştirin
                    </h1>
                    <p class="hero-subtitle">
                        .NET için Kurumsal Düzeyde RAG Kütüphanesi. Çok Veritabanlı RAG + Çok Modlu Yapay Zeka özellikli. 
                        Dokümanları, görselleri, sesleri ve veritabanlarını yapay zeka destekli işleyin.
                    </p>
                    <div class="hero-stats">
                        <div class="stat-card">
                            <div class="stat-number">5+</div>
                            <div class="stat-label">AI Sağlayıcı</div>
                        </div>
                        <div class="stat-card">
                            <div class="stat-number">5+</div>
                            <div class="stat-label">Depolama Seçeneği</div>
                        </div>
                        <div class="stat-card">
                            <div class="stat-number">4</div>
                            <div class="stat-label">Veritabanı Tipi</div>
                        </div>
                        <div class="stat-card">
                            <div class="stat-number">7+</div>
                            <div class="stat-label">Doküman Formatı</div>
                        </div>
                    </div>
                    <div class="hero-buttons">
                        <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-primary btn-lg">
                            <i class="fas fa-rocket"></i>
                            Başlayın
                        </a>
                        <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-lg" target="_blank">
                            <i class="fab fa-github"></i>
                            GitHub'da Görüntüle
                        </a>
                        <a href="https://www.nuget.org/packages/SmartRAG" class="btn btn-secondary btn-lg" target="_blank">
                            <i class="fas fa-box"></i>
                            NuGet Paketi
                        </a>
                    </div>
                </div>
            </div>
            <div class="col-lg-6">
                <div class="code-window fade-in-up">
                    <div class="code-header">
                        <div class="code-dots">
                            <span></span>
                            <span></span>
                            <span></span>
                        </div>
                        <div class="code-title">QuickStart.cs</div>
                    </div>
                    <div class="code-content">
                        <pre><code class="language-csharp">// SmartRAG'i .NET projenize ekleyin
services.UseSmartRag(configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.Gemini
);

// Doküman yükleyin (PDF, Word, Excel, Görsel, Ses, Veritabanı)
var document = await documentService.UploadDocumentAsync(
    fileStream, "sozlesme.pdf", "application/pdf", "kullanici-id"
);

// Yapay zeka destekli sorular sorun
var cevap = await searchService.QueryIntelligenceAsync(
    "Belirtilen ana faydalar nelerdir?", 
    maxResults: 5
);

Console.WriteLine(cevap.Answer);
// AI dokümanlarınızı analiz eder ve akıllı cevaplar verir</code></pre>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<section class="section section-light">
    <div class="container">
        <div class="section-header">
            <h2 class="section-title">Temel Özellikler</h2>
            <p class="section-subtitle">
                Akıllı kurumsal uygulamalar geliştirmek için güçlü yetenekler
            </p>
        </div>
        
        <div class="feature-grid">
            <div class="feature-card">
                <div class="feature-icon">
                    <i class="fas fa-database"></i>
                </div>
                <h3>Çok Veritabanlı RAG</h3>
                <p>Aynı anda birden fazla veritabanı sorgulayın - SQL Server, MySQL, PostgreSQL, SQLite. Yapay zeka destekli çapraz veritabanı birleştirmeleri ve akıllı sorgu koordinasyonu.</p>
            </div>
            
            <div class="feature-card">
                <div class="feature-icon">
                    <i class="fas fa-layer-group"></i>
                </div>
                <h3>Çok Modlu Zeka</h3>
                <p>PDF, Excel, Word dokümanları, Görseller (OCR), Ses dosyaları (Konuşmadan Metne), Ve tabanları - hepsi tek bir akıllı platformda birleştirildi.</p>
            </div>
            
            <div class="feature-card">
                <div class="feature-icon">
                    <i class="fas fa-shield-alt"></i>
                </div>
                <h3>Yerinde & Yerel AI</h3>
                <p>Ollama, LM Studio desteğiyle %100 yerel çalışma. GDPR/KVKK/HIPAA uyumlu. Verileriniz asla altyapınızdan ayrılmaz.</p>
            </div>
            
            <div class="feature-card">
                <div class="feature-icon">
                    <i class="fas fa-comments"></i>
                </div>
                <h3>Konuşma Geçmişi</h3>
                <p>Bağlam farkındalığıyla otomatik oturum tabanlı konuşma yönetimi. AI, doğal etkileşimler için önceki soruları hatırlar.</p>
            </div>
            
            <div class="feature-card">
                <div class="feature-icon">
                    <i class="fas fa-search"></i>
                </div>
                <h3>Gelişmiş Anlamsal Arama</h3>
                <p>Üstün arama sonuçları için bağlam farkındalığı ve akıllı sıralamayla hibrit puanlama sistemi (%80 anlamsal + %20 anahtar kelime).</p>
            </div>
            
            <div class="feature-card">
                <div class="feature-icon">
                    <i class="fas fa-route"></i>
                </div>
                <h3>Akıllı Sorgu Amacı</h3>
                <p>Sorguları niyet tespitine göre otomatik olarak sohbet veya doküman aramasına yönlendirir. Dil-agnostik tasarım küresel olarak çalışır.</p>
            </div>
            
            <div class="feature-card">
                <div class="feature-icon">
                    <i class="fas fa-lock"></i>
                </div>
                <h3>Kurumsal Güvenlik</h3>
                <p>Otomatik hassas veri temizleme, şifreleme desteği, yapılandırılabilir veri koruması ve uyumluluk-hazır dağıtımlar.</p>
            </div>
            
            <div class="feature-card">
                <div class="feature-icon">
                    <i class="fas fa-check-circle"></i>
                </div>
                <h3>Üretime Hazır</h3>
                <p>Sıfır uyarı politikası, SOLID/DRY prensipleri, kapsamlı hata işleme, thread-safe operasyonlar ve üretimde test edilmiş.</p>
            </div>
        </div>
    </div>
</section>

<section class="section section-dark">
    <div class="container">
        <div class="section-header">
            <h2 class="section-title">Desteklenen Teknolojiler</h2>
            <p class="section-subtitle">
                Önde gelen AI sağlayıcıları, depolama çözümleri ve veritabanlarıyla entegrasyon
            </p>
        </div>
        
        <div class="row g-5">
            <div class="col-lg-6">
                <h3 class="text-center mb-4">AI Sağlayıcıları</h3>
                <div class="provider-grid">
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-brain"></i>
                        </div>
                        <h4>OpenAI</h4>
                        <p>GPT-4 + Embeddings</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-robot"></i>
                        </div>
                        <h4>Anthropic</h4>
                        <p>Claude + VoyageAI</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fab fa-google"></i>
                        </div>
                        <h4>Gemini</h4>
                        <p>Google AI Modelleri</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cloud"></i>
                        </div>
                        <h4>Azure OpenAI</h4>
                        <p>Kurumsal GPT</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-server"></i>
                        </div>
                        <h4>Özel</h4>
                        <p>Ollama / LM Studio</p>
                    </div>
                </div>
            </div>
            
            <div class="col-lg-6">
                <h3 class="text-center mb-4">Depolama & Veritabanları</h3>
                <div class="provider-grid">
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cube"></i>
                        </div>
                        <h4>Qdrant</h4>
                        <p>Vektör Veritabanı</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-bolt"></i>
                        </div>
                        <h4>Redis</h4>
                        <p>Yüksek Performans Önbellek</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-database"></i>
                        </div>
                        <h4>SQL Server</h4>
                        <p>Kurumsal Veritabanı</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-leaf"></i>
                        </div>
                        <h4>MySQL</h4>
                        <p>Açık Kaynak VT</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-elephant"></i>
                        </div>
                        <h4>PostgreSQL</h4>
                        <p>Gelişmiş VT</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-feather"></i>
                        </div>
                        <h4>SQLite</h4>
                        <p>Gömülü VT</p>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<section class="section section-light">
    <div class="container">
        <div class="section-header">
            <h2 class="section-title">Hızlı Başlangıç</h2>
            <p class="section-subtitle">
                Basit kurulum ve yapılandırma ile dakikalar içinde başlayın
            </p>
        </div>
        
        <div class="row">
            <div class="col-lg-12">
                <div class="code-tabs">
                    <button class="code-tab active" data-tab="install">1. Kurulum</button>
                    <button class="code-tab" data-tab="config">2. Yapılandırma</button>
                    <button class="code-tab" data-tab="usage">3. Kullanım</button>
                </div>
                
                <div class="code-panel active" data-tab="install">
                    <pre><code class="language-bash"># .NET CLI ile kurulum
dotnet add package SmartRAG

# Veya Package Manager ile
Install-Package SmartRAG

# Veya .csproj'a ekleyin
&lt;PackageReference Include="SmartRAG" Version="3.0.0" /&gt;</code></pre>
                </div>
                
                <div class="code-panel" data-tab="config">
                    <pre><code class="language-csharp">using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Basit yapılandırma
builder.Services.UseSmartRag(builder.Configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.Gemini);

// Veya gelişmiş yapılandırma
builder.Services.AddSmartRag(builder.Configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new[] { AIProvider.Anthropic, AIProvider.Gemini };
});

var app = builder.Build();</code></pre>
                </div>
                
                <div class="code-panel" data-tab="usage">
                    <pre><code class="language-csharp">public class IntelligenceController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _searchService;
    
    // Doküman yükle (PDF, Word, Excel, Görsel, Ses, Veritabanı)
    [HttpPost("upload")]
    public async Task&lt;IActionResult&gt; Upload(IFormFile file)
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "kullanici-id"
        );
        
        return Ok(document);
    }
    
    // Akıllı sorular sorun
    [HttpPost("ask")]
    public async Task&lt;IActionResult&gt; Ask([FromBody] QuestionRequest request)
    {
        var response = await _searchService.QueryIntelligenceAsync(
            request.Question,
            maxResults: 5
        );
        
        return Ok(response);
    }
}</code></pre>
                </div>
            </div>
        </div>
        
        <div class="text-center mt-5">
            <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-primary btn-lg">
                <i class="fas fa-book-open"></i>
                Tüm Dokümantasyonu Okuyun
            </a>
        </div>
    </div>
</section>

<section class="section section-dark">
    <div class="container">
        <div class="section-header">
            <h2 class="section-title">Gerçek Dünya Kullanım Senaryoları</h2>
            <p class="section-subtitle">
                SmartRAG'in çok veritabanlı ve çok modlu yetenekleriyle neler yapabileceğinizi görün
            </p>
        </div>
        
        <div class="row g-4">
            <div class="col-lg-6">
                <details>
                    <summary>
                        <h4><i class="fas fa-hospital-alt me-2"></i> Tıbbi Kayıt Zekası</h4>
                    </summary>
                    <div style="margin-top: 1rem;">
                        <p><strong>Zorluk:</strong> Departmanlar arasında dağılmış tam hasta geçmişi</p>
                        <p><strong>SmartRAG Çözümü:</strong></p>
                        <ul>
                            <li>PostgreSQL: Hasta kayıtları, kabul, taburcu özetleri</li>
                            <li>Excel: Birden fazla laboratuvardan lab sonuçları</li>
                            <li>OCR: Taranmış reçeteler ve tıbbi dokümanlar</li>
                            <li>Ses: Randevulardan doktor ses notları</li>
                        </ul>
                        <p><strong>Sonuç:</strong> 4 bağlantısız sistemden tam hasta zaman çizelgesi, saatlerce manuel veri toplama tasarrufu.</p>
                    </div>
                </details>
            </div>
            
            <div class="col-lg-6">
                <details>
                    <summary>
                        <h4><i class="fas fa-university me-2"></i> Bankacılık Kredi Değerlendirmesi</h4>
                    </summary>
                    <div style="margin-top: 1rem;">
                        <p><strong>Zorluk:</strong> Kredi kararları için müşteri finansal profilini değerlendirin</p>
                        <p><strong>SmartRAG Çözümü:</strong></p>
                        <ul>
                            <li>SQL Server: İşlem geçmişi (36 ay)</li>
                            <li>MySQL: Kredi kartı kullanımı ve harcama desenleri</li>
                            <li>PostgreSQL: Krediler, ipotek, kredi skoru geçmişi</li>
                            <li>SQLite: Şube ziyaret geçmişi, müşteri etkileşimleri</li>
                            <li>OCR: Taranmış gelir belgeleri, vergi beyannameleri</li>
                            <li>PDF: Hesap özetleri, yatırım portföyleri</li>
                        </ul>
                        <p><strong>Sonuç:</strong> Kapsamlı risk değerlendirmesi için 360° müşteri finansal zekası.</p>
                    </div>
                </details>
            </div>
            
            <div class="col-lg-6">
                <details>
                    <summary>
                        <h4><i class="fas fa-balance-scale me-2"></i> Hukuki İçtihat Keşfi</h4>
                    </summary>
                    <div style="margin-top: 1rem;">
                        <p><strong>Zorluk:</strong> Yıllarca dava geçmişinden kazanma stratejileri bulun</p>
                        <p><strong>SmartRAG Çözümü:</strong></p>
                        <ul>
                            <li>1.000+ PDF hukuki doküman (davalar, özetler, hükümler)</li>
                            <li>SQL Server dava veritabanı (sonuçlar, tarihler, hakimler)</li>
                            <li>OCR: Taranmış mahkeme kararları</li>
                        </ul>
                        <p><strong>Sonuç:</strong> AI, haftalarca manuel araştırma yerine dakikalar içinde kazanan hukuki desenleri keşfeder.</p>
                    </div>
                </details>
            </div>
            
            <div class="col-lg-6">
                <details>
                    <summary>
                        <h4><i class="fas fa-boxes me-2"></i> Öngörücü Envanter Zekası</h4>
                    </summary>
                    <div style="margin-top: 1rem;">
                        <p><strong>Zorluk:</strong> Stok tükenmelerini oluşmadan önce engelleyin</p>
                        <p><strong>SmartRAG Çözümü:</strong></p>
                        <ul>
                            <li>SQLite: Ürün kataloğu (10.000 SKU)</li>
                            <li>SQL Server: Satış verisi (2M işlem/ay)</li>
                            <li>MySQL: Depo envanteri (gerçek zamanlı)</li>
                            <li>PostgreSQL: Tedarikçi verisi (teslimat süreleri)</li>
                        </ul>
                        <p><strong>Sonuç:</strong> Tüm tedarik zincirinde stok tükenmelerini önleyen çapraz veritabanı öngörücü analitik.</p>
                    </div>
                </details>
            </div>
        </div>
        
        <div class="text-center mt-5">
            <a href="{{ site.baseurl }}/tr/examples" class="btn btn-primary btn-lg">
                <i class="fas fa-lightbulb"></i>
                Daha Fazla Örnek Keşfedin
            </a>
        </div>
    </div>
</section>

<section class="section section-light">
    <div class="container">
        <div class="section-header">
            <h2 class="section-title">Neden SmartRAG?</h2>
        </div>
        
        <div class="row g-4">
            <div class="col-lg-3 col-md-6">
                <div class="feature-card text-center">
                    <div class="feature-icon mx-auto">
                        <i class="fas fa-database"></i>
                    </div>
                    <h3>Çok Veritabanlı RAG</h3>
                    <p>AI destekli koordinasyonla SQL Server, MySQL, PostgreSQL, SQLite'ı aynı anda sorgulayın</p>
                </div>
            </div>
            <div class="col-lg-3 col-md-6">
                <div class="feature-card text-center">
                    <div class="feature-icon mx-auto">
                        <i class="fas fa-layer-group"></i>
                    </div>
                    <h3>Çok Modlu</h3>
                    <p>PDF, Excel, Word, Görseller, Ses ve Veritabanları arasında birleşik zeka</p>
                </div>
            </div>
            <div class="col-lg-3 col-md-6">
                <div class="feature-card text-center">
                    <div class="feature-icon mx-auto">
                        <i class="fas fa-shield-check"></i>
                    </div>
                    <h3>%100 Yerel</h3>
                    <p>Tam veri gizliliği için Ollama/LM Studio ile tamamen yerinde dağıtım</p>
                </div>
            </div>
            <div class="col-lg-3 col-md-6">
                <div class="feature-card text-center">
                    <div class="feature-icon mx-auto">
                        <i class="fas fa-globe"></i>
                    </div>
                    <h3>Dil Agnostik</h3>
                    <p>Her dilde çalışır - Türkçe, İngilizce, Almanca, Rusça, Çince, Arapça</p>
                </div>
            </div>
        </div>
    </div>
</section>

<section class="section section-dark">
    <div class="container text-center">
        <div class="section-header">
            <h2 class="section-title">Harika Bir Şey İnşa Etmeye Hazır Mısınız?</h2>
            <p class="section-subtitle">
                SmartRAG ile akıllı uygulamalar geliştiren geliştiricilere katılın
            </p>
        </div>
        
        <div class="hero-buttons" style="justify-content: center;">
            <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-primary btn-lg">
                <i class="fas fa-rocket"></i>
                Şimdi Başlayın
            </a>
            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-lg" target="_blank">
                <i class="fab fa-github"></i>
                GitHub'da Yıldızlayın
            </a>
            <a href="https://www.nuget.org/packages/SmartRAG" class="btn btn-secondary btn-lg" target="_blank">
                <i class="fas fa-download"></i>
                NuGet'ten İndirin
            </a>
        </div>
    </div>
</section>

