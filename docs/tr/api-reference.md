---
layout: default
title: API Referans
description: SmartRAG interface'leri, metodları ve modelleri için eksiksiz API dokümantasyonu
lang: tr
---

<div class="container">

## Temel Interface'ler

SmartRAG tüm işlemler için iyi tanımlanmış interface'ler sağlar. Bu interface'leri dependency injection ile enjekte edin.

---

## IDocumentSearchService

**Amaç:** RAG pipeline ve konuşma yönetimi ile AI destekli akıllı sorgu işleme

**Namespace:** `SmartRAG.Interfaces`

### Metodlar

#### QueryIntelligenceAsync

RAG ve otomatik oturum yönetimi ile akıllı sorguları işleyin.

```csharp
Task<RagResponse> QueryIntelligenceAsync(
    string query, 
    int maxResults = 5, 
    bool startNewConversation = false
)
```

**Parametreler:**
- `query` (string): Kullanıcının sorusu veya sorgusu
- `maxResults` (int): Alınacak maksimum doküman parçası sayısı (varsayılan: 5)
- `startNewConversation` (bool): Yeni bir konuşma oturumu başlat (varsayılan: false)

**Döndürür:** AI cevabı, kaynakları ve metadata ile `RagResponse`

**Örnek:**

```csharp
var response = await _searchService.QueryIntelligenceAsync(
    "Ana faydalar nelerdir?", 
    maxResults: 5
);

Console.WriteLine(response.Answer);
// Kaynaklar response.Sources içinde mevcut
```

#### SearchDocumentsAsync

AI cevabı üretmeden dokümanları anlamsal olarak arayın.

```csharp
Task<List<DocumentChunk>> SearchDocumentsAsync(
    string query, 
    int maxResults = 5
)
```

**Parametreler:**
- `query` (string): Arama sorgusu
- `maxResults` (int): Döndürülecek maksimum parça sayısı (varsayılan: 5)

**Döndürür:** İlgili doküman parçalarıyla `List<DocumentChunk>`

**Örnek:**

```csharp
var chunks = await _searchService.SearchDocumentsAsync("makine öğrenimi", maxResults: 10);

foreach (var chunk in chunks)
{
    Console.WriteLine($"Skor: {chunk.RelevanceScore}, İçerik: {chunk.Content}");
}
```

#### GenerateRagAnswerAsync (Kullanımdan Kaldırıldı)

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> v3.0.0'da Kullanımdan Kaldırıldı</h4>
    <p class="mb-0">
        Yerine <code>QueryIntelligenceAsync</code> kullanın. Bu metod v4.0.0'da kaldırılacak.
        Geriye dönük uyumluluk için sağlanan eski metod.
    </p>
</div>

```csharp
[Obsolete("Yerine QueryIntelligenceAsync kullanın")]
Task<RagResponse> GenerateRagAnswerAsync(
    string query, 
    int maxResults = 5, 
    bool startNewConversation = false
)
```

---

## IDocumentService

**Amaç:** Doküman CRUD işlemleri ve yönetimi

**Namespace:** `SmartRAG.Interfaces`

### Metodlar

#### UploadDocumentAsync

Tek bir doküman yükleyin ve işleyin.

```csharp
Task<Document> UploadDocumentAsync(
    Stream fileStream, 
    string fileName, 
    string contentType, 
    string uploadedBy, 
    string language = null
)
```

**Parametreler:**
- `fileStream` (Stream): Doküman dosya akışı
- `fileName` (string): Dosya adı
- `contentType` (string): MIME içerik tipi
- `uploadedBy` (string): Kullanıcı tanımlayıcısı
- `language` (string, isteğe bağlı): OCR için dil kodu (örn. "tur", "eng")

**Desteklenen Formatlar:**
- PDF: `application/pdf`
- Word: `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
- Excel: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- Görseller: `image/jpeg`, `image/png`, `image/webp`, vb.
- Ses: `audio/mpeg`, `audio/wav`, vb.
- Veritabanları: `application/x-sqlite3`

**Örnek:**

```csharp
using var fileStream = File.OpenRead("sozlesme.pdf");

