---
layout: default
title: IQdrantSearchService
description: IQdrantSearchService arayüz dokümantasyonu
lang: tr
---

## IQdrantSearchService

**Amaç:** Qdrant vektör veritabanında arama yapmak için arayüz

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Qdrant için vektör, metin ve hibrit arama yetenekleri.

#### Metodlar

```csharp
Task<List<DocumentChunk>> SearchAsync(List<float> queryEmbedding, int maxResults);
Task<List<DocumentChunk>> FallbackTextSearchAsync(string query, int maxResults);
Task<List<DocumentChunk>> HybridSearchAsync(string query, int maxResults);
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

