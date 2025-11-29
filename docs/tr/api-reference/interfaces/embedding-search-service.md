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

```csharp
Task<List<DocumentChunk>> SearchByEmbeddingAsync(List<float> queryEmbedding, int maxResults = 5);
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

