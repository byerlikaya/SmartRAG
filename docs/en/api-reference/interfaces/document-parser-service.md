---
layout: default
title: IDocumentParserService
description: IDocumentParserService interface documentation
lang: en
---
## IDocumentParserService

**Purpose:** Multi-format document parsing and text extraction

**Namespace:** `SmartRAG.Interfaces.Document`

### Methods

#### ParseDocumentAsync

Parse a document and create document entity.

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

Get list of supported file extensions.

```csharp
IEnumerable<string> GetSupportedFileTypes()
```

**Returns:**
- `.pdf`, `.docx`, `.doc`
- `.xlsx`, `.xls`
- `.txt`, `.md`, `.json`, `.xml`, `.csv`
- `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.tiff`, `.webp`
- `.mp3`, `.wav`, `.m4a`, `.aac`, `.ogg`, `.flac`, `.wma`
- `.db`, `.sqlite`, `.sqlite3`

#### GetSupportedContentTypes

Get list of supported MIME content types.

```csharp
IEnumerable<string> GetSupportedContentTypes()
```


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

