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

##### BuildDocumentRagPrompt

Builds a prompt for document-based RAG answer generation.

```csharp
string BuildDocumentRagPrompt(
    string query, 
    string context, 
    string? conversationHistory = null, 
    string? preferredLanguage = null
)
```

**Parameters:**
- `query` (string): User query
- `context` (string): Document context
- `conversationHistory` (string?, optional): Optional conversation history
- `preferredLanguage` (string?, optional): Preferred language code (e.g., "tr", "en") for explicit AI response language

**Returns:** Built prompt string

##### BuildHybridMergePrompt

Builds a prompt for merging hybrid results (database + documents).

```csharp
string BuildHybridMergePrompt(
    string query, 
    string? databaseContext, 
    string? documentContext, 
    string? conversationHistory = null, 
    string? preferredLanguage = null
)
```

**Parameters:**
- `query` (string): User query
- `databaseContext` (string?, optional): Database context
- `documentContext` (string?, optional): Document context
- `conversationHistory` (string?, optional): Optional conversation history
- `preferredLanguage` (string?, optional): Preferred language code (e.g., "tr", "en") for explicit AI response language

**Returns:** Built prompt string

##### BuildConversationPrompt

Builds a prompt for general conversation.

```csharp
string BuildConversationPrompt(
    string query, 
    string? conversationHistory = null, 
    string? preferredLanguage = null
)
```

**Parameters:**
- `query` (string): User query
- `conversationHistory` (string?, optional): Optional conversation history
- `preferredLanguage` (string?, optional): Preferred language code (e.g., "tr", "en") for explicit AI response language

**Returns:** Built prompt string


## Related Interfaces

- [Service Interfaces]({{ site.baseurl }}/en/api-reference/services) - Browse all service interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

