---
layout: default
title: Участие в проекте
description: Узнайте, как внести свой вклад в проект SmartRAG
lang: ru
---

<div class="page-header">
    <div class="container">
        <div class="row">
            <div class="col-lg-8 mx-auto text-center">
                <h1 class="page-title">Участие в проекте</h1>
                <p class="page-description">
                    Узнайте, как внести свой вклад в проект SmartRAG
                </p>
            </div>
        </div>
    </div>
</div>

<div class="page-content">
    <div class="container">
        <!-- How to Contribute Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Как внести вклад</h2>
                    <p>SmartRAG - это проект с открытым исходным кодом, который приветствует вклад сообщества. Вот различные способы внести свой вклад в проект:</p>
                    
                    <h3>🐛 Сообщение об ошибках</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-bug me-2"></i>Нашли ошибку?</h4>
                        <p class="mb-0">Пожалуйста, создайте подробный отчет об ошибке в GitHub Issues.</p>
                    </div>
                    
                    <h3>✨ Запрос функций</h3>
                    <div class="alert alert-success">
                        <h4><i class="fas fa-lightbulb me-2"></i>Есть новые идеи?</h4>
                        <p class="mb-0">Поделитесь своими запросами функций в GitHub Discussions.</p>
                    </div>
                    
                    <h3>📝 Документация</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-book me-2"></i>Улучшите документацию</h4>
                        <p class="mb-0">Исправьте отсутствующую или неверную информацию, добавьте новые примеры.</p>
                    </div>
                    
                    <h3>💻 Вклад в код</h3>
                    <div class="alert alert-primary">
                        <h4><i class="fas fa-code me-2"></i>Напишите код</h4>
                        <p class="mb-0">Добавьте новые функции, исправьте ошибки, улучшите производительность.</p>
                    </div>
                </div>
            </div>
        </section>

        <!-- Prerequisites Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Предварительные требования</h2>
                    <p>Инструменты, которые вам понадобятся перед внесением вклада в SmartRAG:</p>
                    
                    <h3>Необходимые инструменты</h3>
                    <ul>
                        <li><strong>.NET 8.0 SDK</strong> или выше</li>
                        <li><strong>Git</strong> для контроля версий</li>
                        <li><strong>Visual Studio 2022</strong> или <strong>VS Code</strong></li>
                        <li><strong>Docker</strong> (для тестов Qdrant, Redis)</li>
                    </ul>
                    
                    <h3>Требования к аккаунту</h3>
                    <ul>
                        <li><strong>Аккаунт GitHub</strong></li>
                        <li><strong>Аккаунт NuGet</strong> (для публикации пакетов)</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Development Workflow Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Рабочий процесс разработки</h2>
                    <p>Шаги, которые необходимо выполнить для внесения вклада в SmartRAG:</p>
                    
                    <h3>1. Форк репозитория</h3>
                    <div class="code-example">
                        <pre><code class="language-bash"># Форкните репозиторий на GitHub
# Затем клонируйте его в своем аккаунте
git clone https://github.com/VASH_POLZOVATEL/SmartRAG.git
cd SmartRAG</code></pre>
                    </div>
                    
                    <h3>2. Добавьте upstream remote</h3>
                    <div class="code-example">
                        <pre><code class="language-bash">git remote add upstream https://github.com/byerlikaya/SmartRAG.git
git fetch upstream</code></pre>
                    </div>
                    
                    <h3>3. Создайте новую ветку</h3>
                    <div class="code-example">
                        <pre><code class="language-bash"># Создайте feature-ветку
git checkout -b feature/vasha-funktsiya

# Создайте bug-fix ветку
git checkout -b fix/nomer-issue-opisanie</code></pre>
                    </div>
                    
                    <h3>4. Внесите свои изменения</h3>
                    <p>Напишите код, протестируйте его и зафиксируйте:</p>
                    <div class="code-example">
                        <pre><code class="language-bash"># Добавьте изменения в индекс
git add .

# Зафиксируйте
git commit -m "feat: добавить описание новой функции"

