---
layout: default
title: API Referans
description: SmartRAG interface'leri, metodları ve modelleri için eksiksiz API dokümantasyonu
lang: tr
redirect_from: /tr/api-reference.html
---

<script>
    window.location.href = "{{ site.baseurl }}/tr/api-reference/";
</script>


## IDocumentSearchService

**Amaç:** RAG pipeline ve konuşma yönetimi ile AI destekli akıllı sorgu işleme

**Namespace:** `SmartRAG.Interfaces.Document`

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
    bool startNewConversation = false,
    SearchOptions? options = null
)
```

**Parametreler:**
- `query` (string): Kullanıcının sorusu veya sorgusu
- `maxResults` (int): Alınacak maksimum doküman parçası sayısı (varsayılan: 5)
- `startNewConversation` (bool): Yeni bir konuşma oturumu başlat (varsayılan: false)
- `options` (SearchOptions?): Global yapılandırmayı geçersiz kılmak için isteğe bağlı arama seçenekleri (varsayılan: null)

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

**SearchOptions Kullanımı:**

```csharp
// Sadece veritabanı araması
var dbOptions = new SearchOptions
{
    EnableDatabaseSearch = true,
    EnableDocumentSearch = false,
    EnableAudioSearch = false,
    EnableImageSearch = false
};

var dbResponse = await _searchService.QueryIntelligenceAsync(
    "En iyi müşterileri göster",
    maxResults: 5,
    options: dbOptions
);

// Sadece ses araması
var audioOptions = new SearchOptions
{
    EnableDatabaseSearch = false,
    EnableDocumentSearch = false,
    EnableAudioSearch = true,
    EnableImageSearch = false,
    PreferredLanguage = "tr"
};

var audioResponse = await _searchService.QueryIntelligenceAsync(
    "Toplantıda ne konuşuldu?",
    maxResults: 5,
    options: audioOptions
);
```

**Bayrak Tabanlı Filtreleme (Sorgu String Ayrıştırma):**

Sorgu string'lerinden bayrakları ayrıştırarak hızlı arama tipi seçimi yapabilirsiniz:

```csharp
// Sorgu string'inden bayrakları ayrıştır
string userQuery = "-db En iyi müşterileri göster";
var searchOptions = ParseSearchOptions(userQuery, out string cleanQuery);

// cleanQuery = "En iyi müşterileri göster"
// searchOptions.EnableDatabaseSearch = true
// Diğerleri = false

var response = await _searchService.QueryIntelligenceAsync(
    cleanQuery,
    maxResults: 5,
    options: searchOptions
);
```

**Mevcut Bayraklar:**
- `-db`: Sadece veritabanı araması
- `-d`: Sadece doküman (metin) araması
- `-a`: Sadece ses araması
- `-i`: Sadece görüntü araması
- Bayraklar birleştirilebilir (örn: `-db -a` = veritabanı + ses araması)

**Not:** Veritabanı coordinator yapılandırılmamışsa, metod otomatik olarak sadece belge aramasına geri döner, geriye dönük uyumluluğu korur.

#### SearchDocumentsAsync

AI cevabı üretmeden dokümanları anlamsal olarak arayın.

```csharp
Task<List<DocumentChunk>> SearchDocumentsAsync(
    string query, 
    int maxResults = 5,
    SearchOptions? options = null,
    List<string>? queryTokens = null
)
```

**Parametreler:**
- `query` (string): Arama sorgusu
- `maxResults` (int): Döndürülecek maksimum parça sayısı (varsayılan: 5)
- `options` (SearchOptions?, opsiyonel): Global yapılandırmayı geçersiz kılmak için isteğe bağlı arama seçenekleri (varsayılan: null)
- `queryTokens` (List<string>?, opsiyonel): Performans optimizasyonu için önceden hesaplanmış sorgu token'ları (varsayılan: null)

**Döndürür:** İlgili doküman parçalarıyla `List<DocumentChunk>`

**Örnek:**

```csharp
// Temel kullanım
var chunks = await _searchService.SearchDocumentsAsync("makine öğrenimi", maxResults: 10);

foreach (var chunk in chunks)
{
    Console.WriteLine($"Skor: {chunk.RelevanceScore}, İçerik: {chunk.Content}");
}

// Arama seçenekleri ile
var options = new SearchOptions
{
    EnableDocumentSearch = true,
    EnableAudioSearch = false,
    EnableImageSearch = false
};

var filteredChunks = await _searchService.SearchDocumentsAsync(
    "makine öğrenimi", 
    maxResults: 10,
    options: options
);

// Önceden hesaplanmış token'lar ile (performans optimizasyonu)
var tokens = new List<string> { "makine", "öğrenimi", "algoritmalar" };
var optimizedChunks = await _searchService.SearchDocumentsAsync(
    "makine öğrenimi",
    maxResults: 10,
    queryTokens: tokens
);
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

**Namespace:** `SmartRAG.Interfaces.Document`

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

## IConversationManagerService

**Amaç:** Konuşma oturumu yönetimi ve geçmiş takibi

**Namespace:** `SmartRAG.Interfaces.Support`

Bu interface, daha iyi sorumluluk ayrımı için doküman işlemlerinden ayrılmış özel konuşma yönetimi sağlar.

### Metodlar

#### StartNewConversationAsync

Yeni bir konuşma oturumu başlatır.

```csharp
Task<string> StartNewConversationAsync()
```

**Döndürür:** Yeni oturum ID'si (string)

**Örnek:**

```csharp
var sessionId = await _conversationManager.StartNewConversationAsync();
Console.WriteLine($"Oturum başlatıldı: {sessionId}");
```

#### GetOrCreateSessionIdAsync

Mevcut oturum ID'sini alır veya otomatik olarak yeni bir tane oluşturur.

```csharp
Task<string> GetOrCreateSessionIdAsync()
```

**Döndürür:** Oturum ID'si (string)

