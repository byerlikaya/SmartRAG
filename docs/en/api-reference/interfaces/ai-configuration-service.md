---
layout: default
title: IAIConfigurationService
description: IAIConfigurationService interface documentation
lang: en
---
## IAIConfigurationService

**Purpose:** AI provider configuration management

**Namespace:** `SmartRAG.Interfaces.AI`

Separated configuration from execution for better SRP.

#### Methods

```csharp
AIProvider GetProvider();
string GetModel();
string GetEmbeddingModel();
int GetMaxTokens();
double GetTemperature();
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

