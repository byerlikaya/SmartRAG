# SmartRAG Examples

This directory contains example projects demonstrating how to use SmartRAG in different scenarios.

## 📁 Available Examples

### **WebAPI** - ASP.NET Core Web API Example
- **Location**: `WebAPI/`
- **Description**: Complete web API implementation showing document upload, search, and RAG operations
- **Features**: 
  - Multi-document upload (PDF, Word, Excel, text files)
  - Image processing with OCR support (.jpg, .png, .gif, .bmp, .tiff, .webp)
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
- **Console Application** - Command-line interface with OCR support
- **Blazor WebAssembly** - Client-side web app with image upload
- **WPF Application** - Desktop application with document processing
- **Azure Functions** - Serverless implementation with vector search
- **Minimal API** - Lightweight web API with conversation management
- **OCR Service** - Standalone OCR processing service
- **Document Analyzer** - Advanced document analysis with table extraction
