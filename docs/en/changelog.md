---
layout: default
title: Changelog
description: Complete version history, breaking changes, and migration guides for SmartRAG
lang: en
---


All notable changes to SmartRAG are documented here. The project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
- **TypeInitializationException**: Fixed critical startup error that prevented SmartRAG.Demo from running

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

