---
layout: default
title: Veri Modelleri
description: SmartRAG veri modelleri - RagResponse, Document, DocumentChunk, DatabaseConfig ve diğer veri yapıları
lang: tr
---

## Veri Modelleri

### RagResponse

Kaynaklar ve yapılandırma metadata'sı ile AI tarafından üretilen yanıt.

```csharp
public class RagResponse
{
    public string Query { get; set; }                    // Orijinal sorgu
    public string Answer { get; set; }                   // AI tarafından üretilen cevap
    public List<SearchSource> Sources { get; set; }       // Kaynak dokümanlar
    public DateTime SearchedAt { get; set; }             // Zaman damgası
    public RagConfiguration Configuration { get; set; }  // Sağlayıcı yapılandırması
}
```

**RagConfiguration:**

```csharp
public class RagConfiguration
{
    public string AIProvider { get; set; }      // Kullanılan AI sağlayıcı
    public string StorageProvider { get; set; }  // Kullanılan depolama sağlayıcı
    public string Model { get; set; }            // Kullanılan model adı
}
```

**Örnek Yanıt:**

```json
{
  "query": "RAG nedir?",
  "answer": "RAG (Retrieval-Augmented Generation) şudur...",
  "sources": [
    {
      "sourceType": "Document",
      "documentId": "abc-123",
      "fileName": "ml-rehberi.pdf",
      "relevantContent": "RAG retrieval'ı birleştirir...",
      "relevanceScore": 0.92
    }
  ],
  "searchedAt": "2025-10-18T14:30:00Z",
  "configuration": {
    "aiProvider": "OpenAI",
    "storageProvider": "Qdrant",
    "model": "gpt-4"
  }
}
```

### Document

Metadata ve chunk'lar ile doküman varlığı.

```csharp
public class Document
{
    public Guid Id { get; set; }                          // Doküman ID
    public string FileName { get; set; }                   // Orijinal dosya adı
    public string ContentType { get; set; }                // MIME tipi
    public string Content { get; set; }                    // Çıkarılan metin içeriği
    public string UploadedBy { get; set; }                 // Kullanıcı tanımlayıcısı
    public DateTime UploadedAt { get; set; }              // Yükleme zaman damgası
    public List<DocumentChunk> Chunks { get; set; }        // Doküman chunk'ları
    public Dictionary<string, object> Metadata { get; set; } // İsteğe bağlı metadata
    public long FileSize { get; set; }                     // Dosya boyutu (bayt)
}
```

### DocumentChunk

Embedding ve ilgili skor ile doküman chunk'ı.

```csharp
public class DocumentChunk
{
    public Guid Id { get; set; }                          // Chunk ID
    public Guid DocumentId { get; set; }                 // Ana doküman ID
    public string Content { get; set; }                   // Chunk metin içeriği
    public int ChunkIndex { get; set; }                    // Dokümandaki konum
    public List<float> Embedding { get; set; }             // Vektör embedding
    public double? RelevanceScore { get; set; }            // Benzerlik skoru (0-1)
    public DateTime CreatedAt { get; set; }                // Oluşturulma zaman damgası
    public int StartPosition { get; set; }                 // Dokümandaki başlangıç konumu
    public int EndPosition { get; set; }                  // Dokümandaki bitiş konumu
}
```

### SearchSource

Doküman bilgisi ve ilgili skor ile arama sonucu kaynağını temsil eder.

```csharp
public class SearchSource
{
    public string SourceType { get; set; }                // Tip: Document, Audio, Database, Image, System
    public Guid DocumentId { get; set; }                  // Doküman ID (varsa)
    public string FileName { get; set; }                  // Dosya adı
    public string RelevantContent { get; set; }           // İlgili içerik özeti
    public double RelevanceScore { get; set; }             // İlgili skor (0-1)
    public string? Location { get; set; }                  // Konum metadata'sı
    public string? DatabaseId { get; set; }                // Veritabanı ID (varsa)
    public string? DatabaseName { get; set; }              // Veritabanı adı (varsa)
    public List<string> Tables { get; set; }               // Referans verilen tablolar (varsa)
    public string? ExecutedQuery { get; set; }             // Çalıştırılan sorgu (varsa)
    public int? RowNumber { get; set; }                    // Satır numarası (varsa)
    public double? StartTimeSeconds { get; set; }          // Ses için başlangıç zamanı (varsa)
    public double? EndTimeSeconds { get; set; }            // Ses için bitiş zamanı (varsa)
    public int? ChunkIndex { get; set; }                   // Chunk indeksi (varsa)
    public int? StartPosition { get; set; }                // Başlangıç konumu (varsa)
    public int? EndPosition { get; set; }                  // Bitiş konumu (varsa)
}
```

