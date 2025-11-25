---
layout: default
title: Changelog
description: Complete version history, breaking changes, and migration guides for SmartRAG
lang: en
---


All notable changes to SmartRAG are documented here. The project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [3.2.0] - 2025-11-19

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

- **Custom SQL Dialect Strategies**: Support for implementing custom database dialects (e.g., Oracle)
- **Custom Scoring Strategies**: Support for implementing custom search relevance logic
- **Custom File Parsers**: Support for implementing custom file format parsers
- **Dedicated Conversation Management**: New service for managing conversation history

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
// Example: Implementing Oracle support
public class OracleDialectStrategy : BaseSqlDialectStrategy
{
    public override string GetDialectName() => "Oracle";
    
    public override string BuildSelectQuery(
        DatabaseSchemaInfo schema, 
        List<string> tables, 
        int maxRows)
    {
        // Oracle-specific SQL generation
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

## [3.1.0] - 2025-11-11

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

## [3.0.3] - 2025-11-06

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

## [3.0.2] - 2025-10-24

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

#### **Documentation Updates**
- **README.md**: Updated to reflect Whisper.net-only audio processing
- **README.tr.md**: Updated Turkish documentation
- **docs/**: Updated all documentation files to remove Google Speech references
- **Examples**: Updated example configurations and documentation

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

## [3.0.1] - 2025-10-22

### 🐛 Fixed
- **LoggerMessage Parameter Mismatch**: Fixed `LogAudioServiceInitialized` LoggerMessage definition with missing `configPath` parameter
- **EventId Conflicts**: Resolved duplicate EventId assignments in ServiceLogMessages.cs (6006, 6008, 6009)
- **Logo Display Issue**: Removed broken logo references from README files that were causing display issues on NuGet
- **TypeInitializationException**: Fixed critical startup error

### 🔧 Technical Improvements
- **ServiceLogMessages.cs**: Updated LoggerMessage definitions to match parameter counts correctly
- **EventId Management**: Reassigned conflicting EventIds to ensure unique logging identifiers
- **Documentation**: Cleaned up README files for better NuGet package display

---

## [3.0.0] - 2025-10-22

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
- **Multi-language README**: Available in English, Turkish, German, and Russian
- **Multi-language CHANGELOG**: Available in 4 languages
- **Enhanced documentation**: Comprehensive on-premise deployment docs
- **Local AI setup examples**: Configuration for Ollama and LM Studio
- **Enterprise use cases**: Banking, Healthcare, Legal, Government, Manufacturing

### 🔧 Improved
- **Retry mechanism**: Enhanced retry prompts with language-specific instructions
- **Error handling**: Better error messages with database type information
- **Documentation structure**: Cleaner README with CHANGELOG links
- **Code quality**: SOLID/DRY principles maintained
- **Performance**: Optimized multi-database query coordination

### 📚 Documentation
- **On-Premise guide**: Comprehensive deployment documentation
- **Privacy guide**: Data privacy and compliance documentation
- **OCR limitations**: Clear capabilities and limitations
- **Audio processing**: Clear requirements and limitations
- **Enterprise scenarios**: Real-world use cases

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

## [2.3.1] - 2025-10-20

### 🐛 Bug Fixes
- **LoggerMessage Parameter Mismatch**: Fixed ServiceLogMessages.LogAudioServiceInitialized parameter mismatch
- **Format String Correction**: Corrected format string to prevent System.ArgumentException
- **Logging Stability**: Improved logging for Google Speech-to-Text initialization

### 🔧 Technical Improvements
- **Logging Infrastructure**: Enhanced reliability
- **Zero Warnings Policy**: Compliance maintained
- **Test Coverage**: All tests passing (8/8)

---

## [2.3.0] - 2025-09-16

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
- **Documentation**: Updated with Speech-to-Text examples

### 📚 Documentation
- **Audio Processing**: Comprehensive feature documentation
- **Google Speech-to-Text**: Enhanced README with capabilities
- **Multi-language Support**: Highlighted 100+ language support
- **Developer Experience**: Better feature visibility

---

## [2.2.0] - 2025-09-15

### ✨ Added
- **Enhanced OCR Documentation**: Comprehensive with real-world use cases
- **Improved README**: Detailed image processing features
- **Use Case Examples**: Scanned documents, receipts, image content

### 🔧 Improved
- **Package Metadata**: Updated project URLs and release notes
- **Documentation Structure**: Enhanced OCR showcase
- **User Guidance**: Improved image processing workflows

### 📚 Documentation
- **OCR Capabilities**: Comprehensive with real-world examples
- **Image Processing**: Enhanced capabilities documentation
- **WebP Support**: Highlighted WebP to PNG conversion
- **Developer Experience**: Better visibility of features

---

## [2.1.0] - 2025-09-05

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

### 📚 Documentation
- **Multi-language Updates**: All languages (EN, TR, DE, RU) updated
- **100% Compliance**: All established rules maintained

---

## [2.0.0] - 2025-08-27

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

### 📚 Documentation
- **Framework Requirements**: Updated for .NET Standard
- **Installation Guide**: Updated package references
- **Migration Guide**: Comprehensive guide for .NET 9.0 users

### 🧪 Testing
- **Framework Compatibility**: Verified .NET Standard 2.1 compatibility
- **Backward Compatibility**: All functionality remains intact
- **Package Compatibility**: Tested all NuGet packages

### 🔒 Security
- **Zero Warnings**: Maintained strict code quality
- **SOLID Principles**: Preserved enterprise-grade architecture
- **Package Security**: Updated packages for security vulnerabilities

---

## [1.1.0] - 2025-08-22

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

### 📚 Documentation
- **Excel Format Support**: Comprehensive Excel processing documentation
- **API Reliability**: Updated error handling documentation
- **Installation Guide**: Updated package references

### 🧪 Testing
- **Excel Parsing**: Verified with various Excel formats
- **API Retry**: Tested retry mechanism
- **Backward Compatibility**: All functionality remains intact

### 🔒 Security
- **License Compliance**: Proper EPPlus non-commercial license
- **Zero Warnings**: Maintained code quality standards

---

## [1.0.3] - 2025-08-20

### 🔧 Fixed
- LoggerMessage parameter count mismatches
- Provider logging message implementations
- Service collection registration issues

### 📚 Documentation
- Updated README with latest features
- Improved installation instructions

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
                <td>Enhanced OCR documentation</td>
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

## Migration Guides

### Migrating from v2.x to v3.0.0
                    
                    <div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Key Changes</h4>
    <p>The primary change is the renaming of <code>GenerateRagAnswerAsync</code> to <code>QueryIntelligenceAsync</code>.</p>
                    </div>

**Step 1: Update method calls**

```csharp
// Before (v2.x)
var response = await _searchService.GenerateRagAnswerAsync(query, maxResults);

// After (v3.0.0)
var response = await _searchService.QueryIntelligenceAsync(query, maxResults);
```

**Step 2: Update API endpoints (if using Web API)**

```csharp
// Before
[HttpPost("generate-answer")]
public async Task<IActionResult> GenerateAnswer([FromBody] QueryRequest request)
{
    var response = await _searchService.GenerateRagAnswerAsync(request.Query);
    return Ok(response);
}

// After
[HttpPost("query")]
public async Task<IActionResult> Query([FromBody] QueryRequest request)
{
    var response = await _searchService.QueryIntelligenceAsync(request.Query);
    return Ok(response);
}
```

**Step 3: Update client code (if applicable)**

```javascript
// Before
const response = await fetch('/api/intelligence/generate-answer', { ... });

// After
const response = await fetch('/api/intelligence/query', { ... });
```

<div class="alert alert-success">
    <h4><i class="fas fa-check-circle me-2"></i> No Immediate Action Required</h4>
    <p class="mb-0">
        The old <code>GenerateRagAnswerAsync</code> method still works (marked as deprecated). 
        You can migrate gradually before v4.0.0 is released.
    </p>
                    </div>

### Migrating from v1.x to v2.0.0

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Framework Change</h4>
    <p class="mb-0">Version 2.0.0 migrated from .NET 9.0 to .NET Standard 2.1</p>
                </div>

**Step 1: Verify framework compatibility**

```xml
<!-- Your project must target one of these frameworks -->
<TargetFramework>netstandard2.0</TargetFramework>
<TargetFramework>netstandard2.1</TargetFramework>
<TargetFramework>netcoreapp2.0</TargetFramework>
<TargetFramework>net461</TargetFramework>
<TargetFramework>net5.0</TargetFramework>
<TargetFramework>net6.0</TargetFramework>
<TargetFramework>net7.0</TargetFramework>
<TargetFramework>net8.0</TargetFramework>
<TargetFramework>net9.0</TargetFramework>
```

**Step 2: Update NuGet package**

```bash
dotnet add package SmartRAG --version 2.0.0
```

**Step 3: Verify code compatibility**

No API changes - all functionality remains the same. Just ensure your project targets compatible framework.

---

## Deprecation Notices

### Deprecated in v3.0.0 (Removed in v4.0.0)

<div class="alert alert-warning">
    <h4><i class="fas fa-clock me-2"></i> Planned for Removal</h4>
    <p>The following methods are deprecated and will be removed in v4.0.0:</p>
    <ul class="mb-0">
        <li><code>IDocumentSearchService.GenerateRagAnswerAsync()</code> - Use <code>QueryIntelligenceAsync()</code> instead</li>
                    </ul>
                </div>

---

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card">
            <div class="feature-icon">
                <i class="fas fa-rocket"></i>
                    </div>
            <h3>Getting Started</h3>
            <p>Install SmartRAG and start building intelligent applications</p>
            <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-outline-primary btn-sm mt-3">
                Get Started
            </a>
                </div>
            </div>
    
    <div class="col-md-6">
        <div class="feature-card">
            <div class="feature-icon">
                <i class="fab fa-github"></i>
            </div>
            <h3>GitHub Repository</h3>
            <p>View source code, report issues, and contribute</p>
            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-outline-primary btn-sm mt-3" target="_blank">
                View on GitHub
            </a>
                    </div>
                </div>
            </div>

