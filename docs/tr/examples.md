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

                    <h4>Gelişmiş Anlamsal Arama</h4>
                    <p>Hibrit puanlama (80% anlamsal + 20% anahtar kelime) ve bağlam farkındalığı ile gelişmiş arama:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task&lt;IEnumerable&lt;SearchResult&gt;&gt; EnhancedSearchAsync(
    string query, 
    SearchOptions options = null)
{
    // Hibrit puanlama ağırlıklarını yapılandır
    var searchConfig = new EnhancedSearchConfiguration
    {
        SemanticWeight = 0.8,        // 80% anlamsal benzerlik
        KeywordWeight = 0.2,          // 20% anahtar kelime eşleşmesi
        ContextWindowSize = 512,      // Bağlam farkındalığı penceresi
        MinSimilarityThreshold = 0.6, // Minimum benzerlik skoru
        EnableFuzzyMatching = true,   // Bulanık anahtar kelime eşleşmesi
        MaxResults = options?.MaxResults ?? 20
    };

    // Hibrit arama yap
    var results = await _searchService.EnhancedSearchAsync(query, searchConfig);
    
    // Bağlam farkındalıklı sıralama uygula
    var rankedResults = await _rankingService.RankByContextAsync(results, query);
    
    return rankedResults;
}</code></pre>
                    </div>

                    <h4>Arama Yapılandırması</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Gelişmiş anlamsal aramayı yapılandır
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
    
    // Gelişmiş anlamsal aramayı etkinleştir
    options.EnableEnhancedSearch = true;
    options.SemanticWeight = 0.8;
    options.KeywordWeight = 0.2;
    options.ContextAwareness = true;
    options.FuzzyMatching = true;
});

// Controller'ınızda kullanın
[HttpGet("enhanced-search")]
public async Task&lt;ActionResult&lt;IEnumerable&lt;SearchResult&gt;&gt;&gt; EnhancedSearch(
    [FromQuery] string query,
    [FromQuery] int maxResults = 20)
{
    var options = new SearchOptions { MaxResults = maxResults };
    var results = await _searchService.EnhancedSearchAsync(query, options);
    return Ok(results);
}</code></pre>
                    </div>

                    <h4>Gelişmiş Anlamsal Arama</h4>
                    <p>Hibrit puanlama (80% anlamsal + 20% anahtar kelime) ve bağlam farkındalığı ile gelişmiş arama:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task&lt;IEnumerable&lt;SearchResult&gt;&gt; EnhancedSearchAsync(
    string query, 
    SearchOptions options = null)
{
    // Hibrit puanlama ağırlıklarını yapılandır
    var searchConfig = new EnhancedSearchConfiguration
    {
        SemanticWeight = 0.8,        // 80% anlamsal benzerlik
        KeywordWeight = 0.2,          // 20% anahtar kelime eşleşmesi
        ContextWindowSize = 512,      // Bağlam farkındalığı penceresi
        MinSimilarityThreshold = 0.6, // Minimum benzerlik skoru
        EnableFuzzyMatching = true,   // Bulanık anahtar kelime eşleşmesi
        MaxResults = options?.MaxResults ?? 20
    };

    // Hibrit arama yap
    var results = await _searchService.EnhancedSearchAsync(query, searchConfig);
    
    // Bağlam farkındalıklı sıralama uygula
    var rankedResults = await _rankingService.RankByContextAsync(results, query);
    
    return rankedResults;
}</code></pre>
                    </div>

                    <h4>Arama Yapılandırması</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Gelişmiş anlamsal aramayı yapılandır
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
    
    // Gelişmiş anlamsal aramayı etkinleştir
    options.EnableEnhancedSearch = true;
    options.SemanticWeight = 0.8;
    options.KeywordWeight = 0.2;
    options.ContextAwareness = true;
    options.FuzzyMatching = true;
});

