---
layout: default
title: Начало работы
description: Установите и настройте SmartRAG в вашем .NET приложении за несколько минут
lang: ru
---

<div class="page-content">
    <div class="container">
        <!-- Installation Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Установка</h2>
                    <p>SmartRAG доступен как пакет NuGet. Выберите предпочитаемый способ установки:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-bash">dotnet add package SmartRAG</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Конфигурация</h2>
                    <p>Настройте SmartRAG в вашем <code>Program.cs</code> или <code>Startup.cs</code>:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
using SmartRAG;

var builder = WebApplication.CreateBuilder(args);

// Добавить сервисы SmartRAG
builder.Services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});

var app = builder.Build();</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Quick Example Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Быстрый пример</h2>
                    <p>Вот простой пример для начала:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Внедрить сервис документов
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    
    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }
    
    [HttpPost("upload")]
    public async Task&lt;IActionResult&gt; UploadDocument(IFormFile file)
    {
        var document = await _documentService.UploadDocumentAsync(file);
        return Ok(document);
    }
    
    [HttpPost("search")]
    public async Task&lt;IActionResult&gt; Search([FromBody] string query)
    {
        var results = await _documentService.SearchAsync(query);
        return Ok(results);
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Next Steps Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Следующие шаги</h2>
                    <p>Теперь, когда вы установили и настроили SmartRAG, изучите эти функции:</p>
                    
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="feature-card">
                                <div class="feature-icon">
                                    <i class="fas fa-cog"></i>
                                </div>
                                <h3>Конфигурация</h3>
                                <p>Узнайте о расширенных опциях конфигурации и лучших практиках.</p>
                                <a href="{{ site.baseurl }}/ru/configuration" class="btn btn-outline-primary btn-sm">Настроить</a>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="feature-card">
                                <div class="feature-icon">
                                    <i class="fas fa-code"></i>
                                </div>
                                <h3>Справочник API</h3>
                                <p>Изучите полную документацию API с примерами.</p>
                                <a href="{{ site.baseurl }}/ru/api-reference" class="btn btn-outline-primary btn-sm">Посмотреть API</a>
                            </div>
                        </div>
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
                        <p class="mb-0">Если вы столкнулись с проблемами или нуждаетесь в поддержке:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">Откройте issue на GitHub</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Свяжитесь с поддержкой по электронной почте</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>