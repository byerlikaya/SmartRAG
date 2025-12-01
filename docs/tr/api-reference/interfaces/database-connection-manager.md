---
layout: default
title: IDatabaseConnectionManager
description: IDatabaseConnectionManager arayüz dokümantasyonu
lang: tr
---

## IDatabaseConnectionManager

**Amaç:** Konfigürasyondan veritabanı bağlantılarını yönetir

**Namespace:** `SmartRAG.Interfaces.Database`

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

**Döndürür:** Bağlantı yapılandırması veya bulunamazsa null

##### ValidateAllConnectionsAsync

Konfigüre edilmiş tüm bağlantıları doğrular.

```csharp
Task<Dictionary<string, bool>> ValidateAllConnectionsAsync()
```

##### ValidateConnectionAsync

Belirli bir bağlantıyı doğrular.

```csharp
Task<bool> ValidateConnectionAsync(string databaseId)
```

**Parametreler:**
- `databaseId` (string): Veritabanı tanımlayıcısı

**Döndürür:** Bağlantı geçerliyse true, aksi takdirde false

##### GetDatabaseIdAsync

Bağlantıdan veritabanı ID'sini alır (Name belirtilmemişse otomatik oluşturur).

```csharp
Task<string> GetDatabaseIdAsync(DatabaseConnectionConfig connectionConfig)
```

**Parametreler:**
- `connectionConfig` (DatabaseConnectionConfig): Bağlantı yapılandırması

**Döndürür:** Benzersiz veritabanı tanımlayıcısı


## İlgili Arayüzler

- [Gelişmiş Arayüzler]({{ site.baseurl }}/tr/api-reference/advanced) - Tüm gelişmiş arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

