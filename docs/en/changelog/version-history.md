---
layout: default
title: Version History
description: Complete version history for SmartRAG
lang: en
---

## Version History

All releases and changes to SmartRAG are documented here.

<div class="accordion mt-4" id="versionAccordion">
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion340">
            <button class="accordion-button" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion340" aria-expanded="true" aria-controls="collapseversion340">
                <strong>v3.4.0</strong> - 2025-12-11
            </button>
        </h2>
        <div id="collapseversion340" class="accordion-collapse collapse show" aria-labelledby="headingversion340" >
            <div class="accordion-body">
{% capture version_content %}

### MCP Integration, File Watcher, and Query Strategy Optimization

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> MINOR Release</h4>
    <p class="mb-0">
        This release adds MCP (Model Context Protocol) integration, file watcher service, and significant query strategy optimizations with early exit and parallel execution improvements.
    </p>
</div>

### ✨ Added

#### MCP (Model Context Protocol) Integration
- **External MCP Server Integration**: Enhanced search capabilities through external MCP servers
- **Multiple MCP Servers**: Support for multiple MCP servers with automatic tool discovery
- **Query Enrichment**: Conversation history context enrichment for MCP queries

#### File Watcher Service
- **Automatic Document Indexing**: Monitor folders and automatically index new documents
- **Multiple Watched Folders**: Support for multiple watched folders with independent configurations
- **Language-Specific Processing**: Per-folder language configuration

#### DocumentType Property
- **Content Type Filtering**: Enhanced document chunk filtering by content type (Document, Audio, Image)
- **Automatic Detection**: Document type detection based on file extension and content type

#### DefaultLanguage Support
- **Global Default Language**: Global default language configuration for document processing
- **ISO 639-1 Support**: Support for ISO 639-1 language codes

#### Enhanced Search Feature Flags
- **Granular Control**: `EnableMcpSearch`, `EnableAudioSearch`, `EnableImageSearch` flags
- **Per-Request and Global Configuration**: Both per-request and global configuration support

#### Early Exit Optimization
- **Performance Improvement**: Early exit when sufficient high-quality results are found
- **Parallel Execution**: Parallel execution of document search and query intent analysis
- **Smart Skip Logic**: Skip eager document answer generation when database intent confidence is high

#### IsExplicitlyNegative Check
- **Fast-Fail Mechanism**: Detecting explicit failure patterns with `[NO_ANSWER_FOUND]` pattern
- **Prevents False Positives**: Prevents false positives when AI returns negative answers despite high-confidence document matches

### 🔧 Improved

#### Query Strategy Optimization
- **Intelligent Source Selection**: Enhanced query execution strategy with intelligent source selection
- **StrongDocumentMatchThreshold**: Improved early exit logic with threshold constant (4.8) for better document prioritization
- **Database Query Skip Logic**: Enhanced logic based on document match strength and AI answer quality

#### Code Quality
- **Comprehensive Cleanup**: Removed redundant comments and language-specific references
- **Improved Naming**: Better constant naming and generic code patterns
- **Enhanced Organization**: Improved code organization and structure

#### Model Organization
- **Logical Subfolders**: Reorganized models into logical subfolders (Configuration/, RequestResponse/, Results/, Schema/)

### 🐛 Fixed

- **Language-Agnostic Missing Data Detection**: Fixed language-specific patterns
- **HttpClient Timeout**: Increased timeout for long-running AI operations
- **Turkish Character Encoding**: Fixed encoding issues in PDF text extraction
- **Chunk0 Retrieval**: Fixed numbered list processing chunk retrieval
- **DI Scope Issues**: Resolved dependency injection scope conflicts
- **Content Type Detection**: Improved content type detection accuracy
- **Conversation Intent Classification**: Enhanced context awareness
- **Conversation History Duplicate Entries**: Fixed duplicate entries
- **Redis Document Retrieval**: Fixed document retrieval when document list is empty
- **SqlValidator DI Compatibility**: Fixed dependency injection compatibility

### 🔄 Changed

- **Feature Flag Naming**: Renamed flags for consistency (`EnableMcpClient` → `EnableMcpSearch`, etc.)
- **Interface Restructuring**: Reorganized interfaces for better organization

### ✨ Benefits

- **Extended Search Capabilities**: MCP integration enables external data source queries
- **Automatic Document Indexing**: File watcher service reduces manual document uploads
- **Better Content Filtering**: DocumentType property enables precise content type filtering
- **Improved Code Quality**: Comprehensive code cleanup and organization improvements
- **Enhanced Multilingual Support**: DefaultLanguage configuration simplifies language handling
- **Performance Optimization**: Early exit optimization improves search response times

### 📝 Notes

- **MCP Integration**: Requires MCP server configuration in `SmartRagOptions.McpServers`
- **File Watcher**: Requires watched folder configuration in `SmartRagOptions.WatchedFolders`
- **Backward Compatibility**: All changes are backward compatible, no breaking changes

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion330">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion330" aria-expanded="false" aria-controls="collapseversion330">
                <strong>v3.3.0</strong> - 2025-12-01
            </button>
        </h2>
        <div id="collapseversion330" class="accordion-collapse collapse" aria-labelledby="headingversion330" >
            <div class="accordion-body">
{% capture version_content %}

### Redis Vector Search & Storage Improvements

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> MINOR Release</h4>
    <p class="mb-0">
        This release enhances Redis vector search capabilities and removes unused storage implementations.
        Active storage providers (Qdrant, Redis, InMemory) remain fully functional.
    </p>
</div>

### ✨ Added

