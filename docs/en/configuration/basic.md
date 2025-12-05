---
layout: default
title: Basic Configuration
description: SmartRAG basic configuration options - configuration methods, chunking and retry settings
lang: en
---

## Configuration Methods

<p>SmartRAG offers two configuration methods:</p>

### Quick Setup (Recommended)

<p>Configure SmartRAG in your <code>Program.cs</code> or <code>Startup.cs</code>:</p>

```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Simple one-line configuration
builder.Services.UseSmartRag(builder.Configuration,
    storageProvider: StorageProvider.InMemory,  // Start with in-memory
    aiProvider: AIProvider.Gemini,              // Choose your AI provider
    defaultLanguage: "tr"                        // Optional: Default language for document processing
);

var app = builder.Build();
app.Run();
```

### Advanced Setup

```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Advanced configuration with options
builder.Services.AddSmartRag(builder.Configuration, options =>
{
    // AI Provider
    options.AIProvider = AIProvider.OpenAI;
    
    // Storage Provider
    options.StorageProvider = StorageProvider.Qdrant;
    
    // Chunking Configuration
    options.MaxChunkSize = 1000;
    options.MinChunkSize = 100;
    options.ChunkOverlap = 200;
    
    // Retry Configuration
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    
    // Fallback Providers
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider> 
    { 
        AIProvider.Anthropic, 
        AIProvider.Gemini 
    };
    
    // Default Language
    options.DefaultLanguage = "tr";  // Optional: Default language for document processing
});

var app = builder.Build();
app.Run();
```

## SmartRagOptions - Core Options

<p>Core configuration options available in SmartRagOptions:</p>

<div class="table-responsive">
    <table class="table">
        <thead>
            <tr>
                <th>Option</th>
                <th>Type</th>
                <th>Default</th>
                <th>Description</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><code>AIProvider</code></td>
                <td><code>AIProvider</code></td>
                <td><code>OpenAI</code></td>
                <td>AI provider for embeddings and text generation</td>
            </tr>
            <tr>
                <td><code>StorageProvider</code></td>
                <td><code>StorageProvider</code></td>
                <td><code>InMemory</code></td>
                <td>Storage backend for documents and vectors</td>
            </tr>
            <tr>
                <td><code>ConversationStorageProvider</code></td>
                <td><code>ConversationStorageProvider?</code></td>
                <td><code>null</code></td>
                <td>Separate storage for conversation history (optional)</td>
            </tr>
            <tr>
                <td><code>EnableAutoSchemaAnalysis</code></td>
                <td><code>bool</code></td>
                <td><code>true</code></td>
                <td>Automatically analyze database schemas on startup</td>
            </tr>
            <tr>
                <td><code>EnablePeriodicSchemaRefresh</code></td>
                <td><code>bool</code></td>
                <td><code>true</code></td>
                <td>Periodically refresh database schemas</td>
            </tr>
            <tr>
                <td><code>DefaultSchemaRefreshIntervalMinutes</code></td>
                <td><code>int</code></td>
                <td><code>60</code></td>
                <td>Default interval in minutes for schema refresh</td>
            </tr>
            <tr>
                <td><code>DefaultLanguage</code></td>
                <td><code>string?</code></td>
                <td><code>null</code></td>
                <td>Default language code for document processing (ISO 639-1 format, e.g., "tr", "en", "de"). Used when language is not specified in WatchedFolderConfig or document upload.</td>
            </tr>
        </tbody>
    </table>
</div>

## ConversationStorageProvider

<p>Separate storage configuration for conversation history, independent from document storage:</p>

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;  // For documents
    options.ConversationStorageProvider = ConversationStorageProvider.Redis;  // For conversations
});
```

### Available Options

| Option | Description |
|--------|-------------|
| `Redis` | Store conversations in Redis (high-performance cache) |
| `SQLite` | Store conversations in SQLite database (embedded, lightweight) |
| `FileSystem` | Store conversations in file system (simple, persistent) |
| `InMemory` | Store conversations in RAM (not persistent, development only) |

### Configuration Example

```json
{
  "SmartRAG": {
    "ConversationStorageProvider": "Redis",
    "RedisConfig": {
      "ConnectionString": "localhost:6379"
    }
  }
}
```

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Conversation Storage Tips</h4>
    <ul class="mb-0">
        <li><strong>Redis:</strong> Best for production, high-performance caching</li>
        <li><strong>SQLite:</strong> Good for development and small deployments</li>
        <li><strong>FileSystem:</strong> Simple, human-readable storage</li>
        <li><strong>InMemory:</strong> Fast, but data lost on restart</li>
    </ul>
</div>

## Chunking Options

<div class="table-responsive">
    <table class="table">
        <thead>
            <tr>
                <th>Option</th>
                <th>Type</th>
                <th>Default</th>
                <th>Description</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><code>MaxChunkSize</code></td>
                <td><code>int</code></td>
                <td><code>1000</code></td>
                <td>Maximum size of each document chunk in characters</td>
            </tr>
            <tr>
                <td><code>MinChunkSize</code></td>
                <td><code>int</code></td>
                <td><code>100</code></td>
                <td>Minimum size of each document chunk in characters</td>
            </tr>
            <tr>
                <td><code>ChunkOverlap</code></td>
                <td><code>int</code></td>
                <td><code>200</code></td>
                <td>Number of characters to overlap between adjacent chunks</td>
            </tr>
        </tbody>
    </table>
</div>

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Chunking Best Practices</h4>
    <ul class="mb-0">
        <li><strong>MaxChunkSize:</strong> 500-1000 characters for optimal balance</li>
        <li><strong>ChunkOverlap:</strong> 15-20% of MaxChunkSize for context preservation</li>
        <li><strong>Larger chunks:</strong> Better context, but slower search</li>
        <li><strong>Smaller chunks:</strong> More precise results, but less context</li>
    </ul>
</div>

## Retry & Resilience Options

<div class="table-responsive">
    <table class="table">
        <thead>
            <tr>
                <th>Option</th>
                <th>Type</th>
                <th>Default</th>
                <th>Description</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><code>MaxRetryAttempts</code></td>
                <td><code>int</code></td>
                <td><code>3</code></td>
                <td>Maximum number of retry attempts for AI provider requests</td>
            </tr>
            <tr>
                <td><code>RetryDelayMs</code></td>
                <td><code>int</code></td>
                <td><code>1000</code></td>
                <td>Delay between retry attempts in milliseconds</td>
            </tr>
            <tr>
                <td><code>RetryPolicy</code></td>
                <td><code>RetryPolicy</code></td>
                <td><code>ExponentialBackoff</code></td>
                <td>Retry policy for failed requests</td>
            </tr>
            <tr>
                <td><code>EnableFallbackProviders</code></td>
                <td><code>bool</code></td>
                <td><code>false</code></td>
                <td>Enable fallback to alternative AI providers on failure</td>
            </tr>
            <tr>
                <td><code>FallbackProviders</code></td>
                <td><code>List&lt;AIProvider&gt;</code></td>
                <td><code>[]</code></td>
                <td>List of fallback AI providers to try sequentially</td>
            </tr>
        </tbody>
    </table>
</div>

**RetryPolicy Enum Values:**
- `RetryPolicy.None` - No retries
- `RetryPolicy.FixedDelay` - Fixed delay between retries
- `RetryPolicy.LinearBackoff` - Linearly increasing delay
- `RetryPolicy.ExponentialBackoff` - Exponentially increasing delay (recommended)

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

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
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
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
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
