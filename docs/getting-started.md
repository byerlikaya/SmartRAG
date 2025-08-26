---
layout: default
title: Getting Started
description: Quick start guide for SmartRAG - Installation, configuration, and first steps
---

# Getting Started with SmartRAG

Welcome to SmartRAG! This guide will help you get up and running quickly with our enterprise-grade RAG solution.

## 📋 Prerequisites

- **.NET 9.0 SDK** or later
- An **AI provider account** (OpenAI, Anthropic, etc.)
- **Visual Studio 2022**, **VS Code**, or **JetBrains Rider**

## 📦 Installation

### Option 1: NuGet Package Manager
```bash
Install-Package SmartRAG
```

### Option 2: .NET CLI
```bash
dotnet add package SmartRAG
```

### Option 3: PackageReference
```xml
<PackageReference Include="SmartRAG" Version="1.1.0" />
```

## ⚡ Quick Setup

### 1. Configure Services
```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Add SmartRAG services with enhanced features
builder.Services.UseSmartRAG(builder.Configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.OpenAI
);

var app = builder.Build();
```

### 2. Configuration (appsettings.json)
```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "your-openai-api-key",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002"
    }
  },
  "Storage": {
    "InMemory": {
      "MaxDocuments": 1000
    }
  }
}
```

### 3. Inject and Use
```csharp
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "user-123"
        );
        
        return Ok(document);
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] QuestionRequest request)
    {
        var response = await _documentService.GenerateRagAnswerAsync(
            request.Question, 
            maxResults: 5
        );
        
        return Ok(response);
    }
}
```

## 🎯 Enhanced Features

### **🧠 Advanced Semantic Search**
SmartRAG automatically uses enhanced semantic search with hybrid scoring:

```csharp
// The system automatically calculates:
// - Semantic similarity (80% weight)
// - Keyword relevance (20% weight)
// - Contextual enhancements
// - Word boundary validation

var response = await _documentService.GenerateRagAnswerAsync(
    "What are the main benefits mentioned in the contract?"
);
```

### **🔍 Smart Document Chunking**
Documents are automatically chunked with intelligent boundary detection:

```csharp
// Features:
// ✅ Word boundary protection (never cuts words)
// ✅ Context preservation between chunks
// ✅ Optimal break points (sentence > paragraph > word)
// ✅ Configurable overlap for continuity

var document = await _documentService.UploadDocumentAsync(
    fileStream, "contract.pdf", "application/pdf", "user-123"
);

// Chunks are automatically created with:
// - Smart boundaries
// - Context overlap
// - Word integrity
var chunks = document.Chunks;
```

### **🌍 Language-Agnostic Design**
Works with any language without hardcoded patterns:

```csharp
// These queries automatically work:
// "What are the contract terms?" (English)
// "Sözleşme şartları nelerdir?" (Turkish)
// "合同条款是什么？" (Chinese)
// "Quais são os termos do contrato?" (Portuguese)

var response = await _documentService.GenerateRagAnswerAsync(query);
```

## 🔧 Advanced Configuration

### **Enhanced Chunking Options**
```csharp
services.AddSmartRAG(configuration, options =>
{
    // Chunking configuration
    options.MaxChunkSize = 1200;        // Maximum chunk size
    options.MinChunkSize = 150;         // Minimum chunk size
    options.ChunkOverlap = 250;         // Overlap between chunks
    
    // Enhanced features
    options.EnableWordBoundaryValidation = true;
    options.EnableOptimalBreakPoints = true;
    
    // Hybrid scoring weights
    options.SemanticScoringWeight = 0.8f;  // 80% semantic
    options.KeywordScoringWeight = 0.2f;   // 20% keyword
});
```

### **VoyageAI Integration (Anthropic)**
For Anthropic Claude models, configure VoyageAI embeddings:

```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "sk-ant-YOUR_ANTHROPIC_KEY",
      "Model": "claude-3.5-sonnet",
      "EmbeddingApiKey": "voyage-YOUR_VOYAGEAI_KEY",
      "EmbeddingModel": "voyage-large-2"
    }
  }
}
```