// Controller'ınızda kullanın
[HttpGet("enhanced-search")]
public async Task&lt;ActionResult&lt;IEnumerable&lt;SearchResult&gt;&gt;&gt; EnhancedSearch(
    [FromQuery] string query,
    [FromQuery] int maxResults = 20)
{
    var options = new SearchOptions { MaxResults = maxResults };
    var results = await _searchService.EnhancedSearchAsync(query, options);
    return Ok(results);
}</code></pre>
                    </div>

                    <h4>Dil-Agnostik Tasarım</h4>
                    <p>SmartRAG, hardcoded dil kalıpları veya dil-spesifik kurallar olmadan herhangi bir dilde çalışır:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Dil-agnostik yapılandırma - herhangi bir dilde çalışır
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
    
    // Dil-agnostik özellikleri etkinleştir
    options.LanguageAgnostic = true;
    options.AutoDetectLanguage = true;
    options.SupportedLanguages = new[] { "en", "tr", "de", "ru", "fr", "es", "ja", "ko", "zh" };
    
    // Hardcoded dil kalıpları yok
    options.EnableMultilingualSupport = true;
    options.FallbackLanguage = "en";
});

// Herhangi bir dildeki sorguları otomatik olarak işle
public async Task&lt;QueryResult&gt; ProcessMultilingualQueryAsync(string query)
{
    // Dil otomatik olarak algılanır
    var detectedLanguage = await _languageService.DetectLanguageAsync(query);
    
    // Dil-agnostik algoritmalarla işle
    var result = await _queryProcessor.ProcessQueryAsync(query, new QueryOptions
    {
        Language = detectedLanguage,
        UseLanguageAgnosticProcessing = true
    });
    
    return result;
}</code></pre>
                    </div>

                    <h4>Gelişmiş Dil-Agnostik Özellikler</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Gelişmiş dil-agnostik yapılandırma
var languageAgnosticConfig = new LanguageAgnosticConfiguration
{
    EnableLanguageDetection = true,
    EnableMultilingualEmbeddings = true,
    EnableCrossLanguageSearch = true,
    LanguageDetectionThreshold = 0.8,
    SupportedScripts = new[] { "Latin", "Cyrillic", "Arabic", "Chinese", "Japanese", "Korean" },
    EnableScriptNormalization = true,
    EnableUnicodeNormalization = true,
    FallbackStrategies = new[] { "transliteration", "romanization", "english" }
};

services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
    
    // Gelişmiş dil-agnostik özellikleri yapılandır
    options.LanguageAgnostic = true;
    options.LanguageAgnosticConfig = languageAgnosticConfig;
});

