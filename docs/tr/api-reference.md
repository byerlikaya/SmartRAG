---
layout: default
title: API Referansı
description: SmartRAG için örnekler ve kullanım desenleri ile tam API dokümantasyonu
lang: tr
---

<div class="page-header">
    <div class="container">
        <div class="row">
            <div class="col-lg-8 mx-auto text-center">
                <h1 class="page-title">API Referansı</h1>
                <p class="page-description">
                    SmartRAG için örnekler ve kullanım desenleri ile tam API dokümantasyonu
                </p>
            </div>
        </div>
    </div>
</div>

<div class="page-content">
    <div class="container">
        <!-- Core Interfaces Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Temel Arayüzler</h2>
                    <p>SmartRAG'ın temel arayüzleri ve servisleri.</p>
                    
                    <h3>IDocumentService</h3>
                    <p>Belge işlemleri için ana servis arayüzü.</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentService
{
    Task&lt;Document&gt; UploadDocumentAsync(IFormFile file);
    Task&lt;Document&gt; GetDocumentByIdAsync(string id);
    Task&lt;IEnumerable&lt;Document&gt;&gt; GetAllDocumentsAsync();
    Task&lt;bool&gt; DeleteDocumentAsync(string id);
    Task&lt;IEnumerable&lt;DocumentChunk&gt;&gt; SearchDocumentsAsync(string query, int maxResults = 5);
    Task&lt;RagResponse&gt; GenerateRagAnswerAsync(string query, int maxResults = 5);
}</code></pre>
                    </div>

                    <h3>IDocumentParserService</h3>
                    <p>Farklı dosya formatlarını ayrıştırmak için servis.</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentParserService
{
    Task&lt;string&gt; ParseDocumentAsync(IFormFile file);
    bool CanParse(string fileName);
    Task&lt;IEnumerable&lt;string&gt;&gt; ChunkTextAsync(string text, int chunkSize = 1000, int overlap = 200);
}</code></pre>
                    </div>

                    <h3>IDocumentSearchService</h3>
                    <p>Belge arama ve RAG işlemleri için servis.</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentSearchService
{
    Task&lt;List&lt;DocumentChunk&gt;&gt; SearchDocumentsAsync(string query, int maxResults = 5);
    Task&lt;RagResponse&gt; GenerateRagAnswerAsync(string query, int maxResults = 5);
}</code></pre>
                    </div>

                    <h3>IAIService</h3>
                    <p>AI sağlayıcıları ile etkileşim için servis.</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IAIService
{
    Task&lt;float[]&gt; GenerateEmbeddingAsync(string text);
    Task&lt;string&gt; GenerateTextAsync(string prompt);
    Task&lt;string&gt; GenerateTextAsync(string prompt, string context);
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Models Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Modeller</h2>
                    <p>SmartRAG'da kullanılan temel veri modelleri.</p>
                    
                    <h3>Document</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class Document
{
    public string Id { get; set; }
    public string FileName { get; set; }
    public string Content { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public List&lt;DocumentChunk&gt; Chunks { get; set; }
}</code></pre>
                    </div>

                    <h3>DocumentChunk</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class DocumentChunk
{
    public string Id { get; set; }
    public string DocumentId { get; set; }
    public string Content { get; set; }
    public float[] Embedding { get; set; }
    public int ChunkIndex { get; set; }
    public DateTime CreatedAt { get; set; }
}</code></pre>
                    </div>

                    <h3>RagResponse</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class RagResponse
{
    public string Answer { get; set; }
    public List&lt;SearchSource&gt; Sources { get; set; }
    public DateTime SearchedAt { get; set; }
    public RagConfiguration Configuration { get; set; }
}</code></pre>
                    </div>

                    <h3>SearchSource</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class SearchSource
{
    public string DocumentId { get; set; }
    public string DocumentName { get; set; }
    public string Content { get; set; }
    public float SimilarityScore { get; set; }
    public int ChunkIndex { get; set; }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Enums Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Enumlar</h2>
                    <p>SmartRAG'da kullanılan enum değerleri.</p>
                    
                    <h3>AIProvider</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public enum AIProvider
{
    OpenAI,
    Anthropic,
    Gemini,
    AzureOpenAI,
    Custom
}</code></pre>
                    </div>

                    <h3>StorageProvider</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public enum StorageProvider
{
    Qdrant,
    Redis,
    SQLite,
    InMemory,
    FileSystem
}</code></pre>
                    </div>

                    <h3>RetryPolicy</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public enum RetryPolicy
{
    None,
    FixedDelay,
    ExponentialBackoff,
    LinearBackoff
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Service Registration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Servis Kaydı</h2>
                    <p>SmartRAG servislerini uygulamanıza nasıl kaydedeceğiniz.</p>
                    
                    <h3>Temel Kayıt</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs veya Startup.cs
services.AddSmartRAG(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
});</code></pre>
                    </div>

                    <h3>Gelişmiş Konfigürasyon</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Redis;
    options.MaxChunkSize = 1500;
    options.MinChunkSize = 100;
    options.ChunkOverlap = 300;
    options.MaxRetryAttempts = 5;
    options.RetryDelayMs = 2000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List&lt;AIProvider&gt; 
    { 
        AIProvider.Anthropic, 
        AIProvider.Gemini 
    };
});</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Usage Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Kullanım Örnekleri</h2>
                    <p>SmartRAG API'sini nasıl kullanacağınız.</p>
                    
                    <h3>Belge Yükleme</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("upload")]
