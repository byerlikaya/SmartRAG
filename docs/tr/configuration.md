---
layout: default
title: Yapılandırma
description: SmartRAG için eksiksiz yapılandırma kılavuzu - AI sağlayıcıları, depolama, veritabanları ve gelişmiş seçenekler
lang: tr
---


## Temel Yapılandırma

SmartRAG iki yapılandırma yöntemi sunar:

### Yöntem 1: UseSmartRag (Basit)

```csharp
builder.Services.UseSmartRag(configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.Gemini
);
```

### Yöntem 2: AddSmartRag (Gelişmiş)

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;
    // ... ek seçenekler
});
```

---

## SmartRagOptions - Eksiksiz Referans

Tüm mevcut yapılandırma seçenekleri:

### Temel Seçenekler

| Seçenek | Tip | Varsayılan | Açıklama |
|--------|------|---------|-------------|
| `AIProvider` | `AIProvider` | `OpenAI` | Embedding'ler ve metin üretimi için AI sağlayıcı |
| `StorageProvider` | `StorageProvider` | `InMemory` | Dokümanlar ve vektörler için depolama backend'i |
| `ConversationStorageProvider` | `ConversationStorageProvider?` | `null` | Konuşma geçmişi için ayrı depolama (isteğe bağlı) |

### Parçalama Seçenekleri

| Seçenek | Tip | Varsayılan | Açıklama |
|--------|------|---------|-------------|
| `MaxChunkSize` | `int` | `1000` | Her doküman parçasının karakter cinsinden maksimum boyutu |
| `MinChunkSize` | `int` | `100` | Her doküman parçasının karakter cinsinden minimum boyutu |
| `ChunkOverlap` | `int` | `200` | Bitişik parçalar arasında örtüşecek karakter sayısı |

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Parçalama En İyi Pratikleri</h4>
    <ul class="mb-0">
        <li><strong>MaxChunkSize:</strong> Optimal denge için 500-1000 karakter</li>
        <li><strong>ChunkOverlap:</strong> Bağlam koruması için MaxChunkSize'ın %15-20'si</li>
        <li><strong>Daha büyük parçalar:</strong> Daha iyi bağlam, ama daha yavaş arama</li>
        <li><strong>Daha küçük parçalar:</strong> Daha kesin sonuçlar, ama daha az bağlam</li>
    </ul>
                    </div>

### Yeniden Deneme & Dayanıklılık Seçenekleri

| Seçenek | Tip | Varsayılan | Açıklama |
|--------|------|---------|-------------|
| `MaxRetryAttempts` | `int` | `3` | AI sağlayıcı istekleri için maksimum yeniden deneme sayısı |
| `RetryDelayMs` | `int` | `1000` | Yeniden denemeler arası bekleme süresi (milisaniye) |
| `RetryPolicy` | `RetryPolicy` | `ExponentialBackoff` | Başarısız istekler için yeniden deneme politikası |
| `EnableFallbackProviders` | `bool` | `false` | Hata durumunda alternatif AI sağlayıcılarına geçiş |
| `FallbackProviders` | `List<AIProvider>` | `[]` | Sırayla denenecek yedek AI sağlayıcıları listesi |

**RetryPolicy Enum Değerleri:**
- `RetryPolicy.None` - Yeniden deneme yok
- `RetryPolicy.FixedDelay` - Sabit bekleme süresi
- `RetryPolicy.LinearBackoff` - Doğrusal artan bekleme
- `RetryPolicy.ExponentialBackoff` - Üssel artan bekleme (önerilen)

### Veritabanı Seçenekleri

| Seçenek | Tip | Varsayılan | Açıklama |
|--------|------|---------|-------------|
| `DatabaseConnections` | `List<DatabaseConnectionConfig>` | `[]` | Akıllı çapraz-veritabanı sorguları için çoklu veritabanı bağlantıları |
| `EnableAutoSchemaAnalysis` | `bool` | `true` | Başlangıçta otomatik olarak veritabanı şemalarını analiz et |
| `EnablePeriodicSchemaRefresh` | `bool` | `true` | Veritabanı şemalarını periyodik olarak yenile |
| `DefaultSchemaRefreshIntervalMinutes` | `int` | `60` | Varsayılan şema yenileme aralığı (0 = bağlantı ayarlarını kullan) |

### Ses Seçenekleri

| Seçenek | Tip | Varsayılan | Açıklama |
|--------|------|---------|-------------|
| `GoogleSpeechConfig` | `GoogleSpeechConfig` | `null` | Ses transkripsiyonu için Google Speech-to-Text yapılandırması |

---

## AI Sağlayıcı Yapılandırması

### OpenAI

```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-ANAHTARINIZ",
      "Endpoint": "https://api.openai.com/v1",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
});
```

**Modeller:**
- `gpt-4`, `gpt-4-turbo`, `gpt-4o` - Gelişmiş akıl yürütme
- `gpt-3.5-turbo` - Hızlı ve uygun maliyetli
- `text-embedding-ada-002`, `text-embedding-3-small`, `text-embedding-3-large` - Embedding'ler

---

### Anthropic (Claude)

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Önemli: VoyageAI Gerekli</h4>
    <p>
        Anthropic Claude modelleri, embedding'ler için <strong>ayrı bir VoyageAI API anahtarı</strong> gerektirir çünkü Anthropic embedding modelleri sağlamaz.
    </p>
    <ul class="mb-0">
        <li><strong>VoyageAI Anahtarı Alın:</strong> <a href="https://console.voyageai.com/" target="_blank">console.voyageai.com</a></li>
        <li><strong>Dokümantasyon:</strong> <a href="https://docs.anthropic.com/en/docs/build-with-claude/embeddings" target="_blank">Anthropic Embeddings Kılavuzu</a></li>
    </ul>
                        </div>
                        
```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "sk-ant-ANTHROPIC_ANAHTARINIZ",
      "Model": "claude-3-5-sonnet-20241022",
      "MaxTokens": 4096,
      "Temperature": 0.3,
      "EmbeddingApiKey": "pa-VOYAGE_ANAHTARINIZ",
      "EmbeddingModel": "voyage-large-2"
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
});
```

**Claude Modelleri:**
- `claude-3-5-sonnet-20241022` - En akıllı (önerilen)
- `claude-3-opus-20240229` - En yüksek yetenek
- `claude-3-haiku-20240307` - En hızlı

**VoyageAI Embedding Modelleri:**
- `voyage-large-2` - Yüksek kalite (önerilen)
- `voyage-code-2` - Kod için optimize edilmiş
- `voyage-2` - Genel amaçlı

---

### Google Gemini

```json
{
  "AI": {
    "Gemini": {
      "ApiKey": "GEMINI_ANAHTARINIZ",
      "Model": "gemini-pro",
      "EmbeddingModel": "embedding-001",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Gemini;
});
```

**Modeller:**
- `gemini-pro` - Metin üretimi
- `gemini-pro-vision` - Çok modlu (metin + görsel)
- `embedding-001` - Metin embedding'leri

---

### Azure OpenAI

```json
{
  "AI": {
    "AzureOpenAI": {
      "ApiKey": "AZURE_ANAHTARINIZ",
      "Endpoint": "https://your-resource.openai.azure.com/",
    "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002",
      "DeploymentName": "gpt-4-deployment",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.AzureOpenAI;
});
```

---

### Özel Sağlayıcı (Ollama / LM Studio)

<div class="alert alert-success">
    <h4><i class="fas fa-server me-2"></i> Ollama / LM Studio ile %100 Yerel AI</h4>
    <p>Tam veri gizliliği için AI modellerini tamamen yerel olarak çalıştırın - yerinde dağıtımlar, GDPR/KVKK/HIPAA uyumluluğu için mükemmel.</p>
                         </div>
                         
#### Ollama (Yerel Modeller)

```json
{
  "AI": {
    "Custom": {
      "ApiKey": "gerekli-degil",
      "Endpoint": "http://localhost:11434/v1/chat/completions",
      "Model": "llama2",
      "EmbeddingModel": "nomic-embed-text"
    }
  }
}
```

#### LM Studio (Yerel Modeller)

```json
{
  "AI": {
    "Custom": {
      "ApiKey": "gerekli-degil",
      "Endpoint": "http://localhost:1234/v1/chat/completions",
      "Model": "local-model",
      "EmbeddingModel": "local-embedding"
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Custom;
});
```

**Desteklenen Özel API'ler:**
- 🦙 Ollama - Yerel modeller
- 🏠 LM Studio - Yerel AI ortamı
- 🔗 OpenRouter - 100+ modele erişim
- ⚡ Groq - Yıldırım hızı çıkarım
- 🌐 Together AI - Açık kaynak modeller
- Herhangi bir OpenAI-uyumlu API

---

## Depolama Sağlayıcı Yapılandırması

### Qdrant (Vektör Veritabanı)

```json
{
  "Storage": {
    "Qdrant": {
      "Host": "localhost:6334",
      "UseHttps": false,
      "ApiKey": "qdrant-anahtariniz",
      "CollectionName": "smartrag_documents",
      "VectorSize": 1536,
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

**Ne zaman kullanılır:**
- Üretim ortamları
- Büyük doküman koleksiyonları (10.000+ doküman)
- Yüksek performanslı benzerlik araması
- Ölçeklenebilir dağıtımlar

---

### Redis (Yüksek Performanslı Önbellek)

```json
{
  "Storage": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Password": "",
      "Database": 0,
      "KeyPrefix": "smartrag:",
      "ConnectionTimeout": 30,
      "EnableSsl": false
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

**Ne zaman kullanılır:**
- Hızlı okuma/yazma operasyonları
- Dağıtık dağıtımlar
- Oturum tabanlı uygulamalar
- Konuşma geçmişi depolama

---

### SQLite (Gömülü Veritabanı)

```json
{
  "Storage": {
    "Sqlite": {
      "DatabasePath": "smartrag.db",
      "EnableForeignKeys": true
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.Sqlite;
});
```

**Ne zaman kullanılır:**
- Masaüstü uygulamaları
- Tek kullanıcılı senaryolar
- Dış bağımlılık yok
- Basit dağıtımlar

---

### FileSystem (Dosya Tabanlı Depolama)

```json
{
  "Storage": {
    "FileSystem": {
      "FileSystemPath": "./smartrag_storage"
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

**Ne zaman kullanılır:**
- Geliştirme ve test
- Basit yedekleme/geri yükleme senaryoları
- Veritabanı altyapısı mevcut değil

---

### InMemory (RAM Depolama)

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

**Ne zaman kullanılır:**
- Geliştirme ve test
- Konsept kanıtı
- Birim testleri
- Geçici veri

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Önemli</h4>
    <p class="mb-0">InMemory depolama, uygulama yeniden başlatıldığında tüm verileri kaybeder. Üretim için uygun değil!</p>
                         </div>

---

## Veritabanı Yapılandırması

### Çoklu Veritabanı Bağlantıları

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.DatabaseConnections = new List<DatabaseConnectionConfig>
    {
        new DatabaseConnectionConfig
        {
            Name = "Satış Veritabanı",
            Type = DatabaseType.SqlServer,
            ConnectionString = "Server=localhost;Database=Sales;...",
            IncludedTables = new List<string> { "Orders", "Customers" },
            MaxRowsPerTable = 1000,
            SanitizeSensitiveData = true
        },
        new DatabaseConnectionConfig
        {
            Name = "Envanter Veritabanı",
            Type = DatabaseType.MySQL,
            ConnectionString = "Server=localhost;Database=Inventory;...",
            MaxRowsPerTable = 1000
        }
    };
    
    options.EnableAutoSchemaAnalysis = true;
    options.EnablePeriodicSchemaRefresh = true;
    options.DefaultSchemaRefreshIntervalMinutes = 60;
});
```

### DatabaseConfig Parametreleri

| Parametre | Tip | Varsayılan | Açıklama |
|-----------|------|---------|-------------|
| `Name` | `string` | - | Veritabanı bağlantısı için kolay ad |
| `Type` | `DatabaseType` | - | Veritabanı tipi (SqlServer, MySql, PostgreSql, Sqlite) |
| `ConnectionString` | `string` | - | Veritabanı bağlantı dizesi |
| `IncludedTables` | `List<string>` | `[]` | Dahil edilecek spesifik tablolar (boş = tüm tablolar) |
| `ExcludedTables` | `List<string>` | `[]` | Analizden hariç tutulacak tablolar |
| `MaxRowsPerTable` | `int` | `1000` | Tablo başına çıkarılacak maksimum satır |
| `SanitizeSensitiveData` | `bool` | `true` | Hassas sütunları otomatik temizle |
| `SensitiveColumns` | `List<string>` | Aşağıya bakın | Temizlenecek sütun adları |

**Varsayılan Hassas Sütunlar:**
- `password`, `pwd`, `pass`
- `ssn`, `social_security`
- `credit_card`, `creditcard`, `cc_number`
- `email`, `mail`
- `phone`, `telephone`
- `salary`, `compensation`

---

## Ses Yapılandırması

### Google Speech-to-Text

```json
{
  "GoogleSpeech": {
    "CredentialsPath": "./path/to/google-credentials.json",
    "DefaultLanguageCode": "tr-TR",
    "EnableAutomaticPunctuation": true,
    "Model": "default"
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.GoogleSpeechConfig = new GoogleSpeechConfig
    {
        CredentialsPath = "./google-credentials.json",
        DefaultLanguageCode = "tr-TR",
        EnableAutomaticPunctuation = true
    };
});
```

**Desteklenen Dil Kodları:**
- `tr-TR` - Türkçe (Türkiye)
- `en-US` - İngilizce (ABD)
- `de-DE` - Almanca (Almanya)
- `fr-FR` - Fransızca (Fransa)
- 100+ dil desteklenir

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Gizlilik Notu</h4>
    <p class="mb-0">
        Ses dosyaları transkripsiyon için Google Cloud'a gönderilir. Tam veri gizliliği için ses dosyası yüklemeyin veya alternatif yerel çözümler kullanın.
    </p>
                    </div>

---

## OCR Yapılandırması

### Tesseract Dil Desteği

```csharp
// Görselleri yüklerken OCR için dil belirtin
var document = await _documentService.UploadDocumentAsync(
    imageStream,
    "fatura.jpg",
    "image/jpeg",
    "kullanici-id",
    language: "tur"  // Türkçe OCR
);

// İngilizce OCR
language: "eng"

// Çoklu dil
language: "tur+eng"
```

**Desteklenen Diller:**
- `tur` - Türkçe
- `eng` - İngilizce
- `deu` - Almanca
- `fra` - Fransızca
- `rus` - Rusça
- `ara` - Arapça
- 100+ dil

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> OCR Yetenekleri</h4>
    <ul class="mb-0">
        <li><strong>✅ Mükemmel çalışır:</strong> Basılı dokümanlar, taranmış metinler, dijital ekran görüntüleri</li>
        <li><strong>⚠️ Sınırlı destek:</strong> El yazısı metin (çok düşük doğruluk)</li>
        <li><strong>💡 En iyi sonuçlar:</strong> Basılı dokümanların yüksek kaliteli taramaları</li>
        <li><strong>🔒 %100 Yerel:</strong> Buluta veri gönderilmez - Tesseract yerel olarak çalışır</li>
    </ul>
                    </div>
                    
---

## Gelişmiş Yapılandırma Örnekleri

### Yedek Sağlayıcılar

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // Birincil sağlayıcı
    options.AIProvider = AIProvider.OpenAI;
    
    // Yedekleme etkinleştir
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider> 
    { 
        AIProvider.Anthropic,  // OpenAI başarısız olursa Anthropic dene
        AIProvider.Gemini,     // Anthropic başarısız olursa Gemini dene
        AIProvider.Custom      // Son çare olarak özel sağlayıcı
    };
    
    // Yeniden deneme yapılandırması
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
});
```

---

## En İyi Pratikler

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="alert alert-success">
            <h4><i class="fas fa-check-circle me-2"></i> Güvenlik</h4>
            <ul class="mb-0">
                <li>API anahtarlarını asla kaynak kontrolüne commit etmeyin</li>
                <li>Üretim için environment variables kullanın</li>
                <li>Veritabanları için SanitizeSensitiveData'yı etkinleştirin</li>
                <li>Dış servisler için HTTPS kullanın</li>
            </ul>
                </div>
            </div>
    
    <div class="col-md-6">
        <div class="alert alert-info">
            <h4><i class="fas fa-bolt me-2"></i> Performans</h4>
            <ul class="mb-0">
                <li>Üretim için Qdrant veya Redis kullanın</li>
                <li>Uygun chunk boyutları yapılandırın</li>
                <li>Güvenilirlik için yedek sağlayıcıları etkinleştirin</li>
                <li>Makul MaxRowsPerTable limitleri ayarlayın</li>
            </ul>
                     </div>
                </div>
            </div>

---

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-code"></i>
            </div>
            <h3>API Referans</h3>
            <p>Tüm interface'ler ve metodlarla eksiksiz API dokümantasyonunu keşfedin</p>
            <a href="{{ site.baseurl }}/tr/api-reference" class="btn btn-outline-primary btn-sm mt-3">
                API Dokümanlarını Görüntüle
            </a>
                            </div>
                        </div>
    
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-lightbulb"></i>
                            </div>
            <h3>Örnekler</h3>
            <p>Pratik örnekleri ve gerçek dünya kullanım senaryolarını görün</p>
            <a href="{{ site.baseurl }}/tr/examples" class="btn btn-outline-primary btn-sm mt-3">
                Örnekleri Gör
            </a>
                            </div>
                        </div>
    
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-book"></i>
                            </div>
            <h3>Changelog</h3>
            <p>Versiyon geçmişini ve taşınma kılavuzlarını takip edin</p>
            <a href="{{ site.baseurl }}/tr/changelog" class="btn btn-outline-primary btn-sm mt-3">
                Changelog'u Görüntüle
            </a>
                        </div>
                    </div>
                </div>

