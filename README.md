# 🚀 SmartRAG - Enterprise-Grade RAG Solution

[![Build Status](https://github.com/byerlikaya/SmartRAG/workflows/CI%2FCD%20Pipeline/badge.svg)](https://github.com/byerlikaya/SmartRAG/actions)
[![NuGet Version](https://img.shields.io/nuget/v/SmartRAG.svg)](https://www.nuget.org/packages/SmartRAG)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SmartRAG.svg)](https://www.nuget.org/packages/SmartRAG)
[![.NET](https://img.shields.io/badge/.NET%20Standard-2.0%2F2.1-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

SmartRAG is a **production-ready** .NET Standard 2.0/2.1 library that provides a complete **Retrieval-Augmented Generation (RAG)** solution for building **AI-powered question answering systems**. Upload your documents, ask questions in natural language, and get intelligent answers based on your content - all with enterprise-grade AI providers and storage options. Compatible with .NET Framework 4.6.1+, .NET Core 2.0+, and .NET 5+ applications.

## ✨ Key Highlights

- 🎯 **AI Question Answering**: Ask questions about your documents and get intelligent, contextual answers
- 🧠 **Smart Query Intent Detection**: Automatically distinguishes between general conversation and document search queries
- 💬 **Conversation History**: Automatic session-based conversation management with context awareness
- 🌍 **Language-Agnostic**: Works with any language without hardcoded patterns or keywords
- 🤖 **Universal AI Support**: 5 dedicated providers + CustomProvider for unlimited AI APIs  
- 🏢 **Enterprise Storage**: Vector databases, Redis, SQL, FileSystem with advanced configurations
- 🧠 **Advanced RAG Pipeline**: Smart chunking, semantic retrieval, AI-powered answer generation
- ⚡ **Lightning Fast**: Optimized vector search with context-aware answer synthesis
- 🔌 **Plug & Play**: Single-line integration with dependency injection
- 📄 **Multi-Format**: PDF, Word, Excel, text files with intelligent parsing
- 🖼️ **Revolutionary OCR**: Enterprise-grade image processing with Tesseract 5.2.0 + SkiaSharp, WebP support, multi-language OCR, table extraction
- 🎵 **Speech-to-Text**: Google Speech-to-Text integration for audio file transcription and analysis
- 🗄️ **Universal Database Support**: SQLite, SQL Server, MySQL, PostgreSQL with live connections, schema analysis, and intelligent data extraction
- 🎯 **Enhanced Semantic Search**: Advanced hybrid scoring with 80% semantic + 20% keyword relevance
- 🔍 **Smart Document Chunking**: Word boundary validation and optimal break points for context preservation
- ✅ **Enterprise Grade**: Zero Warnings Policy, SOLID principles, comprehensive logging, XML documentation
- 🔧 **Cross-Platform Compatibility**: .NET Standard 2.0/2.1 support for maximum framework compatibility
- 📚 **Documentation**: Comprehensive documentation with GitHub Pages support

## 🎯 What Makes SmartRAG Special

### 🚀 **Complete RAG Workflow**
```
📄 Document Upload → 🔍 Smart Chunking → 🧠 AI Embeddings → 💾 Vector Storage
                                                                        ↓
🙋‍♂️ User Question → 🎯 Intent Detection → 🔍 Find Relevant Chunks → 🤖 AI Answer Generation → ✨ Smart Response
```

### 🏆 **Production Features**
- **Revolutionary OCR Capabilities**: Enterprise-grade image processing with Tesseract 5.2.0 + SkiaSharp integration
- **Smart Chunking**: Maintains context continuity between document segments with word boundary validation
- **Intelligent Query Routing**: Automatically routes general conversation to AI chat, document queries to RAG search
- **Conversation History**: Automatic session-based conversation management with intelligent context truncation
- **Language-Agnostic Design**: No hardcoded language patterns - works globally with any language
- **Multiple Storage Options**: From in-memory to enterprise vector databases
- **AI Provider Flexibility**: Switch between providers without code changes
- **Universal Document Intelligence**: Advanced parsing for PDF, Word, Excel, text formats, AND images with OCR
- **Configuration-First**: Environment-based configuration with sensible defaults
- **Dependency Injection**: Full DI container integration
- **Enhanced Semantic Search**: Advanced hybrid scoring combining semantic similarity and keyword relevance
- **VoyageAI Integration**: High-quality embeddings for Anthropic Claude models
- **Cross-Platform Compatibility**: .NET Standard 2.0/2.1 support for maximum framework compatibility
- **Enterprise Architecture**: Zero Warnings Policy, SOLID/DRY principles, comprehensive XML documentation
- **Production Ready**: Thread-safe operations, centralized logging, proper error handling
- **Documentation**: Professional documentation site with GitHub Pages integration

### 🎯 **Revolutionary OCR Use Cases**
- **📄 Scanned Documents**: Upload scanned contracts, reports, forms and get instant intelligent answers
- **🧾 Receipt Processing**: Process receipts, invoices, and financial documents with OCR + RAG intelligence
- **📊 Image-Based Reports**: Extract and query data from charts, graphs, and visual reports
- **✍️ Handwritten Notes**: Transform handwritten notes, annotations into searchable knowledge base
- **📱 Screenshot Analysis**: Process screenshots, UI captures, and digital images with text content
- **🏥 Medical Documents**: Process medical reports, prescriptions, and healthcare documents
- **📚 Educational Materials**: Extract content from textbooks, handouts, and educational images
- **🏢 Business Documents**: Process business cards, presentations, and corporate materials

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

## 🎯 Enhanced Semantic Search & Chunking

### **🧠 Advanced Semantic Search**
SmartRAG uses a sophisticated **hybrid scoring system** that combines multiple relevance factors:

```csharp
// Hybrid Scoring Algorithm (80% Semantic + 20% Keyword)
var hybridScore = (enhancedSemanticScore * 0.8) + (keywordScore * 0.2);

// Enhanced Semantic Similarity
var enhancedSemanticScore = await _semanticSearchService
    .CalculateEnhancedSemanticSimilarityAsync(query, chunk.Content);

// Keyword Relevance
var keywordScore = CalculateKeywordRelevanceScore(query, chunk.Content);
```

**Scoring Components:**
- **Semantic Similarity (80%)**: Advanced text analysis with context awareness
- **Keyword Relevance (20%)**: Traditional text matching and frequency analysis
- **Contextual Enhancement**: Semantic coherence and contextual keyword detection
- **Domain Independence**: Generic scoring without hardcoded domain patterns

### **🔍 Smart Document Chunking**
Advanced chunking algorithm that preserves context and maintains word integrity:

```csharp
// Word Boundary Validation
private static int ValidateWordBoundary(string content, int breakPoint)
{
    // Ensures chunks don't cut words in the middle
    // Finds optimal break points at sentence, paragraph, or word boundaries
    // Maintains semantic continuity between chunks
}

// Optimal Break Point Detection
private static int FindOptimalBreakPoint(string content, int startIndex, int maxChunkSize)
{
    // 1. Sentence boundaries (preferred)
    // 2. Paragraph boundaries (secondary)
    // 3. Word boundaries (fallback)
    // 4. Character boundaries (last resort)
}
```

**Chunking Features:**
- **Word Boundary Protection**: Never cuts words in the middle
- **Context Preservation**: Maintains semantic continuity between chunks
- **Optimal Break Points**: Intelligent selection of chunk boundaries
- **Overlap Management**: Configurable overlap for context continuity
- **Size Optimization**: Dynamic chunk sizing based on content structure

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
<PackageReference Include="SmartRAG" Version="2.4.0" />
```

## 📄 Supported Document Formats

SmartRAG supports a wide range of document formats with intelligent parsing and text extraction:

### **📊 Excel Files (.xlsx, .xls)**
- **Advanced Parsing**: Extracts text from all worksheets and cells
- **Structured Data**: Preserves table structure with tab-separated values
- **Worksheet Names**: Includes worksheet names for context
- **Cell Content**: Extracts all non-empty cell values
- **Format Preservation**: Maintains data organization for better context

### **📝 Word Documents (.docx, .doc)**
- **Rich Text Extraction**: Preserves formatting and structure
- **Table Support**: Extracts content from tables and lists
- **Paragraph Handling**: Maintains paragraph breaks and flow
- **Metadata Preservation**: Keeps document structure intact

### **📋 PDF Documents (.pdf)**
- **Multi-Page Support**: Processes all pages with text extraction
- **Layout Preservation**: Maintains document structure and flow
- **Text Quality**: High-quality text extraction for analysis
- **Page Separation**: Clear page boundaries for context

### **📄 Text Files (.txt, .md, .json, .xml, .csv, .html, .htm)**
- **Universal Support**: Handles all text-based formats
- **Encoding Detection**: Automatic UTF-8 and encoding detection
- **Structure Preservation**: Maintains original formatting
- **Fast Processing**: Optimized for text-based content

### **🖼️ Image Files (.jpg, .jpeg, .png, .gif, .bmp, .tiff, .webp) - REVOLUTIONARY OCR POWER**
- **🚀 Advanced OCR Engine**: Enterprise-grade Tesseract 5.2.0 with SkiaSharp 3.119.0 integration
- **🌍 Multi-Language OCR**: English (eng), Turkish (tur), and extensible language framework
- **🔄 WebP to PNG Conversion**: Seamless WebP image processing using SkiaSharp for Tesseract compatibility
- **📊 Intelligent Table Extraction**: Advanced table detection and structured data parsing from images
- **🎯 Character Whitelisting**: Optimized OCR character recognition for superior accuracy
- **⚡ Image Preprocessing Pipeline**: Advanced image enhancement for maximum OCR performance
- **📈 Confidence Scoring**: Detailed OCR confidence metrics with processing time tracking
- **🔍 Format Auto-Detection**: Automatic image format detection and validation across all supported types
- **🏗️ Structured Data Output**: Converts images to searchable, queryable knowledge base content

### **🎵 Audio Files (.mp3, .wav, .m4a, .aac, .ogg, .flac, .wma) - SPEECH-TO-TEXT REVOLUTION**
- **🎤 Google Speech-to-Text**: Enterprise-grade speech recognition with Google Cloud AI
- **🌍 Multi-Language Support**: Turkish (tr-TR), English (en-US), and 100+ languages supported
- **⚡ Real-time Transcription**: Advanced speech-to-text conversion with confidence scoring
- **📊 Detailed Results**: Segment-level transcription with timestamps and confidence metrics
- **🔍 Audio Format Detection**: Automatic format validation and content type recognition
- **🎯 Intelligent Processing**: Smart audio stream validation and error handling
- **📈 Performance Optimized**: Efficient audio processing with minimal memory footprint
- **🏗️ Structured Output**: Converts audio content to searchable, queryable knowledge base

### **🗄️ Database Files (.db, .sqlite, .sqlite3) - ENTERPRISE DATABASE INTEGRATION**
- **🚀 Universal Database Support**: SQLite, SQL Server, MySQL, PostgreSQL with live connections
- **📊 Intelligent Schema Analysis**: Automatic table schema extraction with data types and constraints
- **🔗 Relationship Mapping**: Foreign key relationships and index information extraction
- **🛡️ Security-First**: Automatic sensitive data sanitization and configurable data protection
- **⚡ Performance Optimized**: Configurable row limits, query timeouts, and connection pooling
- **🎯 Smart Filtering**: Include/exclude specific tables with advanced filtering options
- **📈 Enterprise Features**: Connection validation, custom SQL query execution, and error handling
- **🌐 Cross-Platform**: Works with cloud databases (Azure SQL, AWS RDS, Google Cloud SQL)
- **🔍 Metadata Extraction**: Column details, primary keys, indexes, and database version information
- **🏗️ Structured Output**: Converts database content to searchable, queryable knowledge base

### **🔍 Content Type Support**
SmartRAG automatically detects file types using both file extensions and MIME content types:
- **Excel**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `application/vnd.ms-excel`
- **Word**: `application/vnd.openxmlformats-officedocument.wordprocessingml.document`, `application/msword`
- **PDF**: `application/pdf`
- **Text**: `text/*`, `application/json`, `application/xml`, `application/csv`
- **Images**: `image/jpeg`, `image/png`, `image/gif`, `image/bmp`, `image/tiff`, `image/webp`
- **Audio**: `audio/mpeg`, `audio/wav`, `audio/mp4`, `audio/aac`, `audio/ogg`, `audio/flac`, `audio/x-ms-wma`
- **Databases**: `application/x-sqlite3`, `application/vnd.sqlite3`, `application/octet-stream`

## 🚀 Quick Start

### 1. **Development Setup**
```bash
# Clone the repository
git clone https://github.com/byerlikaya/SmartRAG.git
cd SmartRAG

# Copy development configuration template
cp examples/WebAPI/appsettings.Development.template.json examples/WebAPI/appsettings.Development.json

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

### 3. **Upload Documents & Connect Databases**
```csharp
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDatabaseParserService _databaseService;

    // Upload files (PDF, Word, Excel, Images, Audio, SQLite databases)
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

    // Connect to live databases (SQL Server, MySQL, PostgreSQL)
    [HttpPost("connect-database")]
    public async Task<IActionResult> ConnectDatabase([FromBody] DatabaseRequest request)
    {
        var config = new DatabaseConfig
        {
            Type = request.DatabaseType,
            ConnectionString = request.ConnectionString,
            IncludedTables = request.Tables,
            MaxRowsPerTable = 1000,
            SanitizeSensitiveData = true
        };

        var content = await _databaseService.ParseDatabaseConnectionAsync(
            request.ConnectionString, 
            config);
        
        return Ok(new { content, message = "Database connected successfully" });
    }
}
```

### 4. **AI-Powered Question Answering with Conversation History**
```csharp
public class QAController : ControllerBase
{
    private readonly IDocumentSearchService _documentSearchService;

    [HttpPost("ask")]
    public async Task<IActionResult> AskQuestion([FromBody] QuestionRequest request)
    {
        // User asks: "What are the main benefits mentioned in the contract?"
        var response = await _documentSearchService.GenerateRagAnswerAsync(
            request.Question,
            maxResults: 5
        );
        
        // Returns intelligent answer based on document content + conversation context
        return Ok(response);
    }
}

public class QuestionRequest
{
    public string Question { get; set; } = string.Empty;
}
```

### 5. **Configuration**

⚠️ **Security Note**: Never commit real API keys! Use `appsettings.Development.json` for local development.

```bash
# Copy template and add your real keys
cp examples/WebAPI/appsettings.json examples/WebAPI/appsettings.Development.json
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
  },
  "Database": {
    "MaxRowsPerTable": 1000,
    "QueryTimeoutSeconds": 30,
    "SanitizeSensitiveData": true,
    "SensitiveColumns": ["password", "ssn", "credit_card", "email"]
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

## 💬 Conversation History

SmartRAG includes **automatic conversation history management** that maintains context across multiple questions within a session. This enables more natural, contextual conversations with your AI system.

### **Key Features**
- **Session-Based**: Each conversation is tied to a unique session ID
- **Automatic Management**: No manual conversation handling required
- **Context Awareness**: Previous questions and answers inform current responses
- **Intelligent Truncation**: Automatically manages conversation length to prevent token limits
- **Storage Integration**: Uses your configured storage provider for persistence

### **How It Works**
```csharp
// First question in session
var response1 = await _documentSearchService.GenerateRagAnswerAsync(
    "What is the company's refund policy?",
    maxResults: 5
);

// Follow-up question - AI remembers previous context
var response2 = await _documentSearchService.GenerateRagAnswerAsync(
    "What about international orders?",  // AI knows this relates to refund policy
    maxResults: 5
);
```

### **Conversation Flow Example**
```
User: "What is the company's refund policy?"
AI: "Based on the policy document, customers can request refunds within 30 days..."

User: "What about international orders?"  // AI remembers previous context
AI: "For international orders, the refund policy extends to 45 days due to shipping considerations..."

User: "How do I initiate a refund?"  // AI maintains full conversation context
AI: "To initiate a refund, you can contact customer service or use the online portal..."
```

### **Session Management**
- **Unique Session IDs**: Generate unique identifiers for each user/conversation
- **Automatic Cleanup**: Old conversations are automatically truncated to maintain performance
- **Cross-Request Persistence**: Conversation history persists across multiple API calls
- **Privacy**: Each session is isolated - no cross-contamination between users

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
                                │
                                ▼
                       ┌──────────────────┐
                       │ Semantic Search  │
                       │                  │
                       │ • Hybrid Scoring │
                       │ • Context Aware  │
                       │ • Word Boundary  │
                       └──────────────────┘
```

### **Key Components**

- **📄 DocumentService**: Main orchestrator for document operations
- **🤖 AIService**: Handles AI provider interactions and embeddings  
- **📝 DocumentParserService**: Multi-format document parsing with smart chunking
- **🔍 SemanticSearchService**: Advanced semantic search with hybrid scoring
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

### **Database Management**
```bash
# Upload SQLite database file
curl -X POST "http://localhost:5000/api/documents/upload" \
  -F "file=@company.db"

# Connect to live SQL Server database
curl -X POST "http://localhost:5000/api/database/connect-database" \
  -H "Content-Type: application/json" \
  -d '{
    "connectionString": "Server=localhost;Database=Northwind;Trusted_Connection=true;",
    "databaseType": "SqlServer",
    "includedTables": ["Customers", "Orders", "Products"],
    "maxRows": 1000,
    "sanitizeSensitiveData": true
  }'

# Connect to MySQL database
curl -X POST "http://localhost:5000/api/database/connect-database" \
  -H "Content-Type: application/json" \
  -d '{
    "connectionString": "Server=localhost;Database=sakila;Uid=root;Pwd=password;",
    "databaseType": "MySQL",
    "includedTables": ["actor", "film", "customer"]
  }'

# Connect to PostgreSQL database
curl -X POST "http://localhost:5000/api/database/connect-database" \
  -H "Content-Type: application/json" \
  -d '{
    "connectionString": "Host=localhost;Database=dvdrental;Username=postgres;Password=password;",
    "databaseType": "PostgreSQL",
    "includeSchema": true,
    "includeForeignKeys": true
  }'

# Execute custom SQL query
curl -X POST "http://localhost:5000/api/database/execute-query" \
  -H "Content-Type: application/json" \
  -d '{
    "connectionString": "Server=localhost;Database=Northwind;Trusted_Connection=true;",
    "query": "SELECT TOP 10 CustomerID, CompanyName FROM Customers WHERE Country = '\''USA'\''",
    "databaseType": "SqlServer",
    "maxRows": 10
  }'
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
- **Enhanced Chunking**: ~300ms for 10KB document with smart boundary detection
- **Hybrid Scoring**: ~150ms for semantic + keyword relevance calculation

### **Scaling Tips**
- Use **Redis** or **Qdrant** for production workloads
- Enable **connection pooling** for database connections
- Implement **caching** for frequently accessed documents
- Use **background services** for bulk document processing
- Optimize **chunk sizes** based on your content type
- Use **semantic search threshold** to filter low-relevance results

## 🧪 Testing

SmartRAG includes comprehensive testing with xUnit and follows best practices:

### **Test Project Structure**
```
tests/
└── SmartRAG.Tests/
    ├── FileUploadTests.cs          # File upload functionality tests
    ├── GlobalUsings.cs             # Centralized using directives
    └── SmartRAG.Tests.csproj      # Test project configuration
```

### **Test Features**
- ✅ **xUnit Framework**: Modern, extensible testing framework
- ✅ **GlobalUsings**: Clean, maintainable test code
- ✅ **File Upload Tests**: Comprehensive document upload testing
- ✅ **Mock Support**: Moq integration for dependency mocking
- ✅ **Async Testing**: Full async/await support

### **Running Tests**
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/SmartRAG.Tests/

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🛠️ Development

### **Building from Source**
```bash
git clone https://github.com/byerlikaya/SmartRAG.git
cd SmartRAG
dotnet restore
dotnet build
dotnet test
```

### **Running Tests**
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/SmartRAG.Tests/

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### **Running the Sample API**
```bash
cd examples/WebAPI
dotnet run
```

Browse to `https://localhost:5001/swagger` for interactive API documentation.

## 🤝 Contributing

We welcome contributions!

### **Development Setup**
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 🆕 What's New

### **Latest Release (v2.4.0) - Universal Database Support**
- 🗄️ **Universal Database Integration** - Complete support for SQLite, SQL Server, MySQL, PostgreSQL databases
- 🔗 **Live Database Connections** - Connect to production databases with real-time data extraction
- 📊 **Intelligent Schema Analysis** - Automatic table schema extraction with data types, constraints, and relationships
- 🛡️ **Enterprise Security** - Automatic sensitive data sanitization with configurable protection patterns
- 🎯 **Smart Table Filtering** - Include/exclude specific tables with advanced filtering capabilities
- ⚡ **Performance Optimized** - Configurable row limits, query timeouts, and efficient data processing
- 🔍 **Custom SQL Queries** - Execute custom SQL queries with formatted results for advanced analysis
- 📈 **Metadata Extraction** - Foreign keys, indexes, primary keys, and database version information
- 🌐 **Cloud Database Support** - Works with Azure SQL, AWS RDS, Google Cloud SQL, and other cloud providers
- 🏗️ **Enterprise Architecture** - SOLID principles, comprehensive logging, and production-ready implementation
- ✅ **Zero Warnings Policy** - Maintained code quality standards with full XML documentation
- 📚 **Documentation Updates** - All language versions updated with comprehensive database examples

### **Previous Release (v2.3.0) - Google Speech-to-Text Integration**
- 🎵 **Google Speech-to-Text Integration** - Enterprise-grade speech recognition with Google Cloud AI
- 🌍 **Enhanced Language Support** - 100+ languages including Turkish, English, and global languages
- ⚡ **Real-time Audio Processing** - Advanced speech-to-text conversion with confidence scoring
- 📊 **Detailed Transcription Results** - Segment-level transcription with timestamps and confidence metrics
- 🔍 **Automatic Format Detection** - Support for MP3, WAV, M4A, AAC, OGG, FLAC, WMA formats
- 🎯 **Intelligent Audio Processing** - Smart audio stream validation and error handling
- 📈 **Performance Optimized** - Efficient audio processing with minimal memory footprint
- 🏗️ **Structured Audio Output** - Converts audio content to searchable, queryable knowledge base
- ✅ **Zero Warnings Policy** - Maintained with comprehensive error handling and logging
- 📚 **Documentation Updates** - All language versions updated with Google Speech-to-Text examples

### **Previous Release (v2.2.0) - Enhanced OCR Documentation**
- 🖼️ **Enhanced OCR Documentation** - Comprehensive documentation showcasing OCR capabilities
- 📚 **Improved README** - Detailed image processing features highlighting Tesseract 5.2.0 + SkiaSharp
- 🎯 **Use Case Examples** - Added detailed examples for scanned documents, receipts, and image content
- 📈 **Developer Experience** - Better visibility of image processing features for developers

### **Previous Release (v2.1.0) - Automatic Session Management**
- 🎯 **Automatic Session Management** - No more manual session ID handling required
- 💬 **Persistent Conversation History** - Conversations survive application restarts
- 🆕 **New Conversation Commands** - `/new`, `/reset`, `/clear` for conversation control
- 🔄 **Enhanced API** - Backward-compatible with optional `startNewConversation` parameter
- 🗄️ **Storage Integration** - Works seamlessly with all providers (Redis, SQLite, FileSystem, InMemory)
- 🔧 **Format Consistency** - Standardized conversation format across all storage providers
- 🧵 **Thread Safety** - Enhanced concurrent access handling for conversation operations
- 🌍 **Platform Agnostic** - Maintains compatibility with all .NET environments
- 📚 **Documentation Updates** - All language versions (EN, TR, DE, RU) updated with real examples
- ✅ **100% Compliance** - All established rules maintained with zero warnings policy

### **Previous Release (v1.0.3)**
- 🧠 **Smart Query Intent Detection** - Automatically routes queries to chat vs document search
- 🌍 **Language-Agnostic Design** - Removed all hardcoded language patterns  
- 🔍 **Enhanced Search Relevance** - Improved name detection and content scoring
- 🔤 **Unicode Normalization** - Fixed special character handling issues
- ⚡ **Rate Limiting & Retry Logic** - Robust API handling with exponential backoff
- 🚀 **VoyageAI Integration** - Anthropic embedding support
- 📚 **Enhanced Documentation** - Official documentation links
- 🧹 **Configuration Cleanup** - Removed unnecessary fields
- 🎯 **Project Simplification** - Streamlined for better performance

### **Architecture & Code Quality**
- 🎯 **Enhanced Semantic Search** - Advanced hybrid scoring (80% semantic + 20% keyword)
- 🔍 **Smart Document Chunking** - Word boundary validation and optimal break points
- 🧠 **SemanticSearchService** - Dedicated service for semantic relevance scoring
- ⚙️ **Configuration Management** - User settings take absolute priority
- 🔧 **Enterprise Error Handling** - Comprehensive logging and retry mechanisms
- 📊 **Performance Optimizations** - Faster chunking and search algorithms
- ✅ **Code Quality** - SOLID principles, zero warnings, comprehensive documentation

## 📚 Resources

- **📧 [Contact & Support](mailto:b.yerlikaya@outlook.com)**
- **💼 [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)**
- **🐙 [GitHub Profile](https://github.com/byerlikaya)**
- **📦 [NuGet Packages](https://www.nuget.org/profiles/barisyerlikaya)**
- **📖 [Documentation](https://byerlikaya.github.io/SmartRAG)** - Comprehensive guides and API reference

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.



**Built with ❤️ by Barış Yerlikaya**

Made in Turkey 🇹🇷 | [Contact](mailto:b.yerlikaya@outlook.com) | [LinkedIn](https://www.linkedin.com/in/barisyerlikaya/)