var document = await _documentService.UploadDocumentAsync(
    fileStream,
    "sozlesme.pdf",
    "application/pdf",
    "kullanici-123"
);

Console.WriteLine($"Yüklendi: {document.FileName}, Parçalar: {document.Chunks.Count}");
```

#### GetDocumentAsync

ID'sine göre bir doküman alın.

```csharp
Task<Document> GetDocumentAsync(Guid id)
```

#### GetAllDocumentsAsync

Tüm yüklenmiş dokümanları alın.

```csharp
Task<List<Document>> GetAllDocumentsAsync()
```

#### DeleteDocumentAsync

Bir dokümanı ve parçalarını silin.

```csharp
Task<bool> DeleteDocumentAsync(Guid id)
```

#### GetStorageStatisticsAsync

Depolama istatistiklerini ve metriklerini alın.

```csharp
Task<Dictionary<string, object>> GetStorageStatisticsAsync()
```

**Örnek:**

```csharp
var stats = await _documentService.GetStorageStatisticsAsync();

Console.WriteLine($"Toplam Doküman: {stats["TotalDocuments"]}");
Console.WriteLine($"Toplam Parça: {stats["TotalChunks"]}");
```

---

## IDatabaseParserService

**Amaç:** Canlı bağlantılarla evrensel veritabanı desteği

**Namespace:** `SmartRAG.Interfaces`

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

---

## Veri Modelleri

### RagResponse

Kaynaklarla AI tarafından üretilmiş yanıt.

```csharp
public class RagResponse
{
    public string Query { get; set; }              // Orijinal sorgu
    public string Answer { get; set; }             // AI tarafından üretilen cevap
    public List<SearchSource> Sources { get; set; } // Kaynak dokümanlar
    public DateTime SearchedAt { get; set; }       // Zaman damgası
    public Configuration Configuration { get; set; } // Sağlayıcı yapılandırması
}
```

**Örnek Yanıt:**

```json
{
  "query": "RAG nedir?",
  "answer": "RAG (Retrieval-Augmented Generation), alma ve üretimi birleştiren...",
  "sources": [
    {
      "documentId": "abc-123",
      "fileName": "ml-kilavuzu.pdf",
      "chunkContent": "RAG, almayı birleştirir...",
      "relevanceScore": 0.92
    }
  ],
  "searchedAt": "2025-10-18T14:30:00Z"
}
```

### DocumentChunk

İlgililik skoru ile doküman parçası.

```csharp
public class DocumentChunk
{
    public string Id { get; set; }               // Parça ID
    public string DocumentId { get; set; }       // Üst doküman ID
    public string Content { get; set; }          // Parça metin içeriği
    public List<float> Embedding { get; set; }   // Vektör embedding
    public double RelevanceScore { get; set; }   // Benzerlik skoru (0-1)
    public int ChunkIndex { get; set; }          // Dokümandaki pozisyon
}
```

### Document

Metadata ile doküman varlığı.

```csharp
public class Document
{
    public Guid Id { get; set; }                 // Doküman ID
    public string FileName { get; set; }         // Orijinal dosya adı
    public string ContentType { get; set; }      // MIME tipi
    public long FileSize { get; set; }           // Byte cinsinden dosya boyutu
    public DateTime UploadedAt { get; set; }     // Yükleme zaman damgası
    public string UploadedBy { get; set; }       // Kullanıcı tanımlayıcısı
    public string Content { get; set; }          // Çıkarılan metin içeriği
    public List<DocumentChunk> Chunks { get; set; } // Doküman parçaları
}
```

---

## Enumerasyonlar

### AIProvider

Desteklenen AI sağlayıcıları.

```csharp
public enum AIProvider
{
    OpenAI,        // OpenAI GPT modelleri
    Anthropic,     // Anthropic Claude modelleri
    Gemini,        // Google Gemini modelleri
    AzureOpenAI,   // Azure OpenAI servisi
    Custom         // Özel/Ollama/LM Studio/OpenRouter
}
```

### StorageProvider

Desteklenen depolama backend'leri.

```csharp
public enum StorageProvider
{
    Qdrant,       // Vektör veritabanı
    Redis,        // Yüksek performanslı önbellek
    Sqlite,       // Gömülü veritabanı
    FileSystem,   // Dosya tabanlı depolama
    InMemory      // RAM depolama (sadece geliştirme)
}
```

### DatabaseType

Desteklenen veritabanı tipleri.

```csharp
public enum DatabaseType
{
    SqlServer,    // Microsoft SQL Server
    MySQL,        // MySQL / MariaDB
    PostgreSql,   // PostgreSQL
    Sqlite        // SQLite
}
```

### RetryPolicy

Başarısız istekler için yeniden deneme politikaları.

```csharp
public enum RetryPolicy
{
    None,                // Yeniden deneme yok
    FixedDelay,         // Sabit gecikme
    LinearBackoff,      // Doğrusal artan gecikme
    ExponentialBackoff  // Üssel artan gecikme (önerilen)
}
```

---

## Kullanım Desenleri

### Dependency Injection

Servislerinizde/controller'larınızda interface'leri enjekte edin:

```csharp
public class MyService
{
    private readonly IDocumentSearchService _searchService;
    private readonly IDocumentService _documentService;
    private readonly IDatabaseParserService _databaseService;
    
