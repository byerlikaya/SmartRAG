<p align="center">
  <img src="icon.svg" alt="SmartRAG Logo" width="200" height="200">
</p>

<p align="center">
  <b>Multi-DB RAG for .NET — query many databases + documents in one NL request</b>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/SmartRAG"><img src="https://img.shields.io/nuget/v/SmartRAG.svg?style=for-the-badge&logo=nuget" alt="NuGet Version"/></a>
  <a href="https://www.nuget.org/packages/SmartRAG"><img src="https://img.shields.io/nuget/dt/SmartRAG.svg?style=for-the-badge&logo=nuget" alt="Downloads"/></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green.svg?style=for-the-badge" alt="License"/></a>
  <a href="https://github.com/byerlikaya/SmartRAG/actions"><img src="https://img.shields.io/github/actions/workflow/status/byerlikaya/SmartRAG/build.yml?style=for-the-badge&logo=github" alt="Build Status"/></a>
  <a href="https://codecov.io/gh/byerlikaya/SmartRAG"><img src="https://img.shields.io/codecov/c/github/byerlikaya/SmartRAG?style=for-the-badge&logo=codecov" alt="Code Coverage"/></a>
</p>

<p align="center">
  <a href="https://byerlikaya.github.io/SmartRAG/en/"><img src="https://img.shields.io/badge/📚-Complete_Documentation-blue?style=for-the-badge&logo=book" alt="Documentation"/></a>
  <a href="README.tr.md"><img src="https://img.shields.io/badge/🇹🇷-Türkçe_README-red?style=for-the-badge" alt="Turkish README"/></a>
</p>

---

## 🚀 **Quick Use Cases**

### **🏦 Banking**
```csharp
var answer = await intelligence.QueryIntelligenceAsync(
    "Which customers have overdue payments and what's their total outstanding balance?"
);
// → Queries Customer DB, Payment DB, Account DB and combines results
```

### **🏥 Healthcare**
```csharp
var answer = await intelligence.QueryIntelligenceAsync(
    "Show me all patients with diabetes who haven't had their HbA1c checked in 6 months"
);
// → Combines Patient DB, Lab Results DB, Appointment DB and identifies at-risk patients
```

### **📦 Inventory**
```csharp
var answer = await intelligence.QueryIntelligenceAsync(
    "Which products are running low on stock and which suppliers can restock them fastest?"
);
// → Analyzes Inventory DB, Supplier DB, Order History DB and provides restocking recommendations
```

---

## 🚀 **Quick Start**

```csharp
// 1. Setup
builder.Services.UseSmartRAG(builder.Configuration,
    aiProvider: AIProvider.OpenAI,
    storageProvider: StorageProvider.InMemory
);

// 2. Connect databases & upload documents
await connector.ConnectAsync(sqlServer: "Server=localhost;Database=Sales;");
await documents.UploadAsync(files);

// 3. Ask questions
var answer = await intelligence.QueryIntelligenceAsync(
    "Show me customers with over $100K revenue across all databases"
);
// → AI automatically queries SQL Server, MySQL, PostgreSQL and combines results
```

---

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

