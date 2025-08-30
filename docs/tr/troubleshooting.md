---
layout: default
title: Sorun Giderme
description: SmartRAG uygulaması için yaygın sorunlar ve çözümler
lang: tr
---

<div class="page-content">
    <div class="container">
        <!-- Common Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Yaygın Sorunlar</h2>
                    <p>SmartRAG kullanırken karşılaşabileceğiniz yaygın sorunlar ve çözümleri.</p>

                    <h3>Derleme Sorunları</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-exclamation-triangle me-2"></i>Uyarı</h4>
                        <p class="mb-0">Derleme hatalarını çözmek için önce temiz bir çözüm çalıştırın.</p>
                    </div>

                    <h4>NuGet Paket Hatası</h4>
                    <div class="code-example">
                        <pre><code class="language-bash"># Temiz çözüm
dotnet clean
dotnet restore
dotnet build</code></pre>
                    </div>

                    <h4>Bağımlılık Çakışması</h4>
                    <div class="code-example">
                        <pre><code class="language-xml">&lt;PackageReference Include="SmartRAG" Version="1.1.0" /&gt;
&lt;PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" /&gt;
&lt;PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" /&gt;</code></pre>
                    </div>

                    <h3>Çalışma Zamanı Sorunları</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Yapılandırma</h4>
                        <p class="mb-0">Çoğu çalışma zamanı sorunu yapılandırma problemleriyle ilgilidir.</p>
                    </div>

                    <h4>AI Provider Yapılandırılmamış</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Doğru yapılandırmayı sağlayın
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});</code></pre>
                    </div>

                    <h4>API Anahtarı Sorunları</h4>
                    <div class="code-example">
                        <pre><code class="language-bash"># Ortam değişkenini ayarlayın
export SMARTRAG_API_KEY=your-api-key

# Veya appsettings.json kullanın
{
  "SmartRAG": {
    "ApiKey": "your-api-key"
  }
}</code></pre>
                    </div>

                    <h3>Performans Sorunları</h3>
                    <div class="alert alert-success">
                        <h4><i class="fas fa-tachometer-alt me-2"></i>Optimizasyon</h4>
                        <p class="mb-0">Performans doğru yapılandırma ile iyileştirilebilir.</p>
                    </div>

                    <h4>Yavaş Belge İşleme</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Chunk boyutunu optimize edin
services.AddSmartRAG(options =>
{
    options.ChunkSize = 500; // Daha hızlı işleme için küçük chunk'lar
    options.ChunkOverlap = 100;
});</code></pre>
                    </div>

                    <h4>Bellek Kullanımı</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Uygun depolama provider'ını kullanın
services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Redis; // Yüksek bellek kullanımı için
    // veya
    options.StorageProvider = StorageProvider.Qdrant; // Büyük veri setleri için
});</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Debugging Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Hata Ayıklama</h2>
                    <p>SmartRAG uygulamalarınızı hata ayıklamanıza yardımcı olacak araçlar ve teknikler.</p>

                    <h3>Logging'i Etkinleştirin</h3>
                    
                    <h5>Logging Yapılandırması</h5>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Logging'i yapılandırın
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);</code></pre>
                    </div>

                    <h5>Servis Implementasyonu</h5>
                    <div class="code-example">
                        <pre><code class="language-csharp">private readonly ILogger&lt;DocumentService&gt; _logger;

public async Task&lt;Document&gt; UploadDocumentAsync(IFormFile file)
{
    _logger.LogInformation("Belge yükleniyor: {FileName}", file.FileName);
    // ... implementasyon
}</code></pre>
                    </div>

                    <h3>Hata Yönetimi</h3>
                    
                    <h5>Temel Hata Yönetimi</h5>
                    <div class="code-example">
                        <pre><code class="language-csharp">try
{
    var document = await _documentService.UploadDocumentAsync(file);
    return Ok(document);
}
catch (ArgumentException ex)
{
    _logger.LogError(ex, "Geçersiz dosya formatı");
    return BadRequest("Geçersiz dosya formatı");
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "AI provider hatası");
    return StatusCode(503, "Servis geçici olarak kullanılamıyor");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Beklenmeyen hata");
    return StatusCode(500, "İç sunucu hatası");
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Testing Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Test Etme</h2>
                    <p>SmartRAG implementasyonunuzu nasıl test edeceğiniz.</p>

                    <h3>Birim Testi</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task UploadDocument_ValidFile_ReturnsDocument()
{
    // Arrange
    var mockFile = new Mock&lt;IFormFile&gt;();
    var service = new DocumentService(mockLogger.Object);
    
    // Act
    var result = await service.UploadDocumentAsync(mockFile.Object);
    
    // Assert
    Assert.IsNotNull(result);
    Assert.IsNotEmpty(result.Id);
}</code></pre>
                    </div>

                    <h3>Entegrasyon Testi</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task SearchDocuments_ReturnsRelevantResults()
{
    // Arrange
    var testQuery = "test sorgusu";
    
    // Act
    var results = await _documentService.SearchDocumentsAsync(testQuery);
    
    // Assert
    Assert.IsNotNull(results);
    Assert.IsTrue(results.Any());
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Getting Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Yardım Alma</h2>
                    <p>Hala sorun yaşıyorsanız, işte yardım almanın yolları.</p>

                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-github me-2"></i>GitHub Issues</h4>
                                <p class="mb-0">GitHub'da hata raporlayın ve özellik isteyin.</p>
                                <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank" class="btn btn-sm btn-outline-info mt-2">Issue Aç</a>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-envelope me-2"></i>E-posta Desteği</h4>
                                <p class="mb-0">E-posta ile doğrudan yardım alın.</p>
                                <a href="mailto:b.yerlikaya@outlook.com" class="btn btn-sm btn-outline-success mt-2">İletişim</a>
                            </div>
                        </div>
                    </div>

                    <h3>Yardım İstemeden Önce</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-list me-2"></i>Kontrol Listesi</h4>
                        <ul class="mb-0">
                            <li><a href="{{ site.baseurl }}/tr/getting-started">Başlangıç</a> kılavuzunu kontrol edin</li>
                            <li><a href="{{ site.baseurl }}/tr/configuration">Yapılandırma</a> dokümantasyonunu inceleyin</li>
                            <li>Mevcut <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub Issues</a>'ları arayın</li>
                            <li>Hata mesajları ve yapılandırma detaylarını dahil edin</li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>

        <!-- Prevention Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Önleme</h2>
                    <p>Yaygın sorunları önlemek için en iyi uygulamalar.</p>

                    <h3>Yapılandırma En İyi Uygulamaları</h3>
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="alert alert-primary">
                                <h4><i class="fas fa-key me-2"></i>API Anahtarları</h4>
                                <p class="mb-0">API anahtarlarını asla kod içinde sabit yazmayın. Ortam değişkenleri veya güvenli yapılandırma kullanın.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-database me-2"></i>Depolama</h4>
                                <p class="mb-0">Kullanım durumunuz için doğru depolama provider'ını seçin.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-shield-alt me-2"></i>Hata Yönetimi</h4>
                                <p class="mb-0">Uygun hata yönetimi ve logging implementasyonu yapın.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-warning">
                                <h4><i class="fas fa-balance-scale me-2"></i>Performans</h4>
                                <p class="mb-0">Performansı izleyin ve chunk boyutlarını optimize edin.</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
