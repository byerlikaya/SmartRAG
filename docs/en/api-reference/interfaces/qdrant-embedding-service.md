---
layout: default
title: IQdrantEmbeddingService
description: IQdrantEmbeddingService interface documentation
lang: en
---
## IQdrantEmbeddingService

**Purpose:** Interface for generating embeddings for text content

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Embedding generation for Qdrant vector storage.

#### Methods

```csharp
Task<List<float>> GenerateEmbeddingAsync(string text);
Task<int> GetVectorDimensionAsync();
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

