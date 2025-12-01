---
layout: default
title: Storage Providers
description: SmartRAG storage provider configuration - Qdrant, Redis and InMemory storage options
lang: en
---

## Storage Provider Configuration

SmartRAG supports various storage providers:

## Qdrant (Vector Database)

<p>Qdrant is a high-performance vector database designed for production use with millions of vectors:</p>

```json
{
  "Storage": {
    "Qdrant": {
      "Host": "localhost",
      "UseHttps": false,
      "ApiKey": "",
      "CollectionName": "smartrag_documents",
      "VectorSize": 768,
      "DistanceMetric": "Cosine"
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
- 🐳 Requires Docker or cloud service
- 💾 Additional resource usage
- 🔧 Setup complexity

## Redis (High-Performance Cache)

<p>Redis provides fast in-memory storage with vector similarity search capabilities using RediSearch:</p>

```json
{
  "Storage": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Password": "",
      "Username": "",
      "Database": 0,
      "KeyPrefix": "smartrag:local:",
      "ConnectionTimeout": 30,
      "EnableSsl": false,
      "RetryCount": 3,
      "RetryDelay": 1000,
      "EnableVectorSearch": true,
      "VectorIndexAlgorithm": "HNSW",
      "DistanceMetric": "COSINE",
      "VectorDimension": 768,
      "VectorIndexName": "smartrag_vector_idx"
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

## InMemory (RAM Storage)

<p>InMemory storage is ideal for testing and development, storing all data in RAM:</p>

```json
{
  "Storage": {
    "InMemory": {
      "MaxDocuments": 1000
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

## Storage Provider Comparison

<p>Compare storage providers to choose the best option for your use case:</p>

<div class="table-responsive">
<table class="table">
<thead>
<tr>
<th>Provider</th>
<th>Performance</th>
<th>Scalability</th>
<th>Setup</th>
<th>Cost</th>
<th>Production Ready</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Qdrant</strong></td>
<td>⭐⭐⭐⭐⭐</td>
<td>⭐⭐⭐⭐⭐</td>
<td>⭐⭐⭐</td>
<td>⭐⭐⭐</td>
<td><span class="badge bg-success">Excellent</span></td>
</tr>
<tr>
<td><strong>Redis</strong></td>
<td>⭐⭐⭐⭐⭐</td>
<td>⭐⭐⭐</td>
<td>⭐⭐⭐</td>
<td>⭐⭐⭐</td>
<td><span class="badge bg-success">Good</span></td>
</tr>
<tr>
<td><strong>InMemory</strong></td>
<td>⭐⭐⭐⭐⭐</td>
<td>⭐</td>
<td>⭐⭐⭐⭐⭐</td>
<td>⭐⭐⭐⭐⭐</td>
<td><span class="badge bg-secondary">Test only</span></td>
</tr>
</tbody>
</table>
</div>

## Recommended Use Cases

### Development and Testing
```csharp
// For fast development and testing
options.StorageProvider = StorageProvider.InMemory;
```

### Medium-Scale Applications
```csharp
// Fast and scalable with RediSearch
options.StorageProvider = StorageProvider.Redis;
```

### Large-Scale Production Applications
```csharp
// Maximum performance and scalability for millions of vectors
options.StorageProvider = StorageProvider.Qdrant;
```

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
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
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
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
