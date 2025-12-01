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

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

