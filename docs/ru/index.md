---
layout: default
title: Документация SmartRAG
description: Корпоративная RAG-библиотека для .NET приложений
lang: ru
---

<div class="hero-section text-center py-5 mb-5">
    <div class="hero-content">
        <div class="hero-icon mb-4">
            <i class="fas fa-brain fa-4x text-primary"></i>
        </div>
        <p class="hero-description lead mb-5">
            Создавайте интеллектуальные приложения с продвинутой обработкой документов, ИИ-эмбеддингами и семантическим поиском.
        </p>
        <div class="hero-buttons">
            <a href="{{ site.baseurl }}/ru/getting-started" class="btn btn-primary btn-lg me-3">
                <i class="fas fa-rocket me-2"></i>Начать
            </a>
            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-lg me-3" target="_blank" rel="noopener noreferrer">
                <i class="fab fa-github me-2"></i>Посмотреть на GitHub
            </a>
            <a href="https://www.nuget.org/packages/SmartRAG" class="btn btn-outline-success btn-lg" target="_blank" rel="noopener noreferrer">
                <i class="fas fa-box me-2"></i>NuGet пакет
            </a>
        </div>
    </div>
</div>

## 🚀 Что такое SmartRAG?

SmartRAG — это комплексная .NET библиотека, которая предоставляет интеллектуальную обработку документов, генерацию эмбеддингов и семантический поиск. Она разработана для простоты использования, предлагая мощные функции для создания ИИ-приложений.

<div class="row mt-5 mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body p-4">
                <h5 class="card-title">
                    <div class="feature-icon">
                        <i class="fas fa-file-alt text-primary"></i>
                    </div>
                    Поддержка множественных форматов
                </h5>
                <p class="card-text">Легко обрабатывайте документы Word, PDF, Excel и текстовые файлы. Наша библиотека автоматически обрабатывает все основные форматы документов.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body p-4">
                <h5 class="card-title">
                    <div class="feature-icon">
                        <i class="fas fa-robot text-success"></i>
                    </div>
                    Интеграция с ИИ-провайдерами
                </h5>
                <p class="card-text">Бесшовная интеграция с OpenAI, Anthropic, Azure OpenAI, Gemini и пользовательскими ИИ-провайдерами для мощной генерации эмбеддингов.</p>
            </div>
        </div>
    </div>
</div>

<div class="row mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body p-4">
                <h5 class="card-title">
                    <div class="feature-icon">
                        <i class="fas fa-database text-warning"></i>
                    </div>
                    Векторное хранилище
                </h5>
                <p class="card-text">Множественные бэкенды хранения, включая Qdrant, Redis, SQLite, In-Memory и файловую систему для гибкого развертывания.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body p-4">
                <h5 class="card-title">
                    <div class="feature-icon">
                        <i class="fas fa-search text-info"></i>
                    </div>
                    Семантический поиск
                </h5>
                <p class="card-text">Продвинутые возможности поиска с оценкой схожести и интеллектуальным ранжированием результатов для лучшего пользовательского опыта.</p>
            </div>
        </div>
    </div>
</div>

## 🌟 Почему SmartRAG?

<div class="alert alert-info">
    <h5><i class="fas fa-star me-2"></i>Готово для предприятий</h5>
    <p class="mb-0">Создано для производственных сред с учетом производительности, масштабируемости и надежности.</p>
</div>

<div class="alert alert-success">
    <h5><i class="fas fa-shield-alt me-2"></i>Протестировано в продакшене</h5>
    <p class="mb-0">Используется в реальных приложениях с проверенной репутацией и активной поддержкой.</p>
</div>

<div class="alert alert-warning">
    <h5><i class="fas fa-code me-2"></i>Открытый исходный код</h5>
    <p class="mb-0">Проект с открытым исходным кодом под лицензией MIT с прозрачной разработкой и регулярными обновлениями.</p>
</div>

## ⚡ Быстрый старт

Начните работу за считанные минуты с нашим простым процессом настройки:

```csharp
// Добавьте SmartRAG в ваш проект
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});

// Используйте сервис документов
var documentService = serviceProvider.GetRequiredService<IDocumentService>();
var document = await documentService.UploadDocumentAsync(file);
```

