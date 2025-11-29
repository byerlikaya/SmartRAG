---
layout: default
title: Gelişmiş Yapılandırma
description: SmartRAG gelişmiş yapılandırma seçenekleri - yedek sağlayıcılar, en iyi pratikler ve sonraki adımlar
lang: tr
---

## Gelişmiş Yapılandırma

<p>SmartRAG'in gelişmiş özelliklerini kullanarak daha güvenilir ve performanslı sistemler oluşturun:</p>

## Yedek Sağlayıcılar

### Fallback Provider Yapılandırması

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // Ana AI sağlayıcı
    options.AIProvider = AIProvider.OpenAI;
    
    // Yedek sağlayıcıları etkinleştir
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider>
    {
        AIProvider.Anthropic,    // İlk yedek
        AIProvider.Gemini,       // İkinci yedek
        AIProvider.Custom        // Son yedek (Ollama)
    };
    
    // Yeniden deneme yapılandırması
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
});
```

### Fallback Senaryoları

```csharp
// Senaryo 1: OpenAI → Anthropic → Gemini
options.FallbackProviders = new List<AIProvider>
{
    AIProvider.Anthropic,
    AIProvider.Gemini
};

// Senaryo 2: Azure OpenAI → OpenAI → Anthropic
options.FallbackProviders = new List<AIProvider>
{
    AIProvider.OpenAI,
    AIProvider.Anthropic
};

// Senaryo 3: Cloud → On-premise
options.FallbackProviders = new List<AIProvider>
{
    AIProvider.Custom  // Ollama/LM Studio
};

// Senaryo 4: Premium → Budget
options.FallbackProviders = new List<AIProvider>
{
    AIProvider.Gemini  // Daha uygun maliyetli
};
```

## Yeniden Deneme Politikaları

### RetryPolicy Seçenekleri

```csharp
// Senaryo 1: Hızlı yeniden deneme
options.RetryPolicy = RetryPolicy.FixedDelay;
options.RetryDelayMs = 500;  // 500ms sabit bekleme

// Senaryo 2: Doğrusal artan bekleme
options.RetryPolicy = RetryPolicy.LinearBackoff;
options.RetryDelayMs = 1000;  // 1s, 2s, 3s, 4s...

// Senaryo 3: Üssel artan bekleme (önerilen)
options.RetryPolicy = RetryPolicy.ExponentialBackoff;
options.RetryDelayMs = 1000;  // 1s, 2s, 4s, 8s...

// Senaryo 4: Yeniden deneme yok
options.RetryPolicy = RetryPolicy.None;
```

### Özel Yeniden Deneme Mantığı

```csharp
// Kritik uygulamalar için agresif yeniden deneme
options.MaxRetryAttempts = 5;
options.RetryDelayMs = 200;
options.RetryPolicy = RetryPolicy.ExponentialBackoff;

// Test ortamları için minimal yeniden deneme
options.MaxRetryAttempts = 1;
options.RetryDelayMs = 1000;
options.RetryPolicy = RetryPolicy.FixedDelay;
```

## Performans Optimizasyonu

### Chunk Boyutu Optimizasyonu

```csharp
// Senaryo 1: Hızlı arama (küçük parçalar)
options.MaxChunkSize = 500;
options.MinChunkSize = 100;
options.ChunkOverlap = 75;

// Senaryo 2: Bağlam koruması (büyük parçalar)
options.MaxChunkSize = 2000;
options.MinChunkSize = 200;
options.ChunkOverlap = 400;

// Senaryo 3: Denge (önerilen)
options.MaxChunkSize = 1000;
options.MinChunkSize = 100;
options.ChunkOverlap = 200;
```

### Depolama Optimizasyonu

```csharp
// Senaryo 1: Maksimum performans
options.StorageProvider = StorageProvider.Qdrant;
options.ConversationStorageProvider = ConversationStorageProvider.Redis;

// Senaryo 2: Maliyet optimizasyonu
options.StorageProvider = StorageProvider.Redis;
options.ConversationStorageProvider = ConversationStorageProvider.InMemory;

// Senaryo 3: Hibrit yaklaşım
options.StorageProvider = StorageProvider.Redis;
options.ConversationStorageProvider = ConversationStorageProvider.SQLite;
```

## Güvenlik Yapılandırması

### API Anahtarı Yönetimi

```csharp
// Environment variables kullanımı (önerilen)
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    // API anahtarı otomatik olarak environment'dan yüklenir
});

// appsettings.json kullanımı (geliştirme için)
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-ANAHTARINIZ"
    }
  }
}
```

### Veritabanı Bağlantı Yapılandırması

```csharp
// Veritabanı bağlantı yapılandırması
options.DatabaseConnections = new List<DatabaseConnectionConfig>
{
    new DatabaseConnectionConfig
    {
        Name = "Güvenli Veritabanı",
        DatabaseType = DatabaseType.SqlServer,
        ConnectionString = "Server=localhost;Database=SecureDB;...",
        Description = "Hassas veri içeren üretim veritabanı",
        Enabled = true,
        MaxRowsPerQuery = 1000,
        QueryTimeoutSeconds = 30,
        SchemaRefreshIntervalMinutes = 60,
        IncludedTables = new string[] { "Orders", "Customers" },
        ExcludedTables = new string[] { "Logs", "TempData" }
    }
};
```

**Not:** `SanitizeSensitiveData` ve `MaxRowsPerTable` ayarları `DatabaseConfig` içinde yapılandırılır, `DatabaseConnectionConfig` içinde değil. Bu ayarlar tüm veritabanı bağlantılarına global olarak uygulanır.

## Monitoring ve Logging

### Detaylı Logging

```csharp
// Logging yapılandırması
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

