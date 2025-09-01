---
layout: default
title: Устранение Неполадок
description: Распространенные проблемы и решения для реализации SmartRAG
lang: ru
---

<div class="page-content">
    <div class="container">
        <!-- Common Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Распространенные Проблемы</h2>
                    <p>Распространенные проблемы и решения, которые вы можете встретить при использовании SmartRAG.</p>

                    <h3>Проблемы Регистрации Сервисов</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-exclamation-triangle me-2"></i>Предупреждение</h4>
                        <p class="mb-0">Всегда убеждайтесь в правильной регистрации сервисов и настройке внедрения зависимостей.</p>
                    </div>

                    <h4>Сервис Не Зарегистрирован</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Убедитесь, что сервисы правильно зарегистрированы
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// Получить необходимые сервисы
var documentService = serviceProvider.GetRequiredService<IDocumentService>();
var documentSearchService = serviceProvider.GetRequiredService<IDocumentSearchService>();</code></pre>
                    </div>

                    <h4>Проблемы Конфигурации</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Обеспечить правильную конфигурацию
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

                    <h3>Конфигурация API Ключей</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Конфигурация</h4>
                        <p class="mb-0">API ключи должны быть настроены в appsettings.json или переменных окружения.</p>
                    </div>

                    <h4>Переменные Окружения</h4>
                    <div class="code-example">
                        <pre><code class="language-bash"># Установить переменные окружения
export ANTHROPIC_API_KEY=your-anthropic-api-key
export QDRANT_API_KEY=your-qdrant-api-key

# Или использовать appsettings.json
{
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
                    </div>

                    <h3>Проблемы Производительности</h3>
                    <div class="alert alert-success">
                        <h4><i class="fas fa-tachometer-alt me-2"></i>Оптимизация</h4>
                        <p class="mb-0">Производительность можно улучшить с помощью правильной конфигурации.</p>
                    </div>

                    <h4>Медленная Обработка Документов</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Оптимизировать размер чанков для более быстрой обработки
services.AddSmartRag(configuration, options =>
{
    options.MaxChunkSize = 500; // Меньшие чанки для более быстрой обработки
    options.MinChunkSize = 50;
    options.ChunkOverlap = 100;
    options.MaxRetryAttempts = 2; // Уменьшить повторные попытки для быстрого сбоя
});</code></pre>
                    </div>

                    <h4>Оптимизация Использования Памяти</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Использовать подходящий провайдер хранилища
services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.InMemory; // Для небольших наборов данных
    // или
    options.StorageProvider = StorageProvider.Qdrant; // Для больших наборов данных
    options.EnableFallbackProviders = true; // Включить резервные провайдеры для надежности
});</code></pre>
                    </div>

                    <h3>Конфигурация Повторных Попыток</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Настроить политики повторных попыток
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
                    <h2>Отладка</h2>
                    <p>Инструменты и техники, которые помогут вам отлаживать приложения SmartRAG.</p>

                    <h3>Включить Логирование</h3>
                    
                    <h4>Конфигурация Логирования</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Настроить логирование
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Добавить специальное логирование SmartRAG
builder.Logging.AddFilter("SmartRAG", LogLevel.Debug);</code></pre>
                    </div>

                    <h4>Реализация Сервиса</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">private readonly ILogger<DocumentsController> _logger;

