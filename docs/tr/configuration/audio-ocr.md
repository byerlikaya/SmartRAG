---
layout: default
title: Ses & OCR
description: SmartRAG ses ve OCR yapılandırması - Whisper.net ve Tesseract OCR ayarları
lang: tr
---

## Ses & OCR Yapılandırması

SmartRAG ses dosyalarını metne çevirme ve görsellerden metin çıkarma yetenekleri sunar:

## Whisper.net (Yerel Ses Transkripsiyonu)

Whisper.net, 99+ dil desteğiyle yerel, on-premise ses transkripsiyonu sağlar:

### WhisperConfig Parametreleri

| Parametre | Tip | Varsayılan | Açıklama |
|-----------|-----|-----------|----------|
| `ModelPath` | string | `"models/ggml-large-v3.bin"` | Whisper model dosyası yolu |
| `DefaultLanguage` | string | `"auto"` | Transkripsiyon için dil kodu |
| `MinConfidenceThreshold` | double | `0.3` | Minimum güven skoru (0.0-1.0) |
| `IncludeWordTimestamps` | bool | `false` | Kelime bazlı zaman damgaları dahil et |
| `PromptHint` | string | `""` | Daha iyi doğruluk için bağlam ipucu |
| `MaxThreads` | int | `0` | CPU thread sayısı (0 = otomatik algılama) |

### Whisper Model Boyutları

| Model | Boyut | Hız | Doğruluk | Kullanım Durumu |
|-------|-------|-----|----------|-----------------|
| `tiny` | 75MB | ⭐⭐⭐⭐⭐ | ⭐⭐ | Hızlı prototipleme |
| `base` | 142MB | ⭐⭐⭐⭐ | ⭐⭐⭐ | Dengeli performans |
| `small` | 244MB | ⭐⭐⭐ | ⭐⭐⭐⭐ | İyi doğruluk |
| `medium` | 769MB | ⭐⭐ | ⭐⭐⭐⭐⭐ | Yüksek doğruluk |
| `large-v3` | 1.5GB | ⭐ | ⭐⭐⭐⭐⭐ | En iyi doğruluk |

### Model İndirme

Whisper.net, ilk kullanımda Hugging Face'den GGML modellerini otomatik olarak indirir. Modeller `ModelPath` yapılandırmasında belirtilen yola kaydedilir:

**Otomatik İndirme:**
- Modeller ilk kullanıldığında `WhisperGgmlDownloader` aracılığıyla otomatik indirilir
- Hugging Face deposundan indirilir
- `ModelPath` içinde belirtilen yola kaydedilir (varsayılan: `models/ggml-large-v3.bin`)
- Manuel indirme gerekmez

**Model Dosyaları:**
- Format: `ggml-{model-adı}.bin` (örn., `ggml-base.bin`, `ggml-large-v3.bin`)
- Mevcut modeller: `tiny`, `base`, `small`, `medium`, `large-v3`
- İlk kullanımda model otomatik indirilir (~5-10 dakika, bağlantı ve model boyutuna bağlı)

**Yapılandırma:**
```json
{
  "SmartRAG": {
    "WhisperConfig": {
      "ModelPath": "models/ggml-large-v3.bin"
    }
  }
}
```

**Önemli Notlar:**
- Whisper.net kendi GGML model formatını ve indirme sistemini kullanır
- Bu, Ollama, LM Studio veya cloud servislerinden **bağımsızdır**
- Modeller `ModelPath` konumunda yerel olarak saklanır
- On-premise dağıtımlar için, uygulamanın model dizinine yazma erişimi olduğundan emin olun
- Cloud dağıtımlar için, modelleri önceden indirmeyi veya kalıcı depolama birimleri kullanmayı düşünün

### Yapılandırma Örneği

