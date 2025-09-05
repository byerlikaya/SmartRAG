---
layout: default
title: Журнал изменений
description: Отслеживание новых функций, улучшений и исправлений ошибок в версиях SmartRAG
lang: ru
---

# Журнал изменений

Отслеживание новых функций, улучшений и исправлений ошибок в версиях SmartRAG.

## [2.1.0] - 2025-01-20

### Добавлено
- **Автоматическое управление сессиями**: Больше не требуется ручное управление ID сессий
- **Постоянная история разговоров**: Разговоры сохраняются после перезапуска приложения
- **Новые команды разговора**: /new, /reset, /clear для управления разговором
- **Улучшенный API**: Обратно совместимый с опциональным параметром startNewConversation
- **Интеграция хранилища**: Работает со всеми провайдерами (Redis, SQLite, FileSystem, InMemory)
- **Согласованность формата**: Стандартизированный формат разговора для всех провайдеров хранилища
- **Потокобезопасность**: Улучшенная обработка параллельного доступа для операций разговора
- **Платформо-агностик**: Поддерживает совместимость со всеми .NET окружениями
- **Обновления документации**: Все языковые версии (EN, TR, DE, RU) обновлены реальными примерами
- **100% соответствие**: Все установленные правила соблюдены с политикой нулевых предупреждений

## [2.0.0] - 2025-08-27

### Добавлено
- **.NET Standard 2.0/2.1**: Совместимость с .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+
- **Максимальная совместимость**: Поддержка устаревших и корпоративных .NET приложений
- **Изменение фреймворка**: Миграция с .NET 9.0 на .NET Standard
- **Зависимости пакетов**: Обновлены версии пакетов для совместимости

## [1.1.0] - 2025-08-22

### Добавлено
- **Поддержка Excel файлов**: Добавлен парсинг Excel файлов (.xlsx, .xls) с интеграцией EPPlus 8.1.0
- **Улучшенная логика повторов**: Улучшен механизм повторов Anthropic API для HTTP 529 (Overloaded) ошибок
- **Валидация контента**: Улучшена валидация содержимого документов
- **Документация Excel**: Комплексная документация по формату Excel

## [1.1.0] - 2025-08-22

### ✨ Added
- **Excel Document Support**: Comprehensive Excel file parsing (.xlsx, .xls) with intelligent content extraction
- **EPPlus 8.1.0 Integration**: Modern Excel processing library with proper non-commercial license setup
- **Worksheet Parsing**: Intelligent parsing of all worksheets with tab-separated data preservation
- **Enhanced Content Validation**: Improved content quality checks with Excel-specific fallback handling
- **Anthropic API Reliability**: Enhanced retry mechanism for HTTP 529 (Overloaded) errors

### 🔧 Improved
- **API Error Handling**: Better retry logic for rate limiting and server overload scenarios
- **Content Processing**: More robust document parsing with fallback error messages
- **Performance**: Optimized Excel content extraction and validation

### 📚 Documentation
- **Excel Format Support**: Comprehensive documentation of Excel file processing capabilities
- **API Reliability**: Updated documentation for enhanced error handling
- **Installation Guide**: Updated package references and configuration examples

### 🧪 Testing
- **Excel Parsing**: Verified with various Excel formats and content types
- **API Retry**: Tested retry mechanism with error scenarios
- **Backward Compatibility**: Ensured all existing functionality remains intact

### 🔒 Security
- **License Compliance**: Proper EPPlus non-commercial license setup
- **Zero Warnings**: Maintained strict code quality standards

## [1.0.3] - 2025-08-20

### Added
- **Multi-language Support**: Added comprehensive documentation in English, Turkish, German, and Russian
- **GitHub Pages**: Complete documentation site with modern Bootstrap design
- **Enhanced Examples**: Added comprehensive code examples and tutorials
- **Troubleshooting Guide**: Detailed troubleshooting and debugging information
- **Contributing Guidelines**: Complete contribution guide with coding standards

### Improved
- **Documentation**: Complete rewrite with modern design and better organization
- **Code Examples**: More realistic and comprehensive examples
- **API Reference**: Detailed API documentation with usage patterns
- **Configuration Guide**: Enhanced configuration options and best practices

### Fixed
- **Type Conflicts**: Resolved conflicts between Qdrant, OpenXML, and other libraries
- **Global Usings**: Implemented GlobalUsings for all projects to reduce code duplication
- **Build Issues**: Fixed various compilation and build warnings

