<p align="center">
  <img src="icon.svg" alt="SmartRAG Logo" width="200" height="200">
</p>

<p align="center">
  <b>Multi-Modal RAG for .NET — query databases, documents, images & audio in natural language</b>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/SmartRAG"><img src="https://img.shields.io/nuget/v/SmartRAG.svg?style=for-the-badge&logo=nuget" alt="NuGet Version"/></a>
  <a href="https://www.nuget.org/packages/SmartRAG"><img src="https://img.shields.io/nuget/dt/SmartRAG.svg?style=for-the-badge&logo=nuget" alt="Downloads"/></a>
  <a href="https://github.com/byerlikaya/SmartRAG/stargazers"><img src="https://img.shields.io/github/stars/byerlikaya/SmartRAG?style=for-the-badge&logo=github" alt="GitHub Stars"/></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green.svg?style=for-the-badge" alt="License"/></a>
</p>

<p align="center">
  <a href="https://github.com/byerlikaya/SmartRAG/actions"><img src="https://img.shields.io/github/actions/workflow/status/byerlikaya/SmartRAG/build.yml?style=for-the-badge&logo=github" alt="Build Status"/></a>
  <a href="https://codecov.io/gh/byerlikaya/SmartRAG"><img src="https://img.shields.io/codecov/c/github/byerlikaya/SmartRAG?style=for-the-badge&logo=codecov" alt="Code Coverage"/></a>
  <a href="https://www.nuget.org/packages/SmartRAG"><img src="https://img.shields.io/badge/.NET%20Standard-2.1-blue?style=for-the-badge&logo=.net" alt=".NET Standard 2.1"/></a>
</p>

<p align="center">
  <a href="https://byerlikaya.github.io/SmartRAG/en/"><img src="https://img.shields.io/badge/📚-Complete_Documentation-blue?style=for-the-badge&logo=book" alt="Documentation"/></a>
  <a href="README.tr.md"><img src="https://img.shields.io/badge/🇹🇷-Türkçe_README-red?style=for-the-badge" alt="Turkish README"/></a>
</p>

## 🚀 **Quick Start**

### **1. Install SmartRAG**
```bash
dotnet add package SmartRAG
```

### **2. Setup**
```csharp
builder.Services.UseSmartRAG(builder.Configuration,
    aiProvider: AIProvider.OpenAI,
    storageProvider: StorageProvider.InMemory
);
```

### **3. Configure databases in appsettings.json**
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

### **4. Upload documents & ask questions**
```csharp
// Upload document
var document = await documentService.UploadDocumentAsync(
    fileStream, fileName, contentType, "user-123"
);

// Query across databases and documents
var response = await searchService.QueryIntelligenceAsync(
    "Show me all customers who made purchases over $10,000 in the last quarter, their payment history, and any complaints or feedback they provided"
);
// → AI automatically queries SQL Server (orders), MySQL (payments), PostgreSQL (customer data), 
//   analyzes uploaded PDF contracts, OCR-scanned invoices, and transcribed call recordings
```