**Kullanım Senaryosu:** Manuel oturum yönetimi olmadan otomatik oturum sürekliliği

**Örnek:**

```csharp
// Otomatik olarak oturumu yönetir - yoksa yeni oluşturur
var sessionId = await _conversationManager.GetOrCreateSessionIdAsync();
```

#### AddToConversationAsync

Oturum geçmişine bir konuşma turu (soru + cevap) ekler.

```csharp
Task AddToConversationAsync(
    string sessionId, 
    string question, 
    string answer
)
```

**Parametreler:**
- `sessionId` (string): Oturum tanımlayıcısı
- `question` (string): Kullanıcının sorusu
- `answer` (string): AI'ın cevabı

**Örnek:**

```csharp
await _conversationManager.AddToConversationAsync(
    sessionId,
    "Makine öğrenimi nedir?",
    "Makine öğrenimi, sistemlerin öğrenmesini sağlayan AI'ın bir alt kümesidir..."
);
```

#### GetConversationHistoryAsync

Bir oturum için tam konuşma geçmişini alır.

```csharp
Task<string> GetConversationHistoryAsync(string sessionId)
```

**Parametreler:**
- `sessionId` (string): Oturum tanımlayıcısı

**Döndürür:** String olarak biçimlendirilmiş konuşma geçmişi

**Format:**
```
User: [soru]
Assistant: [cevap]
User: [sonraki soru]
Assistant: [sonraki cevap]
```

**Örnek:**

```csharp
var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
Console.WriteLine(history);
```

#### TruncateConversationHistory

Sadece son turları tutmak için konuşma geçmişini kısaltır (bellek yönetimi).

```csharp
string TruncateConversationHistory(
    string history, 
    int maxTurns = 3
)
```

**Parametreler:**
- `history` (string): Tam konuşma geçmişi
- `maxTurns` (int): Tutulacak maksimum konuşma turu sayısı (varsayılan: 3)

**Döndürür:** Kısaltılmış konuşma geçmişi

**Kullanım Senaryosu:** AI prompt'larında context window taşmasını önler

**Örnek:**

```csharp
var fullHistory = await _conversationManager.GetConversationHistoryAsync(sessionId);
var recentHistory = _conversationManager.TruncateConversationHistory(fullHistory, maxTurns: 5);
```

### Tam Kullanım Örneği

```csharp
public class ChatService
{
    private readonly IConversationManagerService _conversationManager;
    private readonly IDocumentSearchService _searchService;
    
    public ChatService(
        IConversationManagerService conversationManager,
        IDocumentSearchService searchService)
    {
        _conversationManager = conversationManager;
        _searchService = searchService;
    }
    
    public async Task<string> HandleChatAsync(string userMessage)
    {
        // Oturum al veya oluştur
        var sessionId = await _conversationManager.GetOrCreateSessionIdAsync();
        
        // Context için konuşma geçmişini al
        var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
        
        // Context ile sorgu
        var response = await _searchService.QueryIntelligenceAsync(userMessage);
        
        // Konuşma geçmişine kaydet
        await _conversationManager.AddToConversationAsync(
            sessionId, 
            userMessage, 
            response.Answer
        );
        
        return response.Answer;
    }
    
    public async Task<string> StartNewChatAsync()
    {
        var newSessionId = await _conversationManager.StartNewConversationAsync();
        return $"Yeni konuşma başlatıldı: {newSessionId}";
    }
}
```

### Depolama Backend'leri

Konuşma geçmişi yapılandırılmış `IConversationRepository` kullanılarak depolanır:
- **SQLite**: `SqliteConversationRepository` - Kalıcı dosya tabanlı depolama
- **InMemory**: `InMemoryConversationRepository` - Hızlı, kalıcı olmayan (geliştirme)
- **FileSystem**: `FileSystemConversationRepository` - JSON dosya tabanlı depolama
- **Redis**: `RedisConversationRepository` - Yüksek performanslı dağıtık depolama

Depolama backend'i `StorageProvider` yapılandırmanıza göre otomatik olarak seçilir.

---

## IDocumentParserService

**Amaç:** Çoklu format doküman ayrıştırma ve metin çıkarma

**Namespace:** `SmartRAG.Interfaces.Document`

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

**Namespace:** `SmartRAG.Interfaces.Search`

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

## IContextExpansionService

**Amaç:** Aynı dokümandaki bitişik chunk'ları dahil ederek doküman chunk context'ini genişletme

**Namespace:** `SmartRAG.Interfaces.Search`

### Metodlar

#### ExpandContextAsync

Aynı dokümandaki bitişik chunk'ları dahil ederek context'i genişletir. Bu, bir başlık bir chunk'ta ve içerik bir sonraki chunk'ta olsa bile, her ikisinin de arama sonuçlarına dahil edilmesini sağlar.

```csharp
Task<List<DocumentChunk>> ExpandContextAsync(
    List<DocumentChunk> chunks, 
    int contextWindow = 2
)
```

**Parametreler:**
- `chunks` (List<DocumentChunk>): Arama ile bulunan başlangıç chunk'ları
- `contextWindow` (int): Bulunan her chunk'ın öncesi ve sonrasına dahil edilecek bitişik chunk sayısı (varsayılan: 2, maksimum: 5)

**Döndürür:** Context ile genişletilmiş chunk listesi, doküman ID ve chunk index'e göre sıralanmış

**Örnek:**

```csharp
// İlgili chunk'ları ara
var chunks = await _searchService.SearchDocumentsAsync("SRS bakımı", maxResults: 5);

// Context'i genişletmek için bitişik chunk'ları dahil et
var expandedChunks = await _contextExpansion.ExpandContextAsync(chunks, contextWindow: 2);

// Artık expandedChunks başlık chunk'ını VE içerik chunk'larını içeriyor
foreach (var chunk in expandedChunks)
{
    Console.WriteLine($"Chunk {chunk.ChunkIndex}: {chunk.Content.Substring(0, 100)}...");
}
```

