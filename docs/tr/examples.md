---
layout: default
title: Örnekler
description: SmartRAG'dan öğrenmek için gerçek dünya örnekleri ve örnek uygulamalar
lang: tr
---

<div class="page-header">
    <div class="container">
        <div class="row">
            <div class="col-lg-8 mx-auto text-center">
                <h1 class="page-title">Örnekler</h1>
                <p class="page-description">
                    SmartRAG'dan öğrenmek için gerçek dünya örnekleri ve örnek uygulamalar
                </p>
            </div>
        </div>
    </div>
</div>

<div class="page-content">
    <div class="container">
        <!-- Basic Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Temel Örnekler</h2>
                    <p>SmartRAG ile başlamanız için basit örnekler.</p>
                    
                    <h3>Basit Belge Yükleme</h3>
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
                </div>
            </div>
        </section>

        <!-- Advanced Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Gelişmiş Örnekler</h2>
                    <p>Gelişmiş kullanım durumları için daha karmaşık örnekler.</p>
                    
                    <h3>Toplu Belge İşleme</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task&lt;IEnumerable&lt;Document&gt;&gt; ProcessMultipleDocumentsAsync(
    IEnumerable&lt;IFormFile&gt; files)
{
    var tasks = files.Select(async file =>
    {
        try
        {
            return await _documentService.UploadDocumentAsync(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process file: {FileName}", file.FileName);
            return null;
        }
    });

    var results = await Task.WhenAll(tasks);
    return results.Where(d => d != null);
}</code></pre>
                    </div>

                    <h3>Akıllı Sorgu Niyet Algılama</h3>
                    <p>Niyet analizine dayalı olarak sorguları otomatik olarak sohbet veya belge aramasına yönlendirin:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task&lt;QueryResult&gt; ProcessQueryAsync(string query)
{
    // Sorgu niyetini analiz et
    var intent = await _queryIntentService.AnalyzeIntentAsync(query);
    
    switch (intent.Type)
    {
        case QueryIntentType.Chat:
            // Konuşma AI'ına yönlendir
            return await _chatService.ProcessChatQueryAsync(query);
            
        case QueryIntentType.DocumentSearch:
            // Belge aramasına yönlendir
            var searchResults = await _documentService.SearchDocumentsAsync(query);
            return new QueryResult 
            { 
                Type = QueryResultType.DocumentSearch,
                Results = searchResults 
            };
            
        case QueryIntentType.Mixed:
            // Her iki yaklaşımı birleştir
            var chatResponse = await _chatService.ProcessChatQueryAsync(query);
            var docResults = await _documentService.SearchDocumentsAsync(query);
            
            return new QueryResult 
            { 
                Type = QueryResultType.Mixed,
                ChatResponse = chatResponse,
                DocumentResults = docResults 
            };
            
        default:
            throw new ArgumentException($"Bilinmeyen niyet türü: {intent.Type}");
    }
}</code></pre>
                    </div>

                    <h4>Niyet Analizi Yapılandırması</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Niyet algılamayı yapılandır
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
    
    // Akıllı sorgu niyet algılamayı etkinleştir
    options.EnableQueryIntentDetection = true;
    options.IntentDetectionThreshold = 0.7; // Güven eşiği
    options.LanguageAgnostic = true; // Herhangi bir dilde çalışır
});

