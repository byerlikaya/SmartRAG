---
layout: default
title: Справочник API
description: Полная документация API для сервисов и интерфейсов SmartRAG
lang: ru
---

<div class="page-content">
    <div class="container">
        <!-- Core Interfaces Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Основные интерфейсы</h2>
                    <p>Основные интерфейсы и сервисы SmartRAG.</p>
                    
                    <h3>IDocumentService</h3>
                    <p>Основной интерфейс сервиса для операций с документами.</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentService
{
    Task<Document> UploadDocumentAsync(IFormFile file);
    Task<Document> GetDocumentByIdAsync(string id);
    Task<IEnumerable<Document>> GetAllDocumentsAsync();
    Task<bool> DeleteDocumentAsync(string id);
    Task<IEnumerable<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5);
    Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5);
}</code></pre>
                    </div>

                    <h3>IDocumentParserService</h3>
                    <p>Сервис для парсинга различных форматов файлов.</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentParserService
{
    Task<string> ParseDocumentAsync(IFormFile file);
    bool CanParse(string fileName);
    Task<IEnumerable<string>> ChunkTextAsync(string text, int chunkSize = 1000, int overlap = 200);
}</code></pre>
                    </div>

                    <h3>IDocumentSearchService</h3>
                    <p>Сервис для поиска документов и RAG операций.</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentSearchService
{
    Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5);
    Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5);
}</code></pre>
                    </div>

                    <h3>IAIService</h3>
                    <p>Сервис для взаимодействия с AI провайдерами.</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IAIService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<string> GenerateTextAsync(string prompt);
    Task<string> GenerateTextAsync(string prompt, string context);
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Models Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Модели</h2>
                    <p>Основные модели данных, используемые в SmartRAG.</p>
                    
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
    public List<DocumentChunk> Chunks { get; set; }
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
    public List<SearchSource> Sources { get; set; }
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
                    <h2>Перечисления</h2>
                    <p>Значения перечислений, используемые в SmartRAG.</p>
                    
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
                    <h2>Регистрация сервисов</h2>
                    <p>Как зарегистрировать SmartRAG сервисы в вашем приложении.</p>
                    
                    <h3>Базовая регистрация</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs или Startup.cs
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

                    <h3>Расширенная конфигурация</h3>
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
    options.FallbackProviders = new List<AIProvider> 
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
                    <h2>Примеры использования</h2>
                    <p>Как использовать SmartRAG API.</p>
                    
                    <h3>Загрузка документа</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("upload")]
public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
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

                    <h3>Поиск документов</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpGet("search")]
public async Task<ActionResult<IEnumerable<DocumentChunk>>> SearchDocuments(
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

                    <h3>Генерация RAG ответа</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("ask")]
public async Task<ActionResult<RagResponse>> AskQuestion([FromBody] string question)
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
                    <h2>Обработка ошибок</h2>
                    <p>Обработка ошибок и типы исключений в SmartRAG.</p>
                    
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-exclamation-triangle me-2"></i>Важно</h4>
                        <p class="mb-0">Все сервисы SmartRAG разработаны с надлежащей обработкой ошибок. Оберните ваши API вызовы в try-catch блоки.</p>
                    </div>

                    <h3>Частые ошибки</h3>
                    <ul>
                        <li><strong>ArgumentException</strong>: Неверные параметры</li>
                        <li><strong>FileNotFoundException</strong>: Файл не найден</li>
                        <li><strong>UnauthorizedAccessException</strong>: Неверный API ключ</li>
                        <li><strong>HttpRequestException</strong>: Проблемы с сетевым подключением</li>
                        <li><strong>TimeoutException</strong>: Таймаут запроса</li>
                    </ul>

                    <h3>Пример обработки ошибок</h3>
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
                    <h2>Логирование</h2>
                    <p>Логирование и мониторинг в SmartRAG.</p>
                    
                    <h3>Уровни логирования</h3>
                    <ul>
                        <li><strong>Information</strong>: Обычные операции</li>
                        <li><strong>Warning</strong>: Предупреждения и неожиданные ситуации</li>
                        <li><strong>Error</strong>: Ошибки и исключения</li>
                        <li><strong>Debug</strong>: Подробная отладочная информация</li>
                    </ul>

                    <h3>Конфигурация логирования</h3>
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
                    <h2>Соображения производительности</h2>
                    <p>Оптимизация производительности в SmartRAG.</p>
                    
                    <h3>Рекомендуемые настройки</h3>
                    <ul>
                        <li><strong>Chunk Size</strong>: 1000-1500 символов</li>
                        <li><strong>Chunk Overlap</strong>: 200-300 символов</li>
                        <li><strong>Max Results</strong>: 5-10 результатов</li>
                        <li><strong>Retry Attempts</strong>: 3-5 попыток</li>
                    </ul>

                    <h3>Советы по производительности</h3>
                    <ul>
                        <li>Обрабатывайте большие файлы заранее</li>
                        <li>Используйте подходящие размеры чанков</li>
                        <li>Включайте механизмы кэширования</li>
                        <li>Предпочитайте асинхронные операции</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Нужна помощь?</h4>
                        <p class="mb-0">По вопросам API:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/ru/getting-started">Начало работы</a></li>
                            <li><a href="{{ site.baseurl }}/ru/examples">Примеры</a></li>
                            <li><a href="{{ site.baseurl }}/ru/configuration">Конфигурация</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">Создать GitHub Issue</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Поддержка по email</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>