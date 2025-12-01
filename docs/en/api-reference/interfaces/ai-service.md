---
layout: default
title: IAIService
description: IAIService interface documentation
lang: en
---
## IAIService

**Purpose:** AI provider communication for text generation and embeddings

**Namespace:** `SmartRAG.Interfaces.AI`

### Methods

#### GenerateResponseAsync

Generate AI response based on query and context.

```csharp
Task<string> GenerateResponseAsync(
    string query, 
    IEnumerable<string> context
)
```

**Example:**

```csharp
var contextChunks = new List<string>
{
    "Document chunk 1...",
    "Document chunk 2...",
    "Document chunk 3..."
};

var response = await _aiService.GenerateResponseAsync(
    "What are the main topics?",
    contextChunks
);

Console.WriteLine(response);
```

#### GenerateEmbeddingsAsync

Generate embedding vector for text.

```csharp
Task<List<float>> GenerateEmbeddingsAsync(string text)
```

**Returns:** Embedding vector (typically 768 or 1536 dimensions)

#### GenerateEmbeddingsBatchAsync

Generate embeddings for multiple texts in batch.

```csharp
Task<List<List<float>>> GenerateEmbeddingsBatchAsync(
    IEnumerable<string> texts
)
```

**Example:**

```csharp
var texts = new List<string> { "Text 1", "Text 2", "Text 3" };
var embeddings = await _aiService.GenerateEmbeddingsBatchAsync(texts);

Console.WriteLine($"Generated {embeddings.Count} embeddings");
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

