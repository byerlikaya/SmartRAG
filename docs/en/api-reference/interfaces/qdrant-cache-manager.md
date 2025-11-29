---
layout: default
title: IQdrantCacheManager
description: IQdrantCacheManager interface documentation
lang: en
---
## IQdrantCacheManager

**Purpose:** Interface for managing search result caching in Qdrant operations

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Search result caching for performance optimization.

#### Methods

```csharp
List<DocumentChunk> GetCachedResults(string queryHash);
void CacheResults(string queryHash, List<DocumentChunk> results);
void CleanupExpiredCache();
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