# Отправьте
git push origin feature/vasha-funktsiya</code></pre>
                    </div>
                    
                    <h3>5. Создайте Pull Request</h3>
                    <p>Создайте Pull Request на GitHub и опишите свои изменения.</p>
                </div>
            </div>
        </section>

        <!-- Code Style Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Стиль кода</h2>
                    <p>Проект SmartRAG следует определенным стандартам кода:</p>
                    
                    <h3>Стандарты C# кода</h3>
                    <ul>
                        <li><strong>PascalCase</strong>: Имена классов, методов и свойств</li>
                        <li><strong>camelCase</strong>: Имена локальных переменных и параметров</li>
                        <li><strong>UPPER_CASE</strong>: Значения констант</li>
                        <li><strong>Async/Await</strong>: Для асинхронных операций</li>
                    </ul>
                    
                    <h3>Организация файлов</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Using-директивы в начале файла
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// Пространство имен
namespace SmartRAG.Services
{
    // Определение класса
    public class ExampleService : IExampleService
    {
        // Поля
        private readonly ILogger<ExampleService> _logger;
        
        // Конструктор
        public ExampleService(ILogger<ExampleService> logger)
        {
            _logger = logger;
        }
        
        // Публичные методы
        public async Task<string> DoSomethingAsync()
        {
            // Реализация
        }
        
        // Приватные методы
        private void HelperMethod()
        {
            // Реализация
        }
    }
}</code></pre>
                    </div>
                    
                    <h3>XML-документация</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">/// <summary>
