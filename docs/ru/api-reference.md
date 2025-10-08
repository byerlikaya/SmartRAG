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
                    <!-- Updated for v2.3.1 -->
                    <p>SmartRAG предоставляет несколько основных интерфейсов для обработки и управления документами.</p>
                    
                    <h3>IDocumentSearchService</h3>
                    <p>Поиск документов и генерация ответов с помощью ИИ с использованием RAG (Retrieval-Augmented Generation).</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentSearchService
{
    Task&lt;List&lt;DocumentChunk&gt;&gt; SearchDocumentsAsync(string query, int maxResults = 5);
    Task&lt;RagResponse&gt; GenerateRagAnswerAsync(string query, int maxResults = 5, bool startNewConversation = false);
}</code></pre>
                    </div>

                    <h4>GenerateRagAnswerAsync</h4>
                    <p>Генерирует ответы с помощью ИИ с автоматическим управлением сессиями и историей разговоров.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">Task&lt;RagResponse&gt; GenerateRagAnswerAsync(string query, int maxResults = 5, bool startNewConversation = false)</code></pre>
                    </div>

                    <p><strong>Параметры:</strong></p>
                    <ul>
                        <li><code>query</code> (string): Вопрос пользователя</li>
                        <li><code>maxResults</code> (int): Максимальное количество фрагментов документов для получения (по умолчанию: 5)</li>
                        <li><code>startNewConversation</code> (bool): Начать новую сессию разговора (по умолчанию: false)</li>
                    </ul>
                    
                    <p><strong>Возвращает:</strong> <code>RagResponse</code> с ответом ИИ, источниками и метаданными</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Базовое использование
var response = await documentSearchService.GenerateRagAnswerAsync("Какая погода?");

// Начать новый разговор
var response = await documentSearchService.GenerateRagAnswerAsync("/new");</code></pre>
                    </div>

                    <h3>Другие ключевые интерфейсы</h3>
                    <p>Дополнительные сервисы для обработки и хранения документов.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Обработка и анализ документов
IDocumentParserService - Анализ документов и извлечение текста
IDocumentRepository - Операции хранения документов
IAIService - Коммуникация с провайдерами ИИ
IAudioParserService - Транскрипция аудио (Google Speech-to-Text)</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Models Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Ключевые модели</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Основные модели данных для операций SmartRAG.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Основная модель ответа
public class RagResponse
{
    public string Query { get; set; }
    public string Answer { get; set; }
    public List&lt;SearchSource&gt; Sources { get; set; }
    public DateTime SearchedAt { get; set; }
}

// Фрагмент документа для результатов поиска
public class DocumentChunk
{
    public string Id { get; set; }
    public string DocumentId { get; set; }
    public string Content { get; set; }
    public double RelevanceScore { get; set; }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Конфигурация</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Ключевые параметры конфигурации для SmartRAG.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Провайдеры ИИ
AIProvider.Anthropic    // Модели Claude
AIProvider.OpenAI       // GPT модели
AIProvider.Gemini       // Модели Google

// Провайдеры хранения  
StorageProvider.Qdrant  // Векторная база данных
StorageProvider.Redis   // Высокопроизводительный кэш
StorageProvider.Sqlite  // Локальная база данных</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Quick Start Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Быстрый старт</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Начните работу с SmartRAG за минуты.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// 1. Регистрация сервисов
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
});

// 2. Внедрение и использование
public class MyController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    public async Task<ActionResult> Ask(string question)
    {
        var response = await _searchService.GenerateRagAnswerAsync(question);
        return Ok(response);
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Common Patterns Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Общие паттерны</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Часто используемые паттерны и конфигурации.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Загрузка документа
        var document = await _documentService.UploadDocumentAsync(file);

// Поиск документов  
var results = await _searchService.SearchDocumentsAsync(query, 10);

// RAG разговор
var response = await _searchService.GenerateRagAnswerAsync(question);

// Конфигурация
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
});</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Error Handling Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Обработка ошибок</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Частые исключения и паттерны обработки ошибок.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">try
{
    var response = await _searchService.GenerateRagAnswerAsync(query);
    return Ok(response);
}
catch (SmartRagException ex)
{
    return BadRequest(ex.Message);
}
catch (Exception ex)
{
    return StatusCode(500, "Internal server error");
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Performance Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Советы по производительности</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Оптимизируйте производительность SmartRAG с этими советами.</p>
                    
                    <div class="alert alert-info">
                        <ul class="mb-0">
                            <li><strong>Размер чанков</strong>: 500-1000 символов для оптимального баланса</li>
                            <li><strong>Пакетные операции</strong>: Обрабатывайте несколько документов вместе</li>
                            <li><strong>Кэширование</strong>: Используйте Redis для лучшей производительности</li>
                            <li><strong>Векторное хранилище</strong>: Qdrant для производственного использования</li>
                    </ul>
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
                        <p class="mb-0">Если вам нужна помощь с API:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/ru/getting-started">Руководство по началу работы</a></li>
                            <li><a href="{{ site.baseurl }}/ru/configuration">Параметры конфигурации</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">Создать issue на GitHub</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Связаться с поддержкой по email</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>