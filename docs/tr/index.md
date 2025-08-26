---
layout: default
title: SmartRAG Dokümantasyonu
description: .NET uygulamaları için kurumsal düzeyde RAG kütüphanesi
lang: tr
hide_title: true
---

<!-- Hero Section -->
<section class="hero-section">
    <div class="hero-background"></div>
    <div class="container">
        <div class="row align-items-center min-vh-100">
            <div class="col-lg-6">
                <div class="hero-content">
                    <div class="hero-badge">
                        <i class="fas fa-star"></i>
                        <span>Kurumsal Hazır</span>
                    </div>
                    <h1 class="hero-title">
                        Akıllı Uygulamalar Oluşturun 
                        <span class="text-gradient">SmartRAG</span> ile
                    </h1>
                    <p class="hero-description">
                        Belge işleme, AI embedding'leri ve anlamsal arama için en güçlü .NET kütüphanesi. 
                        Uygulamalarınızı kurumsal düzeyde RAG yetenekleri ile dönüştürün.
                    </p>
                    <div class="hero-stats">
                        <div class="stat-item">
                            <div class="stat-number">5+</div>
                            <div class="stat-label">AI Sağlayıcısı</div>
                        </div>
                        <div class="stat-item">
                            <div class="stat-number">5+</div>
                            <div class="stat-label">Depolama Seçeneği</div>
                        </div>
                        <div class="stat-item">
                            <div class="stat-number">100%</div>
                            <div class="stat-label">Açık Kaynak</div>
                        </div>
                    </div>
                    <div class="hero-buttons">
                        <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-primary btn-lg">
                            <i class="fas fa-rocket"></i>
                            Başlayın
                        </a>
                        <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-light btn-lg" target="_blank">
                            <i class="fab fa-github"></i>
                            GitHub'da Görüntüle
                        </a>
                    </div>
                </div>
            </div>
            <div class="col-lg-6">
                <div class="hero-visual">
                    <div class="code-window">
                        <div class="code-header">
                            <div class="code-dots">
                                <span></span>
                                <span></span>
                                <span></span>
                            </div>
                            <div class="code-title">SmartRAG.cs</div>
                        </div>
                        <div class="code-content">
                            <pre><code class="language-csharp">// SmartRAG'ı projenize ekleyin
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});

// Belge yükleyin ve işleyin
var document = await documentService
    .UploadDocumentAsync(file);

// Anlamsal arama yapın
var results = await documentService
    .SearchAsync("your query");</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Features Section -->
<section class="features-section">
    <div class="container">
        <div class="section-header text-center">
            <h2 class="section-title">Temel Özellikler</h2>
            <p class="section-description">
                Akıllı uygulamalar oluşturmak için güçlü yetenekler
            </p>
        </div>
        
        <div class="row g-4">
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-brain"></i>
                    </div>
                    <h3>AI Destekli</h3>
                    <p>Güçlü embedding'ler ve akıllı işleme için önde gelen AI sağlayıcıları ile entegrasyon.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-file-alt"></i>
                    </div>
                    <h3>Çoklu Format Desteği</h3>
                    <p>Otomatik format algılama ile Word, PDF, Excel ve metin belgelerini işleyin.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-search"></i>
                    </div>
                    <h3>Anlamsal Arama</h3>
                    <p>Benzerlik puanlaması ve akıllı sonuç sıralaması ile gelişmiş arama.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-database"></i>
                    </div>
                    <h3>Esnek Depolama</h3>
                    <p>Esnek dağıtım seçenekleri için çoklu depolama backend'leri.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-rocket"></i>
                    </div>
                    <h3>Kolay Entegrasyon</h3>
                    <p>Dependency injection ile basit kurulum. Dakikalar içinde başlayın.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-shield-alt"></i>
                    </div>
                    <h3>Üretim Hazır</h3>
                    <p>Performans ve güvenilirlik ile kurumsal ortamlar için inşa edilmiştir.</p>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Providers Section -->