public async Task&lt;ActionResult&lt;Document&gt;&gt; UploadDocument(IFormFile file)
{
    try
    {
        var document = await _documentService.UploadDocumentAsync(file);
        return Ok(document);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>Belge Arama</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpGet("search")]
public async Task&lt;ActionResult&lt;IEnumerable&lt;DocumentChunk&gt;&gt;&gt; SearchDocuments(
    [FromQuery] string query, 
    [FromQuery] int maxResults = 10)
{
    try
    {
        var results = await _documentService.SearchDocumentsAsync(query, maxResults);
        return Ok(results);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>RAG Yanıt Üretme</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("ask")]
public async Task&lt;ActionResult&lt;RagResponse&gt;&gt; AskQuestion([FromBody] string question)
{
    try
    {
        var response = await _documentService.GenerateRagAnswerAsync(question, 5);
        return Ok(response);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Error Handling Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Hata Yönetimi</h2>
                    <p>SmartRAG'da hata yönetimi ve istisna türleri.</p>
                    
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-exclamation-triangle me-2"></i>Önemli</h4>
                        <p class="mb-0">Tüm SmartRAG servisleri uygun hata yönetimi ile tasarlanmıştır. API çağrılarınızı try-catch blokları ile sarmalayın.</p>
                    </div>

                    <h3>Yaygın Hatalar</h3>
                    <ul>
                        <li><strong>ArgumentException</strong>: Geçersiz parametreler</li>
                        <li><strong>FileNotFoundException</strong>: Dosya bulunamadı</li>
                        <li><strong>UnauthorizedAccessException</strong>: API anahtarı geçersiz</li>
                        <li><strong>HttpRequestException</strong>: Ağ bağlantı sorunları</li>
                        <li><strong>TimeoutException</strong>: İstek zaman aşımı</li>
                    </ul>

                    <h3>Hata Yönetimi Örneği</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">try
{
    var document = await _documentService.UploadDocumentAsync(file);
    return Ok(document);
}
catch (ArgumentException ex)
{
    _logger.LogWarning("Invalid argument: {Message}", ex.Message);
    return BadRequest("Invalid file or parameters");
}
catch (UnauthorizedAccessException ex)
{
    _logger.LogError("Authentication failed: {Message}", ex.Message);
    return Unauthorized("Invalid API key");
}
catch (HttpRequestException ex)
{
    _logger.LogError("Network error: {Message}", ex.Message);
    return StatusCode(503, "Service temporarily unavailable");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error occurred");
    return StatusCode(500, "Internal server error");
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Logging Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Loglama</h2>
                    <p>SmartRAG'da loglama ve izleme.</p>
                    
                    <h3>Log Seviyeleri</h3>
                    <ul>
                        <li><strong>Information</strong>: Normal işlemler</li>
                        <li><strong>Warning</strong>: Uyarılar ve beklenmeyen durumlar</li>
                        <li><strong>Error</strong>: Hatalar ve istisnalar</li>
                        <li><strong>Debug</strong>: Detaylı hata ayıklama bilgileri</li>
                    </ul>

                    <h3>Loglama Konfigürasyonu</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SmartRAG": "Debug",
      "Microsoft": "Warning"
    }
  }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Performance Considerations Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Performans Değerlendirmeleri</h2>
                    <p>SmartRAG'da performans optimizasyonu.</p>
                    
                    <h3>Önerilen Ayarlar</h3>
                    <ul>
                        <li><strong>Chunk Size</strong>: 1000-1500 karakter</li>
                        <li><strong>Chunk Overlap</strong>: 200-300 karakter</li>
                        <li><strong>Max Results</strong>: 5-10 sonuç</li>
                        <li><strong>Retry Attempts</strong>: 3-5 deneme</li>
                    </ul>

                    <h3>Performans İpuçları</h3>
                    <ul>
                        <li>Büyük dosyaları önceden işleyin</li>
                        <li>Uygun chunk boyutları kullanın</li>
                        <li>Cache mekanizmalarını etkinleştirin</li>
                        <li>Asenkron işlemleri tercih edin</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Yardıma mı ihtiyacınız var?</h4>
                        <p class="mb-0">API ile ilgili sorularınız için:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/tr/getting-started">Başlangıç Kılavuzu</a></li>
                            <li><a href="{{ site.baseurl }}/tr/examples">Örnekler</a></li>
                            <li><a href="{{ site.baseurl }}/tr/configuration">Konfigürasyon</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub'da Issue Açın</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">E-posta ile Destek Alın</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>