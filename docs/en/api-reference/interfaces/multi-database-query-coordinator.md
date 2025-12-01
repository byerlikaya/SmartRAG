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

**Overload 1:** Analyze query intent and execute query.

```csharp
Task<RagResponse> QueryMultipleDatabasesAsync(
    string userQuery, 
    int maxResults = 5
)
```

**Overload 2:** Execute query using pre-analyzed query intent (avoids redundant AI calls).

```csharp
Task<RagResponse> QueryMultipleDatabasesAsync(
    string userQuery, 
    QueryIntent preAnalyzedIntent,
    int maxResults = 5
)
```

**Parameters:**
- `userQuery` (string): Natural language user query
- `preAnalyzedIntent` (QueryIntent, optional): Pre-analyzed query intent to avoid redundant AI calls
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

**Example with pre-analyzed intent:**

```csharp
// Pre-analyze intent once
var intent = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(userQuery);

// Use pre-analyzed intent for multiple queries
var response1 = await _coordinator.QueryMultipleDatabasesAsync(userQuery, intent);
var response2 = await _coordinator.QueryMultipleDatabasesAsync(userQuery, intent);
```

##### AnalyzeQueryIntentAsync (Deprecated)

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Deprecated</h4>
    <p class="mb-0">
        Use <code>IQueryIntentAnalyzer.AnalyzeQueryIntentAsync</code> instead. This method will be removed in v4.0.0.
        Legacy method provided for backward compatibility.
    </p>
</div>

Analyzes user query and determines which databases/tables to query.

```csharp
[Obsolete("Use IQueryIntentAnalyzer.AnalyzeQueryIntentAsync instead")]
Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery)
```

**Parameters:**
- `userQuery` (string): Natural language user query

**Returns:** `QueryIntent` with database routing information

**Note:** This method is deprecated. Use `IQueryIntentAnalyzer.AnalyzeQueryIntentAsync` instead.

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

##### MultiDatabaseQueryResult

Results from multi-database query execution.

```csharp
public class MultiDatabaseQueryResult
{
    public Dictionary<string, DatabaseQueryResult> DatabaseResults { get; set; }
    public bool Success { get; set; }
    public List<string> Errors { get; set; }
    public long ExecutionTimeMs { get; set; }
}
```

##### DatabaseQueryResult

Results from a single database query.

```csharp
public class DatabaseQueryResult
{
    public string DatabaseId { get; set; }
    public string DatabaseName { get; set; }
    public string ExecutedQuery { get; set; }
    public string ResultData { get; set; }
    public int RowCount { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }
}
```


## Related Interfaces

- [Advanced Interfaces]({{ site.baseurl }}/en/api-reference/advanced) - Browse all advanced interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

