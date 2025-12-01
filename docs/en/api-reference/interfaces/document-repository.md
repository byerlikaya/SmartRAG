---
layout: default
title: IDocumentRepository
description: IDocumentRepository interface documentation
lang: en
---
## IDocumentRepository

**Purpose:** Repository interface for document storage operations

**Namespace:** `SmartRAG.Interfaces.Document`

Separated repository layer from business logic.

#### Methods

##### AddAsync

Adds a new document to storage.

```csharp
Task<Document> AddAsync(Document document)
```

**Parameters:**
- `document` (Document): Document entity to add

**Returns:** Added document entity

##### GetByIdAsync

Retrieves document by unique identifier.

```csharp
Task<Document> GetByIdAsync(Guid id)
```

**Parameters:**
- `id` (Guid): Unique document identifier

**Returns:** Document entity or null if not found

##### GetAllAsync

Retrieves all documents from storage.

```csharp
Task<List<Document>> GetAllAsync()
```

**Returns:** List of all document entities

##### DeleteAsync

Removes document from storage by ID.

```csharp
Task<bool> DeleteAsync(Guid id)
```

**Parameters:**
- `id` (Guid): Unique document identifier

**Returns:** True if document was deleted successfully

##### GetCountAsync

Gets total count of documents in storage.

```csharp
Task<int> GetCountAsync()
```

**Returns:** Total number of documents

##### SearchAsync

Searches documents using query string.

```csharp
Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5)
```

**Parameters:**
- `query` (string): Search query string
- `maxResults` (int): Maximum number of results to return (default: 5)

**Returns:** List of relevant document chunks

##### ClearAllAsync

Clear all documents from storage (efficient bulk delete).

```csharp
Task<bool> ClearAllAsync()
```

**Returns:** True if all documents were cleared successfully


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

