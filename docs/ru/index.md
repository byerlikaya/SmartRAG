---
layout: default
title: Документация SmartRAG
description: Корпоративная библиотека RAG для .NET приложений
lang: ru
---

<div class="hero-section text-center py-5 mb-5">
    <div class="hero-content">
        <h1 class="hero-title display-4 fw-bold mb-4">
            <i class="fas fa-brain me-3"></i>
            SmartRAG
        </h1>
        <p class="hero-subtitle lead mb-4">
            Корпоративная библиотека RAG для .NET приложений
        </p>
        <p class="hero-description mb-5">
            Создавайте интеллектуальные приложения с продвинутой обработкой документов, AI-генерацией эмбеддингов и семантическим поиском.
        </p>
        <div class="hero-buttons">
            <a href="{{ site.baseurl }}/ru/getting-started" class="btn btn-primary btn-lg me-3">
                <i class="fas fa-rocket me-2"></i>Начать
            </a>
            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-lg me-3" target="_blank" rel="noopener noreferrer">
                <i class="fab fa-github me-2"></i>Посмотреть на GitHub
            </a>
            <a href="https://www.nuget.org/packages/SmartRAG" class="btn btn-outline-success btn-lg" target="_blank" rel="noopener noreferrer">
                <i class="fas fa-box me-2"></i>NuGet Пакет
            </a>
        </div>
    </div>
</div>

## 🚀 Что такое SmartRAG?

SmartRAG - это комплексная .NET библиотека, которая предоставляет интеллектуальную обработку документов, генерацию эмбеддингов и семантические возможности поиска. Она разработана для простоты использования, предлагая при этом мощные функции для создания AI-приложений.

<div class="row mt-5 mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-file-alt fa-3x text-primary"></i>
                </div>
                <h5 class="card-title">Поддержка Множественных Форматов</h5>
                <p class="card-text">Легко обрабатывайте документы Word, PDF, Excel и текстовые файлы. Наша библиотека автоматически обрабатывает все основные форматы документов.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-robot fa-3x text-success"></i>
                </div>
                <h5 class="card-title">Интеграция с AI Провайдерами</h5>
                <p class="card-text">Безупречная интеграция с OpenAI, Anthropic, Azure OpenAI, Gemini и пользовательскими AI провайдерами для мощной генерации эмбеддингов.</p>
            </div>
        </div>
    </div>
</div>

<div class="row mb-5">
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-database fa-3x text-warning"></i>
                </div>
                <h5 class="card-title">Векторное Хранилище</h5>
                <p class="card-text">Множественные бэкенды хранения включая Qdrant, Redis, SQLite, In-Memory, файловую систему и пользовательское хранилище для гибкого развертывания.</p>
            </div>
        </div>
    </div>
    <div class="col-md-6">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <div class="feature-icon mb-3">
                    <i class="fas fa-search fa-3x text-info"></i>
                </div>
                <h5 class="card-title">Семантический Поиск</h5>
                <p class="card-text">Продвинутые возможности поиска с оценкой сходства и интеллектуальным ранжированием результатов для лучшего пользовательского опыта.</p>
            </div>
        </div>
    </div>
</div>

## ⚡ Быстрый Старт

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

## 🚀 Поддерживаемые Технологии

SmartRAG интегрируется с ведущими AI провайдерами и решениями хранения, чтобы предоставить вам наилучший возможный опыт.

### 🤖 AI Провайдеры

<div class="row mt-4 mb-5">
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fab fa-google fa-3x text-warning"></i>
            </div>
            <h6 class="mb-1">Gemini</h6>
            <small class="text-muted">Google AI</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fab fa-openai fa-3x text-primary"></i>
            </div>
            <h6 class="mb-1">OpenAI</h6>
            <small class="text-muted">GPT Модели</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cloud fa-3x text-secondary"></i>
            </div>
            <h6 class="mb-1">Azure OpenAI</h6>
            <small class="text-muted">Корпоративный</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-robot fa-3x text-success"></i>
            </div>
            <h6 class="mb-1">Anthropic</h6>
            <small class="text-muted">Claude Модели</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cogs fa-3x text-dark"></i>
            </div>
            <h6 class="mb-1">Пользовательский</h6>
            <small class="text-muted">Расширяемый</small>
        </div>
    </div>
</div>

### 🗄️ Провайдеры Хранилищ

