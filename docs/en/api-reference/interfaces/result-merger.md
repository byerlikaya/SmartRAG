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

```csharp
Task<string> MergeResultsAsync(MultiDatabaseQueryResult queryResult, string userQuery);
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