**Not:** Bu servis, RAG cevapları oluştururken `DocumentSearchService` tarafından otomatik olarak kullanılır. Sadece başlıkların bulunup karşılık gelen içeriğin bulunmadığı durumları önlemeye yardımcı olur.

---

## IAIService

**Amaç:** AI provider'ları ile etkileşim

**Namespace:** `SmartRAG.Interfaces.AI`

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

### SearchOptions

İstek başına arama yapılandırması ile arama tipleri üzerinde detaylı kontrol.

```csharp
public class SearchOptions
{
    public bool EnableDatabaseSearch { get; set; } = true;
    public bool EnableDocumentSearch { get; set; } = true;
    public bool EnableAudioSearch { get; set; } = true;
    public bool EnableImageSearch { get; set; } = true;
    public string? PreferredLanguage { get; set; }
    
    public static SearchOptions Default => new SearchOptions();
    public static SearchOptions FromConfig(SmartRagOptions options);
}
```

**Özellikler:**
- `EnableDatabaseSearch` (bool): Veritabanlarında arama yapmayı etkinleştir (varsayılan: true)
- `EnableDocumentSearch` (bool): Metin dokümanlarında arama yapmayı etkinleştir (varsayılan: true)
- `EnableAudioSearch` (bool): Ses dosyalarında transkripsiyon ile arama yapmayı etkinleştir (varsayılan: true)
- `EnableImageSearch` (bool): Görüntülerde OCR ile arama yapmayı etkinleştir (varsayılan: true)
- `PreferredLanguage` (string?): AI yanıtları için ISO 639-1 dil kodu (örn: "tr", "en", "de")

**Statik Metodlar:**
- `Default`: Tüm özellikler etkin varsayılan arama seçenekleri oluşturur
- `FromConfig(SmartRagOptions)`: Global yapılandırmadan arama seçenekleri oluşturur

**Örnek:**

```csharp
// Özel arama seçenekleri
var options = new SearchOptions
{
    EnableDatabaseSearch = true,
    EnableDocumentSearch = false,
    EnableAudioSearch = false,
    EnableImageSearch = false,
    PreferredLanguage = "tr"
};

var response = await _searchService.QueryIntelligenceAsync(
    "En iyi müşterileri göster",
    maxResults: 5,
    options: options
);

// Global yapılandırmayı kullan
var globalOptions = SearchOptions.FromConfig(_smartRagOptions);
var response2 = await _searchService.QueryIntelligenceAsync(
    "Her şeyde ara",
    maxResults: 5,
    options: globalOptions
);
```

**Bayrak Tabanlı Filtreleme:**

Hızlı arama tipi seçimi için sorgu string bayrak ayrıştırması uygulayabilirsiniz:

```csharp
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
        return null; // Varsayılanı kullan
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

// Kullanım
string userQuery = "-db En iyi müşterileri göster";
var searchOptions = ParseSearchOptions(userQuery, out string cleanQuery);
// cleanQuery = "En iyi müşterileri göster"
// searchOptions.EnableDatabaseSearch = true, diğerleri = false

var response = await _searchService.QueryIntelligenceAsync(
    cleanQuery,
    maxResults: 5,
    options: searchOptions
);
```

**Mevcut Bayraklar:**
- `-db`: Sadece veritabanı araması
- `-d`: Sadece doküman (metin) araması
- `-a`: Sadece ses araması
- `-i`: Sadece görüntü araması
- Bayraklar birleştirilebilir (örn: `-db -a` = veritabanı + ses araması)

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

### SearchOptions

İstek başına arama yapılandırması ile arama tipleri üzerinde detaylı kontrol.

```csharp
public class SearchOptions
{
    public bool EnableDatabaseSearch { get; set; } = true;
    public bool EnableDocumentSearch { get; set; } = true;
    public bool EnableAudioSearch { get; set; } = true;
    public bool EnableImageSearch { get; set; } = true;
    public string? PreferredLanguage { get; set; }
    
    public static SearchOptions Default => new SearchOptions();
    public static SearchOptions FromConfig(SmartRagOptions options);
}
```

**Özellikler:**
- `EnableDatabaseSearch` (bool): Veritabanlarında arama yapmayı etkinleştir (varsayılan: true)
- `EnableDocumentSearch` (bool): Metin dokümanlarında arama yapmayı etkinleştir (varsayılan: true)
- `EnableAudioSearch` (bool): Ses dosyalarında transkripsiyon ile arama yapmayı etkinleştir (varsayılan: true)
- `EnableImageSearch` (bool): Görüntülerde OCR ile arama yapmayı etkinleştir (varsayılan: true)
- `PreferredLanguage` (string?): AI yanıtları için ISO 639-1 dil kodu (örn: "tr", "en", "de")

**Statik Metodlar:**
- `Default`: Tüm özellikler etkin varsayılan arama seçenekleri oluşturur
- `FromConfig(SmartRagOptions)`: Global yapılandırmadan arama seçenekleri oluşturur

**Örnek:**

```csharp
// Özel arama seçenekleri
var options = new SearchOptions
{
    EnableDatabaseSearch = true,
    EnableDocumentSearch = false,
    EnableAudioSearch = false,
    EnableImageSearch = false,
    PreferredLanguage = "tr"
};

var response = await _searchService.QueryIntelligenceAsync(
    "En iyi müşterileri göster",
    maxResults: 5,
    options: options
);
```

**Bayrak Tabanlı Filtreleme:**

Hızlı arama tipi seçimi için sorgu string bayrak ayrıştırması uygulayabilirsiniz. Detaylar için [Examples](/tr/examples) sayfasına bakın.

**Mevcut Bayraklar:**
- `-db`: Sadece veritabanı araması
- `-d`: Sadece doküman (metin) araması
- `-a`: Sadece ses araması
- `-i`: Sadece görüntü araması
- Bayraklar birleştirilebilir (örn: `-db -a` = veritabanı + ses araması)

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

