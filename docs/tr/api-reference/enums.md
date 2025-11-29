---
layout: default
title: Numaralandırmalar
description: SmartRAG numaralandırmaları - AIProvider, StorageProvider, DatabaseType, RetryPolicy ve diğer enum'lar
lang: tr
---

## Numaralandırmalar

### AIProvider

Desteklenen AI sağlayıcıları.

```csharp
public enum AIProvider
{
    OpenAI,        // OpenAI GPT modelleri
    Anthropic,     // Anthropic Claude modelleri
    Gemini,        // Google Gemini modelleri
    AzureOpenAI,   // Azure OpenAI servisi
    Custom         // Özel/Ollama/LM Studio/OpenRouter
}
```

### StorageProvider

Doküman ve vektör veri kalıcılığı için desteklenen depolama arka uçları.

```csharp
public enum StorageProvider
{
    InMemory,    // RAM depolama (kalıcı değil, test ve geliştirme için)
    Redis,       // Yüksek performanslı önbellek ve depolama
    Qdrant       // Gelişmiş vektör arama yetenekleri için vektör veritabanı
}
```

**Not:** `SQLite` ve `FileSystem`, `StorageProvider` seçenekleri olarak mevcut değildir. Bunlar yalnızca konuşma geçmişi depolama için `ConversationStorageProvider` seçenekleri olarak mevcuttur.

### DatabaseType

Desteklenen veritabanı tipleri.

```csharp
public enum DatabaseType
{
    SQLite,       // SQLite gömülü veritabanı
    SqlServer,    // Microsoft SQL Server
    MySQL,        // MySQL / MariaDB
    PostgreSQL    // PostgreSQL
}
```

### RetryPolicy

Başarısız istekler için yeniden deneme politikaları.

```csharp
public enum RetryPolicy
{
    None,                // Yeniden deneme yok
    FixedDelay,         // Yeniden denemeler arasında sabit gecikme
    LinearBackoff,      // Doğrusal artan gecikme
    ExponentialBackoff  // Üssel artan gecikme (önerilen)
}
```

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-database"></i>
            </div>
            <h3>Veri Modelleri</h3>
            <p>RagResponse, Document, DocumentChunk ve diğer veri yapıları</p>
            <a href="{{ site.baseurl }}/tr/api-reference/models" class="btn btn-outline-primary btn-sm mt-3">
                Veri Modelleri
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-home"></i>
            </div>
            <h3>API Referans</h3>
            <p>API Referans ana sayfasına dön</p>
            <a href="{{ site.baseurl }}/tr/api-reference" class="btn btn-outline-primary btn-sm mt-3">
                API Referans
            </a>
        </div>
    </div>
</div>

