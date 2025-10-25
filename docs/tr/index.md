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
                        <span>.NET Standard 2.1</span>
                    </div>
                    <div class="hero-premise-badge">
                        <i class="fas fa-cloud-upload-alt"></i>
                        <span>%100 On-Premise • Cloud • Hybrid</span>
                    </div>
                    <h1 class="hero-title">
                        <span class="text-gradient">SmartRAG</span> ile verilerinizle konuşun
                    </h1>
                    <p class="hero-subtitle">
                        Dokümanlarınızı, veritabanlarınızı, görsellerinizi ve ses dosyalarınızı konuşmalı yapay zeka sistemine dönüştürün.
                    </p>
                    <div class="hero-stats">
                        <div class="stat-card">
                            <div class="stat-number">5</div>
                            <div class="stat-label">Yapay Zeka Sağlayıcı</div>
                        </div>
                        <div class="stat-card">
                            <div class="stat-number">5</div>
                            <div class="stat-label">Depolama Seçeneği</div>
                        </div>
                        <div class="stat-card">
                            <div class="stat-number">4</div>
                            <div class="stat-label">Veritabanı Tipi</div>
                        </div>
                        <div class="stat-card">
                            <div class="stat-number">7</div>
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
                            GitHub
                        </a>
                        <a href="https://www.nuget.org/packages/SmartRAG" class="btn btn-secondary btn-lg" target="_blank">
                            <i class="fas fa-box"></i>
                            NuGet
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
var answer = await searchService.QueryIntelligenceAsync(
    "Belirtilen ana faydalar nelerdir?", 
    maxResults: 5
);

Console.WriteLine(answer.Answer);
// Yapay zeka dokümanlarınızı analiz eder ve akıllı cevaplar verir</code></pre>
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
                <p>PDF, Excel, Word dokümanları, Görseller (OCR), Ses dosyaları (Konuşmadan Metne) ve Veritabanları - hepsi tek bir akıllı platformda birleştirildi.</p>
            </div>
            
            <div class="feature-card">
                <div class="feature-icon">
                    <i class="fas fa-shield-alt"></i>
                </div>
                <h3>On-Premise & Şirket İçi AI</h3>
                <p>Ollama, LM Studio desteğiyle %100 on-premise çalışma. GDPR/KVKK/HIPAA uyumlu. Verileriniz asla altyapınızdan ayrılmaz.</p>
            </div>
            
            <div class="feature-card">
                <div class="feature-icon">
                    <i class="fas fa-comments"></i>
                </div>
                <h3>Konuşma Geçmişi</h3>
                <p>Bağlam farkındalığıyla otomatik oturum tabanlı konuşma yönetimi. Yapay zeka, doğal etkileşimler için önceki soruları hatırlar.</p>
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
                <h3 class="text-center mb-4">Yapay Zeka Sağlayıcıları</h3>
                <div class="provider-grid">
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-brain"></i>
                        </div>
                        <h4>OpenAI</h4>
                        <p>GPT-4 + Vektör Gösterimleri</p>
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
                        <p>Google Yapay Zeka Modelleri</p>
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
                        <p>Açık Kaynak Veritabanı</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-database"></i>
                        </div>
                        <h4>PostgreSQL</h4>
                        <p>Gelişmiş Veritabanı</p>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-feather"></i>
                        </div>
                        <h4>SQLite</h4>
                        <p>Gömülü Veritabanı</p>
                    </div>
                </div>
            </div>
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
            
            <div class="col-lg-6">
                <details>
                    <summary>
                        <h4><i class="fas fa-industry me-2"></i> Üretim Kök Neden Analizi</h4>
                    </summary>
                    <div style="margin-top: 1rem;">
                        <p><strong>Zorluk:</strong> Üretim kalitesinin neden düştüğünü bulun</p>
                        <p><strong>SmartRAG Çözümü:</strong></p>
                        <ul>
                            <li>Excel: Üretim raporları (5 hat, saatlik veri)</li>
                            <li>PostgreSQL: Sensör verisi (100K+ okuma)</li>
                            <li>OCR: Müfettiş notlarıyla kalite kontrol fotoğrafları</li>
                            <li>PDF: Ekipman bakım logları</li>
                        </ul>
                        <p><strong>Sonuç:</strong> Yapay zeka, tam kök nedeni belirlemek için milyonlarca veri noktası üzerindeki sıcaklık anomalilerini ilişkilendirir.</p>
                    </div>
                </details>
            </div>
            
            <div class="col-lg-6">
                <details>
                    <summary>
                        <h4><i class="fas fa-user-tie me-2"></i> Yapay Zeka Özgeçmiş Taraması</h4>
                    </summary>
                    <div style="margin-top: 1rem;">
                        <p><strong>Zorluk:</strong> 500+ başvurudan en iyi adayları bulun</p>
                        <p><strong>SmartRAG Çözümü:</strong></p>
                        <ul>
                            <li>500+ Özgeçmiş PDF'i (birden fazla dil, format)</li>
                            <li>SQL Server: Aday veritabanı (yetenekler, deneyim)</li>
                            <li>OCR: Taranmış sertifikalar (AWS, Azure, Cloud)</li>
                            <li>Ses: Video görüşme transkriptleri</li>
                        </ul>
                        <p><strong>Sonuç:</strong> Yapay zeka adayları birden fazla veri türü arasında dakikalar içinde tarar ve sıralar.</p>
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
                    <p>Yapay zeka destekli koordinasyonla SQL Server, MySQL, PostgreSQL, SQLite'ı aynı anda sorgulayın</p>
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
                        <i class="fas fa-home"></i>
                    </div>
                    <h3>%100 On-Premise</h3>
                    <p>Tam veri gizliliği için Ollama/LM Studio ile tamamen on-premise dağıtım</p>
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
        
        <div class="hero-buttons">
            <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-primary btn-lg">
                <i class="fas fa-rocket"></i>
                Şimdi Başlayın
            </a>
            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-lg" target="_blank">
                <i class="fab fa-github"></i>
                GitHub
            </a>
            <a href="https://www.nuget.org/packages/SmartRAG" class="btn btn-secondary btn-lg" target="_blank">
                <i class="fas fa-download"></i>
                NuGet
            </a>
        </div>
    </div>
</section>