Doküman ve vektör verisi kalıcılığı için desteklenen depolama backend'leri.

```csharp
public enum StorageProvider
{
    InMemory,    // RAM depolama (kalıcı değil, test ve geliştirme için)
    Redis,       // Yüksek performanslı önbellek ve depolama
    Qdrant       // Gelişmiş vektör arama yetenekleri için vektör veritabanı
}
```

**Not:** `SQLite` ve `FileSystem`, `StorageProvider` seçenekleri olarak mevcut değildir. Bunlar yalnızca konuşma geçmişi depolama için `ConversationStorageProvider` seçenekleri olarak mevcuttur.

### DatabaseType

Desteklenen veritabanı tipleri.

```csharp
public enum DatabaseType
{
    SQLite,       // SQLite gömülü veritabanı
    SqlServer,    // Microsoft SQL Server
    MySQL,        // MySQL / MariaDB
    PostgreSQL    // PostgreSQL
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
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
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
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
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

**Namespace:** `SmartRAG.Interfaces.Database`

Bu interface, doğal dil kullanarak birden fazla veritabanına aynı anda sorgu yapmayı sağlar. AI sorguyu analiz eder, hangi veritabanları ve tablolara erişileceğini belirler, optimize edilmiş SQL sorguları oluşturur ve sonuçları tutarlı bir yanıt halinde birleştirir.

#### Metodlar

##### QueryMultipleDatabasesAsync

Tam bir akıllı sorguyu çalıştırır: intent analizi + yürütme + sonuç birleştirme.

**Overload 1:** Otomatik intent analizi ile tam sorgu

```csharp
Task<RagResponse> QueryMultipleDatabasesAsync(
    string userQuery, 
    int maxResults = 5
)
```

**Overload 2:** Önceden analiz edilmiş intent ile sorgu (gereksiz AI çağrılarını önler)

```csharp
Task<RagResponse> QueryMultipleDatabasesAsync(
    string userQuery, 
    QueryIntent preAnalyzedIntent,
    int maxResults = 5
)
```

**Parametreler:**
- `userQuery` (string): Doğal dil kullanıcı sorgusu
- `preAnalyzedIntent` (QueryIntent, opsiyonel): Gereksiz AI çağrılarını önlemek için önceden analiz edilmiş sorgu intent'i
- `maxResults` (int): Döndürülecek maksimum sonuç sayısı (varsayılan: 5)

**Döndürür:** Birden fazla veritabanından verilerle AI üretilmiş yanıt içeren `RagResponse`

**Örnek 1 - Otomatik Intent Analizi:**

```csharp
var response = await _coordinator.QueryMultipleDatabasesAsync(
    "TableA kayıtlarını ve bunların Database1'den gelen TableB detaylarını göster"
);

Console.WriteLine(response.Answer);
// Birden fazla veritabanından gelen veriler birleştirilmiş AI cevabı
```

**Örnek 2 - Önceden Analiz Edilmiş Intent (Performans Optimizasyonu):**

```csharp
// Intent'i bir kez analiz et
var intent = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(
    "TableA kayıtlarını ve bunların Database1'den gelen TableB detaylarını göster"
);

// Gereksiz AI çağrılarını önlemek için önceden analiz edilmiş intent'i kullan
var response = await _coordinator.QueryMultipleDatabasesAsync(
    "TableA kayıtlarını ve bunların Database1'den gelen TableB detaylarını göster",
    intent,
    maxResults: 10
);

Console.WriteLine(response.Answer);
```

##### AnalyzeQueryIntentAsync (Deprecated)

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> 'da Deprecated</h4>
    <p class="mb-0">
        Bunun yerine <code>IQueryIntentAnalyzer.AnalyzeQueryIntentAsync</code> kullanın. Bu method v4.0.0'da kaldırılacak.
    </p>
</div>

Kullanıcı sorgusunu analiz eden ve hangi veritabanları/tabloları sorgulayacağını belirleyen eski method.

```csharp
[Obsolete("Use IQueryIntentAnalyzer.AnalyzeQueryIntentAsync instead. Will be removed in v4.0.0")]
Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery)
```

**Parametreler:**
- `userQuery` (string): Doğal dil kullanıcı sorgusu

**Dönen Değer:** `QueryIntent` veritabanı yönlendirme bilgileri ile

**Önerilen Kullanım:**

```csharp
// Bunun yerine IQueryIntentAnalyzer kullanın
var intent = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(
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
Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent)
```

**Parametreler:**
- `queryIntent` (QueryIntent): SQL oluşturulacak sorgu intent'i

**Dönen Değer:** Oluşturulmuş SQL sorguları ile güncellenmiş `QueryIntent`

**Not:** `MergeResultsAsync`, `IMultiDatabaseQueryCoordinator` interface'inde değil, `IResultMerger` interface'inde mevcuttur. Coordinator, result merger'ı dahili olarak otomatik kullanır.

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

##### ValidateConnectionAsync

Belirli bir bağlantıyı doğrular.

```csharp
Task<bool> ValidateConnectionAsync(string databaseId)
```

**Parametreler:**
- `databaseId` (string): Veritabanı tanımlayıcısı

**Dönen Değer:** Bağlantı geçerliyse true, aksi takdirde false

**Örnek:**

```csharp
bool isValid = await _connectionManager.ValidateConnectionAsync("database-1");

if (isValid)
{
    Console.WriteLine("Bağlantı geçerli");
}
```

##### GetDatabaseIdAsync

Bağlantıdan veritabanı ID'sini alır (Name sağlanmamışsa otomatik oluşturur).

```csharp
Task<string> GetDatabaseIdAsync(DatabaseConnectionConfig connectionConfig)
```

**Parametreler:**
- `connectionConfig` (DatabaseConnectionConfig): Bağlantı yapılandırması

**Dönen Değer:** Benzersiz veritabanı tanımlayıcısı

**Örnek:**

```csharp
var config = new DatabaseConnectionConfig
{
    Name = "SalesDB",
    ConnectionString = "Server=localhost;Database=Sales;Trusted_Connection=true;",
    DatabaseType = DatabaseType.SqlServer
};

