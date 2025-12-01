---
layout: default
title: IResultMerger
description: IResultMerger interface documentation
lang: en
---
## IResultMerger

**Purpose:** Merge results from multiple databases

**Namespace:** `SmartRAG.Interfaces.Database`

AI-powered result merging.

#### Methods

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

**Returns:** Merged and ranked results as formatted string

##### GenerateFinalAnswerAsync

Generates final AI answer from merged database results.

```csharp
Task<RagResponse> GenerateFinalAnswerAsync(
    string userQuery, 
    string mergedData, 
    MultiDatabaseQueryResult queryResults
)
```

**Parameters:**
- `userQuery` (string): Original user query
- `mergedData` (string): Merged data from databases
- `queryResults` (MultiDatabaseQueryResult): Query results

**Returns:** RAG response with AI-generated answer


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

