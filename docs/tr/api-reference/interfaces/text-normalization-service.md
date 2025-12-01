---
layout: default
title: ITextNormalizationService
description: ITextNormalizationService arayüz dokümantasyonu
lang: tr
---

## ITextNormalizationService

**Amaç:** Metin normalizasyonu ve temizleme

**Namespace:** `SmartRAG.Interfaces.Support`

#### Metodlar

##### NormalizeText

Daha iyi arama eşleştirmesi için metni normalleştirir (Unicode encoding sorunlarını ele alır).

```csharp
string NormalizeText(string text)
```

**Parametreler:**
- `text` (string): Normalleştirilecek metin

**Döndürür:** Normalleştirilmiş metin

##### NormalizeForMatching

Eşleştirme amaçlı metni normalleştirir (kontrol karakterlerini kaldırır ve boşlukları normalleştirir).

```csharp
string NormalizeForMatching(string value)
```

**Parametreler:**
- `value` (string): Normalleştirilecek metin

**Döndürür:** Normalleştirilmiş metin

##### ContainsNormalizedName

İçeriğin normalleştirilmiş isim içerip içermediğini kontrol eder (encoding sorunlarını ele alır).

```csharp
bool ContainsNormalizedName(string content, string searchName)
```

**Parametreler:**
- `content` (string): Aranacak içerik
- `searchName` (string): Aranacak isim

**Döndürür:** İçerikte isim bulunursa true

##### SanitizeForLog

Güvenli loglama için kullanıcı girdisini temizler (kontrol karakterlerini kaldırır ve uzunluğu sınırlar).

```csharp
string SanitizeForLog(string input)
```

**Parametreler:**
- `input` (string): Temizlenecek girdi

**Döndürür:** Temizlenmiş girdi


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