<div class="row mt-4 mb-5">
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cube fa-3x text-primary"></i>
            </div>
            <h6 class="mb-1">Qdrant</h6>
            <small class="text-muted">Векторная База Данных</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fab fa-redis fa-3x text-success"></i>
            </div>
            <h6 class="mb-1">Redis</h6>
            <small class="text-muted">In-Memory Кэш</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-hdd fa-3x text-info"></i>
            </div>
            <h6 class="mb-1">SQLite</h6>
            <small class="text-muted">Локальная База Данных</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-microchip fa-3x text-warning"></i>
            </div>
            <h6 class="mb-1">In-Memory</h6>
            <small class="text-muted">Быстрая Разработка</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-folder-open fa-3x text-secondary"></i>
            </div>
            <h6 class="mb-1">Файловая Система</h6>
            <small class="text-muted">Локальное Хранилище</small>
        </div>
    </div>
    <div class="col-md-2 mb-3">
        <div class="tech-logo-card text-center p-3">
            <div class="tech-logo mb-2">
                <i class="fas fa-cogs fa-3x text-dark"></i>
            </div>
            <h6 class="mb-1">Пользовательский</h6>
            <small class="text-muted">Расширяемое Хранилище</small>
        </div>
    </div>
</div>

## 📚 Документация

<div class="row mt-4">
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-rocket fa-2x text-primary mb-3"></i>
                <h5 class="card-title">Начало Работы</h5>
                <p class="card-text">Быстрое руководство по установке и настройке, чтобы начать работу.</p>
                <a href="{{ site.baseurl }}/ru/getting-started" class="btn btn-primary">Начать</a>
            </div>
        </div>
    </div>
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-cog fa-2x text-success mb-3"></i>
                <h5 class="card-title">Конфигурация</h5>
                <p class="card-text">Детальные опции конфигурации и лучшие практики.</p>
                <a href="{{ site.baseurl }}/ru/configuration" class="btn btn-success">Настроить</a>
            </div>
        </div>
    </div>
    <div class="col-md-4 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-code fa-2x text-warning mb-3"></i>
                <h5 class="card-title">API Справочник</h5>
                <p class="card-text">Полная документация API с примерами и паттернами использования.</p>
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
                <p class="card-text">Реальные примеры и примеры приложений для обучения.</p>
                <a href="{{ site.baseurl }}/ru/examples" class="btn btn-info">Посмотреть Примеры</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-tools fa-2x text-danger mb-3"></i>
                <h5 class="card-title">Устранение Неполадок</h5>
                <p class="card-text">Распространенные проблемы и решения, чтобы помочь решить проблемы.</p>
                <a href="{{ site.baseurl }}/ru/troubleshooting" class="btn btn-danger">Получить Помощь</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-history fa-2x text-secondary mb-3"></i>
                <h5 class="card-title">Журнал Изменений</h5>
                <p class="card-text">Отслеживайте новые функции, улучшения и исправления ошибок между версиями.</p>
                <a href="{{ site.baseurl }}/ru/changelog" class="btn btn-secondary">Посмотреть Изменения</a>
            </div>
        </div>
    </div>
    <div class="col-md-3 mb-3">
        <div class="card h-100 border-0 shadow-sm">
            <div class="card-body text-center p-4">
                <i class="fas fa-hands-helping fa-2x text-dark mb-3"></i>
                <h5 class="card-title">Вклад в Разработку</h5>
                <p class="card-text">Узнайте, как внести вклад в разработку SmartRAG.</p>
                <a href="{{ site.baseurl }}/ru/contributing" class="btn btn-dark">Внести Вклад</a>
            </div>
        </div>
    </div>
</div>

## 🌟 Почему SmartRAG?

<div class="alert alert-info">
    <h5><i class="fas fa-star me-2"></i>Готов для Корпоративного Использования</h5>
    <p class="mb-0">Разработан для производственных сред с фокусом на производительность, масштабируемость и надежность.</p>
</div>

<div class="alert alert-success">
    <h5><i class="fas fa-shield-alt me-2"></i>Протестирован в Производстве</h5>
    <p class="mb-0">Используется в реальных приложениях с проверенным трек-рекордом и активным обслуживанием.</p>
</div>

<div class="alert alert-warning">
    <h5><i class="fas fa-code me-2"></i>Открытый Исходный Код</h5>
    <p class="mb-0">Проект с открытым исходным кодом под лицензией MIT с прозрачной разработкой и регулярными обновлениями.</p>
</div>

## 📦 Установка

Установите SmartRAG через NuGet:

```bash
dotnet add package SmartRAG
```

Или используя Package Manager:

```bash
Install-Package SmartRAG
```

## 🤝 Вклад в Разработку

Мы приветствуем вклады! Подробности смотрите в нашем [Руководстве по Вкладу]({{ site.baseurl }}/ru/contributing).

## 📄 Лицензия

Этот проект лицензирован под MIT License - см. [LICENSE](https://github.com/byerlikaya/SmartRAG/blob/main/LICENSE) для деталей.

---

<div class="text-center mt-5">
    <p class="text-muted">
        <i class="fas fa-heart text-danger"></i> Создано с любовью Барышом Ерликайа
    </p>
</div>
