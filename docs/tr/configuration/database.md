---
layout: default
title: Veritabanı Yapılandırması
description: SmartRAG veritabanı yapılandırması - çoklu veritabanı bağlantıları, şema analizi ve güvenlik ayarları
lang: tr
---

## Veritabanı Yapılandırması

SmartRAG çoklu veritabanı desteği ile akıllı çapraz-veritabanı sorguları yapabilir:

---

## Çoklu Veritabanı Bağlantıları

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.DatabaseConnections = new List<DatabaseConnectionConfig>
    {
        new DatabaseConnectionConfig
        {
            Name = "Satış Veritabanı",
            Type = DatabaseType.SqlServer,
            ConnectionString = "Server=localhost;Database=Sales;...",
            IncludedTables = new List<string> { "Orders", "Customers" },
            MaxRowsPerTable = 1000,
            SanitizeSensitiveData = true
        },
        new DatabaseConnectionConfig
        {
            Name = "Envanter Veritabanı",
            Type = DatabaseType.MySQL,
            ConnectionString = "Server=localhost;Database=Inventory;...",
            MaxRowsPerTable = 1000
        }
    };
    
    options.EnableAutoSchemaAnalysis = true;
    options.EnablePeriodicSchemaRefresh = true;
    options.DefaultSchemaRefreshIntervalMinutes = 60;
});
```

---

## DatabaseConfig Parametreleri

| Parametre | Tip | Varsayılan | Açıklama |
|-----------|------|---------|-------------|
| `Name` | `string` | - | Veritabanı bağlantısı için kolay ad |
| `Type` | `DatabaseType` | - | Veritabanı tipi (SqlServer, MySql, PostgreSql, Sqlite) |
| `ConnectionString` | `string` | - | Veritabanı bağlantı dizesi |
| `IncludedTables` | `List<string>` | `[]` | Dahil edilecek spesifik tablolar (boş = tüm tablolar) |
| `ExcludedTables` | `List<string>` | `[]` | Analizden hariç tutulacak tablolar |
| `MaxRowsPerTable` | `int` | `1000` | Tablo başına çıkarılacak maksimum satır |
| `SanitizeSensitiveData` | `bool` | `true` | Hassas verileri otomatik olarak temizle (SSN, kredi kartı vb.) |
| `SchemaRefreshIntervalMinutes` | `int` | `60` | Bu veritabanı için şema yenileme aralığı (0 = varsayılan) |

---

## Desteklenen Veritabanları

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

---

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

---

## Şema Analizi ve Yenileme

### Otomatik Şema Analizi

```csharp
options.EnableAutoSchemaAnalysis = true;  // Başlangıçta şemaları analiz et
options.EnablePeriodicSchemaRefresh = true;  // Periyodik olarak yenile
options.DefaultSchemaRefreshIntervalMinutes = 60;  // 60 dakikada bir yenile
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

---

## Çapraz Veritabanı Sorgu Örnekleri

SmartRAG çoklu veritabanından veri çekerek akıllı sorgular yapabilir:

### Örnek 1: Satış ve Envanter Analizi
```
"Son 3 ayda hangi ürünlerin stoku tükendi ve bunların satış performansı nasıl?"
```

### Örnek 2: Müşteri ve Sipariş Analizi
```
"En çok sipariş veren müşterilerin demografik bilgileri neler?"
```

### Örnek 3: Finansal Raporlama
```
"Muhasebe ve satış veritabanlarından bu ayın gelir raporunu oluştur"
```

---

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

---

## Hata Yönetimi

```csharp
// Bağlantı hatalarını yönetme
try
{
    var result = await _multiDatabaseQueryCoordinator.QueryAsync(query);
}
catch (DatabaseConnectionException ex)
{
    _logger.LogError(ex, "Veritabanı bağlantı hatası: {DatabaseName}", ex.DatabaseName);
    // Fallback stratejisi uygula
}
```

---

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-microphone"></i>
            </div>
            <h3>Ses & OCR</h3>
            <p>Google Speech-to-Text ve Tesseract OCR yapılandırması</p>
            <a href="{{ site.baseurl }}/tr/configuration/audio-ocr" class="btn btn-outline-primary btn-sm mt-3">
                Ses & OCR
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
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
