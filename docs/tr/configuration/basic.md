---
layout: default
title: Temel Yapılandırma
description: SmartRAG temel yapılandırma seçenekleri - yapılandırma yöntemleri, parçalama ve yeniden deneme ayarları
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

## SmartRagOptions - Temel Seçenekler

| Seçenek | Tip | Varsayılan | Açıklama |
|--------|------|---------|-------------|
| `AIProvider` | `AIProvider` | `OpenAI` | Embedding'ler ve metin üretimi için AI sağlayıcı |
| `StorageProvider` | `StorageProvider` | `InMemory` | Dokümanlar ve vektörler için depolama backend'i |
| `ConversationStorageProvider` | `ConversationStorageProvider?` | `null` | Konuşma geçmişi için ayrı depolama (isteğe bağlı) |

---

## Parçalama Seçenekleri

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

---

## Yeniden Deneme & Dayanıklılık Seçenekleri

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

---

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

---

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
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
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
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