## [1.0.2] - 2024-12-XX

### Added
- **Global Usings**: Implemented GlobalUsings for SmartRAG core library
- **Type Resolution**: Added explicit type resolution for conflicting types
- **Enhanced Logging**: Improved logging with LoggerMessage delegates

### Improved
- **Code Organization**: Better #region organization and SOLID principles
- **Performance**: Optimized document processing and storage operations
- **Error Handling**: Enhanced error handling and exception management

### Fixed
- **Build Warnings**: Resolved all compiler warnings and messages
- **Type Conflicts**: Fixed conflicts between external library types
- **Memory Leaks**: Improved resource disposal and memory management

## [1.0.1] - 2024-11-XX

### Added
- **Test Project**: Added comprehensive xUnit test suite
- **Example Web API**: Complete example web application
- **Documentation**: Initial documentation structure

### Improved
- **Code Quality**: Applied SOLID and DRY principles
- **Error Handling**: Better exception handling and validation
- **Logging**: Structured logging throughout the application

### Fixed
- **Minor Bugs**: Various bug fixes and improvements
- **Performance**: Optimized document processing
- **Security**: Enhanced input validation and sanitization

## [1.0.0] - 2024-10-XX

### Initial Release
- **Core RAG Functionality**: Document processing, embedding generation, and semantic search
- **AI Provider Support**: OpenAI, Anthropic, Azure OpenAI, and Gemini integration
- **Storage Providers**: Qdrant, Redis, SQLite, In-Memory, and File System support
- **Document Formats**: PDF, Word, Excel, and text document processing
- **.NET 8 Support**: Full compatibility with .NET 8 LTS
- **Dependency Injection**: Native .NET dependency injection support
- **Async/Await**: Full asynchronous operation support
- **Extensible Architecture**: Plugin-based provider system

## Versioning

SmartRAG follows [Semantic Versioning](https://semver.org/) (SemVer):

- **MAJOR**: Incompatible API changes
- **MINOR**: New functionality in a backwards compatible manner
- **PATCH**: Backwards compatible bug fixes

## Release Schedule

- **Major Releases**: Every 6-12 months with significant new features
- **Minor Releases**: Every 2-3 months with new functionality
- **Patch Releases**: As needed for critical bug fixes

## Breaking Changes

### 1.0.0 to 1.0.1
- No breaking changes

### 1.0.1 to 1.0.2
- No breaking changes

### 1.0.2 to 1.0.3
- No breaking changes

## Migration Guides

### Upgrading from 1.0.2 to 1.0.3

No migration required. This is a fully backward-compatible release.

### Upgrading from 1.0.1 to 1.0.2

No migration required. This is a fully backward-compatible release.

### Upgrading from 1.0.0 to 1.0.1

No migration required. This is a fully backward-compatible release.

## Support Policy

- **Current Version**: Full support and bug fixes
- **Previous Version**: Security updates and critical bug fixes only
- **Older Versions**: No support

## Roadmap

### Upcoming Features (1.1.0)

- **Advanced Chunking**: Intelligent document chunking strategies
- **Custom Embeddings**: Support for custom embedding models
- **Batch Processing**: Improved batch document processing
- **Performance Monitoring**: Built-in performance metrics and monitoring
- **Cloud Integration**: Enhanced cloud provider support

### Future Plans (2.0.0)

- **Multi-modal Support**: Image and audio document processing
- **Advanced Search**: Semantic search with context awareness
- **Real-time Updates**: Live document indexing and search
- **Distributed Processing**: Support for distributed deployments
- **Advanced Analytics**: Document usage and search analytics

## Contributing to Changelog

When contributing to SmartRAG, please update the changelog:

1. **Add your changes** to the appropriate section
2. **Use consistent formatting** following the existing style
3. **Group changes** by type (Added, Improved, Fixed, etc.)
4. **Provide clear descriptions** of what changed
5. **Include breaking changes** in a separate section

### Changelog Entry Format

```markdown
### Added
- **Feature Name**: Brief description of the new feature

### Improved
- **Component Name**: Description of improvements made

### Fixed
- **Issue Description**: Description of the bug fix
```

## Need Help?

If you need assistance with version updates:

- [Back to Documentation]({{ site.baseurl }}/en/) - Main documentation
- [Open an issue](https://github.com/byerlikaya/SmartRAG/issues) - GitHub Issues
- [Contact support](mailto:b.yerlikaya@outlook.com) - Email support
