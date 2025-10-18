---
layout: default
title: API Reference
description: Complete API documentation for SmartRAG interfaces, methods, and models
lang: en
---

<div class="container">

## Core Interfaces

SmartRAG provides well-defined interfaces for all operations. Inject these interfaces via dependency injection.

---

## IDocumentSearchService

**Purpose:** AI-powered intelligent query processing with RAG pipeline and conversation management

**Namespace:** `SmartRAG.Interfaces`

### Methods

#### QueryIntelligenceAsync

Process intelligent queries with RAG and automatic session management.

```csharp
Task<RagResponse> QueryIntelligenceAsync(
    string query, 
    int maxResults = 5, 
    bool startNewConversation = false
)
```

**Parameters:**
- `query` (string): The user's question or query
- `maxResults` (int): Maximum number of document chunks to retrieve (default: 5)
- `startNewConversation` (bool): Start a new conversation session (default: false)

**Returns:** `RagResponse` with AI answer, sources, and metadata

**Example:**

```csharp
var response = await _searchService.QueryIntelligenceAsync(
    "What are the main benefits?", 
    maxResults: 5
);

Console.WriteLine(response.Answer);
// Sources available in response.Sources
```

#### SearchDocumentsAsync

Search documents semantically without generating an AI answer.

```csharp
Task<List<DocumentChunk>> SearchDocumentsAsync(
    string query, 
    int maxResults = 5
)
```

**Parameters:**
- `query` (string): Search query
- `maxResults` (int): Maximum chunks to return (default: 5)

**Returns:** `List<DocumentChunk>` with relevant document chunks

**Example:**

```csharp
var chunks = await _searchService.SearchDocumentsAsync("machine learning", maxResults: 10);

foreach (var chunk in chunks)
{
    Console.WriteLine($"Score: {chunk.RelevanceScore}, Content: {chunk.Content}");
}
```

#### GenerateRagAnswerAsync (Deprecated)

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Deprecated in v3.0.0</h4>
    <p class="mb-0">
        Use <code>QueryIntelligenceAsync</code> instead. This method will be removed in v4.0.0.
        Legacy method provided for backward compatibility.
    </p>
</div>

```csharp
[Obsolete("Use QueryIntelligenceAsync instead")]
Task<RagResponse> GenerateRagAnswerAsync(
    string query, 
    int maxResults = 5, 
    bool startNewConversation = false
)
```

---

## IDocumentService

**Purpose:** Document CRUD operations and management

**Namespace:** `SmartRAG.Interfaces`

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

---

## IDocumentParserService

**Purpose:** Multi-format document parsing and text extraction

**Namespace:** `SmartRAG.Interfaces`

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

---

## IDatabaseParserService

**Purpose:** Universal database support with live connections

**Namespace:** `SmartRAG.Interfaces`

### Methods

#### ParseDatabaseFileAsync

Parse a database file (SQLite).

```csharp
Task<string> ParseDatabaseFileAsync(Stream dbStream, string fileName)
```

**Example:**

```csharp
using var dbStream = File.OpenRead("catalog.db");
var content = await _databaseService.ParseDatabaseFileAsync(dbStream, "catalog.db");

Console.WriteLine(content); // Extracted table data as text
```

#### ParseDatabaseConnectionAsync

Connect to live database and extract content.

```csharp
Task<string> ParseDatabaseConnectionAsync(
    string connectionString, 
    DatabaseConfig config
)
```

**Example:**

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

Extract data from a specific table.

```csharp
Task<string> ExtractTableDataAsync(
    string connectionString, 
    string tableName, 
    DatabaseType databaseType, 
    int maxRows = 1000
)
```

#### ExecuteQueryAsync

Execute custom SQL query.

```csharp
Task<string> ExecuteQueryAsync(
    string connectionString, 
    string query, 
    DatabaseType databaseType, 
    int maxRows = 1000
)
```

**Example:**

```csharp
var result = await _databaseService.ExecuteQueryAsync(
    "Server=localhost;Database=Sales;Trusted_Connection=true;",
    "SELECT TOP 10 CustomerID, CompanyName FROM Customers WHERE Country = 'USA'",
    DatabaseType.SqlServer,
    maxRows: 10
);
```

#### GetTableNamesAsync

Get list of table names from database.