### SearchOptions

Belirli bir arama isteğini yapılandırmak için seçenekler.

```csharp
public class SearchOptions
{
    public bool EnableDatabaseSearch { get; set; } = true;    // Veritabanı aramasını etkinleştir
    public bool EnableDocumentSearch { get; set; } = true;    // Doküman aramasını etkinleştir
    public bool EnableAudioSearch { get; set; } = true;        // Ses aramasını etkinleştir
    public bool EnableImageSearch { get; set; } = true;       // Görüntü aramasını etkinleştir
    public string? PreferredLanguage { get; set; }             // Tercih edilen dil (ISO 639-1)
    
    public static SearchOptions Default => new SearchOptions();
    public static SearchOptions FromConfig(SmartRagOptions options) { ... }
}
```

### QueryIntent

Çoklu veritabanı sorgulama için AI tarafından analiz edilen sorgu niyetini temsil eder.

```csharp
public class QueryIntent
{
    public string OriginalQuery { get; set; }                    // Orijinal kullanıcı sorgusu
    public string QueryUnderstanding { get; set; }               // AI'nın anlayışı
    public List<DatabaseQueryIntent> DatabaseQueries { get; set; } // Gerekli sorgular
    public double Confidence { get; set; }                        // Güven seviyesi (0-1)
    public bool RequiresCrossDatabaseJoin { get; set; }          // Cross-DB join gerekli
    public string Reasoning { get; set; }                        // AI mantığı
}
```

**DatabaseQueryIntent:**

```csharp
public class DatabaseQueryIntent
{
    public string DatabaseId { get; set; }                       // Veritabanı ID
    public string DatabaseName { get; set; }                     // Veritabanı adı
    public List<string> RequiredTables { get; set; }             // Sorgulanacak tablolar
    public Dictionary<string, List<string>> RequiredColumns { get; set; } // Gerekli kolonlar
    public string GeneratedQuery { get; set; }                   // Üretilen SQL
    public string Purpose { get; set; }                           // Sorgu amacı
    public int Priority { get; set; } = 1;                      // Öncelik (yüksek = daha önemli)
}
```

### DatabaseConfig

Veritabanı ayrıştırma işlemleri için yapılandırma.

```csharp
public class DatabaseConfig
{
    public DatabaseType Type { get; set; }                      // Veritabanı tipi
    public string ConnectionString { get; set; }                 // Bağlantı dizesi
    public List<string> IncludedTables { get; set; }             // Dahil edilecek tablolar
    public List<string> ExcludedTables { get; set; }             // Hariç tutulacak tablolar
    public int MaxRowsPerTable { get; set; } = 1000;             // Tablo başına maksimum satır
    public bool IncludeSchema { get; set; } = true;              // Şema bilgisi dahil et
    public bool IncludeIndexes { get; set; } = false;            // İndeks bilgisi dahil et
    public bool IncludeForeignKeys { get; set; } = true;         // Foreign key'leri dahil et
    public int QueryTimeoutSeconds { get; set; } = 30;           // Sorgu zaman aşımı
    public bool SanitizeSensitiveData { get; set; } = true;     // Hassas verileri temizle
    public List<string> SensitiveColumns { get; set; }            // Hassas kolon desenleri
    public bool EnableConnectionPooling { get; set; } = true;     // Bağlantı havuzlamayı etkinleştir
    public int MaxPoolSize { get; set; } = 10;                   // Maksimum havuz boyutu
    public int MinPoolSize { get; set; } = 2;                    // Minimum havuz boyutu
    public bool EnableQueryCaching { get; set; } = true;        // Sorgu önbelleğini etkinleştir
    public int CacheDurationMinutes { get; set; } = 30;         // Önbellek süresi
    public bool EnableParallelProcessing { get; set; } = true;   // Paralel işlemeyi etkinleştir
    public int MaxDegreeOfParallelism { get; set; } = 3;        // Maksimum paralellik
    public bool EnableStreaming { get; set; } = true;            // Streaming'i etkinleştir
    public int StreamingBatchSize { get; set; } = 1000;         // Streaming batch boyutu
    public int MaxMemoryThresholdMB { get; set; } = 500;         // Bellek eşiği
    public bool EnableAutoGarbageCollection { get; set; } = true; // Otomatik GC
    public bool ForceStreamingMode { get; set; } = false;       // Streaming modunu zorla
    public int MaxStringBuilderCapacity { get; set; } = 65536;  // String builder kapasitesi
}
```

