---
layout: default
title: Примеры
description: Практические примеры и примеры кода для интеграции SmartRAG
lang: ru
---

<div class="page-content">
    <div class="container">
        <!-- Basic Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Быстрые примеры</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Начните работу с SmartRAG за минуты.</p>
                    
                    <h3>Базовое использование</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// 1. Загрузить документ
var document = await _documentService.UploadDocumentAsync(file);

// 2. Поиск документов
var results = await _searchService.SearchDocumentsAsync(query, 10);

// 3. Генерация RAG ответа с историей разговора
var response = await _searchService.GenerateRagAnswerAsync(question);</code></pre>
                    </div>

                    <h3>Пример контроллера</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    [HttpPost("search")]
    public async Task<ActionResult> Search([FromBody] SearchRequest request)
    {
        var response = await _searchService.GenerateRagAnswerAsync(
            request.Query, request.MaxResults);
        return Ok(response);
    }
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 5;
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Advanced Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Расширенное использование</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Расширенные примеры для производственного использования.</p>
                    
                    <h3>Пакетная обработка</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Загрузить несколько документов
var documents = await _documentService.UploadDocumentsAsync(files);

// Получить статистику хранилища
var stats = await _documentService.GetStorageStatisticsAsync();

// Управление документами
var allDocs = await _documentService.GetAllDocumentsAsync();
await _documentService.DeleteDocumentAsync(documentId);</code></pre>
                    </div>

                    <h3>Операции обслуживания</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Регенерация эмбеддингов
await _documentService.RegenerateAllEmbeddingsAsync();

// Очистка данных
await _documentService.ClearAllEmbeddingsAsync();
await _documentService.ClearAllDocumentsAsync();</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Конфигурация</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Настройте SmartRAG под ваши потребности.</p>
                    
                    <h3>Регистрация сервисов</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});</code></pre>
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
                        <p class="mb-0">Если вам нужна помощь с примерами:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/ru/getting-started">Руководство по началу работы</a></li>
                            <li><a href="{{ site.baseurl }}/ru/api-reference">Справочник API</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">Создать issue на GitHub</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Связаться с поддержкой по email</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>