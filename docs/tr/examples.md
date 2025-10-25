---
layout: default
title: Örnekler
description: SmartRAG için pratik kod örnekleri ve gerçek dünya kullanım senaryoları
lang: tr
---


## Hızlı Örnekler

### 1. Basit Doküman Arama

Bir doküman yükleyin ve arayın:

```csharp
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _searchService;
    
    // Doküman yükleme
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "kullanici-123"
        );
        
        return Ok(new { 
            id = document.Id, 
            fileName = document.FileName,
            chunks = document.Chunks.Count 
        });
    }
    
    // Doküman arama
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        var response = await _searchService.QueryIntelligenceAsync(
            request.Query,
            maxResults: request.MaxResults
        );
        
        return Ok(response);
    }
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 5;
}
```

---

### 2. Çoklu Veritabanı Sorgusu

Aynı anda birden fazla veritabanını sorgulayın:

```csharp
public class DatabaseController : ControllerBase
{
    private readonly IDatabaseParserService _databaseService;
    private readonly IMultiDatabaseQueryCoordinator _multiDbCoordinator;
    
    // SQL Server veritabanına bağlan
    [HttpPost("connect/sqlserver")]
    public async Task<IActionResult> ConnectSqlServer([FromBody] ConnectionRequest request)
    {
        var config = new DatabaseConfig
        {
            Type = DatabaseType.SqlServer,
            ConnectionString = request.ConnectionString,
            IncludedTables = request.Tables,
            MaxRowsPerTable = 1000,
            SanitizeSensitiveData = true
        };
        
        var content = await _databaseService.ParseDatabaseConnectionAsync(
            request.ConnectionString,
            config
        );
        
        return Ok(new { message = "Başarıyla bağlandı", tables = request.Tables });
    }
    
    // MySQL veritabanına bağlan
    [HttpPost("connect/mysql")]
    public async Task<IActionResult> ConnectMySQL([FromBody] ConnectionRequest request)
    {
        var config = new DatabaseConfig
        {
            Type = DatabaseType.MySQL,
            ConnectionString = request.ConnectionString,
            MaxRowsPerTable = 1000
        };
        
        var content = await _databaseService.ParseDatabaseConnectionAsync(
            request.ConnectionString,
            config
        );
        
        return Ok(new { message = "Başarıyla bağlandı" });
    }
    
    // Birden fazla veritabanını sorgula
    [HttpPost("query")]
    public async Task<IActionResult> QueryDatabases([FromBody] MultiDbQueryRequest request)
    {
        var response = await _multiDbCoordinator.QueryMultipleDatabasesAsync(
            request.Query,
            maxResults: request.MaxResults
        );
        
        return Ok(response);
    }
}
```

**Örnek Sorgu:**
```
"SQL Server'dan toplam satışları MySQL'den mevcut envanter seviyeleriyle göster"
```

SmartRAG şunları yapacak:
1. Sorgu amacını analiz eder
2. İlgili veritabanlarını ve tabloları belirler
3. Her veritabanı için uygun SQL üretir
4. Sorguları paralel olarak çalıştırır
5. Sonuçları akıllıca birleştirir
6. Birleşik AI destekli cevap döndürür

---

### 3. OCR Doküman İşleme

Görselleri Tesseract OCR ile işleyin:

```csharp
public class OcrController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _searchService;
    
    // OCR işleme için görsel yükleme
    [HttpPost("upload/image")]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string language = "tur")
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "kullanici-123",
            language: language  // OCR dili: tur, eng, deu, vb.
        );
        
        return Ok(new { 
            id = document.Id,
            extractedText = document.Content,
            confidence = "OCR başarıyla tamamlandı"
        });
    }
    
    // OCR işlenmiş dokümanları sorgula
    [HttpPost("query/image-content")]
    public async Task<IActionResult> QueryImageContent([FromBody] string query)
    {
        var response = await _searchService.QueryIntelligenceAsync(query);
        return Ok(response);
    }
}
```

**Desteklenen Görsel Formatları:**
- JPEG/JPG, PNG, GIF, BMP, TIFF, WebP