### DatabaseConnectionConfig

Veritabanı bağlantısı için yapılandırma.

```csharp
public class DatabaseConnectionConfig
{
    public string Name { get; set; }                            // İsteğe bağlı bağlantı adı
    public string ConnectionString { get; set; }                // Bağlantı dizesi
    public DatabaseType DatabaseType { get; set; }               // Veritabanı tipi
    public string Description { get; set; }                     // İsteğe bağlı açıklama
    public bool Enabled { get; set; } = true;                    // Etkin mi
    public int MaxRowsPerQuery { get; set; }                     // Sorgu başına maksimum satır
    public int QueryTimeoutSeconds { get; set; }                 // Sorgu zaman aşımı
    public int SchemaRefreshIntervalMinutes { get; set; } = 0;   // Otomatik yenileme aralığı
    public string[] IncludedTables { get; set; }                 // Dahil edilecek tablolar
    public string[] ExcludedTables { get; set; }                 // Hariç tutulacak tablolar
}
```

### DatabaseConnectionRequest

Veritabanı bağlantı işlemleri için istek modeli.

```csharp
public class DatabaseConnectionRequest
{
    public string ConnectionString { get; set; }                // Bağlantı dizesi
    public DatabaseType DatabaseType { get; set; }               // Veritabanı tipi
    public List<string> IncludedTables { get; set; }              // Dahil edilecek tablolar
    public List<string> ExcludedTables { get; set; }              // Hariç tutulacak tablolar
    public int MaxRows { get; set; } = 1000;                      // Maksimum satır
    public bool IncludeSchema { get; set; } = true;              // Şema dahil et
    public bool IncludeForeignKeys { get; set; } = true;          // Foreign key'leri dahil et
    public bool SanitizeSensitiveData { get; set; } = true;      // Hassas verileri temizle
}
```

### DatabaseSchemaInfo

Veritabanı için kapsamlı şema bilgisi.

```csharp
public class DatabaseSchemaInfo
{
    public string DatabaseId { get; set; }                      // Benzersiz veritabanı ID
    public string DatabaseName { get; set; }                     // Veritabanı adı
    public DatabaseType DatabaseType { get; set; }               // Veritabanı tipi
    public string Description { get; set; }                      // Açıklama
    public DateTime LastAnalyzed { get; set; }                   // Son analiz zamanı
    public List<TableSchemaInfo> Tables { get; set; }           // Tablolar
    public string AISummary { get; set; }                        // AI tarafından üretilen özet
    public long TotalRowCount { get; set; }                      // Toplam satır sayısı
    public SchemaAnalysisStatus Status { get; set; }              // Analiz durumu
    public string ErrorMessage { get; set; }                     // Hata mesajı (başarısızsa)
}
```

**TableSchemaInfo:**

```csharp
public class TableSchemaInfo
{
    public string TableName { get; set; }                        // Tablo adı
    public List<ColumnSchemaInfo> Columns { get; set; }          // Kolonlar
    public List<string> PrimaryKeys { get; set; }                 // Primary key'ler
    public List<ForeignKeyInfo> ForeignKeys { get; set; }         // Foreign key'ler
    public long RowCount { get; set; }                           // Satır sayısı
    public string AIDescription { get; set; }                    // AI açıklaması
    public string SampleData { get; set; }                       // Örnek veri
}
```

