---
layout: default
title: IDatabaseSchemaAnalyzer
description: IDatabaseSchemaAnalyzer interface documentation
lang: en
---
## IDatabaseSchemaAnalyzer

**Purpose:** Analyzes database schemas and generates intelligent metadata

**Namespace:** `SmartRAG.Interfaces.Database`

Extracts comprehensive schema information including tables, columns, relationships, and generates AI-powered summaries.

#### Methods

##### AnalyzeDatabaseSchemaAsync

Analyzes a database connection and extracts comprehensive schema information.

```csharp
Task<DatabaseSchemaInfo> AnalyzeDatabaseSchemaAsync(
    DatabaseConnectionConfig connectionConfig
)
```

**Parameters:**
- `connectionConfig` (DatabaseConnectionConfig): Database connection configuration

**Returns:** Complete `DatabaseSchemaInfo` including tables, columns, foreign keys, and AI-generated summaries

**Example:**

```csharp
var config = new DatabaseConnectionConfig
{
    ConnectionString = "Server=localhost;Database=Northwind;Trusted_Connection=true;",
    DatabaseType = DatabaseType.SqlServer
};

var schemaInfo = await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(config);

Console.WriteLine($"Database: {schemaInfo.DatabaseName}");
Console.WriteLine($"Tables: {schemaInfo.Tables.Count}");
Console.WriteLine($"Total Rows: {schemaInfo.TotalRowCount:N0}");
Console.WriteLine($"AI Summary: {schemaInfo.AISummary}");
```

##### RefreshSchemaAsync

Refreshes schema information for a specific database.

```csharp
Task<DatabaseSchemaInfo> RefreshSchemaAsync(string databaseId)
```

**Parameters:**
- `databaseId` (string): Database identifier

**Returns:** Updated schema information

##### GetAllSchemasAsync

Gets all analyzed database schemas.

```csharp
Task<List<DatabaseSchemaInfo>> GetAllSchemasAsync()
```

**Returns:** List of all database schemas currently in memory

##### GetSchemaAsync

Gets schema for a specific database.

```csharp
Task<DatabaseSchemaInfo> GetSchemaAsync(string databaseId)
```

**Parameters:**
- `databaseId` (string): Database identifier

**Returns:** Database schema information or null if not found

##### GetSchemasNeedingRefreshAsync

Checks if any schemas need refresh based on configured intervals.

```csharp
Task<List<string>> GetSchemasNeedingRefreshAsync()
```

**Returns:** List of database IDs that need schema refresh

**Example:**

```csharp
var needsRefresh = await _schemaAnalyzer.GetSchemasNeedingRefreshAsync();

if (needsRefresh.Any())
{
    Console.WriteLine($"Databases needing refresh: {string.Join(", ", needsRefresh)}");
    
    foreach (var databaseId in needsRefresh)
    {
        await _schemaAnalyzer.RefreshSchemaAsync(databaseId);
    }
}
```

##### GenerateAISummaryAsync

Generates AI-powered summary of database content.

```csharp
Task<string> GenerateAISummaryAsync(DatabaseSchemaInfo schemaInfo)
```

**Parameters:**
- `schemaInfo` (DatabaseSchemaInfo): Schema information to summarize

**Returns:** AI-generated summary describing the database purpose and content


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