var databaseId = await _connectionManager.GetDatabaseIdAsync(config);
Console.WriteLine($"Veritabanı ID: {databaseId}");
```

---

### IDatabaseSchemaAnalyzer

**Amaç:** Veritabanı şemalarını analiz eder ve akıllı metadata oluşturur

**Namespace:** `SmartRAG.Interfaces.Database`

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
Console.WriteLine($"Toplam Satır: {schemaInfo.TotalRowCount:N0}");
Console.WriteLine($"AI Özeti: {schemaInfo.AISummary}");
```

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

**Namespace:** `SmartRAG.Interfaces.Parser`

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

**Namespace:** `SmartRAG.Interfaces.Parser`

Görüntülerden metin çıkarmak için optik karakter tanıma (OCR) sağlar. Tüm işlem Tesseract kullanarak lokaldir.

#### Metodlar

##### ExtractTextFromImageAsync

OCR kullanarak bir görüntüden metin çıkarır.

```csharp
Task<string> ExtractTextFromImageAsync(
    Stream imageStream, 
    string language = null
)
```

**Parametreler:**
- `imageStream` (Stream): İşlenecek görüntü akışı
- `language` (string, opsiyonel): OCR için dil kodu (örn: "eng", "tur"). Null ise sistem yerel ayarını otomatik kullanır

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
    string language = null
)
```

**Parametreler:**
- `imageStream` (Stream): İşlenecek görüntü akışı
- `language` (string, opsiyonel): OCR için dil kodu (örn: "eng", "tur"). Null ise sistem yerel ayarını otomatik kullanır

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

##### CorrectCurrencySymbols

Metindeki para birimi sembolü yanlış okumalarını düzeltir (örn: % → ₺, $, €). Bu method, OCR sonuçlarında kullanılan aynı para birimi düzeltme mantığını herhangi bir metne uygular.

```csharp
string CorrectCurrencySymbols(string text, string language = null)
```

**Parametreler:**
- `text` (string): Düzeltilecek metin
- `language` (string, opsiyonel): Context için dil kodu (loglama için kullanılır)

**Döndürür:** Düzeltilmiş para birimi sembolleri ile metin

**Örnek:**

```csharp
var correctedText = _imageParser.CorrectCurrencySymbols("Fiyat: 100%", "tr");
Console.WriteLine(correctedText); // "Fiyat: 100₺"
```

**Desteklenen Görüntü Formatları:**
- JPEG, PNG, GIF, BMP, TIFF, WEBP

---

## Strategy Pattern Interface'leri 

SmartRAG , genişletilebilirlik ve özelleştirme için Strategy Pattern'i tanıtıyor.

### ISqlDialectStrategy

**Amaç:** Veritabanına özgü SQL üretimi ve doğrulama

**Namespace:** `SmartRAG.Interfaces.Database.Strategies`

Veritabanına özgü SQL optimizasyonu ve özel veritabanı desteği sağlar.

#### Özellikler

```csharp
DatabaseType DatabaseType { get; }
```

#### Metodlar

##### BuildSystemPrompt

Bu veritabanı diyalektine özgü SQL üretimi için AI sistem prompt'u oluşturur.

```csharp
string BuildSystemPrompt(DatabaseSchemaInfo schema, string userQuery)
```

##### ValidateSyntax

Bu belirli diyalekt için SQL sözdizimini doğrular.

```csharp
bool ValidateSyntax(string sql, out string errorMessage)
```

##### FormatSql

SQL sorgusunu diyalekte özgü kurallara göre biçimlendirir.

```csharp
string FormatSql(string sql)
```

##### GetLimitClause

Bu diyalekt için LIMIT cümlesi formatını alır.

```csharp
string GetLimitClause(int limit)
```

**Döndürür:**
- SQLite/MySQL: `LIMIT {limit}`
- SQL Server: `TOP {limit}`
- PostgreSQL: `LIMIT {limit}`

#### Yerleşik Uygulamalar

- `SqliteDialectStrategy` - SQLite için optimize edilmiş SQL
- `PostgreSqlDialectStrategy` - PostgreSQL için optimize edilmiş SQL
- `MySqlDialectStrategy` - MySQL/MariaDB için optimize edilmiş SQL
- `SqlServerDialectStrategy` - SQL Server için optimize edilmiş SQL

#### Özel Uygulama Örneği

**Not:** Bu kavramsal bir örnektir. Yeni bir veritabanı tipi desteği eklemek için:
1. `DatabaseType` enum'ına veritabanı tipini ekleyin
2. O veritabanı için `ISqlDialectStrategy` implementasyonu yapın
3. Dependency injection'da stratejiyi kaydedin

```csharp
// Örnek: Özel veritabanı diyalekt stratejisi
public class CustomDialectStrategy : BaseSqlDialectStrategy
{
    public override DatabaseType DatabaseType => DatabaseType.Custom; // Custom'ın enum'a eklendiği varsayılıyor
    
    public override string BuildSystemPrompt(DatabaseSchemaInfo schema, string userQuery)
    {
        return $"SQL oluştur: {userQuery}\\nŞema: {schema}";
    }
    
    public override bool ValidateSyntax(string sql, out string errorMessage)
    {
        // Veritabanına özgü doğrulama
        errorMessage = null;
        return true;
    }
    
    public override string FormatSql(string sql)
    {
        // Veritabanına özgü biçimlendirme
        return sql;
    }
    