**Want to test SmartRAG immediately?** → [Jump to Examples & Testing](#-examples--testing)


## 🏆 **Why SmartRAG?**

🎯 **Multi-Database RAG** - Query multiple databases simultaneously with natural language

🧠 **Multi-Modal Intelligence** - PDF, Word, Excel, Images, Audio, and more  

🏠 **100% Local Processing** - GDPR, KVKK, HIPAA compliant

🚀 **Production Ready** - Enterprise-grade, thread-safe, high performance

## 🎯 **Real-World Use Cases**

### **1. Banking - Customer Financial Profile**
```csharp
var answer = await searchService.QueryIntelligenceAsync(
    "Which customers have overdue payments and what's their total outstanding balance?"
);
// → Queries Customer DB, Payment DB, Account DB and combines results
// → Provides comprehensive financial risk assessment for credit decisions
```

### **2. Healthcare - Patient Care Management**
```csharp
var answer = await searchService.QueryIntelligenceAsync(
    "Show me all patients with diabetes who haven't had their HbA1c checked in 6 months"
);
// → Combines Patient DB, Lab Results DB, Appointment DB and identifies at-risk patients
// → Ensures preventive care compliance and reduces complications
```

### **3. Inventory - Supply Chain Optimization**
```csharp
var answer = await searchService.QueryIntelligenceAsync(
    "Which products are running low on stock and which suppliers can restock them fastest?"
);
// → Analyzes Inventory DB, Supplier DB, Order History DB and provides restocking recommendations
// → Prevents stockouts and optimizes supply chain efficiency
```

## 🚀 **What Makes SmartRAG Special?**

- **Only .NET library** with native multi-database RAG capabilities
- **Automatic schema detection** across different database types  
- **100% local processing** with Ollama and Whisper.net
- **Enterprise-ready** with comprehensive error handling and logging
- **Cross-database queries** without manual SQL writing
- **Multi-modal intelligence** combining documents, databases, and AI

## 🧪 **Examples & Testing**

SmartRAG provides comprehensive example applications for different use cases:

### **📁 Available Examples**
```
examples/
├── SmartRAG.API/          # Complete REST API with Swagger UI
└── SmartRAG.Demo/         # Interactive console application
```

### **🚀 Quick Test with Demo**

Want to see SmartRAG in action immediately? Try our interactive console demo:

```bash
# Clone and run the demo
git clone https://github.com/byerlikaya/SmartRAG.git
cd SmartRAG/examples/SmartRAG.Demo
dotnet run
```

**Prerequisites:** You need to have databases and AI services running locally, or use Docker for easy setup.

📖 **[SmartRAG.Demo README](examples/SmartRAG.Demo/README.md)** - Complete demo application guide and setup instructions

#### **🐳 Docker Setup (Recommended)**

For the easiest experience with all services pre-configured:

```bash
# Start all services (SQL Server, MySQL, PostgreSQL, Ollama, Qdrant, Redis)
docker-compose up -d

# Setup AI models
docker exec -it smartrag-ollama ollama pull llama3.2
docker exec -it smartrag-ollama ollama pull nomic-embed-text
```

📚 **[Complete Docker Setup Guide](examples/SmartRAG.Demo/README-Docker.md)** - Detailed Docker configuration, troubleshooting, and management

### **📋 Demo Features & Steps:**

**🔗 Database Management:**
- **Step 1-2**: Show connections & health check
- **Step 3-5**: Create test databases (SQL Server, MySQL, PostgreSQL)
- **Step 6**: View database schemas and relationships

**🤖 AI & Query Testing:**
- **Step 7**: Query analysis - see how natural language converts to SQL
- **Step 8**: Automatic test queries - pre-built scenarios
- **Step 9**: Multi-database AI queries - ask questions across all databases

**🏠 Local AI Setup:**
- **Step 10**: Setup Ollama models for 100% local processing
- **Step 11**: Test vector stores (InMemory, Redis, SQLite, Qdrant)

**📄 Document Processing:**
- **Step 12**: Upload documents (PDF, Word, Excel, Images, Audio)
- **Step 13**: List and manage uploaded documents
- **Step 14**: Multi-modal RAG - combine documents + databases
- **Step 15**: Clear documents for fresh testing

**Perfect for:** Quick evaluation, proof-of-concept, team demos, learning SmartRAG capabilities

📚 **[Complete Examples & Testing Guide](https://byerlikaya.github.io/SmartRAG/en/examples)** - Step-by-step tutorials and test scenarios

## 🎯 **Supported Data Sources**

**📊 Databases:** SQL Server, MySQL, PostgreSQL, SQLite  
**📄 Documents:** PDF, Word, Excel, PowerPoint, Images, Audio  
**🤖 AI Models:** OpenAI, Anthropic, Ollama (local), LM Studio  
**🗄️ Vector Stores:** Qdrant, Redis, SQLite, InMemory

## 📊 **Comparison with Other RAG Libraries**

| Feature | SmartRAG | LM-Kit.NET | Semantic Kernel | LangChain.NET |
|---------|----------|------------|----------------|---------------|
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

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

**Built with ❤️ by Barış Yerlikaya**

Made in Turkey 🇹🇷 | [Contact](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)