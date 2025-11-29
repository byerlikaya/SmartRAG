---
layout: default
title: IDatabaseConnectionManager
description: IDatabaseConnectionManager interface documentation
lang: en
---
## IDatabaseConnectionManager

**Purpose:** Manages database connections from configuration

**Namespace:** `SmartRAG.Interfaces.Database`

Handles database connection lifecycle, validation, and runtime management.

#### Methods

##### InitializeAsync

Initializes all database connections from configuration.

```csharp
Task InitializeAsync()
```

**Example:**

```csharp
await _connectionManager.InitializeAsync();
Console.WriteLine("All database connections initialized");
```

##### GetAllConnectionsAsync

Gets all configured database connections.

```csharp
Task<List<DatabaseConnectionConfig>> GetAllConnectionsAsync()
```

**Returns:** List of all database connection configurations

##### GetConnectionAsync

Gets a specific connection by ID.

```csharp
Task<DatabaseConnectionConfig> GetConnectionAsync(string databaseId)
```

**Parameters:**
- `databaseId` (string): Database identifier

**Returns:** Connection configuration or null if not found

##### ValidateAllConnectionsAsync

Validates all configured connections.

```csharp
Task<Dictionary<string, bool>> ValidateAllConnectionsAsync()
```

**Returns:** Dictionary of database IDs and their validation status (true = valid, false = invalid)

**Example:**

```csharp
var validationResults = await _connectionManager.ValidateAllConnectionsAsync();

foreach (var (databaseId, isValid) in validationResults)
{
    Console.WriteLine($"{databaseId}: {(isValid ? "Valid" : "Invalid")}");
}
```

##### ValidateConnectionAsync

Validates a specific connection.

```csharp
Task<bool> ValidateConnectionAsync(string databaseId)
```

**Parameters:**
- `databaseId` (string): Database identifier

**Returns:** True if connection is valid, false otherwise

##### GetDatabaseIdAsync

Gets database identifier from connection (auto-generates if Name not provided).

```csharp
Task<string> GetDatabaseIdAsync(DatabaseConnectionConfig connectionConfig)
```

**Parameters:**
- `connectionConfig` (DatabaseConnectionConfig): Connection configuration

**Returns:** Unique database identifier

##### AddConnectionAsync

Adds a new database connection at runtime.

```csharp
Task<string> AddConnectionAsync(DatabaseConnectionConfig connectionConfig)
```

**Parameters:**
- `connectionConfig` (DatabaseConnectionConfig): Connection configuration

**Returns:** Generated database identifier

**Example:**

```csharp
var config = new DatabaseConnectionConfig
{
    Name = "SalesDB",
    ConnectionString = "Server=localhost;Database=Sales;Trusted_Connection=true;",
    DatabaseType = DatabaseType.SqlServer,
    Enabled = true
};

var databaseId = await _connectionManager.AddConnectionAsync(config);
Console.WriteLine($"Added database with ID: {databaseId}");
```

##### RemoveConnectionAsync

Removes a database connection.

```csharp
Task RemoveConnectionAsync(string databaseId)
```

**Parameters:**
- `databaseId` (string): Database identifier to remove


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

