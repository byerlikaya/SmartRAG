---
layout: default
title: Yapılandırma
description: SmartRAG için detaylı yapılandırma seçenekleri ve en iyi uygulamalar
lang: tr
---

# Yapılandırma

SmartRAG için detaylı yapılandırma seçenekleri ve en iyi uygulamalar.

## Temel Yapılandırma

SmartRAG, ihtiyaçlarınıza uygun çeşitli seçeneklerle yapılandırılabilir.

### Servis Kaydı

```csharp
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});
```

### Yapılandırma Seçenekleri

| Seçenek | Tip | Varsayılan | Açıklama |
|---------|-----|------------|----------|
| `AIProvider` | `AIProvider` | `Anthropic` | Embedding'ler için kullanılacak AI provider |
| `StorageProvider` | `StorageProvider` | `Qdrant` | Vektörler için depolama provider'ı |
| `ApiKey` | `string` | Gerekli | AI provider için API anahtarınız |
| `ModelName` | `string` | Provider varsayılanı | Kullanılacak spesifik model |
| `ChunkSize` | `int` | 1000 | Belge parçalarının boyutu |
| `ChunkOverlap` | `int` | 200 | Parçalar arasındaki örtüşme |

## AI Provider Yapılandırması

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

### Özel AI Provider

```csharp
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Custom;
    options.CustomEndpoint = "https://your-custom-api.com/v1/embeddings";
    options.ApiKey = "your-custom-key";
    options.ModelName = "your-custom-model";
});
```

## Depolama Provider Yapılandırması

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

### Bellek İçi

```csharp
services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.InMemory;
    // Bellek içi depolama için ek yapılandırma gerekmez
});
```

### Dosya Sistemi

```csharp
services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.FileSystem;
    options.StoragePath = "./data/smartrag";
});
```

## Gelişmiş Yapılandırma

### Özel Parçalama

```csharp
services.AddSmartRAG(options =>
{
    options.ChunkSize = 500;
    options.ChunkOverlap = 100;
    options.ChunkingStrategy = ChunkingStrategy.Sentence;
});
```

### Belge İşleme

```csharp
services.AddSmartRAG(options =>
{
    options.SupportedFormats = new[] { ".pdf", ".docx", ".txt" };
    options.MaxFileSize = 10 * 1024 * 1024; // 10MB
    options.EnableTextExtraction = true;
});
```

## Ortam Yapılandırması

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

### Ortam Değişkenleri

```bash
export SMARTRAG_AI_PROVIDER=Anthropic
export SMARTRAG_STORAGE_PROVIDER=Qdrant
export SMARTRAG_API_KEY=your-api-key
```

## En İyi Uygulamalar

1. **API Anahtarları**: API anahtarlarını kaynak kodda asla hardcode yapmayın
2. **Parça Boyutu**: Bağlam ve performans arasında denge kurun
3. **Depolama**: Ölçeğinize göre depolama provider'ı seçin
4. **İzleme**: Üretim ortamları için günlük kaydını etkinleştirin
5. **Güvenlik**: Depolama için uygun erişim kontrollerini kullanın

## Yardıma mı ihtiyacınız var?

Yapılandırma konusunda yardıma ihtiyacınız varsa:

- [Ana Dokümantasyona Dön]({{ site.baseurl }}/tr/) - Ana dokümantasyon
- [GitHub'da issue açın](https://github.com/byerlikaya/SmartRAG/issues) - GitHub Issues
- [Destek için iletişime geçin](mailto:b.yerlikaya@outlook.com) - E-posta desteği
