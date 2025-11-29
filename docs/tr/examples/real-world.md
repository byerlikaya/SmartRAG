---
layout: default
title: Gerçek Dünya Kullanım Senaryoları
description: Çeşitli endüstrilerden production-ready örnekler
lang: tr
---

## Gerçek Dünya Kullanım Senaryoları

### 1. Tıbbi Kayıt Zekası Sistemi

Hasta verilerini birden fazla sistemde birleştirin:

```csharp
// Birden fazla veritabanına bağlan
var postgresConfig = new DatabaseConfig
{
    Name = "Hasta Kayıtları",
    Type = DatabaseType.PostgreSql,
    ConnectionString = "Host=localhost;Database=Hastane;...",
    IncludedTables = new List<string> { "Hastalar", "Kabuller", "Taburcular" }
};

await _databaseService.ParseDatabaseConnectionAsync(postgresConfig.ConnectionString, postgresConfig);

// Excel lab sonuçlarını yükle
await _documentService.UploadDocumentAsync(labSonuclari, "labs.xlsx", "application/vnd.ms-excel", "lab-teknisyeni");

// Taranmış reçeteleri yükle (OCR)
await _documentService.UploadDocumentAsync(receteGorsel, "recete.jpg", "image/jpeg", "doktor", language: "tur");

// Doktor ses notlarını yükle (Ses transkripsiyonu)
await _documentService.UploadDocumentAsync(sesAkis, "notlar.mp3", "audio/mpeg", "doktor", language: "tr");

// Tüm veri kaynaklarında sorgula
var response = await _searchService.QueryIntelligenceAsync(
    "Ayşe Yılmaz'ın son bir yıl için tam tıbbi geçmişini göster"
);

// AI birleştirir: PostgreSQL + Excel + OCR + Ses → Bağlantısız sistemlerden tam hasta zaman çizelgesi
```

**Güç:** 4 veri kaynağı birleştirildi (PostgreSQL + Excel + OCR + Ses) → Bağlantısız sistemlerden tam hasta zaman çizelgesi.

---

### 2. Bankacılık Kredi Limit Değerlendirmesi

Kapsamlı finansal profil analizi:

```csharp
// 4 farklı veritabanına bağlan
var sqlServerConfig = new DatabaseConfig
{
    Type = DatabaseType.SqlServer,
    ConnectionString = "...",
    IncludedTables = new List<string> { "Islemler", "FaturaOdemeleri", "MaasYatirmalari" }
};

var mySqlConfig = new DatabaseConfig
{
    Type = DatabaseType.MySQL,
    ConnectionString = "...",
    IncludedTables = new List<string> { "KrediKartlari", "Harcamalar", "OdemeGecmisi" }
};

// OCR taranmış dokümanları yükle
await _documentService.UploadDocumentAsync(vergiGorsel, "vergi.jpg", "image/jpeg", "rm", language: "tur");

// PDF hesap özetlerini yükle
await _documentService.UploadDocumentAsync(ozetPdf, "ozet.pdf", "application/pdf", "rm");

// Kapsamlı sorgu
var response = await _multiDbCoordinator.QueryMultipleDatabasesAsync(
    "Mehmet Kaya'nın kredi kartı limitini 8K'dan 18K'ya çıkarmalı mıyız?"
);

// AI analiz eder: 36 ay işlemler + kredi davranışı + varlıklar + ziyaret geçmişi + OCR dokümanlar + PDF'ler
```

**Güç:** 6 veri kaynağı koordine edildi (4 veritabanı + OCR + PDF) → Risksiz kararlar için 360° finansal zeka.

---

### 3. Hukuki İçtihat Keşif Motoru

Dava geçmişinden kazanma stratejileri bulun:

