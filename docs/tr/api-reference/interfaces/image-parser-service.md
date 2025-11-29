---
layout: default
title: IImageParserService
description: IImageParserService arayüz dokümantasyonu
lang: tr
---

## IImageParserService

**Amaç:** Tesseract kullanarak görüntülerden OCR metin çıkarma

**Namespace:** `SmartRAG.Interfaces.Parser`

Görüntülerden metin çıkarmak için optik karakter tanıma (OCR) sağlar. Tüm işlem Tesseract kullanarak lokaldir.

#### Metodlar

##### ExtractTextFromImageAsync

OCR kullanarak bir görüntüden metin çıkarır.

```csharp
Task<string> ExtractTextFromImageAsync(
    Stream imageStream, 
    string language = "eng"
)
```

**Parametreler:**
- `imageStream` (Stream): İşlenecek görüntü akışı
- `language` (string, isteğe bağlı): OCR için dil kodu (varsayılan: "eng")

**Döndürür:** Çıkarılan metin (string)

**Örnek:**

```csharp
using var imageStream = File.OpenRead("dokuman.png");

var text = await _imageParser.ExtractTextFromImageAsync(
    imageStream, 
    language: "tur"
);

Console.WriteLine($"Çıkarılan Metin: {text}");
```

##### ExtractTextWithConfidenceAsync

Güven skorlarıyla görüntüden metin çıkarır.

```csharp
Task<OcrResult> ExtractTextWithConfidenceAsync(
    Stream imageStream, 
    string language = "eng"
)
```

**Parametreler:**
- `imageStream` (Stream): İşlenecek görüntü akışı
- `language` (string, isteğe bağlı): OCR için dil kodu (varsayılan: "eng")

**Döndürür:** Çıkarılan metin, güven skorları ve metin bloklarıyla `OcrResult`

**Örnek:**

```csharp
using var imageStream = File.OpenRead("fatura.jpg");

var result = await _imageParser.ExtractTextWithConfidenceAsync(
    imageStream, 
    language: "tur"
);

Console.WriteLine($"Metin: {result.ExtractedText}");
Console.WriteLine($"Güven: {result.Confidence:P}");
```

##### PreprocessImageAsync

Daha iyi OCR sonuçları için görüntüyü ön işleme tabi tutar.

```csharp
Task<Stream> PreprocessImageAsync(Stream imageStream)
```

**Parametreler:**
- `imageStream` (Stream): Giriş görüntü akışı

**Dönen Değer:** Ön işleme tabi tutulmuş görüntü akışı

**Ön İşleme Adımları:**
- Gri tonlama dönüşümü
- Kontrast artırma
- Gürültü azaltma
- İkili hale getirme

**Örnek:**

```csharp
using var originalStream = File.OpenRead("dusuk-kalite.jpg");

var preprocessedStream = await _imageParser.PreprocessImageAsync(originalStream);

var result = await _imageParser.ExtractTextFromImageAsync(preprocessedStream);
Console.WriteLine($"Ön işleme sonrası metin: {result}");
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

