---
layout: default
title: IDatabaseQueryExecutor
description: IDatabaseQueryExecutor arayüz dokümantasyonu
lang: tr
---

## IDatabaseQueryExecutor

**Amaç:** Birden fazla veritabanında sorgu yürütme

**Namespace:** `SmartRAG.Interfaces.Database`

Veritabanları arasında paralel sorgu yürütme.

#### Metodlar

```csharp
Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(QueryIntent queryIntent);
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