**Örnek Kullanım:**
```bash
# Fatura görseli yükle
curl -X POST "http://localhost:5000/api/ocr/upload/image?language=tur" \
  -F "file=@fatura.jpg"

# Sorgu: "Bu faturadaki toplam tutar nedir?"
```

---

### 4. Ses Transkripsiyonu

Ses dosyalarını Whisper.net ile transkribe edin:

```csharp
public class AudioController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _searchService;
    
    // Transkripsiyon için ses yükleme
    [HttpPost("upload/audio")]
    public async Task<IActionResult> UploadAudio(IFormFile file, [FromQuery] string language = "tr")
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "kullanici-123",
            language: language  // Konuşma dili: tr, en, auto, vb.
        );
        
        return Ok(new { 
            id = document.Id,
            transcription = document.Content,
            message = "Ses başarıyla transkribe edildi"
        });
    }
    
    // Transkripsiyon içeriğini sorgula
    [HttpPost("query/audio-content")]
    public async Task<IActionResult> QueryAudioContent([FromBody] string query)
    {
        var response = await _searchService.QueryIntelligenceAsync(query);
        return Ok(response);
    }
}
```

**Desteklenen Ses Formatları:**
- MP3, WAV, M4A, AAC, OGG, FLAC, WMA

**Dil Kodları:**
- `tr` - Türkçe
- `en` - İngilizce
- `de` - Almanca
- `fr` - Fransızca
- `auto` - Otomatik tespit (önerilen)
- 100+ dil desteklenir

<div class="alert alert-success">
    <h4><i class="fas fa-lock me-2"></i> Gizlilik Notu</h4>
    <p class="mb-0">
        Tüm işlem %100 yerel olarak yapılır. Ses transkripsiyonu Whisper.net, OCR Tesseract kullanır. Hiçbir veri harici servislere gönderilmez.
    </p>
</div>

---

### 5. Konuşma Geçmişi

Doğal çok turlu konuşmalar:

```csharp
public class ConversationController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var response = await _searchService.QueryIntelligenceAsync(
            request.Message,
            maxResults: 5,
            startNewConversation: request.StartNew
        );
        
        return Ok(new {
            answer = response.Answer,
            sources = response.Sources.Count,
            timestamp = response.SearchedAt
        });
    }
}
```

**Konuşma Akışı Örneği:**

```
Kullanıcı: "Makine öğrenimi nedir?"
AI: "Makine öğrenimi, yapay zekanın bir alt kümesidir..."

Kullanıcı: "Denetimli öğrenmeyi açıklar mısın?"  // AI bağlamı hatırlar
AI: "Makine öğrenimi hakkındaki önceki tartışmamıza dayanarak, denetimli öğrenme..."

Kullanıcı: "Yaygın algoritmalar nelerdir?"  // Konuşma bağlamını korur
AI: "Yaygın denetimli öğrenme algoritmaları şunlardır..."

Kullanıcı: "/yeni"  // Yeni konuşma başlat
AI: "Yeni konuşma başlatıldı. Size nasıl yardımcı olabilirim?"
```

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

## Gelişmiş Örnekler

### Konuşma Bağlamı Yönetimi

```csharp
// İlk soru
var q1 = await _searchService.QueryIntelligenceAsync(
    "Şirketin iade politikası nedir?"
);

// Takip - AI bağlamı hatırlar
var q2 = await _searchService.QueryIntelligenceAsync(
    "Uluslararası siparişler ne olacak?"
);

// Başka bir takip - tam bağlamı korur
var q3 = await _searchService.QueryIntelligenceAsync(
    "İade nasıl başlatırım?"
);

// Yeni konuşma başlat
var newConv = await _searchService.QueryIntelligenceAsync(
    "Kargo hakkında konuşalım",
    startNewConversation: true
);
```

### Toplu Doküman İşleme

```csharp
// Birden fazla dokümanı aynı anda yükle
var files = new List<(Stream, string, string)>
{
    (File.OpenRead("rapor1.pdf"), "rapor1.pdf", "application/pdf"),
    (File.OpenRead("rapor2.docx"), "rapor2.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
    (File.OpenRead("rapor3.xlsx"), "rapor3.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
};

var documents = await _documentService.UploadDocumentsAsync(files, "user-123");

Console.WriteLine($"Yüklenen doküman sayısı: {documents.Count}");
```

