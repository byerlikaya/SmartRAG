---
layout: default
title: Data Models
description: SmartRAG data models - RagResponse, Document, DocumentChunk, DatabaseConfig and other data structures
lang: en
---

## Data Models

### RagResponse

AI-generated response with sources.

```csharp
public class RagResponse
{
    public string Query { get; set; }              // Original query
    public string Answer { get; set; }             // AI-generated answer
    public List<SearchSource> Sources { get; set; } // Source documents
    public DateTime SearchedAt { get; set; }       // Timestamp
    public Configuration Configuration { get; set; } // Provider config
}
```

**Example Response:**

```json
{
  "query": "What is RAG?",
  "answer": "RAG (Retrieval-Augmented Generation) is...",
  "sources": [
    {
      "documentId": "abc-123",
      "fileName": "ml-guide.pdf",
      "chunkContent": "RAG combines retrieval...",
      "relevanceScore": 0.92
    }
  ],
  "searchedAt": "2025-10-18T14:30:00Z"
}
```

### DocumentChunk

Document chunk with relevance score.

```csharp
public class DocumentChunk
{
    public string Id { get; set; }               // Chunk ID
    public string DocumentId { get; set; }       // Parent document ID
    public string Content { get; set; }          // Chunk text content
    public List<float> Embedding { get; set; }   // Vector embedding
    public double RelevanceScore { get; set; }   // Similarity score (0-1)
    public int ChunkIndex { get; set; }          // Position in document
}
```

### Document

Document entity with metadata.

```csharp
public class Document
{
    public Guid Id { get; set; }                 // Document ID
    public string FileName { get; set; }         // Original file name
    public string ContentType { get; set; }      // MIME type
    public long FileSize { get; set; }           // File size in bytes
    public DateTime UploadedAt { get; set; }     // Upload timestamp
    public string UploadedBy { get; set; }       // User identifier
    public string Content { get; set; }          // Extracted text content
    public List<DocumentChunk> Chunks { get; set; } // Document chunks
}
```

### DatabaseConfig

Database connection configuration.

```csharp
public class DatabaseConfig
{
    public DatabaseType Type { get; set; }              // Database type
    public string ConnectionString { get; set; }        // Connection string
    public List<string> IncludedTables { get; set; }    // Tables to include
    public List<string> ExcludedTables { get; set; }    // Tables to exclude
    public int MaxRowsPerTable { get; set; } = 1000;    // Row limit
    public bool SanitizeSensitiveData { get; set; } = true; // Sanitize sensitive columns
    public List<string> SensitiveColumns { get; set; }  // Columns to sanitize
}
```

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-list"></i>
            </div>
            <h3>Enumerations</h3>
            <p>AIProvider, StorageProvider, DatabaseType and other enums</p>
            <a href="{{ site.baseurl }}/en/api-reference/enums" class="btn btn-outline-primary btn-sm mt-3">
                Enumerations
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-home"></i>
            </div>
            <h3>API Reference</h3>
            <p>Back to API Reference index</p>
            <a href="{{ site.baseurl }}/en/api-reference" class="btn btn-outline-primary btn-sm mt-3">
                API Reference
            </a>
        </div>
    </div>
</div>

