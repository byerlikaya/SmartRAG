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

##### MergeResultsAsync

Birden fazla veritabanından gelen sonuçları tutarlı bir yanıt halinde birleştirir.

```csharp
Task<string> MergeResultsAsync(
    MultiDatabaseQueryResult queryResults, 
    string originalQuery
)
```

**Parametreler:**
- `queryResults` (MultiDatabaseQueryResult): Birden fazla veritabanından gelen sonuçlar
- `originalQuery` (string): Orijinal kullanıcı sorgusu

**Döndürür:** Birleştirilmiş ve sıralanmış sonuçlar formatlanmış string olarak

##### GenerateFinalAnswerAsync

Birleştirilmiş veritabanı sonuçlarından nihai AI cevabı oluşturur.

```csharp
Task<RagResponse> GenerateFinalAnswerAsync(
    string userQuery, 
    string mergedData, 
    MultiDatabaseQueryResult queryResults
)
```

**Parametreler:**
- `userQuery` (string): Orijinal kullanıcı sorgusu
- `mergedData` (string): Veritabanlarından birleştirilmiş veri
- `queryResults` (MultiDatabaseQueryResult): Sorgu sonuçları

**Döndürür:** AI tarafından üretilmiş cevap ile RAG yanıtı


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

