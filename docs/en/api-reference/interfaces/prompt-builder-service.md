---
layout: default
title: IPromptBuilderService
description: IPromptBuilderService interface documentation
lang: en
---
## IPromptBuilderService

**Purpose:** Service for building AI prompts for different scenarios

**Namespace:** `SmartRAG.Interfaces.AI`

Centralized prompt construction with conversation history support.

#### Methods

```csharp
string BuildDocumentRagPrompt(string query, string context, string? conversationHistory = null);
string BuildHybridMergePrompt(string query, string? databaseContext, string? documentContext, string? conversationHistory = null);
string BuildConversationPrompt(string query, string? conversationHistory = null);
```


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

