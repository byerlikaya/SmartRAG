---
layout: default
title: SmartRAG Documentation
nav_order: 1
---

# SmartRAG Documentation

Welcome to SmartRAG, a powerful and intelligent RAG (Retrieval-Augmented Generation) library for .NET applications.

## What is SmartRAG?

SmartRAG is a comprehensive .NET library that provides intelligent document processing, embedding generation, and semantic search capabilities. It's designed to be easy to use while offering powerful features for building AI-powered applications.

## Key Features

- **Multi-Format Document Support**: Process Word, PDF, Excel, and text documents
- **AI Provider Integration**: Support for OpenAI, Anthropic, and other AI providers
- **Vector Storage**: Multiple storage backends including Qdrant, Redis, and SQLite
- **Semantic Search**: Advanced search capabilities with similarity scoring
- **Extensible Architecture**: Plugin-based design for easy customization
- **Performance Optimized**: Built for high-performance production environments

## Quick Start

```csharp
// Add SmartRAG to your project
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});

// Use the document service
var documentService = serviceProvider.GetRequiredService<IDocumentService>();
var document = await documentService.UploadDocumentAsync(file);
```

## Getting Started

- [Installation Guide](getting-started.md)
- [Configuration](configuration.md)
- [API Reference](api-reference.md)
- [Troubleshooting](troubleshooting.md)

## Examples

Check out our [examples folder](https://github.com/yourusername/SmartRAG/tree/main/examples) for complete working applications.

## Contributing

We welcome contributions! Please see our [Contributing Guide](../CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.
