---
layout: default
title: Примеры
description: Практические примеры и образцы кода для интеграции SmartRAG
lang: ru
---

<div class="page-content">
    <div class="container">
        <!-- Basic Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Базовые примеры</h2>
                    <p>Простые примеры для начала работы с SmartRAG.</p>
                    
                    <h3>Загрузка и поиск документов</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger&lt;DocumentController&gt; _logger;

    public DocumentController(IDocumentService documentService, ILogger&lt;DocumentController&gt; logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task&lt;ActionResult&lt;Document&gt;&gt; UploadDocument(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран");

            var document = await _documentService.UploadDocumentAsync(file);
            _logger.LogInformation("Документ {FileName} успешно загружен", file.FileName);
            
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке документа");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpGet("search")]
    public async Task&lt;ActionResult&lt;IEnumerable&lt;DocumentChunk&gt;&gt;&gt; SearchDocuments(
        [FromQuery] string query, 
        [FromQuery] int maxResults = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Поисковый запрос не может быть пустым");

            var results = await _documentService.SearchDocumentsAsync(query, maxResults);
            _logger.LogInformation("Поиск по '{Query}' дал {Count} результатов", query, results.Count());
            
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при поиске документов");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}</code></pre>
                    </div>

                    <h3>Генерация RAG ответа</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("ask")]
public async Task&lt;ActionResult&lt;RagResponse&gt;&gt; AskQuestion([FromBody] AskQuestionRequest request)
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Вопрос не может быть пустым");

        var response = await _documentService.GenerateRagAnswerAsync(request.Question, request.MaxResults ?? 5);
        _logger.LogInformation("RAG ответ на вопрос '{Question}' сгенерирован", request.Question);
        
        return Ok(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при генерации RAG ответа");
        return StatusCode(500, "Внутренняя ошибка сервера");
    }
}

public class AskQuestionRequest
{
    public string Question { get; set; }
    public int? MaxResults { get; set; }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Advanced Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Расширенные примеры</h2>
                    <p>Более сложные случаи использования и расширенные функции.</p>
                    
                    <h3>Умное определение намерения запроса</h3>
                    <p>Автоматически направляйте запросы в чат или поиск документов на основе анализа намерения:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task&lt;QueryResult&gt; ProcessQueryAsync(string query)
{
    // Анализировать намерение запроса
    var intent = await _queryIntentService.AnalyzeIntentAsync(query);
    
    switch (intent.Type)
    {
        case QueryIntentType.Chat:
            // Направить к разговорному ИИ
            return await _chatService.ProcessChatQueryAsync(query);
            
        case QueryIntentType.DocumentSearch:
            // Направить к поиску документов
            var searchResults = await _documentService.SearchDocumentsAsync(query);
            return new QueryResult 
            { 
                Type = QueryResultType.DocumentSearch,
                Results = searchResults 
            };
            
        case QueryIntentType.Mixed:
            // Объединить оба подхода
            var chatResponse = await _chatService.ProcessChatQueryAsync(query);
            var docResults = await _documentService.SearchDocumentsAsync(query);
            
            return new QueryResult 
            { 
                Type = QueryResultType.Mixed,
                ChatResponse = chatResponse,
                DocumentResults = docResults 
            };
            
        default:
            throw new ArgumentException($"Неизвестный тип намерения: {intent.Type}");
    }
}</code></pre>
                    </div>

                    <h4>Расширенный семантический поиск</h4>
                    <p>Расширенный поиск с гибридной оценкой (80% семантическая + 20% ключевые слова) и осведомленностью о контексте:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task&lt;IEnumerable&lt;SearchResult&gt;&gt; EnhancedSearchAsync(
    string query, 
    SearchOptions options = null)
{
    // Настроить веса гибридной оценки
    var searchConfig = new EnhancedSearchConfiguration
    {
        SemanticWeight = 0.8,        // 80% семантическое сходство
        KeywordWeight = 0.2,          // 20% соответствие ключевым словам
        ContextWindowSize = 512,      // Окно осведомленности о контексте
        MinSimilarityThreshold = 0.6, // Минимальный порог сходства
        EnableFuzzyMatching = true,   // Нечеткое соответствие ключевым словам
        MaxResults = options?.MaxResults ?? 20
    };

    // Выполнить гибридный поиск
    var results = await _searchService.EnhancedSearchAsync(query, searchConfig);
    
    // Применить ранжирование с учетом контекста
    var rankedResults = await _rankingService.RankByContextAsync(results, query);
    
    return rankedResults;
}</code></pre>
                    </div>

                    <h4>Интеграция VoyageAI</h4>
                    <p>Высококачественные эмбеддинги для моделей Anthropic Claude с использованием VoyageAI:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Настроить интеграцию VoyageAI
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-anthropic-api-key";
    
    // Включить VoyageAI для высококачественных эмбеддингов
    options.EnableVoyageAI = true;
    options.VoyageAI.ApiKey = "your-voyageai-api-key";
    options.VoyageAI.Model = "voyage-large-2"; // Последняя модель
    options.VoyageAI.Dimensions = 1536; // Размеры эмбеддингов
    options.VoyageAI.BatchSize = 100; // Пакетная обработка
});

