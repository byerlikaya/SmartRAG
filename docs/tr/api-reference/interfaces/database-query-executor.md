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

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

