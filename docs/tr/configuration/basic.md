---
layout: default
title: Temel Yapılandırma
description: SmartRAG temel yapılandırma seçenekleri - yapılandırma yöntemleri, parçalama ve yeniden deneme ayarları
lang: tr
---

## Yapılandırma Yöntemleri

<p>SmartRAG iki yapılandırma yöntemi sunar:</p>

### Hızlı Kurulum (Önerilen)

<p><code>Program.cs</code> veya <code>Startup.cs</code> dosyanızda SmartRAG'i yapılandırın:</p>

```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Tek satırda basit yapılandırma
builder.Services.UseSmartRag(builder.Configuration,
    storageProvider: StorageProvider.InMemory,  // In-memory ile başlayın
    aiProvider: AIProvider.Gemini,              // AI sağlayıcınızı seçin
    defaultLanguage: "tr"                        // Opsiyonel: Doküman işleme için varsayılan dil
);

var app = builder.Build();
app.Run();
```

### Gelişmiş Kurulum

```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Seçeneklerle gelişmiş yapılandırma
builder.Services.AddSmartRag(builder.Configuration, options =>
{
    // AI Sağlayıcı
    options.AIProvider = AIProvider.OpenAI;
    
    // Depolama Sağlayıcı
    options.StorageProvider = StorageProvider.Qdrant;
    
    // Parçalama Yapılandırması
    options.MaxChunkSize = 1000;
    options.MinChunkSize = 100;
    options.ChunkOverlap = 200;
    
    // Yeniden Deneme Yapılandırması
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    
    // Yedek Sağlayıcılar
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider> 
    { 
        AIProvider.Anthropic, 
        AIProvider.Gemini 
    };
    
    // Varsayılan Dil
    options.DefaultLanguage = "tr";  // Opsiyonel: Doküman işleme için varsayılan dil
});

var app = builder.Build();
app.Run();
```

## SmartRagOptions - Temel Seçenekler

SmartRagOptions'da mevcut temel yapılandırma seçenekleri:

<div class="table-responsive">
    <table class="table">
        <thead>
            <tr>
                <th>Seçenek</th>
                <th>Tip</th>
                <th>Varsayılan</th>
                <th>Açıklama</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><code>AIProvider</code></td>
                <td><code>AIProvider</code></td>
                <td><code>OpenAI</code></td>
                <td>Embedding'ler ve metin üretimi için AI sağlayıcı</td>
            </tr>
            <tr>
                <td><code>StorageProvider</code></td>
                <td><code>StorageProvider</code></td>
                <td><code>InMemory</code></td>
                <td>Dokümanlar ve vektörler için depolama backend'i</td>
            </tr>
            <tr>
                <td><code>ConversationStorageProvider</code></td>
                <td><code>ConversationStorageProvider?</code></td>
                <td><code>null</code></td>
                <td>Konuşma geçmişi için ayrı depolama (isteğe bağlı)</td>
            </tr>
            <tr>
                <td><code>EnableAutoSchemaAnalysis</code></td>
                <td><code>bool</code></td>
                <td><code>true</code></td>
                <td>Başlangıçta veritabanı şemalarını otomatik olarak analiz et</td>
            </tr>
            <tr>
                <td><code>DefaultLanguage</code></td>
                <td><code>string?</code></td>
                <td><code>null</code></td>
                <td>Doküman işleme için varsayılan dil kodu (ISO 639-1 formatı, örn. "tr", "en", "de"). WatchedFolderConfig veya doküman yüklemede dil belirtilmediğinde kullanılır.</td>
            </tr>
        </tbody>
    </table>
</div>

## ConversationStorageProvider

