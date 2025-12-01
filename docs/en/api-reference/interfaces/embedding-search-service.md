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

##### SearchByEmbeddingAsync

Performs embedding-based search on document chunks.

```csharp
Task<List<DocumentChunk>> SearchByEmbeddingAsync(
    string query, 
    List<DocumentChunk> allChunks, 
    int maxResults
)
```

**Parameters:**
- `query` (string): Search query
- `allChunks` (List<DocumentChunk>): All available document chunks
- `maxResults` (int): Maximum number of results to return

**Returns:** List of relevant document chunks sorted by relevance


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

