---
layout: default
title: ISemanticSearchService
description: ISemanticSearchService interface documentation
lang: en
---
## ISemanticSearchService

**Purpose:** Advanced semantic search with hybrid scoring

**Namespace:** `SmartRAG.Interfaces.Search`

### Methods

#### CalculateEnhancedSemanticSimilarityAsync

Calculate enhanced semantic similarity using advanced text analysis.

```csharp
Task<double> CalculateEnhancedSemanticSimilarityAsync(
    string query, 
    string content
)
```

**Algorithm:** Hybrid scoring (80% semantic + 20% keyword)

**Returns:** Similarity score between 0.0 and 1.0

**Example:**

```csharp
double similarity = await _semanticSearch.CalculateEnhancedSemanticSimilarityAsync(
    "machine learning algorithms",
    "This document discusses various ML algorithms including neural networks..."
);

Console.WriteLine($"Similarity: {similarity:P}"); // e.g., "Similarity: 85%"
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

