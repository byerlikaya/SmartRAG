---
layout: default
title: IResultMerger
description: IResultMerger arayüz dokümantasyonu
lang: tr
---

## IResultMerger

**Amaç:** Birden fazla veritabanından sonuçları birleştirme

**Namespace:** `SmartRAG.Interfaces.Database`

AI destekli sonuç birleştirme.

#### Metodlar

```csharp
Task<string> MergeResultsAsync(MultiDatabaseQueryResult queryResult, string userQuery);
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