// Çok dilli belge işleme için controller
[HttpPost("multilingual-upload")]
public async Task&lt;ActionResult&lt;MultilingualUploadResult&gt;&gt; UploadMultilingualDocument(
    [FromBody] MultilingualUploadRequest request)
{
    try
    {
        // Belgeyi herhangi bir dilde işle
        var document = await _documentService.UploadMultilingualDocumentAsync(
            request.Content, 
            request.FileName,
            request.DetectedLanguage);
        
        // Dil-agnostik algoritmalar kullanarak embedding'ler oluştur
        var embeddings = await _embeddingService.GenerateMultilingualEmbeddingsAsync(
            document.Chunks,
            document.DetectedLanguage);
        
        // Dil metadata'sı ile sakla
        await _storageService.StoreMultilingualDocumentAsync(document, embeddings);
        
        return Ok(new MultilingualUploadResult
        {
            DocumentId = document.Id,
            DetectedLanguage = document.DetectedLanguage,
            LanguageConfidence = document.LanguageConfidence,
            TotalChunks = document.Chunks.Count,
            ProcessingTime = document.ProcessingTime
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Çok dilli belge işlenirken hata");
        return StatusCode(500, "Çok dilli belge işlenemedi");
    }
}

// Tüm dillerde çok dilli arama
[HttpGet("multilingual-search")]
public async Task&lt;ActionResult&lt;MultilingualSearchResult&gt;&gt; SearchMultilingual(
    [FromQuery] string query,
    [FromQuery] string[] languages = null,
    [FromQuery] int maxResults = 20)
{
    try
    {
        var searchOptions = new MultilingualSearchOptions
        {
            Query = query,
            TargetLanguages = languages ?? new[] { "auto" },
            MaxResults = maxResults,
            EnableCrossLanguageSearch = true,
            UseLanguageAgnosticScoring = true
        };
        
        var results = await _searchService.SearchMultilingualAsync(searchOptions);
        
        return Ok(new MultilingualSearchResult
        {
            Query = query,
            DetectedQueryLanguage = results.DetectedLanguage,
            Results = results.Results,
            CrossLanguageMatches = results.CrossLanguageMatches,
            TotalResults = results.TotalResults
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Çok dilli aramada hata");
        return StatusCode(500, "Çok dilli arama yapılamadı");
    }
}</code></pre>
                    </div>

                    <h4>Anthropic API Retry Mekanizması</h4>
                    <p>HTTP 529 (Overloaded) hataları için gelişmiş retry logic:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task&lt;ChatResponse&gt; ProcessWithRetryAsync(string prompt, int maxRetries = 3)
{
    var retryPolicy = new ExponentialBackoffRetryPolicy
    {
        MaxRetries = maxRetries,
        BaseDelay = TimeSpan.FromSeconds(2),
        MaxDelay = TimeSpan.FromSeconds(30),
        JitterFactor = 0.1
    };

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var response = await _anthropicService.ChatAsync(new ChatRequest
            {
                Model = "claude-3-sonnet-20240229",
                MaxTokens = 1000,
                Messages = new[] { new Message { Role = "user", Content = prompt } }
            });

            return response;
        }
        catch (AnthropicApiException ex) when (ex.StatusCode == 529)
        {
            _logger.LogWarning("Anthropic API aşırı yüklü (HTTP 529), deneme {Attempt}/{MaxRetries}", 
                attempt, maxRetries);

            if (attempt == maxRetries)
            {
                throw new AnthropicServiceUnavailableException(
                    "Anthropic API şu anda birden fazla deneme sonrası aşırı yüklü", ex);
            }

            var delay = retryPolicy.CalculateDelay(attempt);
            await Task.Delay(delay);
        }
        catch (AnthropicApiException ex) when (ex.StatusCode == 429)
        {
            // Rate limiting - exponential backoff kullan
            var delay = retryPolicy.CalculateDelay(attempt);
            await Task.Delay(delay);
        }
    }

    throw new InvalidOperationException("Beklenmeyen retry loop çıkışı");
}</code></pre>
                    </div>

                    <h4>Gelişmiş Retry Yapılandırması</h4>
                    <p>Farklı hata senaryoları için sofistike retry politikaları yapılandırın:</p>
                    
                    <h5>Temel Retry Yapılandırması</h5>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-anthropic-api-key";
    
    // Gelişmiş retry mekanizmasını etkinleştir
    options.EnableAdvancedRetry = true;
    options.RetryConfiguration = new AnthropicRetryConfiguration
    {
        MaxRetries = 5,
        BaseDelay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(60),
        JitterFactor = 0.15,
        EnableCircuitBreaker = true,
        CircuitBreakerThreshold = 10,
        CircuitBreakerTimeout = TimeSpan.FromMinutes(5),
        RetryOnStatusCodes = new[] { 429, 529, 500, 502, 503, 504 },
        ExponentialBackoff = true
    };
});</code></pre>
                    </div>

                    <h5>Özel Retry Koşulları</h5>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Özel retry koşulları