#### Redis RediSearch Integration
- **Enhanced Vector Similarity Search**: RediSearch module support for advanced vector search capabilities
- **Vector Index Configuration**: Algorithm (HNSW), distance metric (COSINE), and dimension (default: 768) configuration
- **Files Modified**:
  - `src/SmartRAG/Models/RedisConfig.cs` - Vector search configuration properties
  - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - RediSearch vector search implementation

### 🔧 Improved

#### Redis Vector Search Accuracy
- **Proper Relevance Scoring**: RelevanceScore now correctly calculated and assigned for DocumentSearchService ranking
- **Similarity Calculation**: Distance metrics from RediSearch properly converted to similarity scores
- **Debug Logging**: Score verification logging added
- **Files Modified**:
  - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - RelevanceScore assignment

#### Redis Embedding Generation
- **AI Configuration Handling**: IAIConfigurationService injection for proper config retrieval
- **Graceful Fallback**: Text search fallback when config unavailable
- **Files Modified**:
  - `src/SmartRAG/Repositories/RedisDocumentRepository.cs` - AI config handling
  - `src/SmartRAG/Factories/StorageFactory.cs` - IAIConfigurationService injection

#### StorageFactory Dependency Injection
- **Scope Resolution**: Fixed Singleton/Scoped lifetime mismatch using lazy resolution
- **IServiceProvider Pattern**: Changed to lazy dependency resolution via IServiceProvider
- **Files Modified**:
  - `src/SmartRAG/Factories/StorageFactory.cs` - Lazy dependency resolution
  - `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - IAIProvider lifetime adjustment

### 🐛 Fixed

- **StorageFactory DI Scope Issue**: Fixed InvalidOperationException when resolving IAIProvider
- **Redis Relevance Scoring**: Fixed RelevanceScore being 0.0000 in search results
- **Redis Embedding Config**: Fixed NullReferenceException when generating embeddings

### 🗑️ Removed

- **FileSystemDocumentRepository**: Removed unused file system storage implementation
- **SqliteDocumentRepository**: Removed unused SQLite storage implementation
- **StorageConfig Properties**: Removed FileSystemPath and SqliteConfig (unused)

### ⚠️ Breaking Changes

- **FileSystem and SQLite Document Repositories Removed**
  - These were unused implementations
  - Active storage providers (Qdrant, Redis, InMemory) remain fully functional
  - If you were using FileSystem or SQLite, migrate to Qdrant, Redis, or InMemory

### 📝 Notes

- **Redis Requirements**: Vector search requires RediSearch module
  - Use `redis/redis-stack-server:latest` Docker image
  - Or install RediSearch module on your Redis server
  - Without RediSearch, only text search works (no vector search)

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion320">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion320" aria-expanded="false" aria-controls="collapseversion320">
                <strong>v3.2.0</strong> - 2025-11-27
            </button>
        </h2>
        <div id="collapseversion320" class="accordion-collapse collapse" aria-labelledby="headingversion320" >
            <div class="accordion-body">
{% capture version_content %}

### 🏗️ Architectural Refactoring - Modular Design

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> MINOR Release</h4>
    <p class="mb-0">
        This release introduces significant architectural improvements while maintaining full backward compatibility.
        All existing code continues to work without changes.
    </p>
</div>

#### **Strategy Pattern Implementation**

##### SQL Dialect Strategy
- **`ISqlDialectStrategy`**: Interface for database-specific SQL generation
- **Dialect Implementations**: 
  - `SqliteDialectStrategy` - SQLite-optimized SQL generation
  - `PostgreSqlDialectStrategy` - PostgreSQL-optimized SQL generation
  - `MySqlDialectStrategy` - MySQL/MariaDB-optimized SQL generation
  - `SqlServerDialectStrategy` - SQL Server-optimized SQL generation
- **`ISqlDialectStrategyFactory`**: Factory for creating appropriate dialect strategies
- **Benefits**: Open/Closed Principle (OCP), easier to add new database support

##### Scoring Strategy
- **`IScoringStrategy`**: Interface for document relevance scoring
- **`HybridScoringStrategy`**: Combines semantic and keyword-based scoring
- **Benefits**: Pluggable scoring algorithms, easier to customize search behavior

##### File Parser Strategy
- **`IFileParser`**: Interface for file format parsing
- **Strategy-based parsing**: Each file type has dedicated parser implementation
- **Benefits**: Single Responsibility Principle (SRP), easier to add new file formats

#### **Repository Layer Separation**

##### Conversation Repository
- **`IConversationRepository`**: Dedicated interface for conversation data access
- **Implementations**:
  - `SqliteConversationRepository` - SQLite-based conversation storage
  - `InMemoryConversationRepository` - In-memory conversation storage
  - `FileSystemConversationRepository` - File-based conversation storage
  - `RedisConversationRepository` - Redis-based conversation storage
- **`IConversationManagerService`**: Business logic for conversation management
- **Benefits**: Separation of Concerns (SoC), Interface Segregation Principle (ISP)

##### Repository Cleanup
- **`IDocumentRepository`**: Removed conversation-related methods
- **Clear separation**: Documents vs Conversations
- **Benefits**: Cleaner interfaces, better testability

#### **Service Layer Refactoring**

##### AI Service Decomposition
- **`IAIConfigurationService`**: AI provider configuration management
- **`IAIRequestExecutor`**: AI request execution with retry/fallback
- **`IPromptBuilderService`**: Prompt construction and optimization
- **`IAIProviderFactory`**: Factory for creating AI provider instances
- **Benefits**: Single Responsibility Principle (SRP), better testability

##### Database Services
- **`IQueryIntentAnalyzer`**: Query intent analysis and classification
- **`IDatabaseQueryExecutor`**: Database query execution
- **`IResultMerger`**: Multi-database result merging
- **`ISQLQueryGenerator`**: SQL query generation with validation
- **`IDatabaseConnectionManager`**: Database connection lifecycle management
- **`IDatabaseSchemaAnalyzer`**: Database schema analysis and caching

##### Search Services
- **`IEmbeddingSearchService`**: Embedding-based search operations
- **`ISourceBuilderService`**: Search result source building

##### Parser Services
- **`IAudioParserService`**: Audio file parsing and transcription
- **`IImageParserService`**: Image OCR processing
- **`IAudioParserFactory`**: Factory for audio parser creation

##### Support Services
- **`IQueryIntentClassifierService`**: Query intent classification
- **`ITextNormalizationService`**: Text normalization and cleaning

#### **Model Consolidation**

#### **New Features: Customization Support**

- **Custom SQL Dialect Strategies**: Support for implementing custom database dialects and extending existing ones (SQLite, SQL Server, MySQL, PostgreSQL)
- **Custom Scoring Strategies**: Support for implementing custom search relevance logic
- **Custom File Parsers**: Support for implementing custom file format parsers
- **Dedicated Conversation Management**: New service for managing conversation history

### ✨ Added

- **SearchOptions Support**: Per-request search configuration with granular control
  - `SearchOptions` model with feature flags for database, document, audio, and image search
  - `PreferredLanguage` property for ISO 639-1 language code support
  - Conditional service registration based on feature flags
  - **Flag-Based Document Filtering**: Query string flags (`-db`, `-d`, `-a`, `-i`) for quick search type selection
  - **Document Type Filtering**: Automatic filtering by content type (text, audio, image)

- **Native Qdrant Text Search**: Token-based filtering for improved search performance
  - Native Qdrant text search with token-based OR filtering
  - Automatic stopword filtering and token match counting

- **ClearAllAsync Methods**: Efficient bulk deletion operations
  - `IDocumentRepository.ClearAllAsync()` - Efficient bulk delete
  - `IDocumentService.ClearAllDocumentsAsync()` - Clear all documents
  - `IDocumentService.ClearAllEmbeddingsAsync()` - Clear embeddings only

- **Tesseract On-Demand Language Data Download**: Automatic language support
  - Automatic download of Tesseract language data files
  - Support for 30+ languages with ISO 639-1/639-2 code mapping

- **Currency Symbol Correction**: Improved OCR accuracy for financial documents
  - Automatic correction of common OCR misreads (`%`, `6`, `t`, `&` → currency symbols)
  - Applied to both OCR and PDF parsing

- **Parallel Batch Processing for Ollama Embeddings**: Performance optimization
  - Parallel batch processing for embedding generation
  - Improved throughput for large document sets

- **Query Tokens Parameter**: Pre-computed token support
  - Optional `queryTokens` parameter to eliminate redundant tokenization

- **FeatureToggles Model**: Global feature flag configuration
  - `FeatureToggles` class for centralized feature management
  - `SearchOptions.FromConfig()` static method for easy configuration

- **ContextExpansionService**: Adjacent chunk context expansion
  - Expands document chunk context by including adjacent chunks
  - Configurable context window for better AI responses

- **FileParserResult Model**: Standardized parser result structure
  - Consistent parser output format with content and metadata

- **DatabaseFileParser**: SQLite database file parsing support
  - Direct database file upload and parsing (.db, .sqlite, .sqlite3, .db3)

- **Native Library Inclusion**: Tesseract OCR native libraries bundled
  - No manual library installation required
  - Supports Windows, macOS, and Linux

- **Nullable Reference Types**: Enhanced null safety
  - Better compile-time null checking across 14+ files

### Improved

- **Unicode Normalization for Qdrant**: Better text retrieval across languages
- **PDF OCR Encoding Issue Detection**: Automatic fallback handling
- **Numbered List Chunk Detection**: Improved counting query accuracy
- **RAG Scoring Improvements**: Enhanced relevance calculation with unique keyword bonus
- **Document Search Adaptive Threshold**: Dynamic relevance threshold adjustment
- **Prompt Builder Rules**: Enhanced AI answer generation
- **QdrantDocumentRepository GetAllAsync**: Performance optimization
- **Text Processing and AI Prompt Services**: General improvements
- **Image Parser Service**: Comprehensive improvements

### Fixed

- **Table Alias Enforcement in SQL Generation**: Prevents ambiguous column errors
- **EnableDatabaseSearch Config Respect**: Proper feature flag handling
- **macOS Native Libraries**: OCR library inclusion and DYLD_LIBRARY_PATH configuration
- **Missing Method Signature**: DocumentSearchService restoration

### Changed

- **IEmbeddingSearchService Dependency Removal**: Simplified architecture
- **Code Cleanup**: Inline comments and unused directives removal
- **Logging Cleanup**: Reduced verbose logging
- **NuGet Package Updates**: Latest compatible versions
- **Service Method Annotations**: Better code documentation with `[AI Query]`, `[Document Query]`, `[DB Query]` tags

### 🔧 Code Quality

#### **Build Quality**
- **Zero Warnings**: Maintained 0 errors, 0 warnings across all projects
- **SOLID Compliance**: Full adherence to SOLID principles
- **Clean Architecture**: Clear separation of concerns across layers

#### **Files Modified**
- `src/SmartRAG/Interfaces/` - New interfaces for Strategy Pattern
- `src/SmartRAG/Services/` - Service layer refactoring
- `src/SmartRAG/Repositories/` - Repository separation
- `src/SmartRAG/Models/` - Model consolidation
- `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Updated DI registrations

