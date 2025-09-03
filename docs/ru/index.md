---
layout: default
title: Документация SmartRAG
description: Корпоративная RAG-библиотека для .NET приложений
lang: ru
hide_title: true
---

<!-- Hero Section -->
<section class="hero-section">
    <div class="hero-background"></div>
    <div class="container">
        <div class="row align-items-center min-vh-100">
            <div class="col-lg-6">
                <div class="hero-content">
                    <div class="hero-badge">
                        <i class="fas fa-star"></i>
                        <span>Готово для предприятий</span>
                    </div>
                    <h1 class="hero-title">
                        Создавайте интеллектуальные приложения с 
                        <span class="text-gradient">SmartRAG</span>
                    </h1>
                    <p class="hero-description">
                        Самая мощная .NET библиотека для обработки документов, ИИ-эмбеддингов и семантического поиска. 
                        Преобразуйте ваши приложения с корпоративными возможностями RAG.
                    </p>
                    <div class="hero-stats">
                        <div class="stat-item">
                            <div class="stat-number">5+</div>
                            <div class="stat-label">ИИ-провайдеры</div>
                        </div>
                        <div class="stat-item">
                            <div class="stat-number">5+</div>
                            <div class="stat-label">Варианты хранения</div>
                        </div>
                        <div class="stat-item">
                            <div class="stat-number">100%</div>
                            <div class="stat-label">Открытый код</div>
                        </div>
                    </div>
                    <div class="hero-buttons">
                        <a href="{{ site.baseurl }}/ru/getting-started" class="btn btn-primary btn-lg">
                            <i class="fas fa-rocket"></i>
                            Начать
                        </a>
                        <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-light btn-lg" target="_blank">
                            <i class="fab fa-github"></i>
                            Посмотреть на GitHub
                        </a>
                    </div>
                </div>
            </div>
            <div class="col-lg-6">
                <div class="hero-visual">
                    <div class="code-window">
                        <div class="code-header">
                            <div class="code-dots">
                                <span></span>
                                <span></span>
                                <span></span>
                            </div>
                            <div class="code-title">SmartRAG.cs</div>
                        </div>
                        <div class="code-content">
                            <pre><code class="language-csharp">// Добавьте SmartRAG в ваш проект
services.UseSmartRag(configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.Gemini
);

// Загрузите и обработайте документ
var document = await documentService
    .UploadDocumentAsync(fileStream, fileName, contentType, "user123");

// Общайтесь с вашими документами с помощью ИИ
var answer = await documentSearchService
    .GenerateRagAnswerAsync("О чем этот документ?", maxResults: 5);</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Features Section -->
<section class="features-section">
    <div class="container">
        <div class="section-header text-center">
            <h2 class="section-title">Ключевые функции</h2>
            <p class="section-description">
                Мощные возможности для создания интеллектуальных приложений
            </p>
        </div>
        
        <div class="row g-4">
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-brain"></i>
                    </div>
                    <h3>ИИ-поддержка</h3>
                    <p>Интеграция с ведущими ИИ-провайдерами для мощных эмбеддингов и интеллектуальной обработки.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-file-alt"></i>
                    </div>
                    <h3>Поддержка множественных форматов</h3>
                    <p>Обрабатывайте документы Word, PDF, Excel и текстовые файлы с автоматическим определением формата.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-search"></i>
                    </div>
                    <h3>Расширенный семантический поиск</h3>
                    <p>Гибридная оценка (80% семантическая + 20% ключевые слова) с осведомленностью о контексте и интеллектуальным ранжированием.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-database"></i>
                    </div>
                    <h3>Гибкое хранение</h3>
                    <p>Множественные бэкенды хранения для гибких вариантов развертывания.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-rocket"></i>
                    </div>
                    <h3>Простая интеграция</h3>
                    <p>Простая настройка с внедрением зависимостей. Начните за считанные минуты.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-magic"></i>
                    </div>
                    <h3>Умное определение намерения</h3>
                    <p>Автоматически направляет запросы в чат или поиск документов на основе определения намерения.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="feature-card">
                    <div class="feature-icon">
                        <i class="fas fa-shield-alt"></i>
                    </div>
                    <h3>Готово к продакшену</h3>
                    <p>Создано для корпоративных сред с производительностью и надежностью.</p>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Providers Section -->
