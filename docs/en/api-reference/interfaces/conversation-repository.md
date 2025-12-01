---
layout: default
title: IConversationRepository
description: IConversationRepository interface documentation
lang: en
---
## IConversationRepository

**Purpose:** Data access layer for conversation storage

**Namespace:** `SmartRAG.Interfaces.Storage`

Separated from `IDocumentRepository` for better SRP compliance.

#### Methods

```csharp
Task<string> GetConversationHistoryAsync(string sessionId);
Task SaveConversationAsync(string sessionId, string history);
Task DeleteConversationAsync(string sessionId);
Task<bool> ConversationExistsAsync(string sessionId);
```

#### Implementations

- `SqliteConversationRepository`
- `InMemoryConversationRepository`
- `FileSystemConversationRepository`
- `RedisConversationRepository`


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