### ✨ Benefits

- **Maintainability**: Cleaner, more modular codebase
- **Extensibility**: Easy to add new databases, AI providers, file formats
- **Testability**: Better unit testing with clear interfaces
- **Performance**: Optimized SQL generation per database dialect
- **Flexibility**: Pluggable strategies for scoring, parsing, SQL generation
- **Backward Compatibility**: All existing code works without changes

### 📚 Migration Guide

#### No Breaking Changes
All changes are backward compatible. Existing code continues to work without modifications.

#### Optional Enhancements

**Use New Conversation Management**:
```csharp
// Old approach (still works)
await _documentSearchService.QueryIntelligenceAsync(query);

// New approach (recommended for conversation tracking)
var sessionId = await _conversationManager.StartNewConversationAsync();
await _conversationManager.AddToConversationAsync(sessionId, userMessage, aiResponse);
var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
```

#### Customization Examples (Optional)

**Custom SQL Dialect Strategy**:
```csharp
// Example: Extending PostgreSQL support with custom validation
public class EnhancedPostgreSqlDialectStrategy : BaseSqlDialectStrategy
{
    public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;
    
    public override string GetDialectName() => "Enhanced PostgreSQL";
    
    public override string BuildSystemPrompt(
        DatabaseSchemaInfo schema, 
        string userQuery)
    {
        // Enhanced PostgreSQL-specific SQL generation
        return $"Generate PostgreSQL SQL for: {userQuery}\\nSchema: {schema}";
    }
}
```

