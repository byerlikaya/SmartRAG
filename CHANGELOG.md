
# Changelog

All notable changes to SmartRAG will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
- **Google Cloud required**: Audio transcription requires Google Cloud Speech-to-Text API
- **Data privacy**: Audio files will be sent to Google Cloud for processing
- **Alternative**: Avoid uploading audio files if data privacy is critical
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

- **3.0.0** (2025-10-18) - Intelligence Library Revolution, SQL Generation, On-Premise Support
- **2.3.1** (2025-10-08) - Bug fixes, Logging stability improvements
- **2.3.0** (2025-09-16) - Google Speech-to-Text integration, Audio processing
- **2.2.0** (2025-09-15) - Enhanced OCR documentation
- **2.1.0** (2025-09-05) - Automatic session management, Persistent conversation history
- **2.0.0** (2025-08-27) - .NET Standard 2.0/2.1 migration
- **1.1.0** (2025-08-22) - Excel support, EPPlus integration
- **1.0.3** (2025-08-20) - Bug fixes and logging improvements
- **1.0.2** (2025-08-19) - Initial stable release
- **1.0.1** (2025-08-17) - Beta release
- **1.0.0** (2025-08-15) - Initial release
