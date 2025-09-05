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
                    <h2>Базовые Примеры</h2>
                    <p>Простые примеры для начала работы с SmartRAG.</p>
                    
                    <h3>Простая Загрузка Документа</h3>
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

                    <h3>Поиск Документов</h3>
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

                    <h3>Генерация RAG Ответа (с историей разговоров)</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("search")]
public async Task<ActionResult<object>> Search([FromBody] SearchRequest request)
{
    string query = request?.Query ?? string.Empty;
    int maxResults = request?.MaxResults ?? 5;

    if (string.IsNullOrWhiteSpace(query))
        return BadRequest("Query cannot be empty");

    try
    {
        var response = await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);
        return Ok(response);
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Internal server error: {ex.Message}");
    }
}

public class SearchRequest
{
    [Required]
    public string Query { get; set; } = string.Empty;

    [Range(1, 50)]
    [DefaultValue(5)]
    public int MaxResults { get; set; } = 5;

    /// <summary>
    /// Session ID for conversation history
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Advanced Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Продвинутые Примеры</h2>
                    <p>Более сложные примеры для продвинутых случаев использования.</p>
                    
                    <h3>Пакетная Обработка Документов</h3>
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

                    <h3>Управление Документами</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Получить все документы
[HttpGet]
public async Task<ActionResult<List<Document>>> GetAllDocuments()
{
    var documents = await _documentService.GetAllDocumentsAsync();
    return Ok(documents);
}

// Получить конкретный документ
[HttpGet("{id}")]
public async Task<ActionResult<Document>> GetDocument(Guid id)
{
    var document = await _documentService.GetDocumentAsync(id);
    if (document == null)
        return NotFound();
    
    return Ok(document);
}

// Удалить документ
[HttpDelete("{id}")]
public async Task<ActionResult> DeleteDocument(Guid id)
{
    var success = await _documentService.DeleteDocumentAsync(id);
    if (!success)
        return NotFound();
    
    return NoContent();
}</code></pre>
                    </div>

                    <h3>Статистика Хранилища</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpGet("statistics")]
public async Task<ActionResult<Dictionary<string, object>>> GetStorageStatistics()
{
    var stats = await _documentService.GetStorageStatisticsAsync();
    return Ok(stats);
}</code></pre>
                    </div>

                    <h3>Операции с Embedding</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Регенерировать все embedding
[HttpPost("regenerate-embeddings")]
public async Task<ActionResult> RegenerateAllEmbeddings()
{
    var success = await _documentService.RegenerateAllEmbeddingsAsync();
    if (success)
        return Ok("Все embedding успешно регенерированы");
    else
        return BadRequest("Не удалось регенерировать embedding");
}

// Очистить все embedding
[HttpPost("clear-embeddings")]
public async Task<ActionResult> ClearAllEmbeddings()
{
    var success = await _documentService.ClearAllEmbeddingsAsync();
    if (success)
        return Ok("Все embedding успешно очищены");
    else
        return BadRequest("Не удалось очистить embedding");
}