// Использовать эмбеддинги VoyageAI в вашем сервисе
public async Task&lt;IEnumerable&lt;float[]&gt;&gt; GenerateEmbeddingsAsync(
    IEnumerable&lt;string&gt; texts)
{
    var embeddingService = serviceProvider.GetRequiredService&lt;IVoyageAIEmbeddingService&gt;();
    
    // Генерировать высококачественные эмбеддинги
    var embeddings = await embeddingService.GenerateEmbeddingsAsync(texts);
    
    return embeddings;
}</code></pre>
                    </div>

                    <h4>Расширенная конфигурация VoyageAI</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Расширенная конфигурация VoyageAI с пользовательскими настройками
var voyageAIConfig = new VoyageAIConfiguration
{
    ApiKey = "your-voyageai-api-key",
    Model = "voyage-large-2",
    Dimensions = 1536,
    BatchSize = 100,
    MaxRetries = 3,
    Timeout = TimeSpan.FromSeconds(30),
    EnableCompression = true,
    CustomHeaders = new Dictionary&lt;string, string&gt;
    {
        ["User-Agent"] = "SmartRAG/1.1.0"
    }
};

services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-anthropic-api-key";
    
    // Настроить VoyageAI с пользовательскими настройками
    options.EnableVoyageAI = true;
    options.VoyageAI = voyageAIConfig;
});

// Реализация контроллера
[HttpPost("generate-embeddings")]
public async Task&lt;ActionResult&lt;EmbeddingResponse&gt;&gt; GenerateEmbeddings(
    [FromBody] EmbeddingRequest request)
{
    try
    {
        var embeddingService = _serviceProvider.GetRequiredService&lt;IVoyageAIEmbeddingService&gt;();
        
        var embeddings = await embeddingService.GenerateEmbeddingsAsync(request.Texts);
        
        return Ok(new EmbeddingResponse
        {
            Embeddings = embeddings,
            Model = "voyage-large-2",
            Dimensions = embeddings.FirstOrDefault()?.Length ?? 0,
            TotalTokens = request.Texts.Sum(t => t.Split(' ').Length)
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при генерации эмбеддингов с VoyageAI");
        return StatusCode(500, "Не удалось сгенерировать эмбеддинги");
    }
}</code></pre>
                    </div>

                    <h4>Конфигурация поиска</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Настроить расширенный семантический поиск
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
    
    // Включить расширенный семантический поиск
    options.EnableEnhancedSearch = true;
    options.SemanticWeight = 0.8;
    options.KeywordWeight = 0.2;
    options.ContextAwareness = true;
    options.FuzzyMatching = true;
});

