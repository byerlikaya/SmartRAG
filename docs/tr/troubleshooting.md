---
layout: default
title: Sorun Giderme
description: SmartRAG için yaygın sorunlar ve çözümleri
lang: tr
---

<!-- Page Header -->
<div class="page-header">
    <div class="container">
        <h1 class="page-title">Sorun Giderme</h1>
        <p class="page-description">
            SmartRAG kullanırken karşılaşabileceğiniz yaygın sorunlar ve çözümleri
        </p>
    </div>
</div>

<!-- Main Content -->
<div class="main-content">
    <div class="container">
        
        <!-- Quick Navigation -->
        <div class="content-section">
            <div class="row">
                <div class="col-12">
                    <div class="alert alert-info" role="alert">
                        <i class="fas fa-info-circle me-2"></i>
                        <strong>Yardıma mı ihtiyacınız var?</strong> Burada çözüm bulamadıysanız, 
                        <a href="{{ site.baseurl }}/tr/getting-started" class="alert-link">Başlangıç Rehberi</a>'mizi kontrol edin 
                        veya <a href="https://github.com/byerlikaya/SmartRAG" class="alert-link" target="_blank">GitHub</a>'da sorun oluşturun.
                    </div>
                </div>
            </div>
        </div>

        <!-- Configuration Issues -->
        <div class="content-section">
            <h2>Yapılandırma Sorunları</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-key"></i>
                        </div>
                        <h3>API Anahtarı Yapılandırması</h3>
                        <p><strong>Sorun:</strong> AI veya depolama sağlayıcıları ile kimlik doğrulama hataları alıyorsunuz.</p>
                        <p><strong>Çözüm:</strong> API anahtarlarınızın <code>appsettings.json</code> dosyasında doğru yapılandırıldığından emin olun:</p>
                        
                        <div class="code-example">
                            <pre><code class="language-json">{
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
}</code></pre>
                        </div>
                        
                        <p>Veya ortam değişkenlerini ayarlayın:</p>
                        <div class="code-example">
                            <pre><code class="language-bash"># Ortam değişkenlerini ayarlayın
export ANTHROPIC_API_KEY=your-anthropic-api-key
export QDRANT_API_KEY=your-qdrant-api-key</code></pre>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-cogs"></i>
                        </div>
                        <h3>Servis Kayıt Sorunları</h3>
                        <p><strong>Sorun:</strong> Dependency injection hataları alıyorsunuz.</p>
                        <p><strong>Çözüm:</strong> SmartRAG servislerinin <code>Program.cs</code> dosyanızda doğru şekilde kayıtlı olduğundan emin olun:</p>
                        
                        <div class="code-example">
                            <pre><code class="language-csharp">using SmartRAG.Extensions;

var builder = WebApplication.CreateBuilder(args);

// SmartRAG servislerini ekleyin
builder.Services.AddSmartRag(builder.Configuration);

