---
layout: default
title: Устранение неполадок
description: Распространенные проблемы и решения для реализации SmartRAG
lang: ru
---

<div class="page-content">
    <div class="container">
        <!-- Common Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Частые проблемы</h2>
                    <p>Частые проблемы и решения, с которыми вы можете столкнуться при использовании SmartRAG.</p>

                    <h3>Проблемы компиляции</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-exclamation-triangle me-2"></i>Внимание</h4>
                        <p class="mb-0">Сначала создайте чистое решение для устранения ошибок компиляции.</p>
                    </div>

                    <h4>Ошибка пакета NuGet</h4>
                    <div class="code-example">
                        <pre><code class="language-bash"># Чистое решение
dotnet clean
dotnet restore
dotnet build</code></pre>
                    </div>

                    <h4>Конфликт зависимостей</h4>
                    <div class="code-example">
                        <pre><code class="language-xml"><PackageReference Include="SmartRAG" Version="1.1.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" /></code></pre>
                    </div>

                    <h3>Проблемы времени выполнения</h3>
                    
                    <h4>Ошибка API ключа</h4>
                    <div class="alert alert-danger">
                        <h4><i class="fas fa-times-circle me-2"></i>Ошибка</h4>
                        <p class="mb-0">UnauthorizedAccessException: API ключ недействителен или отсутствует.</p>
                    </div>

                    <div class="code-example">
                        <pre><code class="language-json">// appsettings.json
{
  "SmartRAG": {
    "AIProvider": "Anthropic",
    "Anthropic": {
      "ApiKey": "your-api-key-here",
      "Model": "claude-3-sonnet-20240229"
    }
  }
}</code></pre>
                    </div>

                    <h4>Таймаут соединения</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxRetryAttempts = 5;
    options.RetryDelayMs = 2000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
});</code></pre>
                    </div>

                    <h3>Проблемы производительности</h3>
                    
                    <h4>Медленный поиск</h4>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Совет</h4>
                        <p class="mb-0">Оптимизируйте размеры чанков для улучшения производительности.</p>
                    </div>

                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(configuration, options =>
{
    options.MaxChunkSize = 1000;  // Рекомендуется 1000-1500
    options.ChunkOverlap = 200;   // Рекомендуется 200-300
    options.MaxRetryAttempts = 3;
});</code></pre>
                    </div>

                    <h4>Использование памяти</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Используйте потоковую передачу для больших файлов
public async Task<Document> UploadLargeDocumentAsync(IFormFile file)
{
    using var stream = file.OpenReadStream();
    var chunks = await _documentParserService.ChunkTextAsync(stream, 1000, 200);
    
    var document = new Document
    {
        Id = Guid.NewGuid().ToString(),
        FileName = file.FileName,
        Content = await _documentParserService.ParseDocumentAsync(file),
        ContentType = file.ContentType,
        FileSize = file.Length,
        UploadedAt = DateTime.UtcNow
    };
    
    return document;
}</code></pre>
                    </div>

                    <h3>Проблемы конфигурации</h3>
                    
                    <h4>Неправильный выбор провайдера</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Правильная конфигурация
services.AddSmartRAG(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    
    // Необходимые настройки для Qdrant
    options.Qdrant = new QdrantOptions
    {
        Host = "localhost",
        Port = 6333,
        CollectionName = "smartrag_documents"
    };
});</code></pre>
                    </div>

                    <h4>Отсутствующие зависимости</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Необходимые сервисы
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SmartRAG сервисы
builder.Services.AddSmartRAG(builder.Configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
});

var app = builder.Build();

// Пайплайн middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Debugging Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Отладка</h2>
                    <p>Техники отладки для вашего SmartRAG приложения.</p>
                    
                    <h3>Конфигурация логирования</h3>
                    
                    <h5>Конфигурация логирования</h5>
                    <div class="code-example">
                        <pre><code class="language-json">// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SmartRAG": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}</code></pre>
                    </div>

                    <h5>Реализация контроллера</h5>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(IDocumentService documentService, ILogger<DocumentController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }
}</code></pre>
                    </div>

                    <h5>Обработка ошибок</h5>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("upload")]
public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
{
    _logger.LogInformation("Загрузка документа начата: {FileName}, Размер: {Size}", 
        file?.FileName, file?.Length);

    try
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Файл null или пустой");
            return BadRequest("Файл не выбран");
        }

        _logger.LogDebug("Файл проверен, начинается обработка");
        var document = await _documentService.UploadDocumentAsync(file);
        
        _logger.LogInformation("Документ успешно загружен: {DocumentId}", document.Id);
        return Ok(document);
    }
    catch (ArgumentException ex)
    {
        _logger.LogError(ex, "Неверный формат файла: {FileName}", file?.FileName);
        return BadRequest($"Неверный формат файла: {ex.Message}");
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogError(ex, "Ошибка API ключа");
        return Unauthorized("Неверный API ключ");
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Ошибка сетевого подключения");
        return StatusCode(503, "Сервис временно недоступен");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Произошла неожиданная ошибка");
        return StatusCode(500, "Внутренняя ошибка сервера");
    }
}</code></pre>
                    </div>

                    <h3>Мониторинг производительности</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("search")]
