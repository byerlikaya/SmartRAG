---
layout: default
title: IQdrantCacheManager
description: IQdrantCacheManager arayüz dokümantasyonu
lang: tr
---

## IQdrantCacheManager

**Amaç:** Qdrant işlemlerinde arama sonuçlarını önbelleğe almak için arayüz

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Performans optimizasyonu için arama sonuçlarını önbelleğe alma.

#### Metodlar

```csharp
List<DocumentChunk> GetCachedResults(string queryHash);
void CacheResults(string queryHash, List<DocumentChunk> results);
void CleanupExpiredCache();
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