/// Загружает и обрабатывает документ
/// </summary>
/// <param name="file">Файл для загрузки</param>
/// <returns>Обработанный документ</returns>
/// <exception cref="ArgumentException">Неверный формат файла</exception>
public async Task<Document> UploadDocumentAsync(IFormFile file)
{
    // Реализация
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
                    <p>Все вклады должны быть протестированы:</p>
                    
                    <h3>Модульные тесты</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task UploadDocument_ValidFile_ReturnsDocument()
{
    // Arrange
    var mockFile = new Mock<IFormFile>();
    var service = new DocumentService(mockLogger.Object);
    
    // Act
    var result = await service.UploadDocumentAsync(mockFile.Object);
    
    // Assert
    Assert.IsNotNull(result);
    Assert.IsNotEmpty(result.Id);
}</code></pre>
                    </div>
                    
                    <h3>Запуск тестов</h3>
                    <div class="code-example">
                        <pre><code class="language-bash"># Запустить все тесты
dotnet test

# Тестировать конкретный проект
dotnet test tests/SmartRAG.Tests/

# Получить отчет о покрытии
dotnet test --collect:"XPlat Code Coverage"</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Documentation Standards Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Стандарты документации</h2>
                    <p>Стандарты для вкладов в документацию:</p>
                    
                    <h3>Формат Markdown</h3>
                    <ul>
                        <li><strong>Заголовки</strong>: Используйте иерархическую структуру (H1, H2, H3)</li>
                        <li><strong>Блоки кода</strong>: Указывайте язык (```csharp, ```bash)</li>
                        <li><strong>Ссылки</strong>: Используйте описательные тексты ссылок</li>
                        <li><strong>Списки</strong>: Используйте последовательный формат</li>
                    </ul>
                    
                    <h3>Многоязычная поддержка</h3>
                    <p>Вся документация должна быть доступна на 4 языках:</p>
                    <ul>
                        <li><strong>Английский</strong> (en) - Основной язык</li>
                        <li><strong>Турецкий</strong> (tr) - Локальный язык</li>
                        <li><strong>Немецкий</strong> (de) - Международный</li>
                        <li><strong>Русский</strong> (ru) - Международный</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Issue Reporting Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Сообщение об ошибках</h2>
                    <p>При сообщении об ошибках, пожалуйста, включите следующую информацию:</p>
                    
                    <h3>Необходимая информация</h3>
                    <ul>
                        <li><strong>Версия SmartRAG</strong>: Какую версию вы используете?</li>
                        <li><strong>Версия .NET</strong>: .NET 8.0, 9.0?</li>
                        <li><strong>Операционная система</strong>: Windows, Linux, macOS?</li>
                        <li><strong>Сообщение об ошибке</strong>: Полное сообщение об ошибке и стек-трейс</li>
                        <li><strong>Шаги</strong>: Шаги для воспроизведения ошибки</li>
                        <li><strong>Ожидаемое поведение</strong>: Что вы ожидали?</li>
                        <li><strong>Фактическое поведение</strong>: Что произошло?</li>
                    </ul>
                    
                    <h3>Шаблон ошибки</h3>
                    <div class="code-example">
                        <pre><code class="language-markdown">## Описание ошибки
Краткое и ясное описание ошибки

## Шаги воспроизведения
1. Перейдите к '...'
2. Нажмите на '...'
3. Появляется ошибка '...'

## Ожидаемое поведение
Что вы ожидали

## Фактическое поведение
Что произошло

## Скриншоты
Добавьте скриншоты, если есть

## Информация об окружении
- Версия SmartRAG: 1.1.0
- Версия .NET: 8.0
- Операционная система: Windows 11
- Браузер: Chrome 120</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- PR Process Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Процесс Pull Request</h2>
                    <p>Моменты, на которые следует обратить внимание при создании Pull Request:</p>
                    
                    <h3>Шаблон PR</h3>
                    <div class="code-example">
                        <pre><code class="language-markdown">## Описание изменений
Что делает этот PR?

## Тип изменений
- [ ] Исправление ошибки
- [ ] Новая функция
- [ ] Критическое изменение
- [ ] Обновление документации

## Протестировано?
- [ ] Модульные тесты проходят
- [ ] Интеграционные тесты проходят
- [ ] Выполнено ручное тестирование

## Контрольный список
- [ ] Соответствует стандартам кода
- [ ] Документация обновлена
- [ ] Добавлены тесты
- [ ] Нет критических изменений</code></pre>
                    </div>
                    
                    <h3>Процесс ревью</h3>
                    <ul>
                        <li><strong>Автоматические тесты</strong>: CI/CD pipeline должен пройти</li>
                        <li><strong>Ревью кода</strong>: Требуется минимум 1 одобрение</li>
                        <li><strong>Документация</strong>: Должна быть обновлена при необходимости</li>
                        <li><strong>Критические изменения</strong>: Требуют особого внимания</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Release Process Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Процесс релиза</h2>
                    <p>Как публикуются версии SmartRAG:</p>
                    
                    <h3>Типы версий</h3>
                    <ul>
                        <li><strong>Patch (1.0.1)</strong>: Исправления ошибок</li>
                        <li><strong>Minor (1.1.0)</strong>: Новые функции</li>
                        <li><strong>Major (2.0.0)</strong>: Критические изменения</li>
                    </ul>
                    
                    <h3>Шаги релиза</h3>
                    <ol>
                        <li><strong>Обновление Changelog</strong>: Перечислить все изменения</li>
                        <li><strong>Увеличение версии</strong>: Обновить .csproj файлы</li>
                        <li><strong>Git Tag</strong>: Создать тег для новой версии</li>
                        <li><strong>Публикация NuGet</strong>: Загрузить пакет на NuGet</li>
                        <li><strong>GitHub Release</strong>: Опубликовать с заметками о релизе</li>
                    </ol>
                </div>
            </div>
        </section>

        <!-- Community Guidelines Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Руководящие принципы сообщества</h2>
                    <p>Правила, которым необходимо следовать в сообществе SmartRAG:</p>
                    
                    <h3>Правила поведения</h3>
                    <ul>
                        <li><strong>Быть уважительным</strong>: Проявляйте уважение ко всем</li>
                        <li><strong>Быть конструктивным</strong>: Давайте конструктивную обратную связь</li>
                        <li><strong>Быть открытым к обучению</strong>: Принимайте новые идеи</li>
                        <li><strong>Быть профессиональным</strong>: Используйте профессиональный язык</li>
                    </ul>
                    
                    <h3>Каналы связи</h3>
                    <ul>
                        <li><strong>GitHub Issues</strong>: Сообщения об ошибках и запросы функций</li>
                        <li><strong>GitHub Discussions</strong>: Общие обсуждения</li>
                        <li><strong>Email</strong>: b.yerlikaya@outlook.com</li>
                    </ul>
                </div>
            </div>
        </section>

        <!-- Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Нужна помощь?</h4>
                        <p class="mb-0">Если вам нужна помощь с участием в проекте:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/ru/getting-started">Начало работы</a></li>
                            <li><a href="{{ site.baseurl }}/ru/api-reference">Справочник API</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub Issues</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Поддержка по email</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