    public MyService(
        IDocumentSearchService searchService,
        IDocumentService documentService,
        IDatabaseParserService databaseService)
    {
        _searchService = searchService;
        _documentService = documentService;
        _databaseService = databaseService;
    }
    
    public async Task<string> ProcessQuery(string query)
    {
        var response = await _searchService.QueryIntelligenceAsync(query);
        return response.Answer;
    }
}
```

### Hata Yönetimi

```csharp
try
{
    var response = await _searchService.QueryIntelligenceAsync(query);
    return Ok(response);
}
catch (SmartRagException ex)
{
    // SmartRAG'e özgü istisnalar
    _logger.LogError(ex, "SmartRAG hatası: {Message}", ex.Message);
    return BadRequest(new { error = ex.Message });
}
catch (Exception ex)
{
    // Genel istisnalar
    _logger.LogError(ex, "Beklenmeyen hata");
    return StatusCode(500, "Sunucu hatası");
}
```

---

## Performans İpuçları

<div class="alert alert-success">
    <h4><i class="fas fa-bolt me-2"></i> Performans Optimizasyonu</h4>
    <ul class="mb-0">
        <li><strong>Chunk Boyutu:</strong> Optimal denge için 500-1000 karakter</li>
        <li><strong>MaxResults:</strong> Genellikle 5-10 parça yeterli</li>
        <li><strong>Toplu İşlemler:</strong> Birden fazla dosya için <code>UploadDocumentsAsync</code> kullanın</li>
        <li><strong>Depolama:</strong> Üretim için Qdrant veya Redis kullanın (InMemory değil)</li>
        <li><strong>Önbellekleme:</strong> Daha iyi performans için konuşma depolamayı etkinleştirin</li>
        <li><strong>Veritabanı Limitleri:</strong> Makul MaxRowsPerTable ayarlayın (1000-5000)</li>
    </ul>
</div>

---

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card">
            <div class="feature-icon">
                <i class="fas fa-lightbulb"></i>
            </div>
            <h3>Örnekler</h3>
            <p>Pratik kod örneklerini ve gerçek dünya uygulamalarını görün</p>
            <a href="{{ site.baseurl }}/tr/examples" class="btn btn-outline-primary btn-sm mt-3">
                Örnekleri Görüntüle
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card">
            <div class="feature-icon">
                <i class="fas fa-rocket"></i>
            </div>
            <h3>Başlangıç</h3>
            <p>Hızlı kurulum ve yapılandırma kılavuzu</p>
            <a href="{{ site.baseurl }}/tr/getting-started" class="btn btn-outline-primary btn-sm mt-3">
                Başlayın
            </a>
        </div>
    </div>
</div>

</div>