// Использовать в вашем контроллере
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

                    <h4>Языково-агностический дизайн</h4>
                    <p>SmartRAG работает с любым языком без жестко заданных языковых шаблонов или языково-специфичных правил:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Языково-агностическая конфигурация - работает с любым языком
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
    
    // Включить языково-агностические функции
    options.LanguageAgnostic = true;
    options.AutoDetectLanguage = true;
    options.SupportedLanguages = new[] { "en", "tr", "de", "ru", "fr", "es", "ja", "ko", "zh" };
    
    // Нет жестко заданных языковых шаблонов
    options.EnableMultilingualSupport = true;
    options.FallbackLanguage = "en";
});

// Автоматически обрабатывать запросы на любом языке
public async Task&lt;QueryResult&gt; ProcessMultilingualQueryAsync(string query)
{
    // Язык определяется автоматически
    var detectedLanguage = await _languageService.DetectLanguageAsync(query);
    
    // Обрабатывать с помощью языково-агностических алгоритмов
    var result = await _queryProcessor.ProcessQueryAsync(query, new QueryOptions
    {
        Language = detectedLanguage,
        UseLanguageAgnosticProcessing = true
    });
    
    return result;
}</code></pre>
                    </div>

                    <h4>Расширенные языково-агностические функции</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Расширенная языково-агностическая конфигурация
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
    
    // Настроить расширенные языково-агностические функции
    options.LanguageAgnostic = true;
    options.LanguageAgnosticConfig = languageAgnosticConfig;
});

// Контроллер для многоязычной обработки документов
[HttpPost("multilingual-upload")]
public async Task&lt;ActionResult&lt;MultilingualUploadResult&gt;&gt; UploadMultilingualDocument(
    [FromBody] MultilingualUploadRequest request)
{
    try
    {
        // Обработать документ на любом языке
        var document = await _documentService.UploadMultilingualDocumentAsync(
            request.Content, 
            request.FileName,
            request.DetectedLanguage);
        
        // Сгенерировать эмбеддинги с помощью языково-агностических алгоритмов
        var embeddings = await _embeddingService.GenerateMultilingualEmbeddingsAsync(
            document.Chunks,
            document.DetectedLanguage);
        
        // Сохранить с языковыми метаданными
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
        _logger.LogError(ex, "Ошибка при многоязычной обработке документа");
        return StatusCode(500, "Не удалось обработать многоязычный документ");
    }
}

// Многоязычный поиск по всем языкам
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
        _logger.LogError(ex, "Ошибка при многоязычном поиске");
        return StatusCode(500, "Не удалось выполнить многоязычный поиск");
    }
}</code></pre>
                    </div>

                    <h4>Механизм повторных попыток Anthropic API</h4>
                    <p>Расширенная логика повторных попыток для обработки ошибок HTTP 529 (Overloaded):</p>
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
            _logger.LogWarning("Anthropic API перегружен (HTTP 529), попытка {Attempt}/{MaxRetries}", 
                attempt, maxRetries);

            if (attempt == maxRetries)
            {
                throw new AnthropicServiceUnavailableException(
                    "Anthropic API в настоящее время перегружен после нескольких попыток", ex);
            }

            var delay = retryPolicy.CalculateDelay(attempt);
            await Task.Delay(delay);
        }
        catch (AnthropicApiException ex) when (ex.StatusCode == 429)
        {
            // Ограничение скорости - используйте экспоненциальную задержку
            var delay = retryPolicy.CalculateDelay(attempt);
            await Task.Delay(delay);
        }
    }

    throw new InvalidOperationException("Неожиданный выход из цикла повторных попыток");
}</code></pre>
                    </div>

                    <h4>Расширенная конфигурация повторных попыток</h4>
                    <p>Настройте сложные политики повторных попыток для различных сценариев ошибок:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Настройка расширенных политик повторных попыток
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-anthropic-api-key";
    
    // Включить расширенный механизм повторных попыток
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
        
        // Специальная обработка ошибок
        RetryOnStatusCodes = new[] { 429, 529, 500, 502, 503, 504 },
        ExponentialBackoff = true,
        LinearBackoff = false,
        
        // Пользовательские условия повторных попыток
        CustomRetryPredicate = async (exception, attempt) =>
        {
            if (exception is AnthropicApiException apiEx)
            {
                // Всегда повторять при 529 (Overloaded)
                if (apiEx.StatusCode == 529) return true;
                
                // Повторять при 429 (Rate Limited) с задержкой
                if (apiEx.StatusCode == 429) return attempt <= 3;
                
                // Повторять при ошибках сервера
                if (apiEx.StatusCode >= 500) return attempt <= 2;
            }
            
            return false;
        }
    };
});

