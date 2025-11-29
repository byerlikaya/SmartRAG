---
layout: default
title: Depolama Sağlayıcıları
description: SmartRAG depolama sağlayıcı yapılandırması - Qdrant, Redis ve InMemory depolama seçenekleri
lang: tr
---

## Depolama Sağlayıcı Yapılandırması

SmartRAG çeşitli depolama sağlayıcılarını destekler:

## Qdrant (Vektör Veritabanı)

Qdrant, milyonlarca vektörle üretim kullanımı için tasarlanmış yüksek performanslı bir vektör veritabanıdır:

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

**Avantajlar:**
- 🚀 Yüksek performanslı vektör arama
- 📈 Ölçeklenebilir (milyonlarca vektör)
- 🔍 Gelişmiş filtreleme ve metadata desteği
- 🏢 Üretim için ideal

**Dezavantajlar:**
- 🐳 Docker gerektirir
- 💾 Ek kaynak kullanımı
- 🔧 Kurulum karmaşıklığı

## Redis (Yüksek Performanslı Önbellek)

Redis, RediSearch kullanarak vektör benzerlik araması yetenekleriyle hızlı bellek içi depolama sağlar:

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

**Avantajlar:**
- ⚡ Çok hızlı erişim
- 🔄 Otomatik expire desteği
- 📊 Zengin veri tipleri
- 🔍 RediSearch ile vektör benzerlik araması
- 🏢 Üretim için uygun

**Dezavantajlar:**
- 💾 RAM tabanlı (sınırlı kapasite)
- 🔧 Vektör arama için RediSearch modülü gerekli
- 💰 Ek maliyet

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> RediSearch Modülü Gerekli</h4>
    <p class="mb-0"><strong>Vektör arama için RediSearch modülü gereklidir.</strong> <code>redis/redis-stack-server:latest</code> Docker image'ını kullanın veya Redis sunucunuza RediSearch modülünü yükleyin. RediSearch olmadan sadece metin araması çalışır (vektör benzerlik araması çalışmaz).</p>
    <p class="mb-0 mt-2"><strong>Docker örneği:</strong></p>
    <pre class="mt-2"><code>docker run -d -p 6379:6379 redis/redis-stack-server:latest</code></pre>
</div>

## InMemory (RAM Depolama)

InMemory depolama, test ve geliştirme için idealdir, tüm verileri RAM'de saklar:

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

**Kullanım Senaryoları:**
- 🧪 Test ve geliştirme
- 🚀 Prototip oluşturma
- 📊 Geçici veri
- 🔬 Konsept kanıtı

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Önemli</h4>
    <p class="mb-0">InMemory depolama, uygulama yeniden başlatıldığında tüm verileri kaybeder. Üretim için uygun değil!</p>
</div>

## Depolama Sağlayıcı Karşılaştırması

<p>Kullanım durumunuz için en iyi seçeneği seçmek üzere depolama sağlayıcılarını karşılaştırın:</p>

<div class="table-responsive">
<table class="table">
<thead>
<tr>
<th>Sağlayıcı</th>
<th>Performans</th>
<th>Ölçeklenebilirlik</th>
<th>Kurulum</th>
<th>Maliyet</th>
<th>Üretim Uygunluğu</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Qdrant</strong></td>
<td>⭐⭐⭐⭐⭐</td>
<td>⭐⭐⭐⭐⭐</td>
<td>⭐⭐⭐</td>
<td>⭐⭐⭐</td>
<td><span class="badge bg-success">Mükemmel</span></td>
</tr>
<tr>
<td><strong>Redis</strong></td>
<td>⭐⭐⭐⭐⭐</td>
<td>⭐⭐⭐⭐</td>
<td>⭐⭐⭐</td>
<td>⭐⭐⭐</td>
<td><span class="badge bg-success">İyi</span></td>
</tr>
<tr>
<td><strong>InMemory</strong></td>
<td>⭐⭐⭐⭐⭐</td>
<td>⭐</td>
<td>⭐⭐⭐⭐⭐</td>
<td>⭐⭐⭐⭐⭐</td>
<td><span class="badge bg-secondary">Sadece test</span></td>
</tr>
</tbody>
</table>
</div>

## Önerilen Kullanım Senaryoları

### Geliştirme ve Test
```csharp
// Hızlı geliştirme ve test için
options.StorageProvider = StorageProvider.InMemory;
```

### Orta Ölçekli Uygulamalar
```csharp
// RediSearch ile hızlı ve ölçeklenebilir
options.StorageProvider = StorageProvider.Redis;
```

### Büyük Ölçekli Üretim Uygulamaları
```csharp
// Milyonlarca vektör için maksimum performans ve ölçeklenebilirlik
options.StorageProvider = StorageProvider.Qdrant;
```

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-server"></i>
            </div>
            <h3>Veritabanı Yapılandırması</h3>
            <p>Çoklu veritabanı bağlantıları ve şema analizi</p>
            <a href="{{ site.baseurl }}/tr/configuration/database" class="btn btn-outline-primary btn-sm mt-3">
                Veritabanı Yapılandırması
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-microphone"></i>
            </div>
            <h3>Ses & OCR</h3>
            <p>Whisper.net ve Tesseract OCR</p>
            <a href="{{ site.baseurl }}/tr/configuration/audio-ocr" class="btn btn-outline-primary btn-sm mt-3">
                Ses & OCR
            </a>
        </div>
    </div>
</div>
