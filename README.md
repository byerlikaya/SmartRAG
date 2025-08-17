# 🚀 SmartRAG - Enterprise-Grade RAG Solution

[![Build Status](https://github.com/byerlikaya/SmartRAG/workflows/CI%2FCD%20Pipeline/badge.svg)](https://github.com/byerlikaya/SmartRAG/actions)
[![CodeQL](https://github.com/byerlikaya/SmartRAG/workflows/CodeQL%20Analysis/badge.svg)](https://github.com/byerlikaya/SmartRAG/actions)
[![NuGet Version](https://img.shields.io/nuget/v/SmartRAG.svg)](https://www.nuget.org/packages/SmartRAG)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SmartRAG.svg)](https://www.nuget.org/packages/SmartRAG)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

SmartRAG is a **production-ready** .NET 9.0 library that provides a complete **Retrieval-Augmented Generation (RAG)** solution for building **AI-powered question answering systems**. Upload your documents, ask questions in natural language, and get intelligent answers based on your content - all with enterprise-grade AI providers and storage options.

## ✨ Key Highlights

- 🎯 **AI Question Answering**: Ask questions about your documents and get intelligent, contextual answers
- 🧠 **Smart Query Intent Detection**: Automatically distinguishes between general conversation and document search queries
- 🌍 **Language-Agnostic**: Works with any language without hardcoded patterns or keywords
- 🤖 **Universal AI Support**: 5 dedicated providers + CustomProvider for unlimited AI APIs  
- 🏢 **Enterprise Storage**: Vector databases, Redis, SQL, FileSystem with advanced configurations
- 🧠 **Advanced RAG Pipeline**: Smart chunking, semantic retrieval, AI-powered answer generation
- ⚡ **Lightning Fast**: Optimized vector search with context-aware answer synthesis
- 🔌 **Plug & Play**: Single-line integration with dependency injection
- 📄 **Multi-Format**: PDF, Word, text files with intelligent parsing

## 🎯 What Makes SmartRAG Special

### 🚀 **Complete RAG Workflow**
```
📄 Document Upload → 🔍 Smart Chunking → 🧠 AI Embeddings → 💾 Vector Storage
                                                                        ↓
🙋‍♂️ User Question → 🎯 Intent Detection → 🔍 Find Relevant Chunks → 🤖 AI Answer Generation → ✨ Smart Response
```

### 🏆 **Production Features**
- **Smart Chunking**: Maintains context continuity between document segments
- **Intelligent Query Routing**: Automatically routes general conversation to AI chat, document queries to RAG search
- **Language-Agnostic Design**: No hardcoded language patterns - works globally with any language
- **Multiple Storage Options**: From in-memory to enterprise vector databases
- **AI Provider Flexibility**: Switch between providers without code changes
- **Document Intelligence**: Advanced parsing for PDF, Word, and text formats
- **Configuration-First**: Environment-based configuration with sensible defaults
- **Dependency Injection**: Full DI container integration

## 🧠 Smart Query Intent Detection

SmartRAG automatically detects whether your query is a general conversation or a document search request:

### **General Conversation** (Direct AI Chat)
- ✅ **"How are you?"** → Direct AI response
- ✅ **"What's the weather like?"** → Direct AI response  
- ✅ **"Tell me a joke"** → Direct AI response
- ✅ **"Emin misin?"** → Direct AI response (Turkish)
- ✅ **"你好吗？"** → Direct AI response (Chinese)

### **Document Search** (RAG with your documents)
- 🔍 **"What are the main benefits in the contract?"** → Searches your documents
- 🔍 **"Çalışan maaş bilgileri nedir?"** → Searches your documents (Turkish)
- 🔍 **"2025年第一季度报告的主要发现是什么？"** → Searches your documents (Chinese)
- 🔍 **"Show me the employee salary data"** → Searches your documents

**How it works:** The system analyzes query structure (numbers, dates, formats, length) to determine intent without any hardcoded language patterns.

## 📦 Installation

### NuGet Package Manager
```bash
Install-Package SmartRAG
```

### .NET CLI
```bash
dotnet add package SmartRAG
```

### PackageReference
```xml
<PackageReference Include="SmartRAG" Version="1.0.0" />
```

## 🚀 Quick Start

### 1. **Development Setup**
```bash
# Clone the repository
git clone https://github.com/byerlikaya/SmartRAG.git
cd SmartRAG

# Copy development configuration template
cp src/SmartRAG.API/appsettings.Development.template.json src/SmartRAG.API/appsettings.Development.json

# Edit appsettings.Development.json with your API keys
# - OpenAI API Key
# - Azure OpenAI credentials
# - Database connection strings
```

### 2. **Basic Setup**
```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Add SmartRAG with minimal configuration
builder.Services.UseSmartRAG(builder.Configuration,
    storageProvider: StorageProvider.InMemory,  // Start simple
    aiProvider: AIProvider.OpenAI               // Your preferred AI
);

var app = builder.Build();
```

### 2. **Upload Documents**
```csharp
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;

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
}
```

### 3. **AI-Powered Question Answering**
```csharp
public class QAController : ControllerBase
{
    private readonly IDocumentService _documentService;

    [HttpPost("ask")]
    public async Task<IActionResult> AskQuestion([FromBody] QuestionRequest request)
    {
        // User asks: "What are the main benefits mentioned in the contract?"
        var response = await _documentService.GenerateRagAnswerAsync(
            request.Question, 
            maxResults: 5
        );
        
        // Returns intelligent answer based on document content
        return Ok(response);
    }
}
```

### 4. **Configuration**

⚠️ **Security Note**: Never commit real API keys! Use `appsettings.Development.json` for local development.

```bash
# Copy template and add your real keys
cp src/SmartRAG.API/appsettings.json src/SmartRAG.API/appsettings.Development.json
```

**appsettings.Development.json** (your real keys):
```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-YOUR_REAL_KEY",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002"
    },
    "Anthropic": {
      "ApiKey": "sk-ant-YOUR_REAL_KEY",
      "Model": "claude-3.5-sonnet",
      "EmbeddingApiKey": "voyage-YOUR_REAL_KEY",
      "EmbeddingModel": "voyage-large-2"
    }
  },
  "Storage": {
    "InMemory": {
      "MaxDocuments": 1000
    }
  }
}
```

📖 **[Complete Configuration Guide](docs/configuration.md) | [🔧 Troubleshooting Guide](docs/troubleshooting.md)**

### 🔑 **Important Note for Anthropic Users**
**Anthropic Claude models require a separate VoyageAI API key for embeddings:**
- **Why?** Anthropic doesn't provide embedding models, so we use VoyageAI's high-quality embeddings
- **Official Documentation:** [Anthropic Embeddings Guide](https://docs.anthropic.com/en/docs/build-with-claude/embeddings#how-to-get-embeddings-with-anthropic)
- **Get API Key:** [VoyageAI API Keys](https://console.voyageai.com/)
- **Models:** `voyage-large-2` (recommended), `voyage-code-2`, `voyage-01`
- **Documentation:** [VoyageAI Embeddings API](https://docs.voyageai.com/embeddings/)

## 🤖 AI Providers - Universal Support

### 🎯 **Dedicated Providers** (Optimized & Battle-Tested)

| Provider | Capabilities | Special Features |
|----------|-------------|------------------|
| **🤖 OpenAI** | ✅ Latest GPT models<br/>✅ Advanced embeddings | Industry standard, reliable, extensive model family |
| **🧠 Anthropic** | ✅ Claude family models<br/>✅ VoyageAI embeddings | Safety-focused, constitutional AI, long context, requires separate VoyageAI API key |
| **🌟 Google Gemini** | ✅ Gemini models<br/>✅ Multimodal embeddings | Multimodal support, latest Google AI innovations |
| **☁️ Azure OpenAI** | ✅ Enterprise GPT models<br/>✅ Enterprise embeddings | GDPR compliant, enterprise security, SLA support |

### 🛠️ **CustomProvider** - Universal API Support
**One provider to rule them all!** Connect to any OpenAI-compatible API:

```json
{
  "AI": {
  "Custom": {
    "ApiKey": "your-api-key",
      "Endpoint": "https://api.openrouter.ai/v1/chat/completions",
      "Model": "anthropic/claude-3.5-sonnet",
      "EmbeddingModel": "text-embedding-ada-002"
    }
  }
}
```

**Supported APIs via CustomProvider:**
- 🔗 **OpenRouter** - Access 100+ models
- ⚡ **Groq** - Lightning-fast inference  
- 🌐 **Together AI** - Open source models
- 🚀 **Perplexity** - Search + AI
- 🇫🇷 **Mistral AI** - European AI leader
- 🔥 **Fireworks AI** - Ultra-fast inference
- 🦙 **Ollama** - Local models
- 🏠 **LM Studio** - Local AI playground
- 🛠️ **Any OpenAI-compatible API**

## 🗄️ Storage Solutions - Enterprise Grade

### 🎯 **Vector Databases**
```json
{
  "Storage": {
    "Qdrant": {
      "Host": "your-qdrant-host.com",
      "ApiKey": "your-api-key",
      "CollectionName": "documents",
      "VectorSize": 1536
    },
    "Redis": {
      "ConnectionString": "localhost:6379",
      "KeyPrefix": "smartrag:",
      "Database": 0
    }
  }
}
```

### 🏢 **Traditional Databases**  
```json
{
  "Storage": {
    "Sqlite": {
      "DatabasePath": "smartrag.db",
      "EnableForeignKeys": true
    },
    "FileSystem": {
      "FileSystemPath": "Documents"
    }
  }
}
```

### ⚡ **Development**
```json
{
  "Storage": {
    "InMemory": {
      "MaxDocuments": 1000
    }
  }
}
```

## 📄 Document Processing

### **Supported Formats**
- **📄 PDF**: Advanced text extraction with iText7
- **📝 Word**: .docx and .doc support with OpenXML
- **📋 Text**: .txt, .md, .json, .xml, .csv, .html
- **🔤 Plain Text**: UTF-8 encoding with BOM detection

### **Smart Document Parsing**
```csharp
// Automatic format detection and parsing
var document = await documentService.UploadDocumentAsync(
    fileStream,
    "contract.pdf",     // Automatically detects PDF
    "application/pdf",
    "legal-team"
);

// Smart chunking with overlap for context preservation
var chunks = document.Chunks; // Automatically chunked with smart boundaries
```

### **Advanced Chunking Options**
```csharp
services.AddSmartRAG(configuration, options =>
{
    options.MaxChunkSize = 1000;      // Maximum chunk size
    options.MinChunkSize = 100;       // Minimum chunk size  
    options.ChunkOverlap = 200;       // Overlap between chunks
    options.SemanticSearchThreshold = 0.3; // Similarity threshold
});
```

## 🔧 Advanced Configuration

### **Complete Configuration Example**
```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-...",
      "Endpoint": "https://api.openai.com/v1",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002",
      "MaxTokens": 4096,
      "Temperature": 0.7
    },
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "Model": "claude-3.5-sonnet",
      "MaxTokens": 4096,
      "Temperature": 0.3,
      "EmbeddingApiKey": "voyage-...",
      "EmbeddingModel": "voyage-large-2"
    }
  },
  "Storage": {
    "Qdrant": {
      "Host": "localhost:6334",
      "UseHttps": false,
      "CollectionName": "smartrag_docs",
      "VectorSize": 1536,
      "DistanceMetric": "Cosine"
    },
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Password": "",
      "Database": 0,
      "KeyPrefix": "smartrag:",
      "ConnectionTimeout": 30,
      "EnableSsl": false
    }
  }
}
```

### **Runtime Provider Switching**
```csharp
services.AddSmartRAG(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = [AIProvider.Anthropic, AIProvider.Gemini];
});
```

## 🏗️ Architecture

SmartRAG follows clean architecture principles with clear separation of concerns:

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   API Layer    │    │  Service Layer   │    │ Repository Layer│
│                 │    │                  │    │                 │
│ • Controllers   │───▶│ • DocumentService│───▶│ • Redis Repo    │
│ • DTOs          │    │ • AIService      │    │ • Qdrant Repo   │
│ • Validation    │    │ • ParserService  │    │ • SQLite Repo   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌──────────────────┐
                       │   AI Providers   │
                       │                  │
                       │ • OpenAI         │
                       │ • Anthropic      │
                       │ • Gemini         │
                       │ • CustomProvider │
                       └──────────────────┘
```

### **Key Components**

- **📄 DocumentService**: Main orchestrator for document operations
- **🤖 AIService**: Handles AI provider interactions and embeddings  
- **📝 DocumentParserService**: Multi-format document parsing
- **🏭 Factories**: Provider instantiation and configuration
- **📚 Repositories**: Storage abstraction layer
- **🔧 Extensions**: Dependency injection configuration

## 🎨 API Examples

### **Document Management**
```bash
# Upload document
curl -X POST "http://localhost:5000/api/documents/upload" \
  -F "file=@research-paper.pdf"

# Get document
curl "http://localhost:5000/api/documents/{document-id}"

# Delete document  
curl -X DELETE "http://localhost:5000/api/documents/{document-id}"

# List all documents
curl "http://localhost:5000/api/documents/search"
```

### **AI Question Answering & Chat**

SmartRAG handles both document search and general conversation automatically:

```bash
# Ask questions about your documents (RAG mode)
curl -X POST "http://localhost:5000/api/search/search" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "What are the main risks mentioned in the financial report?",
    "maxResults": 5
  }'

# General conversation (Direct AI chat mode)
curl -X POST "http://localhost:5000/api/search/search" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "How are you today?",
    "maxResults": 1
  }'
```

**Document Search Response Example:**
```json
{
  "query": "What are the main risks mentioned in the financial report?",
  "answer": "Based on the financial documents, the main risks identified include: 1) Market volatility affecting revenue projections, 2) Regulatory changes in the European market, 3) Currency exchange fluctuations, and 4) Supply chain disruptions. The report emphasizes that market volatility poses the highest risk with potential 15-20% impact on quarterly earnings...",
  "sources": [
    {
      "documentId": "doc-456",
      "fileName": "Q3-financial-report.pdf", 
      "chunkContent": "Market volatility remains our primary concern, with projected impact of 15-20% on quarterly earnings...",
      "relevanceScore": 0.94
    }
  ],
  "searchedAt": "2025-08-16T14:57:06.2312433Z",
  "configuration": {
    "aiProvider": "Anthropic",
    "storageProvider": "Redis",
    "model": "Claude + VoyageAI"
  }
}
```

**General Chat Response Example:**
```json
{
  "query": "How are you today?",
  "answer": "I'm doing well, thank you for asking! I'm here to help you with any questions you might have about your documents or just general conversation. How can I assist you today?",
  "sources": [],
  "searchedAt": "2025-08-16T14:57:06.2312433Z",
  "configuration": {
    "aiProvider": "Anthropic",
    "storageProvider": "Redis", 
    "model": "Claude + VoyageAI"
  }
}
```


## 📊 Performance & Scaling

### **Benchmarks**
- **Document Upload**: ~500ms for 100KB file, ~1-2s for 1MB file
- **Semantic Search**: ~200ms for simple queries, ~500ms for complex queries
- **AI Response**: ~2-5s for 5 sources, ~3-8s for 10 sources
- **Memory Usage**: ~50MB base + documents, ~100MB with Redis cache



### **Scaling Tips**
- Use **Redis** or **Qdrant** for production workloads
- Enable **connection pooling** for database connections
- Implement **caching** for frequently accessed documents
- Use **background services** for bulk document processing

## 🛠️ Development

### **Building from Source**
```bash
git clone https://github.com/byerlikaya/SmartRAG.git
cd SmartRAG
dotnet restore
dotnet build
dotnet test
```

### **Running the Sample API**
```bash
cd src/SmartRAG.API
dotnet run
```

Browse to `https://localhost:5001/scalar/v1` for interactive API documentation.

## 🤝 Contributing

We welcome contributions!

### **Development Setup**
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 🆕 What's New

### **Latest Release (v1.1.0)**
- 🧠 **Smart Query Intent Detection** - Automatically routes queries to chat vs document search
- 🌍 **Language-Agnostic Design** - Removed all hardcoded language patterns  
- 🔍 **Enhanced Search Relevance** - Improved name detection and content scoring
- 🔤 **Unicode Normalization** - Fixed special character handling issues
- ⚡ **Rate Limiting & Retry Logic** - Robust API handling with exponential backoff
- 🚀 **VoyageAI Integration** - Anthropic embedding support
- 📚 **Enhanced Documentation** - Official documentation links
- 🧹 **Configuration Cleanup** - Removed unnecessary fields
- 🎯 **Project Simplification** - Streamlined for better performance

## 📚 Resources

- **📧 [Contact & Support](mailto:b.yerlikaya@outlook.com)**
- **💼 [LinkedIn](https://www.linkedin.com/in/barisyerlikaya)**
- **🐙 [GitHub Profile](https://github.com/byerlikaya)**
- **📦 [NuGet Packages](https://www.nuget.org/profiles/barisyerlikaya)**

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🌟 Star History

[![Star History Chart](https://api.star-history.com/svg?repos=byerlikaya/SmartRAG&type=Date)](https://star-history.com/#byerlikaya/SmartRAG&Date)

---

**Built with ❤️ by Barış Yerlikaya**

Made in Turkey 🇹🇷 | [Contact](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya)