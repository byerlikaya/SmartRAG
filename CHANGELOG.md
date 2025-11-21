
# Changelog

All notable changes to SmartRAG will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Performance Improvements
- **Optimized AI Query Intent Analysis**: Eliminated redundant AI calls by adding overload method to `QueryMultipleDatabasesAsync` that accepts pre-analyzed query intent
  - `IMultiDatabaseQueryCoordinator.QueryMultipleDatabasesAsync(string, QueryIntent, int)` - New overload method to avoid redundant AI analysis
  - `DocumentSearchService` now passes pre-analyzed query intent to `MultiDatabaseQueryCoordinator` to prevent duplicate AI calls
  - **Files Modified**:
    - `src/SmartRAG/Interfaces/Database/IMultiDatabaseQueryCoordinator.cs` - Added overload method with pre-analyzed intent parameter
    - `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Implemented overload method with null safety validation
    - `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Updated to pass pre-analyzed query intent to coordinator

### Fixed
- **SQL Query Validation**: Fixed ORDER BY alias validation to correctly handle SELECT aliases in GROUP BY queries
  - Validation now recognizes SELECT aliases (e.g., `SUM(Quantity) AS TotalQuantity`) in ORDER BY clauses
  - Previously flagged valid SQL as errors when using aggregate aliases in ORDER BY
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Enhanced validation logic to extract and validate SELECT aliases

### Improved
- **Cross-Database Query Prompt Enhancement**: Improved AI prompt guidance for cross-database queries
  - Added clearer examples for handling relationships across databases (e.g., "most sold category" requires sales data + category names)
  - Enhanced guidance on returning foreign keys and aggregates for application-level merging
  - **Files Modified**:
    - `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Updated cross-database query pattern examples in AI prompts

- **Async/Await Pattern Improvements**: Eliminated blocking calls (`.Result`, `.Wait()`) and improved thread safety
  - `AudioConversionService` now uses `SemaphoreSlim` instead of `lock` for async initialization
  - `SqliteConversationRepository` now uses `SemaphoreSlim` for thread-safe async operations
  - `DocumentService.GetStorageStatisticsAsync` properly uses `await` instead of `.Result`
  - **Files Modified**:
    - `src/SmartRAG/Services/Parser/AudioConversionService.cs` - Replaced `lock` + `.Wait()` with `SemaphoreSlim`
    - `src/SmartRAG/Repositories/SqliteConversationRepository.cs` - Replaced `lock` + `.Result` with `SemaphoreSlim`
    - `src/SmartRAG/Services/Document/DocumentService.cs` - Changed `GetStorageStatisticsAsync` to use `await`
  - **Benefits**: Better async/await compliance, improved thread safety, no blocking operations


### Changed
- **Code Architecture Refactoring**: Services and interfaces reorganized into modular folder structure for better organization and maintainability
  - Interfaces organized by category: `AI/`, `Database/`, `Document/`, `Parser/`, `Search/`, `Storage/`, `Support/`
  - Services organized by category: `AI/`, `Database/`, `Document/`, `Parser/`, `Search/`, `Storage/Qdrant/`, `Support/`, `Shared/`
  - Namespaces updated: `SmartRAG.Interfaces` → `SmartRAG.Interfaces.{Category}`, `SmartRAG.Services` → `SmartRAG.Services.{Category}`
  - File paths updated:
    - `src/SmartRAG/Services/MultiDatabaseQueryCoordinator.cs` → `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs`
    - `src/SmartRAG/Services/DocumentSearchService.cs` → `src/SmartRAG/Services/Document/DocumentSearchService.cs`
    - `src/SmartRAG/Services/AIService.cs` → `src/SmartRAG/Services/AI/AIService.cs`
    - `src/SmartRAG/Services/SemanticSearchService.cs` → `src/SmartRAG/Services/Search/SemanticSearchService.cs`
    - All interfaces moved from `src/SmartRAG/Interfaces/` to `src/SmartRAG/Interfaces/{Category}/`
  - **Breaking Changes**: Namespace changes may require using statement updates in consuming code
  - **Benefits**: Better code organization, improved maintainability, clearer separation of concerns

### Added
- **Unified Query Intelligence**: `QueryIntelligenceAsync` now supports unified search across databases, documents, images (OCR), and audio (transcription) in a single query
- **Smart Hybrid Routing**: AI-based intent detection with confidence scoring automatically determines optimal search strategy
  - High confidence (>0.7) + database queries → Database query only
  - High confidence (>0.7) + no database queries → Document query only
  - Medium confidence (0.3-0.7) → Both database and document queries, merged results
  - Low confidence (<0.3) → Document query only (fallback)
- **QueryStrategy Enum**: New enum for query execution strategies (DatabaseOnly, DocumentOnly, Hybrid)

### Changed
- `QueryIntelligenceAsync` method now integrates database queries alongside document queries
- Improved query routing logic with graceful degradation and fallback mechanisms
- Enhanced error handling for database query failures

### Notes
- Backward compatible: Existing `QueryIntelligenceAsync` signature unchanged
- If database coordinator not available, behavior identical to previous implementation
- No breaking changes to `RagResponse` model

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
- **Intelligent Routing**: Improved query routing logic with graceful degradation and fallback mechanisms
- **Enhanced Error Handling**: Better error handling for database query failures

#### **New Services & Interfaces**
- `src/SmartRAG/Services/Database/QueryIntentAnalyzer.cs` - Analyzes user queries and determines which databases/tables to query using AI
- `src/SmartRAG/Services/Database/DatabaseQueryExecutor.cs` - Executes queries across multiple databases in parallel for better performance
- `src/SmartRAG/Services/Database/ResultMerger.cs` - Merges results from multiple databases into coherent responses using AI
- `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Generates optimized SQL queries for each database based on query intent
- `src/SmartRAG/Interfaces/Database/IQueryIntentAnalyzer.cs` - Interface for query intent analysis
- `src/SmartRAG/Interfaces/Database/IDatabaseQueryExecutor.cs` - Interface for multi-database query execution
- `src/SmartRAG/Interfaces/Database/IResultMerger.cs` - Interface for result merging
- `src/SmartRAG/Interfaces/Database/ISQLQueryGenerator.cs` - Interface for SQL query generation

