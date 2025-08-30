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
                        <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
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
                                    <td><code>ApiKey</code></td>
                                    <td><code>string</code></td>
                                    <td>Обязательно</td>
                                    <td>Ваш API-ключ для ИИ-провайдера</td>
                                </tr>
                                <tr>
                                    <td><code>ModelName</code></td>
                                    <td><code>string</code></td>
                                    <td>По умолчанию провайдера</td>
                                    <td>Конкретная модель для использования</td>
                                </tr>
                                <tr>
                                    <td><code>ChunkSize</code></td>
                                    <td><code>int</code></td>
                                    <td>1000</td>
                                    <td>Размер фрагментов документа</td>
                                </tr>
                                <tr>
                                    <td><code>ChunkOverlap</code></td>
                                    <td><code>int</code></td>
                                    <td>200</td>
                                    <td>Перекрытие между фрагментами</td>
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
                        <div class="code-tab" data-tab="azure">Azure OpenAI</div>
                        <div class="code-tab" data-tab="gemini">Gemini</div>
                        <div class="code-tab" data-tab="custom">Пользовательский</div>
                    </div>
                    
                    <div class="code-content">
                        <div class="code-panel active" id="anthropic">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.ApiKey = "your-anthropic-key";
    options.ModelName = "claude-3-sonnet-20240229";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="openai">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.ApiKey = "your-openai-key";
    options.ModelName = "text-embedding-ada-002";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="azure">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.AzureOpenAI;
    options.ApiKey = "your-azure-key";
    options.Endpoint = "https://your-resource.openai.azure.com/";
    options.ModelName = "text-embedding-ada-002";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="gemini">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Gemini;
    options.ApiKey = "your-gemini-key";
    options.ModelName = "embedding-001";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="custom">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Custom;
    options.CustomEndpoint = "https://your-custom-api.com/v1/embeddings";
    options.ApiKey = "your-custom-key";
    options.ModelName = "your-custom-model";
});</code></pre>
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
                        <div class="code-tab" data-tab="redis">Redis</div>
                        <div class="code-tab" data-tab="sqlite">SQLite</div>
                        <div class="code-tab" data-tab="memory">In-Memory</div>
                        <div class="code-tab" data-tab="filesystem">Файловая система</div>
                    </div>
                    
                    <div class="code-content">
                        <div class="code-panel active" id="qdrant">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Qdrant;
    options.QdrantUrl = "http://localhost:6333";
    options.CollectionName = "smartrag_documents";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="redis">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Redis;
    options.RedisConnectionString = "localhost:6379";
    options.DatabaseId = 0;
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="sqlite">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Sqlite;
    options.ConnectionString = "Data Source=smartrag.db";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="memory">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.InMemory;
    // Дополнительная конфигурация не требуется
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="code-panel" id="filesystem">
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.FileSystem;
    options.StoragePath = "./data/smartrag";
});</code></pre>
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
                        <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.ChunkSize = 500;
    options.ChunkOverlap = 100;
    options.ChunkingStrategy = ChunkingStrategy.Sentence;
});</code></pre>
                    </div>
                    
                    <h3>Обработка документов</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.SupportedFormats = new[] { ".pdf", ".docx", ".txt" };
    options.MaxFileSize = 10 * 1024 * 1024; // 10MB
    options.EnableTextExtraction = true;
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
    "ApiKey": "your-api-key",
    "ChunkSize": 1000,
    "ChunkOverlap": 200
  }
}</code></pre>
                    </div>
                    
                    <h3>Переменные окружения</h3>
                    <div class="code-example">
                        <pre><code class="language-bash">export SMARTRAG_AI_PROVIDER=Anthropic
export SMARTRAG_STORAGE_PROVIDER=Qdrant
export SMARTRAG_API_KEY=your-api-key</code></pre>
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