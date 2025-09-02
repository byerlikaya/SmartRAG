---
layout: default
title: Örnekler
description: SmartRAG entegrasyonu için pratik örnekler ve kod örnekleri
lang: tr
---

<div class="page-content">
    <div class="container">
        <!-- Basic Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Temel Örnekler</h2>
                    <p>SmartRAG ile başlamak için basit örnekler.</p>
                    
                    <h3>Basit Doküman Yükleme</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("upload")]
public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
{
    try
    {
        using var stream = file.OpenReadStream();
        var document = await _documentService.UploadDocumentAsync(
            stream, file.FileName, file.ContentType, "user123");
        return Ok(document);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>Doküman Arama</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpGet("search")]
public async Task<ActionResult<IEnumerable<DocumentChunk>>> SearchDocuments(
    [FromQuery] string query, 
    [FromQuery] int maxResults = 10)
{
    try
    {
        var results = await _documentSearchService.SearchDocumentsAsync(query, maxResults);
        return Ok(results);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>RAG Cevap Üretimi</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("chat")]
public async Task<ActionResult<RagResponse>> ChatWithDocuments(
    [FromBody] string query,
    [FromQuery] int maxResults = 5)
{
    try
    {
        var response = await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);
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

        <!-- Advanced Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Gelişmiş Örnekler</h2>
                    <p>Gelişmiş kullanım senaryoları için daha karmaşık örnekler.</p>
                    
                    <h3>Toplu Doküman İşleme</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("upload-multiple")]
public async Task<ActionResult<List<Document>>> UploadMultipleDocuments(
    IEnumerable<IFormFile> files)
{
    try
    {
        var streams = new List<Stream>();
        var fileNames = new List<string>();
        var contentTypes = new List<string>();

        foreach (var file in files)
        {
            streams.Add(file.OpenReadStream());
            fileNames.Add(file.FileName);
            contentTypes.Add(file.ContentType);
        }

        var documents = await _documentService.UploadDocumentsAsync(
            streams, fileNames, contentTypes, "user123");
        
        return Ok(documents);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>Doküman Yönetimi</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Tüm dokümanları getir
[HttpGet]
public async Task<ActionResult<List<Document>>> GetAllDocuments()
{
    var documents = await _documentService.GetAllDocumentsAsync();
    return Ok(documents);
}

// Belirli dokümanı getir
[HttpGet("{id}")]
public async Task<ActionResult<Document>> GetDocument(Guid id)
{
    var document = await _documentService.GetDocumentAsync(id);
    if (document == null)
        return NotFound();
    
    return Ok(document);
}

// Dokümanı sil
[HttpDelete("{id}")]
public async Task<ActionResult> DeleteDocument(Guid id)
{
    var success = await _documentService.DeleteDocumentAsync(id);
    if (!success)
        return NotFound();
    
    return NoContent();
}</code></pre>
                    </div>

                    <h3>Depolama İstatistikleri</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpGet("statistics")]
public async Task<ActionResult<Dictionary<string, object>>> GetStorageStatistics()
{
    var stats = await _documentService.GetStorageStatisticsAsync();
    return Ok(stats);
}</code></pre>
                    </div>

                    <h3>Embedding İşlemleri</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Tüm embedding'leri yeniden oluştur
[HttpPost("regenerate-embeddings")]
public async Task<ActionResult> RegenerateAllEmbeddings()
{
    var success = await _documentService.RegenerateAllEmbeddingsAsync();
    if (success)
        return Ok("Tüm embedding'ler başarıyla yeniden oluşturuldu");
    else
        return BadRequest("Embedding'ler yeniden oluşturulamadı");
}

// Tüm embedding'leri temizle
[HttpPost("clear-embeddings")]
public async Task<ActionResult> ClearAllEmbeddings()
{
    var success = await _documentService.ClearAllEmbeddingsAsync();
    if (success)
        return Ok("Tüm embedding'ler başarıyla temizlendi");
    else
        return BadRequest("Embedding'ler temizlenemedi");
}

// Tüm dokümanları temizle
[HttpPost("clear-all")]
public async Task<ActionResult> ClearAllDocuments()
{
    var success = await _documentService.ClearAllDocumentsAsync();
    if (success)
        return Ok("Tüm dokümanlar başarıyla temizlendi");
    else
        return BadRequest("Dokümanlar temizlenemedi");
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Web API Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Web API Örnekleri</h2>
                    <p>Web uygulamaları için tam controller örnekleri.</p>
                    
                    <h3>Tam Controller</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _documentSearchService;
    private readonly ILogger<DocumentsController> _logger;
    
    public DocumentsController(
        IDocumentService documentService,
        IDocumentSearchService documentSearchService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _documentSearchService = documentSearchService;
        _logger = logger;
    }
    
    [HttpPost("upload")]
    public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Dosya sağlanmadı");
            
        try
        {
            using var stream = file.OpenReadStream();
            var document = await _documentService.UploadDocumentAsync(
                stream, file.FileName, file.ContentType, "user123");
            _logger.LogInformation("Doküman yüklendi: {DocumentId}", document.Id);
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Doküman yükleme başarısız: {FileName}", file.FileName);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<DocumentChunk>>> SearchDocuments(
        [FromQuery] string query, 
        [FromQuery] int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Sorgu parametresi gerekli");
            
        try
        {
            var results = await _documentSearchService.SearchDocumentsAsync(query, maxResults);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arama başarısız, sorgu: {Query}", query);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("chat")]
    public async Task<ActionResult<RagResponse>> ChatWithDocuments(
        [FromBody] string query,
        [FromQuery] int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Sorgu parametresi gerekli");
            
        try
        {
            var response = await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sohbet başarısız, sorgu: {Query}", query);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Document>> GetDocument(Guid id)
    {
        try
        {
            var document = await _documentService.GetDocumentAsync(id);
            if (document == null)
                return NotFound();
                
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Doküman getirme başarısız: {DocumentId}", id);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet]
    public async Task<ActionResult<List<Document>>> GetAllDocuments()
    {
        try
        {
            var documents = await _documentService.GetAllDocumentsAsync();
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tüm dokümanları getirme başarısız");
            return BadRequest(ex.Message);
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDocument(Guid id)
    {
        try
        {
            var success = await _documentService.DeleteDocumentAsync(id);
            if (!success)
                return NotFound();
                
            _logger.LogInformation("Doküman silindi: {DocumentId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Doküman silme başarısız: {DocumentId}", id);
            return BadRequest(ex.Message);
        }
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Console Application Example Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Konsol Uygulaması Örneği</h2>
                    <p>Tam bir konsol uygulaması örneği.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">class Program
{
    static async Task Main(string[] args)
    {
        // Create configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
            
        var services = new ServiceCollection();
        
        // Servisleri yapılandır
        services.AddSmartRag(configuration, options =>
        {
            options.AIProvider = AIProvider.Anthropic;
            options.StorageProvider = StorageProvider.Qdrant;
            options.MaxChunkSize = 1000;
            options.ChunkOverlap = 200;
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentService>();
        var documentSearchService = serviceProvider.GetRequiredService<IDocumentSearchService>();
        
        Console.WriteLine("SmartRAG Konsol Uygulaması");
        Console.WriteLine("===========================");
        
        while (true)
        {
            Console.WriteLine("\nSeçenekler:");
            Console.WriteLine("1. Doküman yükle");
            Console.WriteLine("2. Doküman ara");
            Console.WriteLine("3. Dokümanlarla sohbet et");
            Console.WriteLine("4. Tüm dokümanları listele");
            Console.WriteLine("5. Çıkış");
            Console.Write("Bir seçenek seçin: ");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    await UploadDocument(documentService);
                    break;
                case "2":
                    await SearchDocuments(documentSearchService);
                    break;
                case "3":
                    await ChatWithDocuments(documentSearchService);
                    break;
                case "4":
                    await ListDocuments(documentService);
                    break;
                case "5":
                    return;
                default:
                    Console.WriteLine("Geçersiz seçenek. Lütfen tekrar deneyin.");
                    break;
            }
        }
    }
    
    static async Task UploadDocument(IDocumentService documentService)
    {
        Console.Write("Dosya yolunu girin: ");
        var filePath = Console.ReadLine();
        
        if (!File.Exists(filePath))
        {
            Console.WriteLine("Dosya bulunamadı.");
            return;
        }
        
        try
        {
            var fileInfo = new FileInfo(filePath);
            using var fileStream = File.OpenRead(filePath);
            
            var document = await documentService.UploadDocumentAsync(
                fileStream, fileInfo.Name, "application/octet-stream", "console-user");
            Console.WriteLine($"Doküman başarıyla yüklendi. ID: {document.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Doküman yükleme hatası: {ex.Message}");
        }
    }
    
    static async Task SearchDocuments(IDocumentSearchService documentSearchService)
    {
        Console.Write("Arama sorgusunu girin: ");
        var query = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine("Sorgu boş olamaz.");
            return;
        }
        
        try
        {
            var results = await documentSearchService.SearchDocumentsAsync(query, 5);
            Console.WriteLine($"{results.Count} sonuç bulundu:");
            
            foreach (var result in results)
            {
                Console.WriteLine($"- {result.Content.Substring(0, Math.Min(100, result.Content.Length))}...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Doküman arama hatası: {ex.Message}");
        }
    }
    
    static async Task ChatWithDocuments(IDocumentSearchService documentSearchService)
    {
        Console.Write("Sorunuzu girin: ");
        var query = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine("Soru boş olamaz.");
            return;
        }
        
        try
        {
            var response = await documentSearchService.GenerateRagAnswerAsync(query, 5);
            Console.WriteLine($"AI Cevabı: {response.Answer}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dokümanlarla sohbet hatası: {ex.Message}");
        }
    }
    
    static async Task ListDocuments(IDocumentService documentService)
    {
        try
        {
            var documents = await documentService.GetAllDocumentsAsync();
            Console.WriteLine($"Toplam doküman: {documents.Count}");
            
            foreach (var doc in documents)
            {
                Console.WriteLine($"- {doc.FileName} (ID: {doc.Id})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Doküman listeleme hatası: {ex.Message}");
        }
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Configuration Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Yapılandırma Örnekleri</h2>
                    <p>SmartRAG servislerini yapılandırmanın farklı yolları.</p>
                    
                    <h3>Temel Yapılandırma</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
services.UseSmartRag(configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.Gemini
);</code></pre>
                    </div>

                    <h3>Gelişmiş Yapılandırma</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
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
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new[] { AIProvider.Gemini, AIProvider.OpenAI };
});</code></pre>
                    </div>

                    <h3>appsettings.json Yapılandırması</h3>
                    <div class="code-example">
                        <pre><code class="language-json">{
  "SmartRAG": {
    "AIProvider": "Anthropic",
    "StorageProvider": "Qdrant",
    "MaxChunkSize": 1000,
    "MinChunkSize": 50,
    "ChunkOverlap": 200,
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000,
    "RetryPolicy": "ExponentialBackoff",
    "EnableFallbackProviders": true,
    "FallbackProviders": ["Gemini", "OpenAI"]
  },
  "Anthropic": {
    "ApiKey": "your-anthropic-api-key"
  },
  "Qdrant": {
    "ApiKey": "your-qdrant-api-key"
  }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Need Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Yardıma mı ihtiyacınız var?</h4>
                        <p class="mb-2">Örneklerle ilgili yardıma ihtiyacınız varsa:</p>
                        <ul class="mb-0">
                            <li><a href="{{ site.baseurl }}/tr/getting-started">Başlangıç Rehberi</a></li>
                            <li><a href="{{ site.baseurl }}/tr/api-reference">API Referansı</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues">GitHub'da sorun açın</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">E-posta ile destek alın</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>