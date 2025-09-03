---
layout: default
title: Устранение неполадок
description: Частые проблемы и решения для SmartRAG
lang: ru
---

<!-- Page Header -->
<div class="page-header">
    <div class="container">
        <h1 class="page-title">Устранение неполадок</h1>
        <p class="page-description">
            Решения частых проблем, с которыми вы можете столкнуться при использовании SmartRAG
        </p>
    </div>
</div>

<!-- Main Content -->
<div class="main-content">
    <div class="container">
        
        <!-- Quick Navigation -->
        <div class="content-section">
            <div class="row">
                <div class="col-12">
                    <div class="alert alert-info" role="alert">
                        <i class="fas fa-info-circle me-2"></i>
                        <strong>Нужна помощь?</strong> Если вы не можете найти решение здесь, проверьте наш 
                        <a href="{{ site.baseurl }}/ru/getting-started" class="alert-link">Руководство по началу работы</a> 
                        или создайте проблему на <a href="https://github.com/byerlikaya/SmartRAG" class="alert-link" target="_blank">GitHub</a>.
                    </div>
                </div>
                    </div>
                    </div>

        <!-- Configuration Issues -->
        <div class="content-section">
            <h2>Проблемы конфигурации</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-key"></i>
                    </div>
                        <h3>Конфигурация API ключей</h3>
                        <p><strong>Проблема:</strong> Ошибки аутентификации с AI или провайдерами хранилищ.</p>
                        <p><strong>Решение:</strong> Убедитесь, что ваши API ключи правильно настроены в <code>appsettings.json</code>:</p>

                    <div class="code-example">
                            <pre><code class="language-json">{
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

                        <p>Или установите переменные окружения:</p>
                    <div class="code-example">
                            <pre><code class="language-bash"># Установить переменные окружения
export ANTHROPIC_API_KEY=your-anthropic-api-key
export QDRANT_API_KEY=your-qdrant-api-key</code></pre>
                    </div>
                    </div>
                    </div>

                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-cogs"></i>
                    </div>
                        <h3>Проблемы регистрации сервисов</h3>
                        <p><strong>Проблема:</strong> Ошибки dependency injection.</p>
                        <p><strong>Решение:</strong> Убедитесь, что сервисы SmartRAG правильно зарегистрированы в вашем <code>Program.cs</code>:</p>
                    
                    <div class="code-example">
                            <pre><code class="language-csharp">using SmartRAG.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Добавить сервисы SmartRAG
builder.Services.AddSmartRag(builder.Configuration);

var app = builder.Build();
app.UseSmartRag(builder.Configuration, StorageProvider.Qdrant, AIProvider.Anthropic);
app.Run();</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Document Upload Issues -->
        <div class="content-section">
            <h2>Проблемы загрузки документов</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-file-upload"></i>
                        </div>
                        <h3>Ограничения размера файлов</h3>
                        <p><strong>Проблема:</strong> Большие документы не могут быть загружены или обработаны.</p>
                        <p><strong>Решения:</strong></p>
                        <ul>
                            <li>Проверьте ограничения размера файлов вашего приложения в <code>appsettings.json</code></li>
                            <li>Рассмотрите возможность разделения больших документов на меньшие части</li>
                            <li>Убедитесь, что достаточно памяти для обработки</li>
                        </ul>
                    </div>
                    </div>

                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-file-alt"></i>
                        </div>
                        <h3>Неподдерживаемые типы файлов</h3>
                        <p><strong>Проблема:</strong> Ошибки для определенных форматов файлов.</p>
                        <p><strong>Решение:</strong> SmartRAG поддерживает распространенные текстовые форматы:</p>
                        <ul>
                            <li>PDF файлы (.pdf)</li>
                            <li>Текстовые файлы (.txt)</li>
                            <li>Word документы (.docx)</li>
                            <li>Markdown файлы (.md)</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Search and Retrieval Issues -->
        <div class="content-section">
            <h2>Проблемы поиска и извлечения</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-search"></i>
                        </div>
                        <h3>Нет результатов поиска</h3>
                        <p><strong>Проблема:</strong> Поисковые запросы не возвращают результатов.</p>
                        <p><strong>Возможные решения:</strong></p>
                        <ol>
                            <li><strong>Проверьте загрузку документов:</strong> Убедитесь, что документы были успешно загружены</li>
                            <li><strong>Проверьте эмбеддинги:</strong> Убедитесь, что эмбеддинги были правильно сгенерированы</li>
                            <li><strong>Специфичность запроса:</strong> Попробуйте более специфичные поисковые термины</li>
                            <li><strong>Соединение с хранилищем:</strong> Проверьте доступность вашего провайдера хранилища</li>
                        </ol>
                    </div>
                    </div>

                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-chart-line"></i>
                        </div>
                        <h3>Плохое качество поиска</h3>
                        <p><strong>Проблема:</strong> Результаты поиска не релевантны.</p>
                        <p><strong>Решения:</strong></p>
                        <ul>
                            <li>Настройте параметры <code>MaxChunkSize</code> и <code>ChunkOverlap</code></li>
                            <li>Используйте более специфичные поисковые запросы</li>
                            <li>Убедитесь, что документы правильно отформатированы</li>
                            <li>Проверьте актуальность эмбеддингов</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Performance Issues -->
        <div class="content-section">
            <h2>Проблемы производительности</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-tachometer-alt"></i>
                        </div>
                        <h3>Медленная обработка документов</h3>
                        <p><strong>Проблема:</strong> Загрузка и обработка документов занимает слишком много времени.</p>
                        <p><strong>Решения:</strong></p>
                        <ul>
                            <li>Увеличьте <code>MaxChunkSize</code>, чтобы уменьшить количество частей</li>
                            <li>Используйте более мощного AI провайдера</li>
                            <li>Оптимизируйте конфигурацию провайдера хранилища</li>
                            <li>Рассмотрите использование async операций во всем приложении</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-memory"></i>
                        </div>
                        <h3>Проблемы с памятью</h3>
                        <p><strong>Проблема:</strong> Приложение исчерпывает память во время обработки.</p>
                        <p><strong>Решения:</strong></p>
                        <ul>
                            <li>Уменьшите <code>MaxChunkSize</code>, чтобы создавать меньшие части</li>
                            <li>Обрабатывайте документы пакетами</li>
                            <li>Мониторьте использование памяти и оптимизируйте соответственно</li>
                            <li>Рассмотрите использование streaming операций для больших файлов</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Storage Provider Issues -->
        <div class="content-section">
            <h2>Проблемы провайдеров хранилищ</h2>
            
            <div class="row g-4">
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-database"></i>
                        </div>
                        <h3>Проблемы соединения с Qdrant</h3>
                        <p><strong>Проблема:</strong> Не удается подключиться к Qdrant.</p>
                        <p><strong>Решения:</strong></p>
                        <ul>
                            <li>Проверьте правильность API ключа Qdrant</li>
                            <li>Проверьте сетевое соединение с сервисом Qdrant</li>
                            <li>Убедитесь, что сервис Qdrant запущен и доступен</li>
                            <li>Проверьте настройки файрвола</li>
                        </ul>
                    </div>
                    </div>

                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-redis"></i>
                        </div>
                        <h3>Проблемы соединения с Redis</h3>
                        <p><strong>Проблема:</strong> Не удается подключиться к Redis.</p>
                        <p><strong>Решения:</strong></p>
                        <ul>
                            <li>Проверьте строку соединения Redis</li>
                            <li>Убедитесь, что сервер Redis запущен</li>
                            <li>Проверьте сетевое соединение</li>
                            <li>Проверьте конфигурацию Redis в <code>appsettings.json</code></li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-hdd"></i>
                        </div>
                        <h3>Проблемы с SQLite</h3>
                        <p><strong>Проблема:</strong> Ошибки базы данных SQLite.</p>
                        <p><strong>Решения:</strong></p>
                        <ul>
                            <li>Проверьте права доступа к файлам для директории базы данных</li>
                            <li>Убедитесь, что достаточно места на диске</li>
                            <li>Проверьте правильность пути к файлу базы данных</li>
                            <li>Проверьте на повреждение базы данных</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- AI Provider Issues -->
        <div class="content-section">
            <h2>Проблемы AI провайдеров</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-robot"></i>
                        </div>
                        <h3>Ошибки API Anthropic</h3>
                        <p><strong>Проблема:</strong> Ошибки от API Anthropic.</p>
                        <p><strong>Решения:</strong></p>
                        <ul>
                            <li>Проверьте, что API ключ действителен и имеет достаточно кредитов</li>
                            <li>Проверьте лимиты API</li>
                            <li>Убедитесь в правильности конфигурации API endpoint</li>
                            <li>Мониторьте использование API и квоты</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-brain"></i>
                        </div>
                        <h3>Ошибки API OpenAI</h3>
                        <p><strong>Проблема:</strong> Ошибки от API OpenAI.</p>
                        <p><strong>Решения:</strong></p>
                        <ul>
                            <li>Проверьте, что API ключ действителен</li>
                            <li>Проверьте лимиты API и квоты</li>
                            <li>Убедитесь в правильности конфигурации модели</li>
                            <li>Мониторьте использование API</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Testing and Debugging -->
        <div class="content-section">
            <h2>Тестирование и отладка</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-vial"></i>
                        </div>
                    <h3>Модульные тесты</h3>
                        <p><strong>Проблема:</strong> Тесты не проходят из-за зависимостей SmartRAG.</p>
                        <p><strong>Решение:</strong> Используйте мокирование для сервисов SmartRAG в модульных тестах:</p>
                        
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task TestDocumentUpload()
{
    // Arrange
    var mockDocumentService = new Mock<IDocumentService>();
    var mockSearchService = new Mock<IDocumentSearchService>();
    
    var controller = new DocumentsController(
        mockDocumentService.Object, 
        mockSearchService.Object, 
        Mock.Of<ILogger<DocumentsController>>());

    // Act & Assert
    // Ваша логика теста здесь
}</code></pre>
                        </div>
                    </div>
                    </div>

                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-cogs"></i>
                        </div>
                    <h3>Интеграционные тесты</h3>
                        <p><strong>Проблема:</strong> Интеграционные тесты не проходят.</p>
                        <p><strong>Решение:</strong> Используйте тестовую конфигурацию и убедитесь в правильности настройки:</p>
                        
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task TestEndToEndWorkflow()
{
    // Arrange
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.test.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();
    services.AddSmartRag(configuration);
    
    var serviceProvider = services.BuildServiceProvider();
    
    // Act & Assert
    // Ваша логика интеграционного теста здесь
}</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Common Error Messages -->
        <div class="content-section">
            <h2>Частые сообщения об ошибках</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-exclamation-triangle"></i>
                        </div>
                        <h3>Частые ошибки</h3>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"Document not found"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Проверьте правильность ID документа</li>
                                <li>Проверьте, был ли документ успешно загружен</li>
                                <li>Убедитесь, что документ не был удален</li>
                            </ul>
                        </div>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"Storage provider not configured"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Проверьте настройку <code>StorageProvider</code> в конфигурации</li>
                                <li>Убедитесь, что предоставлены все необходимые настройки хранилища</li>
                                <li>Проверьте регистрацию сервисов</li>
                            </ul>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-exclamation-circle"></i>
                        </div>
                        <h3>Другие ошибки</h3>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"AI provider not configured"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Проверьте настройку <code>AIProvider</code> в конфигурации</li>
                                <li>Убедитесь, что предоставлен API ключ для выбранного провайдера</li>
                                <li>Проверьте регистрацию сервисов</li>
                            </ul>
                    </div>

                        <div class="alert alert-warning" role="alert">
                            <strong>"Invalid file format"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Убедитесь, что файл в поддерживаемом формате</li>
                                <li>Проверьте расширение файла и содержимое</li>
                                <li>Проверьте, что файл не поврежден</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Getting Help -->
        <div class="content-section">
            <h2>Получение помощи</h2>
            
            <div class="row g-4">
                <div class="col-lg-8">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-question-circle"></i>
                        </div>
                        <h3>Все еще нужна помощь?</h3>
                        <p>Если у вас все еще есть проблемы, следуйте этим шагам:</p>
                        
                        <div class="row g-3">
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-file-alt"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Проверьте логи</h5>
                                        <p class="text-muted">Проверьте логи приложения для детальных сообщений об ошибках</p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-cog"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Проверьте конфигурацию</h5>
                                        <p class="text-muted">Проверьте все настройки конфигурации еще раз</p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-play"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Тестируйте с минимальной настройкой</h5>
                                        <p class="text-muted">Попробуйте сначала с простой конфигурацией</p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-book"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Проверьте документацию</h5>
                                        <p class="text-muted">Проверьте другие страницы документации для руководства</p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fab fa-github"></i>
                        </div>
                        <h3>Дополнительная поддержка</h3>
                        <p>Для дополнительной поддержки обратитесь к:</p>
                        
                        <div class="d-grid gap-3">
                            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-primary" target="_blank">
                                <i class="fab fa-github me-2"></i>
                                GitHub Репозиторий
                            </a>
                            <a href="https://github.com/byerlikaya/SmartRAG/issues" class="btn btn-outline-primary" target="_blank">
                                <i class="fas fa-bug me-2"></i>
                                Создать проблему
                            </a>
                            <a href="{{ site.baseurl }}/ru/getting-started" class="btn btn-outline-primary">
                                <i class="fas fa-rocket me-2"></i>
                                Начало работы
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>

    </div>
</div>
