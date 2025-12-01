---
layout: default
title: IEmbeddingSearchService
description: IEmbeddingSearchService arayüz dokümantasyonu
lang: tr
---

## IEmbeddingSearchService

**Amaç:** Embedding tabanlı semantik arama

**Namespace:** `SmartRAG.Interfaces.Search`

Temel embedding arama işlevselliği.

#### Metodlar

##### SearchByEmbeddingAsync

Doküman chunk'larında embedding tabanlı arama yapar.

```csharp
Task<List<DocumentChunk>> SearchByEmbeddingAsync(
    string query, 
    List<DocumentChunk> allChunks, 
    int maxResults
)
```

**Parametreler:**
- `query` (string): Arama sorgusu
- `allChunks` (List<DocumentChunk>): Tüm mevcut doküman chunk'ları
- `maxResults` (int): Döndürülecek maksimum sonuç sayısı

**Döndürür:** İlgili skora göre sıralanmış ilgili doküman chunk'ları listesi


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

