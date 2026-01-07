# SmartRAG Demo

<p align="center">
  <img src="../../icon.svg" alt="SmartRAG Logo" width="100" height="100">
</p>

<p align="center">
  <b>Interactive demonstration application for SmartRAG</b>
</p>

<p align="center">
  <a href="../../README.md"><img src="https://img.shields.io/badge/📚-Main_README-blue?style=for-the-badge&logo=book" alt="Main README"/></a>
  <a href="README.tr.md"><img src="https://img.shields.io/badge/🇹🇷-Türkçe_README-red?style=for-the-badge" alt="Turkish README"/></a>
  <a href="README-Docker.md"><img src="https://img.shields.io/badge/🐳-Docker_Setup-green?style=for-the-badge&logo=docker" alt="Docker Setup"/></a>
</p>

---

## 🚀 **Quick Start**

```bash
# Clone and run the demo
git clone https://github.com/byerlikaya/SmartRAG.git
cd SmartRAG/examples/SmartRAG.Demo
dotnet run
```

### **Prerequisites**
- **.NET 9.0 SDK** - Required for running the demo
- **Docker Desktop** (optional) - For local services (AI, databases, vector stores)
- **OR Cloud AI API Keys** (optional) - For cloud AI providers

## 🐳 **Docker Setup**

For detailed Docker configuration and management:

📖 **[Complete Docker Setup Guide](README-Docker.md)**

## 🎯 **What You Can Test**

### **🔗 Database Management**
- **Step 1-2**: Show connections & health check
- **Step 3-5**: Create test databases (SQL Server, MySQL, PostgreSQL)
- **Step 6**: Create SQLite test database
- **Step 7**: View database schemas and relationships

#### **📁 Database Scripts**
The demo uses Northwind database scripts located in `DatabaseScripts/` directory:
- `instnwnd.sql` - Reference script (SQL Server, all tables) - **DO NOT MODIFY**
- `DatabaseScripts/instnwnd.sqlite.sql` - SQLite: Customers, Categories, Suppliers, Products
- `DatabaseScripts/instnwnd.mysql.sql` - MySQL: Shippers, CustomerDemographics, CustomerCustomerDemo
- `DatabaseScripts/instnwnd.postgresql.sql` - PostgreSQL: Employees, Region, Territories, EmployeeTerritories
- `DatabaseScripts/instnwnd.sqlserver.sql` - SQL Server: Orders, Order Details

Each script contains:
- DROP TABLE statements for clean database creation
- CREATE TABLE statements with proper data types for each database
- Indexes for optimal query performance
- Foreign key constraints where applicable

**Note**: INSERT statements are directly included in the database-specific SQL files, extracted from the reference `instnwnd.sql` file.

### **🤖 AI & Query Testing**
- **Step 8**: Query analysis - see how natural language converts to SQL
- **Step 9**: Automatic test queries - pre-built scenarios
- **Step 10**: Multi-database AI queries - ask questions across all databases

### **🏠 Local AI Setup**
- **Step 11**: Setup Ollama models for 100% local processing
- **Step 12**: Test vector stores (InMemory, FileSystem, Redis, SQLite, Qdrant)

### **📄 Document Processing**
- **Step 13**: Upload documents (PDF, Word, Excel, Images, Audio)
- **Step 14**: List and manage uploaded documents
- **Step 15**: Clear documents for fresh testing
- **Step 16**: Conversational Assistant - combine databases + documents + chat

## 💬 **Example Queries**

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
The application supports queries in multiple languages:
- ISO 639-1 language codes (e.g., "en", "de", "tr", "ru")
- Automatic language detection
- Custom language configuration per query

## 🔧 **Configuration**

### **Configuration Files**
- `appsettings.json` - Main configuration (safe to commit)
- `appsettings.Development.json` - Development settings (git-ignored, contains API keys)

