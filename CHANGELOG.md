# Changelog

All notable changes to SmartRAG will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.3.1] - 2025-10-08

### 🐛 Fixed
- **LoggerMessage Parameter Mismatch**: Fixed `System.ArgumentException` in `ServiceLogMessages.LogAudioServiceInitialized` where format string expected 1 parameter but had 0
- **Service Initialization**: Corrected LoggerMessage.Define signature to match actual usage pattern
- **Logging Stability**: Improved logging infrastructure reliability for Google Speech-to-Text service

### 🔧 Improved
- **Code Quality**: Maintained zero warnings policy compliance
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

## [2.0.0] - 2025-09-10

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

- **2.0.0** - .NET Standard 2.0/2.1 migration, maximum framework compatibility
- **1.1.0** - Excel support, EPPlus integration, API reliability improvements
- **1.0.3** - Bug fixes and logging improvements
- **1.0.2** - Initial stable release
- **1.0.1** - Beta release with core functionality
- **1.0.0** - Initial release
