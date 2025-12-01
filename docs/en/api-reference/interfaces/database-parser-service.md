---
layout: default
title: IDatabaseParserService
description: IDatabaseParserService interface documentation
lang: en
---
## IDatabaseParserService

**Purpose:** Universal database support with live connections

**Namespace:** `SmartRAG.Interfaces.Database`

### Methods

#### ParseDatabaseFileAsync

Parse a database file (SQLite).

```csharp
Task<string> ParseDatabaseFileAsync(Stream dbStream, string fileName)
```

**Example:**

```csharp
using var dbStream = File.OpenRead("catalog.db");
var content = await _databaseService.ParseDatabaseFileAsync(dbStream, "catalog.db");

Console.WriteLine(content); // Extracted table data as text
```

#### ParseDatabaseConnectionAsync

Connect to live database and extract content.

```csharp
Task<string> ParseDatabaseConnectionAsync(
    string connectionString, 
    DatabaseConfig config
)
```

**Example:**

```csharp
var config = new DatabaseConfig
{
    Type = DatabaseType.SqlServer,
    ConnectionString = "Server=localhost;Database=Northwind;Trusted_Connection=true;",
    IncludedTables = new List<string> { "Customers", "Orders", "Products" },
    MaxRowsPerTable = 1000,
    SanitizeSensitiveData = true
};

var content = await _databaseService.ParseDatabaseConnectionAsync(
    config.ConnectionString, 
    config
);
```

#### ExecuteQueryAsync

Execute custom SQL query.

```csharp
Task<string> ExecuteQueryAsync(
    string connectionString, 
    string query, 
    DatabaseType databaseType, 
    int maxRows = 1000
)
```

**Example:**

```csharp
var result = await _databaseService.ExecuteQueryAsync(
    "Server=localhost;Database=Sales;Trusted_Connection=true;",
    "SELECT TOP 10 CustomerID, CompanyName FROM Customers WHERE Country = 'USA'",
    DatabaseType.SqlServer,
    maxRows: 10
);
```

#### GetTableNamesAsync

Get list of table names from database.

```csharp
Task<List<string>> GetTableNamesAsync(
    string connectionString, 
    DatabaseType databaseType
)
```

#### ValidateConnectionAsync

Validate database connection.

```csharp
Task<bool> ValidateConnectionAsync(
    string connectionString, 
    DatabaseType databaseType
)
```

**Example:**

```csharp
bool isValid = await _databaseService.ValidateConnectionAsync(
    "Server=localhost;Database=MyDb;Trusted_Connection=true;",
    DatabaseType.SqlServer
);

if (isValid)
{
    Console.WriteLine("Connection successful!");
}
```

#### GetSupportedDatabaseTypes

Get list of supported database types.

```csharp
IEnumerable<DatabaseType> GetSupportedDatabaseTypes()
```

**Returns:**
- `DatabaseType.SqlServer`
- `DatabaseType.MySQL`
- `DatabaseType.PostgreSql`
- `DatabaseType.Sqlite`

#### GetSupportedDatabaseFileExtensions

Get supported file extensions for database files.

```csharp
IEnumerable<string> GetSupportedDatabaseFileExtensions()
```

**Returns:** `.db`, `.sqlite`, `.sqlite3`


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

