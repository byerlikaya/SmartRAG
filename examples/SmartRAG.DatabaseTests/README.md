# SmartRAG Multi-Database Test Application

Console application designed to test SmartRAG's new **multi-database RAG** features.

## 🎯 Purpose

This application tests the ability to fetch information from multiple databases simultaneously and combine them with AI.

## 🗄️ Test Databases

### SQLite - ProductCatalog.db
**Content:** Customers, Products, Categories
- Categories (5 items)
- Products (15 items)
- Customers (10 items)

### SQL Server - SalesManagement
**Content:** Sales, Orders, Payments
- Orders (references SQLite Customers via CustomerID)
- OrderDetails (references SQLite Products via ProductID)
- Payments
- SalesSummary

**IMPORTANT:** SQL Server tables use SQLite IDs (cross-database join)

## 🚀 Setup

### 1. Create Test Databases

#### SQLite (Automatic)
Created automatically on first run.

#### SQL Server (Automatic via Menu)
Use menu option: `6. Create SQL Server Test Database`

### 2. Set API Keys

Create `appsettings.Development.json`:

```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "sk-ant-YOUR_REAL_KEY",
      "EmbeddingApiKey": "pa-YOUR_VOYAGE_KEY"
    }
  }
}
```

### 3. Run

```bash
cd examples/SmartRAG.DatabaseTests
dotnet run
```

## 💬 Usage

When the application starts:

1. Databases are automatically loaded
2. Schema analysis is performed
3. You can ask questions

### Menu Options

```
1. 🔗 Show Database Connections
2. 📊 Show Database Schemas
3. 🤖 Multi-Database Query (AI)
4. 🔬 Query Analysis (SQL Generation)
5. 🧪 Automatic Test Queries
6. 🗄️  Create SQL Server Test Database
0. 🚪 Exit
```

## 📝 Example Queries

### Multi-Database (Cross-Database) Queries

```
What is the best-selling product?
→ Product info: SQLite, Sales info: SQL Server

What products did customer 1 buy and at what price?
→ Customer: SQLite, Orders: SQL Server, Products: SQLite

What is the total sales amount?
→ SQL Server

How many customers from Istanbul placed orders?
→ Customers: SQLite, Orders: SQL Server
```

### Single Database Queries

```
How many customers are from Istanbul?
→ SQLite only

Which orders are pending?
→ SQL Server only
```

## 🔬 Features

- ✅ Automatic schema analysis
- ✅ AI-powered query routing
- ✅ Cross-database join support
- ✅ Parallel query execution
- ✅ SQL generation display
- ✅ Detailed schema viewing
- ✅ Natural language interface

## 🛠️ Technical Details

### Query Flow

```
User Question
    ↓
AI Query Intent Analysis
    ↓
SQL Generation (for each DB)
    ↓
Parallel Execution
    ↓
Result Merging
    ↓
AI Final Answer
```

### How the System Works

1. **Startup:** Application analyzes all databases
2. **Question:** User asks: "What is the best-selling product?"
3. **AI Analysis:** "Product info in SQLite, sales in SQL Server"
4. **SQL Generation:** Creates custom SQL for each DB
5. **Parallel Execution:** Queries both databases simultaneously
6. **Merging:** Combines results
7. **Final Answer:** AI generates response

## ⚠️ Notes

- If SQL Server is unavailable, works with SQLite only
- First run schema analysis may take 3-5 seconds
- AI calls consume API usage

## 🐛 Troubleshooting

**"Multi-database services could not be loaded"**
→ Check DatabaseConnections section in appsettings.json

**"Connection failed"**
→ Is SQL Server running? Is connection string correct?

**"Schema analysis not completed"**
→ Wait 5 seconds, check manually with "schemas" command