    public override string GetLimitClause(int limit)
    {
        // Veritabanına özgü LIMIT clause formatı
        return $"LIMIT {limit}";
    }
}
```

---

### IScoringStrategy

**Amaç:** Özelleştirilebilir doküman ilgililik skorlaması

**Namespace:** `SmartRAG.Interfaces.Search.Strategies`

Arama sonuçları için özel skorlama algoritmaları sağlar.

#### Metodlar

##### CalculateScoreAsync

Bir doküman parçası için ilgililik skoru hesaplar.

```csharp
Task<double> CalculateScoreAsync(
    string query, 
    DocumentChunk chunk, 
    List<float> queryEmbedding
)
```

**Parametreler:**
- `query` (string): Arama sorgusu
- `chunk` (DocumentChunk): Skorlanacak doküman parçası
- `queryEmbedding` (List<float>): Sorgu embedding vektörü

**Döndürür:** 0.0 ile 1.0 arasında skor

#### Yerleşik Uygulama

**HybridScoringStrategy** (varsayılan):
- %80 semantik benzerlik (embedding'lerin kosinüs benzerliği)
- %20 anahtar kelime eşleşmesi (BM25 benzeri skorlama)

#### Özel Uygulama Örneği

```csharp
public class SemanticOnlyScoringStrategy : IScoringStrategy
{
    public async Task<double> CalculateScoreAsync(
        string query, 
        DocumentChunk chunk, 
        List<float> queryEmbedding)
    {
        // Saf semantik benzerlik (%100 embedding tabanlı)
        return CosineSimilarity(queryEmbedding, chunk.Embedding);
    }
    