Konuşma geçmişi için doküman depolamasından bağımsız ayrı depolama konfigürasyonu:

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;  // Dokümanlar için
    options.ConversationStorageProvider = ConversationStorageProvider.Redis;  // Konuşmalar için
});
```

### Mevcut Seçenekler

| Seçenek | Açıklama |
|--------|-------------|
| `Redis` | Konuşmaları Redis'te sakla (yüksek performans önbellek) |
| `SQLite` | Konuşmaları SQLite veritabanında sakla (gömülü, hafif) |
| `FileSystem` | Konuşmaları dosya sisteminde sakla (basit, kalıcı) |
| `InMemory` | Konuşmaları RAM'de sakla (kalıcı değil, sadece geliştirme) |

### Konfigürasyon Örneği

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
    <h4><i class="fas fa-info-circle me-2"></i> Konuşma Depolama İpuçları</h4>
    <ul class="mb-0">
        <li><strong>Redis:</strong> Üretim için en iyi, yüksek performans önbellekleme</li>
        <li><strong>SQLite:</strong> Geliştirme ve küçük deployment'lar için iyi</li>
        <li><strong>FileSystem:</strong> Basit, insan tarafından okunabilir depolama</li>
        <li><strong>InMemory:</strong> Hızlı, ancak yeniden başlatmada veri kaybolur</li>
    </ul>
</div>

## Parçalama Seçenekleri

<div class="table-responsive">
    <table class="table">
        <thead>
            <tr>
                <th>Seçenek</th>
                <th>Tip</th>
                <th>Varsayılan</th>
                <th>Açıklama</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><code>MaxChunkSize</code></td>
                <td><code>int</code></td>
                <td><code>1000</code></td>
                <td>Her doküman parçasının karakter cinsinden maksimum boyutu</td>
            </tr>
            <tr>
                <td><code>MinChunkSize</code></td>
                <td><code>int</code></td>
                <td><code>100</code></td>
                <td>Her doküman parçasının karakter cinsinden minimum boyutu</td>
            </tr>
            <tr>
                <td><code>ChunkOverlap</code></td>
                <td><code>int</code></td>
                <td><code>200</code></td>
                <td>Bitişik parçalar arasında örtüşecek karakter sayısı</td>
            </tr>
        </tbody>
    </table>
</div>

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Parçalama En İyi Pratikleri</h4>
    <ul class="mb-0">
        <li><strong>MaxChunkSize:</strong> Optimal denge için 500-1000 karakter</li>
        <li><strong>ChunkOverlap:</strong> Bağlam koruması için MaxChunkSize'ın %15-20'si</li>
        <li><strong>Daha büyük parçalar:</strong> Daha iyi bağlam, ama daha yavaş arama</li>
        <li><strong>Daha küçük parçalar:</strong> Daha kesin sonuçlar, ama daha az bağlam</li>
    </ul>
</div>

## Özellik Bayrakları (Feature Toggles)

Hangi arama yeteneklerinin global olarak etkinleştirileceğini kontrol edin:

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.Features.EnableDatabaseSearch = true;
    options.Features.EnableDocumentSearch = true;
    options.Features.EnableAudioSearch = true;
    options.Features.EnableImageSearch = true;
    options.Features.EnableMcpSearch = false;
    options.Features.EnableFileWatcher = false;
});
```

### Özellik Bayrağı Seçenekleri

<div class="table-responsive">
    <table class="table">
        <thead>
            <tr>
                <th>Seçenek</th>
                <th>Tip</th>
                <th>Varsayılan</th>
                <th>Açıklama</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><code>Features.EnableDatabaseSearch</code></td>
                <td><code>bool</code></td>
                <td><code>true</code></td>
                <td>Çoklu veritabanı sorgu yeteneklerini etkinleştir (SQL Server, MySQL, PostgreSQL, SQLite)</td>
            </tr>
            <tr>
                <td><code>Features.EnableDocumentSearch</code></td>
                <td><code>bool</code></td>
                <td><code>true</code></td>
                <td>Doküman metin aramasını etkinleştir (PDF, Word, Excel, vb.)</td>
            </tr>
            <tr>
                <td><code>Features.EnableAudioSearch</code></td>
                <td><code>bool</code></td>
                <td><code>true</code></td>
                <td>Ses dosyası transkripsiyonu ve aramasını etkinleştir (MP3, WAV, vb.)</td>
            </tr>
            <tr>
                <td><code>Features.EnableImageSearch</code></td>
                <td><code>bool</code></td>
                <td><code>true</code></td>
                <td>Görüntü OCR ve aramasını etkinleştir (PNG, JPG, vb.)</td>
            </tr>
            <tr>
                <td><code>Features.EnableMcpSearch</code></td>
                <td><code>bool</code></td>
                <td><code>false</code></td>
                <td>Harici araçlar için MCP (Model Context Protocol) sunucu entegrasyonunu etkinleştir</td>
            </tr>
            <tr>
                <td><code>Features.EnableFileWatcher</code></td>
                <td><code>bool</code></td>
                <td><code>false</code></td>
                <td>İzlenen klasörlerden otomatik doküman indekslemeyi etkinleştir</td>
            </tr>
        </tbody>
    </table>
</div>

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Özellik Bayrağı İpuçları</h4>
    <ul class="mb-0">
        <li><strong>İstek Bazlı Kontrol:</strong> İstek bazlı özellik kontrolü için <code>SearchOptions</code> kullanın</li>
        <li><strong>Global Kontrol:</strong> Global özellik etkinleştirme için <code>Features</code> kullanın</li>
        <li><strong>Performans:</strong> Kullanılmayan özellikleri devre dışı bırakarak performansı artırın</li>
        <li><strong>Kaynak Yönetimi:</strong> Gerekmiyorsa ses/görüntü aramasını devre dışı bırakarak işleme kaynaklarını tasarruf edin</li>
    </ul>