public async Task<ActionResult<IEnumerable<DocumentChunk>>> SearchDocuments(
    [FromQuery] string query, 
    [FromQuery] int maxResults = 10)
{
    var stopwatch = Stopwatch.StartNew();
    _logger.LogInformation("Поиск начат: {Query}", query);

    try
    {
        var results = await _documentService.SearchDocumentsAsync(query, maxResults);
        stopwatch.Stop();
        
        _logger.LogInformation("Поиск завершен: {Query}, Результаты: {Count}, Время: {Duration}ms", 
            query, results.Count(), stopwatch.ElapsedMilliseconds);
        
        return Ok(results);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger.LogError(ex, "Ошибка поиска: {Query}, Время: {Duration}ms", 
            query, stopwatch.ElapsedMilliseconds);
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
                    <h2>Тестирование</h2>
                    <p>Методы тестирования вашего SmartRAG приложения.</p>
                    
                    <h3>Модульные тесты</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task UploadDocument_ValidFile_ReturnsDocument()
{
    // Arrange
    var mockFile = new Mock<IFormFile>();
    mockFile.Setup(f => f.FileName).Returns("test.pdf");
    mockFile.Setup(f => f.Length).Returns(1024);
    mockFile.Setup(f => f.ContentType).Returns("application/pdf");
    
    var mockDocumentService = new Mock<IDocumentService>();
    var expectedDocument = new Document { Id = "test-id", FileName = "test.pdf" };
    mockDocumentService.Setup(s => s.UploadDocumentAsync(It.IsAny<IFormFile>()))
                      .ReturnsAsync(expectedDocument);
    
    var controller = new DocumentController(mockDocumentService.Object, Mock.Of<ILogger<DocumentController>>());
    
    // Act
    var result = await controller.UploadDocument(mockFile.Object);
    
    // Assert
    Assert.IsInstanceOf<OkObjectResult>(result);
    var okResult = result as OkObjectResult;
    Assert.AreEqual(expectedDocument, okResult.Value);
}</code></pre>
                    </div>

                    <h3>Интеграционные тесты</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task SearchDocuments_IntegrationTest()
{
    // Arrange
    var host = CreateTestHost();
    using var scope = host.Services.CreateScope();
    var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();
    
    // Загрузить тестовые данные
    var testFile = CreateTestFile("test-document.txt", "Это тестовый документ.");
    await documentService.UploadDocumentAsync(testFile);
    
    // Act
    var results = await documentService.SearchDocumentsAsync("test", 5);
    
    // Assert
    Assert.IsNotNull(results);
    Assert.IsTrue(results.Any());
    Assert.IsTrue(results.First().Content.Contains("test"));
}

private IHost CreateTestHost()
{
    return Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            services.AddSmartRAG(context.Configuration, options =>
            {
                options.AIProvider = AIProvider.Anthropic;
                options.StorageProvider = StorageProvider.InMemory; // Использовать InMemory для тестов
            });
        })
        .Build();
}</code></pre>
                    </div>

                    <h3>API тесты</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task UploadDocument_ApiTest()
{
    // Arrange
    var client = _factory.CreateClient();
    var content = new MultipartFormDataContent();
    var fileContent = new ByteArrayContent(File.ReadAllBytes("test-file.pdf"));
    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
    content.Add(fileContent, "file", "test-file.pdf");
    
    // Act
    var response = await client.PostAsync("/api/document/upload", content);
    
    // Assert
    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    var document = JsonSerializer.Deserialize<Document>(responseContent);
    Assert.IsNotNull(document);
    Assert.AreEqual("test-file.pdf", document.FileName);
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Getting Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Получение помощи</h2>
                    <p>Методы получения помощи с проблемами SmartRAG.</p>
                    
                    <h3>GitHub Issues</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-github me-2"></i>GitHub</h4>
                        <p class="mb-0">Сообщайте о проблемах на GitHub и получайте поддержку сообщества.</p>
                    </div>

                    <h3>Поддержка по email</h3>
                    <div class="alert alert-success">
                        <h4><i class="fas fa-envelope me-2"></i>Email</h4>
                        <p class="mb-0">Получайте прямую поддержку по email: <a href="mailto:b.yerlikaya@outlook.com">b.yerlikaya@outlook.com</a></p>
                    </div>

                    <h3>Документация</h3>
                    <ul>
                        <li><a href="{{ site.baseurl }}/ru/getting-started">Начало работы</a></li>
                        <li><a href="{{ site.baseurl }}/ru/configuration">Конфигурация</a></li>
                        <li><a href="{{ site.baseurl }}/ru/api-reference">Справочник API</a></li>
                        <li><a href="{{ site.baseurl }}/ru/examples">Примеры</a></li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Prevention Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Профилактика</h2>
                    <p>Методы предотвращения проблем в вашем SmartRAG приложении.</p>
                    
                    <h3>Лучшие практики</h3>
                    <ul>
                        <li><strong>Обработка ошибок</strong>: Оберните все API вызовы в try-catch блоки</li>
                        <li><strong>Логирование</strong>: Выполняйте детальное логирование и настройте уровни логов соответствующим образом</li>
                        <li><strong>Производительность</strong>: Оптимизируйте размеры чанков и используйте потоковую передачу для больших файлов</li>
                        <li><strong>Безопасность</strong>: Безопасно храните API ключи</li>
                        <li><strong>Тестирование</strong>: Напишите комплексные тесты и запускайте их в CI/CD пайплайнах</li>
                    </ul>

                    <h3>Проверка конфигурации</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class SmartRAGHealthCheck : IHealthCheck
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<SmartRAGHealthCheck> _logger;

    public SmartRAGHealthCheck(IDocumentService documentService, ILogger<SmartRAGHealthCheck> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Простой тестовый запрос
            var results = await _documentService.SearchDocumentsAsync("health check", 1);
            
            return HealthCheckResult.Healthy("SmartRAG сервис работает");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Проверка здоровья SmartRAG не удалась");
            return HealthCheckResult.Unhealthy("SmartRAG сервис не работает", ex);
        }
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
