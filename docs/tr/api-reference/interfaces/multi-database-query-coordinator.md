---
layout: default
title: IMultiDatabaseQueryCoordinator
description: IMultiDatabaseQueryCoordinator arayüz dokümantasyonu
lang: tr
---

## IMultiDatabaseQueryCoordinator

**Amaç:** AI kullanarak çoklu veritabanı sorgularını koordine eder

**Namespace:** `SmartRAG.Interfaces.Database`

Bu interface, doğal dil kullanarak birden fazla veritabanına aynı anda sorgu yapmayı sağlar. AI sorguyu analiz eder, hangi veritabanları ve tablolara erişileceğini belirler, optimize edilmiş SQL sorguları oluşturur ve sonuçları tutarlı bir yanıt halinde birleştirir.

#### Metodlar

##### QueryMultipleDatabasesAsync

Tam bir akıllı sorguyu çalıştırır: intent analizi + yürütme + sonuç birleştirme.

```csharp
Task<RagResponse> QueryMultipleDatabasesAsync(
    string userQuery, 
    int maxResults = 5
)
```

**Parametreler:**
- `userQuery` (string): Doğal dil kullanıcı sorgusu
- `maxResults` (int): Döndürülecek maksimum sonuç sayısı (varsayılan: 5)

**Döndürür:** Birden fazla veritabanından verilerle AI üretilmiş yanıt içeren `RagResponse`

**Örnek:**

```csharp
var response = await _coordinator.QueryMultipleDatabasesAsync(
    "TableA kayıtlarını ve bunların Database1'den gelen TableB detaylarını göster"
);

Console.WriteLine(response.Answer);
// Birden fazla veritabanından gelen veriler birleştirilmiş AI cevabı
```

##### AnalyzeQueryIntentAsync

Kullanıcı sorgusunu analiz eder ve hangi veritabanları/tabloları sorgulayacağını belirler.

```csharp
Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery)
```

**Parametreler:**
- `userQuery` (string): Doğal dil kullanıcı sorgusu

**Dönen Değer:** `QueryIntent` veritabanı yönlendirme bilgileri ile

**Örnek:**

```csharp
var intent = await _coordinator.AnalyzeQueryIntentAsync(
    "Database1 ve Database2 arasındaki verileri karşılaştır"
);

Console.WriteLine($"Güven: {intent.Confidence}");
Console.WriteLine($"Cross-DB Join Gerekiyor: {intent.RequiresCrossDatabaseJoin}");

foreach (var dbQuery in intent.DatabaseQueries)
{
    Console.WriteLine($"Veritabanı: {dbQuery.DatabaseName}");
    Console.WriteLine($"Tablolar: {string.Join(", ", dbQuery.RequiredTables)}");
}
```

##### ExecuteMultiDatabaseQueryAsync

Sorgu intent'ine göre birden fazla veritabanında sorguları çalıştırır.

```csharp
Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(
    QueryIntent queryIntent
)
```

**Parametreler:**
- `queryIntent` (QueryIntent): Analiz edilmiş sorgu intent'i

**Dönen Değer:** `MultiDatabaseQueryResult` tüm veritabanlarından birleştirilmiş sonuçlarla

##### GenerateDatabaseQueriesAsync

Intent'e göre her veritabanı için optimize edilmiş SQL sorguları oluşturur.

```csharp
Task<List<DatabaseQuery>> GenerateDatabaseQueriesAsync(
    QueryIntent queryIntent
)
```

**Parametreler:**
- `queryIntent` (QueryIntent): Analiz edilmiş sorgu intent'i

**Dönen Değer:** `List<DatabaseQuery>` her veritabanı için SQL sorguları

##### MergeResultsAsync

Birden fazla veritabanından gelen sonuçları birleştirir.

```csharp
Task<MultiDatabaseQueryResult> MergeResultsAsync(
    List<DatabaseQueryResult> results
)
```

**Parametreler:**
- `results` (List<DatabaseQueryResult>): Veritabanı sorgu sonuçları

**Dönen Değer:** `MultiDatabaseQueryResult` birleştirilmiş sonuçlar

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Generic Sorgu Örnekleri</h4>
    <p class="mb-0">
        Tüm örnekler <strong>generic placeholder</strong> isimler kullanır (TableA, TableB, Database1).
        Asla domain-specific isimler kullanılmaz (Products, Orders, Customers gibi).
    </p>
</div>


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

