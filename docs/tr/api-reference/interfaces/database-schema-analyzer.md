---
layout: default
title: IDatabaseSchemaAnalyzer
description: IDatabaseSchemaAnalyzer arayüz dokümantasyonu
lang: tr
---

## IDatabaseSchemaAnalyzer

**Amaç:** Veritabanı şemalarını analiz eder ve akıllı metadata oluşturur

**Namespace:** `SmartRAG.Interfaces.Database`

Tabloları, sütunları, ilişkileri içeren kapsamlı şema bilgisini çıkarır ve AI destekli özetler oluşturur.

#### Metodlar

##### AnalyzeDatabaseSchemaAsync

Bir veritabanı bağlantısını analiz eder ve kapsamlı şema bilgisi çıkarır.

```csharp
Task<DatabaseSchemaInfo> AnalyzeDatabaseSchemaAsync(
    DatabaseConnectionConfig connectionConfig
)
```

**Parametreler:**
- `connectionConfig` (DatabaseConnectionConfig): Veritabanı bağlantı konfigürasyonu

**Döndürür:** Tablolar, sütunlar, foreign key'ler ve AI özetler dahil tam `DatabaseSchemaInfo`

**Örnek:**

```csharp
var config = new DatabaseConnectionConfig
{
    ConnectionString = "Server=localhost;Database=Northwind;Trusted_Connection=true;",
    DatabaseType = DatabaseType.SqlServer
};

var schemaInfo = await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(config);

Console.WriteLine($"Veritabanı: {schemaInfo.DatabaseName}");
Console.WriteLine($"Tablo Sayısı: {schemaInfo.Tables.Count}");
Console.WriteLine($"AI Özeti: {schemaInfo.AISummary}");
```

##### RefreshSchemaAsync

Belirli bir veritabanı için şema bilgilerini yeniler.

```csharp
Task<DatabaseSchemaInfo> RefreshSchemaAsync(string databaseId)
```

**Parametreler:**
- `databaseId` (string): Veritabanı tanımlayıcısı

**Dönen Değer:** Güncellenmiş şema bilgisi

##### GetAllSchemasAsync

Tüm analiz edilmiş veritabanı şemalarını alır.

```csharp
Task<List<DatabaseSchemaInfo>> GetAllSchemasAsync()
```

**Dönen Değer:** Bellekte bulunan tüm veritabanı şemalarının listesi

##### GetSchemaAsync

Belirli bir veritabanı için şemayı alır.

```csharp
Task<DatabaseSchemaInfo> GetSchemaAsync(string databaseId)
```

**Parametreler:**
- `databaseId` (string): Veritabanı tanımlayıcısı

**Dönen Değer:** Veritabanı şema bilgisi veya bulunamazsa null

##### GetSchemasNeedingRefreshAsync

Yapılandırılmış aralıklara göre herhangi bir şemanın yenilenmesi gerekip gerekmediğini kontrol eder.

```csharp
Task<List<string>> GetSchemasNeedingRefreshAsync()
```

**Dönen Değer:** Şema yenilemesi gereken veritabanı ID'lerinin listesi

**Örnek:**

```csharp
var databasesNeedingRefresh = await _schemaAnalyzer.GetSchemasNeedingRefreshAsync();

if (databasesNeedingRefresh.Any())
{
    Console.WriteLine($"Yenilenmesi gereken veritabanları: {string.Join(", ", databasesNeedingRefresh)}");
    
    foreach (var dbId in databasesNeedingRefresh)
    {
        await _schemaAnalyzer.RefreshSchemaAsync(dbId);
        Console.WriteLine($"{dbId} şeması yenilendi");
    }
}
```

##### GenerateAISummaryAsync

Veritabanı şeması için AI destekli özet oluşturur.

```csharp
Task<string> GenerateAISummaryAsync(DatabaseSchemaInfo schemaInfo)
```

**Parametreler:**
- `schemaInfo` (DatabaseSchemaInfo): Şema bilgisi

**Dönen Değer:** AI tarafından oluşturulan şema özeti


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

