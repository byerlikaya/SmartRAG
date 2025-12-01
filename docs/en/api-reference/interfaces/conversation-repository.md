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

##### GetConversationHistoryAsync

Gets conversation history for a session.

```csharp
Task<string> GetConversationHistoryAsync(string sessionId)
```

**Parameters:**
- `sessionId` (string): Session identifier

**Returns:** Conversation history as formatted string

##### AddToConversationAsync

Adds a conversation turn to the session.

```csharp
Task AddToConversationAsync(string sessionId, string question, string answer)
```

**Parameters:**
- `sessionId` (string): Session identifier
- `question` (string): User question
- `answer` (string): Assistant answer

##### ClearConversationAsync

Clears conversation history for a session.

```csharp
Task ClearConversationAsync(string sessionId)
```

**Parameters:**
- `sessionId` (string): Session identifier

##### SessionExistsAsync

Checks if a session exists.

```csharp
Task<bool> SessionExistsAsync(string sessionId)
```

**Parameters:**
- `sessionId` (string): Session identifier

**Returns:** True if session exists, false otherwise

#### Implementations

- `SqliteConversationRepository`
- `InMemoryConversationRepository`
- `FileSystemConversationRepository`
- `RedisConversationRepository`


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

