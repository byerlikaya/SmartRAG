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

##### GetAIProviderConfig

Gets AI provider configuration for the currently configured provider.

```csharp
AIProviderConfig? GetAIProviderConfig()
```

**Returns:** AI provider configuration or null if not configured

##### GetProviderConfig

Gets AI provider configuration for a specific provider.

```csharp
AIProviderConfig? GetProviderConfig(AIProvider provider)
```

**Parameters:**
- `provider` (AIProvider): AI provider to get configuration for

**Returns:** AI provider configuration or null if not found


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

