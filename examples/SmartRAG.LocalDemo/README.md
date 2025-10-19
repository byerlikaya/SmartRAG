# SmartRAG Local Demo

Fully local or cloud-enabled demonstration application for SmartRAG - Multi-Database RAG System with support for 100% local AI deployment.

## 🎯 Purpose

This application demonstrates SmartRAG's capabilities in two deployment modes:
- **🏠 LOCAL Mode**: 100% local operation using Ollama, Qdrant, and Redis (GDPR/KVKV/HIPAA compliant)
- **☁️ CLOUD Mode**: Cloud-powered AI with Anthropic Claude, OpenAI GPT, or Google Gemini

## ✨ Key Features

### Environment Selection
- Choose between LOCAL or CLOUD deployment at startup
- Switch between modes without code changes
- Test both environments to compare performance

### Local Environment (100% Privacy-Focused)
- ✅ **AI**: Ollama (llama3.2, phi3, mistral, etc.)
- ✅ **Vector Database**: Qdrant (local docker container)
- ✅ **Cache**: Redis (local docker container)
- ✅ **Databases**: SQL Server, MySQL, PostgreSQL, SQLite
- ✅ **All data stays on your machine** - Perfect for sensitive data

### Cloud Environment (High Performance)
- ⚡ **AI**: Anthropic Claude / OpenAI GPT / Google Gemini
- ⚡ **Vector Database**: Qdrant Cloud
- ⚡ **Cache**: Redis Cloud
- ⚡ **Databases**: Cloud or local databases

## 🗄️ Test Databases

### SQLite - ProductCatalog
- Customers, Products, Categories, Orders, Employees
- Automatically created on first run
- **Location**: `TestData/ProductCatalog.db`

### SQL Server - SalesManagement
- Orders, OrderDetails, Payments, SalesSummary
- References SQLite via CustomerID and ProductID (cross-database joins!)
- **Setup**: Menu option 6 or docker-compose

### MySQL - InventoryManagement
- Stock management and warehouse inventory
- References SQLite Products via ProductID
- **Setup**: Menu option 7 or docker-compose

### PostgreSQL - LogisticsManagement
- Facilities, Shipments, Routes, Carriers
- References other databases for complete logistics tracking
- **Setup**: Menu option 8 or docker-compose

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- Docker Desktop (for local mode)
- OR Cloud AI API keys (for cloud mode)

### 1. Start Docker Services (Local Mode)

```bash
cd examples/SmartRAG.LocalDemo
docker-compose up -d
```

This starts:
- Ollama (AI - port 11434)
- Qdrant (Vector DB - ports 6333, 6334)
- Redis (Cache - port 6379)
- SQL Server (port 1433)
- MySQL (port 3306)
- PostgreSQL (port 5432)

### 2. Setup Ollama Models (Local Mode Only)

```bash
# Download required AI model
docker exec -it smartrag-ollama ollama pull llama3.2

# Download embedding model
docker exec -it smartrag-ollama ollama pull nomic-embed-text
```

Or use menu option 9 after starting the application.

### 3. Configure API Keys (Cloud Mode Only)

Create `appsettings.Development.json` (already git-ignored):

   ```json
{
   "AI": {
     "Anthropic": {
      "ApiKey": "sk-ant-YOUR-KEY-HERE",
      "EmbeddingApiKey": "pa-YOUR-VOYAGE-KEY-HERE"
    }
     }
   }
   ```

### 4. Run the Application

   ```bash
   dotnet run
   ```

## 📋 Menu Options

### Setup & Configuration
- **9. 🤖 Setup Ollama Models** - Download and manage AI models
- **11. 🔧 System Health Check** - Check all services status

### Database Management
- **1. 🔗 Show Database Connections** - View all connected databases
- **2. 📊 Show Database Schemas** - Detailed schema information
- **6-8. Create Test Databases** - Setup SQL Server, MySQL, PostgreSQL

### AI & RAG Features
- **3. 🤖 Multi-Database Query (AI)** - Natural language cross-database queries
- **4. 🔬 Query Analysis** - See generated SQL without execution
- **5. 🧪 Automatic Test Queries** - Run comprehensive test suite
- **10. 📦 Test Vector Store** - Verify Qdrant/Redis connectivity

## 💬 Example Queries

### Cross-Database Queries
```
"What is the total sales amount?"
→ Queries: SQLite (Products) + SQL Server (Orders)

"Show inventory for all warehouses"
→ Queries: SQLite (Products) + MySQL (Stock)

"Which orders are ready to ship?"
→ Queries: SQL Server (Orders) + PostgreSQL (Shipments)

"Calculate total value of products in stock"
→ Queries: SQLite (Products) + MySQL (Stock) with price calculation
```

### Multi-Language Support
The application supports queries in:
- 🇬🇧 English
- 🇩🇪 German (Deutsch)
- 🇹🇷 Turkish (Türkçe)
- 🇷🇺 Russian (Русский)
- 🌐 Custom languages

## 🔧 Configuration

### Local Mode (Default)
```json
{
  "AI": {
    "Custom": {
      "Endpoint": "http://localhost:11434",
      "Model": "llama3.2"
    }
  },
  "Storage": {
    "Qdrant": {
      "Host": "http://localhost:6333"
    }
  }
}
```

### Cloud Mode
```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "your-key",
      "Model": "claude-3-haiku-20240307"
    }
  }
}
```

## 🐳 Docker Services

| Service | Port | Purpose |
|---------|------|---------|
| Ollama | 11434 | Local AI models |
| Qdrant | 6333, 6334 | Vector database |
| Redis | 6379 | Cache & vectors |
| SQL Server | 1433 | Test database |
| MySQL | 3306 | Test database |
| PostgreSQL | 5432 | Test database |

## 🔒 Security & Privacy

### Local Mode Benefits
- ✅ **Zero external API calls** - All processing on your machine
- ✅ **GDPR/KVKK/HIPAA compliant** - Data never leaves your infrastructure
- ✅ **No internet required** - Works completely offline
- ✅ **Full control** - Own your AI and data
- ✅ **Cost-effective** - No API usage charges

### Configuration Security
- `appsettings.json` - Safe to commit (no sensitive data)
- `appsettings.Development.json` - Git-ignored (contains API keys)
- See [SECURITY.md](SECURITY.md) for more details

## 🛠️ Troubleshooting

### Ollama not responding
```bash
# Check if container is running
docker ps | grep ollama

# Start if not running
docker-compose up -d ollama

# Check logs
docker logs smartrag-ollama
```

### Qdrant connection failed
```bash
# Check service
docker ps | grep qdrant

# Restart
docker-compose restart qdrant

# View logs
docker logs smartrag-qdrant
```

### Database connection issues
```bash
# Check all database containers
docker-compose ps

# Start specific database
docker-compose up -d sqlserver
docker-compose up -d mysql
docker-compose up -d postgres
```

## 📚 Additional Resources

- **Docker Setup Guide**: [README-Docker.md](README-Docker.md)
- **Security Guide**: [SECURITY.md](SECURITY.md)
- **Main Documentation**: https://byerlikaya.github.io/SmartRAG/en/
- **GitHub**: https://github.com/byerlikaya/SmartRAG

## 🤝 Contact

- **LinkedIn**: https://www.linkedin.com/in/barisyerlikaya/
- **GitHub**: https://github.com/byerlikaya
- **Email**: b.yerlikaya@outlook.com
- **NuGet**: https://www.nuget.org/packages/SmartRAG

## 📄 License

This project is part of SmartRAG and follows the same MIT License.
