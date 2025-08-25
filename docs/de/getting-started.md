---
layout: default
title: Getting Started
description: Quick installation and setup guide to get you up and running with SmartRAG
lang: en
---

# Getting Started

Quick installation and setup guide to get you up and running with SmartRAG.

## Installation

SmartRAG is available as a NuGet package and can be installed in several ways.

### Using .NET CLI:

```bash
dotnet add package SmartRAG
```

### Using Package Manager:

```bash
Install-Package SmartRAG
```

### Direct package reference:

```xml
<PackageReference Include="SmartRAG" Version="1.0.3" />
```

## Basic Setup

Once installed, you can configure SmartRAG in your application.

### Basic Configuration:

```csharp
// Add SmartRAG to your project
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});
```

### Advanced Configuration:

```csharp
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Redis;
    options.ApiKey = "your-openai-key";
    options.ModelName = "text-embedding-ada-002";
    options.ChunkSize = 1000;
    options.ChunkOverlap = 200;
});
```

## Quick Start Example

Here's a simple example to get you started:

```csharp
// Add SmartRAG services to your DI container
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

## Next Steps

Now that you have SmartRAG installed and configured, you can:

- Upload and process documents
- Generate embeddings using AI providers
- Perform semantic search queries
- Explore advanced features

## Need Help?

If you encounter any issues or need assistance:

- [Back to Documentation]({{ site.baseurl }}/en/) - Main documentation
- [Open an issue](https://github.com/byerlikaya/SmartRAG/issues) - GitHub Issues
- [Contact support](mailto:b.yerlikaya@outlook.com) - Email support