// Используйте в вашем сервисе с обработкой повторных попыток
public class AnthropicService
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
                _logger.LogError(ex, "Ошибка Anthropic API: {StatusCode} - {Message}", 
                    ex.StatusCode, ex.Message);
                
                // Логировать специальные детали ошибок для мониторинга
                if (ex.StatusCode == 529)
                {
                    _logger.LogWarning("Обнаружена перегрузка API - применяется стратегия задержки");
                }
                
                throw;
            }
        });
    }
}</code></pre>
                    </div>

                    <h4>Шаблон Circuit Breaker</h4>
                    <p>Реализуйте Circuit Breaker для защиты API:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Реализация Circuit Breaker
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
                _logger.LogInformation("Достигнут таймаут Circuit Breaker, пытаемся закрыть");
                _state = CircuitBreakerState.HalfOpen;
            }
            else
            {
                throw new CircuitBreakerOpenException("Circuit Breaker открыт");
            }
        }

        try
        {
            var result = await action();
            
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _logger.LogInformation("Circuit Breaker успешно закрыт");
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
                _logger.LogWarning("Circuit Breaker открыт после {FailureCount} сбоев", _failureCount);
                _state = CircuitBreakerState.Open;
            }
            
            throw;
        }
    }
}

// Реализация контроллера с Circuit Breaker
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
        return StatusCode(503, new { error = "Служба временно недоступна из-за высокой частоты ошибок" });
    }
    catch (AnthropicServiceUnavailableException ex)
    {
        return StatusCode(503, new { error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Неожиданная ошибка в службе чата");
        return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
    }
}</code></pre>
                    </div>

                    <h4>Конфигурация определения намерения</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Настроить определение намерения
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
    
    // Включить умное определение намерения запроса
    options.EnableQueryIntentDetection = true;
    options.IntentDetectionThreshold = 0.7; // Порог доверия
    options.LanguageAgnostic = true; // Работает с любым языком
});

// Использовать в вашем контроллере
[HttpPost("query")]
public async Task&lt;ActionResult&lt;QueryResult&gt;&gt; ProcessQuery([FromBody] QueryRequest request)
{
    var result = await _queryProcessor.ProcessQueryAsync(request.Query);
    return Ok(result);
}</code></pre>
                    </div>

                    <h3>Пакетная обработка документов</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("upload-batch")]