<section class="providers-section">
    <div class="container">
        <div class="section-header text-center">
            <h2 class="section-title">Поддерживаемые технологии</h2>
            <p class="section-description">
                Выберите из ведущих ИИ-провайдеров и решений для хранения
            </p>
        </div>
        
        <div class="providers-grid">
            <div class="provider-category">
                <h3>ИИ-провайдеры</h3>
                <div class="provider-cards">
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fab fa-google"></i>
                        </div>
                        <h4>Gemini</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-brain"></i>
                        </div>
                        <h4>OpenAI</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cloud"></i>
                        </div>
                        <h4>Azure OpenAI</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-robot"></i>
                        </div>
                        <h4>Anthropic</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cogs"></i>
                        </div>
                        <h4>Пользовательский</h4>
                    </div>
                </div>
            </div>
            
            <div class="provider-category">
                <h3>Провайдеры хранения</h3>
                <div class="provider-cards">
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-cube"></i>
                        </div>
                        <h4>Qdrant</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-database"></i>
                        </div>
                        <h4>Redis</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-hdd"></i>
                        </div>
                        <h4>SQLite</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-microchip"></i>
                        </div>
                        <h4>In-Memory</h4>
                    </div>
                    <div class="provider-card">
                        <div class="provider-logo">
                            <i class="fas fa-folder-open"></i>
                        </div>
                        <h4>Файловая система</h4>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Quick Start Section -->
<section class="quick-start-section">
    <div class="container">
        <div class="row align-items-center">
            <div class="col-lg-6">
                <div class="quick-start-content">
                    <h2>Начните за считанные минуты</h2>
                    <p>Простая и мощная интеграция для ваших .NET приложений.</p>
                    
                    <div class="steps">
                        <div class="step">
                            <div class="step-number">1</div>
                            <div class="step-content">
                                <h4>Установить пакет</h4>
                                <p>Добавьте SmartRAG через NuGet</p>
                            </div>
                        </div>
                        <div class="step">
                            <div class="step-number">2</div>
                            <div class="step-content">
                                <h4>Настроить сервисы</h4>
                                <p>Настройте ИИ и провайдеры хранения</p>
                            </div>
                        </div>
                        <div class="step">
                            <div class="step-number">3</div>
                            <div class="step-content">
                                <h4>Начать разработку</h4>
                                <p>Загружайте документы и выполняйте поиск</p>
                            </div>
                        </div>
                    </div>
                    
                    <a href="{{ site.baseurl }}/ru/getting-started" class="btn btn-primary btn-lg">
                        <i class="fas fa-play"></i>
                        Начать разработку
                    </a>
                </div>
            </div>
            <div class="col-lg-6">
                <div class="code-example">
                    <div class="code-tabs">
                        <button class="code-tab active" data-tab="install">Установить</button>
                        <button class="code-tab" data-tab="configure">Настроить</button>
                        <button class="code-tab" data-tab="use">Использовать</button>
                    </div>
                    <div class="code-content">
                        <div class="code-panel active" data-tab="install">
                            <pre><code class="language-bash">dotnet add package SmartRAG</code></pre>
                        </div>
                        <div class="code-panel" data-tab="configure">
                            <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});</code></pre>
                        </div>
                        <div class="code-panel" data-tab="use">
                            <pre><code class="language-csharp">var documentService = serviceProvider
    .GetRequiredService<IDocumentService>();

var results = await documentService
    .SearchAsync("your query");</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Documentation Section -->
<section class="documentation-section">
    <div class="container">
        <div class="section-header text-center">
            <h2 class="section-title">Документация</h2>
            <p class="section-description">
                Все что вам нужно для разработки с SmartRAG
            </p>
        </div>
        
        <div class="row g-4">
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/ru/getting-started" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-rocket"></i>
                    </div>
                    <h3>Начало работы</h3>
                    <p>Быстрое руководство по установке и настройке</p>
                </a>
            </div>
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/ru/configuration" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-cog"></i>
                    </div>
                    <h3>Конфигурация</h3>
                    <p>Подробные опции конфигурации</p>
                </a>
            </div>
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/ru/api-reference" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-code"></i>
                    </div>
                    <h3>Справочник API</h3>
                    <p>Полная документация API</p>
                </a>
            </div>
            <div class="col-lg-3 col-md-6">
                <a href="{{ site.baseurl }}/ru/examples" class="doc-card">
                    <div class="doc-icon">
                        <i class="fas fa-lightbulb"></i>
                    </div>
                    <h3>Примеры</h3>
                    <p>Реальные примеры и примеры приложений</p>
                </a>
            </div>
        </div>
    </div>
</section>

<!-- CTA Section -->
<section class="cta-section">
    <div class="container">
        <div class="cta-content text-center">
            <h2>Готовы создать что-то удивительное?</h2>
            <p>Присоединяйтесь к тысячам разработчиков, использующих SmartRAG для создания интеллектуальных приложений</p>
            <div class="cta-buttons">
                <a href="{{ site.baseurl }}/ru/getting-started" class="btn btn-primary btn-lg">
                    <i class="fas fa-rocket"></i>
                    Начать сейчас
                </a>
                <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-light btn-lg" target="_blank">
                    <i class="fab fa-github"></i>
                    Оценить на GitHub
                </a>
            </div>
        </div>
    </div>
</section>