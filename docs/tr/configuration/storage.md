---
layout: default
title: Depolama Sağlayıcıları
description: SmartRAG depolama sağlayıcı yapılandırması - Qdrant, Redis, SQLite, FileSystem ve InMemory depolama seçenekleri
lang: tr
---

## Depolama Sağlayıcı Yapılandırması

SmartRAG çeşitli depolama sağlayıcılarını destekler:

---

## Qdrant (Vektör Veritabanı)

```json
{
  "Storage": {
    "Qdrant": {
      "Host": "localhost:6334",
      "UseHttps": false,
      "ApiKey": "qdrant-anahtariniz",
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

**Avantajlar:**
- 🚀 Yüksek performanslı vektör arama
- 📈 Ölçeklenebilir (milyonlarca vektör)
- 🔍 Gelişmiş filtreleme ve metadata desteği
- 🏢 Üretim için ideal

**Dezavantajlar:**
- 🐳 Docker gerektirir
- 💾 Ek kaynak kullanımı
- 🔧 Kurulum karmaşıklığı

---

## Redis (Yüksek Performanslı Önbellek)

```json
{
  "Storage": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Database": 0,
      "KeyPrefix": "smartrag:"
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
- 🏢 Üretim için uygun

**Dezavantajlar:**
- 💾 RAM tabanlı (sınırlı kapasite)
- 🔧 Redis kurulumu gerekli
- 💰 Ek maliyet

---

## SQLite (Gömülü Veritabanı)

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

**Avantajlar:**
- 📁 Tek dosya veritabanı
- 🔒 Veri gizliliği (yerel)
- 🚀 Hızlı kurulum
- 💰 Maliyet yok

**Dezavantajlar:**
- 📊 Sınırlı eşzamanlı erişim
- 🔄 Backup gerektirir
- 📈 Ölçeklenebilirlik sınırları

---

## FileSystem (Dosya Tabanlı Depolama)

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

**Avantajlar:**
- 📁 Basit dosya sistemi
- 🔍 Kolay debug ve inceleme
- 💾 Sınırsız kapasite
- 🔒 Tam kontrol

**Dezavantajlar:**
- 🐌 Yavaş arama performansı
- 📊 Metadata sınırları
- 🔄 Manuel backup

---

## InMemory (RAM Depolama)

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

**Kullanım Senaryoları:**
- 🧪 Test ve geliştirme
- 🚀 Prototip oluşturma
- 📊 Geçici veri
- 🔬 Konsept kanıtı

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Önemli</h4>
    <p class="mb-0">InMemory depolama, uygulama yeniden başlatıldığında tüm verileri kaybeder. Üretim için uygun değil!</p>
</div>

---

## Depolama Sağlayıcı Karşılaştırması

| Sağlayıcı | Performans | Ölçeklenebilirlik | Kurulum | Maliyet | Üretim Uygunluğu |
|-----------|------------|-------------------|---------|---------|------------------|
| **Qdrant** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ✅ Mükemmel |
| **Redis** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ✅ İyi |
| **SQLite** | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⚠️ Sınırlı |
| **FileSystem** | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ❌ Uygun değil |
| **InMemory** | ⭐⭐⭐⭐⭐ | ⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ❌ Test only |

---

## Önerilen Kullanım Senaryoları

### Geliştirme ve Test
```csharp
// Hızlı geliştirme için
options.StorageProvider = StorageProvider.InMemory;
```

### Küçük Ölçekli Uygulamalar
```csharp
// Basit ve güvenilir
options.StorageProvider = StorageProvider.SQLite;
```

### Orta Ölçekli Uygulamalar
```csharp
// Hızlı ve ölçeklenebilir
options.StorageProvider = StorageProvider.Redis;
```

### Büyük Ölçekli Uygulamalar
```csharp
// Maksimum performans ve ölçeklenebilirlik
options.StorageProvider = StorageProvider.Qdrant;
```

---

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
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
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-microphone"></i>
            </div>
            <h3>Ses & OCR</h3>
            <p>Google Speech-to-Text ve Tesseract OCR</p>
            <a href="{{ site.baseurl }}/tr/configuration/audio-ocr" class="btn btn-outline-primary btn-sm mt-3">
                Ses & OCR
            </a>
        </div>
    </div>
</div>
