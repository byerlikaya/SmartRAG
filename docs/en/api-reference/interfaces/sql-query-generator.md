---
layout: default
title: ISQLQueryGenerator
description: ISQLQueryGenerator interface documentation
lang: en
---
## ISQLQueryGenerator

**Purpose:** Generate and validate SQL queries

**Namespace:** `SmartRAG.Interfaces.Database`

Uses `ISqlDialectStrategy` for database-specific SQL.

#### Methods

```csharp
Task<string> GenerateSqlAsync(string userQuery, DatabaseSchemaInfo schema, DatabaseType databaseType);
bool ValidateSql(string sql, DatabaseSchemaInfo schema, out string errorMessage);
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