public async Task&lt;ActionResult&lt;BatchUploadResult&gt;&gt; UploadBatchDocuments(IFormFileCollection files)
{
    try
    {
        var results = new List&lt;Document&gt;();
        var errors = new List&lt;string&gt;();

        foreach (var file in files)
        {
            try
            {
                var document = await _documentService.UploadDocumentAsync(file);
                results.Add(document);
                _logger.LogInformation("Документ {FileName} успешно обработан", file.FileName);
            }
            catch (Exception ex)
            {
                var error = $"Ошибка при обработке {file.FileName}: {ex.Message}";
                errors.Add(error);
                _logger.LogWarning(ex, "Ошибка при обработке {FileName}", file.FileName);
            }
        }

        return Ok(new BatchUploadResult
        {
            SuccessfulUploads = results,
            Errors = errors,
            TotalFiles = files.Count,
            SuccessCount = results.Count,
            ErrorCount = errors.Count
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при пакетной обработке");
        return StatusCode(500, "Внутренняя ошибка сервера");
    }
}

public class BatchUploadResult
{
    public List&lt;Document&gt; SuccessfulUploads { get; set; }
    public List&lt;string&gt; Errors { get; set; }
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
}</code></pre>
                    </div>

                    <h3>Расширенный поиск с фильтрами</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("advanced-search")]
public async Task&lt;ActionResult&lt;AdvancedSearchResult&gt;&gt; AdvancedSearch([FromBody] AdvancedSearchRequest request)
{
    try
    {
        var searchResults = await _documentService.SearchDocumentsAsync(request.Query, request.MaxResults);
        
        // Применение дополнительных фильтров
        var filteredResults = searchResults
            .Where(r => request.MinSimilarityScore == null || r.SimilarityScore >= request.MinSimilarityScore)
            .Where(r => request.ContentTypes == null || !request.ContentTypes.Any() || 
                       request.ContentTypes.Contains(r.Document.ContentType))
            .OrderByDescending(r => r.SimilarityScore)
            .ToList();

        var result = new AdvancedSearchResult
        {
            Query = request.Query,
            Results = filteredResults,
            TotalResults = filteredResults.Count,
            SearchTime = DateTime.UtcNow,
            AppliedFilters = new
            {
                MinSimilarityScore = request.MinSimilarityScore,
                ContentTypes = request.ContentTypes
            }
        };

        _logger.LogInformation("Расширенный поиск по '{Query}' дал {Count} отфильтрованных результатов", 
            request.Query, filteredResults.Count);
        
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при расширенном поиске");
        return StatusCode(500, "Внутренняя ошибка сервера");
    }
}

public class AdvancedSearchRequest
{
    public string Query { get; set; }
    public int MaxResults { get; set; } = 10;
    public float? MinSimilarityScore { get; set; }
    public List&lt;string&gt; ContentTypes { get; set; }
}

public class AdvancedSearchResult
{
    public string Query { get; set; }
    public List&lt;DocumentChunk&gt; Results { get; set; }
    public int TotalResults { get; set; }
    public DateTime SearchTime { get; set; }
    public object AppliedFilters { get; set; }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Web API Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Примеры Web API</h2>
                    <p>Полные Web API контроллеры со всеми функциями SmartRAG.</p>
                    
                    <h3>Полный Document Controller</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger&lt;DocumentController&gt; _logger;

    public DocumentController(IDocumentService documentService, ILogger&lt;DocumentController&gt; logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Загружает документ
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(Document), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task&lt;ActionResult&lt;Document&gt;&gt; UploadDocument(IFormFile file)
    {
        // Реализация см. выше
    }

    /// <summary>
    /// Получает документ по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Document), 200)]
    [ProducesResponseType(404)]
    public async Task&lt;ActionResult&lt;Document&gt;&gt; GetDocument(string id)
    {
        try
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound($"Документ с ID {id} не найден");

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении документа {Id}", id);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    /// <summary>
    /// Получает все документы
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable&lt;Document&gt;), 200)]
    public async Task&lt;ActionResult&lt;IEnumerable&lt;Document&gt;&gt;&gt; GetAllDocuments()
    {
        try
        {
            var documents = await _documentService.GetAllDocumentsAsync();
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении всех документов");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    /// <summary>
    /// Удаляет документ
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task&lt;ActionResult&gt; DeleteDocument(string id)
    {
        try
        {
            var success = await _documentService.DeleteDocumentAsync(id);
            if (!success)
                return NotFound($"Документ с ID {id} не найден");

            _logger.LogInformation("Документ {Id} успешно удален", id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении документа {Id}", id);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    /// <summary>
    /// Ищет в документах
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable&lt;DocumentChunk&gt;), 200)]
    public async Task&lt;ActionResult&lt;IEnumerable&lt;DocumentChunk&gt;&gt;&gt; SearchDocuments(
        [FromQuery] string query, 
        [FromQuery] int maxResults = 10)
    {
        // Реализация см. выше
    }

    /// <summary>
    /// Генерирует RAG ответ
    /// </summary>
    [HttpPost("ask")]
    [ProducesResponseType(typeof(RagResponse), 200)]
    public async Task&lt;ActionResult&lt;RagResponse&gt;&gt; AskQuestion([FromBody] AskQuestionRequest request)
    {
        // Реализация см. выше
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
                    <h2>Пример консольного приложения</h2>
                    <p>Простое консольное приложение с интеграцией SmartRAG.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces;

namespace SmartRAG.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            using var scope = host.Services.CreateScope();
            var documentService = scope.ServiceProvider.GetRequiredService&lt;IDocumentService&gt;();
            var logger = scope.ServiceProvider.GetRequiredService&lt;ILogger&lt;Program&gt;&gt;();

            logger.LogInformation("Консольное приложение SmartRAG запущено");

            while (true)
            {
                Console.WriteLine("\n=== Консольное приложение SmartRAG ===");
                Console.WriteLine("1. Загрузить документ");
                Console.WriteLine("2. Поиск документов");
                Console.WriteLine("3. Задать вопрос");
                Console.WriteLine("4. Выход");
                Console.Write("Выберите опцию (1-4): ");

                var choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await UploadDocument(documentService, logger);
                            break;
                        case "2":
                            await SearchDocuments(documentService, logger);
                            break;
                        case "3":
                            await AskQuestion(documentService, logger);
                            break;
                        case "4":
                            logger.LogInformation("Приложение завершается");
                            return;
                        default:
                            Console.WriteLine("Неверная опция. Пожалуйста, выберите 1-4.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при выполнении");
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }
        }

        static async Task UploadDocument(IDocumentService documentService, ILogger logger)
        {
            Console.Write("Введите путь к файлу: ");
            var filePath = Console.ReadLine();

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Файл не найден!");
                return;
            }

            var fileInfo = new FileInfo(filePath);
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var fileStream = new MemoryStream(fileBytes);
            var formFile = new FormFile(fileStream, 0, fileBytes.Length, "file", fileInfo.Name);

            var document = await documentService.UploadDocumentAsync(formFile);
            Console.WriteLine($"Документ успешно загружен: {document.Id}");
        }

        static async Task SearchDocuments(IDocumentService documentService, ILogger logger)
        {
            Console.Write("Введите поисковый запрос: ");
            var query = Console.ReadLine();

            var results = await documentService.SearchDocumentsAsync(query, 5);
            
            Console.WriteLine($"\nНайденные результаты ({results.Count()}):");
            foreach (var result in results)
            {
                Console.WriteLine($"- {result.Document.FileName} (Сходство: {result.SimilarityScore:P2})");
                Console.WriteLine($"  {result.Content.Substring(0, Math.Min(100, result.Content.Length))}...");
            }
        }

        static async Task AskQuestion(IDocumentService documentService, ILogger logger)
        {
            Console.Write("Задайте ваш вопрос: ");
            var question = Console.ReadLine();

            var response = await documentService.GenerateRagAnswerAsync(question, 5);
            
            Console.WriteLine($"\nОтвет: {response.Answer}");
            Console.WriteLine($"\nИсточники ({response.Sources.Count}):");
            foreach (var source in response.Sources)
            {
                Console.WriteLine($"- {source.DocumentName} (Сходство: {source.SimilarityScore:P2})");
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSmartRAG(context.Configuration, options =>
                    {
                        options.AIProvider = AIProvider.Anthropic;
                        options.StorageProvider = StorageProvider.Qdrant;
                    });
                });
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
                        <h4><i class="fas fa-question-circle me-2"></i>Нужна помощь?</h4>
                        <p class="mb-0">Для дополнительных примеров и поддержки:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/ru/getting-started">Начало работы</a></li>
                            <li><a href="{{ site.baseurl }}/ru/configuration">Конфигурация</a></li>
                            <li><a href="{{ site.baseurl }}/ru/api-reference">Справочник API</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG" target="_blank">GitHub репозиторий</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Поддержка по email</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>