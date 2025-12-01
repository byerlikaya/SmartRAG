---
layout: default
title: ISourceBuilderService
description: ISourceBuilderService interface documentation
lang: en
---
## ISourceBuilderService

**Purpose:** Build search result sources

**Namespace:** `SmartRAG.Interfaces.Search`

Constructs `SearchSource` objects from chunks.

#### Methods

##### BuildSourcesAsync

Builds search sources from document chunks.

```csharp
Task<List<SearchSource>> BuildSourcesAsync(
    List<DocumentChunk> chunks, 
    IDocumentRepository documentRepository
)
```

**Parameters:**
- `chunks` (List<DocumentChunk>): Document chunks to build sources from
- `documentRepository` (IDocumentRepository): Repository for document operations

**Returns:** List of search sources with metadata


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

