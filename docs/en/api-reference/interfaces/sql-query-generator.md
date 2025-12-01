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

##### GenerateDatabaseQueriesAsync

Generates optimized SQL queries for each database based on intent.

```csharp
Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent)
```

**Parameters:**
- `queryIntent` (QueryIntent): Query intent to generate SQL for

**Returns:** Updated `QueryIntent` with generated SQL queries


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

