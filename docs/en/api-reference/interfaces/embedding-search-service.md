---
layout: default
title: IEmbeddingSearchService
description: IEmbeddingSearchService interface documentation
lang: en
---
## IEmbeddingSearchService

**Purpose:** Embedding-based semantic search

**Namespace:** `SmartRAG.Interfaces.Search`

Core embedding search functionality.

#### Methods

```csharp
Task<List<DocumentChunk>> SearchByEmbeddingAsync(List<float> queryEmbedding, int maxResults = 5);
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

