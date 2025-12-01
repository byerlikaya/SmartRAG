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

```csharp
Task<string> GenerateTextAsync(string prompt, AIProviderConfig config);
Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config);
Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config);
Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000);
```


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