    private double CosineSimilarity(List<float> a, List<float> b)
    {
        double dotProduct = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Count; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
```

---

### IFileParser

**Amaç:** Belirli dosya formatlarını ayrıştırma stratejisi

**Namespace:** `SmartRAG.Interfaces.Parser.Strategies`

Özel dosya formatı ayrıştırıcıları sağlar.

#### Metodlar

##### ParseAsync

Bir dosyayı ayrıştırır ve içeriği çıkarır.

```csharp
Task<FileParserResult> ParseAsync(Stream fileStream, string fileName)
```

##### CanParse

Bu ayrıştırıcının verilen dosyayı işleyip işleyemeyeceğini kontrol eder.

```csharp
bool CanParse(string fileName, string contentType)
```

#### Yerleşik Uygulamalar

- `PdfFileParser` - PDF dokümanları
- `WordFileParser` - Word dokümanları (.docx)
- `ExcelFileParser` - Excel elektronik tabloları (.xlsx)
- `TextFileParser` - Düz metin dosyaları
- `ImageFileParser` - OCR ile görseller
- `AudioFileParser` - Ses transkripsiyon
- `DatabaseFileParser` - SQLite veritabanları

#### Özel Uygulama Örneği

```csharp
public class MarkdownFileParser : IFileParser
{
    public bool CanParse(string fileName, string contentType)
    {
        return fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
               contentType == "text/markdown";
    }
    
    public async Task<FileParserResult> ParseAsync(Stream fileStream, string fileName)
    {
        using var reader = new StreamReader(fileStream);
        var content = await reader.ReadToEndAsync();
        
        // Düz metin için markdown sözdizimini kaldır
        var plainText = StripMarkdownSyntax(content);
        
        return new FileParserResult
        {
            Content = plainText,
            Success = true
        };
    }
    
    private string StripMarkdownSyntax(string markdown)
    {
        // Markdown biçimlendirmesini kaldır
        return Regex.Replace(markdown, @"[#*`\[\]()]", "");
    }
}
```

---

## Ek Servis Interface'leri 

### IConversationRepository

**Amaç:** Konuşma depolama için veri erişim katmanı

**Namespace:** `SmartRAG.Interfaces.Storage`

Daha iyi SRP uyumu için `IDocumentRepository`'den ayrıldı.

#### Metodlar

```csharp
Task<string> GetConversationHistoryAsync(string sessionId);
Task SaveConversationAsync(string sessionId, string history);
Task DeleteConversationAsync(string sessionId);
Task<bool> ConversationExistsAsync(string sessionId);
```

#### Uygulamalar

- `SqliteConversationRepository`
- `InMemoryConversationRepository`
- `FileSystemConversationRepository`
- `RedisConversationRepository`

---

### IAIConfigurationService

**Amaç:** AI sağlayıcı yapılandırma yönetimi

**Namespace:** `SmartRAG.Interfaces.AI`

Daha iyi SRP için yapılandırma yürütmeden ayrıldı.

#### Metodlar

```csharp
AIProvider GetProvider();
string GetModel();
string GetEmbeddingModel();
int GetMaxTokens();
double GetTemperature();
```

---

### IAIRequestExecutor

**Amaç:** Yeniden deneme/yedekleme ile AI istek yürütme

**Namespace:** `SmartRAG.Interfaces.AI`

Otomatik yeniden deneme ve yedekleme mantığı ile AI isteklerini işler.

#### Metodlar

```csharp
Task<string> ExecuteRequestAsync(string prompt, CancellationToken cancellationToken = default);
Task<List<float>> ExecuteEmbeddingRequestAsync(string text, CancellationToken cancellationToken = default);
```

---

### IQueryIntentAnalyzer

**Amaç:** Veritabanı yönlendirmesi için sorgu niyet analizi

**Namespace:** `SmartRAG.Interfaces.Database`

Veritabanı yönlendirme stratejisini belirlemek için sorguları analiz eder.

#### Metodlar

```csharp
Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery);
```

---

### IDatabaseQueryExecutor

**Amaç:** Birden fazla veritabanında sorgu yürütme

**Namespace:** `SmartRAG.Interfaces.Database`

Veritabanları arasında paralel sorgu yürütme.

#### Metodlar

```csharp
Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(QueryIntent queryIntent);
```

---

### IResultMerger

**Amaç:** Birden fazla veritabanından sonuçları birleştirme

**Namespace:** `SmartRAG.Interfaces.Database`

AI destekli sonuç birleştirme.

#### Metodlar

##### MergeResultsAsync

Birden fazla veritabanından gelen sonuçları tutarlı bir yanıta birleştirir.

```csharp
Task<string> MergeResultsAsync(MultiDatabaseQueryResult queryResults, string originalQuery)
```

**Parametreler:**
- `queryResults` (MultiDatabaseQueryResult): Birden fazla veritabanından gelen sonuçlar
- `originalQuery` (string): Orijinal kullanıcı sorgusu

**Döndürür:** Birleştirilmiş ve formatlanmış sonuçlar string olarak

##### GenerateFinalAnswerAsync

Birleştirilmiş veritabanı sonuçlarından nihai AI yanıtı oluşturur.

```csharp
Task<RagResponse> GenerateFinalAnswerAsync(
    string userQuery, 
    string mergedData, 
    MultiDatabaseQueryResult queryResults
)
```

**Parametreler:**
- `userQuery` (string): Orijinal kullanıcı sorgusu
- `mergedData` (string): Veritabanlarından birleştirilmiş veri
- `queryResults` (MultiDatabaseQueryResult): Sorgu sonuçları

**Döndürür:** AI üretilmiş yanıt içeren `RagResponse`

---

### ISQLQueryGenerator

**Amaç:** SQL sorguları oluşturma ve doğrulama

**Namespace:** `SmartRAG.Interfaces.Database`

Veritabanına özgü SQL için `ISqlDialectStrategy` kullanır.

#### Metodlar

```csharp
Task<string> GenerateSqlAsync(string userQuery, DatabaseSchemaInfo schema, DatabaseType databaseType);
bool ValidateSql(string sql, DatabaseSchemaInfo schema, out string errorMessage);
```

---

### IEmbeddingSearchService

**Amaç:** Embedding tabanlı semantik arama

**Namespace:** `SmartRAG.Interfaces.Search`

Temel embedding arama işlevselliği.

#### Metodlar

```csharp
Task<List<DocumentChunk>> SearchByEmbeddingAsync(List<float> queryEmbedding, int maxResults = 5);
```

---

### ISourceBuilderService

**Amaç:** Arama sonucu kaynakları oluşturma

**Namespace:** `SmartRAG.Interfaces.Search`

Chunk'lardan `SearchSource` nesneleri oluşturur.

#### Metodlar

```csharp
List<SearchSource> BuildSources(List<DocumentChunk> chunks);
```

---

### IAudioParserService

**Amaç:** Ses dosyası ayrıştırma ve transkripsiyon

**Namespace:** `SmartRAG.Interfaces.Parser`

#### Metodlar

```csharp
Task<AudioTranscriptionResult> TranscribeAudioAsync(Stream audioStream, string fileName, string language = null);
```

**Parametreler:**
- `audioStream` (Stream): Transkripsiyon yapılacak ses stream'i
- `fileName` (string): Format algılama için ses dosyasının adı
- `language` (string, opsiyonel): Transkripsiyon için dil kodu (örn: "tr-TR", "en-US", "auto")

**Döndürür:** Transkripsiyon edilmiş metin, güven skoru ve metadata içeren `AudioTranscriptionResult`

---

### IImageParserService

**Amaç:** Görüntü OCR işleme

**Namespace:** `SmartRAG.Interfaces.Parser`

#### Metodlar

```csharp
Task<string> ExtractTextFromImageAsync(Stream imageStream, string language = null);
Task<OcrResult> ExtractTextWithConfidenceAsync(Stream imageStream, string language = null);
Task<Stream> PreprocessImageAsync(Stream imageStream);
string CorrectCurrencySymbols(string text, string language = null);
```

**Parametreler:**
- `imageStream` (Stream): İşlenecek görüntü stream'i
- `language` (string, opsiyonel): OCR için dil kodu (örn: "eng", "tur"). Null ise sistem yerel ayarını otomatik kullanır
- `text` (string): Para birimi sembollerini düzeltilecek metin

**Döndürür:**
- `ExtractTextFromImageAsync`: String olarak çıkarılmış metin
- `ExtractTextWithConfidenceAsync`: Metin, güven skorları ve metin blokları içeren `OcrResult`
- `PreprocessImageAsync`: Ön işlenmiş görüntü stream'i
- `CorrectCurrencySymbols`: Düzeltilmiş para birimi sembolleri ile metin (örn: % → ₺, $, €)

---

### IAIProvider

**Amaç:** Metin üretimi ve embedding'ler için düşük seviye AI sağlayıcı arayüzü

**Namespace:** `SmartRAG.Interfaces.AI`

Birden fazla AI backend için sağlayıcı soyutlaması.

#### Metodlar

```csharp
Task<string> GenerateTextAsync(string prompt, AIProviderConfig config);
Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config);
Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config);
Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000);
```

---

### IAIProviderFactory

**Amaç:** AI sağlayıcı örnekleri oluşturmak için fabrika

**Namespace:** `SmartRAG.Interfaces.AI`

AI sağlayıcı oluşturma için fabrika deseni.

#### Metodlar

```csharp
IAIProvider CreateProvider(AIProvider providerType);
```

---

### IPromptBuilderService

**Amaç:** Farklı senaryolar için AI prompt'ları oluşturmak için servis

**Namespace:** `SmartRAG.Interfaces.AI`

Konuşma geçmişi desteği ile merkezi prompt oluşturma.

#### Metodlar

```csharp
string BuildDocumentRagPrompt(string query, string context, string? conversationHistory = null, string? preferredLanguage = null);
string BuildHybridMergePrompt(string query, string? databaseContext, string? documentContext, string? conversationHistory = null, string? preferredLanguage = null);
string BuildConversationPrompt(string query, string? conversationHistory = null, string? preferredLanguage = null);
```

**Parametreler:**
- `query` (string): Kullanıcı sorgusu
- `context` (string): Doküman context'i (BuildDocumentRagPrompt için)
- `databaseContext` (string?, opsiyonel): Veritabanı sorgu sonuçları (BuildHybridMergePrompt için)
- `documentContext` (string?, opsiyonel): Doküman arama sonuçları (BuildHybridMergePrompt için)
- `conversationHistory` (string?, opsiyonel): Önceki konuşma turları
- `preferredLanguage` (string?, opsiyonel): AI yanıt dili için tercih edilen dil kodu (örn: "tr", "en")

---

### IDocumentRepository

**Amaç:** Doküman depolama işlemleri için repository arayüzü

**Namespace:** `SmartRAG.Interfaces.Document`

İş mantığından ayrılmış repository katmanı.

#### Metodlar

##### AddAsync

Depolamaya yeni bir doküman ekler.

```csharp
Task<Document> AddAsync(Document document)
```

##### GetByIdAsync

Benzersiz tanımlayıcıya göre dokümanı alır.

```csharp
Task<Document> GetByIdAsync(Guid id)
```

##### GetAllAsync

Depolamadan tüm dokümanları alır.

```csharp
Task<List<Document>> GetAllAsync()
```

##### DeleteAsync

ID'ye göre depolamadan dokümanı kaldırır.

```csharp
Task<bool> DeleteAsync(Guid id)
```

##### GetCountAsync

Depolamadaki toplam doküman sayısını alır.

```csharp
Task<int> GetCountAsync()
```

##### SearchAsync

Sorgu string'i kullanarak dokümanları arar.

```csharp
Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5)
```

**Parametreler:**
- `query` (string): Arama sorgu string'i
- `maxResults` (int): Döndürülecek maksimum sonuç sayısı (varsayılan: 5)

**Döndürür:** İlgili doküman chunk'larının listesi

##### ClearAllAsync

Depolamadan tüm dokümanları temizler (verimli toplu silme).

```csharp
Task<bool> ClearAllAsync()
```

**Döndürür:** Tüm dokümanlar başarıyla temizlendiyse true

---

### IDocumentScoringService

**Amaç:** Sorgu ilgisine göre doküman parçalarını puanlamak için servis

**Namespace:** `SmartRAG.Interfaces.Document`

Anahtar kelime ve semantik ilgi ile hibrit puanlama stratejisi.

#### Metodlar

```csharp
List<DocumentChunk> ScoreChunks(List<DocumentChunk> chunks, string query, List<string> queryWords, List<string> potentialNames);
double CalculateKeywordRelevanceScore(string query, string content);
```

---

### IAudioParserFactory

**Amaç:** Ses ayrıştırıcı servis örnekleri oluşturmak için fabrika

**Namespace:** `SmartRAG.Interfaces.Parser`

Ses ayrıştırıcı oluşturma için fabrika deseni.

#### Metodlar

```csharp
IAudioParserService CreateAudioParser(AudioProvider provider);
```

---

### IStorageFactory

**Amaç:** Doküman ve konuşma depolama repository'leri oluşturmak için fabrika

**Namespace:** `SmartRAG.Interfaces.Storage`

Tüm depolama işlemleri için birleşik fabrika.

#### Metodlar

```csharp
IDocumentRepository CreateRepository(StorageConfig config);
IDocumentRepository CreateRepository(StorageProvider provider);
StorageProvider GetCurrentProvider();
IDocumentRepository GetCurrentRepository();
IConversationRepository CreateConversationRepository(StorageConfig config);
IConversationRepository CreateConversationRepository(StorageProvider provider);
IConversationRepository GetCurrentConversationRepository();
```

---

### IQdrantCacheManager

**Amaç:** Qdrant işlemlerinde arama sonuçlarını önbelleğe almak için arayüz

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Performans optimizasyonu için arama sonuçlarını önbelleğe alma.

#### Metodlar

```csharp
List<DocumentChunk> GetCachedResults(string queryHash);
void CacheResults(string queryHash, List<DocumentChunk> results);
void CleanupExpiredCache();
```

---

### IQdrantCollectionManager

**Amaç:** Qdrant koleksiyonlarını ve doküman depolamayı yönetmek için arayüz

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Qdrant vektör veritabanı için koleksiyon yaşam döngüsü yönetimi.

#### Metodlar

```csharp
Task EnsureCollectionExistsAsync();
Task CreateCollectionAsync(string collectionName, int vectorDimension);
Task EnsureDocumentCollectionExistsAsync(string collectionName, Document document);
Task<int> GetVectorDimensionAsync();
```

---

### IQdrantEmbeddingService

**Amaç:** Metin içeriği için embedding'ler oluşturmak için arayüz

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Qdrant vektör depolama için embedding oluşturma.

#### Metodlar

```csharp
Task<List<float>> GenerateEmbeddingAsync(string text);
Task<int> GetVectorDimensionAsync();
```

---

### IQdrantSearchService

**Amaç:** Qdrant vektör veritabanında arama yapmak için arayüz

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Qdrant için vektör, metin ve hibrit arama yetenekleri.

#### Metodlar

```csharp
Task<List<DocumentChunk>> SearchAsync(List<float> queryEmbedding, int maxResults);
Task<List<DocumentChunk>> FallbackTextSearchAsync(string query, int maxResults);
Task<List<DocumentChunk>> HybridSearchAsync(string query, int maxResults);
```

---

### IQueryIntentClassifierService

**Amaç:** Sorgu niyetini sınıflandırmak için servis (konuşma vs bilgi)

**Namespace:** `SmartRAG.Interfaces.Support`

Hibrit yönlendirme için AI tabanlı sorgu niyet sınıflandırması.

#### Metodlar

```csharp
Task<bool> IsGeneralConversationAsync(string query, string? conversationHistory = null);
bool TryParseCommand(string input, out QueryCommandType commandType, out string payload);
```

**Komut Türleri:**
- `QueryCommandType.None`: Komut algılanmadı
- `QueryCommandType.NewConversation`: `/new` veya `/reset` komutu
- `QueryCommandType.ForceConversation`: `/conv` komutu

---

### ITextNormalizationService

**Amaç:** Metin normalizasyonu ve temizleme

**Namespace:** `SmartRAG.Interfaces.Support`

#### Metodlar

```csharp
string NormalizeText(string text);
string RemoveExtraWhitespace(string text);
```

---