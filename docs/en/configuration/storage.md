---
layout: default
title: Storage Providers
description: SmartRAG storage provider configuration - Qdrant, Redis, SQLite, FileSystem and InMemory storage options
lang: en
---

## Storage Provider Configuration

SmartRAG supports various storage providers:

---

## Qdrant (Vector Database)

```json
{
  "Storage": {
    "Qdrant": {
      "Host": "localhost:6334",
      "UseHttps": false,
      "ApiKey": "qdrant-key",
      "CollectionName": "smartrag_documents"
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.Qdrant;
});
```

**Advantages:**
- 🚀 High-performance vector search
- 📈 Scalable (millions of vectors)
- 🔍 Advanced filtering and metadata support
- 🏢 Ideal for production

**Disadvantages:**
- 🐳 Requires Docker
- 💾 Additional resource usage
- 🔧 Complex setup

---

## Redis (High-Performance Cache)

```json
{
  "Storage": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Database": 0,
      "KeyPrefix": "smartrag:",
      "EnableVectorSearch": true,
      "DistanceMetric": "COSINE",
      "VectorDimension": 768
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.Redis;
});
```

**Advantages:**
- ⚡ Very fast access
- 🔄 Automatic expire support
- 📊 Rich data types
- 🔍 Vector similarity search with RediSearch
- 🏢 Suitable for production

**Disadvantages:**
- 💾 RAM-based (limited capacity)
- 🔧 Redis with RediSearch module required for vector search
- 💰 Additional cost

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> RediSearch Module Required</h4>
    <p class="mb-0"><strong>Vector search requires RediSearch module.</strong> Use <code>redis/redis-stack-server:latest</code> Docker image or install RediSearch module on your Redis server. Without RediSearch, only text search will work (no vector similarity search).</p>
    <p class="mb-0 mt-2"><strong>Docker example:</strong></p>
    <pre class="mt-2"><code>docker run -d -p 6379:6379 redis/redis-stack-server:latest</code></pre>
</div>

---

## SQLite (Embedded Database)

```json
{
  "Storage": {
    "SQLite": {
      "ConnectionString": "Data Source=./smartrag.db",
      "EnableWAL": true
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.SQLite;
});
```

**Advantages:**
- 📁 Single file database
- 🔒 Data privacy (local)
- 🚀 Quick setup
- 💰 No cost

**Disadvantages:**
- 📊 Limited concurrent access
- 🔄 Requires backup
- 📈 Scalability limitations

---

## FileSystem (File-Based Storage)

```json
{
  "Storage": {
    "FileSystem": {
      "BasePath": "./documents",
      "EnableCompression": true
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.FileSystem;
});
```

**Advantages:**
- 📁 Simple file system
- 🔍 Easy debug and inspection
- 💾 Unlimited capacity
- 🔒 Full control

**Disadvantages:**
- 🐌 Slow search performance
- 📊 Metadata limitations
- 🔄 Manual backup

---

## InMemory (RAM Storage)

```json
{
  "Storage": {
    "InMemory": {
      "MaxDocuments": 10000,
      "EnablePersistence": false
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.InMemory;
});
```

**Use Cases:**
- 🧪 Testing and development
- 🚀 Prototyping
- 📊 Temporary data
- 🔬 Proof of concept

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Important</h4>
    <p class="mb-0">InMemory storage loses all data when application restarts. Not suitable for production!</p>
</div>

---

## Storage Provider Comparison

| Provider | Performance | Scalability | Setup | Cost | Production Ready |
|----------|-------------|-------------|-------|------|------------------|
| **Qdrant** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ✅ Excellent |
| **Redis** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ✅ Good |
| **SQLite** | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⚠️ Limited |
| **FileSystem** | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ❌ Not suitable |
| **InMemory** | ⭐⭐⭐⭐⭐ | ⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ❌ Test only |

---

## Recommended Use Cases

### Development and Testing
```csharp
// For fast development
options.StorageProvider = StorageProvider.InMemory;
```

### Small-Scale Applications
```csharp
// Simple and reliable
options.StorageProvider = StorageProvider.SQLite;
```

### Medium-Scale Applications
```csharp
// Fast and scalable
options.StorageProvider = StorageProvider.Redis;
```

### Large-Scale Applications
```csharp
// Maximum performance and scalability
options.StorageProvider = StorageProvider.Qdrant;
```

---

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-server"></i>
            </div>
            <h3>Database Configuration</h3>
            <p>Multi-database connections and schema analysis</p>
            <a href="{{ site.baseurl }}/en/configuration/database" class="btn btn-outline-primary btn-sm mt-3">
                Database Configuration
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-microphone"></i>
            </div>
            <h3>Audio & OCR</h3>
            <p>Whisper.net and Tesseract OCR</p>
            <a href="{{ site.baseurl }}/en/configuration/audio-ocr" class="btn btn-outline-primary btn-sm mt-3">
                Audio & OCR
            </a>
        </div>
    </div>
</div>
