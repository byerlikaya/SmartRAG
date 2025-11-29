---
layout: default
title: IDocumentScoringService
description: IDocumentScoringService interface documentation
lang: en
---
## IDocumentScoringService

**Purpose:** Service for scoring document chunks based on query relevance

**Namespace:** `SmartRAG.Interfaces.Document`

Hybrid scoring strategy with keyword and semantic relevance.

#### Methods

```csharp
List<DocumentChunk> ScoreChunks(List<DocumentChunk> chunks, string query, List<string> queryWords, List<string> potentialNames);
double CalculateKeywordRelevanceScore(string query, string content);
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

