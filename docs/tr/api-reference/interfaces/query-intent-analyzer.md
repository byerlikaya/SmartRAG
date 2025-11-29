---
layout: default
title: IQueryIntentAnalyzer
description: IQueryIntentAnalyzer arayüz dokümantasyonu
lang: tr
---

## IQueryIntentAnalyzer

**Amaç:** Veritabanı yönlendirmesi için sorgu niyet analizi

**Namespace:** `SmartRAG.Interfaces.Database`

Veritabanı yönlendirme stratejisini belirlemek için sorguları analiz eder.

#### Metodlar

```csharp
Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery);
```


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

