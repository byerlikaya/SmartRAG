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

##### EnsureCollectionExistsAsync

Ensures the main collection exists and is ready for operations.

```csharp
Task EnsureCollectionExistsAsync()
```

##### CreateCollectionAsync

Creates a new collection with specified vector parameters.

```csharp
Task CreateCollectionAsync(string collectionName, int vectorDimension)
```

**Parameters:**
- `collectionName` (string): Name of the collection to create
- `vectorDimension` (int): Dimension of vectors to store

##### EnsureDocumentCollectionExistsAsync

Ensures a document-specific collection exists.

```csharp
Task EnsureDocumentCollectionExistsAsync(
    string collectionName, 
    Document document
)
```

**Parameters:**
- `collectionName` (string): Name of the document collection
- `document` (Document): Document to store

##### GetVectorDimensionAsync

Gets the vector dimension for collections.

```csharp
Task<int> GetVectorDimensionAsync()
```

**Returns:** Vector dimension

##### DeleteCollectionAsync

Deletes a collection completely.

```csharp
Task DeleteCollectionAsync(string collectionName)
```

**Parameters:**
- `collectionName` (string): Name of the collection to delete

##### RecreateCollectionAsync

Recreates a collection (deletes and creates anew).

```csharp
Task RecreateCollectionAsync(string collectionName)
```

**Parameters:**
- `collectionName` (string): Name of the collection to recreate


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

