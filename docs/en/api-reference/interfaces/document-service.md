---
layout: default
title: IDocumentService
description: IDocumentService interface documentation
lang: en
---
## IDocumentService

**Purpose:** Document CRUD operations and management

**Namespace:** `SmartRAG.Interfaces.Document`

### Methods

#### UploadDocumentAsync

Upload and process a single document.

```csharp
Task<Document> UploadDocumentAsync(
    Stream fileStream, 
    string fileName, 
    string contentType, 
    string uploadedBy, 
    string language = null
)
```

**Parameters:**
- `fileStream` (Stream): Document file stream
- `fileName` (string): Name of the file
- `contentType` (string): MIME content type
- `uploadedBy` (string): User identifier
- `language` (string, optional): Language code for OCR (e.g., "eng", "tur")

**Supported Formats:**
- PDF: `application/pdf`
- Word: `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
- Excel: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- Images: `image/jpeg`, `image/png`, `image/webp`, etc.
- Audio: `audio/mpeg`, `audio/wav`, etc.
- Databases: `application/x-sqlite3`

**Example:**

```csharp
using var fileStream = File.OpenRead("contract.pdf");

var document = await _documentService.UploadDocumentAsync(
    fileStream,
    "contract.pdf",
    "application/pdf",
    "user-123"
);

Console.WriteLine($"Uploaded: {document.FileName}, Chunks: {document.Chunks.Count}");
```

#### UploadDocumentsAsync

Upload and process multiple documents in batch.

```csharp
Task<List<Document>> UploadDocumentsAsync(
    IEnumerable<Stream> fileStreams, 
    IEnumerable<string> fileNames, 
    IEnumerable<string> contentTypes, 
    string uploadedBy
)
```

#### GetDocumentAsync

Get a document by its ID.

```csharp
Task<Document> GetDocumentAsync(Guid id)
```

#### GetAllDocumentsAsync

Get all uploaded documents.

```csharp
Task<List<Document>> GetAllDocumentsAsync()
```

#### DeleteDocumentAsync

Delete a document and its chunks.

```csharp
Task<bool> DeleteDocumentAsync(Guid id)
```

#### GetStorageStatisticsAsync

Get storage statistics and metrics.

```csharp
Task<Dictionary<string, object>> GetStorageStatisticsAsync()
```

**Example:**

```csharp
var stats = await _documentService.GetStorageStatisticsAsync();

Console.WriteLine($"Total Documents: {stats["TotalDocuments"]}");
Console.WriteLine($"Total Chunks: {stats["TotalChunks"]}");
```

#### RegenerateAllEmbeddingsAsync

Regenerate embeddings for all documents (useful after changing AI provider).

```csharp
Task<bool> RegenerateAllEmbeddingsAsync()
```

#### ClearAllEmbeddingsAsync

Clear all embeddings while keeping document content.

```csharp
Task<bool> ClearAllEmbeddingsAsync()
```

#### ClearAllDocumentsAsync

Clear all documents and their embeddings.

```csharp
Task<bool> ClearAllDocumentsAsync()
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

