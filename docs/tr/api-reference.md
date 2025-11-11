---
layout: default
title: API Referans
description: SmartRAG interface'leri, metodları ve modelleri için eksiksiz API dokümantasyonu
lang: tr
---


## Temel Interface'ler

SmartRAG tüm işlemler için iyi tanımlanmış interface'ler sağlar. Bu interface'leri dependency injection ile enjekte edin.

---

## IDocumentSearchService

**Amaç:** RAG pipeline ve konuşma yönetimi ile AI destekli akıllı sorgu işleme

**Namespace:** `SmartRAG.Interfaces`

### Metodlar

#### QueryIntelligenceAsync

Birleşik akıllı sorgu işleme ile RAG ve otomatik oturum yönetimi. Smart Hybrid yönlendirme kullanarak tek sorguda veritabanları, belgeler, görüntüler (OCR) ve ses dosyalarını (transkript) arar.

**Akıllı Hibrit Yönlendirme:**
- **Yüksek Güven (>0.7) + Veritabanı Sorguları**: Sadece veritabanı sorgusu çalıştırır
- **Yüksek Güven (>0.7) + Veritabanı Sorgusu Yok**: Sadece belge sorgusu çalıştırır
- **Orta Güven (0.3-0.7)**: Hem veritabanı hem belge sorgularını çalıştırır, sonuçları birleştirir
- **Düşük Güven (<0.3)**: Sadece belge sorgusu çalıştırır (fallback)

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

**Döndürür:** Tüm mevcut veri kaynaklarından (veritabanları, belgeler, görüntüler, ses) AI cevabı, kaynaklar ve metadata içeren `RagResponse`

**Örnek:**

```csharp
// Tüm veri kaynaklarında birleşik sorgu
var response = await _searchService.QueryIntelligenceAsync(
    "En iyi müşterileri ve son geri bildirimlerini göster", 
    maxResults: 5
);

Console.WriteLine(response.Answer);
// Kaynaklar hem veritabanı hem belge kaynaklarını içerir
foreach (var source in response.Sources)
{
    Console.WriteLine($"Kaynak: {source.FileName}");
}
```

**Not:** Veritabanı coordinator yapılandırılmamışsa, metod otomatik olarak sadece belge aramasına geri döner, geriye dönük uyumluluğu korur.

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

#### UploadDocumentsAsync

Birden fazla dokümanı toplu olarak yükleyin.

```csharp
Task<List<Document>> UploadDocumentsAsync(
    List<(Stream Stream, string FileName, string ContentType)> files,
    string uploadedBy,
    string language = null
)
```

**Örnek:**

```csharp
var files = new List<(Stream, string, string)>
{
    (File.OpenRead("doc1.pdf"), "doc1.pdf", "application/pdf"),
    (File.OpenRead("doc2.docx"), "doc2.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
};

var documents = await _documentService.UploadDocumentsAsync(files, "user-123");
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

#### RegenerateAllEmbeddingsAsync

Tüm dokümanlar için embedding'leri yeniden oluşturur (AI provider değiştikten sonra yararlı).

```csharp
Task<bool> RegenerateAllEmbeddingsAsync()
```

#### ClearAllEmbeddingsAsync

Doküman içeriğini koruyarak tüm embedding'leri temizler.

```csharp
Task<bool> ClearAllEmbeddingsAsync()
```

#### ClearAllDocumentsAsync

Tüm dokümanları ve embedding'lerini temizler.

```csharp
Task<bool> ClearAllDocumentsAsync()
```

---

## IDocumentParserService

**Amaç:** Çoklu format doküman ayrıştırma ve metin çıkarma

**Namespace:** `SmartRAG.Interfaces`

### Metodlar

#### ParseDocumentAsync

Bir dokümanı ayrıştırın ve doküman entity'si oluşturun.

```csharp
Task<Document> ParseDocumentAsync(
    Stream fileStream, 
    string fileName, 
    string contentType, 
    string uploadedBy, 
    string language = null
)
```

#### GetSupportedFileTypes

Desteklenen dosya uzantılarının listesini alın.

```csharp
IEnumerable<string> GetSupportedFileTypes()
```

**Dönen Değerler:**
- `.pdf`, `.docx`, `.doc`
- `.xlsx`, `.xls`
- `.txt`, `.md`, `.json`, `.xml`, `.csv`
- `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.tiff`, `.webp`
- `.mp3`, `.wav`, `.m4a`, `.aac`, `.ogg`, `.flac`, `.wma`
- `.db`, `.sqlite`, `.sqlite3`

#### GetSupportedContentTypes

Desteklenen MIME content type'larının listesini alın.

```csharp
IEnumerable<string> GetSupportedContentTypes()
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

