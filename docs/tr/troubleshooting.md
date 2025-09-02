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
                    <p>SmartRAG kullanırken karşılaşabileceğiniz yaygın sorunlar ve çözümler.</p>

                    <h3>Servis Kayıt Sorunları</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-exclamation-triangle me-2"></i>Uyarı</h4>
                        <p class="mb-0">Her zaman uygun servis kaydı ve bağımlılık enjeksiyonu kurulumunu sağlayın.</p>
                    </div>

                    <h4>Servis Kaydedilmemiş</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Servislerin düzgün kaydedildiğinden emin olun
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// Gerekli servisleri alın
var documentService = serviceProvider.GetRequiredService<IDocumentService>();
var documentSearchService = serviceProvider.GetRequiredService<IDocumentSearchService>();</code></pre>
                    </div>

                    <h4>Yapılandırma Sorunları</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Uygun yapılandırmayı sağlayın
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.MinChunkSize = 50;
    options.ChunkOverlap = 200;
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
});</code></pre>
                    </div>

                    <h3>API Anahtarı Yapılandırması</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Yapılandırma</h4>
                        <p class="mb-0">API anahtarları appsettings.json veya ortam değişkenlerinde yapılandırılmalıdır.</p>
                    </div>

                    <h4>Ortam Değişkenleri</h4>
                    <div class="code-example">
                        <pre><code class="language-bash"># Ortam değişkenlerini ayarlayın
export ANTHROPIC_API_KEY=your-anthropic-api-key
export QDRANT_API_KEY=your-qdrant-api-key</code></pre>
                    </div>

                    <h4>appsettings.json Yapılandırması</h4>
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

                    <h3>Performans Sorunları</h3>
                    <div class="alert alert-success">
                        <h4><i class="fas fa-tachometer-alt me-2"></i>Optimizasyon</h4>
                        <p class="mb-0">Performans uygun yapılandırma ile iyileştirilebilir.</p>
                    </div>

                    <h4>Yavaş Doküman İşleme</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Daha hızlı işleme için chunk boyutunu optimize edin
services.AddSmartRag(configuration, options =>
{
    options.MaxChunkSize = 500; // Daha hızlı işleme için küçük chunk'lar
    options.MinChunkSize = 50;
    options.ChunkOverlap = 100;
    options.MaxRetryAttempts = 2; // Daha hızlı hata için retry'ları azaltın
});</code></pre>
                    </div>

                    <h4>Bellek Kullanımı Optimizasyonu</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Uygun depolama sağlayıcısını kullanın
services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.InMemory; // Küçük veri setleri için
    // veya
    options.StorageProvider = StorageProvider.Qdrant; // Büyük veri setleri için
    options.EnableFallbackProviders = true; // Güvenilirlik için fallback'i etkinleştirin
});</code></pre>
                    </div>

                    <h3>Retry Yapılandırması</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Retry politikalarını yapılandırın
