---
layout: default
title: IDatabaseQueryExecutor
description: IDatabaseQueryExecutor interface documentation
lang: en
---
## IDatabaseQueryExecutor

**Purpose:** Execute queries across multiple databases

**Namespace:** `SmartRAG.Interfaces.Database`

Parallel query execution across databases.

#### Methods

```csharp
Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(QueryIntent queryIntent);
```


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

