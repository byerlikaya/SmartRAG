# SmartRAG Examples

This directory contains example projects demonstrating how to use SmartRAG in different scenarios.

## 📁 Available Examples

### **WebAPI** - ASP.NET Core Web API Example
- **Location**: `WebAPI/`
- **Description**: Complete web API implementation showing document upload, search, and RAG operations
- **Features**: 
  - Multi-document upload
  - AI-powered question answering
  - Smart query intent detection
  - Multiple storage providers
  - Comprehensive API documentation

## 🚀 Running Examples

### WebAPI Example
```bash
cd examples/WebAPI
dotnet restore
dotnet run
```

Browse to `https://localhost:5001/scalar/v1` for interactive API documentation.

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
- **Console Application** - Command-line interface
- **Blazor WebAssembly** - Client-side web app
- **WPF Application** - Desktop application
- **Azure Functions** - Serverless implementation
- **Minimal API** - Lightweight web API
