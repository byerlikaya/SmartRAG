---
layout: default
title: Конфигурация
description: Настройте SmartRAG с предпочитаемыми AI и хранилищем
lang: ru
---

<div class="page-content">
    <div class="container">
        <!-- Basic Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Базовая конфигурация</h2>
                    <p>SmartRAG можно настроить с различными опциями в соответствии с вашими потребностями:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});</code></pre>
                    </div>

                    <h3>Опции конфигурации</h3>
                    <div class="table-responsive">
                        <table class="table table-striped">
                            <thead>
                                <tr>
                                    <th>Опция</th>
                                    <th>Тип</th>
                                    <th>По умолчанию</th>
                                    <th>Описание</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td><code>AIProvider</code></td>
                                    <td><code>AIProvider</code></td>
                                    <td><code>Anthropic</code></td>
                                    <td>ИИ-провайдер для эмбеддингов</td>
                                </tr>
                                <tr>
                                    <td><code>StorageProvider</code></td>
                                    <td><code>StorageProvider</code></td>
                                    <td><code>Qdrant</code></td>
                                    <td>Провайдер хранения для векторов</td>
                                </tr>
                                <tr>
                                    <td><code>MaxChunkSize</code></td>
                                    <td><code>int</code></td>
                                    <td>1000</td>
                                    <td>Максимальный размер фрагментов документа</td>
                                </tr>
                                <tr>
                                    <td><code>MinChunkSize</code></td>
                                    <td><code>int</code></td>
                                    <td>50</td>
                                    <td>Минимальный размер фрагментов документа</td>
                                </tr>
                                <tr>
                                    <td><code>ChunkOverlap</code></td>
                                    <td><code>int</code></td>
                                    <td>200</td>
                                    <td>Перекрытие между фрагментами</td>
                                </tr>
                                <tr>
                                    <td><code>MaxRetryAttempts</code></td>
                                    <td><code>int</code></td>
                                    <td>3</td>
                                    <td>Максимальное количество попыток повтора</td>
                                </tr>
                                <tr>
                                    <td><code>RetryDelayMs</code></td>
                                    <td><code>int</code></td>
                                    <td>1000</td>
                                    <td>Задержка между попытками повтора (мс)</td>
                                </tr>
                                <tr>
                                    <td><code>RetryPolicy</code></td>
                                    <td><code>RetryPolicy</code></td>
                                    <td><code>ExponentialBackoff</code></td>
                                    <td>Политика повтора</td>
                                </tr>
                                <tr>
                                    <td><code>EnableFallbackProviders</code></td>
                                    <td><code>bool</code></td>
                                    <td>false</td>
                                    <td>Включить резервные провайдеры</td>
                                </tr>
                                <tr>
                                    <td><code>FallbackProviders</code></td>
                                    <td><code>AIProvider[]</code></td>
                                    <td>[]</td>
                                    <td>Список резервных ИИ-провайдеров</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </section>

        <!-- AI Providers Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Конфигурация ИИ-провайдеров</h2>
                    <p>Выберите из нескольких ИИ-провайдеров для генерации эмбеддингов:</p>
                    
                    <div class="code-tabs">
                        <div class="code-tab active" data-tab="anthropic">Anthropic</div>
                        <div class="code-tab" data-tab="openai">OpenAI</div>
                        <div class="code-tab" data-tab="gemini">Gemini</div>
                    </div>
                    
                    <div class="code-content">
                        <div class="code-panel active" id="anthropic">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Anthropic": {
    "ApiKey": "your-anthropic-key"
  }
}</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="openai">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "OpenAI": {
    "ApiKey": "your-openai-key"
  }
}</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="gemini">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Gemini;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Gemini": {
    "ApiKey": "your-gemini-key"
  }
}</code></pre>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- Storage Providers Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Конфигурация провайдеров хранения</h2>
                    <p>Выберите бэкенд хранения, который лучше всего подходит для ваших потребностей:</p>
                    
                    <div class="code-tabs">
                        <div class="code-tab active" data-tab="qdrant">Qdrant</div>
                        <div class="code-tab" data-tab="memory">In-Memory</div>
                    </div>
                    
                    <div class="code-content">
                        <div class="code-panel active" id="qdrant">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Qdrant": {
    "ApiKey": "your-qdrant-key"
  }
}</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="memory">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.InMemory;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});
// Дополнительная конфигурация не требуется</code></pre>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- Advanced Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Расширенная конфигурация</h2>
                    <p>Настройте SmartRAG для ваших конкретных требований:</p>
                    
                    <h3>Пользовательское разбиение на фрагменты</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.MaxChunkSize = 500;
    options.MinChunkSize = 50;
    options.ChunkOverlap = 100;
});</code></pre>
                    </div>
                    
                    <h3>Конфигурация повторов</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
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

        <!-- Environment Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Конфигурация окружения</h2>
                    <p>Настройте SmartRAG с помощью переменных окружения или файлов конфигурации:</p>
                    
                    <h3>appsettings.json</h3>
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
    "EnableFallbackProviders": false
  },
  "Anthropic": {
    "ApiKey": "your-anthropic-key"
  },
  "Qdrant": {
    "ApiKey": "your-qdrant-key"
  }
}</code></pre>
                    </div>
                    
                    <h3>Переменные окружения</h3>
                    <div class="code-example">
                        <pre><code class="language-bash">export SmartRAG__AIProvider=Anthropic
export SmartRAG__StorageProvider=Qdrant
export SmartRAG__MaxChunkSize=1000
export SmartRAG__ChunkOverlap=200
export ANTHROPIC_API_KEY=your-anthropic-key
export QDRANT_API_KEY=your-qdrant-key</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Best Practices Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Лучшие практики</h2>
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="alert alert-warning">
                                <h4><i class="fas fa-key me-2"></i>API-ключи</h4>
                                <p class="mb-0">Никогда не храните API-ключи в исходном коде. Используйте переменные окружения или безопасную конфигурацию.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-balance-scale me-2"></i>Размер фрагментов</h4>
                                <p class="mb-0">Балансируйте между контекстом и производительностью. Меньшие фрагменты для точности, большие для контекста.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-database me-2"></i>Хранение</h4>
                                <p class="mb-0">Выбирайте провайдера хранения на основе вашего масштаба и требований.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-primary">
                                <h4><i class="fas fa-shield-alt me-2"></i>Безопасность</h4>
                                <p class="mb-0">Используйте соответствующие средства контроля доступа и мониторинга для производственных сред.</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>