**Custom Scoring Strategy**:
```csharp
// Example: Implementing custom scoring logic
public class CustomScoringStrategy : IScoringStrategy
{
    public double CalculateScore(DocumentChunk chunk, string query)
    {
        // Custom scoring logic
    }
}
```

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion310">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion310" aria-expanded="false" aria-controls="collapseversion310">
                <strong>v3.1.0</strong> - 2025-11-11
            </button>
        </h2>
        <div id="collapseversion310" class="accordion-collapse collapse" aria-labelledby="headingversion310" >
            <div class="accordion-body">
{% capture version_content %}

### ✨ Unified Query Intelligence

#### **Major Feature: Unified Search Across All Data Sources**
- **Unified Query Intelligence**: `QueryIntelligenceAsync` now supports unified search across databases, documents, images (OCR), and audio (transcription) in a single query
- **Smart Hybrid Routing**: AI-based intent detection with confidence scoring automatically determines optimal search strategy
  - High confidence (>0.7) + database queries → Database query only
  - High confidence (>0.7) + no database queries → Document query only
  - Medium confidence (0.3-0.7) → Both database and document queries, merged results
  - Low confidence (<0.3) → Document query only (fallback)
- **QueryStrategy Enum**: New enum for query execution strategies (DatabaseOnly, DocumentOnly, Hybrid)
- **New Service Architecture**: Modular design with QueryIntentAnalyzer, DatabaseQueryExecutor, ResultMerger, and SQLQueryGenerator services
- **Parallel Query Execution**: Multi-database queries execute in parallel for better performance
- **Intelligent Result Merging**: AI-powered merging of results from multiple databases
- **Intelligent Routing**: Improved query routing logic with graceful degradation and fallback mechanisms
- **Enhanced Error Handling**: Better error handling for database query failures

#### **New Services & Interfaces**
- `QueryIntentAnalyzer` - Analyzes user queries and determines which databases/tables to query using AI
- `DatabaseQueryExecutor` - Executes queries across multiple databases in parallel
- `ResultMerger` - Merges results from multiple databases into coherent responses using AI
- `SQLQueryGenerator` - Generates optimized SQL queries for each database based on query intent

#### **New Models**
- `AudioSegmentMetadata` - Metadata model for audio transcription segments with timestamps and confidence scores

#### **Enhanced Models**
- `SearchSource` - Enhanced with source type differentiation (Database, Document, Image, Audio)

### 🔧 Code Quality & AI Prompt Optimization

#### **Code Quality Improvements**
- **Build Quality**: Achieved 0 errors, 0 warnings across all projects
- **Code Standards**: Full compliance with project coding standards

#### **AI Prompt Optimization**
- **Emoji Reduction**: Reduced emoji usage in AI prompts from 235 to 5 (only critical: 🚨, ✓, ✗)
- **Token Efficiency**: Improved token efficiency (~100 tokens saved per prompt)
- **Strategic Usage**: Better AI comprehension through strategic emoji usage

#### **Files Modified**
- `src/SmartRAG/Services/SQLQueryGenerator.cs` - Emoji optimization in AI prompts
- `src/SmartRAG/Services/MultiDatabaseQueryCoordinator.cs` - Emoji optimization
- `src/SmartRAG/Services/QueryIntentAnalyzer.cs` - Emoji optimization
- `src/SmartRAG/Services/DocumentSearchService.cs` - Emoji optimization

### ✨ Benefits
- **Cleaner Codebase**: Zero warnings across all projects
- **Better Performance**: More efficient AI prompt processing
- **Improved Maintainability**: Better code quality and standards compliance
- **Cost Efficiency**: Reduced token usage in AI prompts

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion303">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion303" aria-expanded="false" aria-controls="collapseversion303">
                <strong>v3.0.3</strong> - 2025-11-06
            </button>
        </h2>
        <div id="collapseversion303" class="accordion-collapse collapse" aria-labelledby="headingversion303" >
            <div class="accordion-body">
{% capture version_content %}

