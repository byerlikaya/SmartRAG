---
layout: default
title: IDatabaseParserService
description: IDatabaseParserService arayüz dokümantasyonu
lang: tr
---

## IDatabaseParserService

**Amaç:** Canlı bağlantılarla evrensel veritabanı desteği

**Namespace:** `SmartRAG.Interfaces.Database`

### Metodlar

#### ParseDatabaseFileAsync

Bir veritabanı dosyasını ayrıştırın (SQLite).

```csharp
Task<string> ParseDatabaseFileAsync(Stream dbStream, string fileName)
```

#### ParseDatabaseConnectionAsync

Canlı veritabanına bağlanın ve içeriği çıkarın.

```csharp
Task<string> ParseDatabaseConnectionAsync(
    string connectionString, 
    DatabaseConfig config
)
```

**Örnek:**

```csharp
var config = new DatabaseConfig
{
    Type = DatabaseType.SqlServer,
    ConnectionString = "Server=localhost;Database=Northwind;Trusted_Connection=true;",
    IncludedTables = new List<string> { "Customers", "Orders", "Products" },
    MaxRowsPerTable = 1000,
    SanitizeSensitiveData = true
};

var content = await _databaseService.ParseDatabaseConnectionAsync(
    config.ConnectionString, 
    config
);
```

#### ExecuteQueryAsync

Özel SQL sorgusu çalıştırın.

```csharp
Task<string> ExecuteQueryAsync(
    string connectionString, 
    string query, 
    DatabaseType databaseType, 
    int maxRows = 1000
)
```

**Örnek:**

```csharp
var result = await _databaseService.ExecuteQueryAsync(
    "Server=localhost;Database=Sales;Trusted_Connection=true;",
    "SELECT TOP 10 CustomerID, CompanyName FROM Customers WHERE Country = 'USA'",
    DatabaseType.SqlServer,
    maxRows: 10
);
```

#### GetTableNamesAsync

Veritabanından tablo isimlerinin listesini alın.

```csharp
Task<List<string>> GetTableNamesAsync(
    string connectionString, 
    DatabaseType databaseType
)
```

#### ValidateConnectionAsync

Veritabanı bağlantısını doğrulayın.

```csharp
Task<bool> ValidateConnectionAsync(
    string connectionString, 
    DatabaseType databaseType
)
```

**Örnek:**

```csharp
bool isValid = await _databaseService.ValidateConnectionAsync(
    "Server=localhost;Database=MyDb;Trusted_Connection=true;",
    DatabaseType.SqlServer
);

if (isValid)
{
    Console.WriteLine("Bağlantı başarılı!");
}
```

#### GetSupportedDatabaseTypes

Desteklenen veritabanı türlerinin listesini alın.

```csharp
IEnumerable<DatabaseType> GetSupportedDatabaseTypes()
```

#### GetSupportedDatabaseFileExtensions

Desteklenen veritabanı dosya uzantılarının listesini alın.

```csharp
IEnumerable<string> GetSupportedDatabaseFileExtensions()
```

**Döndürür:** `.db`, `.sqlite`, `.sqlite3`


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

