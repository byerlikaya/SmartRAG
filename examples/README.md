# SmartRAG Examples

This directory contains example projects demonstrating how to use SmartRAG in different scenarios.

## 📁 Available Examples

### **WebAPI** - ASP.NET Core Web API Example
- **Location**: `WebAPI/`
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

## 🚀 Running Examples

### WebAPI Example
```bash
cd examples/WebAPI
dotnet restore
dotnet run
```

Browse to `https://localhost:5001/swagger` for interactive API documentation.

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
- **Console Application** - Command-line interface with OCR and Speech-to-Text support
- **Blazor WebAssembly** - Client-side web app with image and audio upload
- **WPF Application** - Desktop application with document and audio processing
- **Azure Functions** - Serverless implementation with vector search
- **Minimal API** - Lightweight web API with conversation management
- **OCR Service** - Standalone OCR processing service
- **Speech-to-Text Service** - Standalone audio transcription service
- **Document Analyzer** - Advanced document analysis with table extraction

## 📞 Support

For questions, issues, or contributions, please visit our [GitHub repository](https://github.com/byerlikaya/SmartRAG).

### Contact Information
- **📧 [Contact & Support](mailto:b.yerlikaya@outlook.com)**
- **💼 [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)**
- **🐙 [GitHub Profile](https://github.com/byerlikaya)**
- **📦 [NuGet Packages](https://www.nuget.org/profiles/byerlikaya)**
- **📖 [Documentation](https://byerlikaya.github.io/SmartRAG)** - Comprehensive guides and API reference

---
**Made in Turkey 🇹🇷 | [Contact](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)**
