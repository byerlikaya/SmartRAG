---
layout: default
title: IConversationManagerService
description: IConversationManagerService interface documentation
lang: en
---
## IConversationManagerService

**Purpose:** Conversation session management and history tracking

**Namespace:** `SmartRAG.Interfaces.Support`

This interface provides dedicated conversation management, separated from document operations for better separation of concerns.

### Methods

#### StartNewConversationAsync

Start a new conversation session.

```csharp
Task<string> StartNewConversationAsync()
```

**Returns:** New session ID (string)

**Example:**

```csharp
var sessionId = await _conversationManager.StartNewConversationAsync();
Console.WriteLine($"Started session: {sessionId}");
```

#### GetOrCreateSessionIdAsync

Get existing session ID or create a new one automatically.

```csharp
Task<string> GetOrCreateSessionIdAsync()
```

**Returns:** Session ID (string)

**Use Case:** Automatic session continuity without manual session management

**Example:**

```csharp
// Automatically manages session - creates new if none exists
var sessionId = await _conversationManager.GetOrCreateSessionIdAsync();
```

#### AddToConversationAsync

Add a conversation turn (question + answer) to the session history.

```csharp
Task AddToConversationAsync(
    string sessionId, 
    string question, 
    string answer
)
```

**Parameters:**
- `sessionId` (string): Session identifier
- `question` (string): User's question
- `answer` (string): AI's answer

**Example:**

```csharp
await _conversationManager.AddToConversationAsync(
    sessionId,
    "What is machine learning?",
    "Machine learning is a subset of AI that enables systems to learn..."
);
```

#### GetConversationHistoryAsync

Retrieve full conversation history for a session.

```csharp
Task<string> GetConversationHistoryAsync(string sessionId)
```

**Parameters:**
- `sessionId` (string): Session identifier

**Returns:** Formatted conversation history as string

**Format:**
```
User: [question]
Assistant: [answer]
User: [next question]
Assistant: [next answer]
```

**Example:**

```csharp
var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
Console.WriteLine(history);
```

#### TruncateConversationHistory

Truncate conversation history to keep only recent turns (memory management).

```csharp
string TruncateConversationHistory(
    string history, 
    int maxTurns = 3
)
```

**Parameters:**
- `history` (string): Full conversation history
- `maxTurns` (int): Maximum number of conversation turns to keep (default: 3)

**Returns:** Truncated conversation history

**Use Case:** Prevent context window overflow in AI prompts

**Example:**

```csharp
var fullHistory = await _conversationManager.GetConversationHistoryAsync(sessionId);
var recentHistory = _conversationManager.TruncateConversationHistory(fullHistory, maxTurns: 5);
```

### Complete Usage Example

```csharp
public class ChatService
{
    private readonly IConversationManagerService _conversationManager;
    private readonly IDocumentSearchService _searchService;
    
    public ChatService(
        IConversationManagerService conversationManager,
        IDocumentSearchService searchService)
    {
        _conversationManager = conversationManager;
        _searchService = searchService;
    }
    
    public async Task<string> HandleChatAsync(string userMessage)
    {
        // Get or create session
        var sessionId = await _conversationManager.GetOrCreateSessionIdAsync();
        
        // Get conversation history for context
        var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
        
        // Query with context
        var response = await _searchService.QueryIntelligenceAsync(userMessage);
        
        // Save to conversation history
        await _conversationManager.AddToConversationAsync(
            sessionId, 
            userMessage, 
            response.Answer
        );
        
        return response.Answer;
    }
    
    public async Task<string> StartNewChatAsync()
    {
        var newSessionId = await _conversationManager.StartNewConversationAsync();
        return $"Started new conversation: {newSessionId}";
    }
}
```

### Storage Backends

Conversation history is stored using the configured `IConversationRepository`:
- **SQLite**: `SqliteConversationRepository` - Persistent file-based storage
- **InMemory**: `InMemoryConversationRepository` - Fast, non-persistent (development)
- **FileSystem**: `FileSystemConversationRepository` - JSON file-based storage
- **Redis**: `RedisConversationRepository` - High-performance distributed storage

Storage backend is automatically selected based on your `StorageProvider` configuration.


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