#### **New Enums**
- `src/SmartRAG/Enums/QueryStrategy.cs` - New enum for query execution strategies (DatabaseOnly, DocumentOnly, Hybrid)

#### **New Models**
- `src/SmartRAG/Models/AudioSegmentMetadata.cs` - Metadata model for audio transcription segments with timestamps and confidence scores

#### **Enhanced Models**
- `src/SmartRAG/Models/SearchSource.cs` - Enhanced with source type differentiation (Database, Document, Image, Audio)

#### **Files Modified**
- `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Major refactoring: Unified query intelligence implementation with hybrid routing (918+ lines changed)
- `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Refactored to use new service architecture for better separation of concerns (355+ lines changed)
- `src/SmartRAG/Services/AI/AIService.cs` - Enhanced AI service with better error handling
- `src/SmartRAG/Services/Document/DocumentParserService.cs` - Improved document parsing with audio segment metadata support
- `src/SmartRAG/Interfaces/Document/IDocumentSearchService.cs` - Updated interface documentation
- `src/SmartRAG/Extensions/ServiceCollectionExtensions.cs` - Registered new services in DI container

### 🔧 Code Quality & AI Prompt Optimization

#### **Code Quality Improvements**
- **Build Quality**: Achieved 0 errors, 0 warnings across all projects
- **Code Standards**: Full compliance with project coding standards

#### **AI Prompt Optimization**
- **Emoji Reduction**: Reduced emoji usage in AI prompts from 235 to 5 (only critical: 🚨, ✓, ✗)
- **Token Efficiency**: Improved token efficiency (~100 tokens saved per prompt)
- **Strategic Usage**: Better AI comprehension through strategic emoji usage

#### **Files Modified**
- `src/SmartRAG/Services/Database/SQLQueryGenerator.cs` - Emoji optimization in AI prompts
- `src/SmartRAG/Services/Database/MultiDatabaseQueryCoordinator.cs` - Emoji optimization
- `src/SmartRAG/Services/Database/QueryIntentAnalyzer.cs` - Emoji optimization
- `src/SmartRAG/Services/Document/DocumentSearchService.cs` - Emoji optimization

### ✨ Benefits
- **Single Query Interface**: Query all data sources (databases, documents, images, audio) with one method
- **Intelligent Routing**: AI automatically selects the best search strategy based on query intent and confidence scoring
- **Parallel Execution**: Multi-database queries execute in parallel for better performance
- **Modular Architecture**: New service-based architecture improves maintainability and testability
- **Better Separation of Concerns**: Each service has a single responsibility (SOLID principles)
- **Cleaner Codebase**: Zero warnings across all projects
- **Better Performance**: More efficient AI prompt processing and parallel query execution
- **Improved Maintainability**: Better code quality and standards compliance
- **Cost Efficiency**: Reduced token usage in AI prompts (~100 tokens saved per prompt)