**Why VoyageAI?**
- Claude models don't provide embeddings
- VoyageAI offers high-quality embeddings
- Get your key at: [console.voyageai.com](https://console.voyageai.com/)

### **Production Storage Setup**
```csharp
// For production, use Redis or Qdrant
services.UseSmartRag(configuration,
    storageProvider: StorageProvider.Redis,
    aiProvider: AIProvider.Anthropic
);
```

```json
{
  "Storage": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Database": 0,
      "KeyPrefix": "smartrag:"
    }
  }
}
```

## 📊 Performance Optimization

### **Chunking Performance**
```csharp
// Optimize for your content type
services.AddSmartRAG(configuration, options =>
{
    // For technical documents
    options.MaxChunkSize = 800;   // Smaller chunks
    options.ChunkOverlap = 150;   // Less overlap
    
    // For narrative content
    options.MaxChunkSize = 1200;  // Larger chunks
    options.ChunkOverlap = 250;   // More overlap
});
```

### **Search Performance**
```csharp
// Adjust search thresholds
services.AddSmartRAG(configuration, options =>
{
    options.SemanticSearchThreshold = 0.25;  // Lower = more results
});
```

## 🚀 Next Steps

1. **[Choose Your AI Provider]({{ site.baseurl }}/configuration#ai-providers)** - Configure OpenAI, Anthropic, Gemini, etc.
2. **[Select Storage Backend]({{ site.baseurl }}/configuration#storage-providers)** - Set up Qdrant, Redis, SQLite, etc.
3. **[Upload Documents]({{ site.baseurl }}/api-reference#document-management)** - Learn about supported formats
4. **[Ask Questions]({{ site.baseurl }}/api-reference#ai-question-answering--chat)** - Master the RAG pipeline
5. **[Advanced Configuration]({{ site.baseurl }}/configuration)** - Fine-tune your setup
6. **[Performance Tuning]({{ site.baseurl }}/configuration#performance-tuning)** - Optimize for your use case

## 🔍 Understanding the RAG Pipeline

### **1. Document Upload & Processing**
```
📄 File Upload → 🔍 Format Detection → 📝 Text Extraction → ✂️ Smart Chunking
```

**Smart Chunking Features:**
- **Word Boundary Validation**: Never cuts words in the middle
- **Context Preservation**: Maintains semantic continuity
- **Optimal Break Points**: Intelligent boundary selection

### **2. Search & Retrieval**
```
🙋‍♂️ User Question → 🎯 Intent Detection → 🔍 Hybrid Search → 📊 Relevance Scoring
```

**Hybrid Search Features:**
- **Semantic Similarity (80%)**: Advanced text analysis
- **Keyword Relevance (20%)**: Traditional text matching
- **Contextual Enhancement**: Semantic coherence analysis

### **3. Answer Generation**
```
📚 Relevant Chunks → 🤖 AI Processing → ✨ Intelligent Answer → 📋 Source Attribution
```

## 🆘 Need Help?

- 📖 **[Documentation]({{ site.baseurl }}/)** - Back to main documentation
- 🐛 **[Report Issues](https://github.com/byerlikaya/SmartRAG/issues)** - GitHub Issues
- 💬 **[GitHub Discussions](https://github.com/byerlikaya/SmartRAG/discussions)** - Community discussions
- 📧 **[Contact](mailto:b.yerlikaya@outlook.com)** - Email support

## 🎉 What's New in v1.0.3

- 🧠 **Enhanced Semantic Search** - Advanced hybrid scoring system
- 🔍 **Smart Document Chunking** - Word boundary validation
- 🌍 **Language-Agnostic Design** - Works with any language
- 🚀 **VoyageAI Integration** - High-quality embeddings for Anthropic
- ⚙️ **Configuration Priority** - User settings take absolute priority
- 🔧 **Performance Optimizations** - Faster chunking and search

Happy building! 🎉
