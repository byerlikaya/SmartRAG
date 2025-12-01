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

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