### Özel SQL Çalıştırma

```csharp
// Belirli bir veritabanında özel SQL sorgusu çalıştır
var result = await _databaseService.ExecuteQueryAsync(
    "Server=localhost;Database=Sales;Trusted_Connection=true;",
    "SELECT TOP 10 CustomerID, CompanyName, TotalOrders FROM CustomerSummary ORDER BY TotalOrders DESC",
    DatabaseType.SqlServer,
    maxRows: 10
);

Console.WriteLine($"SQL Sonucu: {result}");
```

### Depolama İstatistikleri

```csharp
// Depolama durumunu kontrol et
var stats = await _documentService.GetStorageStatisticsAsync();

Console.WriteLine($"Toplam Doküman: {stats["TotalDocuments"]}");
Console.WriteLine($"Toplam Parça: {stats["TotalChunks"]}");
Console.WriteLine($"Depolama Boyutu: {stats["StorageSizeMB"]} MB");
Console.WriteLine($"Son Güncelleme: {stats["LastUpdated"]}");
```

### Embedding'leri Yeniden Oluştur

```csharp
// AI provider değiştikten sonra embedding'leri yenile
var success = await _documentService.RegenerateAllEmbeddingsAsync();

if (success)
{
    Console.WriteLine("Tüm embedding'ler başarıyla yenilendi");
}
else
{
    Console.WriteLine("Embedding yenileme başarısız");
}
```

## Test Örnekleri

### Unit Test Örneği

```csharp
[Test]
public async Task QueryIntelligenceAsync_ShouldReturnValidResponse_WhenValidQueryProvided()
{
    // Arrange
    var query = "Test sorgusu";
    var maxResults = 5;
    
    // Act
    var response = await _searchService.QueryIntelligenceAsync(query, maxResults);
    
    // Assert
    Assert.That(response, Is.Not.Null);
    Assert.That(response.Answer, Is.Not.Empty);
    Assert.That(response.Sources, Is.Not.Empty);
    Assert.That(response.Sources.Count, Is.LessThanOrEqualTo(maxResults));
}
```

---

## En İyi Pratikler

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="alert alert-success">
            <h4><i class="fas fa-check-circle me-2"></i> Yapılması Gerekenler</h4>
            <ul class="mb-0">
                <li>Servisler için dependency injection kullanın</li>
                <li>İstisnaları düzgün bir şekilde yönetin</li>
                <li>Async/await'i tutarlı kullanın</li>
                <li>Kullanıcı girdilerini doğrulayın</li>
                <li>Makul maxResults limitleri ayarlayın</li>
                <li>Doğal etkileşimler için konuşma geçmişini kullanın</li>
            </ul>
                </div>
            </div>
    
    <div class="col-md-6">
        <div class="alert alert-warning">
            <h4><i class="fas fa-times-circle me-2"></i> Yapılmaması Gerekenler</h4>
            <ul class="mb-0">
                <li>Async metodlarda .Result veya .Wait() kullanmayın</li>
                <li>API anahtarlarını kaynak kontrolüne commit etmeyin</li>
                <li>Üretimde InMemory depolama kullanmayın</li>
                <li>Hata yönetimini atlamayın</li>
                <li>Satır limitleri olmadan veritabanlarını sorgulamayın</li>
                <li>Hassas veriyi temizlemeden yüklemeyin</li>
                        </ul>
                    </div>
                </div>
            </div>

---

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-rocket"></i>
            </div>
            <h3>Başlangıç</h3>
            <p>Hızlı kurulum ve yapılandırma kılavuzu</p>
            <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-outline-primary btn-sm mt-3">
                Başlayın
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-cog"></i>
            </div>
            <h3>Yapılandırma</h3>
            <p>Eksiksiz yapılandırma referansı</p>
            <a href="{{ site.baseurl }}/tr/configuration" class="btn btn-outline-primary btn-sm mt-3">
                Yapılandır
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-history"></i>
            </div>
            <h3>Changelog</h3>
            <p>Versiyon geçmişi ve güncellemeler</p>
            <a href="{{ site.baseurl }}/tr/changelog" class="btn btn-outline-primary btn-sm mt-3">
                Changelog'u Görüntüle
            </a>
        </div>
    </div>
</div>

