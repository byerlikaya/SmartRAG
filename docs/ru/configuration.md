---
layout: default
title: Configuration
description: Detailed configuration options and best practices for SmartRAG
lang: en
---

# Configuration

Detailed configuration options and best practices for SmartRAG.

## Basic Configuration

SmartRAG can be configured with various options to suit your needs.

### Service Registration

```csharp
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `AIProvider` | `AIProvider` | `Anthropic` | The AI provider to use for embeddings |
| `StorageProvider` | `StorageProvider` | `Qdrant` | The storage provider for vectors |
| `ApiKey` | `string` | Required | Your API key for the AI provider |
| `ModelName` | `string` | Provider default | The specific model to use |
| `ChunkSize` | `int` | 1000 | Size of document chunks |
| `ChunkOverlap` | `int` | 200 | Overlap between chunks |

## AI Provider Configuration

### Anthropic

```csharp
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.ApiKey = "your-anthropic-key";
    options.ModelName = "claude-3-sonnet-20240229";
});
```

### OpenAI

```csharp
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.ApiKey = "your-openai-key";
    options.ModelName = "text-embedding-ada-002";
});
```

### Azure OpenAI

```csharp
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.AzureOpenAI;
    options.ApiKey = "your-azure-key";
    options.Endpoint = "https://your-resource.openai.azure.com/";
    options.ModelName = "text-embedding-ada-002";
});
```

### Gemini

```csharp
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Gemini;
    options.ApiKey = "your-gemini-key";
    options.ModelName = "embedding-001";
});
```

### Пользовательский ИИ-провайдер

```csharp
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Custom;
    options.CustomEndpoint = "https://your-custom-api.com/v1/embeddings";
    options.ApiKey = "your-custom-key";
    options.ModelName = "your-custom-model";
});
```

## Storage Provider Configuration

### Qdrant

```csharp
services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Qdrant;
    options.QdrantUrl = "http://localhost:6333";
    options.CollectionName = "smartrag_documents";
});
```

### Redis

```csharp
services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Redis;
    options.RedisConnectionString = "localhost:6379";
    options.DatabaseId = 0;
});
```

### SQLite

```csharp
services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Sqlite;
    options.ConnectionString = "Data Source=smartrag.db";
});
```

### In-Memory

```csharp
services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.InMemory;
    // Дополнительная конфигурация для хранения в памяти не требуется
});
```

### Файловая система

```csharp
services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.FileSystem;
    options.StoragePath = "./data/smartrag";
});
```

## Advanced Configuration

### Custom Chunking

```csharp
services.AddSmartRAG(options =>
{
    options.ChunkSize = 500;
    options.ChunkOverlap = 100;
    options.ChunkingStrategy = ChunkingStrategy.Sentence;
});
```

### Document Processing

```csharp
services.AddSmartRAG(options =>
{
    options.SupportedFormats = new[] { ".pdf", ".docx", ".txt" };
    options.MaxFileSize = 10 * 1024 * 1024; // 10MB
    options.EnableTextExtraction = true;
});
```

## Environment Configuration

### appsettings.json

```json
{
  "SmartRAG": {
    "AIProvider": "Anthropic",
    "StorageProvider": "Qdrant",
    "ApiKey": "your-api-key",
    "ChunkSize": 1000,
    "ChunkOverlap": 200
  }
}
```

### Environment Variables

```bash
export SMARTRAG_AI_PROVIDER=Anthropic
export SMARTRAG_STORAGE_PROVIDER=Qdrant
export SMARTRAG_API_KEY=your-api-key
```

## Best Practices

1. **API Keys**: Never hardcode API keys in source code
2. **Chunk Size**: Balance between context and performance
3. **Storage**: Choose storage provider based on your scale
4. **Monitoring**: Enable logging for production environments
5. **Security**: Use appropriate access controls for storage

## Need Help?

If you need assistance with configuration:

- [Back to Documentation]({{ site.baseurl }}/en/) - Main documentation
- [Open an issue](https://github.com/byerlikaya/SmartRAG/issues) - GitHub Issues
- [Contact support](mailto:b.yerlikaya@outlook.com) - Email support