## 🚀 Поддерживаемые технологии

SmartRAG интегрируется с ведущими ИИ-провайдерами и решениями для хранения, чтобы предоставить вам наилучший опыт.

### 🤖 ИИ-провайдеры

<div class="row mt-4 mb-5">
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fab fa-google"></i>
            </div>
            <h6>Gemini</h6>
            <small>Google AI</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-brain"></i>
            </div>
            <h6>OpenAI</h6>
            <small>GPT модели</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-cloud"></i>
            </div>
            <h6>Azure OpenAI</h6>
            <small>Корпоративный</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-robot"></i>
            </div>
            <h6>Anthropic</h6>
            <small>Claude модели</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-cogs"></i>
            </div>
            <h6>Пользовательский</h6>
            <small>Расширяемый</small>
        </div>
    </div>
</div>

### 🗄️ Провайдеры хранения

<div class="row mt-4 mb-5">
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-cube"></i>
            </div>
            <h6>Qdrant</h6>
            <small>Векторная база данных</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-database"></i>
            </div>
            <h6>Redis</h6>
            <small>Кэш в памяти</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-hdd"></i>
            </div>
            <h6>SQLite</h6>
            <small>Локальная база данных</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-microchip"></i>
            </div>
            <h6>In-Memory</h6>
            <small>Быстрая разработка</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="provider-card text-center p-4">
            <div class="provider-icon">
                <i class="fas fa-folder-open"></i>
            </div>
            <h6>Файловая система</h6>
            <small>Локальное хранилище</small>
        </div>
    </div>
</div>

## 📚 Документация

<div class="row mt-4">
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-rocket fa-2x text-primary mb-3"></i>
                <h5 class="card-title">Начало работы</h5>
                <p class="card-text">Быстрое руководство по установке и настройке для начала работы.</p>
                <a href="{{ site.baseurl }}/ru/getting-started" class="btn btn-primary">Начать</a>
            </div>
        </div>
    </div>
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-cog fa-2x text-success mb-3"></i>
                <h5 class="card-title">Конфигурация</h5>
                <p class="card-text">Подробные опции конфигурации и лучшие практики.</p>
                <a href="{{ site.baseurl }}/ru/configuration" class="btn btn-success">Настроить</a>
            </div>
        </div>
    </div>
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-code fa-2x text-warning mb-3"></i>
                <h5 class="card-title">Справочник API</h5>
                <p class="card-text">Полная документация API с примерами и шаблонами использования.</p>
                <a href="{{ site.baseurl }}/ru/api-reference" class="btn btn-warning">Посмотреть API</a>
            </div>
        </div>
    </div>
</div>

<div class="row mt-4">
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-lightbulb fa-2x text-info mb-3"></i>
                <h5 class="card-title">Примеры</h5>
                <p class="card-text">Реальные примеры и примеры приложений для изучения.</p>
                <a href="{{ site.baseurl }}/ru/examples" class="btn btn-info">Посмотреть примеры</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-tools fa-2x text-danger mb-3"></i>
                <h5 class="card-title">Устранение неполадок</h5>
                <p class="card-text">Распространенные проблемы и решения для решения проблем.</p>
                <a href="{{ site.baseurl }}/ru/troubleshooting" class="btn btn-danger">Получить помощь</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-history fa-2x text-secondary mb-3"></i>
                <h5 class="card-title">Журнал изменений</h5>
                <p class="card-text">Отслеживайте новые функции, улучшения и исправления ошибок в версиях.</p>
                <a href="{{ site.baseurl }}/ru/changelog" class="btn btn-secondary">Посмотреть изменения</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-hands-helping fa-2x text-dark mb-3"></i>
                <h5 class="card-title">Вклад в проект</h5>
                <p class="card-text">Узнайте, как внести вклад в разработку SmartRAG.</p>
                <a href="{{ site.baseurl }}/ru/contributing" class="btn btn-dark">Внести вклад</a>
            </div>
        </div>
    </div>
</div>

---

<div class="text-center mt-5">
    <p class="text-muted">
        <i class="fas fa-heart text-danger"></i> Создано с любовью Барьш Йерликая
    </p>
</div>