### 🎯 Package Optimization - Native Libraries

#### **Package Size Reduction**
- **Native Libraries Excluded**: Whisper.net.Runtime native libraries (ggml-*.dll, libggml-*.so, libggml-*.dylib) are no longer included in SmartRAG NuGet package
- **Tessdata Excluded**: `tessdata/eng.traineddata` file is no longer included in SmartRAG NuGet package
- **Reduced Package Size**: Significantly smaller NuGet package footprint
- **Cleaner Output**: No unnecessary native library files in project output directory

#### **Files Modified**
- `src/SmartRAG/SmartRAG.csproj` - Added `PrivateAssets="All"` to Whisper.net.Runtime package reference
- `src/SmartRAG/SmartRAG.csproj` - Added `Pack="false"` to tessdata/eng.traineddata content file

### ✨ Benefits
- **Smaller Package Size**: Reduced NuGet package size by excluding native libraries
- **Cleaner Projects**: No unnecessary native library files in project output
- **Better Dependency Management**: Native libraries come from their respective packages (Whisper.net.Runtime, Tesseract)
- **Consistent Behavior**: Matches behavior when directly referencing Whisper.net.Runtime package

### 📚 Migration Guide
If you're using OCR or Audio Transcription features:

**For Audio Transcription (Whisper.net):**
1. Add `Whisper.net.Runtime` package to your project:
   ```xml
   <PackageReference Include="Whisper.net.Runtime" Version="1.8.1" />
   ```
2. Native libraries will be automatically included from Whisper.net.Runtime package
3. No other changes required

**For OCR (Tesseract):**
1. Add `Tesseract` package to your project:
   ```xml
   <PackageReference Include="Tesseract" Version="5.2.0" />
   ```
2. Tesseract package includes tessdata files automatically
3. No other changes required

**Note**: If you're not using OCR or Audio Transcription features, no action is required. The packages are still downloaded as dependencies, but native libraries won't be included unless you explicitly reference the packages.

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion302">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion302" aria-expanded="false" aria-controls="collapseversion302">
                <strong>v3.0.2</strong> - 2025-10-24
            </button>
        </h2>
        <div id="collapseversion302" class="accordion-collapse collapse" aria-labelledby="headingversion302" >
            <div class="accordion-body">
{% capture version_content %}

### 🚀 BREAKING CHANGES - Google Speech-to-Text Removal

#### **Audio Processing Changes**
- **Google Speech-to-Text Removed**: Complete removal of Google Cloud Speech-to-Text integration
- **Whisper.net Only**: Audio transcription now exclusively uses Whisper.net for 100% local processing
- **Data Privacy**: All audio processing is now completely local, ensuring GDPR/KVKK/HIPAA compliance
- **Simplified Configuration**: Removed GoogleSpeechConfig and related configuration options

#### **Files Removed**
- `src/SmartRAG/Services/GoogleAudioParserService.cs` - Google Speech-to-Text service
- `src/SmartRAG/Models/GoogleSpeechConfig.cs` - Google Speech configuration model