### 📝 Notes
- Backward compatible: Existing `QueryIntelligenceAsync` signature unchanged
- If database coordinator not available, behavior identical to previous implementation
- No breaking changes to `RagResponse` model

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

## [3.0.0] - 2025-10-22

### 🚀 BREAKING CHANGES - Intelligence Library Revolution

#### **Framework Requirements**
- **Minimum .NET Version**: Now requires .NET Standard 2.1 (.NET Core 3.0+)
- **Dropped Support**: .NET Framework 4.x and .NET Standard 2.0 no longer supported
- **Reason**: Enable modern API features, better performance, and align with current AI provider SDK requirements
- **Compatible With**: .NET Core 3.0+, .NET 5, .NET 6, .NET 7, .NET 8, .NET 9

#### **Major API Changes**
- **`GenerateRagAnswerAsync` → `QueryIntelligenceAsync`**: Method renamed to better represent intelligent query processing
- **Enhanced `IDocumentSearchService` interface**: New intelligent query processing method with advanced RAG pipeline
- **Service layer improvements**: Advanced semantic search and conversation management
- **Backward compatibility maintained**: Legacy methods marked as deprecated (will be removed in v4.0.0)

### 🔧 SQL Generation & Multi-Language Support

#### **Language-Safe SQL Generation**
- **Automatic validation**: Detection and prevention of non-English text in SQL queries
- **Enhanced SQL validation**: Strict validation preventing Turkish/German/Russian characters and keywords in SQL
- **Multi-language query support**: AI handles queries in any language while generating pure English SQL
- **Character validation**: Detection of non-English characters (Turkish: ç, ğ, ı, ö, ş, ü; German: ä, ö, ü, ß; Russian: Cyrillic)
- **Keyword validation**: Prevention of non-English keywords in SQL (sorgu, abfrage, запрос)
- **Improved error messages**: Better diagnostics with database type information in error reports

#### **PostgreSQL Full Support**
- **Complete integration**: Full PostgreSQL database support with live connections
- **Schema analysis**: Intelligent schema extraction and relationship mapping
- **Multi-database queries**: Cross-database query coordination with PostgreSQL
- **Production ready**: Comprehensive testing and validation

### 🔒 On-Premise & Local AI Support

#### **Complete Local Operation**
- **Local AI models**: Full support for Ollama, LM Studio, and any OpenAI-compatible local API
- **Document processing**: PDF, Word, Excel parsing - completely local
- **OCR processing**: Tesseract 5.2.0 - completely local, no data sent to cloud
- **Database integration**: SQLite, SQL Server, MySQL, PostgreSQL - all local connections
- **Storage options**: In-Memory, SQLite, FileSystem, Redis - all local
- **Complete privacy**: All your data stays on your infrastructure

#### **Enterprise Compliance**
- **GDPR compliant**: Keep all data within your infrastructure
- **KVKK compliant**: Turkish data protection law compliance
- **Air-gapped systems**: Works without internet (except for audio transcription)
- **Financial institutions**: Bank-grade security with local deployment
- **Healthcare**: HIPAA-compliant deployments possible
- **Government**: Classified data handling with local models

### ⚠️ Important Limitations Documented

#### **Audio Files**
- **Google Speech-to-Text**: Audio transcription uses Google Cloud AI for enterprise-grade speech recognition
- **Whisper.net**: Local audio transcription option for privacy-sensitive deployments
- **Data privacy**: Whisper.net processes audio locally, Google Speech-to-Text sends to cloud
- **Multi-language**: Both providers support 99+ languages with automatic detection
- **Other formats**: All other file types (PDF, Word, Excel, Images, Databases) remain completely local

