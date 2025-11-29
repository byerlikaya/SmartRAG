---
layout: default
title: Veri Modelleri
description: SmartRAG veri modelleri - RagResponse, Document, DocumentChunk, DatabaseConfig ve diğer veri yapıları
lang: tr
---

## Veri Modelleri

### RagResponse

Kaynaklarla AI tarafından üretilen yanıt.

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
  "answer": "RAG (Retrieval-Augmented Generation) şudur...",
  "sources": [
    {
      "documentId": "abc-123",
      "fileName": "ml-rehberi.pdf",
      "chunkContent": "RAG retrieval'ı birleştirir...",
      "relevanceScore": 0.92
    }
  ],
  "searchedAt": "2025-10-18T14:30:00Z"
}
```

### DocumentChunk

İlgili skor ile doküman chunk'ı.

```csharp
public class DocumentChunk
{
    public string Id { get; set; }               // Chunk ID
    public string DocumentId { get; set; }       // Ana doküman ID
    public string Content { get; set; }          // Chunk metin içeriği
    public List<float> Embedding { get; set; }   // Vektör embedding
    public double RelevanceScore { get; set; }   // Benzerlik skoru (0-1)
    public int ChunkIndex { get; set; }          // Dokümandaki konum
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
    public long FileSize { get; set; }           // Bayt cinsinden dosya boyutu
    public DateTime UploadedAt { get; set; }     // Yükleme zaman damgası
    public string UploadedBy { get; set; }        // Kullanıcı tanımlayıcısı
    public string Content { get; set; }           // Çıkarılan metin içeriği
    public List<DocumentChunk> Chunks { get; set; } // Doküman chunk'ları
}
```

### DatabaseConfig

Veritabanı bağlantı yapılandırması.

```csharp
public class DatabaseConfig
{
    public DatabaseType Type { get; set; }              // Veritabanı tipi
    public string ConnectionString { get; set; }        // Bağlantı dizesi
    public List<string> IncludedTables { get; set; }    // Dahil edilecek tablolar
    public List<string> ExcludedTables { get; set; }    // Hariç tutulacak tablolar
    public int MaxRowsPerTable { get; set; } = 1000;    // Satır limiti
    public bool SanitizeSensitiveData { get; set; } = true; // Hassas kolonları temizle
    public List<string> SensitiveColumns { get; set; }  // Temizlenecek kolonlar
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