public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
{
    _logger.LogInformation("Загрузка документа: {FileName}", file.FileName);
    try
    {
        using var stream = file.OpenReadStream();
        var document = await _documentService.UploadDocumentAsync(
            stream, file.FileName, file.ContentType, "user123");
        _logger.LogInformation("Документ успешно загружен: {DocumentId}", document.Id);
        return Ok(document);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Не удалось загрузить документ: {FileName}", file.FileName);
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>Обработка Исключений</h3>
                    
                    <h4>Базовая Обработка Ошибок</h4>
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
    _logger.LogError(ex, "Неверный формат файла: {FileName}", file.FileName);
    return BadRequest("Неверный формат файла");
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Ошибка AI провайдера: {Message}", ex.Message);
    return StatusCode(503, "Сервис временно недоступен");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Неожиданная ошибка при загрузке: {FileName}", file.FileName);
    return StatusCode(500, "Внутренняя ошибка сервера");
}</code></pre>
                    </div>

                    <h4>Обработка Ошибок На Уровне Сервиса</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy)
{
    try
    {
        _logger.LogInformation("Начинается загрузка документа: {FileName}", fileName);
        
        // Проверить входные данные
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("Поток файла равен null или пуст");
        
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Имя файла обязательно");
        
        // Обработать документ
        var document = await ProcessDocumentAsync(fileStream, fileName, contentType, uploadedBy);
        
        _logger.LogInformation("Документ успешно загружен: {DocumentId}", document.Id);
        return document;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Не удалось загрузить документ: {FileName}", fileName);
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
                    <p>Как тестировать вашу реализацию SmartRAG.</p>

                    <h3>Модульные Тесты</h3>
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

                    <h3>Интеграционные Тесты</h3>
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
        new DocumentChunk { Content = "Тестовое содержимое 1" },
        new DocumentChunk { Content = "Тестовое содержимое 2" }
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

                    <h3>End-to-End Тесты</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task CompleteWorkflow_UploadSearchChat_WorksCorrectly()
{
    // Arrange
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
    
    // Создать тестовый файл
    var testContent = "Это тестовый документ об искусственном интеллекте.";
    var testStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testContent));
    
    // Act - Загрузка
    var document = await documentService.UploadDocumentAsync(
        testStream, "test.txt", "text/plain", "test-user");
    
    // Assert - Загрузка
    Assert.IsNotNull(document);
    Assert.AreEqual("test.txt", document.FileName);
    
    // Act - Поиск
    var searchResults = await documentSearchService.SearchDocumentsAsync("искусственный интеллект", 5);
    
    // Assert - Поиск
    Assert.IsNotNull(searchResults);
    Assert.IsTrue(searchResults.Count > 0);
    
    // Act - Чат
    var chatResponse = await documentSearchService.GenerateRagAnswerAsync("О чем этот документ?", 5);
    
    // Assert - Чат
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
                    <h2>Получение Помощи</h2>
                    <p>Если у вас все еще есть проблемы, вот как получить помощь.</p>

                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-github me-2"></i>GitHub Issues</h4>
                                <p class="mb-0">Сообщайте об ошибках и запрашивайте функции на GitHub.</p>
                                <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank" class="btn btn-sm btn-outline-info mt-2">Открыть Issue</a>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-envelope me-2"></i>Email Поддержка</h4>
                                <p class="mb-0">Получите прямую помощь по электронной почте.</p>
                                <a href="mailto:b.yerlikaya@outlook.com" class="btn btn-sm btn-outline-success mt-2">Связаться</a>
                            </div>
                        </div>
                    </div>

                    <h3>Перед Обращением За Помощью</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-list me-2"></i>Контрольный Список</h4>
                        <ul class="mb-0">
                            <li>Проверьте руководство <a href="{{ site.baseurl }}/ru/getting-started">Начало Работы</a></li>
                            <li>Просмотрите документацию по <a href="{{ site.baseurl }}/ru/configuration">Конфигурации</a></li>
                            <li>Поищите существующие <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub Issues</a></li>
                            <li>Включите сообщения об ошибках и детали конфигурации</li>
                            <li>Проверьте <a href="{{ site.baseurl }}/ru/api-reference">API Справочник</a> для правильных сигнатур методов</li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>

        <!-- Prevention Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Профилактика</h2>
                    <p>Лучшие практики для избежания распространенных проблем.</p>

                    <h3>Лучшие Практики Конфигурации</h3>
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="alert alert-primary">
                                <h4><i class="fas fa-key me-2"></i>API Ключи</h4>
                                <p class="mb-0">Никогда не хардкодите API ключи. Используйте переменные окружения или безопасную конфигурацию.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-database me-2"></i>Хранилище</h4>
                                <p class="mb-0">Выбирайте правильного провайдера хранилища для вашего случая использования.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-shield-alt me-2"></i>Обработка Ошибок</h4>
                                <p class="mb-0">Реализуйте правильную обработку ошибок и логирование.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-warning">
                                <h4><i class="fas fa-balance-scale me-2"></i>Производительность</h4>
                                <p class="mb-0">Мониторьте производительность и оптимизируйте размеры чанков.</p>
                            </div>
                        </div>
                    </div>

                    <h3>Конфигурация Разработки vs Продакшена</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Конфигурация разработки
if (builder.Environment.IsDevelopment())
{
    services.AddSmartRag(configuration, options =>
    {
        options.AIProvider = AIProvider.Gemini; // Бесплатный уровень для разработки
        options.StorageProvider = StorageProvider.InMemory; // Быстро для разработки
        options.MaxChunkSize = 500;
        options.ChunkOverlap = 100;
        options.MaxRetryAttempts = 1; // Быстрый сбой в разработке
    });
}
else
{
    // Конфигурация продакшена
    services.AddSmartRag(configuration, options =>
    {
        options.AIProvider = AIProvider.Anthropic; // Лучшее качество для продакшена
        options.StorageProvider = StorageProvider.Qdrant; // Постоянное хранилище
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
