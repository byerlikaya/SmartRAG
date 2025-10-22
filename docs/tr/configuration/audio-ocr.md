---
layout: default
title: Ses & OCR
description: SmartRAG ses ve OCR yapılandırması - Google Speech-to-Text ve Tesseract OCR ayarları
lang: tr
---

## Ses & OCR Yapılandırması

SmartRAG ses dosyalarını metne çevirme ve görsellerden metin çıkarma yetenekleri sunar:

---

## Google Speech-to-Text

### Yapılandırma

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
        CredentialsPath = "./path/to/google-credentials.json",
        DefaultLanguageCode = "tr-TR",
        EnableAutomaticPunctuation = true,
        Model = "default"
    };
});
```

### Desteklenen Diller

- `tr-TR` - Türkçe (Türkiye)
- `en-US` - İngilizce (ABD)
- `de-DE` - Almanca (Almanya)
- `fr-FR` - Fransızca (Fransa)
- `es-ES` - İspanyolca (İspanya)
- `it-IT` - İtalyanca (İtalya)
- `ru-RU` - Rusça (Rusya)
- `ja-JP` - Japonca (Japonya)
- `ko-KR` - Korece (Güney Kore)
- `zh-CN` - Çince (Çin)
- 100+ dil desteklenir - [Tüm dilleri görün](https://cloud.google.com/speech-to-text/docs/languages)

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

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Gizlilik Notu</h4>
    <p class="mb-0">
        Ses dosyaları transkripsiyon için Google Cloud'a gönderilir. Tam veri gizliliği için ses dosyası yüklemeyin veya alternatif on-premise çözümler kullanın.
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

---

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

**Görsel Formatları:**
- `image/jpeg` - JPEG görseller
- `image/png` - PNG görseller
- `image/tiff` - TIFF görseller
- `image/bmp` - BMP görseller
- `image/gif` - GIF görseller

**PDF Formatları:**
- `application/pdf` - PDF dokümanları (sayfa sayfa OCR)

### OCR Kalite İpuçları

1. **Yüksek Çözünürlük:** En az 300 DPI tarama kalitesi
2. **Temiz Görüntü:** Bulanık veya gölgeli görüntülerden kaçının
3. **Doğru Dil:** Görüntüdeki metnin dilini doğru belirtin
4. **Kontrast:** Yüksek kontrastlı, siyah-beyaz görüntüler tercih edin

---

## Ses ve OCR Karşılaştırması

| Özellik | Google Speech-to-Text | Tesseract OCR |
|---------|----------------------|---------------|
| **Veri Gizliliği** | ❌ Buluta gönderilir | ✅ %100 On-premise |
| **Doğruluk** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Dil Desteği** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Kurulum** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Maliyet** | 💰 Ücretli | 🆓 Ücretsiz |
| **Performans** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |

---

## Güvenlik ve Gizlilik

### Ses Dosyaları için Öneriler

```csharp
// Hassas ses dosyaları için on-premise çözümler kullanın
if (isSensitiveAudio)
{
    // Alternatif: Whisper.cpp veya diğer on-premise çözümler
    throw new NotSupportedException("Hassas ses dosyaları için on-premise çözümler kullanın");
}
```

### OCR için Güvenlik

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

---

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
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
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
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
