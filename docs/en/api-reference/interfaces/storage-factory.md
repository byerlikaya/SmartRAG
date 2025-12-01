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

##### CreateRepository (StorageConfig)

Creates repository using storage configuration.

```csharp
IDocumentRepository CreateRepository(StorageConfig config)
```

**Parameters:**
- `config` (StorageConfig): Storage configuration settings

**Returns:** Document repository instance

##### CreateRepository (StorageProvider)

Creates repository using storage provider type.

```csharp
IDocumentRepository CreateRepository(StorageProvider provider)
```

**Parameters:**
- `provider` (StorageProvider): Storage provider type

**Returns:** Document repository instance

##### GetCurrentProvider

Gets the currently active storage provider.

```csharp
StorageProvider GetCurrentProvider()
```

**Returns:** Currently active storage provider

##### GetCurrentRepository

Gets the currently active repository instance.

```csharp
IDocumentRepository GetCurrentRepository()
```

**Returns:** Currently active document repository instance

##### CreateConversationRepository

Creates conversation repository using conversation storage provider type.

```csharp
IConversationRepository CreateConversationRepository(ConversationStorageProvider provider)
```

**Parameters:**
- `provider` (ConversationStorageProvider): Conversation storage provider type

**Returns:** Conversation repository instance

##### GetCurrentConversationRepository

Gets the currently active conversation repository instance.

```csharp
IConversationRepository GetCurrentConversationRepository()
```

**Returns:** Currently active conversation repository instance


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

