# Changelog

All notable changes to SmartRAG will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Excel file support with EPPlus
- Batch document processing
- Advanced search filters
- Performance monitoring

## [1.0.2] - 2025-01-20

### Added
- 🚀 **Hybrid Search Algorithm**: Revolutionary semantic + keyword boost approach for superior relevance scoring
- 🎯 **Gemini RAG Optimization**: Full Google Gemini support with native embeddings and optimized performance
- 🧩 **Smart Document Chunking**: Intelligent overlap between chunks to prevent information loss at boundaries
- 🔄 **Case-Insensitive Search**: Robust text normalization for improved multi-language support
- 📊 **Enhanced Search Scoring**: Relevance scores >1.0 with keyword boosting for precise results
- ⚡ **Production-Ready Performance**: Optimized maxResults parameter and interface improvements

### Improved
- **Semantic Search Engine**: Hybrid approach combining vector similarity with keyword matching
- **AI Prompt Construction**: Enhanced RAG prompt building for better AI responses
- **Document Chunking**: Added configurable overlap to preserve context between chunks
- **Search Interface**: Increased default maxResults from 5 to 10 for better coverage
- **Text Normalization**: Case-insensitive search with proper Unicode handling

### Fixed
- **Gemini Provider**: Fixed embedding model configuration and batch processing
- **AzureOpenAI Provider**: Enhanced batch embedding support and error handling
- **Chunk Boundaries**: Resolved information loss at document chunk boundaries
- **Search Relevance**: Improved keyword matching and scoring accuracy
- **RAG Responses**: Fixed AI context handling for proper document-based answers

### Technical Details
- Added `CalculateKeywordBoost` method for semantic + keyword hybrid scoring
- Implemented chunk overlap in `DocumentParserService` for context preservation
- Enhanced `SemanticKernelSearchProvider` with production-ready hybrid search
- Optimized `AIService` prompt construction for better RAG performance
- Fixed Gemini API endpoint configuration and batch embedding handling
- Enhanced `AzureOpenAIProvider` with batch embeddings and improved error handling
- Added embedding management methods (`ClearEmbeddingsAsync`, `RegenerateAllEmbeddingsAsync`)

## [1.0.1] - 2025-01-19

### Improved
- 🧠 **Smart Query Intent Detection**: Enhanced query routing between chat and document search
- 🌍 **Language-Agnostic Design**: Removed all hardcoded language patterns for global compatibility
- 🔍 **Enhanced Search Relevance**: Improved name detection and content scoring algorithms
- 🔤 **Unicode Normalization**: Fixed special character handling issues (e.g., Turkish characters)
- ⚡ **Rate Limiting & Retry Logic**: Robust API handling with exponential backoff
- 🚀 **VoyageAI Integration**: Optimized Anthropic embedding support
- 📚 **Enhanced Documentation**: Added official documentation links and troubleshooting guide
- 🧹 **Configuration Cleanup**: Removed unnecessary configuration fields
- 🎯 **Project Simplification**: Streamlined codebase for better performance

### Fixed
- Query intent detection for general conversation vs document search
- Special character handling in search queries
- Rate limiting issues with AI providers
- Configuration validation and error handling

## [1.0.0] - 2025-01-19

### Added
- 🎯 **Core RAG Pipeline**: Complete Retrieval-Augmented Generation workflow
- 🤖 **AI Provider Support**: OpenAI, Anthropic, Gemini, Azure OpenAI, CustomProvider
- 🗄️ **Storage Options**: Qdrant, Redis, SQLite, FileSystem, InMemory
- 📄 **Document Processing**: PDF, Word (.docx/.doc), text files with smart chunking
- 🔍 **Semantic Search**: Vector-based document retrieval with similarity scoring
- 🧠 **AI-Powered Q&A**: Context-aware answer generation from documents
- ⚡ **Dependency Injection**: Full .NET DI container integration
- 🔧 **Configuration-First**: Environment-based configuration with sensible defaults

### Technical Features
- **Clean Architecture**: SOLID principles with clear separation of concerns
- **Factory Pattern**: Flexible AI provider and storage instantiation
- **Interface-Based Design**: Extensible architecture for custom implementations
- **Async/Await**: Full asynchronous programming support
- **Error Handling**: Comprehensive exception handling and logging
- **Memory Optimization**: Efficient text chunking and vector operations

### Documentation
- 📖 **Comprehensive README**: Complete setup and usage guide
- 🤝 **Contributing Guide**: Detailed contribution guidelines
- 🐛 **Issue Templates**: Bug report and feature request templates
- 📝 **PR Template**: Standardized pull request format
- ⚙️ **CI/CD Pipeline**: Automated testing and NuGet publishing

### Supported Formats
- **PDF**: Advanced text extraction with iText7
- **Word**: .docx and .doc support with OpenXML
- **Text**: .txt, .md, .json, .xml, .csv, .html with UTF-8 encoding

### AI Providers
- **OpenAI**: GPT models with embedding support
- **Anthropic**: Claude family models
- **Google Gemini**: Latest Gemini models with multimodal capabilities
- **Azure OpenAI**: Enterprise-grade GPT with SLA support
- **CustomProvider**: Universal OpenAI-compatible API support

### Storage Providers
- **Qdrant**: Professional vector database with advanced search
- **Redis**: In-memory vector storage with persistence
- **SQLite**: Local database with vector support
- **FileSystem**: Simple file-based storage
- **InMemory**: Development and testing storage

### Architecture
- **Entities**: Document, DocumentChunk data models
- **Enums**: AIProvider, StorageProvider, RetryPolicy
- **Extensions**: ServiceCollection integration helpers
- **Factories**: AI provider and storage factory patterns
- **Interfaces**: Comprehensive abstraction layer
- **Models**: Configuration and response models
- **Providers**: AI service implementations
- **Repositories**: Storage abstraction implementations
- **Services**: Core business logic services

### Performance
- **Document Upload**: ~500ms for 10MB PDF
- **Semantic Search**: ~200ms with 10K documents
- **AI Response**: ~2-5s depending on provider
- **Memory Usage**: ~50MB base + documents

### Security
- **API Key Management**: Environment variable configuration
- **Input Validation**: Comprehensive request validation
- **Error Sanitization**: Safe error message handling

---

## Release Process

### Version Format
- **Major.Minor.Patch** (e.g., 1.0.0)
- **Major**: Breaking changes
- **Minor**: New features (backward compatible)
- **Patch**: Bug fixes (backward compatible)

### Release Triggers
- Commit message containing `[release]` on main branch
- Automatic NuGet publishing via GitHub Actions
- Automatic GitHub release creation

### Tags
- Format: `v1.0.0`
- Automatic creation on NuGet publish
- Includes release notes from this changelog

---

## Contributors

### Core Team
- **Barış Yerlikaya** - Project Creator & Maintainer
  - 💼 [LinkedIn](https://www.linkedin.com/in/barisyerlikaya)
  - 🐙 [GitHub](https://github.com/byerlikaya)
  - 📧 [Email](mailto:b.yerlikaya@outlook.com)

### Special Thanks
- Community contributors (see [Contributors](https://github.com/byerlikaya/SmartRAG/graphs/contributors))
- .NET Community for inspiration and best practices
- AI Provider teams for excellent APIs

---

Made with ❤️ in Turkey 🇹🇷
