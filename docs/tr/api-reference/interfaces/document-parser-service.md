---
layout: default
title: IDocumentParserService
description: IDocumentParserService arayüz dokümantasyonu
lang: tr
---

## IDocumentParserService

**Amaç:** Çoklu format doküman ayrıştırma ve metin çıkarma

**Namespace:** `SmartRAG.Interfaces.Document`

### Metodlar

#### ParseDocumentAsync

Bir dokümanı ayrıştırın ve doküman entity'si oluşturun.

```csharp
Task<Document> ParseDocumentAsync(
    Stream fileStream, 
    string fileName, 
    string contentType, 
    string uploadedBy, 
    string language = null
)
```

#### GetSupportedFileTypes

Desteklenen dosya uzantılarının listesini alın.

```csharp
IEnumerable<string> GetSupportedFileTypes()
```

**Dönen Değerler:**
- `.pdf`, `.docx`, `.doc`
- `.xlsx`, `.xls`
- `.txt`, `.md`, `.json`, `.xml`, `.csv`
- `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.tiff`, `.webp`
- `.mp3`, `.wav`, `.m4a`, `.aac`, `.ogg`, `.flac`, `.wma`
- `.db`, `.sqlite`, `.sqlite3`

#### GetSupportedContentTypes

Desteklenen MIME content type'larının listesini alın.

```csharp
IEnumerable<string> GetSupportedContentTypes()
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