// SmartRAG için özel logging
builder.Services.AddSmartRag(configuration, options =>
{
    // Logging otomatik olarak etkinleştirilir
});
```

### Performance Monitoring

```csharp
// Performans metrikleri için
public class SmartRagMetrics
{
    public int TotalQueries { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int FallbackUsageCount { get; set; }
    public int RetryCount { get; set; }
}
```

## En İyi Pratikler

### Güvenlik

<div class="alert alert-success">
    <h4><i class="fas fa-check-circle me-2"></i> Güvenlik En İyi Pratikleri</h4>
    <ul class="mb-0">
        <li>API anahtarlarını asla kaynak kontrolüne commit etmeyin</li>
        <li>Üretim için environment variables kullanın</li>
        <li>Veritabanı bağlantılarını güvenli şekilde yapılandırın</li>
        <li>Dış servisler için HTTPS kullanın</li>
        <li>Hassas veriler için on-premise AI sağlayıcıları tercih edin</li>
    </ul>
</div>

### Performans

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Performans En İyi Pratikleri</h4>
    <ul class="mb-0">
        <li>Üretim için Qdrant veya Redis kullanın</li>
        <li>Uygun chunk boyutları yapılandırın</li>
        <li>Güvenilirlik için yedek sağlayıcıları etkinleştirin</li>
        <li>Veritabanı bağlantıları için makul MaxRowsPerQuery limitleri ayarlayın</li>
        <li>ExponentialBackoff retry policy kullanın</li>
    </ul>
</div>

### Maliyet Optimizasyonu

<div class="alert alert-warning">
    <h4><i class="fas fa-dollar-sign me-2"></i> Maliyet Optimizasyonu</h4>
    <ul class="mb-0">
        <li>Geliştirme için Gemini veya Custom sağlayıcıları kullanın</li>
        <li>Üretim için OpenAI, Azure OpenAI veya Anthropic tercih edin</li>
        <li>InMemory depolama sadece test için kullanın</li>
        <li>Maliyet etkin üretim depolaması için Redis kullanın</li>
        <li>Ollama/LM Studio ile %100 on-premise çözümler</li>
    </ul>
</div>

## Özel Stratejiler

SmartRAG, temel bileşenler için Strateji Deseni'ni (Strategy Pattern) sunarak özel mantık enjekte etmenize olanak tanır.

### Özel Stratejilerin Kaydı

İlgili arayüzleri (interface) uygulayarak kendi stratejilerinizi geliştirebilirsiniz. İşte bunları nasıl kaydedebileceğinize dair örnekler:

```csharp
// Örnek: Özel bir SQL Diyalekti kaydı (örn. EnhancedPostgreSqlDialectStrategy geliştirdiyseniz)
services.AddSingleton<ISqlDialectStrategy, EnhancedPostgreSqlDialectStrategy>();

// Örnek: Özel bir Skorlama Stratejisi kaydı
services.AddSingleton<IScoringStrategy, CustomScoringStrategy>();

// Örnek: Özel bir Dosya Ayrıştırıcı kaydı (örn. Markdown dosyaları için)
services.AddSingleton<IFileParser, MarkdownFileParser>();
```

## Örnek Yapılandırmalar

### Geliştirme Ortamı

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // Hızlı geliştirme için
    options.AIProvider = AIProvider.Gemini;
    options.StorageProvider = StorageProvider.InMemory;
    options.MaxChunkSize = 500;
    options.ChunkOverlap = 100;
    options.MaxRetryAttempts = 1;
    options.RetryPolicy = RetryPolicy.FixedDelay;
});
```

### Test Ortamı

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // Test için güvenilir yapılandırma
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Redis;
    options.ConversationStorageProvider = ConversationStorageProvider.SQLite;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
    options.MaxRetryAttempts = 3;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider> { AIProvider.Gemini };
});
```

### Üretim Ortamı

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // Güvenilir üretim için
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ConversationStorageProvider = ConversationStorageProvider.Redis;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
    options.MaxRetryAttempts = 5;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider> 
    { 
        AIProvider.AzureOpenAI,  // İlk yedek (Azure)
        AIProvider.Anthropic,    // İkinci yedek
        AIProvider.Custom       // Son yedek (Ollama)
    };
    
    // Veritabanı yapılandırması
    options.DatabaseConnections = new List<DatabaseConnectionConfig>
    {
        new DatabaseConnectionConfig
        {
            Name = "Üretim DB",
            DatabaseType = DatabaseType.SqlServer,
            ConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING"),
            Description = "Üretim veritabanı",
            Enabled = true,
            MaxRowsPerQuery = 5000,
            QueryTimeoutSeconds = 30,
            SchemaRefreshIntervalMinutes = 60
        }
    };
});
```

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-rocket"></i>
            </div>
            <h3>Başlangıç</h3>
            <p>SmartRAG'ı projenize entegre edin</p>
            <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-outline-primary btn-sm mt-3">
                Başlangıç Kılavuzu
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-code"></i>
            </div>
            <h3>Örnekler</h3>
            <p>Pratik örnekleri ve gerçek dünya kullanım senaryolarını görün</p>
            <a href="{{ site.baseurl }}/tr/examples" class="btn btn-outline-primary btn-sm mt-3">
                Örnekleri Gör
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-book"></i>
            </div>
            <h3>API Referansı</h3>
            <p>Detaylı API dokümantasyonu ve metod referansları</p>
            <a href="{{ site.baseurl }}/tr/api-reference" class="btn btn-outline-primary btn-sm mt-3">
                API Referansı
            </a>
        </div>
    </div>
</div>
