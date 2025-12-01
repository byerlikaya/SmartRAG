---
layout: default
title: IAIProvider
description: IAIProvider interface documentation
lang: en
---
## IAIProvider

**Purpose:** Low-level AI provider interface for text generation and embeddings

**Namespace:** `SmartRAG.Interfaces.AI`

Provider abstraction for multiple AI backends.

#### Methods

##### GenerateTextAsync

Generates text response using the AI provider.

```csharp
Task<string> GenerateTextAsync(string prompt, AIProviderConfig config)
```

**Parameters:**
- `prompt` (string): Text prompt to send to the AI provider
- `config` (AIProviderConfig): AI provider configuration settings

**Returns:** AI-generated text response

##### GenerateEmbeddingAsync

Generates embedding vector for the given text.

```csharp
Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config)
```

**Parameters:**
- `text` (string): Text to generate embedding for
- `config` (AIProviderConfig): AI provider configuration settings

**Returns:** List of float values representing the embedding vector

##### GenerateEmbeddingsBatchAsync

Generates embeddings for multiple texts in a single request (if supported).

```csharp
Task<List<List<float>>> GenerateEmbeddingsBatchAsync(
    IEnumerable<string> texts, 
    AIProviderConfig config
)
```

**Parameters:**
- `texts` (IEnumerable<string>): Collection of texts to generate embeddings for
- `config` (AIProviderConfig): AI provider configuration settings

**Returns:** List of embedding vectors, one for each input text

##### ChunkTextAsync

Chunks text into smaller segments for processing.

```csharp
Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000)
```

**Parameters:**
- `text` (string): Text to chunk
- `maxChunkSize` (int): Maximum size of each chunk in characters (default: 1000)

**Returns:** List of text chunks


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

