---
layout: default
title: IDatabaseConnectionManager
description: IDatabaseConnectionManager arayüz dokümantasyonu
lang: tr
---

## IDatabaseConnectionManager

**Amaç:** Konfigürasyondan veritabanı bağlantılarını yönetir

**Namespace:** `SmartRAG.Interfaces.Document`

Veritabanı bağlantı yaşam döngüsü, doğrulama ve runtime yönetimini ele alır.

#### Metodlar

##### InitializeAsync

Konfigürasyondan tüm veritabanı bağlantılarını başlatır.

```csharp
Task InitializeAsync()
```

**Örnek:**

```csharp
await _connectionManager.InitializeAsync();
Console.WriteLine("Tüm veritabanı bağlantıları başlatıldı");
```

##### GetAllConnectionsAsync

Konfigüre edilmiş tüm veritabanı bağlantılarını alır.

```csharp
Task<List<DatabaseConnectionConfig>> GetAllConnectionsAsync()
```

**Döndürür:** Tüm veritabanı bağlantı konfigürasyonları listesi

##### GetConnectionAsync

ID'ye göre belirli bir bağlantıyı alır.

```csharp
Task<DatabaseConnectionConfig> GetConnectionAsync(string databaseId)
```

**Parametreler:**
- `databaseId` (string): Veritabanı tanımlayıcısı

**Dönen Değer:** Bağlantı yapılandırması veya bulunamazsa null

##### ValidateAllConnectionsAsync

Konfigüre edilmiş tüm bağlantıları doğrular.

```csharp
Task<Dictionary<string, bool>> ValidateAllConnectionsAsync()
```

**Döndürür:** Veritabanı ID'leri ve doğrulama durumları sözlüğü

**Örnek:**

```csharp
var validationResults = await _connectionManager.ValidateAllConnectionsAsync();

foreach (var (databaseId, isValid) in validationResults)
{
    Console.WriteLine($"{databaseId}: {(isValid ? "Geçerli" : "Geçersiz")}");
}
```

##### ValidateConnectionAsync

Belirli bir bağlantıyı doğrular.

```csharp
Task<bool> ValidateConnectionAsync(string databaseId)
```

**Parametreler:**
- `databaseId` (string): Veritabanı tanımlayıcısı

**Dönen Değer:** Bağlantı geçerliyse true, aksi takdirde false

##### GetDatabaseIdAsync

Bağlantı dizesinden veritabanı ID'sini alır.

```csharp
string GetDatabaseIdAsync(string connectionString)
```

**Parametreler:**
- `connectionString` (string): Veritabanı bağlantı dizesi

**Dönen Değer:** Veritabanı ID'si

##### AddConnectionAsync

Runtime'da yeni bir bağlantı ekler.

```csharp
Task<bool> AddConnectionAsync(DatabaseConnectionConfig config)
```

**Parametreler:**
- `config` (DatabaseConnectionConfig): Yeni bağlantı yapılandırması

**Dönen Değer:** Başarılıysa true, aksi takdirde false

**Örnek:**

```csharp
var newConfig = new DatabaseConnectionConfig
{
    Name = "NewDatabase",
    ConnectionString = "Server=localhost;Database=NewDb;Trusted_Connection=true;",
    DatabaseType = DatabaseType.SqlServer,
    Description = "Yeni veritabanı",
    Enabled = true
};

bool success = await _connectionManager.AddConnectionAsync(newConfig);
if (success)
{
    Console.WriteLine("Yeni bağlantı eklendi");
}
```

##### RemoveConnectionAsync

Runtime'da bir bağlantıyı kaldırır.

```csharp
Task<bool> RemoveConnectionAsync(string databaseId)
```

**Parametreler:**
- `databaseId` (string): Kaldırılacak veritabanı ID'si

**Dönen Değer:** Başarılıysa true, aksi takdirde false


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

