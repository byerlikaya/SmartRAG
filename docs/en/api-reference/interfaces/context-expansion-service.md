---
layout: default
title: IContextExpansionService
description: IContextExpansionService interface documentation
lang: en
---
## IContextExpansionService

**Purpose:** Expand document chunk context by including adjacent chunks from the same document

**Namespace:** `SmartRAG.Interfaces.Search`

### Methods

#### ExpandContextAsync

Expands context by including adjacent chunks from the same document. This ensures that if a heading is in one chunk and content is in the next, both are included in the search results.

```csharp
Task<List<DocumentChunk>> ExpandContextAsync(
    List<DocumentChunk> chunks, 
    int contextWindow = 2
)
```

**Parameters:**
- `chunks` (List<DocumentChunk>): Initial chunks found by search
- `contextWindow` (int): Number of adjacent chunks to include before and after each found chunk (default: 2, max: 5)

**Returns:** Expanded list of chunks with context, sorted by document ID and chunk index

**Example:**

```csharp
// Search for relevant chunks
var chunks = await _searchService.SearchDocumentsAsync("SRS maintenance", maxResults: 5);

// Expand context to include adjacent chunks
var expandedChunks = await _contextExpansion.ExpandContextAsync(chunks, contextWindow: 2);

// Now expandedChunks includes the heading chunk AND its content chunks
foreach (var chunk in expandedChunks)
{
    Console.WriteLine($"Chunk {chunk.ChunkIndex}: {chunk.Content.Substring(0, 100)}...");
}
```

**Note:** This service is automatically used by `DocumentSearchService` when generating RAG answers. It helps prevent situations where only headings are found without their corresponding content.


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