**ColumnSchemaInfo:**

```csharp
public class ColumnSchemaInfo
{
    public string ColumnName { get; set; }                       // Kolon adı
    public string DataType { get; set; }                         // Veri tipi
    public bool IsNullable { get; set; }                          // Nullable mı
    public bool IsPrimaryKey { get; set; }                       // Primary key mi
    public bool IsForeignKey { get; set; }                       // Foreign key mi
    public int? MaxLength { get; set; }                          // Maksimum uzunluk (string'ler için)
}
```

**ForeignKeyInfo:**

```csharp
public class ForeignKeyInfo
{
    public string ForeignKeyName { get; set; }                  // Foreign key adı
    public string ColumnName { get; set; }                       // Mevcut tablodaki kolon
    public string ReferencedTable { get; set; }                  // Referans verilen tablo
    public string ReferencedColumn { get; set; }                 // Referans verilen kolon
}
```

**SchemaAnalysisStatus:**

```csharp
public enum SchemaAnalysisStatus
{
    Pending,        // Analiz bekleniyor
    InProgress,     // Analiz devam ediyor
    Completed,      // Analiz tamamlandı
    Failed,         // Analiz başarısız
    RefreshNeeded   // Şema yenileme gerekli
}
```

### SmartRagOptions

SmartRag kütüphanesi için yapılandırma seçenekleri.

```csharp
public class SmartRagOptions
{
    public AIProvider AIProvider { get; set; }                   // AI sağlayıcı
    public StorageProvider StorageProvider { get; set; }         // Depolama sağlayıcı
    public ConversationStorageProvider? ConversationStorageProvider { get; set; } // Konuşma depolama
    public int MaxChunkSize { get; set; } = 1000;                 // Maksimum chunk boyutu
    public int MinChunkSize { get; set; } = 100;                  // Minimum chunk boyutu
    public int ChunkOverlap { get; set; } = 200;                  // Chunk örtüşmesi
    public int MaxRetryAttempts { get; set; } = 3;                // Maksimum yeniden deneme
    public int RetryDelayMs { get; set; } = 1000;                 // Yeniden deneme gecikmesi
    public RetryPolicy RetryPolicy { get; set; }                 // Yeniden deneme politikası
    public bool EnableFallbackProviders { get; set; }            // Fallback sağlayıcıları etkinleştir
    public List<AIProvider> FallbackProviders { get; set; }     // Fallback sağlayıcıları
    public AudioProvider AudioProvider { get; set; }              // Ses sağlayıcı
    public WhisperConfig WhisperConfig { get; set; }             // Whisper yapılandırması
    public List<DatabaseConnectionConfig> DatabaseConnections { get; set; } // Veritabanı bağlantıları
    public bool EnableAutoSchemaAnalysis { get; set; } = true;   // Otomatik şema analizi
    public bool EnablePeriodicSchemaRefresh { get; set; } = true; // Periyodik yenileme
    public int DefaultSchemaRefreshIntervalMinutes { get; set; } = 60; // Yenileme aralığı
    public FeatureToggles Features { get; set; }                 // Özellik anahtarları
}
```

**FeatureToggles:**

```csharp
public class FeatureToggles
{
    public bool EnableDatabaseSearch { get; set; } = true;       // Veritabanı aramasını etkinleştir
    public bool EnableDocumentSearch { get; set; } = true;      // Doküman aramasını etkinleştir
    public bool EnableAudioParsing { get; set; } = true;         // Ses ayrıştırmayı etkinleştir
    public bool EnableImageParsing { get; set; } = true;         // Görüntü ayrıştırmayı etkinleştir
}
```

### AudioTranscriptionResult

Ses transkripsiyon işleminin sonucunu temsil eder.

