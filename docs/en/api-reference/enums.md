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