```csharp
Task<List<string>> GetTableNamesAsync(
    string connectionString, 
    DatabaseType databaseType
)
```

#### GetTableSchemaAsync

Get schema information for a specific table.

```csharp
Task<string> GetTableSchemaAsync(
    string connectionString, 
    string tableName, 
    DatabaseType databaseType
)
```

#### ValidateConnectionAsync

Validate database connection.

```csharp
Task<bool> ValidateConnectionAsync(
    string connectionString, 
    DatabaseType databaseType
)
```

**Example:**

```csharp
bool isValid = await _databaseService.ValidateConnectionAsync(
    "Server=localhost;Database=MyDb;Trusted_Connection=true;",
    DatabaseType.SqlServer
);

if (isValid)
{
    Console.WriteLine("Connection successful!");
}
```

#### GetSupportedDatabaseTypes

Get list of supported database types.

```csharp
IEnumerable<DatabaseType> GetSupportedDatabaseTypes()
```

**Returns:**
- `DatabaseType.SqlServer`
- `DatabaseType.MySQL`
- `DatabaseType.PostgreSql`
- `DatabaseType.Sqlite`

#### GetSupportedDatabaseFileExtensions

Get supported file extensions for database files.

```csharp
IEnumerable<string> GetSupportedDatabaseFileExtensions()
```

**Returns:** `.db`, `.sqlite`, `.sqlite3`

#### ClearMemoryCache

Clear memory cache and dispose resources.

```csharp
void ClearMemoryCache()
```

---

## ISemanticSearchService

**Purpose:** Advanced semantic search with hybrid scoring

**Namespace:** `SmartRAG.Interfaces`

### Methods

#### CalculateEnhancedSemanticSimilarityAsync

Calculate enhanced semantic similarity using advanced text analysis.

```csharp
Task<double> CalculateEnhancedSemanticSimilarityAsync(
    string query, 
    string content
)
```

**Algorithm:** Hybrid scoring (80% semantic + 20% keyword)

**Returns:** Similarity score between 0.0 and 1.0

**Example:**

```csharp
double similarity = await _semanticSearch.CalculateEnhancedSemanticSimilarityAsync(
    "machine learning algorithms",
    "This document discusses various ML algorithms including neural networks..."
);

Console.WriteLine($"Similarity: {similarity:P}"); // e.g., "Similarity: 85%"
```

---

## IAIService

**Purpose:** AI provider communication for text generation and embeddings

**Namespace:** `SmartRAG.Interfaces`

### Methods

#### GenerateResponseAsync

Generate AI response based on query and context.

```csharp
Task<string> GenerateResponseAsync(
    string query, 
    IEnumerable<string> context
)
```

**Example:**

```csharp
var contextChunks = new List<string>
{
    "Document chunk 1...",
    "Document chunk 2...",
    "Document chunk 3..."
};

var response = await _aiService.GenerateResponseAsync(
    "What are the main topics?",
    contextChunks
);

Console.WriteLine(response);
```

#### GenerateEmbeddingsAsync

Generate embedding vector for text.

```csharp
Task<List<float>> GenerateEmbeddingsAsync(string text)
```

**Returns:** Embedding vector (typically 768 or 1536 dimensions)

#### GenerateEmbeddingsBatchAsync

Generate embeddings for multiple texts in batch.

```csharp
Task<List<List<float>>> GenerateEmbeddingsBatchAsync(
    IEnumerable<string> texts
)
```

**Example:**

```csharp
var texts = new List<string> { "Text 1", "Text 2", "Text 3" };
var embeddings = await _aiService.GenerateEmbeddingsBatchAsync(texts);

Console.WriteLine($"Generated {embeddings.Count} embeddings");
```

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

---

## Enumerations

### AIProvider

Supported AI providers.

```csharp
public enum AIProvider
{
    OpenAI,        // OpenAI GPT models
    Anthropic,     // Anthropic Claude models
    Gemini,        // Google Gemini models
    AzureOpenAI,   // Azure OpenAI service
    Custom         // Custom/Ollama/LM Studio/OpenRouter
}
```

### StorageProvider

Supported storage backends.

```csharp
public enum StorageProvider
{
    Qdrant,       // Vector database
    Redis,        // High-performance cache
    Sqlite,       // Embedded database
    FileSystem,   // File-based storage
    InMemory      // RAM storage (development only)
}
```

