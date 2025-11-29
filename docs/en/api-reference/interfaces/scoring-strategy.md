---
layout: default
title: IScoringStrategy
description: IScoringStrategy interface documentation
lang: en
---
## IScoringStrategy

**Purpose:** Customizable document relevance scoring

**Namespace:** `SmartRAG.Interfaces.Search.Strategies`

Enables custom scoring algorithms for search results.

#### Methods

##### CalculateScoreAsync

Calculate relevance score for a document chunk.

```csharp
Task<double> CalculateScoreAsync(
    string query, 
    DocumentChunk chunk, 
    List<float> queryEmbedding
)
```

**Parameters:**
- `query` (string): Search query
- `chunk` (DocumentChunk): Document chunk to score
- `queryEmbedding` (List<float>): Query embedding vector

**Returns:** Score between 0.0 and 1.0

#### Built-in Implementation

**HybridScoringStrategy** (default):
- 80% semantic similarity (cosine similarity of embeddings)
- 20% keyword matching (BM25-like scoring)

#### Custom Implementation Example

```csharp
public class SemanticOnlyScoringStrategy : IScoringStrategy
{
    public async Task<double> CalculateScoreAsync(
        string query, 
        DocumentChunk chunk, 
        List<float> queryEmbedding)
    {
        // Pure semantic similarity (100% embedding-based)
        return CosineSimilarity(queryEmbedding, chunk.Embedding);
    }
    
    private double CosineSimilarity(List<float> a, List<float> b)
    {
        double dotProduct = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Count; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