📚 **[Complete Examples & Testing Guide](https://byerlikaya.github.io/SmartRAG/examples)** - Step-by-step tutorials and test scenarios

---

## 🚀 What Makes SmartRAG Special

✅ **Multi-Database RAG** - Query multiple databases with natural language  
✅ **Multi-Modal Intelligence** - PDF + Excel + Images + Audio + Databases  
✅ **On-Premise Ready** - 100% local with Ollama/LM Studio/Whisper.net  
✅ **Production Ready** - Enterprise-grade error handling and testing  

📚 **[Complete Technical Documentation](https://byerlikaya.github.io/SmartRAG)** - Architecture, API reference, advanced examples

---

## 📦 Installation

### NuGet Package Manager
```bash
Install-Package SmartRAG
```

### .NET CLI
```bash
dotnet add package SmartRAG
```

### Package Manager Console
```powershell
Install-Package SmartRAG
```

---

## 🏆 **Why SmartRAG?**

### **🎯 Multi-Database RAG**
- **Cross-Database Queries**: Query multiple databases simultaneously with natural language
- **Intelligent Data Fusion**: Automatically combines results from different data sources
- **Schema-Aware Processing**: Understands database relationships and foreign keys
- **Real-Time Data Access**: Works with live database connections, not just static exports

### **🧠 Multi-Modal Intelligence**
- **Universal Document Support**: PDF, Word, Excel, PowerPoint, Images, Audio, and more
- **Advanced OCR**: Extract text from images, scanned documents, and handwritten notes
- **Audio Transcription**: Convert speech to text with Whisper.net (99+ languages)
- **Smart Chunking**: Intelligent document segmentation that preserves context

### **🏠 On-Premise Ready**
- **100% Local Processing**: Run everything on your own infrastructure
- **Privacy & Compliance**: GDPR, KVKK, HIPAA compliant with local data processing
- **No Cloud Dependencies**: Works offline with local AI models (Ollama, LM Studio)
- **Enterprise Security**: Full control over data and processing

### **🚀 Production Ready**
- **Enterprise-Grade**: Thread-safe operations, comprehensive error handling
- **High Performance**: Optimized for speed and scalability
- **Comprehensive Testing**: Extensive test coverage and quality assurance
- **Professional Support**: Commercial support and consulting available

---

## 🔧 **Configuration & Setup**

For detailed configuration examples, local AI setup, and enterprise deployment guides:

📚 **[Complete Configuration Guide](https://byerlikaya.github.io/SmartRAG/configuration)**  
🏠 **[Local AI Setup](https://byerlikaya.github.io/SmartRAG/configuration/local-ai)**  
🏢 **[Enterprise Deployment](https://byerlikaya.github.io/SmartRAG/configuration/enterprise)**  
🎤 **[Audio Configuration](https://byerlikaya.github.io/SmartRAG/configuration/audio-ocr)**  
🗄️ **[Database Setup](https://byerlikaya.github.io/SmartRAG/configuration/database)**

---

## 📊 **Comparison with Other RAG Libraries**

| Feature | SmartRAG | Semantic Kernel | LangChain.NET | Other RAG Libraries |
|---------|----------|----------------|---------------|-------------------|
| **Multi-Database RAG** | ✅ Native | ❌ Manual | ❌ Manual | ❌ Not supported |
| **Multi-Modal Support** | ✅ PDF+Excel+Images+Audio+DB | ❌ Limited | ❌ Limited | ❌ Limited |
| **On-Premise Ready** | ✅ 100% Local | ❌ Cloud required | ❌ Cloud required | ❌ Cloud required |
| **Production Ready** | ✅ Enterprise-grade | ⚠️ Basic | ⚠️ Basic | ⚠️ Basic |
| **Cross-Database Queries** | ✅ Automatic | ❌ Not supported | ❌ Not supported | ❌ Not supported |
| **Local AI Support** | ✅ Ollama/LM Studio | ❌ Limited | ❌ Limited | ❌ Limited |
| **Audio Processing** | ✅ Whisper.net | ❌ Not supported | ❌ Not supported | ❌ Not supported |
| **OCR Capabilities** | ✅ Tesseract 5.2.0 | ❌ Not supported | ❌ Not supported | ❌ Not supported |
| **Database Integration** | ✅ SQL Server+MySQL+PostgreSQL+SQLite | ❌ Manual | ❌ Manual | ❌ Manual |
| **Enterprise Features** | ✅ Thread-safe, DI, Logging | ⚠️ Basic | ⚠️ Basic | ⚠️ Basic |

**SmartRAG is the ONLY library that provides true multi-database RAG with cross-database query capabilities.**

---

## 🎯 **Real-World Use Cases**

### **1. Financial Services - Risk Assessment**
```csharp
var answer = await intelligence.QueryIntelligenceAsync(
    "Find customers with credit score below 600 who have missed payments in the last 3 months"
);
// → Queries Credit DB, Payment History DB, Account DB, and Risk Assessment DB
// → Identifies high-risk customers for proactive intervention
```

### **2. Healthcare - Preventive Care**
```csharp
var answer = await intelligence.QueryIntelligenceAsync(
    "Which diabetic patients haven't had their annual eye exam and foot check in the past year?"
);
// → Queries Patient DB, Appointment DB, Diagnosis DB, and Insurance DB
// → Ensures preventive care compliance and reduces complications
```

### **3. E-commerce - Inventory Optimization**
```csharp
var answer = await intelligence.QueryIntelligenceAsync(
    "Which products are frequently returned together and what's causing the high return rate?"
);
// → Queries Order DB, Return DB, Product DB, and Customer Feedback DB
// → Identifies product quality issues and packaging problems
```

### **4. Manufacturing - Predictive Maintenance**
```csharp
var answer = await intelligence.QueryIntelligenceAsync(
    "Which machines show vibration patterns indicating potential failure in the next 30 days?"
);
// → Queries Sensor DB, Maintenance DB, Production DB, and Equipment DB
// → Prevents unplanned downtime and reduces maintenance costs
```

### **5. Education - Early Intervention**
```csharp
var answer = await intelligence.QueryIntelligenceAsync(
    "Which students have declining attendance and falling grades in the same subjects?"
);
// → Queries Attendance DB, Grades DB, Student Support DB, and Family DB
// → Enables early intervention before students drop out
```

### **6. Real Estate - Market Analysis**
```csharp
var answer = await intelligence.QueryIntelligenceAsync(
    "Which neighborhoods have properties selling 20% below market value with good school ratings?"
);
// → Queries Property DB, Sales DB, School DB, and Market Trends DB
// → Identifies undervalued properties with growth potential
```

### **7. Government - Fraud Detection**
```csharp
var answer = await intelligence.QueryIntelligenceAsync(
    "Find citizens receiving multiple benefits from different departments with overlapping eligibility periods"
);
// → Queries Benefits DB, Citizen DB, Eligibility DB, and Payment DB
// → Prevents duplicate benefit payments and reduces fraud
```

### **8. Automotive - Safety Analysis**
```csharp
var answer = await intelligence.QueryIntelligenceAsync(
    "Which vehicle models have the highest accident rates in specific weather conditions?"
);
// → Queries Accident DB, Vehicle DB, Weather DB, and Insurance DB
// → Improves safety recommendations and insurance pricing
```

### **9. Retail - Customer Retention**
```csharp
var answer = await intelligence.QueryIntelligenceAsync(
    "Which customers who bought premium products haven't made a purchase in 90 days?"
);
// → Queries Customer DB, Purchase DB, Product DB, and Engagement DB
// → Identifies at-risk customers for targeted retention campaigns
```

### **10. Research - Trend Analysis**
```csharp
var answer = await intelligence.QueryIntelligenceAsync(
    "What research topics are gaining momentum but have limited funding opportunities?"
);
// → Queries Publication DB, Funding DB, Citation DB, and Grant DB
// → Identifies emerging research areas for funding allocation
```

---

## 🎯 **Supported Data Sources**

### **📄 Document Types**
- **PDF Files** - Text extraction and intelligent chunking
- **Word Documents** - .docx, .doc support with formatting preservation
- **Excel Spreadsheets** - .xlsx, .xls with data analysis capabilities
- **PowerPoint Presentations** - .pptx, .ppt with slide content extraction
- **Text Files** - .txt, .md, .csv with encoding detection
- **Images** - .jpg, .png, .gif, .bmp, .tiff with OCR text extraction
- **Audio Files** - Local transcription with Whisper.net (99+ languages)

### **🗄️ Database Types**
- **SQL Server** - Full support with live connections
- **MySQL** - Complete integration with all data types
- **PostgreSQL** - Advanced support with JSON and custom types
- **SQLite** - Local database support with file-based storage

### **🧠 AI Providers**
- **OpenAI** - GPT-4, GPT-3.5-turbo with function calling
- **Anthropic** - Claude 3.5 Sonnet with VoyageAI embeddings
- **Google** - Gemini Pro with advanced reasoning
- **Azure OpenAI** - Enterprise-grade OpenAI services
- **Custom Providers** - Extensible architecture for any AI service

### **💾 Storage Providers**
- **In-Memory** - Fast development and testing
- **Redis** - High-performance caching and storage
- **Qdrant** - Vector database for semantic search
- **SQLite** - Local file-based storage
- **File System** - Simple file-based document storage

---

## 🏆 **Advanced Features**

### **🧠 Smart Query Intent Detection**
SmartRAG automatically detects whether your query is a general conversation or a document search request:

- **General Conversation**: "How are you?" → Direct AI response
- **Document Search**: "What are the main benefits?" → Searches your documents
- **Multi-Database Query**: "Show me sales data" → Queries connected databases
- **Cross-Database Analysis**: "Compare performance across departments" → Combines data from multiple sources

### **🔍 Enhanced Semantic Search**
- **Hybrid Scoring**: Combines semantic similarity (80%) with keyword relevance (20%)
- **Context Awareness**: Maintains conversation context across queries
- **Multi-Language Support**: Works with any language without hardcoded patterns
- **Intelligent Chunking**: Preserves context and maintains word boundaries

### **🎯 Multi-Modal Intelligence**
- **Document Processing**: PDF, Word, Excel, PowerPoint, Images, Audio
- **OCR Capabilities**: Extract text from images, scanned documents, handwritten notes
- **Audio Transcription**: Convert speech to text with Whisper.net
- **Smart Chunking**: Intelligent document segmentation that preserves context

### **🏠 On-Premise Deployment**
- **Local AI Models**: Ollama, LM Studio integration for complete privacy
- **No Cloud Dependencies**: Works offline with local processing
- **Enterprise Security**: Full control over data and processing
- **Compliance Ready**: GDPR, KVKK, HIPAA compliant with local data processing

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

**Built with ❤️ by Barış Yerlikaya**

Made in Turkey 🇹🇷 | [Contact](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)