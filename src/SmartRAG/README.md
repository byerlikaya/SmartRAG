# SmartRAG

**Multi-Database RAG Library for .NET**  
Ask questions about your data in natural language

## Description

SmartRAG is a comprehensive Retrieval-Augmented Generation (RAG) library that enables you to query multiple databases, documents, images, and audio files using natural language. Transform your data into intelligent conversations with a single, unified API.

### Key Capabilities

- **Multi-Database RAG** - Query SQL Server, MySQL, PostgreSQL, SQLite together
- **Multi-Modal Intelligence** - PDF, Word, Excel, Images (OCR), Audio (Whisper.net)
- **On-Premise Ready** - 100% local with Ollama, LM Studio, Whisper.net
- **Conversation History** - Built-in automatic context management
- **Universal AI Support** - OpenAI, Anthropic, Gemini, Azure, Custom APIs
- **Enterprise Storage** - Qdrant, Redis, InMemory (documents); Redis, SQLite, FileSystem, InMemory (conversations)

## Getting Started

### Prerequisites

- .NET Standard 2.1 or higher
- AI Provider API key (OpenAI, Anthropic, etc.) or local AI setup (Ollama)
- Database connections (SQL Server, MySQL, PostgreSQL, SQLite)

### Installation

```bash
dotnet add package SmartRAG
```

### Basic Setup

