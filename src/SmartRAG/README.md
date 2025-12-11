# SmartRAG

**Multi-Database RAG Library for .NET**  
Ask questions about your data in natural language

SmartRAG is a comprehensive Retrieval-Augmented Generation (RAG) library that enables you to query multiple databases, documents, images, and audio files using natural language. Transform your data into intelligent conversations with a single, unified API.

## 🚀 Quick Start

### Installation

```bash
dotnet add package SmartRAG
```

### Basic Setup

```csharp
// For Web API applications
builder.Services.AddSmartRag(builder.Configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.InMemory;
});

// For Console applications
var serviceProvider = services.UseSmartRag(
    configuration,
    aiProvider: AIProvider.OpenAI,
    storageProvider: StorageProvider.InMemory
);
```

### Configuration

Add database connections to your `appsettings.json`:

```json
{
  "SmartRAG": {
    "DatabaseConnections": [
      {
        "Name": "Sales",
        "ConnectionString": "Server=localhost;Database=Sales;...",
        "DatabaseType": "SqlServer"
      }
    ]
  }
}
```

### Usage Example

```csharp
// Upload document
var document = await documentService.UploadDocumentAsync(
    fileStream, fileName, contentType, "user-123"
);

// Unified query across databases, documents, images, and audio
var response = await searchService.QueryIntelligenceAsync(
    "Show me all customers who made purchases over $10,000 in the last quarter, their payment history, and any complaints or feedback they provided"
);
// → AI automatically analyzes query intent and routes intelligently:
//   - High confidence + database queries → Searches databases only
//   - High confidence + document queries → Searches documents only  
//   - Medium confidence → Searches both databases and documents, merges results
// → Queries SQL Server (orders), MySQL (payments), PostgreSQL (customer data)
// → Analyzes uploaded PDF contracts, OCR-scanned invoices, and transcribed call recordings
// → Provides unified answer combining all sources
```

## ✨ Key Features

🎯 **Unified Query Intelligence** - Single query searches across databases, documents, images, and audio automatically  
🧠 **Smart Hybrid Routing** - AI analyzes query intent and automatically determines optimal search strategy  
🗄️ **Multi-Database RAG** - Query multiple databases simultaneously with natural language  
📄 **Multi-Modal Intelligence** - PDF, Word, Excel, Images (OCR), Audio (Speech-to-Text), and more  
🔌 **MCP Client Integration** - Connect to external MCP servers and extend capabilities with external tools  
📁 **Automatic File Watching** - Monitor folders and automatically index new documents without manual uploads  
🏠 **100% Local Processing** - GDPR, KVKK, HIPAA compliant with Ollama and Whisper.net  
🚀 **Production Ready** - Enterprise-grade, thread-safe, high performance

## 📊 Supported Data Sources

**Databases:** SQL Server, MySQL, PostgreSQL, SQLite  
**Documents:** PDF, Word, Excel, PowerPoint, Images, Audio  
**AI Models:** OpenAI, Anthropic, Gemini, Azure OpenAI, Ollama (local), LM Studio  
**Vector Stores:** Qdrant, Redis, InMemory  
**Conversation Storage:** Redis, SQLite, FileSystem, InMemory (independent from document storage)  
**External Integrations:** MCP (Model Context Protocol) servers for extended tool capabilities  
**File Monitoring:** Automatic folder watching with real-time document indexing

## 🎯 Real-World Use Cases

### Banking - Customer Financial Profile
```csharp
var answer = await searchService.QueryIntelligenceAsync(
    "Which customers have overdue payments and what's their total outstanding balance?"
);
// → Queries Customer DB, Payment DB, Account DB and combines results
// → Provides comprehensive financial risk assessment for credit decisions
```

### Healthcare - Patient Care Management
```csharp
var answer = await searchService.QueryIntelligenceAsync(
    "Show me all patients with diabetes who haven't had their HbA1c checked in 6 months"
);
// → Combines Patient DB, Lab Results DB, Appointment DB and identifies at-risk patients
// → Ensures preventive care compliance and reduces complications
```

### Inventory - Supply Chain Optimization
```csharp
var answer = await searchService.QueryIntelligenceAsync(
    "Which products are running low on stock and which suppliers can restock them fastest?"
);
// → Analyzes Inventory DB, Supplier DB, Order History DB and provides restocking recommendations
// → Prevents stockouts and optimizes supply chain efficiency
```

## 📚 Additional Resources

- **Complete Documentation** - [https://byerlikaya.github.io/SmartRAG/en/](https://byerlikaya.github.io/SmartRAG/en/) - Comprehensive guides, API reference, and tutorials
- **GitHub Repository** - [https://github.com/byerlikaya/SmartRAG](https://github.com/byerlikaya/SmartRAG) - Source code, examples, and community
- **Live Examples** - [https://byerlikaya.github.io/SmartRAG/en/examples](https://byerlikaya.github.io/SmartRAG/en/examples) - Real-world usage scenarios
- **API Reference** - [https://byerlikaya.github.io/SmartRAG/en/api-reference](https://byerlikaya.github.io/SmartRAG/en/api-reference) - Complete API documentation
- **Changelog** - [https://github.com/byerlikaya/SmartRAG/blob/main/CHANGELOG.md](https://github.com/byerlikaya/SmartRAG/blob/main/CHANGELOG.md) - Version history and updates

## 📞 Support

- **Email Support** - [b.yerlikaya@outlook.com](mailto:b.yerlikaya@outlook.com)
- **LinkedIn** - [https://www.linkedin.com/in/barisyerlikaya/](https://www.linkedin.com/in/barisyerlikaya/)
- **GitHub Issues** - [https://github.com/byerlikaya/SmartRAG/issues](https://github.com/byerlikaya/SmartRAG/issues)
- **Website** - [https://byerlikaya.github.io/SmartRAG/en/](https://byerlikaya.github.io/SmartRAG/en/)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/byerlikaya/SmartRAG/blob/main/LICENSE) file for details.

**Built with ❤️ by Barış Yerlikaya**

Made in Turkey 🇹🇷 | [Contact](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/) | [Website](https://byerlikaya.github.io/SmartRAG/en/)
