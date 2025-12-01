---
layout: default
title: IQdrantSearchService
description: IQdrantSearchService interface documentation
lang: en
---
## IQdrantSearchService

**Purpose:** Interface for performing searches in Qdrant vector database

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Vector, text, and hybrid search capabilities for Qdrant.

#### Methods

```csharp
Task<List<DocumentChunk>> SearchAsync(List<float> queryEmbedding, int maxResults);
Task<List<DocumentChunk>> FallbackTextSearchAsync(string query, int maxResults);
Task<List<DocumentChunk>> HybridSearchAsync(string query, int maxResults);
```


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