```json
{
  "SmartRAG": {
    "WhisperConfig": {
      "ModelPath": "models/ggml-large-v3.bin",
      "DefaultLanguage": "auto",
      "MinConfidenceThreshold": 0.3,
      "IncludeWordTimestamps": false,
      "PromptHint": "",
      "MaxThreads": 0
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.WhisperConfig = new WhisperConfig
    {
        ModelPath = "models/ggml-large-v3.bin",
        DefaultLanguage = "auto",
        MinConfidenceThreshold = 0.3,
        IncludeWordTimestamps = false,
        PromptHint = "",
        MaxThreads = 0
    };
});
```

- `auto` - Otomatik dil algılama (önerilen)
- `tr` - Türkçe
- `en` - İngilizce
- `de` - Almanca
- `fr` - Fransızca
- `es` - İspanyolca
- `it` - İtalyanca
- `ru` - Rusça
- `ja` - Japonca
- `ko` - Korece
- `zh` - Çince
- 99+ dil desteklenir

### Kullanım Örneği

```csharp
// Ses dosyası yükleme
var document = await _documentService.UploadDocumentAsync(
    audioStream,
    "toplanti-kaydi.mp3",
    "audio/mpeg",
    "kullanici-id"
);

// AI ile ses dosyası hakkında soru sorma
var response = await _aiService.AskAsync(
    "Bu toplantıda hangi konular konuşuldu?",
    "kullanici-id"
);
```

<div class="alert alert-success">
    <h4><i class="fas fa-shield-alt me-2"></i> Gizlilik Öncelikli</h4>
    <p class="mb-0">
        Ses dosyaları Whisper.net kullanılarak yerel olarak işlenir. Hiçbir veri makinenizi terk etmez - GDPR/KVKK/HIPAA uyumluluğu için mükemmel.
    </p>
</div>

## OCR Yapılandırması

Tesseract OCR, 100+ dil desteğiyle görsellerden ve PDF'lerden metin çıkarma sağlar:

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

### Desteklenen OCR Dilleri

- `tur` - Türkçe
- `eng` - İngilizce
- `deu` - Almanca
- `fra` - Fransızca
- `spa` - İspanyolca
- `ita` - İtalyanca
- `rus` - Rusça
- `ara` - Arapça
- `chi` - Çince
- `jpn` - Japonca
- `kor` - Korece
- `hin` - Hintçe
- 100+ dil desteklenir

### OCR Kullanım Örnekleri

```csharp
// Fatura analizi
var invoice = await _documentService.UploadDocumentAsync(
    invoiceStream,
    "fatura-2024-01.pdf",
    "application/pdf",
    "kullanici-id",
    language: "tur"
);

var analysis = await _aiService.AskAsync(
    "Bu faturada hangi ürünler var ve toplam tutar nedir?",
    "kullanici-id"
);

// Kimlik belgesi analizi
var idCard = await _documentService.UploadDocumentAsync(
    idCardStream,
    "kimlik.jpg",
    "image/jpeg",
    "kullanici-id",
    language: "tur"
);

var info = await _aiService.AskAsync(
    "Bu kimlik belgesindeki kişinin adı ve doğum tarihi nedir?",
    "kullanici-id"
);
```

## OCR Yetenekleri

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> OCR Yetenekleri</h4>
    <ul class="mb-0">
        <li><strong>✅ Mükemmel çalışır:</strong> Basılı dokümanlar, taranmış metinler, dijital ekran görüntüleri</li>
        <li><strong>⚠️ Sınırlı destek:</strong> El yazısı metin (çok düşük doğruluk)</li>
        <li><strong>💡 En iyi sonuçlar:</strong> Basılı dokümanların yüksek kaliteli taramaları</li>
        <li><strong>🔒 %100 On-Premise:</strong> Buluta veri gönderilmez - Tesseract on-premise olarak çalışır</li>
    </ul>
</div>

### Desteklenen Dosya Formatları

**Ses Formatları:**
- `audio/mpeg` - MP3 dosyaları
- `audio/wav` - WAV dosyaları
- `audio/m4a` - M4A dosyaları
- `audio/flac` - FLAC dosyaları
- `audio/ogg` - OGG dosyaları

