---
layout: default
title: Basic Configuration
description: SmartRAG basic configuration options - configuration methods, chunking and retry settings
lang: en
---

## Basic Configuration

SmartRAG offers two configuration methods:

### Method 1: UseSmartRag (Simple)

```csharp
builder.Services.UseSmartRag(configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.Gemini
);
```

### Method 2: AddSmartRag (Advanced)

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;
    // ... additional options
});
```

---

## SmartRagOptions - Core Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `AIProvider` | `AIProvider` | `OpenAI` | AI provider for embeddings and text generation |
| `StorageProvider` | `StorageProvider` | `InMemory` | Storage backend for documents and vectors |
| `ConversationStorageProvider` | `ConversationStorageProvider?` | `null` | Separate storage for conversation history (optional) |

---

## Chunking Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `MaxChunkSize` | `int` | `1000` | Maximum size of each document chunk in characters |
| `MinChunkSize` | `int` | `100` | Minimum size of each document chunk in characters |
| `ChunkOverlap` | `int` | `200` | Number of characters to overlap between adjacent chunks |

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Chunking Best Practices</h4>
    <ul class="mb-0">
        <li><strong>MaxChunkSize:</strong> 500-1000 characters for optimal balance</li>
        <li><strong>ChunkOverlap:</strong> 15-20% of MaxChunkSize for context preservation</li>
        <li><strong>Larger chunks:</strong> Better context, but slower search</li>
        <li><strong>Smaller chunks:</strong> More precise results, but less context</li>
    </ul>
</div>

---

## Retry & Resilience Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `MaxRetryAttempts` | `int` | `3` | Maximum number of retry attempts for AI provider requests |
| `RetryDelayMs` | `int` | `1000` | Delay between retry attempts in milliseconds |
| `RetryPolicy` | `RetryPolicy` | `ExponentialBackoff` | Retry policy for failed requests |
| `EnableFallbackProviders` | `bool` | `false` | Enable fallback to alternative AI providers on failure |
| `FallbackProviders` | `List<AIProvider>` | `[]` | List of fallback AI providers to try sequentially |

**RetryPolicy Enum Values:**
- `RetryPolicy.None` - No retries
- `RetryPolicy.FixedDelay` - Fixed delay between retries
- `RetryPolicy.LinearBackoff` - Linearly increasing delay
- `RetryPolicy.ExponentialBackoff` - Exponentially increasing delay (recommended)

---

## Example Configurations

### Development Environment

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // For fast development
    options.AIProvider = AIProvider.Gemini;
    options.StorageProvider = StorageProvider.InMemory;
    options.MaxChunkSize = 500;
    options.ChunkOverlap = 100;
});
```

### Production Environment

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // For reliable production
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
    options.MaxRetryAttempts = 5;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider> { AIProvider.Anthropic };
});
```

---

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-brain"></i>
            </div>
            <h3>AI Providers</h3>
            <p>OpenAI, Anthropic, Google Gemini and custom providers</p>
            <a href="{{ site.baseurl }}/en/configuration/ai-providers" class="btn btn-outline-primary btn-sm mt-3">
                AI Providers
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>Storage Providers</h3>
            <p>Qdrant, Redis, SQLite and other storage options</p>
            <a href="{{ site.baseurl }}/en/configuration/storage" class="btn btn-outline-primary btn-sm mt-3">
                Storage Providers
            </a>
        </div>
    </div>
</div>