// Очистить все документы
[HttpPost("clear-all")]
public async Task<ActionResult> ClearAllDocuments()
{
    var success = await _documentService.ClearAllDocumentsAsync();
    if (success)
        return Ok("Все документы успешно очищены");
    else
        return BadRequest("Не удалось очистить документы");
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
                    <p>Полные примеры контроллеров для веб-приложений.</p>
                    
                    <h3>Полный Контроллер</h3>
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
            return BadRequest("Файл не предоставлен");
            
        try
        {
            using var stream = file.OpenReadStream();
            var document = await _documentService.UploadDocumentAsync(
                stream, file.FileName, file.ContentType, "user123");
            _logger.LogInformation("Документ загружен: {DocumentId}", document.Id);
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось загрузить документ: {FileName}", file.FileName);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<DocumentChunk>>> SearchDocuments(
        [FromQuery] string query, 
        [FromQuery] int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Параметр запроса обязателен");
            
        try
        {
            var results = await _documentSearchService.SearchDocumentsAsync(query, maxResults);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Поиск не удался для запроса: {Query}", query);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("chat")]
    public async Task<ActionResult<RagResponse>> ChatWithDocuments(
        [FromBody] string query,
        [FromQuery] int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Параметр запроса обязателен");
            
        try
        {
            var response = await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Чат не удался для запроса: {Query}", query);
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
            _logger.LogError(ex, "Не удалось получить документ: {DocumentId}", id);
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
            _logger.LogError(ex, "Не удалось получить все документы");
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

            _logger.LogInformation("Документ удален: {DocumentId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось удалить документ: {DocumentId}", id);
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
                    <h2>Пример Консольного Приложения</h2>
                    <p>Полный пример консольного приложения.</p>
                    
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
        
        // Настроить сервисы
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
        
        Console.WriteLine("SmartRAG Консольное Приложение");
        Console.WriteLine("===============================");

            while (true)
            {
            Console.WriteLine("\nОпции:");
                Console.WriteLine("1. Загрузить документ");
            Console.WriteLine("2. Искать документы");
            Console.WriteLine("3. Чат с документами");
            Console.WriteLine("4. Список всех документов");
            Console.WriteLine("5. Выход");
            Console.Write("Выберите опцию: ");

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
                    Console.WriteLine("Неверная опция. Попробуйте снова.");
                            break;
            }
        }
    }
    
    static async Task UploadDocument(IDocumentService documentService)
        {
            Console.Write("Введите путь к файлу: ");
            var filePath = Console.ReadLine();

            if (!File.Exists(filePath))
            {
            Console.WriteLine("Файл не найден.");
                return;
            }

        try
        {
            var fileInfo = new FileInfo(filePath);
            using var fileStream = File.OpenRead(filePath);
            
            var document = await documentService.UploadDocumentAsync(
                fileStream, fileInfo.Name, "application/octet-stream", "console-user");
            Console.WriteLine($"Документ успешно загружен. ID: {document.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки документа: {ex.Message}");
        }
    }
    
    static async Task SearchDocuments(IDocumentSearchService documentSearchService)
        {
            Console.Write("Введите поисковый запрос: ");
            var query = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine("Запрос не может быть пустым.");
            return;
        }
        
        try
        {
            var results = await documentSearchService.SearchDocumentsAsync(query, 5);
            Console.WriteLine($"Найдено {results.Count} результатов:");
            
            foreach (var result in results)
            {
                Console.WriteLine($"- {result.Content.Substring(0, Math.Min(100, result.Content.Length))}...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка поиска документов: {ex.Message}");
        }
    }
    
    static async Task ChatWithDocuments(IDocumentSearchService documentSearchService)
    {
        Console.Write("Введите ваш вопрос: ");
        var query = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine("Вопрос не может быть пустым.");
            return;
        }
        
        try
        {
            var response = await documentSearchService.GenerateRagAnswerAsync(query, 5);
            Console.WriteLine($"ИИ Ответ: {response.Answer}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка чата с документами: {ex.Message}");
        }
    }
    
    static async Task ListDocuments(IDocumentService documentService)
    {
        try
        {
            var documents = await documentService.GetAllDocumentsAsync();
            Console.WriteLine($"Всего документов: {documents.Count}");
            
            foreach (var doc in documents)
            {
                Console.WriteLine($"- {doc.FileName} (ID: {doc.Id})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка списка документов: {ex.Message}");
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
                    <h2>Примеры Конфигурации</h2>
                    <p>Различные способы настройки сервисов SmartRAG.</p>
                    
                    <h3>Базовая Конфигурация</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
services.UseSmartRag(configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.Gemini
);</code></pre>
                    </div>

                    <h3>Продвинутая Конфигурация</h3>
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

                    <h3>Конфигурация appsettings.json</h3>
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
                        <h4><i class="fas fa-question-circle me-2"></i>Нужна Помощь?</h4>
                        <p class="mb-2">Если вам нужна помощь с примерами:</p>
                        <ul class="mb-0">
                            <li><a href="{{ site.baseurl }}/ru/getting-started">Руководство по началу работы</a></li>
                            <li><a href="{{ site.baseurl }}/ru/api-reference">Справочник API</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues">Открыть issue на GitHub</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Связаться с поддержкой по email</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