<section class="providers-section">
    <div class="container">
        <div class="section-header text-center">
            <h2 class="section-title">Desteklenen Teknolojiler</h2>
            <p class="section-description">
                Önde gelen AI sağlayıcıları ve depolama çözümleri arasından seçin
            </p>
        </div>
        
        <div class="providers-grid">
            <div class="provider-category">
                <h3>AI Sağlayıcıları</h3>
                <div class="provider-cards">
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fab fa-google"></i>
                        </div>
                        <h4>Gemini</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-brain"></i>
                        </div>
                        <h4>OpenAI</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cloud"></i>
                        </div>
                        <h4>Azure OpenAI</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-robot"></i>
                        </div>
                        <h4>Anthropic</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cogs"></i>
                        </div>
                        <h4>Özel</h4>
                    </div>
                </div>
            </div>
            
            <div class="provider-category">
                <h3>Depolama Sağlayıcıları</h3>
                <div class="provider-cards">
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cube"></i>
                        </div>
                        <h4>Qdrant</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-database"></i>
                        </div>
                        <h4>Redis</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-hdd"></i>
                        </div>
                        <h4>SQLite</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-microchip"></i>
                        </div>
                        <h4>In-Memory</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-folder-open"></i>
                        </div>
                        <h4>Dosya Sistemi</h4>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Quick Start Section -->
<section class="quick-start-section">
    <div class="container">
        <div class="row align-items-center">
            <div class="col-lg-6">
                <div class="quick-start-content">
                    <h2>Dakikalar İçinde Başlayın</h2>
                    <p>.NET uygulamalarınız için basit ve güçlü entegrasyon.</p>
                    
                    <div class="steps">
                        <div class="step">
                            <div class="step-number">1</div>
                            <div class="step-content">
                                <h4>Paketi Yükleyin</h4>
                                <p>SmartRAG'ı NuGet ile ekleyin</p>
                            </div>
                        </div>
                        <div class="step">
                            <div class="step-number">2</div>
                            <div class="step-content">
                                <h4>Servisleri Yapılandırın</h4>
                                <p>AI ve depolama sağlayıcılarını ayarlayın</p>
                            </div>
                        </div>
                        <div class="step">
                            <div class="step-number">3</div>
                            <div class="step-content">
                                <h4>Geliştirmeye Başlayın</h4>
                                <p>Belgeleri yükleyin ve arama yapın</p>
                            </div>
                        </div>
                    </div>
                    
                    <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-primary btn-lg">
                        <i class="fas fa-play"></i>
                        Geliştirmeye Başla
                    </a>
                </div>
            </div>
            <div class="col-lg-6">
                <div class="code-example">
                    <div class="code-tabs">
                        <button class="code-tab active" data-tab="install">Yükle</button>
                        <button class="code-tab" data-tab="configure">Yapılandır</button>
                        <button class="code-tab" data-tab="use">Kullan</button>
                    </div>
                    <div class="code-content">
                        <div class="code-panel active" id="install">
                            <pre><code class="language-bash">dotnet add package SmartRAG</code></pre>
                        </div>
                        <div class="code-panel" id="configure">
                            <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});</code></pre>
                        </div>
                        <div class="code-panel" id="use">
                            <pre><code class="language-csharp">var documentService = serviceProvider
    .GetRequiredService&lt;IDocumentService&gt;();

var results = await documentService
    .SearchAsync("your query");</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Documentation Section -->
<section class="documentation-section">
    <div class="container">
        <div class="section-header text-center">
            <h2 class="section-title">Dokümantasyon</h2>
            <p class="section-description">
                SmartRAG ile geliştirme için ihtiyacınız olan her şey
            </p>
        </div>
        
        <div class="row g-4">
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/tr/getting-started" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-rocket"></i>
                    </div>
                    <h3>Başlangıç</h3>
                    <p>Hızlı kurulum ve kurulum kılavuzu</p>
                </a>
            </div>
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/tr/configuration" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-cog"></i>
                    </div>
                    <h3>Yapılandırma</h3>
                    <p>Detaylı yapılandırma seçenekleri</p>
                </a>
            </div>
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/tr/api-reference" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-code"></i>
                    </div>
                    <h3>API Referansı</h3>
                    <p>Tam API dokümantasyonu</p>
                </a>
            </div>
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/tr/examples" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-lightbulb"></i>
                    </div>
                    <h3>Örnekler</h3>
                    <p>Gerçek dünya örnekleri ve örnekler</p>
                </a>
            </div>
        </div>
    </div>
</section>

<!-- CTA Section -->
<section class="cta-section">
    <div class="container">
        <div class="cta-content text-center">
            <h2>Harika Bir Şey Oluşturmaya Hazır mısınız?</h2>
            <p>Akıllı uygulamalar oluşturmak için SmartRAG kullanan binlerce geliştiriciye katılın</p>
            <div class="cta-buttons">
                <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-primary btn-lg">
                    <i class="fas fa-rocket"></i>
                    Şimdi Başlayın
                </a>
                <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-light btn-lg" target="_blank">
                    <i class="fab fa-github"></i>
                    GitHub'da Yıldızla
                </a>
            </div>
        </div>
    </div>
</section>