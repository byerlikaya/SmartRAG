---
layout: default
title: Устранение неполадок
description: Общие проблемы и решения для SmartRAG
lang: ru
---

<div class="page-content">
    <div class="container">
        <!-- Common Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Общие проблемы</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Быстрые решения частых проблем.</p>
                    
                    <h3>Проблемы конфигурации</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Неверный API ключ</h5>
                        <p><strong>Проблема:</strong> Ошибки "Unauthorized" или "Invalid API key"</p>
                        <p><strong>Решение:</strong> Проверьте ваши API ключи в appsettings.json</p>
                    </div>

                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Отсутствует конфигурация</h5>
                        <p><strong>Проблема:</strong> Ошибки "Configuration not found"</p>
                        <p><strong>Решение:</strong> Убедитесь, что раздел SmartRAG существует в appsettings.json</p>
                    </div>

                        <h3>Проблемы регистрации сервисов</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Сервис не зарегистрирован</h5>
                        <p><strong>Проблема:</strong> Ошибки "Unable to resolve service"</p>
                        <p><strong>Решение:</strong> Добавьте SmartRAG сервисы в Program.cs:</p>
                    <div class="code-example">
                            <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
});</code></pre>
                        </div>
                    </div>

                    <h3>Проблемы обработки аудио</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Ошибки Google Speech-to-Text</h5>
                        <p><strong>Проблема:</strong> Транскрипция аудио не удается</p>
                        <p><strong>Решение:</strong> Проверьте Google API ключ и поддерживаемый аудио формат</p>
                    </div>

                    <h3>Проблемы хранилища</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Не удается подключиться к Redis</h5>
                        <p><strong>Проблема:</strong> Не может подключиться к Redis</p>
                        <p><strong>Решение:</strong> Проверьте строку подключения Redis и убедитесь, что Redis запущен</p>
                    </div>

                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Не удается подключиться к Qdrant</h5>
                        <p><strong>Проблема:</strong> Не может подключиться к Qdrant</p>
                        <p><strong>Решение:</strong> Проверьте конфигурацию хоста Qdrant и API ключа</p>
                    </div>
                </div>
            </div>
        </section>

        <!-- Performance Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
            <h2>Проблемы производительности</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Оптимизируйте производительность SmartRAG.</p>
                    
                        <h3>Медленная обработка документов</h3>
                    <div class="alert alert-info">
                        <h5><i class="fas fa-info-circle me-2"></i>Советы по оптимизации</h5>
                        <ul class="mb-0">
                            <li>Используйте подходящие размеры чанков (500-1000 символов)</li>
                            <li>Включите кэширование Redis для лучшей производительности</li>
                            <li>Используйте Qdrant для производственного векторного хранилища</li>
                            <li>Обрабатывайте документы пакетами</li>
                        </ul>
                    </div>

                    <h3>Проблемы с памятью</h3>
                    <div class="alert alert-info">
                        <h5><i class="fas fa-info-circle me-2"></i>Управление памятью</h5>
                        <ul class="mb-0">
                            <li>Ограничьте размер документа для обработки</li>
                            <li>Используйте потоковую передачу для больших файлов</li>
                            <li>Регулярно очищайте кэш эмбеддингов</li>
                            <li>Мониторьте использование памяти в продакшене</li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>

        <!-- Debugging Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Отладка</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Включите логирование и отладку.</p>
                    
                    <h3>Включить отладочное логирование</h3>
                    <div class="code-example">
                        <pre><code class="language-json">{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SmartRAG": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}</code></pre>
                    </div>

                    <h3>Проверить статус сервисов</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Проверить, зарегистрированы ли сервисы
    var serviceProvider = services.BuildServiceProvider();
var documentService = serviceProvider.GetService<IDocumentService>();
var searchService = serviceProvider.GetService<IDocumentSearchService>();
    
if (documentService == null || searchService == null)
{
    Console.WriteLine("Сервисы SmartRAG не зарегистрированы должным образом!");
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Все еще нужна помощь?</h4>
                        <p class="mb-0">Если вы не можете найти решение:</p>
                            <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/ru/getting-started">Руководство по началу работы</a></li>
                            <li><a href="{{ site.baseurl }}/ru/configuration">Руководство по конфигурации</a></li>
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