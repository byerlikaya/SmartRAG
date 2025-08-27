---
layout: default
title: Примеры
description: Практические примеры и случаи использования для SmartRAG
lang: ru
---

<div class="page-header">
    <div class="container">
        <div class="row">
            <div class="col-lg-8 mx-auto text-center">
                <h1 class="page-title">Примеры</h1>
                <p class="page-description">
                    Практические примеры и случаи использования для SmartRAG
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