### **Local Mode (Default)**
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
  },
  "Databases": {
    "SQLite": {
      "ConnectionString": "Data Source=TestSQLiteData/demo.db"
    },
    "SQLServer": {
      "ConnectionString": "Server=localhost,1433;Database=PrimaryDemoDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true"
    },
    "MySQL": {
      "ConnectionString": "Server=localhost;Port=3306;Database=SecondaryDemoDB;Uid=root;Pwd=YourPassword123!;"
    },
    "PostgreSQL": {
      "ConnectionString": "Host=localhost;Port=5432;Database=TertiaryDemoDB;Username=postgres;Password=YourPassword123!;"
    }
  }
}
```

### **Cloud Mode**
```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "your-key",
      "Model": "claude-sonnet-4-5"
    }
  },
  "Storage": {
    "Redis": {
      "ConnectionString": "localhost:6379"
    }
  }
}
```

## 🐳 **Docker Services**

| Service | Port | Purpose | Package Used |
|---------|------|---------|--------------|
| Ollama | 11434 | Local AI models | Microsoft.Extensions.Http |
| Qdrant | 6333, 6334 | Vector database | Qdrant.Client |
| Redis | 6379 | Cache & vectors | StackExchange.Redis |
| SQL Server | 1433 | Test database | Microsoft.Data.SqlClient |
| MySQL | 3306 | Test database | MySqlConnector |
| PostgreSQL | 5432 | Test database | Npgsql |
| SQLite | - | Local database | Microsoft.Data.Sqlite |

## 🔒 **Security & Privacy**

### **Configuration Security**
- `appsettings.json` - Safe to commit (no sensitive data)
- `appsettings.Development.json` - Git-ignored (contains API keys)
- See [SECURITY.md](SECURITY.md) for more details

### **Local Mode Benefits**
- ✅ **Zero external API calls** - All processing on your machine
- ✅ **GDPR/KVKK/HIPAA compliant** - Data never leaves your infrastructure
- ✅ **No internet required** - Works completely offline
- ✅ **Full control** - Own your AI and data
- ✅ **Cost-effective** - No API usage charges

## 🛠️ **Troubleshooting**

### **Build Issues**
```bash
# Clean and restore packages
dotnet clean
dotnet restore
dotnet build

# Check .NET version
dotnet --version  # Should be 9.0.x
```

### **Ollama not responding**
```bash
# Check if container is running
docker ps | grep ollama

# Start if not running
docker-compose up -d smartrag-ollama

# Check logs
docker logs smartrag-ollama
```

### **Qdrant connection failed**
```bash
# Check service
docker ps | grep qdrant

# Restart
docker-compose restart smartrag-qdrant

# View logs
docker logs smartrag-qdrant
```

### **Database connection issues**
```bash
# Check all database containers
docker-compose ps

# Start specific database
docker-compose up -d smartrag-sqlserver
docker-compose up -d smartrag-mysql
docker-compose up -d smartrag-postgres
```

### **Configuration Issues**
```bash
# Check if appsettings files exist
ls -la appsettings*.json

# Verify configuration syntax
dotnet run --dry-run
```

## 📚 **Additional Resources**

### **Project Files**
- **Docker Setup Guide**: [README-Docker.md](README-Docker.md)
- **Security Guide**: [SECURITY.md](SECURITY.md)
- **Configuration**: `appsettings.json` (main), `appsettings.Development.json` (dev)

### **Package References**
- **Database Drivers**: Microsoft.Data.Sqlite, Microsoft.Data.SqlClient, MySqlConnector, Npgsql
- **Configuration**: Microsoft.Extensions.Configuration.Json
- **Logging**: Microsoft.Extensions.Logging.Console
- **Cache**: StackExchange.Redis
- **Async Support**: System.Threading.Tasks.Extensions

### **Documentation**
- **Main Documentation**: https://byerlikaya.github.io/SmartRAG/en/
- **GitHub**: https://github.com/byerlikaya/SmartRAG
- **NuGet**: https://www.nuget.org/packages/SmartRAG

## 🤝 **Contact**

For issues or questions:
- **GitHub**: https://github.com/byerlikaya/SmartRAG
- **LinkedIn**: https://www.linkedin.com/in/barisyerlikaya/
- **NuGet**: https://www.nuget.org/packages/SmartRAG
- **Website**: https://byerlikaya.github.io/SmartRAG/en/
- **Email**: b.yerlikaya@outlook.com

## 📄 **License**

This project is part of SmartRAG and follows the same MIT License.

### **Project Information**
- **Target Framework**: .NET 9.0
- **Output Type**: Console Application
- **Implicit Usings**: Enabled
- **Nullable Reference Types**: Enabled
- **License**: MIT