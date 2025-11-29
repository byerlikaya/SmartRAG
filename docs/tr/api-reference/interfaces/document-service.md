---
layout: default
title: IDocumentService
description: IDocumentService arayüz dokümantasyonu
lang: tr
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


## İlgili Arayüzler

- [Temel Arayüzler]({{ site.baseurl }}/tr/api-reference/core) - Tüm temel arayüzleri görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