// Controller'ınızda kullanın
[HttpPost("query")]
public async Task&lt;ActionResult&lt;QueryResult&gt;&gt; ProcessQuery([FromBody] QueryRequest request)
{
    var result = await _queryProcessor.ProcessQueryAsync(request.Query);
    return Ok(result);
}</code></pre>
                    </div>

                    <h3>Özel Parçalama Stratejisi</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class CustomChunkingStrategy : IChunkingStrategy
{
    public IEnumerable&lt;string&gt; ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List&lt;string&gt;();
        var sentences = text.Split(new[] { '.', '!', '?' }, 
            StringSplitOptions.RemoveEmptyEntries);
        
        var currentChunk = new StringBuilder();
        
        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > chunkSize)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }
            }
            currentChunk.AppendLine(sentence.Trim() + ".");
        }
        
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }
        
        return chunks;
    }
}</code></pre>
                    </div>

                    <h3>Özel AI Provider</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class CustomAIProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    public CustomAIProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["CustomAI:ApiKey"];
    }
    
    public async Task&lt;float[]&gt; GenerateEmbeddingAsync(string text)
    {
        var request = new
        {
            text = text,
            model = "custom-embedding-model"
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            "https://api.customai.com/embeddings", request);
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync&lt;EmbeddingResponse&gt;();
        return result.Embedding;
    }
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
    private readonly ILogger&lt;DocumentsController&gt; _logger;
    
    public DocumentsController(
        IDocumentService documentService,
        ILogger&lt;DocumentsController&gt; logger)
    {
        _documentService = documentService;
        _logger = logger;
    }
    
    [HttpPost("upload")]
    public async Task&lt;ActionResult&lt;Document&gt;&gt; UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");
            
        try
        {
            var document = await _documentService.UploadDocumentAsync(file);
            _logger.LogInformation("Document uploaded: {DocumentId}", document.Id);
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document: {FileName}", file.FileName);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("search")]
    public async Task&lt;ActionResult&lt;IEnumerable&lt;DocumentChunk&gt;&gt;&gt; SearchDocuments(
        [FromQuery] string query, 
        [FromQuery] int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query parameter is required");
            
        try
        {
            var results = await _documentService.SearchDocumentsAsync(query, maxResults);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query: {Query}", query);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("{id}")]
    public async Task&lt;ActionResult&lt;Document&gt;&gt; GetDocument(string id)
    {
        try
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound();
                
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document: {DocumentId}", id);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpDelete("{id}")]
    public async Task&lt;ActionResult&gt; DeleteDocument(string id)
    {
        try
        {
            var success = await _documentService.DeleteDocumentAsync(id);
            if (!success)
                return NotFound();
                
            _logger.LogInformation("Document deleted: {DocumentId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document: {DocumentId}", id);
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
        var services = new ServiceCollection();
        
        // Servisleri yapılandır
        services.AddSmartRAG(options =>
        {
            options.AIProvider = AIProvider.Anthropic;
            options.StorageProvider = StorageProvider.Qdrant;
            options.ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            options.ChunkSize = 1000;
            options.ChunkOverlap = 200;
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService&lt;IDocumentService&gt;();
        
        Console.WriteLine("SmartRAG Konsol Uygulaması");
        Console.WriteLine("============================");
        
        while (true)
        {
            Console.WriteLine("\nSeçenekler:");
            Console.WriteLine("1. Belge yükle");
            Console.WriteLine("2. Belgelerde ara");
            Console.WriteLine("3. Tüm belgeleri listele");
            Console.WriteLine("4. Çıkış");
            Console.Write("Bir seçenek seçin: ");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    await UploadDocument(documentService);
                    break;
                case "2":
                    await SearchDocuments(documentService);
                    break;
                case "3":
                    await ListDocuments(documentService);
                    break;
                case "4":
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
            var fileStream = File.OpenRead(filePath);
            
            // Mock IFormFile oluştur
            var formFile = new FormFile(fileStream, 0, fileInfo.Length, 
                fileInfo.Name, fileInfo.Name);
            
            var document = await documentService.UploadDocumentAsync(formFile);
            Console.WriteLine($"Belge başarıyla yüklendi. ID: {document.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Belge yükleme hatası: {ex.Message}");
        }
    }
    
    static async Task SearchDocuments(IDocumentService documentService)
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
            var results = await documentService.SearchDocumentsAsync(query, 5);
            Console.WriteLine($"{results.Count()} sonuç bulundu:");
            
            foreach (var result in results)
            {
                Console.WriteLine($"- {result.Content.Substring(0, Math.Min(100, result.Content.Length))}...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Belge arama hatası: {ex.Message}");
        }
    }
    
    static async Task ListDocuments(IDocumentService documentService)
    {
        try
        {
            var documents = await documentService.GetAllDocumentsAsync();
            Console.WriteLine($"Toplam belge sayısı: {documents.Count()}");
            
            foreach (var doc in documents)
            {
                Console.WriteLine($"- {doc.FileName} (ID: {doc.Id})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Belge listeleme hatası: {ex.Message}");
        }
    }
}</code></pre>
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
                        <p class="mb-0">Örnekler konusunda yardıma ihtiyacınız varsa:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/tr/getting-started">Başlangıç Kılavuzu</a></li>
                            <li><a href="{{ site.baseurl }}/tr/api-reference">API Referansı</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub'da issue açın</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">E-posta ile destek alın</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>