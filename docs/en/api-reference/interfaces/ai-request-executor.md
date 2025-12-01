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

```csharp
Task<string> ExecuteRequestAsync(string prompt, CancellationToken cancellationToken = default);
Task<List<float>> ExecuteEmbeddingRequestAsync(string text, CancellationToken cancellationToken = default);
```


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