```csharp
public class AudioTranscriptionResult
{
    public string Text { get; set; }                             // Transkribe edilmiş metin
    public double Confidence { get; set; }                        // Güven skoru (0-1)
    public string Language { get; set; }                         // Algılanan dil
    public Dictionary<string, object> Metadata { get; set; }      // Ek metadata
}
```

### AudioTranscriptionOptions

Ses transkripsiyon işlemi için yapılandırma seçenekleri.

```csharp
public class AudioTranscriptionOptions
{
    public string Language { get; set; } = "tr-TR";              // Dil kodu
    public double MinConfidenceThreshold { get; set; } = 0.5;    // Minimum güven (0-1)
    public bool IncludeWordTimestamps { get; set; } = false;     // Kelime zaman damgalarını dahil et
}
```

### AudioSegmentMetadata

Tek bir ses transkripsiyon segmenti için metadata'yı temsil eder.

```csharp
public class AudioSegmentMetadata
{
    public double Start { get; set; }                           // Başlangıç zamanı (saniye)
    public double End { get; set; }                             // Bitiş zamanı (saniye)
    public string Text { get; set; }                             // Transkribe edilmiş metin
    public double Probability { get; set; }                     // Güven olasılığı
    public string NormalizedText { get; set; }                   // Normalize edilmiş metin
    public int StartCharIndex { get; set; }                      // Başlangıç karakter indeksi
    public int EndCharIndex { get; set; }                        // Bitiş karakter indeksi
}
```

### OcrResult

OCR işleminin sonucunu temsil eder.

```csharp
public class OcrResult
{
    public string Text { get; set; }                             // Çıkarılan metin
    public float Confidence { get; set; }                        // Güven skoru
    public long ProcessingTimeMs { get; set; }                   // İşleme süresi (ms)
    public int WordCount { get; set; }                           // Kelime sayısı
    public string Language { get; set; }                         // Kullanılan dil
}
```

### AIProviderConfig

AI sağlayıcıları için yapılandırma.

```csharp
public class AIProviderConfig
{
    public string ApiKey { get; set; }                            // API anahtarı
    public string EmbeddingApiKey { get; set; }                // Embedding API anahtarı (isteğe bağlı)
    public string Endpoint { get; set; }                         // Özel endpoint (isteğe bağlı)
    public string EmbeddingEndpoint { get; set; }                // Embedding endpoint (isteğe bağlı)
    public string ApiVersion { get; set; }                       // API versiyonu (isteğe bağlı)
    public string Model { get; set; }                            // Model adı
    public string EmbeddingModel { get; set; }                   // Embedding model (isteğe bağlı)
    public int MaxTokens { get; set; } = 4096;                   // Maksimum token
    public double Temperature { get; set; } = 0.7;               // Temperature (0-1)
    public string SystemMessage { get; set; }                    // Sistem mesajı (isteğe bağlı)
    public int? EmbeddingMinIntervalMs { get; set; }             // Embedding minimum aralık (ms)
}
```

### WhisperConfig

Whisper ses transkripsiyonu için yapılandırma.

```csharp
public class WhisperConfig
{
    public string ModelPath { get; set; } = "models/ggml-base.bin"; // Model dosya yolu
    public string DefaultLanguage { get; set; } = "auto";         // Varsayılan dil
    public double MinConfidenceThreshold { get; set; } = 0.3;     // Minimum güven (0-1)
    public bool IncludeWordTimestamps { get; set; } = false;       // Kelime zaman damgalarını dahil et
    public string PromptHint { get; set; } = string.Empty;        // Bağlam ipucu
    public int MaxThreads { get; set; } = 0;                      // Maksimum thread (0 = otomatik)
}
```

### GoogleSpeechConfig

Google Speech-to-Text servisi için yapılandırma seçenekleri.

```csharp
public class GoogleSpeechConfig
{
    public string ApiKey { get; set; }                           // API anahtarı veya service account JSON yolu
    public string DefaultLanguage { get; set; } = "tr-TR";      // Varsayılan dil
    public double MinConfidenceThreshold { get; set; } = 0.5;    // Minimum güven (0-1)
    public bool IncludeWordTimestamps { get; set; } = false;      // Kelime zaman damgalarını dahil et
    public bool EnableAutomaticPunctuation { get; set; } = true;  // Otomatik noktalama
    public bool EnableSpeakerDiarization { get; set; } = false;   // Konuşmacı diarizasyonu
    public int MaxSpeakerCount { get; set; } = 2;                 // Maksimum konuşmacı sayısı
}
```

