---
layout: default
title: IAIRequestExecutor
description: IAIRequestExecutor interface documentation
lang: en
---
## IAIRequestExecutor

**Purpose:** AI request execution with retry/fallback

**Namespace:** `SmartRAG.Interfaces.AI`

Handles AI requests with automatic retry and fallback logic.

#### Methods

##### GenerateResponseAsync

Generates a response using the specified provider.

```csharp
Task<string> GenerateResponseAsync(
    AIProvider provider, 
    string query, 
    IEnumerable<string> context
)
```

**Parameters:**
- `provider` (AIProvider): AI provider to use
- `query` (string): User query
- `context` (IEnumerable<string>): Context strings

**Returns:** AI-generated text response

##### GenerateEmbeddingsAsync

Generates embeddings using the specified provider.

```csharp
Task<List<float>> GenerateEmbeddingsAsync(
    AIProvider provider, 
    string text
)
```

**Parameters:**
- `provider` (AIProvider): AI provider to use
- `text` (string): Text to generate embedding for

**Returns:** Embedding vector

##### GenerateEmbeddingsBatchAsync

Generates batch embeddings using the specified provider.

```csharp
Task<List<List<float>>> GenerateEmbeddingsBatchAsync(
    AIProvider provider, 
    IEnumerable<string> texts
)
```

**Parameters:**
- `provider` (AIProvider): AI provider to use
- `texts` (IEnumerable<string>): Collection of texts

**Returns:** List of embedding vectors


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