```csharp
// Program.cs
builder.Services.AddSmartRag(builder.Configuration);
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

## Usage

### Upload Documents

```csharp
// Upload document
var document = await documentService.UploadDocumentAsync(
    fileStream, fileName, contentType, "user-123"
);
```

### Query Across Multiple Data Sources

```csharp
// Query across databases and documents
var response = await documentSearchService.QueryIntelligenceAsync(
    "Show me all customers who made purchases over $10,000 in the last quarter, their payment history, and any complaints or feedback they provided"
);
// → AI automatically queries SQL Server (orders), MySQL (payments), PostgreSQL (customer data), 
//   analyzes uploaded PDF contracts, OCR-scanned invoices, and transcribed call recordings
```

### Multi-Database Queries

```csharp
// Direct multi-database query coordination
var multiDbResponse = await multiDbCoordinator.QueryMultipleDatabasesAsync(
    "Calculate total revenue by customer across all databases", 
    maxResults: 10
);
// → AI analyzes query intent, generates SQL for each database,
//   executes queries, and merges results intelligently
```

## Real-World Use Cases

### Banking - Customer Financial Profile
```csharp
var answer = await documentSearchService.QueryIntelligenceAsync(
    "Which customers have overdue payments and what's their total outstanding balance?"
);
// → Queries Customer DB, Payment DB, Account DB and combines results
// → Provides comprehensive financial risk assessment for credit decisions
```

### Healthcare - Patient Care Management
```csharp
var answer = await documentSearchService.QueryIntelligenceAsync(
    "Show me all patients with diabetes who haven't had their HbA1c checked in 6 months"
);
// → Combines Patient DB, Lab Results DB, Appointment DB and identifies at-risk patients
// → Ensures preventive care compliance and reduces complications
```

### Inventory - Supply Chain Optimization
```csharp
var answer = await documentSearchService.QueryIntelligenceAsync(
    "Which products are running low on stock and which suppliers can restock them fastest?"
);
// → Analyzes Inventory DB, Supplier DB, Order History DB and provides restocking recommendations
// → Prevents stockouts and optimizes supply chain efficiency
```

## What Makes SmartRAG Special?

- **Only .NET library** with native multi-database RAG capabilities
- **Automatic schema detection** across different database types  
- **100% local processing** with Ollama and Whisper.net
- **Enterprise-ready** with comprehensive error handling and logging
- **Cross-database queries** without manual SQL writing
- **Multi-modal intelligence** combining documents, databases, and AI

## Supported Data Sources

**Databases:** SQL Server, MySQL, PostgreSQL, SQLite  
**Documents:** PDF, Word, Excel, PowerPoint, Images, Audio  
**AI Models:** OpenAI, Anthropic, Gemini, Azure OpenAI, Ollama (local), LM Studio  
**Vector Stores:** Qdrant, Redis, InMemory  
**Conversation Storage:** Redis, SQLite, FileSystem, InMemory (independent from document storage)

## Comparison with Other RAG Libraries

| Feature | SmartRAG | LM-Kit.NET | Semantic Kernel | LangChain.NET |
|---------|----------|------------|----------------|---------------|
| **Library Owner** | Barış Yerlikaya | LM-Kit (France) | Microsoft | LangChain Community |
| **Description** | Multi-database RAG library for .NET | Generative AI SDK for .NET applications | AI orchestration framework | .NET port of LangChain framework |
| **RAG Support** | ✅ | ✅ | ✅ | ✅ |
| **Memory Management** | ✅ | ✅ | ✅ | ✅ |
| **Vector Stores** | ✅ | ✅ | ✅ | ✅ |
| **AI Model Integration** | ✅ | ✅ | ✅ | ✅ |
| **Plugin/Extension System** | ✅ | ✅ | ✅ | ✅ |
| **Multi-Modal** | ✅ | ✅ | ❌ | ❌ |
| **Local AI** | ✅ | ✅ | ❌ | ❌ |
| **Audio** | ✅ | ✅ | ❌ | ❌ |
| **OCR** | ✅ | ✅ | ❌ | ❌ |
| **On-Premise** | ✅ | ✅ | ❌ | ❌ |
| **Fallback Providers*** | ✅ | ❌ | ❌ | ❌ |
| **Retry Policies*** | ✅ | ❌ | ❌ | ❌ |
| **Batch Embeddings*** | ✅ | ❌ | ❌ | ❌ |
| **Hybrid Search*** | ✅ | ❌ | ❌ | ❌ |
| **Session Management*** | ✅ | ❌ | ❌ | ❌ |
| **Cross-DB JOIN*** | ✅ | ❌ | ❌ | ❌ |
| **Multi-DB RAG*** | ✅ | ❌ | ❌ | ❌ |
| **Databases*** | ✅ | ❌ | ❌ | ❌ |

**SmartRAG Exclusive Features (*):**
- **Fallback Providers**: Automatic failover to backup AI providers when primary fails
- **Retry Policies**: Configurable retry with FixedDelay, LinearBackoff, ExponentialBackoff
- **Batch Embeddings**: Efficient batch processing for multiple texts simultaneously
- **Hybrid Search**: Semantic + keyword hybrid algorithm (80% semantic, 20% keyword)
- **Session Management**: Persistent conversation continuity across app restarts
- **Cross-DB JOIN**: AI-powered intelligent joins across different databases
- **Multi-DB RAG**: Native multi-database query coordination
- **Databases**: Native support for SQL Server, MySQL, PostgreSQL, SQLite

## Additional Documentation

- **Complete Documentation** - [https://byerlikaya.github.io/SmartRAG/en/](https://byerlikaya.github.io/SmartRAG/en/) - Comprehensive guides, API reference, and tutorials
- **GitHub Repository** - [https://github.com/byerlikaya/SmartRAG](https://github.com/byerlikaya/SmartRAG) - Source code, examples, and community
- **Live Examples** - [https://byerlikaya.github.io/SmartRAG/en/examples](https://byerlikaya.github.io/SmartRAG/en/examples) - Real-world usage scenarios
- **API Reference** - [https://byerlikaya.github.io/SmartRAG/en/api-reference](https://byerlikaya.github.io/SmartRAG/en/api-reference) - Complete API documentation
- **Changelog** - [https://github.com/byerlikaya/SmartRAG/blob/main/CHANGELOG.md](https://github.com/byerlikaya/SmartRAG/blob/main/CHANGELOG.md) - Version history and updates

## Feedback

- **Email Support** - [b.yerlikaya@outlook.com](mailto:b.yerlikaya@outlook.com)
- **LinkedIn** - [https://www.linkedin.com/in/barisyerlikaya/](https://www.linkedin.com/in/barisyerlikaya/)
- **GitHub Issues** - [https://github.com/byerlikaya/SmartRAG/issues](https://github.com/byerlikaya/SmartRAG/issues)
- **Website** - [https://byerlikaya.github.io/SmartRAG/en/](https://byerlikaya.github.io/SmartRAG/en/)

## License

MIT License - see [LICENSE](https://github.com/byerlikaya/SmartRAG/blob/main/LICENSE) for details.

**Built with ❤️ by Barış Yerlikaya**

Made in Turkey 🇹🇷 | [Contact](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/) | [Website](https://byerlikaya.github.io/SmartRAG/en/)