# 🚀 SmartRAG - Multi-Database RAG Library for .NET

[![NuGet Version](https://img.shields.io/nuget/v/SmartRAG.svg?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/SmartRAG)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SmartRAG?style=for-the-badge&logo=nuget&label=Downloads&color=blue)](https://www.nuget.org/packages/SmartRAG)
[![License](https://img.shields.io/badge/license-MIT-green.svg?style=for-the-badge)](https://github.com/byerlikaya/SmartRAG/blob/main/LICENSE)

**Ask questions about your documents, databases, images and audio in natural language.**

SmartRAG is a production-ready .NET Standard 2.1 library that enables you to build intelligent AI systems with:

✅ **Multi-Database RAG** - Query SQL Server, MySQL, PostgreSQL, SQLite together  
✅ **Multi-Modal** - PDF, Word, Excel, Images (OCR), Audio (Speech-to-Text), Databases  
✅ **On-Premise Ready** - 100% local with Ollama, LM Studio, Whisper.net  
✅ **Conversation History** - Built-in automatic context management  
✅ **Production Ready** - Enterprise-grade, comprehensive testing

## ✨ Key Features

- 🗄️ **Multi-Database RAG** - Query SQL Server, MySQL, PostgreSQL, SQLite together with natural language
- 🎯 **Multi-Modal Intelligence** - PDF, Word, Excel, Images (OCR), Audio (Speech-to-Text), Databases
- 🔒 **On-Premise Ready** - 100% local operation with Ollama, LM Studio, Whisper.net (GDPR/KVKK/HIPAA)
- 💬 **Conversation History** - Built-in automatic context management across questions
- 🤖 **Universal AI Support** - OpenAI, Anthropic, Gemini, Azure, Custom (any OpenAI-compatible API)
- 🏢 **Enterprise Storage** - Qdrant, Redis, SQLite, FileSystem, In-Memory
- 🧠 **Smart Intent Detection** - Automatically routes general chat vs document queries
- 🌍 **Language-Agnostic** - Works globally without hardcoded language patterns
- 🎯 **Hybrid Search** - 80% semantic + 20% keyword relevance scoring
- 🔍 **Smart Chunking** - Word boundary protection, context preservation
- ✅ **Production Ready** - Enterprise-grade, comprehensive testing

## 🎯 Quick Start

### Installation
```bash
dotnet add package SmartRAG
```

### 5-Minute Setup
```csharp
// Program.cs
builder.Services.UseSmartRAG(builder.Configuration,
    aiProvider: AIProvider.OpenAI,
    storageProvider: StorageProvider.InMemory
);

// Ask questions in your service
public class MyService
{
    private readonly IDocumentSearchService _intelligence;
    
    public async Task<string> Ask(string question)
    {
        var result = await _intelligence.QueryIntelligenceAsync(question, maxResults: 5);
        return result.Answer;
    }
}
```

### Configuration
```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-your-key",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002"
    }
  }
}
```

That's it! 🎉

## 🗄️ Multi-Database RAG

Connect and query multiple databases with natural language:

```csharp
// Connect databases
await databaseParser.ConnectAsync(
    sqlServer: "Server=localhost;Database=Sales;",
    mysql: "Server=localhost;Database=Customers;",
    postgresql: "Host=localhost;Database=Analytics;"
);

// Ask across all databases
var answer = await intelligence.QueryIntelligenceAsync(
    "Show customers with over $100K revenue across all databases"
);
// → AI queries SQL Server, MySQL, PostgreSQL and combines results
```

**Supported Databases:**
- SQL Server
- MySQL
- PostgreSQL  
- SQLite

## 📄 Supported Formats

**Documents:**
- PDF, Word (.docx, .doc), Excel (.xlsx, .xls)
- Text (.txt, .md, .json, .xml, .csv, .html)

**Multi-Modal:**
- Images (OCR with Tesseract 5.2.0) - JPG, PNG, GIF, BMP, TIFF, WebP
- Audio (Speech-to-Text with Whisper.net) - MP3, WAV, M4A, AAC, OGG, FLAC
- Databases (Live connections) - SQL Server, MySQL, PostgreSQL, SQLite

## 🤖 AI Providers

**Supported Providers:**
- OpenAI (GPT-4, GPT-3.5)
- Anthropic (Claude 3.5)
- Google Gemini
- Azure OpenAI
- Custom (Ollama, LM Studio, any OpenAI-compatible API)

**On-Premise Options:**
```json
{
  "AI": {
  "Custom": {
      "Endpoint": "http://localhost:11434/v1/chat/completions",
      "Model": "llama2",
      "EmbeddingModel": "nomic-embed-text"
    }
  }
}
```

## 💾 Storage Options

**Vector Databases:**
- Qdrant (high-performance vector search)
- Redis (fast in-memory storage)

**Traditional Storage:**
- SQLite (embedded database)
- FileSystem (document storage)
- InMemory (development/testing)

## 🔧 Configuration

### Anthropic Users
Anthropic requires a separate VoyageAI API key for embeddings:
- Get API key: [VoyageAI Console](https://console.voyageai.com/)
- Recommended model: `voyage-large-2`
- [Official guide](https://docs.anthropic.com/en/docs/build-with-claude/embeddings)

### Advanced Options
```csharp
services.UseSmartRAG(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = [AIProvider.Anthropic, AIProvider.Gemini];
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});
```

## 💻 System Requirements

**For Library:**
- .NET Core 3.0+ or .NET 5/6/7/8/9
- 2 GB RAM minimum

**For AI:**
- Cloud AI: OpenAI/Anthropic/Gemini API key (recommended for getting started)
- Local AI: 8-16 GB RAM for Ollama/LM Studio models (on-premise deployments)

## 🆕 What's New in v3.0.0

**Major Features:**
- 🗄️ **Multi-Database RAG** - Query multiple databases with natural language
- 🖼️ **OCR Support** - Image processing with Tesseract 5.2.0
- 🎤 **Audio Support** - Local transcription with Whisper.net (99+ languages)
- 💬 **Conversation History** - Built-in session management
- 🔄 **API Rename** - `GenerateRagAnswerAsync` → `QueryIntelligenceAsync`
- 🌐 **PostgreSQL** - Full support for multi-database queries

[View full changelog →](https://github.com/byerlikaya/SmartRAG/blob/main/CHANGELOG.md)

## 📚 Resources

- **📧 [Contact & Support](mailto:b.yerlikaya@outlook.com)**
- **💼 [LinkedIn](https://www.linkedin.com/in/barisyerlikaya)**
- **🐙 [GitHub Profile](https://github.com/byerlikaya)**
- **📦 [NuGet Packages](https://www.nuget.org/profiles/barisyerlikaya)**
- **📖 [Documentation](https://byerlikaya.github.io/SmartRAG)** - Comprehensive guides and API reference

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.



**Built with ❤️ by Barış Yerlikaya**

Made in Turkey 🇹🇷 | [Contact](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya)