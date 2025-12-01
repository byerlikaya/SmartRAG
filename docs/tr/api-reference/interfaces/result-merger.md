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

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

