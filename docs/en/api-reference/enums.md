---
layout: default
title: Enumerations
description: SmartRAG enumerations - AIProvider, StorageProvider, DatabaseType, RetryPolicy and other enums
lang: en
---

## Enumerations

### AIProvider

Supported AI providers.

```csharp
public enum AIProvider
{
    OpenAI,        // OpenAI GPT models
    Anthropic,     // Anthropic Claude models
    Gemini,        // Google Gemini models
    AzureOpenAI,   // Azure OpenAI service
    Custom         // Custom/Ollama/LM Studio/OpenRouter
}
```

### StorageProvider

Supported storage backends for document and vector data persistence.

```csharp
public enum StorageProvider
{
    InMemory,    // RAM storage (non-persistent, for testing and development)
    Redis,       // High-performance cache and storage
    Qdrant       // Vector database for advanced vector search capabilities
}
```

**Note:** `SQLite` and `FileSystem` are not available as `StorageProvider` options. They are only available as `ConversationStorageProvider` options for conversation history storage.

### ConversationStorageProvider

Available storage providers for conversation history.

```csharp
public enum ConversationStorageProvider
{
    Redis,       // Store conversations in Redis
    SQLite,      // Store conversations in SQLite database
    FileSystem,  // Store conversations in file system
    InMemory     // Store conversations in memory (not persistent)
}
```

**Note:** If `ConversationStorageProvider` is not specified in `SmartRagOptions`, the system uses the same provider as `StorageProvider` (excluding Qdrant, which doesn't support conversation storage).

### DatabaseType

Supported database types.

```csharp
public enum DatabaseType
{
    SQLite,       // SQLite embedded database
    SqlServer,    // Microsoft SQL Server
    MySQL,        // MySQL / MariaDB
    PostgreSQL    // PostgreSQL
}
```

### RetryPolicy

Retry policies for failed requests.

```csharp
public enum RetryPolicy
{
    None,                // No retries
    FixedDelay,         // Fixed delay between retries
    LinearBackoff,      // Linearly increasing delay
    ExponentialBackoff  // Exponentially increasing delay (recommended)
}
```

### AudioProvider

Supported audio transcription providers.

```csharp
public enum AudioProvider
{
    Whisper     // Whisper.net (Local transcription - only supported provider)
}
```

**Note:** Currently, only Whisper.net is supported for local audio transcription. Google Speech-to-Text configuration exists but is not yet implemented.

### QueryStrategy

Strategy for query execution.

```csharp
public enum QueryStrategy
{
    DatabaseOnly,    // Execute database query only
    DocumentOnly,    // Execute document query only
    Hybrid           // Execute both database and document queries
}
```

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-database"></i>
            </div>
            <h3>Data Models</h3>
            <p>RagResponse, Document, DocumentChunk and other data structures</p>
            <a href="{{ site.baseurl }}/en/api-reference/models" class="btn btn-outline-primary btn-sm mt-3">
                Data Models
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-home"></i>
            </div>
            <h3>API Reference</h3>
            <p>Back to API Reference index</p>
            <a href="{{ site.baseurl }}/en/api-reference" class="btn btn-outline-primary btn-sm mt-3">
                API Reference
            </a>
        </div>
    </div>
</div>
