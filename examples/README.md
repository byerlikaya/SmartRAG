# SmartRAG Examples

This directory contains example projects demonstrating how to use SmartRAG in different scenarios.

## 📁 Available Examples

### **SmartRAG.API** - ASP.NET Core Web API Example
- **Location**: `SmartRAG.API/`
- **Description**: Complete web API implementation showing document upload, search, and RAG operations

- **Features**: 
  - **Unified Query Intelligence**: Single endpoint searches across documents, images (OCR), audio (transcription), and databases
  - Multi-document upload (PDF, Word, Excel, text files)
  - Image processing with OCR support (.jpg, .png, .gif, .bmp, .tiff, .webp)
  - Audio processing with Whisper.net (local, 99+ languages)
  - AI-powered question answering with Smart Hybrid routing
  - Smart query intent detection with confidence-based routing
  - Automatic source selection (database, documents, or both)
  - Conversation history management
  - Multiple storage providers (Qdrant, Redis, SQLite, FileSystem, InMemory)
  - Enhanced semantic search with hybrid scoring
  - Comprehensive API documentation

### **SmartRAG.Demo** - Interactive Multi-Database RAG Demo
- **Location**: `SmartRAG.Demo/`
- **Description**: Comprehensive demo showcasing SmartRAG's deployment flexibility and multi-database capabilities
- **Features**:
  - **Unified Query Intelligence**: Single query searches across documents, images, audio, and databases automatically
  - **Deployment Modes**: 100% Local, 100% Cloud, or Hybrid configurations
  - **Smart Hybrid Routing**: AI automatically determines whether to search databases, documents, or both
  - **Multi-Database Queries**: Cross-database natural language queries (SQL Server, MySQL, PostgreSQL, SQLite)
  - **Multi-Modal Support**: Documents (PDF, Word, Excel), Images (OCR), Audio (Speech-to-Text), Databases
  - **Local AI**: Ollama integration for complete on-premise deployment (GDPR/KVKK/HIPAA compliant)
  - **Cloud AI**: Anthropic Claude, OpenAI GPT, Google Gemini support
  - **Docker Orchestration**: Complete containerized environment with docker-compose
  - **Test Databases**: Pre-configured test databases with cross-database relationships
  - **Health Monitoring**: Service health checks for all components
  - **Model Management**: Ollama model download and management
  - **Multi-language**: Query support in English, Turkish, German, Russian

## 🚀 Running Examples

### SmartRAG.API Example
```bash
cd examples/SmartRAG.API
dotnet restore
dotnet run
```

Browse to `https://localhost:5001/swagger` for interactive API documentation.

### SmartRAG.Demo Example
```bash
cd examples/SmartRAG.Demo

# Start Docker services (for local mode)
docker-compose up -d

# Run the application
dotnet restore
dotnet run
```

Choose your deployment mode (Local/Cloud/Hybrid) and explore multi-database RAG capabilities!

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
- **Speech-to-Text Service** - Standalone audio transcription with Whisper.net
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
