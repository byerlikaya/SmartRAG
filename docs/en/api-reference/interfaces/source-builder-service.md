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

```csharp
List<SearchSource> BuildSources(List<DocumentChunk> chunks);
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