services.AddSmartRag(configuration, options =>
{
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new[] { AIProvider.Gemini, AIProvider.OpenAI };
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
                    <p>SmartRAG uygulamalarını hata ayıklamanıza yardımcı olacak araçlar ve teknikler.</p>

                    <h3>Logging'i Etkinleştirin</h3>
                    
                    <h4>Logging Yapılandırması</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Logging'i yapılandırın
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// SmartRAG özel logging'i ekleyin
builder.Logging.AddFilter("SmartRAG", LogLevel.Debug);</code></pre>
                    </div>

                    <h4>Servis Uygulaması</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">private readonly ILogger<DocumentsController> _logger;

public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
{
    _logger.LogInformation("Doküman yükleniyor: {FileName}", file.FileName);
    try
    {
        using var stream = file.OpenReadStream();
        var document = await _documentService.UploadDocumentAsync(
            stream, file.FileName, file.ContentType, "user123");
        _logger.LogInformation("Doküman başarıyla yüklendi: {DocumentId}", document.Id);
        return Ok(document);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Doküman yükleme başarısız: {FileName}", file.FileName);
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>Hata Yönetimi</h3>
                    
                    <h4>Temel Hata Yönetimi</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">try
{
    using var stream = file.OpenReadStream();
    var document = await _documentService.UploadDocumentAsync(
        stream, file.FileName, file.ContentType, "user123");
    return Ok(document);
}
catch (ArgumentException ex)
{
    _logger.LogError(ex, "Geçersiz dosya formatı: {FileName}", file.FileName);
    return BadRequest("Geçersiz dosya formatı");
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "AI sağlayıcı hatası: {Message}", ex.Message);
    return StatusCode(503, "Servis geçici olarak kullanılamıyor");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Yükleme sırasında beklenmeyen hata: {FileName}", file.FileName);
    return StatusCode(500, "İç sunucu hatası");
}</code></pre>
                    </div>

                    <h4>Servis Seviyesi Hata Yönetimi</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy)
{
    try
    {
        _logger.LogInformation("Doküman yükleme başlıyor: {FileName}", fileName);
        
        // Girdiyi doğrulayın
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("Dosya akışı null veya boş");
        
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Dosya adı gerekli");
        
        // Dokümanı işleyin
        var document = await ProcessDocumentAsync(fileStream, fileName, contentType, uploadedBy);
        
        _logger.LogInformation("Doküman başarıyla yüklendi: {DocumentId}", document.Id);
        return document;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Doküman yükleme başarısız: {FileName}", fileName);
        throw;
    }
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
                    <p>SmartRAG uygulamanızı nasıl test edeceğiniz.</p>

                    <h3>Birim Testi</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task UploadDocument_ValidFile_ReturnsDocument()
{
    // Arrange
    var mockDocumentService = new Mock<IDocumentService>();
    var mockDocumentSearchService = new Mock<IDocumentSearchService>();
    var mockLogger = new Mock<ILogger<DocumentsController>>();
    
    var controller = new DocumentsController(
        mockDocumentService.Object, 
        mockDocumentSearchService.Object, 
        mockLogger.Object);
    
    var mockFile = new Mock<IFormFile>();
    mockFile.Setup(f => f.FileName).Returns("test.pdf");
    mockFile.Setup(f => f.ContentType).Returns("application/pdf");
    mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
    
    var expectedDocument = new Document 
    { 
        Id = Guid.NewGuid(), 
        FileName = "test.pdf" 
    };
    
    mockDocumentService.Setup(s => s.UploadDocumentAsync(
        It.IsAny<Stream>(), 
        It.IsAny<string>(), 
        It.IsAny<string>(), 
        It.IsAny<string>()))
        .ReturnsAsync(expectedDocument);
    
    // Act
    var result = await controller.UploadDocument(mockFile.Object);
    
    // Assert
    var okResult = result as OkObjectResult;
    Assert.IsNotNull(okResult);
    Assert.AreEqual(expectedDocument, okResult.Value);
}</code></pre>
                    </div>

                    <h3>Entegrasyon Testi</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task SearchDocuments_ReturnsRelevantResults()
{
    // Arrange
    var mockDocumentSearchService = new Mock<IDocumentSearchService>();
    var mockDocumentService = new Mock<IDocumentService>();
    var mockLogger = new Mock<ILogger<DocumentsController>>();
    
    var controller = new DocumentsController(
        mockDocumentService.Object,
        mockDocumentSearchService.Object,
        mockLogger.Object);
    
    var testQuery = "test query";
    var expectedResults = new List<DocumentChunk>
    {
        new DocumentChunk { Content = "Test içerik 1" },
        new DocumentChunk { Content = "Test içerik 2" }
    };
    
    mockDocumentSearchService.Setup(s => s.SearchDocumentsAsync(testQuery, 10))
        .ReturnsAsync(expectedResults);
    
    // Act
    var result = await controller.SearchDocuments(testQuery);
    
    // Assert
    var okResult = result as OkObjectResult;
    Assert.IsNotNull(okResult);
    var results = okResult.Value as IEnumerable<DocumentChunk>;
    Assert.IsNotNull(results);
    Assert.AreEqual(expectedResults.Count, results.Count());
}</code></pre>
                    </div>

                    <h3>Uçtan Uca Test</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task CompleteWorkflow_UploadSearchChat_WorksCorrectly()
{
    // Arrange
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
        
    var services = new ServiceCollection();
    services.AddSmartRag(configuration, options =>
    {
        options.AIProvider = AIProvider.Anthropic;
        options.StorageProvider = StorageProvider.InMemory;
        options.MaxChunkSize = 1000;
        options.ChunkOverlap = 200;
    });
    
    var serviceProvider = services.BuildServiceProvider();
    var documentService = serviceProvider.GetRequiredService<IDocumentService>();
    var documentSearchService = serviceProvider.GetRequiredService<IDocumentSearchService>();
    
    // Test dosyası oluşturun
    var testContent = "Bu yapay zeka hakkında bir test dokümanıdır.";
    var testStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testContent));
    
    // Act - Yükle
    var document = await documentService.UploadDocumentAsync(
        testStream, "test.txt", "text/plain", "test-user");
    
    // Assert - Yükleme
    Assert.IsNotNull(document);
    Assert.AreEqual("test.txt", document.FileName);
    
    // Act - Arama
    var searchResults = await documentSearchService.SearchDocumentsAsync("yapay zeka", 5);
    
    // Assert - Arama
    Assert.IsNotNull(searchResults);
    Assert.IsTrue(searchResults.Count > 0);
    
    // Act - Sohbet
    var chatResponse = await documentSearchService.GenerateRagAnswerAsync("Bu doküman ne hakkında?", 5);
    
    // Assert - Sohbet
    Assert.IsNotNull(chatResponse);
    Assert.IsFalse(string.IsNullOrWhiteSpace(chatResponse.Answer));
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
                                <p class="mb-0">GitHub'da hata bildirin ve özellik isteyin.</p>
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
                            <li><a href="{{ site.baseurl }}/tr/getting-started">Başlangıç</a> rehberini kontrol edin</li>
                            <li><a href="{{ site.baseurl }}/tr/configuration">Yapılandırma</a> dokümantasyonunu inceleyin</li>
                            <li>Mevcut <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub Issues</a>'ları arayın</li>
                            <li>Hata mesajlarını ve yapılandırma detaylarını dahil edin</li>
                            <li>Doğru method imzaları için <a href="{{ site.baseurl }}/tr/api-reference">API Referansı</a>'nı kontrol edin</li>
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
                                <p class="mb-0">API anahtarlarını asla hardcode etmeyin. Ortam değişkenleri veya güvenli yapılandırma kullanın.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-database me-2"></i>Depolama</h4>
                                <p class="mb-0">Kullanım durumunuz için doğru depolama sağlayıcısını seçin.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-shield-alt me-2"></i>Hata Yönetimi</h4>
                                <p class="mb-0">Uygun hata yönetimi ve logging uygulayın.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-warning">
                                <h4><i class="fas fa-balance-scale me-2"></i>Performans</h4>
                                <p class="mb-0">Performansı izleyin ve chunk boyutlarını optimize edin.</p>
                            </div>
                        </div>
                    </div>

                    <h3>Geliştirme vs Üretim Yapılandırması</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Geliştirme yapılandırması
if (builder.Environment.IsDevelopment())
{
    services.AddSmartRag(configuration, options =>
    {
        options.AIProvider = AIProvider.Gemini; // Geliştirme için ücretsiz katman
        options.StorageProvider = StorageProvider.InMemory; // Geliştirme için hızlı
        options.MaxChunkSize = 500;
        options.ChunkOverlap = 100;
        options.MaxRetryAttempts = 1; // Geliştirmede hızlı hata
    });
}
else
{
    // Üretim yapılandırması
    services.AddSmartRag(configuration, options =>
    {
        options.AIProvider = AIProvider.Anthropic; // Üretim için daha iyi kalite
        options.StorageProvider = StorageProvider.Qdrant; // Kalıcı depolama
        options.MaxChunkSize = 1000;
        options.ChunkOverlap = 200;
        options.MaxRetryAttempts = 3;
        options.RetryDelayMs = 1000;
        options.RetryPolicy = RetryPolicy.ExponentialBackoff;
        options.EnableFallbackProviders = true;
    });
}</code></pre>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