#### ExtractTableDataAsync

Belirli bir tablodan veri çıkarın.

```csharp
Task<string> ExtractTableDataAsync(
    string connectionString, 
    string tableName, 
    DatabaseType databaseType, 
    int maxRows = 1000
)
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

#### GetTableSchemaAsync

Belirli bir tablo için şema bilgilerini alın.

```csharp
Task<string> GetTableSchemaAsync(
    string connectionString, 
    string tableName, 
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

#### ClearMemoryCache

Bellek önbelleğini temizleyin.

```csharp
void ClearMemoryCache()
```

---

## ISemanticSearchService

**Amaç:** Gelişmiş semantik arama ve benzerlik hesaplama

**Namespace:** `SmartRAG.Interfaces`

### Metodlar

#### CalculateEnhancedSemanticSimilarityAsync

Gelişmiş semantik benzerlik skoru hesaplayın.

```csharp
Task<double> CalculateEnhancedSemanticSimilarityAsync(
    string query, 
    string documentContent, 
    CancellationToken cancellationToken = default
)
```

**Örnek:**

```csharp
var similarity = await _semanticSearchService.CalculateEnhancedSemanticSimilarityAsync(
    "machine learning algorithms",
    "artificial intelligence and neural networks",
    cancellationToken
);

Console.WriteLine($"Benzerlik Skoru: {similarity:F2}");
```

---

## IAIService

**Amaç:** AI provider'ları ile etkileşim

**Namespace:** `SmartRAG.Interfaces`

### Metodlar

#### GenerateResponseAsync

AI'dan yanıt oluşturun.

```csharp
Task<string> GenerateResponseAsync(
    string prompt, 
    CancellationToken cancellationToken = default
)
```

#### GenerateEmbeddingsAsync

Tek bir metin için embedding oluşturun.

```csharp
Task<float[]> GenerateEmbeddingsAsync(
    string text, 
    CancellationToken cancellationToken = default
)
```

#### GenerateEmbeddingsBatchAsync

Birden fazla metin için toplu embedding oluşturun.

```csharp
Task<List<float[]>> GenerateEmbeddingsBatchAsync(
    List<string> texts, 
    CancellationToken cancellationToken = default
)
```

**Örnek:**

```csharp
var texts = new List<string>
{
    "Machine learning is fascinating",
    "AI will change the world",
    "Deep learning models are powerful"
};

var embeddings = await _aiService.GenerateEmbeddingsBatchAsync(texts);
Console.WriteLine($"Oluşturulan embedding sayısı: {embeddings.Count}");
```

---

## Veri Modelleri

### DatabaseConfig

Veritabanı yapılandırma modeli.

```csharp
public class DatabaseConfig
{
    public DatabaseType Type { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public List<string> IncludedTables { get; set; } = new();
    public List<string> ExcludedTables { get; set; } = new();
    public int MaxRowsPerTable { get; set; } = 1000;
    public bool SanitizeSensitiveData { get; set; } = true;
    public int QueryTimeoutSeconds { get; set; } = 30;
    public bool EnableSchemaAnalysis { get; set; } = true;
    public int SchemaRefreshIntervalMinutes { get; set; } = 60;
}
```

**Özellikler:**
- `Type` - Veritabanı türü (SqlServer, MySql, PostgreSQL, SQLite)
- `ConnectionString` - Bağlantı dizesi
- `IncludedTables` - Dahil edilecek tablolar
- `ExcludedTables` - Hariç tutulacak tablolar
- `MaxRowsPerTable` - Tablo başına maksimum satır sayısı
- `SanitizeSensitiveData` - Hassas verileri temizle
- `QueryTimeoutSeconds` - Sorgu zaman aşımı
- `EnableSchemaAnalysis` - Şema analizini etkinleştir
- `SchemaRefreshIntervalMinutes` - Şema yenileme aralığı

### RagResponse

Kaynaklarla AI tarafından üretilmiş yanıt.

```csharp
public class RagResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<DocumentChunk> Sources { get; set; } = new();
    public double Confidence { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

**Özellikler:**
- `Answer` - AI tarafından üretilen yanıt
- `Sources` - Yanıt için kullanılan kaynak chunk'lar
- `Confidence` - Yanıtın güven skoru (0.0-1.0)
- `ProcessingTime` - İşlem süresi
- `Metadata` - Ek meta veriler

---

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

### Async/Await En İyi Pratikleri

```csharp
// ✅ İYİ - Async/await kullan
public async Task<SearchResult> SearchAsync(string query)
{
    var embedding = await _embeddingService.GenerateEmbeddingAsync(query);
    var results = await _repository.SearchAsync(embedding);
    return results;
}

// ❌ KÖTÜ - Blocking call
public SearchResult Search(string query)
{
    var embedding = _embeddingService.GenerateEmbeddingAsync(query).Result;
    var results = _repository.SearchAsync(embedding).Result;
    return results;
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

---

## Gelişmiş Interface'ler

### IMultiDatabaseQueryCoordinator

**Amaç:** AI kullanarak çoklu veritabanı sorgularını koordine eder

**Namespace:** `SmartRAG.Interfaces`

Bu interface, doğal dil kullanarak birden fazla veritabanına aynı anda sorgu yapmayı sağlar. AI sorguyu analiz eder, hangi veritabanları ve tablolara erişileceğini belirler, optimize edilmiş SQL sorguları oluşturur ve sonuçları tutarlı bir yanıt halinde birleştirir.

#### Metodlar

##### QueryMultipleDatabasesAsync

Tam bir akıllı sorguyu çalıştırır: intent analizi + yürütme + sonuç birleştirme.

```csharp
Task<RagResponse> QueryMultipleDatabasesAsync(
    string userQuery, 
    int maxResults = 5
)
```

**Parametreler:**
- `userQuery` (string): Doğal dil kullanıcı sorgusu
- `maxResults` (int): Döndürülecek maksimum sonuç sayısı (varsayılan: 5)

**Döndürür:** Birden fazla veritabanından verilerle AI üretilmiş yanıt içeren `RagResponse`

**Örnek:**

```csharp
var response = await _coordinator.QueryMultipleDatabasesAsync(
    "TableA kayıtlarını ve bunların Database1'den gelen TableB detaylarını göster"
);

Console.WriteLine(response.Answer);
// Birden fazla veritabanından gelen veriler birleştirilmiş AI cevabı
```

##### AnalyzeQueryIntentAsync

Kullanıcı sorgusunu analiz eder ve hangi veritabanları/tabloları sorgulayacağını belirler.

```csharp
Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery)
```

**Parametreler:**
- `userQuery` (string): Doğal dil kullanıcı sorgusu

**Dönen Değer:** `QueryIntent` veritabanı yönlendirme bilgileri ile

**Örnek:**

```csharp
var intent = await _coordinator.AnalyzeQueryIntentAsync(
    "Database1 ve Database2 arasındaki verileri karşılaştır"
);

Console.WriteLine($"Güven: {intent.Confidence}");
Console.WriteLine($"Cross-DB Join Gerekiyor: {intent.RequiresCrossDatabaseJoin}");

foreach (var dbQuery in intent.DatabaseQueries)
{
    Console.WriteLine($"Veritabanı: {dbQuery.DatabaseName}");
    Console.WriteLine($"Tablolar: {string.Join(", ", dbQuery.RequiredTables)}");
}
```

##### ExecuteMultiDatabaseQueryAsync

Sorgu intent'ine göre birden fazla veritabanında sorguları çalıştırır.

```csharp
Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(
    QueryIntent queryIntent
)
```

**Parametreler:**
- `queryIntent` (QueryIntent): Analiz edilmiş sorgu intent'i

**Dönen Değer:** `MultiDatabaseQueryResult` tüm veritabanlarından birleştirilmiş sonuçlarla

##### GenerateDatabaseQueriesAsync

Intent'e göre her veritabanı için optimize edilmiş SQL sorguları oluşturur.

```csharp
Task<List<DatabaseQuery>> GenerateDatabaseQueriesAsync(
    QueryIntent queryIntent
)
```

**Parametreler:**
- `queryIntent` (QueryIntent): Analiz edilmiş sorgu intent'i

**Dönen Değer:** `List<DatabaseQuery>` her veritabanı için SQL sorguları

##### MergeResultsAsync

Birden fazla veritabanından gelen sonuçları birleştirir.

```csharp
Task<MultiDatabaseQueryResult> MergeResultsAsync(
    List<DatabaseQueryResult> results
)
```

**Parametreler:**
- `results` (List<DatabaseQueryResult>): Veritabanı sorgu sonuçları

**Dönen Değer:** `MultiDatabaseQueryResult` birleştirilmiş sonuçlar

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Generic Sorgu Örnekleri</h4>
    <p class="mb-0">
        Tüm örnekler <strong>generic placeholder</strong> isimler kullanır (TableA, TableB, Database1).
        Asla domain-specific isimler kullanılmaz (Products, Orders, Customers gibi).
    </p>
</div>

---

### IDatabaseConnectionManager

**Amaç:** Konfigürasyondan veritabanı bağlantılarını yönetir

**Namespace:** `SmartRAG.Interfaces`

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

---

### IDatabaseSchemaAnalyzer

**Amaç:** Veritabanı şemalarını analiz eder ve akıllı metadata oluşturur

**Namespace:** `SmartRAG.Interfaces`

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

---

### IAudioParserService

**Amaç:** Whisper.net ile ses transkripsiyonu (%100 yerel işleme)

**Namespace:** `SmartRAG.Interfaces`

Whisper.net kullanarak yerel ses-metin transkripsiyonu sağlar. Tüm işlem lokalde yapılır.

<div class="alert alert-success">
    <h4><i class="fas fa-lock me-2"></i> Gizlilik Notu</h4>
    <p class="mb-0">
        Ses transkripsiyonu %100 yerel işleme için <strong>Whisper.net</strong> kullanır. 
        Hiçbir ses verisi hiçbir zaman harici servislere gönderilmez. GDPR/KVKK/HIPAA uyumlu.
    </p>
</div>

#### Metodlar

##### TranscribeAudioAsync

Bir akıştan ses içeriğini metne transcribe eder.

```csharp
Task<AudioTranscriptionResult> TranscribeAudioAsync(
    Stream audioStream, 
    string fileName, 
    string language = null
)
```

**Parametreler:**
- `audioStream` (Stream): Transcribe edilecek ses akışı
- `fileName` (string): Format tespiti için ses dosyası adı
- `language` (string, isteğe bağlı): Transkripsiyon için dil kodu (örn. "tr", "en", "auto")

**Döndürür:** Transcribe edilmiş metin, güven skoru ve metadata ile `AudioTranscriptionResult`

**Örnek:**

```csharp
using var audioStream = File.OpenRead("toplanti.mp3");

var result = await _audioParser.TranscribeAudioAsync(
    audioStream, 
    "toplanti.mp3", 
    language: "tr"
);

Console.WriteLine($"Transkripsiyon: {result.Text}");
Console.WriteLine($"Güven: {result.Confidence:P}");
```

---

### IImageParserService

**Amaç:** Tesseract kullanarak görüntülerden OCR metin çıkarma

**Namespace:** `SmartRAG.Interfaces`

Görüntülerden metin çıkarmak için optik karakter tanıma (OCR) sağlar. Tüm işlem Tesseract kullanarak lokaldir.

#### Metodlar

##### ExtractTextFromImageAsync

OCR kullanarak bir görüntüden metin çıkarır.

```csharp
Task<string> ExtractTextFromImageAsync(
    Stream imageStream, 
    string language = "eng"
)
```

**Parametreler:**
- `imageStream` (Stream): İşlenecek görüntü akışı
- `language` (string, isteğe bağlı): OCR için dil kodu (varsayılan: "eng")

**Döndürür:** Çıkarılan metin (string)

**Örnek:**

```csharp
using var imageStream = File.OpenRead("dokuman.png");

var text = await _imageParser.ExtractTextFromImageAsync(
    imageStream, 
    language: "tur"
);

Console.WriteLine($"Çıkarılan Metin: {text}");
```

##### ExtractTextWithConfidenceAsync

Güven skorlarıyla görüntüden metin çıkarır.

```csharp
Task<OcrResult> ExtractTextWithConfidenceAsync(
    Stream imageStream, 
    string language = "eng"
)
```

**Parametreler:**
- `imageStream` (Stream): İşlenecek görüntü akışı
- `language` (string, isteğe bağlı): OCR için dil kodu (varsayılan: "eng")

**Döndürür:** Çıkarılan metin, güven skorları ve metin bloklarıyla `OcrResult`

**Örnek:**

```csharp
using var imageStream = File.OpenRead("fatura.jpg");

var result = await _imageParser.ExtractTextWithConfidenceAsync(
    imageStream, 
    language: "tur"
);

Console.WriteLine($"Metin: {result.ExtractedText}");
Console.WriteLine($"Güven: {result.Confidence:P}");
```

##### PreprocessImageAsync

Daha iyi OCR sonuçları için görüntüyü ön işleme tabi tutar.

```csharp
Task<Stream> PreprocessImageAsync(Stream imageStream)
```

**Parametreler:**
- `imageStream` (Stream): Giriş görüntü akışı

**Dönen Değer:** Ön işleme tabi tutulmuş görüntü akışı

**Ön İşleme Adımları:**
- Gri tonlama dönüşümü
- Kontrast artırma
- Gürültü azaltma
- İkili hale getirme

**Örnek:**

```csharp
using var originalStream = File.OpenRead("dusuk-kalite.jpg");

var preprocessedStream = await _imageParser.PreprocessImageAsync(originalStream);

var result = await _imageParser.ExtractTextFromImageAsync(preprocessedStream);
Console.WriteLine($"Ön işleme sonrası metin: {result}");
```

---