options.RetryConfiguration.CustomRetryPredicate = async (exception, attempt) =>
{
    if (exception is AnthropicApiException apiEx)
    {
        // 529 (Overloaded) durumunda her zaman retry yap
        if (apiEx.StatusCode == 529) return true;
        
        // 429 (Rate Limited) durumunda backoff ile retry yap
        if (apiEx.StatusCode == 429) return attempt <= 3;
        
        // Sunucu hatalarında retry yap
        if (apiEx.StatusCode >= 500) return attempt <= 2;
    }
    
    return false;
};</code></pre>
                    </div>

                    <h5>Servis Implementasyonu</h5>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class AnthropicService
{
    private readonly IAnthropicClient _client;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ILogger<AnthropicService> _logger;

    public AnthropicService(IAnthropicClient client, IRetryPolicy retryPolicy, ILogger<AnthropicService> logger)
    {
        _client = client;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }

    public async Task&lt;ChatResponse&gt; ChatWithRetryAsync(ChatRequest request)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                return await _client.ChatAsync(request);
            }
            catch (AnthropicApiException ex)
            {
                _logger.LogError(ex, "Anthropic API hatası: {StatusCode} - {Message}", 
                    ex.StatusCode, ex.Message);
                
                if (ex.StatusCode == 529)
                {
                    _logger.LogWarning("API aşırı yük tespit edildi - backoff stratejisi uygulanıyor");
                }
                
                throw;
            }
        });
    }
}</code></pre>
                    </div>

                    <h4>Circuit Breaker Pattern</h4>
                    <p>API koruması için circuit breaker uygulayın:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Circuit breaker implementasyonu
public class AnthropicCircuitBreaker
{
    private readonly ILogger<AnthropicCircuitBreaker> _logger;
    private readonly int _failureThreshold;
    private readonly TimeSpan _resetTimeout;
    
    private int _failureCount;
    private DateTime _lastFailureTime;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;

    public AnthropicCircuitBreaker(ILogger<AnthropicCircuitBreaker> logger, 
        int failureThreshold = 10, TimeSpan? resetTimeout = null)
    {
        _logger = logger;
        _failureThreshold = failureThreshold;
        _resetTimeout = resetTimeout ?? TimeSpan.FromMinutes(5);
    }

    public async Task&lt;T&gt; ExecuteAsync&lt;T&gt;(Func&lt;Task&lt;T&gt;&gt; action)
    {
        if (_state == CircuitBreakerState.Open)
        {
            if (DateTime.UtcNow - _lastFailureTime > _resetTimeout)
            {
                _logger.LogInformation("Circuit breaker timeout'a ulaştı, kapatmaya çalışılıyor");
                _state = CircuitBreakerState.HalfOpen;
            }
            else
            {
                throw new CircuitBreakerOpenException("Circuit breaker açık");
            }
        }

        try
        {
            var result = await action();
            
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _logger.LogInformation("Circuit breaker başarıyla kapatıldı");
                _state = CircuitBreakerState.Closed;
                _failureCount = 0;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            
            if (_failureCount >= _failureThreshold)
            {
                _logger.LogWarning("Circuit breaker {FailureCount} başarısızlıktan sonra açıldı", _failureCount);
                _state = CircuitBreakerState.Open;
            }
            
            throw;
        }
    }
}

// Circuit breaker ile controller implementasyonu
[HttpPost("chat-with-retry")]
public async Task&lt;ActionResult&lt;ChatResponse&gt;&gt; ChatWithRetry([FromBody] ChatRequest request)
{
    try
    {
        var response = await _anthropicService.ChatWithRetryAsync(request);
        return Ok(response);
    }
    catch (CircuitBreakerOpenException)
    {
        return StatusCode(503, new { error = "Yüksek hata oranı nedeniyle servis geçici olarak kullanılamıyor" });
    }
    catch (AnthropicServiceUnavailableException ex)
    {
        return StatusCode(503, new { error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Chat servisinde beklenmeyen hata");
        return StatusCode(500, new { error = "İç sunucu hatası" });
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