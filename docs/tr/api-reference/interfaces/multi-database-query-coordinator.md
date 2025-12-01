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

**Overload 1:** Sorgu intent'ini analiz et ve sorguyu çalıştır.

```csharp
Task<RagResponse> QueryMultipleDatabasesAsync(
    string userQuery, 
    int maxResults = 5
)
```

**Overload 2:** Önceden analiz edilmiş sorgu intent'ini kullanarak sorguyu çalıştır (gereksiz AI çağrılarını önler).

```csharp
Task<RagResponse> QueryMultipleDatabasesAsync(
    string userQuery, 
    QueryIntent preAnalyzedIntent,
    int maxResults = 5
)
```

**Parametreler:**
- `userQuery` (string): Doğal dil kullanıcı sorgusu
- `preAnalyzedIntent` (QueryIntent, isteğe bağlı): Gereksiz AI çağrılarını önlemek için önceden analiz edilmiş sorgu intent'i
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

**Önceden analiz edilmiş intent ile örnek:**

```csharp
// Intent'i bir kez önceden analiz et
var intent = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(userQuery);

// Önceden analiz edilmiş intent'i birden fazla sorgu için kullan
var response1 = await _coordinator.QueryMultipleDatabasesAsync(userQuery, intent);
var response2 = await _coordinator.QueryMultipleDatabasesAsync(userQuery, intent);
```

##### AnalyzeQueryIntentAsync (Kullanımdan Kaldırıldı)

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Kullanımdan Kaldırıldı</h4>
    <p class="mb-0">
        Yerine <code>IQueryIntentAnalyzer.AnalyzeQueryIntentAsync</code> kullanın. Bu metod v4.0.0'da kaldırılacak.
        Geriye dönük uyumluluk için sağlanan eski metod.
    </p>
</div>

Kullanıcı sorgusunu analiz eder ve hangi veritabanları/tabloları sorgulayacağını belirler.

```csharp
[Obsolete("Yerine IQueryIntentAnalyzer.AnalyzeQueryIntentAsync kullanın")]
Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery)
```

**Parametreler:**
- `userQuery` (string): Doğal dil kullanıcı sorgusu

**Döndürür:** `QueryIntent` veritabanı yönlendirme bilgileri ile

**Not:** Bu metod kullanımdan kaldırılmıştır. Yerine `IQueryIntentAnalyzer.AnalyzeQueryIntentAsync` kullanın.

##### ExecuteMultiDatabaseQueryAsync

Sorgu intent'ine göre birden fazla veritabanında sorguları çalıştırır.

```csharp
Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(
    QueryIntent queryIntent
)
```

**Parametreler:**
- `queryIntent` (QueryIntent): Analiz edilmiş sorgu intent'i

**Döndürür:** `MultiDatabaseQueryResult` tüm veritabanlarından birleştirilmiş sonuçlarla

##### GenerateDatabaseQueriesAsync

Intent'e göre her veritabanı için optimize edilmiş SQL sorguları oluşturur.

```csharp
Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent)
```

**Parametreler:**
- `queryIntent` (QueryIntent): SQL oluşturulacak sorgu intent'i

**Döndürür:** Üretilmiş SQL sorguları ile güncellenmiş `QueryIntent`

##### MultiDatabaseQueryResult

Çoklu veritabanı sorgu yürütmesinden sonuçlar.

```csharp
public class MultiDatabaseQueryResult
{
    public Dictionary<string, DatabaseQueryResult> DatabaseResults { get; set; }
    public bool Success { get; set; }
    public List<string> Errors { get; set; }
    public long ExecutionTimeMs { get; set; }
}
```

##### DatabaseQueryResult

Tek bir veritabanı sorgusundan sonuçlar.

```csharp
public class DatabaseQueryResult
{
    public string DatabaseId { get; set; }
    public string DatabaseName { get; set; }
    public string ExecutedQuery { get; set; }
    public string ResultData { get; set; }
    public int RowCount { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }
}
```


## İlgili Arayüzler

- [Gelişmiş Arayüzler]({{ site.baseurl }}/tr/api-reference/advanced) - Tüm gelişmiş arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

