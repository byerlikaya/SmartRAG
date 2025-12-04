---
layout: default
title: Hızlı Örnekler
description: Hızlı başlamak için basit, pratik örnekler
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

### 2. Arama Seçenekleri ve Bayrak Tabanlı Filtreleme

`SearchOptions` kullanarak hangi veri kaynaklarında arama yapılacağını kontrol edin:

```csharp
public class IntelligenceController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] QuestionRequest request)
    {
        // Seçenek 1: SearchOptions'ı doğrudan kullan
        var options = new SearchOptions
        {
            EnableDatabaseSearch = true,
            EnableDocumentSearch = false,
            EnableAudioSearch = false,
            EnableImageSearch = false,
            PreferredLanguage = "tr"
        };
        
        var response = await _searchService.QueryIntelligenceAsync(
            request.Question,
            maxResults: 5,
            options: options
        );
        
        return Ok(response);
    }
    
    [HttpPost("ask-with-flags")]
    public async Task<IActionResult> AskWithFlags([FromBody] string query)
    {
        // Seçenek 2: Sorgu string'inden bayrakları ayrıştır
        var searchOptions = ParseSearchOptions(query, out string cleanQuery);
        
        var response = await _searchService.QueryIntelligenceAsync(
            cleanQuery,
            maxResults: 5,
            options: searchOptions
        );
        
        return Ok(response);
    }
    
    private SearchOptions? ParseSearchOptions(string input, out string cleanQuery)
    {
        cleanQuery = input;
        
        var hasDocumentFlag = input.Contains("-d ", StringComparison.OrdinalIgnoreCase) 
            || input.EndsWith("-d", StringComparison.OrdinalIgnoreCase);
        var hasDatabaseFlag = input.Contains("-db ", StringComparison.OrdinalIgnoreCase) 
            || input.EndsWith("-db", StringComparison.OrdinalIgnoreCase);
        var hasAudioFlag = input.Contains("-a ", StringComparison.OrdinalIgnoreCase) 
            || input.EndsWith("-a", StringComparison.OrdinalIgnoreCase);
        var hasImageFlag = input.Contains("-i ", StringComparison.OrdinalIgnoreCase) 
            || input.EndsWith("-i", StringComparison.OrdinalIgnoreCase);
        
        if (!hasDocumentFlag && !hasDatabaseFlag && !hasAudioFlag && !hasImageFlag)
        {
            return null; // Varsayılan seçenekleri kullan
        }
        
        var options = new SearchOptions
        {
            EnableDocumentSearch = hasDocumentFlag,
            EnableDatabaseSearch = hasDatabaseFlag,
            EnableAudioSearch = hasAudioFlag,
            EnableImageSearch = hasImageFlag
        };
        
        // Sorgudan bayrakları kaldır
        var parts = input.Split(' ');
        var cleanParts = parts.Where(p => 
            !p.Equals("-d", StringComparison.OrdinalIgnoreCase) && 
            !p.Equals("-db", StringComparison.OrdinalIgnoreCase) && 
            !p.Equals("-a", StringComparison.OrdinalIgnoreCase) && 
            !p.Equals("-i", StringComparison.OrdinalIgnoreCase));
            
        cleanQuery = string.Join(" ", cleanParts);
        
        return options;
    }
}
```

**Bayrak Örnekleri:**
- `"-db En iyi müşterileri göster"` → Sadece veritabanı araması
- `"-a Toplantıda ne konuşuldu?"` → Sadece ses araması
- `"-i Görselde ne yazıyor?"` → Sadece görüntü OCR araması
- `"-db -a Müşterileri ve toplantı notlarını göster"` → Veritabanı + ses araması
- `"Bayrak olmadan normal sorgu"` → Tüm arama tipleri etkin (varsayılan)

---

### 3. Çoklu Veritabanı Sorgusu

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

### 7. Dosya İzleyici - Otomatik Belge İndeksleme

Klasörleri izleyin ve yeni belgeleri otomatik olarak indeksleyin:

```csharp
// appsettings.json
{
  "SmartRAG": {
    "EnableFileWatcher": true,
    "WatchedFolders": [
      {
        "FolderPath": "C:\\Belgeler\\Gelen",
        "FileExtensions": [".pdf", ".docx", ".txt"],
        "Recursive": true,
        "AutoUpload": true
      }
    ]
  }
}

// FileWatcherService otomatik olarak:
// 1. Belirtilen klasörü yeni dosyalar için izler
// 2. Dosya hash'i kullanarak çiftleri tespit eder
// 3. Yeni belgeleri otomatik olarak yükler ve indeksler
// 4. Dosyalar eklendikçe gerçek zamanlı olarak işler
```

**Özellikler:**
- MD5 dosya hash'i kullanarak otomatik çift tespiti
- Gerçek zamanlı dosya izleme
- Özyinelemeli klasör taraması
- Yapılandırılabilir dosya uzantıları
- Başlangıçta mevcut dosyaların ilk taraması

---

## İlgili Örnekler

- [Örnekler Ana Sayfası]({{ site.baseurl }}/tr/examples) - Örnekler kategorilerine dön