#### **Files Modified**
- `src/SmartRAG/SmartRAG.csproj` - Removed Google.Cloud.Speech.V1 NuGet package
- `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Removed Google service registration
- `src/SmartRAG/Factories/AudioParserFactory.cs` - Simplified to Whisper.net only
- `src/SmartRAG/Models/SmartRagOptions.cs` - Removed GoogleSpeechConfig property
- `src/SmartRAG/Enums/AudioProvider.cs` - Removed GoogleCloud enum value
- `src/SmartRAG/Services/ServiceLogMessages.cs` - Updated log messages for Whisper.net

### ✨ Benefits
- **100% Local Processing**: All audio transcription happens locally with Whisper.net
- **Enhanced Privacy**: No data leaves your infrastructure
- **Simplified Setup**: No Google Cloud credentials required
- **Cost Effective**: No per-minute transcription costs
- **Multi-Language**: 99+ languages supported with automatic detection

### 🔧 Technical Details
- **Whisper.net Integration**: Uses OpenAI's Whisper model via Whisper.net bindings
- **Model Options**: Tiny (75MB), Base (142MB), Medium (1.5GB), Large-v3 (2.9GB)
- **Hardware Acceleration**: CPU, CUDA, CoreML, OpenVino support
- **Auto-Download**: Models automatically download on first use
- **Format Support**: MP3, WAV, M4A, AAC, OGG, FLAC, WMA

### 📚 Migration Guide
If you were using Google Speech-to-Text:
1. Remove any GoogleSpeechConfig from your configuration
2. Ensure WhisperConfig is properly configured
3. Update any custom audio processing code to use Whisper.net
4. Test audio transcription with local Whisper.net models

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion301">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion301" aria-expanded="false" aria-controls="collapseversion301">
                <strong>v3.0.1</strong> - 2025-10-22
            </button>
        </h2>
        <div id="collapseversion301" class="accordion-collapse collapse" aria-labelledby="headingversion301" >
            <div class="accordion-body">
{% capture version_content %}

### 🐛 Fixed
- **LoggerMessage Parameter Mismatch**: Fixed `LogAudioServiceInitialized` LoggerMessage definition with missing `configPath` parameter
- **EventId Conflicts**: Resolved duplicate EventId assignments in ServiceLogMessages.cs (6006, 6008, 6009)
- **Logo Display Issue**: Removed broken logo references from README files that were causing display issues on NuGet
- **TypeInitializationException**: Fixed critical startup error

### 🔧 Technical Improvements
- **ServiceLogMessages.cs**: Updated LoggerMessage definitions to match parameter counts correctly
- **EventId Management**: Reassigned conflicting EventIds to ensure unique logging identifiers

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion300">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion300" aria-expanded="false" aria-controls="collapseversion300">
                <strong>v3.0.0</strong> - 2025-10-22
            </button>
        </h2>
        <div id="collapseversion300" class="accordion-collapse collapse" aria-labelledby="headingversion300" >
            <div class="accordion-body">
{% capture version_content %}

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> BREAKING CHANGES</h4>
    <p class="mb-0">This release includes breaking API changes. See migration guide below.</p>
</div>

### 🚀 Intelligence Library Revolution

#### Major API Changes
- **`GenerateRagAnswerAsync` → `QueryIntelligenceAsync`**: Method renamed to better represent intelligent query processing
- **Enhanced `IDocumentSearchService` interface**: New intelligent query processing with advanced RAG pipeline
- **Service layer improvements**: Advanced semantic search and conversation management
- **Backward compatibility maintained**: Legacy methods marked as deprecated (will be removed in v4.0.0)

### 🔧 SQL Generation & Multi-Language Support

#### Language-Safe SQL Generation
- **Automatic validation**: Detection and prevention of non-English text in SQL queries
- **Enhanced SQL validation**: Strict validation preventing Turkish/German/Russian characters in SQL
- **Multi-language query support**: AI handles queries in any language while generating pure English SQL
- **Character validation**: Detects non-English characters (Turkish: ç, ğ, ı, ö, ş, ü; German: ä, ö, ü, ß; Russian: Cyrillic)
- **Keyword validation**: Prevents non-English keywords in SQL (sorgu, abfrage, запрос)
- **Improved error messages**: Better diagnostics with database type information

#### PostgreSQL Full Support
- **Complete integration**: Full PostgreSQL support with live connections
- **Schema analysis**: Intelligent schema extraction and relationship mapping
- **Multi-database queries**: Cross-database query coordination with PostgreSQL
- **Production ready**: Comprehensive testing and validation

### 🔒 On-Premise & Local AI Support

#### Complete Local Operation
- **Local AI models**: Full support for Ollama, LM Studio, and OpenAI-compatible local APIs
- **Document processing**: PDF, Word, Excel parsing - completely local
- **OCR processing**: Tesseract 5.2.0 - completely local, no data sent to cloud
- **Database integration**: SQLite, SQL Server, MySQL, PostgreSQL - all local
- **Storage options**: In-Memory, SQLite, FileSystem, Redis - all local
- **Complete privacy**: Your data stays on your infrastructure

#### Enterprise Compliance
- **GDPR compliant**: Keep all data within your infrastructure
- **KVKK compliant**: Turkish data protection law compliance
- **Air-gapped systems**: Works without internet (except audio transcription)
- **Financial institutions**: Bank-grade security with local deployment
- **Healthcare**: HIPAA-compliant deployments possible
- **Government**: Classified data handling with local models

### ⚠️ Important Limitations

#### Audio Files
- **Google Speech-to-Text**: Audio transcription uses Google Cloud AI for enterprise-grade speech recognition
- **Whisper.net**: Local audio transcription option for privacy-sensitive deployments
- **Data privacy**: Whisper.net processes audio locally, Google Speech-to-Text sends to cloud
- **Multi-language**: Both providers support 99+ languages with automatic detection
- **Other formats**: All other file types remain completely local

#### OCR (Image to Text)
- **Handwriting limitation**: Tesseract OCR cannot fully support handwritten text (low success rate)
- **Works perfectly**: Printed documents, scanned printed documents, digital screenshots
- **Limited support**: Handwritten notes, forms, cursive writing (very low accuracy)
- **Best results**: High-quality scans of printed documents
- **100+ languages**: [View all supported languages](https://github.com/tesseract-ocr/tessdata)

### ✨ Added
- **Local AI setup examples**: Configuration for Ollama and LM Studio
- **Enterprise use cases**: Banking, Healthcare, Legal, Government, Manufacturing

### 🔧 Improved
- **Retry mechanism**: Enhanced retry prompts with language-specific instructions
- **Error handling**: Better error messages with database type information
- **Code quality**: SOLID/DRY principles maintained
- **Performance**: Optimized multi-database query coordination

### ✅ Quality Assurance
- **Zero Warnings Policy**: Maintained 0 errors, 0 warnings standard
- **SOLID Principles**: Clean code architecture
- **Comprehensive Testing**: Multi-database test coverage with PostgreSQL
- **Security hardening**: Enhanced credential protection
- **Performance optimization**: High performance across all features

### 🔄 Migration Guide (v2.3.0 → v3.0.0)

#### Service Layer Method Changes

**OLD (v2.3.0):**
```csharp
await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);
```

**NEW (v3.0.0):**
```csharp
await _documentSearchService.QueryIntelligenceAsync(query, maxResults);
```

#### Backward Compatibility
- Legacy methods are deprecated but still work (removed in v4.0.0)
- Update methods at your own pace
- No immediate breaking changes with old methods

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion231">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion231" aria-expanded="false" aria-controls="collapseversion231">
                <strong>v2.3.1</strong> - 2025-10-20
            </button>
        </h2>
        <div id="collapseversion231" class="accordion-collapse collapse" aria-labelledby="headingversion231" >
            <div class="accordion-body">
{% capture version_content %}

### 🐛 Bug Fixes
- **LoggerMessage Parameter Mismatch**: Fixed ServiceLogMessages.LogAudioServiceInitialized parameter mismatch
- **Format String Correction**: Corrected format string to prevent System.ArgumentException
- **Logging Stability**: Improved logging for Google Speech-to-Text initialization

### 🔧 Technical Improvements
- **Logging Infrastructure**: Enhanced reliability
- **Zero Warnings Policy**: Compliance maintained
- **Test Coverage**: All tests passing (8/8)

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion230">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion230" aria-expanded="false" aria-controls="collapseversion230">
                <strong>v2.3.0</strong> - 2025-09-16
            </button>
        </h2>
        <div id="collapseversion230" class="accordion-collapse collapse" aria-labelledby="headingversion230" >
            <div class="accordion-body">
{% capture version_content %}

### ✨ Added
- **Google Speech-to-Text Integration**: Enterprise-grade speech recognition
- **Enhanced Language Support**: 100+ languages including Turkish, English, global languages
- **Real-time Audio Processing**: Advanced speech-to-text with confidence scoring
- **Detailed Transcription Results**: Segment-level transcription with timestamps
- **Automatic Format Detection**: MP3, WAV, M4A, AAC, OGG, FLAC, WMA support
- **Intelligent Audio Processing**: Smart audio validation and error handling
- **Performance Optimized**: Efficient processing with minimal memory footprint
- **Structured Audio Output**: Searchable, queryable knowledge base
- **Comprehensive XML Documentation**: Complete API documentation

### 🔧 Improved
- **Audio Processing Pipeline**: Enhanced with Google Cloud AI
- **Configuration Management**: Updated to use GoogleSpeechConfig
- **Error Handling**: Enhanced for audio transcription

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion220">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion220" aria-expanded="false" aria-controls="collapseversion220">
                <strong>v2.2.0</strong> - 2025-09-15
            </button>
        </h2>
        <div id="collapseversion220" class="accordion-collapse collapse" aria-labelledby="headingversion220" >
            <div class="accordion-body">
{% capture version_content %}

### ✨ Added
- **Use Case Examples**: Scanned documents, receipts, image content

### 🔧 Improved
- **Package Metadata**: Updated project URLs and release notes

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion210">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion210" aria-expanded="false" aria-controls="collapseversion210">
                <strong>v2.1.0</strong> - 2025-09-05
            </button>
        </h2>
        <div id="collapseversion210" class="accordion-collapse collapse" aria-labelledby="headingversion210" >
            <div class="accordion-body">
{% capture version_content %}

### ✨ Added
- **Automatic Session Management**: No manual session ID handling
- **Persistent Conversation History**: Conversations survive restarts
- **New Conversation Commands**: `/new`, `/reset`, `/clear`
- **Enhanced API**: Backward-compatible with optional `startNewConversation`
- **Storage Integration**: Works with Redis, SQLite, FileSystem, InMemory

### 🔧 Improved
- **Format Consistency**: Standardized across storage providers
- **Thread Safety**: Enhanced concurrent access handling
- **Platform Agnostic**: Compatible across .NET environments

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion200">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion200" aria-expanded="false" aria-controls="collapseversion200">
                <strong>v2.0.0</strong> - 2025-08-27
            </button>
        </h2>
        <div id="collapseversion200" class="accordion-collapse collapse" aria-labelledby="headingversion200" >
            <div class="accordion-body">
{% capture version_content %}

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> BREAKING CHANGE</h4>
    <p class="mb-0">Migrated from .NET 9.0 to .NET Standard 2.1</p>
</div>

### 🔄 .NET Standard Migration
- **Target Framework**: Migrated from .NET 9.0 to .NET Standard 2.1
- **Framework Compatibility**: Now supports .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+
- **Maximum Reach**: Enhanced compatibility with legacy and enterprise environments

### ✨ Added
- **Cross-Platform Support**: .NET Standard 2.1 target frameworks
- **Legacy Framework Support**: Full .NET Framework compatibility
- **Enterprise Integration**: Seamless integration with existing enterprise solutions

### 🔧 Improved
- **Language Compatibility**: C# 7.3 syntax for .NET Standard 2.1
- **Package Versions**: Updated to .NET Standard compatible versions
- **API Compatibility**: Maintained functionality while ensuring framework compatibility

### 🧪 Testing
- **Framework Compatibility**: Verified .NET Standard 2.1 compatibility
- **Backward Compatibility**: All functionality remains intact
- **Package Compatibility**: Tested all NuGet packages

### 🔒 Security
- **Zero Warnings**: Maintained strict code quality
- **SOLID Principles**: Preserved enterprise-grade architecture
- **Package Security**: Updated packages for security vulnerabilities

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion110">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion110" aria-expanded="false" aria-controls="collapseversion110">
                <strong>v1.1.0</strong> - 2025-08-22
            </button>
        </h2>
        <div id="collapseversion110" class="accordion-collapse collapse" aria-labelledby="headingversion110" >
            <div class="accordion-body">
{% capture version_content %}

### ✨ Added
- **Excel Document Support**: Comprehensive Excel parsing (.xlsx, .xls)
- **EPPlus 8.1.0 Integration**: Modern Excel library with non-commercial license
- **Worksheet Parsing**: Intelligent parsing with tab-separated data preservation
- **Enhanced Content Validation**: Excel-specific fallback handling
- **Anthropic API Reliability**: Enhanced retry for HTTP 529 (Overloaded) errors

### 🔧 Improved
- **API Error Handling**: Better retry logic for rate limiting
- **Content Processing**: More robust document parsing
- **Performance**: Optimized Excel extraction and validation

### 🧪 Testing
- **Excel Parsing**: Verified with various Excel formats
- **API Retry**: Tested retry mechanism
- **Backward Compatibility**: All functionality remains intact

### 🔒 Security
- **License Compliance**: Proper EPPlus non-commercial license
- **Zero Warnings**: Maintained code quality standards

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion103">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion103" aria-expanded="false" aria-controls="collapseversion103">
                <strong>v1.0.3</strong> - 2025-08-20
            </button>
        </h2>
        <div id="collapseversion103" class="accordion-collapse collapse" aria-labelledby="headingversion103" >
            <div class="accordion-body">
{% capture version_content %}

### 🔧 Fixed
- LoggerMessage parameter count mismatches
- Provider logging message implementations
- Service collection registration issues

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion102">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion102" aria-expanded="false" aria-controls="collapseversion102">
                <strong>v1.0.2</strong> - 2025-08-19
            </button>
        </h2>
        <div id="collapseversion102" class="accordion-collapse collapse" aria-labelledby="headingversion102" >
            <div class="accordion-body">
{% capture version_content %}

### 📦 Package Release

#### **Release Notes**
- **Version Update**: Package version updated to 1.0.2
- **Package Metadata**: Updated release notes with v1.0.2 features

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion101">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion101" aria-expanded="false" aria-controls="collapseversion101">
                <strong>v1.0.1</strong> - 2025-08-17
            </button>
        </h2>
        <div id="collapseversion101" class="accordion-collapse collapse" aria-labelledby="headingversion101" >
            <div class="accordion-body">
{% capture version_content %}

### 🔧 Improved

- **Smart Query Intent Detection**: Enhanced query routing between chat and document search
- **Language-Agnostic Design**: Removed all hardcoded language patterns for global compatibility
- **Enhanced Search Relevance**: Improved name detection and content scoring algorithms
- **Unicode Normalization**: Fixed special character handling issues (e.g., Turkish characters)
- **Rate Limiting & Retry Logic**: Robust API handling with exponential backoff
- **VoyageAI Integration**: Optimized Anthropic embedding support

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingversion100">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseversion100" aria-expanded="false" aria-controls="collapseversion100">
                <strong>v1.0.0</strong> - 2025-08-15
            </button>
        </h2>
        <div id="collapseversion100" class="accordion-collapse collapse" aria-labelledby="headingversion100" >
            <div class="accordion-body">
{% capture version_content %}

### 🚀 Initial Release

#### **Features**
- **High-Performance RAG**: Multi-provider AI support implementation
- **5 AI Providers**: OpenAI, Anthropic, Gemini, Azure OpenAI, Custom
- **5 Storage Backends**: Qdrant, Redis, SQLite, FileSystem, InMemory
- **Document Formats**: PDF, Word, Text with intelligent parsing
- **Enterprise Architecture**: Dependency injection and clean architecture
- **CI/CD Pipeline**: Complete GitHub Actions workflow
- **Security**: CodeQL analysis and Codecov coverage reporting
- **NuGet Package**: Professional package with modern metadata

---
{% endcapture %}
{{ version_content | markdownify }}
            </div>
        </div>
    </div>
</div>

---

## Version History

<div class="table-responsive mt-4">
    <table class="table">
        <thead>
            <tr>
                <th>Version</th>
                <th>Date</th>
                <th>Highlights</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><strong>3.1.0</strong></td>
                <td>2025-11-11</td>
                <td>Unified Query Intelligence, Smart Hybrid Routing, New Service Architecture</td>
            </tr>
            <tr>
                <td><strong>3.0.3</strong></td>
                <td>2025-11-06</td>
                <td>Package Optimization - Native Libraries Excluded</td>
            </tr>
            <tr>
                <td><strong>3.0.0</strong></td>
                <td>2025-10-22</td>
                <td>Intelligence Library Revolution, SQL Generation, On-Premise Support, PostgreSQL</td>
            </tr>
            <tr>
                <td><strong>2.3.1</strong></td>
                <td>2025-10-08</td>
                <td>Bug fixes, Logging stability improvements</td>
            </tr>
            <tr>
                <td><strong>2.3.0</strong></td>
                <td>2025-09-16</td>
                <td>Google Speech-to-Text integration, Audio processing</td>
            </tr>
            <tr>
                <td><strong>2.2.0</strong></td>
                <td>2025-09-15</td>
                <td>OCR feature improvements</td>
            </tr>
            <tr>
                <td><strong>2.1.0</strong></td>
                <td>2025-09-05</td>
                <td>Automatic session management, Persistent conversation history</td>
            </tr>
            <tr>
                <td><strong>2.0.0</strong></td>
                <td>2025-08-27</td>
                <td>.NET Standard 2.1 migration</td>
            </tr>
            <tr>
                <td><strong>1.1.0</strong></td>
                <td>2025-08-22</td>
                <td>Excel support, EPPlus integration</td>
            </tr>
            <tr>
                <td><strong>1.0.3</strong></td>
                <td>2025-08-20</td>
                <td>Bug fixes and logging improvements</td>
            </tr>
            <tr>
                <td><strong>1.0.2</strong></td>
                <td>2025-08-19</td>
                <td>Initial stable release</td>
            </tr>
            <tr>
                <td><strong>1.0.1</strong></td>
                <td>2025-08-17</td>
                <td>Beta release</td>
            </tr>
            <tr>
                <td><strong>1.0.0</strong></td>
                <td>2025-08-15</td>
                <td>Initial release</td>
            </tr>
        </tbody>
    </table>
</div>

---