# 🚀 Getting Started with SmartRAG

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
<PackageReference Include="SmartRAG" Version="1.0.0" />
```

## ⚡ Quick Setup

### 1. Configure Services
```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Add SmartRAG services
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

## 🔧 Next Steps

1. **[Choose Your AI Provider](ai-providers.md)** - Configure OpenAI, Anthropic, Gemini, etc.
2. **[Select Storage Backend](storage-providers.md)** - Set up Qdrant, Redis, SQLite, etc.
3. **[Upload Documents](document-processing.md)** - Learn about supported formats
4. **[Ask Questions](querying.md)** - Master the RAG pipeline
5. **[Advanced Configuration](configuration.md)** - Fine-tune your setup

## 🆘 Need Help?

- 📖 **[Full Documentation](../README.md)**
- 🐛 **[Report Issues](https://github.com/byerlikaya/SmartRAG/issues)**
- 💬 **[Discussions](https://github.com/byerlikaya/SmartRAG/discussions)**
- 📧 **[Contact](mailto:b.yerlikaya@outlook.com)**

Happy building! 🎉
