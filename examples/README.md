# SmartRAG Examples

This directory contains example projects demonstrating how to use SmartRAG in different scenarios.

## 📁 Available Examples

### **SmartRAG.API** - ASP.NET Core Web API Example
- **Location**: `SmartRAG.API/`
- **Description**: Complete web API implementation showing document upload, search, and RAG operations

- **Features**: 
  - Multi-document upload (PDF, Word, Excel, text files)
  - Image processing with OCR support (.jpg, .png, .gif, .bmp, .tiff, .webp)
  - Audio processing with Google Speech-to-Text (.mp3, .wav, .m4a, .aac, .ogg, .flac, .wma)
  - AI-powered question answering
  - Smart query intent detection
  - Conversation history management
  - Multiple storage providers (Qdrant, Redis, SQLite, FileSystem, InMemory)
  - Enhanced semantic search with hybrid scoring
  - Comprehensive API documentation

### **SmartRAG.Console** - Console Chat Application
- **Location**: `SmartRAG.Console/`
- **Description**: Interactive console application for AI-powered conversations using SmartRAG
- **Features**:
  - Real-time AI chat with multiple providers (OpenAI, Anthropic, Gemini, Azure OpenAI, Custom)
  - Smart query intent detection (general conversation vs document search)
  - Conversation history management with session persistence
  - Multi-language support (Turkish, English, German, etc.)
  - Easy configuration switching between AI providers
  - Simple command-line interface for testing and development

## 🚀 Running Examples

### SmartRAG.API Example
```bash
cd examples/SmartRAG.API
dotnet restore
dotnet run
```

Browse to `https://localhost:5001/swagger` for interactive API documentation.

### SmartRAG.Console Example
```bash
cd examples/SmartRAG.Console
dotnet restore
dotnet run
```

Start chatting with AI! Type `exit` to quit.

## 🔧 Configuration

Each example includes its own configuration files. Copy and modify the template files as needed:

```bash
# Copy development configuration template
cp appsettings.Development.template.json appsettings.Development.json

# Edit with your API keys and configuration
```

## 📚 Documentation

- **Main Documentation**: [SmartRAG README](../../README.md)
- **API Reference**: [API Documentation](../../docs/api-reference.md)
- **Configuration Guide**: [Configuration Guide](../../docs/configuration.md)

## 🤝 Contributing

Want to add more examples? Create a new directory and submit a pull request!

### Example Types to Consider:
- **Blazor WebAssembly** - Client-side web app with image and audio upload
- **WPF Application** - Desktop application with document and audio processing
- **Azure Functions** - Serverless implementation with vector search
- **Minimal API** - Lightweight web API with conversation management
- **OCR Service** - Standalone OCR processing service
- **Speech-to-Text Service** - Standalone audio transcription service
- **Document Analyzer** - Advanced document analysis with table extraction
- **Mobile App** - Cross-platform mobile application with SmartRAG integration

## 📞 Support

For questions, issues, or contributions, please visit our [GitHub repository](https://github.com/byerlikaya/SmartRAG).

### Contact Information
- **📧 [Contact & Support](mailto:b.yerlikaya@outlook.com)**
- **💼 [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)**
- **🐙 [GitHub Profile](https://github.com/byerlikaya)**
- **📦 [NuGet Packages](https://www.nuget.org/profiles/barisyerlikaya)**
- **📖 [Documentation](https://byerlikaya.github.io/SmartRAG)** - Comprehensive guides and API reference

---
**Made in Turkey 🇹🇷 | [Contact](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)**