var app = builder.Build();
app.UseSmartRag(builder.Configuration, StorageProvider.Qdrant, AIProvider.Anthropic);
app.Run();</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Document Upload Issues -->
        <div class="content-section">
            <h2>Belge Yükleme Sorunları</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-file-upload"></i>
                        </div>
                        <h3>Dosya Boyutu Sınırlamaları</h3>
                        <p><strong>Sorun:</strong> Büyük belgeler yüklenemiyor veya işlenemiyor.</p>
                        <p><strong>Çözümler:</strong></p>
                        <ul>
                            <li><code>appsettings.json</code> dosyasındaki dosya boyutu sınırlarını kontrol edin</li>
                            <li>Büyük belgeleri daha küçük parçalara bölmeyi düşünün</li>
                            <li>İşlem için yeterli bellek olduğundan emin olun</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-file-alt"></i>
                        </div>
                        <h3>Desteklenmeyen Dosya Türleri</h3>
                        <p><strong>Sorun:</strong> Belirli dosya formatları için hatalar alıyorsunuz.</p>
                        <p><strong>Çözüm:</strong> SmartRAG yaygın metin formatlarını destekler:</p>
                        <ul>
                            <li>PDF dosyaları (.pdf)</li>
                            <li>Metin dosyaları (.txt)</li>
                            <li>Word belgeleri (.docx)</li>
                            <li>Markdown dosyaları (.md)</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Search and Retrieval Issues -->
        <div class="content-section">
            <h2>Arama ve Alma Sorunları</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-search"></i>
                        </div>
                        <h3>Arama Sonucu Bulunamıyor</h3>
                        <p><strong>Sorun:</strong> Arama sorguları sonuç döndürmüyor.</p>
                        <p><strong>Olası Çözümler:</strong></p>
                        <ol>
                            <li><strong>Belge yüklemesini kontrol edin:</strong> Belgelerin başarıyla yüklendiğinden emin olun</li>
                            <li><strong>Embedding'leri doğrulayın:</strong> Embedding'lerin düzgün oluşturulduğunu kontrol edin</li>
                            <li><strong>Sorgu özgüllüğü:</strong> Daha spesifik arama terimleri deneyin</li>
                            <li><strong>Depolama bağlantısı:</strong> Depolama sağlayıcınızın erişilebilir olduğunu doğrulayın</li>
                        </ol>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-chart-line"></i>
                        </div>
                        <h3>Düşük Arama Kalitesi</h3>
                        <p><strong>Sorun:</strong> Arama sonuçları ilgili değil.</p>
                        <p><strong>Çözümler:</strong></p>
                        <ul>
                            <li><code>MaxChunkSize</code> ve <code>ChunkOverlap</code> ayarlarını ayarlayın</li>
                            <li>Daha spesifik arama sorguları kullanın</li>
                            <li>Belgelerin düzgün formatlandığından emin olun</li>
                            <li>Embedding'lerin güncel olduğunu kontrol edin</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Performance Issues -->
        <div class="content-section">
            <h2>Performans Sorunları</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-tachometer-alt"></i>
                        </div>
                        <h3>Yavaş Belge İşleme</h3>
                        <p><strong>Sorun:</strong> Belge yükleme ve işleme çok uzun sürüyor.</p>
                        <p><strong>Çözümler:</strong></p>
                        <ul>
                            <li>Parça sayısını azaltmak için <code>MaxChunkSize</code>'ı artırın</li>
                            <li>Daha güçlü bir AI sağlayıcısı kullanın</li>
                            <li>Depolama sağlayıcı yapılandırmanızı optimize edin</li>
                            <li>Uygulamanızda async operasyonları kullanmayı düşünün</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-memory"></i>
                        </div>
                        <h3>Bellek Sorunları</h3>
                        <p><strong>Sorun:</strong> Uygulama işlem sırasında bellek tükeniyor.</p>
                        <p><strong>Çözümler:</strong></p>
                        <ul>
                            <li>Daha küçük parçalar oluşturmak için <code>MaxChunkSize</code>'ı azaltın</li>
                            <li>Belgeleri toplu olarak işleyin</li>
                            <li>Bellek kullanımını izleyin ve optimize edin</li>
                            <li>Büyük dosyalar için streaming operasyonları kullanmayı düşünün</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Storage Provider Issues -->
        <div class="content-section">
            <h2>Depolama Sağlayıcısı Sorunları</h2>
            
            <div class="row g-4">
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-database"></i>
                        </div>
                        <h3>Qdrant Bağlantı Sorunları</h3>
                        <p><strong>Sorun:</strong> Qdrant'a bağlanılamıyor.</p>
                        <p><strong>Çözümler:</strong></p>
                        <ul>
                            <li>Qdrant API anahtarının doğru olduğunu doğrulayın</li>
                            <li>Qdrant servisine ağ bağlantısını kontrol edin</li>
                            <li>Qdrant servisinin çalıştığından ve erişilebilir olduğundan emin olun</li>
                            <li>Güvenlik duvarı ayarlarını kontrol edin</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-redis"></i>
                        </div>
                        <h3>Redis Bağlantı Sorunları</h3>
                        <p><strong>Sorun:</strong> Redis'e bağlanılamıyor.</p>
                        <p><strong>Çözümler:</strong></p>
                        <ul>
                            <li>Redis bağlantı dizesini doğrulayın</li>
                            <li>Redis sunucusunun çalıştığından emin olun</li>
                            <li>Ağ bağlantısını kontrol edin</li>
                            <li><code>appsettings.json</code> dosyasındaki Redis yapılandırmasını doğrulayın</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-hdd"></i>
                        </div>
                        <h3>SQLite Sorunları</h3>
                        <p><strong>Sorun:</strong> SQLite veritabanı hataları.</p>
                        <p><strong>Çözümler:</strong></p>
                        <ul>
                            <li>Veritabanı dizini için dosya izinlerini kontrol edin</li>
                            <li>Yeterli disk alanı olduğundan emin olun</li>
                            <li>Veritabanı dosya yolunun doğru olduğunu doğrulayın</li>
                            <li>Veritabanı bozulmasını kontrol edin</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- AI Provider Issues -->
        <div class="content-section">
            <h2>AI Sağlayıcısı Sorunları</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-robot"></i>
                        </div>
                        <h3>Anthropic API Hataları</h3>
                        <p><strong>Sorun:</strong> Anthropic API'den hatalar alıyorsunuz.</p>
                        <p><strong>Çözümler:</strong></p>
                        <ul>
                            <li>API anahtarının geçerli olduğunu ve yeterli kredisi olduğunu doğrulayın</li>
                            <li>API hız sınırlarını kontrol edin</li>
                            <li>Doğru API endpoint yapılandırmasından emin olun</li>
                            <li>API kullanımını ve kotaları izleyin</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-brain"></i>
                        </div>
                        <h3>OpenAI API Hataları</h3>
                        <p><strong>Sorun:</strong> OpenAI API'den hatalar alıyorsunuz.</p>
                        <p><strong>Çözümler:</strong></p>
                        <ul>
                            <li>API anahtarının geçerli olduğunu doğrulayın</li>
                            <li>API hız sınırlarını ve kotaları kontrol edin</li>
                            <li>Doğru model yapılandırmasından emin olun</li>
                            <li>API kullanımını izleyin</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Testing and Debugging -->
        <div class="content-section">
            <h2>Test ve Hata Ayıklama</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-vial"></i>
                        </div>
                        <h3>Birim Testleri</h3>
                        <p><strong>Sorun:</strong> SmartRAG bağımlılıkları nedeniyle testler başarısız oluyor.</p>
                        <p><strong>Çözüm:</strong> Birim testlerinde SmartRAG servisleri için mocking kullanın:</p>
                        
                        <div class="code-example">
                            <pre><code class="language-csharp">[Test]
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
    // Test mantığınız burada
}</code></pre>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-cogs"></i>
                        </div>
                        <h3>Entegrasyon Testleri</h3>
                        <p><strong>Sorun:</strong> Entegrasyon testleri başarısız oluyor.</p>
                        <p><strong>Çözüm:</strong> Test yapılandırması kullanın ve doğru kurulumdan emin olun:</p>
                        
                        <div class="code-example">
                            <pre><code class="language-csharp">[Test]
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
    // Entegrasyon test mantığınız burada
}</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Common Error Messages -->
        <div class="content-section">
            <h2>Yaygın Hata Mesajları</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-exclamation-triangle"></i>
                        </div>
                        <h3>Yaygın Hatalar</h3>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"Document not found"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Belge ID'sinin doğru olduğunu doğrulayın</li>
                                <li>Belgenin başarıyla yüklendiğini kontrol edin</li>
                                <li>Belgenin silinmediğinden emin olun</li>
                            </ul>
                        </div>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"Storage provider not configured"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Yapılandırmadaki <code>StorageProvider</code> ayarını doğrulayın</li>
                                <li>Tüm gerekli depolama ayarlarının sağlandığından emin olun</li>
                                <li>Servis kaydını kontrol edin</li>
                            </ul>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-exclamation-circle"></i>
                        </div>
                        <h3>Diğer Hatalar</h3>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"AI provider not configured"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Yapılandırmadaki <code>AIProvider</code> ayarını doğrulayın</li>
                                <li>Seçilen sağlayıcı için API anahtarının sağlandığından emin olun</li>
                                <li>Servis kaydını kontrol edin</li>
                            </ul>
                        </div>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"Invalid file format"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Dosyanın desteklenen bir formatta olduğundan emin olun</li>
                                <li>Dosya uzantısını ve içeriğini kontrol edin</li>
                                <li>Dosyanın bozulmadığını doğrulayın</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Getting Help -->
        <div class="content-section">
            <h2>Yardım Alma</h2>
            
            <div class="row g-4">
                <div class="col-lg-8">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-question-circle"></i>
                        </div>
                        <h3>Hala Yardıma mı İhtiyacınız Var?</h3>
                        <p>Hala sorun yaşıyorsanız, şu adımları takip edin:</p>
                        
                        <div class="row g-3">
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-file-alt"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Logları kontrol edin</h5>
                                        <p class="text-muted">Detaylı hata mesajları için uygulama loglarını inceleyin</p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-cog"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Yapılandırmayı doğrulayın</h5>
                                        <p class="text-muted">Tüm yapılandırma ayarlarını tekrar kontrol edin</p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-play"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Minimal kurulumla test edin</h5>
                                        <p class="text-muted">Önce basit bir yapılandırma ile deneyin</p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-book"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Dokümantasyonu inceleyin</h5>
                                        <p class="text-muted">Rehberlik için diğer dokümantasyon sayfalarını kontrol edin</p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fab fa-github"></i>
                        </div>
                        <h3>Ek Destek</h3>
                        <p>Ek destek için şunlara başvurun:</p>
                        
                        <div class="d-grid gap-3">
                            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-primary" target="_blank">
                                <i class="fab fa-github me-2"></i>
                                GitHub Deposu
                            </a>
                            <a href="https://github.com/byerlikaya/SmartRAG/issues" class="btn btn-outline-primary" target="_blank">
                                <i class="fas fa-bug me-2"></i>
                                Sorun Oluştur
                            </a>
                            <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-outline-primary">
                                <i class="fas fa-rocket me-2"></i>
                                Başlangıç Rehberi
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>

    </div>
</div>
