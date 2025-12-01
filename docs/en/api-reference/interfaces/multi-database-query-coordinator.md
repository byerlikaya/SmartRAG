---
layout: default
title: IMultiDatabaseQueryCoordinator
description: IMultiDatabaseQueryCoordinator interface documentation
lang: en
---
## IMultiDatabaseQueryCoordinator

**Purpose:** Coordinates intelligent multi-database queries using AI

**Namespace:** `SmartRAG.Interfaces.Database`

This interface enables querying across multiple databases simultaneously using natural language. The AI analyzes the query, determines which databases and tables to access, generates optimized SQL queries, and merges results into a coherent response.

#### Methods

##### QueryMultipleDatabasesAsync

Executes a full intelligent query: analyze intent + execute + merge results.

```csharp
Task<RagResponse> QueryMultipleDatabasesAsync(
    string userQuery, 
    int maxResults = 5
)
```

**Parameters:**
- `userQuery` (string): Natural language user query
- `maxResults` (int): Maximum number of results to return (default: 5)

**Returns:** `RagResponse` with AI-generated answer and data from multiple databases

**Example:**

```csharp
var response = await _coordinator.QueryMultipleDatabasesAsync(
    "Show records from TableA with their corresponding TableB details"
);

Console.WriteLine(response.Answer);
// AI answer combining data from multiple databases
```

##### AnalyzeQueryIntentAsync

Analyzes user query and determines which databases/tables to query.

```csharp
Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery)
```

**Parameters:**
- `userQuery` (string): Natural language user query

**Returns:** `QueryIntent` with database routing information

**Example:**

```csharp
var intent = await _coordinator.AnalyzeQueryIntentAsync(
    "Compare data between Database1 and Database2"
);

Console.WriteLine($"Confidence: {intent.Confidence}");
Console.WriteLine($"Requires Cross-DB Join: {intent.RequiresCrossDatabaseJoin}");

foreach (var dbQuery in intent.DatabaseQueries)
{
    Console.WriteLine($"Database: {dbQuery.DatabaseName}");
    Console.WriteLine($"Tables: {string.Join(", ", dbQuery.RequiredTables)}");
}
```

##### ExecuteMultiDatabaseQueryAsync

Executes queries across multiple databases based on query intent.

```csharp
Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(
    QueryIntent queryIntent
)
```

**Parameters:**
- `queryIntent` (QueryIntent): Analyzed query intent

**Returns:** `MultiDatabaseQueryResult` with combined results from all databases

##### GenerateDatabaseQueriesAsync

Generates optimized SQL queries for each database based on intent.

```csharp
Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent)
```

**Parameters:**
- `queryIntent` (QueryIntent): Query intent to generate SQL for

**Returns:** Updated `QueryIntent` with generated SQL queries

##### MergeResultsAsync

Merges results from multiple databases into a coherent response.

```csharp
Task<string> MergeResultsAsync(
    MultiDatabaseQueryResult queryResults, 
    string originalQuery
)
```

**Parameters:**
- `queryResults` (MultiDatabaseQueryResult): Results from multiple databases
- `originalQuery` (string): Original user query

**Returns:** Merged and formatted results as string


## Related Interfaces

- [Advanced Interfaces]({{ site.baseurl }}/en/api-reference/advanced) - Browse all advanced interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

