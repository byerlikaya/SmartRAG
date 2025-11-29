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

```csharp
Task<Document> AddAsync(Document document);
Task<Document> GetByIdAsync(Guid id);
Task<List<Document>> GetAllAsync();
Task<bool> DeleteAsync(Guid id);
Task<int> GetCountAsync();
Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5);
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