</div>

## DocumentType Özelliği

<code>DocumentChunk</code> içindeki <code>DocumentType</code> özelliği, parçaları içerik tipine göre filtrelemeye olanak tanır:

- **"Document"**: Normal metin dokümanları (PDF, Word, Excel, TXT)
- **"Audio"**: Ses dosyası transkripsiyonları (MP3, WAV, M4A)
- **"Image"**: Görüntü OCR sonuçları (PNG, JPG, vb.)

### Otomatik Algılama

DocumentType, dosya uzantısı ve içerik tipine göre otomatik olarak belirlenir:

```csharp
// Yüklenen dosyalar otomatik olarak kategorize edilir
var document = await _documentService.UploadDocumentAsync(
    fileStream, 
    "invoice.pdf",      // → DocumentType: "Document"
    "application/pdf",
    "user-123"
);

var audio = await _documentService.UploadDocumentAsync(
    audioStream,
    "meeting.mp3",      // → DocumentType: "Audio"
    "audio/mpeg",
    "user-123"
);

var image = await _documentService.UploadDocumentAsync(
    imageStream,
    "receipt.jpg",      // → DocumentType: "Image"
    "image/jpeg",
    "user-123"
);
```

### DocumentType'a Göre Filtreleme

<code>SearchOptions</code> kullanarak doküman tipine göre filtreleyin:

```csharp
// Sadece metin dokümanlarında ara
var options = new SearchOptions
{
    EnableDocumentSearch = true,
    EnableAudioSearch = false,  // Ses parçalarını hariç tut
    EnableImageSearch = false  // Görüntü parçalarını hariç tut
};

var response = await _searchService.QueryIntelligenceAsync(
    "Fatura detaylarını bul",
    maxResults: 10,
    options: options
);
```

## Yeniden Deneme & Dayanıklılık Seçenekleri

<div class="table-responsive">
    <table class="table">
        <thead>
            <tr>
                <th>Seçenek</th>
                <th>Tip</th>
                <th>Varsayılan</th>
                <th>Açıklama</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><code>MaxRetryAttempts</code></td>
                <td><code>int</code></td>
                <td><code>3</code></td>
                <td>AI sağlayıcı istekleri için maksimum yeniden deneme sayısı</td>
            </tr>
            <tr>
                <td><code>RetryDelayMs</code></td>
                <td><code>int</code></td>
                <td><code>1000</code></td>
                <td>Yeniden denemeler arası bekleme süresi (milisaniye)</td>
            </tr>
            <tr>
                <td><code>RetryPolicy</code></td>
                <td><code>RetryPolicy</code></td>
                <td><code>ExponentialBackoff</code></td>
                <td>Başarısız istekler için yeniden deneme politikası</td>
            </tr>
            <tr>
                <td><code>EnableFallbackProviders</code></td>
                <td><code>bool</code></td>
                <td><code>false</code></td>
                <td>Hata durumunda alternatif AI sağlayıcılarına geçiş</td>
            </tr>
            <tr>
                <td><code>FallbackProviders</code></td>
                <td><code>List&lt;AIProvider&gt;</code></td>
                <td><code>[]</code></td>
                <td>Sırayla denenecek yedek AI sağlayıcıları listesi</td>
            </tr>
        </tbody>
    </table>
</div>

**RetryPolicy Enum Değerleri:**
- `RetryPolicy.None` - Yeniden deneme yok
- `RetryPolicy.FixedDelay` - Sabit bekleme süresi
- `RetryPolicy.LinearBackoff` - Doğrusal artan bekleme
- `RetryPolicy.ExponentialBackoff` - Üssel artan bekleme (önerilen)

## Örnek Yapılandırma

### Geliştirme Ortamı

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // Hızlı geliştirme için
    options.AIProvider = AIProvider.Gemini;
    options.StorageProvider = StorageProvider.InMemory;
    options.MaxChunkSize = 500;
    options.ChunkOverlap = 100;
});
```

### Üretim Ortamı

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // Güvenilir üretim için
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

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-brain"></i>
            </div>
            <h3>AI Sağlayıcıları</h3>
            <p>OpenAI, Anthropic, Google Gemini ve özel sağlayıcılar</p>
            <a href="{{ site.baseurl }}/tr/configuration/ai-providers" class="btn btn-outline-primary btn-sm mt-3">
                AI Sağlayıcıları
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-database"></i>
            </div>
            <h3>Depolama Sağlayıcıları</h3>
            <p>Qdrant, Redis, SQLite ve diğer depolama seçenekleri</p>
            <a href="{{ site.baseurl }}/tr/configuration/storage" class="btn btn-outline-primary btn-sm mt-3">
                Depolama Sağlayıcıları
            </a>
        </div>
    </div>
</div>