### DatabaseType

Supported database types.

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

Retry policies for failed requests.

```csharp
public enum RetryPolicy
{
    None,                // No retries
    FixedDelay,         // Fixed delay between retries
    LinearBackoff,      // Linearly increasing delay
    ExponentialBackoff  // Exponentially increasing delay (recommended)
}
```

---

## Advanced Interfaces

### IMultiDatabaseQueryCoordinator

**Purpose:** Coordinate queries across multiple databases

```csharp
public interface IMultiDatabaseQueryCoordinator
{
    Task<MultiDatabaseQueryResponse> ExecuteQueryAsync(string naturalLanguageQuery);
    Task<List<DatabaseInfo>> GetConnectedDatabasesAsync();
}
```

### IDatabaseSchemaAnalyzer

**Purpose:** Analyze and extract database schemas

```csharp
public interface IDatabaseSchemaAnalyzer
{
    Task<DatabaseSchemaInfo> AnalyzeSchemaAsync(string connectionString, DatabaseType type);
    Task<List<TableInfo>> GetTablesAsync(string connectionString, DatabaseType type);
    Task<List<ColumnInfo>> GetColumnsAsync(string connectionString, DatabaseType type, string tableName);
}
```

### IAudioParserService

**Purpose:** Audio transcription with Google Speech-to-Text

```csharp
public interface IAudioParserService
{
    Task<AudioTranscriptionResult> TranscribeAudioAsync(Stream audioStream, string language = "en-US");
    IEnumerable<string> GetSupportedAudioFormats();
}
```

### IImageParserService

**Purpose:** OCR text extraction from images

```csharp
public interface IImageParserService
{
    Task<OcrResult> ExtractTextFromImageAsync(Stream imageStream, string language = "eng");
    IEnumerable<string> GetSupportedImageFormats();
}
```

---

## Usage Patterns

### Dependency Injection

Inject interfaces in your services/controllers:

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

### Error Handling

```csharp
try
{
    var response = await _searchService.QueryIntelligenceAsync(query);
    return Ok(response);
}
catch (SmartRagException ex)
{
    // SmartRAG-specific exceptions
    _logger.LogError(ex, "SmartRAG error: {Message}", ex.Message);
    return BadRequest(new { error = ex.Message });
}
catch (Exception ex)
{
    // General exceptions
    _logger.LogError(ex, "Unexpected error");
    return StatusCode(500, "Internal server error");
}
```

### Async/Await Best Practices

```csharp
// ✅ GOOD - Await properly
var result = await _searchService.QueryIntelligenceAsync(query);

// ❌ BAD - Blocking call (can cause deadlocks)
var result = _searchService.QueryIntelligenceAsync(query).Result;

// ✅ GOOD - ConfigureAwait in library code
var result = await _searchService.QueryIntelligenceAsync(query).ConfigureAwait(false);
```

---

## Performance Tips

<div class="alert alert-success">
    <h4><i class="fas fa-bolt me-2"></i> Performance Optimization</h4>
    <ul class="mb-0">
        <li><strong>Chunk Size:</strong> 500-1000 characters for optimal balance</li>
        <li><strong>MaxResults:</strong> 5-10 chunks typically sufficient</li>
        <li><strong>Batch Operations:</strong> Use <code>UploadDocumentsAsync</code> for multiple files</li>
        <li><strong>Storage:</strong> Use Qdrant or Redis for production (not InMemory)</li>
        <li><strong>Caching:</strong> Enable conversation storage for better performance</li>
        <li><strong>Database Limits:</strong> Set reasonable MaxRowsPerTable (1000-5000)</li>
    </ul>
</div>

---

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card">
            <div class="feature-icon">
                <i class="fas fa-lightbulb"></i>
            </div>
            <h3>Examples</h3>
            <p>See practical code examples and real-world implementations</p>
            <a href="{{ site.baseurl }}/en/examples" class="btn btn-outline-primary btn-sm mt-3">
                View Examples
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card">
            <div class="feature-icon">
                <i class="fas fa-rocket"></i>
            </div>
            <h3>Getting Started</h3>
            <p>Quick installation and setup guide</p>
            <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-outline-primary btn-sm mt-3">
                Get Started
            </a>
        </div>
    </div>
</div>

</div>