**Görsel Formatları:**
- `image/jpeg` - JPEG görseller
- `image/png` - PNG görseller
- `image/tiff` - TIFF görseller
- `image/bmp` - BMP görseller
- `image/gif` - GIF görseller

**PDF Formatları:**
- `application/pdf` - PDF dokümanları (sayfa sayfa OCR)

### Ses Kalite İpuçları

1. **Temiz Ses:** Arka plan gürültüsü ve eko'dan kaçının
2. **İyi Mikrofon:** Kaliteli kayıt ekipmanı kullanın
3. **Doğru Dil:** Konuşmanın dilini doğru belirtin
4. **Dosya Formatı:** MP3, WAV, M4A formatları en iyi sonucu verir

### OCR Kalite İpuçları

1. **Yüksek Çözünürlük:** En az 300 DPI tarama kalitesi
2. **Temiz Görüntü:** Bulanık veya gölgeli görüntülerden kaçının
3. **Doğru Dil:** Görüntüdeki metnin dilini doğru belirtin
4. **Kontrast:** Yüksek kontrastlı, siyah-beyaz görüntüler tercih edin

## Ses ve OCR Karşılaştırması

<p>Whisper.net ve Tesseract OCR yeteneklerini karşılaştırın:</p>

<div class="table-responsive">
<table class="table">
<thead>
<tr>
<th>Özellik</th>
<th>Whisper.net</th>
<th>Tesseract OCR</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Veri Gizliliği</strong></td>
<td><span class="badge bg-success">%100 On-premise</span></td>
<td><span class="badge bg-success">%100 On-premise</span></td>
</tr>
<tr>
<td><strong>Doğruluk</strong></td>
<td>⭐⭐⭐⭐⭐</td>
<td>⭐⭐⭐⭐</td>
</tr>
<tr>
<td><strong>Dil Desteği</strong></td>
<td>⭐⭐⭐⭐⭐ (99+ dil)</td>
<td>⭐⭐⭐⭐ (100+ dil)</td>
</tr>
<tr>
<td><strong>Kurulum</strong></td>
<td>⭐⭐⭐⭐</td>
<td>⭐⭐⭐⭐⭐</td>
</tr>
<tr>
<td><strong>Maliyet</strong></td>
<td><span class="badge bg-secondary">Ücretsiz</span></td>
<td><span class="badge bg-secondary">Ücretsiz</span></td>
</tr>
<tr>
<td><strong>Performans</strong></td>
<td>⭐⭐⭐⭐</td>
<td>⭐⭐⭐</td>
</tr>
</tbody>
</table>
</div>

## Güvenlik ve Gizlilik

### Ses Güvenliği

```csharp
// Whisper.net tamamen on-premise çalışır
var document = await _documentService.UploadDocumentAsync(
    sensitiveAudioStream,
    "gizli-toplanti.mp3",
    "audio/mpeg",
    "kullanici-id"
    // Veri hiçbir zaman buluta gönderilmez
);
```

### OCR Güvenliği

```csharp
// OCR tamamen on-premise çalışır
var document = await _documentService.UploadDocumentAsync(
    sensitiveImageStream,
    "gizli-dokuman.jpg",
    "image/jpeg",
    "kullanici-id",
    language: "tur"
    // Veri hiçbir zaman buluta gönderilmez
);
```

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-cogs"></i>
            </div>
            <h3>Gelişmiş Yapılandırma</h3>
            <p>Yedek sağlayıcılar ve en iyi pratikler</p>
            <a href="{{ site.baseurl }}/tr/configuration/advanced" class="btn btn-outline-primary btn-sm mt-3">
                Gelişmiş Yapılandırma
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-code"></i>
            </div>
            <h3>Örnekler</h3>
            <p>Ses ve OCR kullanım örnekleri</p>
            <a href="{{ site.baseurl }}/tr/examples" class="btn btn-outline-primary btn-sm mt-3">
                Örnekleri Gör
            </a>
        </div>
    </div>
</div>