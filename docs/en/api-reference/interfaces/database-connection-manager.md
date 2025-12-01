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


## Related Interfaces

- [Advanced Interfaces]({{ site.baseurl }}/en/api-reference/advanced) - Browse all advanced interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

