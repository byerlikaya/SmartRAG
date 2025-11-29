---
layout: default
title: Veritabanı Yapılandırması
description: SmartRAG veritabanı yapılandırması - çoklu veritabanı bağlantıları, şema analizi ve güvenlik ayarları
lang: tr
---

## Veritabanı Yapılandırması

SmartRAG çoklu veritabanı desteği ile akıllı çapraz-veritabanı sorguları yapabilir:

## Çoklu Veritabanı Bağlantıları

Çoklu veritabanı bağlantılarını yapılandırarak çapraz-veritabanı sorgularını etkinleştirin:

Veritabanlarını `appsettings.json` dosyasında yapılandırın:

```json
{
  "SmartRAG": {
    "DatabaseConnections": [
      {
        "Name": "Satış Veritabanı",
        "ConnectionString": "Server=localhost;Database=Sales;...",
        "DatabaseType": "SqlServer",
        "IncludedTables": ["Orders", "Customers"],
        "MaxRowsPerQuery": 1000,
        "Enabled": true
      },
      {
        "Name": "Envanter Veritabanı",
        "ConnectionString": "Server=localhost;Database=Inventory;...",
        "DatabaseType": "MySQL",
        "MaxRowsPerQuery": 1000,
        "Enabled": true
      }
    ]
  }
}
```

## DatabaseConnectionConfig Parametreleri

| Parametre | Tip | Varsayılan | Açıklama |
|-----------|------|---------|-------------|
| `Name` | `string` | - | Veritabanı bağlantısı için açıklayıcı isim (sağlanmazsa otomatik oluşturulur) |
| `ConnectionString` | `string` | - | Veritabanı bağlantı string'i (gerekli) |
| `DatabaseType` | `DatabaseType` | - | Veritabanı tipi (SqlServer, MySql, PostgreSql, Sqlite) (gerekli) |
| `Description` | `string` | - | Veritabanı içeriğini anlamaya yardımcı olacak isteğe bağlı açıklama |
| `Enabled` | `bool` | `true` | Bu bağlantının etkin olup olmadığı |
| `MaxRowsPerQuery` | `int` | `0` | Sorgu başına alınacak maksimum satır (0 = varsayılan kullan) |
| `QueryTimeoutSeconds` | `int` | `0` | Sorgu timeout süresi (saniye) (0 = varsayılan kullan) |
| `SchemaRefreshIntervalMinutes` | `int` | `0` | Otomatik yenileme aralığı (dakika) (0 = otomatik yenileme yok) |
| `IncludedTables` | `string[]` | `[]` | Dahil edilecek belirli tablolar (boş = tüm tablolar) |
| `ExcludedTables` | `string[]` | `[]` | Analizden hariç tutulacak tablolar |

## Desteklenen Veritabanları

SmartRAG aşağıdaki veritabanı türlerini destekler:

### SQL Server

```json
{
  "DatabaseConnections": [
    {
      "Name": "SQL Server DB",
      "Type": "SqlServer",
      "ConnectionString": "Server=localhost;Database=MyDb;User Id=user;Password=pass;TrustServerCertificate=true;"
    }
  ]
}
```

### MySQL

```json
{
  "DatabaseConnections": [
    {
      "Name": "MySQL DB",
      "Type": "MySql",
      "ConnectionString": "Server=localhost;Database=MyDb;Uid=user;Pwd=pass;"
    }
  ]
}
```

### PostgreSQL

```json
{
  "DatabaseConnections": [
    {
      "Name": "PostgreSQL DB",
      "Type": "PostgreSql",
      "ConnectionString": "Host=localhost;Database=MyDb;Username=postgres;Password=password;"
    }
  ]
}
```

### SQLite

```json
{
  "DatabaseConnections": [
    {
      "Name": "SQLite DB",
      "Type": "Sqlite",
      "ConnectionString": "Data Source=./mydb.db;"
    }
  ]
}
```

## Güvenlik ve Hassas Veri Temizleme

SmartRAG otomatik olarak hassas veri tiplerini tespit eder ve temizler:

**Otomatik Temizlenen Hassas Veri Tipleri:**
- `password`, `pwd`, `pass`
- `ssn`, `social_security`
- `credit_card`, `creditcard`, `cc_number`
- `email`, `mail`
- `phone`, `telephone`
- `salary`, `compensation`

```csharp
// Hassas veri temizlemeyi devre dışı bırakma
new DatabaseConnectionConfig
{
    Name = "Güvenli Veritabanı",
    Type = DatabaseType.SqlServer,
    ConnectionString = "Server=localhost;Database=SecureDB;...",
    SanitizeSensitiveData = false  // Dikkatli kullanın!
}
```

## Şema Analizi ve Yenileme

### SmartRAG Seçenekleri - Şema Yönetimi

Bu global seçenekler tüm veritabanları için şema analizi davranışını kontrol eder:

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // Başlangıçta veritabanı şemalarını otomatik olarak analiz et
    options.EnableAutoSchemaAnalysis = true;
    
    // Şema değişikliklerini tespit etmek için şemaları periyodik olarak yenile
    options.EnablePeriodicSchemaRefresh = true;
    
    // Tüm veritabanları için varsayılan yenileme aralığı (veritabanı bazında override edilebilir)
    options.DefaultSchemaRefreshIntervalMinutes = 60;
});
```

**appsettings.json konfigürasyonu:**

```json
{
  "SmartRAG": {
    "EnableAutoSchemaAnalysis": true,
    "EnablePeriodicSchemaRefresh": true,
    "DefaultSchemaRefreshIntervalMinutes": 60
  }
}
```

### Manuel Şema Yenileme

```csharp
// Belirli bir veritabanı için özel yenileme aralığı
new DatabaseConnectionConfig
{
    Name = "Sık Değişen Veritabanı",
    Type = DatabaseType.SqlServer,
    ConnectionString = "Server=localhost;Database=DynamicDB;...",
    SchemaRefreshIntervalMinutes = 15  // 15 dakikada bir yenile
}
```

## Performans Optimizasyonu

### Tablo Filtreleme

```csharp
// Sadece belirli tabloları dahil et
new DatabaseConnectionConfig
{
    Name = "Sadece Ana Tablolar",
    Type = DatabaseType.SqlServer,
    ConnectionString = "Server=localhost;Database=LargeDB;...",
    IncludedTables = new List<string> { "Users", "Orders", "Products" },
    ExcludedTables = new List<string> { "Logs", "TempData", "Cache" }
}
```

### Satır Limiti

```csharp
// Büyük tablolar için satır limiti
new DatabaseConnectionConfig
{
    Name = "Büyük Tablo",
    Type = DatabaseType.SqlServer,
    ConnectionString = "Server=localhost;Database=BigDB;...",
    MaxRowsPerTable = 5000  // Tablo başına maksimum 5000 satır
}
```

## Hata Yönetimi

```csharp
// Bağlantı hatalarını yönetme
try
{
    var result = await _multiDatabaseQueryCoordinator.QueryMultipleDatabasesAsync(query);
}
catch (DatabaseConnectionException ex)
{
    _logger.LogError(ex, "Veritabanı bağlantı hatası: {DatabaseName}", ex.DatabaseName);
    // Fallback stratejisi uygula
}
```

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-microphone"></i>
            </div>
            <h3>Ses & OCR</h3>
            <p>Whisper.net ve Tesseract OCR yapılandırması</p>
            <a href="{{ site.baseurl }}/tr/configuration/audio-ocr" class="btn btn-outline-primary btn-sm mt-3">
                Ses & OCR
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-cogs"></i>
            </div>
            <h3>Gelişmiş Yapılandırma</h3>
            <p>Yedek sağlayıcılar ve en iyi pratikler</p>
            <a href="{{ site.baseurl }}/tr/configuration/advanced" class="btn btn-outline-primary btn-sm mt-3">
                Gelişmiş Yapılandırma
            </a>
        </div>
    </div>
</div>