```csharp
// 1.000+ hukuki PDF'leri yükle
foreach (var hukukiDok in hukukiDokumanlar)
{
    await _documentService.UploadDocumentAsync(
        hukukiDok.Stream,
        hukukiDok.FileName,
        "application/pdf",
        "hukuk-ekibi"
    );
}

// Dava veritabanına bağlan
var davaDbConfig = new DatabaseConfig
{
    Type = DatabaseType.SqlServer,
    ConnectionString = "...",
    IncludedTables = new List<string> { "Davalar", "Sonuclar", "Hakimler", "Musteriler" }
};

await _databaseService.ParseDatabaseConnectionAsync(davaDbConfig.ConnectionString, davaDbConfig);

// OCR taranmış mahkeme kararlarını yükle
await _documentService.UploadDocumentAsync(mahkemeKarariGorsel, "karar.jpg", "image/jpeg", "katip", language: "tur");

// Kazanma desenleri için sorgula
var response = await _searchService.QueryIntelligenceAsync(
    "Son 5 yılda sözleşme uyuşmazlığı davalarımızı hangi argümanlar kazandırdı?"
);

// AI, manuel olarak haftalarca sürecek 1.000+ davadan desenleri keşfeder
```

**Güç:** 1.000+ PDF + SQL Server + OCR → AI, dakikalar içinde kazanan hukuki desenleri keşfeder.

---

### 4. Öngörücü Envanter Zekası

Çapraz veritabanı analitik ile stok tükenmelerini önleyin:

```csharp
// 4 veritabanı ile yapılandırmayı ayarla
builder.Services.AddSmartRag(configuration, options =>
{
    options.DatabaseConnections = new List<DatabaseConnectionConfig>
    {
        new() { Name = "Katalog", Type = DatabaseType.Sqlite, ConnectionString = "Data Source=./katalog.db" },
        new() { Name = "Satislar", Type = DatabaseType.SqlServer, ConnectionString = "..." },
        new() { Name = "Envanter", Type = DatabaseType.MySQL, ConnectionString = "..." },
        new() { Name = "Tedarikciler", Type = DatabaseType.PostgreSql, ConnectionString = "..." }
    };
});

// Tüm veritabanlarında sorgula
var response = await _multiDbCoordinator.QueryMultipleDatabasesAsync(
    "Önümüzdeki 2 hafta içinde hangi ürünlerin stoku tükenecek?"
);

// AI koordine eder: SQLite (10K SKU) + SQL Server (2M işlem) + 
//                  MySQL (gerçek zamanlı stok) + PostgreSQL (tedarikçi teslimat süreleri)
// Sonuç: Tek-DB sorguları ile imkansız stok tükenmelerini önleyen öngörücü analitik
```

**Güç:** 4 veritabanı koordine edildi → Tek veritabanı sorguları ile imkansız çapraz-veritabanı öngörücü analitik.

---

### 5. Üretim Kök Neden Analizi

Üretim kalite sorunlarını bulun:

```csharp
// Excel üretim raporlarını yükle
await _documentService.UploadDocumentAsync(
    excelStream,
    "uretim-raporu.xlsx",
    "application/vnd.ms-excel",
    "kalite-muduru"
);

// PostgreSQL sensör veritabanına bağlan
var sensorConfig = new DatabaseConfig
{
    Type = DatabaseType.PostgreSql,
    ConnectionString = "...",
    IncludedTables = new List<string> { "SensorOkumalari", "MakinaDurumu" },
    MaxRowsPerTable = 100000  // Büyük sensör verisi
};

await _databaseService.ParseDatabaseConnectionAsync(sensorConfig.ConnectionString, sensorConfig);

// OCR kalite kontrol fotoğraflarını yükle
await _documentService.UploadDocumentAsync(
    fotoStream,
    "hata-foto.jpg",
    "image/jpeg",
    "kontrolor",
    language: "tur"
);

// PDF bakım kayıtlarını yükle
await _documentService.UploadDocumentAsync(
    bakimPdf,
    "bakim.pdf",
    "application/pdf",
    "teknisyen"
);

// Kök neden analizi sorgusu
var response = await _searchService.QueryIntelligenceAsync(
    "Geçen haftaki üretim partisinde 47 hata olmasının nedeni neydi?"
);

// AI ilişkilendirir: Excel raporları + PostgreSQL 100K sensör okuması + OCR fotoları + PDF kayıtlar
```

