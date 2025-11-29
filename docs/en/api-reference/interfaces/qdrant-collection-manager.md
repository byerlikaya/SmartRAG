---
layout: default
title: IQdrantCollectionManager
description: IQdrantCollectionManager interface documentation
lang: en
---
## IQdrantCollectionManager

**Purpose:** Interface for managing Qdrant collections and document storage

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Collection lifecycle management for Qdrant vector database.

#### Methods

```csharp
Task EnsureCollectionExistsAsync();
Task CreateCollectionAsync(string collectionName, int vectorDimension);
Task EnsureDocumentCollectionExistsAsync(string collectionName, Document document);
Task<int> GetVectorDimensionAsync();
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

