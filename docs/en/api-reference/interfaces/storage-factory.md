---
layout: default
title: IStorageFactory
description: IStorageFactory interface documentation
lang: en
---
## IStorageFactory

**Purpose:** Factory for creating document and conversation storage repositories

**Namespace:** `SmartRAG.Interfaces.Storage`

Unified factory for all storage operations.

#### Methods

```csharp
IDocumentRepository CreateRepository(StorageConfig config);
IDocumentRepository CreateRepository(StorageProvider provider);
StorageProvider GetCurrentProvider();
IDocumentRepository GetCurrentRepository();
IConversationRepository CreateConversationRepository(StorageConfig config);
IConversationRepository CreateConversationRepository(StorageProvider provider);
IConversationRepository GetCurrentConversationRepository();
```


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

