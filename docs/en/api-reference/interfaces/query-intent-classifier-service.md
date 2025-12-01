---
layout: default
title: IQueryIntentClassifierService
description: IQueryIntentClassifierService interface documentation
lang: en
---
## IQueryIntentClassifierService

**Purpose:** Service for classifying query intent (conversation vs information)

**Namespace:** `SmartRAG.Interfaces.Support`

AI-based query intent classification for hybrid routing.

#### Methods

```csharp
Task<bool> IsGeneralConversationAsync(string query, string? conversationHistory = null);
bool TryParseCommand(string input, out QueryCommandType commandType, out string payload);
```

**Command Types:**
- `QueryCommandType.None`: No command detected
- `QueryCommandType.NewConversation`: `/new` or `/reset` command
- `QueryCommandType.ForceConversation`: `/conv` command


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