### RedisConfig

Redis depolama yapılandırması.

```csharp
public class RedisConfig
{
    public string ConnectionString { get; set; } = "localhost:6379"; // Bağlantı dizesi
    public string Password { get; set; }                            // Şifre
    public string Username { get; set; }                             // Kullanıcı adı
    public int Database { get; set; }                                // Veritabanı numarası
    public string KeyPrefix { get; set; } = "smartrag:doc:";        // Anahtar öneki
    public int ConnectionTimeout { get; set; } = 30;                // Bağlantı zaman aşımı (saniye)
    public bool EnableSsl { get; set; }                              // SSL'i etkinleştir
    public bool UseSsl { get; set; }                                 // SSL kullan (alias)
    public bool EnableVectorSearch { get; set; } = true;             // Vektör aramayı etkinleştir
    public string VectorIndexAlgorithm { get; set; } = "HNSW";      // Vektör indeks algoritması
    public string DistanceMetric { get; set; } = "COSINE";          // Mesafe metriği
    public int VectorDimension { get; set; } = 768;                  // Vektör boyutu
    public string VectorIndexName { get; set; } = "smartrag_vector_idx"; // Vektör indeks adı
    public int RetryCount { get; set; } = 3;                         // Yeniden deneme sayısı
    public int RetryDelay { get; set; } = 1000;                      // Yeniden deneme gecikmesi (ms)
}
```

### QdrantConfig

Qdrant vektör veritabanı depolama için yapılandırma ayarları.

```csharp
public class QdrantConfig
{
    public string Host { get; set; } = "localhost";              // Sunucu host'u
    public bool UseHttps { get; set; }                            // HTTPS kullan
    public string ApiKey { get; set; } = string.Empty;            // API anahtarı
    public string CollectionName { get; set; } = "smartrag_documents"; // Koleksiyon adı
    public int VectorSize { get; set; } = 768;                     // Vektör boyutu
    public string DistanceMetric { get; set; } = "Cosine";        // Mesafe metriği
}
```

### SqliteConfig

SQLite depolama yapılandırması.

```csharp
public class SqliteConfig
{
    public string DatabasePath { get; set; } = "SmartRag.db";     // Veritabanı dosya yolu
    public bool EnableForeignKeys { get; set; } = true;            // Foreign key'leri etkinleştir
}
```

### InMemoryConfig

Bellek içi depolama yapılandırması.

```csharp
public class InMemoryConfig
{
    public int MaxDocuments { get; set; } = 1000;                  // Bellekte maksimum doküman
}
```

### StorageConfig

Farklı depolama sağlayıcıları için depolama yapılandırması.

```csharp
public class StorageConfig
{
    public StorageProvider Provider { get; set; } = StorageProvider.InMemory; // Depolama sağlayıcı
    public RedisConfig Redis { get; set; } = new RedisConfig();   // Redis yapılandırması
    public InMemoryConfig InMemory { get; set; } = new InMemoryConfig(); // InMemory yapılandırması
    public QdrantConfig Qdrant { get; set; } = new QdrantConfig(); // Qdrant yapılandırması
}
```

## Sonraki Adımlar

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-list"></i>
            </div>
            <h3>Numaralandırmalar</h3>
            <p>AIProvider, StorageProvider, DatabaseType ve diğer enum'lar</p>
            <a href="{{ site.baseurl }}/tr/api-reference/enums" class="btn btn-outline-primary btn-sm mt-3">
                Numaralandırmalar
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-home"></i>
            </div>
            <h3>API Referans</h3>
            <p>API Referans ana sayfasına dön</p>
            <a href="{{ site.baseurl }}/tr/api-reference" class="btn btn-outline-primary btn-sm mt-3">
                API Referans
            </a>
        </div>
    </div>
</div>