**Güç:** 4 veri kaynağı birleştirildi → AI, milyonlarca veri noktası arasında hatalara neden olan sıcaklık anomalilerini bulur.

---

### 6. AI Özgeçmiş Tarama

Yüzlerce adayı verimli bir şekilde tarayın:

```csharp
// 500+ özgeçmiş PDF'lerini yükle
foreach (var ozgecmis in ozgecmisDosyalari)
{
    await _documentService.UploadDocumentAsync(
        ozgecmis.Stream,
        ozgecmis.FileName,
        "application/pdf",
        "ik-ekibi"
    );
}

// Başvuru veritabanına bağlan
var basv uruDbConfig = new DatabaseConfig
{
    Type = DatabaseType.SqlServer,
    ConnectionString = "...",
    IncludedTables = new List<string> { "Basvurular", "Beceriler", "Deneyim", "Egitim" }
};

await _databaseService.ParseDatabaseConnectionAsync(basvuruDbConfig.ConnectionString, basvuruDbConfig);

// OCR taranmış sertifikaları yükle
await _documentService.UploadDocumentAsync(
    sertifikaGorsel,
    "aws-sertifika.jpg",
    "image/jpeg",
    "ik-ekibi",
    language: "eng"
);

// Ses müla kat transkriptlerini yükle
await _documentService.UploadDocumentAsync(
    mulakatSes,
    "mulakat.mp3",
    "audio/mpeg",
    "ik-ekibi",
    language: "tr"
);

// En iyi adayları bul
var response = await _searchService.QueryIntelligenceAsync(
    "Python becerileri ve AWS sertifikaları olan senior React geliştiricilerini bul"
);

// AI tarar: 500+ PDF + SQL Server + OCR sertifikaları + Ses mülakatları
```

**Güç:** 4 veri kaynağı birleştirildi → AI, dakikalar içinde adayları tarar ve sıralar (günler yerine).

### 7. Finansal Denetim Otomasyonu

```csharp
// Denetim sürecini otomatikleştir
var auditQuery = await _searchService.QueryIntelligenceAsync(
    "Son 3 aydaki tüm finansal işlemleri analiz et ve şüpheli aktiviteleri tespit et"
);

// Çoklu veritabanı sorgusu
var multiDbResponse = await _multiDbCoordinator.QueryMultipleDatabasesAsync(
    "Hesap bakiyelerini, işlem geçmişini ve müşteri profillerini karşılaştır"
);

Console.WriteLine($"Denetim Raporu: {auditQuery.Answer}");
Console.WriteLine($"Şüpheli İşlemler: {multiDbResponse.Results.Count}");
```

**Güç:** 3 veritabanı birleştirildi → AI, saatler içinde denetim raporu oluşturur (haftalar yerine).

### 8. Akıllı Devlet Hizmetleri

```csharp
// Vatandaş sorgularını otomatik yanıtla
var citizenQuery = await _searchService.QueryIntelligenceAsync(
    "Emeklilik başvurusu için hangi belgeler gerekli?"
);

// Çoklu kaynak arama
var response = await _searchService.QueryIntelligenceAsync(
    "Vergi indirimi nasıl alabilirim?",
    maxResults: 10
);

Console.WriteLine($"Vatandaş Yanıtı: {citizenQuery.Answer}");
Console.WriteLine($"Kaynak Sayısı: {response.Sources.Count}");
```

**Güç:** 5 veri kaynağı birleştirildi → AI, dakikalar içinde vatandaş sorgularını yanıtlar (günler yerine).

---

## İlgili Örnekler

- [Örnekler Ana Sayfası]({{ site.baseurl }}/tr/examples) - Örnekler kategorilerine dön