#### **OCR (Image to Text)**
- **Handwriting limitation**: Tesseract OCR library cannot fully support handwritten text (success rate is very low)
- **Works perfectly**: Printed documents, scanned printed documents, digital screenshots with typed text
- **Limited support**: Handwritten notes, handwritten forms, cursive writing (very low accuracy, not recommended)
- **Best results**: High-quality scans of printed documents, clear digital images with printed text
- **Supported languages**: 100+ languages - [View all supported languages](https://github.com/tesseract-ocr/tessdata)
- **Recommendation**: Use printed text documents for optimal OCR results

### ✨ Added
- **Multi-language README support**: README files now available in English, Turkish, German, and Russian
- **Multi-language CHANGELOG support**: CHANGELOG files now available in 4 languages
- **Enhanced documentation**: Comprehensive on-premise deployment documentation
- **Local AI setup examples**: Configuration examples for Ollama and LM Studio
- **Enterprise use cases**: Documented use cases for Banking, Healthcare, Legal, Government, Manufacturing, and Consulting

### 🔧 Improved
- **Retry mechanism**: Enhanced retry prompts with language-specific instructions
- **Error handling**: Better error messages with database type information
- **Documentation structure**: Cleaner README structure with CHANGELOG links
- **Code quality**: SOLID/DRY principles maintained throughout
- **Performance**: Optimized multi-database query coordination

### 📚 Documentation
- **On-Premise guide**: Comprehensive on-premise deployment documentation
- **Privacy guide**: Data privacy and compliance documentation
- **OCR limitations**: Clear documentation of OCR capabilities and limitations
- **Audio processing notes**: Clear documentation of audio processing requirements
- **Multi-language support**: All documentation available in 4 languages
- **Enterprise scenarios**: Documented real-world enterprise use cases

### ✅ Quality Assurance
- **Zero Warnings Policy**: All changes maintain 0 errors, 0 warnings standard
- **SOLID Principles**: Clean code architecture maintained throughout
- **Comprehensive Testing**: Multi-database test coverage with PostgreSQL integration
- **Security hardening**: Enhanced configuration file management and credential protection
- **Performance optimization**: Maintained high performance across all features

### 🔄 Migration Guide (v2.3.0 → v3.0.0)

#### **Service Layer Method Changes**
```csharp
// OLD (v2.3.0)
await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);

// NEW (v3.0.0)  
await _documentSearchService.QueryIntelligenceAsync(query, maxResults);
```

#### **Backward Compatibility**
- Legacy methods are deprecated but still work (will be removed in v4.0.0)
- Update endpoints and methods at your own pace
- No immediate breaking changes if you continue using old methods

## [2.3.1] - 2025-10-20

### 🐛 Bug Fixes
- **LoggerMessage Parameter Mismatch**: Fixed LoggerMessage.Define parameter mismatch in ServiceLogMessages.LogAudioServiceInitialized
- **Format String Correction**: Corrected format string parameter count to prevent System.ArgumentException during service initialization
- **Logging Stability**: Improved logging stability for Google Speech-to-Text service initialization

### 🔧 Technical Improvements
- **Logging Infrastructure**: Enhanced logging infrastructure reliability
- **Zero Warnings Policy**: Compliance maintained
- **Test Coverage**: All tests passing (8/8)

## [2.3.0] - 2025-09-16

### ✨ Added
- **Google Speech-to-Text Integration**: Enterprise-grade speech recognition with Google Cloud AI
- **Enhanced Language Support**: 100+ languages including Turkish, English, and global languages
- **Real-time Audio Processing**: Advanced speech-to-text conversion with confidence scoring
- **Detailed Transcription Results**: Segment-level transcription with timestamps and confidence metrics
- **Automatic Format Detection**: Support for MP3, WAV, M4A, AAC, OGG, FLAC, WMA formats
- **Intelligent Audio Processing**: Smart audio stream validation and error handling
- **Performance Optimized**: Efficient audio processing with minimal memory footprint
- **Structured Audio Output**: Converts audio content to searchable, queryable knowledge base
- **Comprehensive XML Documentation**: Complete API documentation for all public classes and methods

### 🔧 Improved
- **Audio Processing Pipeline**: Enhanced audio processing with Google Cloud AI
- **Configuration Management**: Updated all configuration files to use GoogleSpeechConfig
- **Error Handling**: Enhanced error handling for audio transcription operations
- **Documentation**: Updated all language versions with Google Speech-to-Text examples

### 📚 Documentation
- **Audio Processing**: Comprehensive audio processing feature documentation
- **Google Speech-to-Text**: Enhanced README with detailed speech-to-text capabilities
- **Multi-language Support**: Highlighted 100+ language support for global applications
- **Developer Experience**: Better visibility of audio processing features for developers

## [2.2.0] - 2025-09-15

### ✨ Added
- **Enhanced OCR Documentation**: Comprehensive documentation showcasing OCR capabilities with real-world use cases
- **Improved README**: Detailed image processing features highlighting Tesseract 5.2.0 + SkiaSharp integration
- **Use Case Examples**: Added detailed examples for scanned documents, receipts, and image content processing

### 🔧 Improved
- **Package Metadata**: Updated project URLs and release notes for better user experience
- **Documentation Structure**: Enhanced documentation showcasing OCR as key differentiator
- **User Guidance**: Improved guidance for image-based document processing workflows

### 📚 Documentation
- **OCR Capabilities**: Comprehensive OCR feature documentation with real-world examples
- **Image Processing**: Enhanced README with detailed image processing capabilities
- **WebP Support**: Highlighted WebP to PNG conversion and multi-language OCR support
- **Developer Experience**: Better visibility of image processing features for developers

## [2.1.0] - 2025-09-05

### ✨ Added
- **Automatic Session Management**: No more manual session ID handling required
- **Persistent Conversation History**: Conversations survive application restarts
- **New Conversation Commands**: `/new`, `/reset`, `/clear` for conversation control
- **Enhanced API**: Backward-compatible with optional `startNewConversation` parameter
- **Storage Integration**: Works seamlessly with all providers (Redis, SQLite, FileSystem, InMemory)

### 🔧 Improved
- **Format Consistency**: Standardized conversation format across all storage providers
- **Thread Safety**: Enhanced concurrent access handling for conversation operations
- **Platform Agnostic**: Maintains compatibility across all .NET environments

### 📚 Documentation
- **Multi-language Updates**: All language versions (EN, TR, DE, RU) updated with real examples
- **100% Compliance**: All established rules maintained with zero warnings policy

## [2.0.0] - 2025-08-27

### 🔄 **BREAKING CHANGE: .NET Standard Migration**
- **Target Framework**: Migrated from .NET 9.0 to .NET Standard 2.0/2.1
- **Framework Compatibility**: Now supports .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+
- **Maximum Reach**: Enhanced compatibility with legacy and enterprise environments

### ✨ Added
- **Cross-Platform Support**: .NET Standard 2.0/2.1 target frameworks for maximum compatibility
- **Legacy Framework Support**: Full compatibility with .NET Framework applications
- **Enterprise Integration**: Seamless integration with existing enterprise .NET solutions

### 🔧 Improved
- **Language Compatibility**: C# 7.3 syntax compatibility for .NET Standard 2.0/2.1
- **Package Versions**: Updated all NuGet packages to .NET Standard compatible versions
- **API Compatibility**: Maintained all existing functionality while ensuring framework compatibility

### 📚 Documentation
- **Framework Requirements**: Updated documentation for .NET Standard compatibility
- **Installation Guide**: Updated package references and framework requirements
- **Migration Guide**: Comprehensive guide for existing .NET 9.0 users

### 🧪 Testing
- **Framework Compatibility**: Verified compatibility with .NET Standard 2.0/2.1
- **Backward Compatibility**: Ensured all existing functionality remains intact
- **Package Compatibility**: Tested all NuGet packages with target frameworks

### 🔒 Security
- **Zero Warnings**: Maintained strict code quality standards
- **SOLID Principles**: Preserved enterprise-grade architecture
- **Package Security**: Updated packages to address security vulnerabilities

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

## [1.0.3] - Previous Release

### 🔧 Fixed
- LoggerMessage parameter count mismatches
- Provider logging message implementations
- Service collection registration issues

### 📚 Documentation
- Updated README with latest features
- Improved installation instructions

---

## Version History

- **3.1.0** (2025-11-11) - Unified Query Intelligence, Smart Hybrid Routing, New Service Architecture
- **3.0.3** (2025-11-06) - Package Optimization - Native Libraries Excluded
- **3.0.2** (2025-10-24) - Google Speech-to-Text removal, Whisper.net only
- **3.0.1** (2025-10-22) - Bug fixes, Logging stability improvements
- **3.0.0** (2025-10-22) - Intelligence Library Revolution, SQL Generation, On-Premise Support
- **2.3.1** (2025-10-20) - Bug fixes, Logging stability improvements
- **2.3.0** (2025-09-16) - Google Speech-to-Text integration, Audio processing
- **2.2.0** (2025-09-15) - Enhanced OCR documentation
- **2.1.0** (2025-09-05) - Automatic session management, Persistent conversation history
- **2.0.0** (2025-08-27) - .NET Standard 2.0/2.1 migration
- **1.1.0** (2025-08-22) - Excel support, EPPlus integration
- **1.0.3** (2025-08-20) - Bug fixes and logging improvements
- **1.0.2** (2025-08-19) - Initial stable release
- **1.0.1** (2025-08-17) - Beta release
- **1.0.0** (2025-08-15) - Initial release
