---
layout: default
title: API Reference
description: Complete API documentation for SmartRAG interfaces, methods, and models
lang: en
redirect_from: /en/api-reference.html
---

This page has been moved. Please visit the [API Reference Index]({{ site.baseurl }}/en/api-reference/).

---

## IDocumentSearchService

**Purpose:** AI-powered intelligent query processing with RAG pipeline and conversation management

**Namespace:** `SmartRAG.Interfaces.Document`

### Methods

#### QueryIntelligenceAsync

Unified intelligent query processing with RAG and automatic session management. Searches across databases, documents, images (OCR), and audio (transcription) in a single query using Smart Hybrid routing.

**Smart Hybrid Routing:**
- **High Confidence (>0.7) + Database Queries**: Executes database query only
- **High Confidence (>0.7) + No Database Queries**: Executes document query only
- **Medium Confidence (0.3-0.7)**: Executes both database and document queries, merges results
- **Low Confidence (<0.3)**: Executes document query only (fallback)

```csharp
Task<RagResponse> QueryIntelligenceAsync(
    string query, 
    int maxResults = 5, 
    bool startNewConversation = false,
    SearchOptions? options = null
)
```

**Parameters:**
- `query` (string): The user's question or query
- `maxResults` (int): Maximum number of document chunks to retrieve (default: 5)
- `startNewConversation` (bool): Start a new conversation session (default: false)
- `options` (SearchOptions?): Optional search options to override global configuration (default: null)

**Returns:** `RagResponse` with AI answer, sources from all available data sources (databases, documents, images, audio), and metadata

**Example:**

```csharp
// Unified query across all data sources
var response = await _searchService.QueryIntelligenceAsync(
    "Show me top customers and their recent feedback", 
    maxResults: 5
);

Console.WriteLine(response.Answer);
// Sources include both database and document sources
foreach (var source in response.Sources)
{
    Console.WriteLine($"Source: {source.FileName}");
}
```

**SearchOptions Usage:**

```csharp
// Enable only database search
var dbOptions = new SearchOptions
{
    EnableDatabaseSearch = true,
    EnableDocumentSearch = false,
    EnableAudioSearch = false,
    EnableImageSearch = false
};

var dbResponse = await _searchService.QueryIntelligenceAsync(
    "Show top customers",
    maxResults: 5,
    options: dbOptions
);

// Enable only audio search
var audioOptions = new SearchOptions
{
    EnableDatabaseSearch = false,
    EnableDocumentSearch = false,
    EnableAudioSearch = true,
    EnableImageSearch = false,
    PreferredLanguage = "en"
};

var audioResponse = await _searchService.QueryIntelligenceAsync(
    "What was discussed in the meeting?",
    maxResults: 5,
    options: audioOptions
);

// Use global configuration
var globalOptions = SearchOptions.FromConfig(_smartRagOptions);
var response = await _searchService.QueryIntelligenceAsync(
    "Search everything",
    maxResults: 5,
    options: globalOptions
);
```

**Flag-Based Filtering (Query String Parsing):**

You can parse flags from query strings for quick search type selection:

```csharp
// Parse flags from query string
string userQuery = "-db Show top customers";
var searchOptions = ParseSearchOptions(userQuery, out string cleanQuery);

// cleanQuery = "Show top customers"
// searchOptions.EnableDatabaseSearch = true
// searchOptions.EnableDocumentSearch = false
// searchOptions.EnableAudioSearch = false
// searchOptions.EnableImageSearch = false

var response = await _searchService.QueryIntelligenceAsync(
    cleanQuery,
    maxResults: 5,
    options: searchOptions
);
```

**Available Flags:**
- `-db`: Enable database search only
- `-d`: Enable document (text) search only
- `-a`: Enable audio search only
- `-i`: Enable image search only
- Flags can be combined (e.g., `-db -a` for database + audio search)

**Note:** If database coordinator is not configured, the method automatically falls back to document-only search, maintaining backward compatibility.

#### SearchDocumentsAsync

Search documents semantically without generating an AI answer.

```csharp
Task<List<DocumentChunk>> SearchDocumentsAsync(
    string query, 
    int maxResults = 5,
    SearchOptions? options = null,
    List<string>? queryTokens = null
)
```

**Parameters:**
- `query` (string): Search query
- `maxResults` (int): Maximum chunks to return (default: 5)
- `options` (SearchOptions?, optional): Optional search options to override global configuration (default: null)
- `queryTokens` (List<string>?, optional): Pre-computed query tokens for performance optimization (default: null)

**Returns:** `List<DocumentChunk>` with relevant document chunks

**Example:**

```csharp
// Basic usage
var chunks = await _searchService.SearchDocumentsAsync("machine learning", maxResults: 10);

foreach (var chunk in chunks)
{
    Console.WriteLine($"Score: {chunk.RelevanceScore}, Content: {chunk.Content}");
}

// With search options
var options = new SearchOptions
{
    EnableDocumentSearch = true,
    EnableAudioSearch = false,
    EnableImageSearch = false
};

var filteredChunks = await _searchService.SearchDocumentsAsync(
    "machine learning", 
    maxResults: 10,
    options: options
);

// With pre-computed tokens (performance optimization)
var tokens = new List<string> { "machine", "learning", "algorithms" };
var optimizedChunks = await _searchService.SearchDocumentsAsync(
    "machine learning",
    maxResults: 10,
    queryTokens: tokens
);
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

---

## IConversationManagerService

**Purpose:** Conversation session management and history tracking

**Namespace:** `SmartRAG.Interfaces.Support`

This interface provides dedicated conversation management, separated from document operations for better separation of concerns.

### Methods

#### StartNewConversationAsync

Start a new conversation session.

```csharp
Task<string> StartNewConversationAsync()
```

**Returns:** New session ID (string)

**Example:**

```csharp
var sessionId = await _conversationManager.StartNewConversationAsync();
Console.WriteLine($"Started session: {sessionId}");
```

#### GetOrCreateSessionIdAsync

Get existing session ID or create a new one automatically.

```csharp
Task<string> GetOrCreateSessionIdAsync()
```

**Returns:** Session ID (string)

**Use Case:** Automatic session continuity without manual session management

**Example:**

```csharp
// Automatically manages session - creates new if none exists
var sessionId = await _conversationManager.GetOrCreateSessionIdAsync();
```

#### AddToConversationAsync

Add a conversation turn (question + answer) to the session history.

```csharp
Task AddToConversationAsync(
    string sessionId, 
    string question, 
    string answer
)
```

**Parameters:**
- `sessionId` (string): Session identifier
- `question` (string): User's question
- `answer` (string): AI's answer

**Example:**

```csharp
await _conversationManager.AddToConversationAsync(
    sessionId,
    "What is machine learning?",
    "Machine learning is a subset of AI that enables systems to learn..."
);
```

#### GetConversationHistoryAsync

Retrieve full conversation history for a session.

```csharp
Task<string> GetConversationHistoryAsync(string sessionId)
```

**Parameters:**
- `sessionId` (string): Session identifier

**Returns:** Formatted conversation history as string

**Format:**
```
User: [question]
Assistant: [answer]
User: [next question]
Assistant: [next answer]
```

**Example:**

```csharp
var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
Console.WriteLine(history);
```

#### TruncateConversationHistory

Truncate conversation history to keep only recent turns (memory management).

```csharp
string TruncateConversationHistory(
    string history, 
    int maxTurns = 3
)
```

**Parameters:**
- `history` (string): Full conversation history
- `maxTurns` (int): Maximum number of conversation turns to keep (default: 3)

**Returns:** Truncated conversation history

**Use Case:** Prevent context window overflow in AI prompts

**Example:**

```csharp
var fullHistory = await _conversationManager.GetConversationHistoryAsync(sessionId);
var recentHistory = _conversationManager.TruncateConversationHistory(fullHistory, maxTurns: 5);
```

### Complete Usage Example

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
        // Get or create session
        var sessionId = await _conversationManager.GetOrCreateSessionIdAsync();
        
        // Get conversation history for context
        var history = await _conversationManager.GetConversationHistoryAsync(sessionId);
        
        // Query with context
        var response = await _searchService.QueryIntelligenceAsync(userMessage);
        
        // Save to conversation history
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
        return $"Started new conversation: {newSessionId}";
    }
}
```

### Storage Backends

Conversation history is stored using the configured `IConversationRepository`:
- **SQLite**: `SqliteConversationRepository` - Persistent file-based storage
- **InMemory**: `InMemoryConversationRepository` - Fast, non-persistent (development)
- **FileSystem**: `FileSystemConversationRepository` - JSON file-based storage
- **Redis**: `RedisConversationRepository` - High-performance distributed storage

Storage backend is automatically selected based on your `StorageProvider` configuration.

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

---

## IDatabaseParserService

**Purpose:** Universal database support with live connections

**Namespace:** `SmartRAG.Interfaces.Database`

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
- `DatabaseType.SQLite`
- `DatabaseType.SqlServer`
- `DatabaseType.MySQL`
- `DatabaseType.PostgreSQL`

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

**Namespace:** `SmartRAG.Interfaces.Search`

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

## IContextExpansionService

**Purpose:** Expand document chunk context by including adjacent chunks from the same document

**Namespace:** `SmartRAG.Interfaces.Search`

### Methods

#### ExpandContextAsync

Expands context by including adjacent chunks from the same document. This ensures that if a heading is in one chunk and content is in the next, both are included in the search results.

```csharp
Task<List<DocumentChunk>> ExpandContextAsync(
    List<DocumentChunk> chunks, 
    int contextWindow = 2
)
```

**Parameters:**
- `chunks` (List<DocumentChunk>): Initial chunks found by search
- `contextWindow` (int): Number of adjacent chunks to include before and after each found chunk (default: 2, max: 5)

**Returns:** Expanded list of chunks with context, sorted by document ID and chunk index

**Example:**

```csharp
// Search for relevant chunks
var chunks = await _searchService.SearchDocumentsAsync("SRS maintenance", maxResults: 5);

// Expand context to include adjacent chunks
var expandedChunks = await _contextExpansion.ExpandContextAsync(chunks, contextWindow: 2);

// Now expandedChunks includes the heading chunk AND its content chunks
foreach (var chunk in expandedChunks)
{
    Console.WriteLine($"Chunk {chunk.ChunkIndex}: {chunk.Content.Substring(0, 100)}...");
}
```

**Note:** This service is automatically used by `DocumentSearchService` when generating RAG answers. It helps prevent situations where only headings are found without their corresponding content.

---

## IAIService

**Purpose:** AI provider communication for text generation and embeddings

**Namespace:** `SmartRAG.Interfaces.AI`

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

Supported storage backends for document and vector data persistence.

```csharp
public enum StorageProvider
{
    InMemory,    // RAM storage (non-persistent, for testing and development)
    Redis,       // High-performance cache and storage
    Qdrant       // Vector database for advanced vector search capabilities
}
```

**Note:** `SQLite` and `FileSystem` are not available as `StorageProvider` options. They are only available as `ConversationStorageProvider` options for conversation history storage.

### DatabaseType

Supported database types.

```csharp
public enum DatabaseType
{
    SQLite,       // SQLite embedded database
    SqlServer,    // Microsoft SQL Server
    MySQL,        // MySQL / MariaDB
    PostgreSQL    // PostgreSQL
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

**Purpose:** Coordinates intelligent multi-database queries using AI

**Namespace:** `SmartRAG.Interfaces.Database`

This interface enables querying across multiple databases simultaneously using natural language. The AI analyzes the query, determines which databases and tables to access, generates optimized SQL queries, and merges results into a coherent response.

#### Methods

##### QueryMultipleDatabasesAsync

Executes a full intelligent query: analyze intent + execute + merge results.

**Overload 1:** Full query with automatic intent analysis

```csharp
Task<RagResponse> QueryMultipleDatabasesAsync(
    string userQuery, 
    int maxResults = 5
)
```

**Overload 2:** Query with pre-analyzed intent (avoids redundant AI calls)

```csharp
Task<RagResponse> QueryMultipleDatabasesAsync(
    string userQuery, 
    QueryIntent preAnalyzedIntent,
    int maxResults = 5
)
```

**Parameters:**
- `userQuery` (string): Natural language user query
- `preAnalyzedIntent` (QueryIntent, optional): Pre-analyzed query intent to avoid redundant AI calls
- `maxResults` (int): Maximum number of results to return (default: 5)

**Returns:** `RagResponse` with AI-generated answer and data from multiple databases

**Example 1 - Automatic Intent Analysis:**

```csharp
var response = await _coordinator.QueryMultipleDatabasesAsync(
    "Show records from TableA with their corresponding TableB details"
);

Console.WriteLine(response.Answer);
// AI answer combining data from multiple databases
```

**Example 2 - Pre-analyzed Intent (Performance Optimization):**

```csharp
// Analyze intent once
var intent = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(
    "Show records from TableA with their corresponding TableB details"
);

// Use pre-analyzed intent to avoid redundant AI calls
var response = await _coordinator.QueryMultipleDatabasesAsync(
    "Show records from TableA with their corresponding TableB details",
    intent,
    maxResults: 10
);

Console.WriteLine(response.Answer);
```

##### AnalyzeQueryIntentAsync (Deprecated)

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Deprecated in </h4>
    <p class="mb-0">
        Use <code>IQueryIntentAnalyzer.AnalyzeQueryIntentAsync</code> instead. This method will be removed in v4.0.0.
    </p>
</div>

Legacy method for analyzing user query and determining which databases/tables to query.

```csharp
[Obsolete("Use IQueryIntentAnalyzer.AnalyzeQueryIntentAsync instead. Will be removed in v4.0.0")]
Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery)
```

**Parameters:**
- `userQuery` (string): Natural language user query

**Returns:** `QueryIntent` with database routing information

**Recommended Usage:**

```csharp
// Use IQueryIntentAnalyzer instead
var intent = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(
    "Compare data between Database1 and Database2"
);

Console.WriteLine($"Confidence: {intent.Confidence}");
Console.WriteLine($"Requires Cross-DB Join: {intent.RequiresCrossDatabaseJoin}");

foreach (var dbQuery in intent.DatabaseQueries)
{
    Console.WriteLine($"Database: {dbQuery.DatabaseName}");
    Console.WriteLine($"Tables: {string.Join(", ", dbQuery.RequiredTables)}");
}
```

##### ExecuteMultiDatabaseQueryAsync

Executes queries across multiple databases based on query intent.

```csharp
Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(
    QueryIntent queryIntent
)
```

**Parameters:**
- `queryIntent` (QueryIntent): Analyzed query intent

**Returns:** `MultiDatabaseQueryResult` with combined results from all databases

##### GenerateDatabaseQueriesAsync

Generates optimized SQL queries for each database based on intent.

```csharp
Task<QueryIntent> GenerateDatabaseQueriesAsync(QueryIntent queryIntent)
```

**Parameters:**
- `queryIntent` (QueryIntent): Query intent to generate SQL for

**Returns:** Updated `QueryIntent` with generated SQL queries

**Note:** `MergeResultsAsync` is available in `IResultMerger` interface, not in `IMultiDatabaseQueryCoordinator`. The coordinator automatically uses the result merger internally.

---

### IDatabaseConnectionManager

**Purpose:** Manages database connections from configuration

**Namespace:** `SmartRAG.Interfaces.Database`

Handles database connection lifecycle, validation, and runtime management.

#### Methods

##### InitializeAsync

Initializes all database connections from configuration.

```csharp
Task InitializeAsync()
```

**Example:**

```csharp
await _connectionManager.InitializeAsync();
Console.WriteLine("All database connections initialized");
```

##### GetAllConnectionsAsync

Gets all configured database connections.

```csharp
Task<List<DatabaseConnectionConfig>> GetAllConnectionsAsync()
```

**Returns:** List of all database connection configurations

##### GetConnectionAsync

Gets a specific connection by ID.

```csharp
Task<DatabaseConnectionConfig> GetConnectionAsync(string databaseId)
```

**Parameters:**
- `databaseId` (string): Database identifier

**Returns:** Connection configuration or null if not found

##### ValidateConnectionAsync

Validates a specific connection.

```csharp
Task<bool> ValidateConnectionAsync(string databaseId)
```

**Parameters:**
- `databaseId` (string): Database identifier

**Returns:** True if connection is valid, false otherwise

**Example:**

```csharp
bool isValid = await _connectionManager.ValidateConnectionAsync("database-1");

if (isValid)
{
    Console.WriteLine("Connection is valid");
}
```

##### GetDatabaseIdAsync

Gets database identifier from connection (auto-generates if Name not provided).

```csharp
Task<string> GetDatabaseIdAsync(DatabaseConnectionConfig connectionConfig)
```

**Parameters:**
- `connectionConfig` (DatabaseConnectionConfig): Connection configuration

**Returns:** Unique database identifier

**Example:**

```csharp
var config = new DatabaseConnectionConfig
{
    Name = "SalesDB",
    ConnectionString = "Server=localhost;Database=Sales;Trusted_Connection=true;",
    DatabaseType = DatabaseType.SqlServer
};

var databaseId = await _connectionManager.GetDatabaseIdAsync(config);
Console.WriteLine($"Database ID: {databaseId}");
```

---

### IDatabaseSchemaAnalyzer

**Purpose:** Analyzes database schemas and generates intelligent metadata

**Namespace:** `SmartRAG.Interfaces.Database`

Extracts comprehensive schema information including tables, columns, relationships, and generates AI-powered summaries.

#### Methods

##### AnalyzeDatabaseSchemaAsync

Analyzes a database connection and extracts comprehensive schema information.

```csharp
Task<DatabaseSchemaInfo> AnalyzeDatabaseSchemaAsync(
    DatabaseConnectionConfig connectionConfig
)
```

**Parameters:**
- `connectionConfig` (DatabaseConnectionConfig): Database connection configuration

**Returns:** Complete `DatabaseSchemaInfo` including tables, columns, foreign keys, and AI-generated summaries

**Example:**

```csharp
var config = new DatabaseConnectionConfig
{
    ConnectionString = "Server=localhost;Database=Northwind;Trusted_Connection=true;",
    DatabaseType = DatabaseType.SqlServer
};

var schemaInfo = await _schemaAnalyzer.AnalyzeDatabaseSchemaAsync(config);

Console.WriteLine($"Database: {schemaInfo.DatabaseName}");
Console.WriteLine($"Tables: {schemaInfo.Tables.Count}");
Console.WriteLine($"Total Rows: {schemaInfo.TotalRowCount:N0}");
Console.WriteLine($"AI Summary: {schemaInfo.AISummary}");
```

##### GetAllSchemasAsync

Gets all analyzed database schemas.

```csharp
Task<List<DatabaseSchemaInfo>> GetAllSchemasAsync()
```

**Returns:** List of all database schemas currently in memory

##### GetSchemaAsync

Gets schema for a specific database.

```csharp
Task<DatabaseSchemaInfo> GetSchemaAsync(string databaseId)
```

**Parameters:**
- `databaseId` (string): Database identifier

**Returns:** Database schema information or null if not found

##### GenerateAISummaryAsync

Generates AI-powered summary of database content.

```csharp
Task<string> GenerateAISummaryAsync(DatabaseSchemaInfo schemaInfo)
```

**Parameters:**
- `schemaInfo` (DatabaseSchemaInfo): Schema information to summarize

**Returns:** AI-generated summary describing the database purpose and content

---

### IAudioParserService

**Purpose:** Audio transcription with Whisper.net (100% local processing)

**Namespace:** `SmartRAG.Interfaces.Parser`

Provides local audio-to-text transcription using Whisper.net. All processing is done on-premise - no data is sent to external services.

<div class="alert alert-success">
    <h4><i class="fas fa-lock me-2"></i> Privacy Note</h4>
    <p class="mb-0">
        Audio transcription uses <strong>Whisper.net</strong> for 100% local processing. 
        No audio data is ever sent to external services. GDPR/KVKK/HIPAA compliant.
    </p>
</div>

#### Methods

##### TranscribeAudioAsync

Transcribes audio content from a stream to text.

```csharp
Task<AudioTranscriptionResult> TranscribeAudioAsync(
    Stream audioStream, 
    string fileName, 
    string language = null
)
```

**Parameters:**
- `audioStream` (Stream): The audio stream to transcribe
- `fileName` (string): The name of the audio file for format detection
- `language` (string, optional): Language code for transcription (e.g., "tr-TR", "en-US", "auto")

**Returns:** `AudioTranscriptionResult` containing transcribed text, confidence score, and metadata

**Example:**

```csharp
using var audioStream = File.OpenRead("meeting.mp3");

var result = await _audioParser.TranscribeAudioAsync(
    audioStream, 
    "meeting.mp3", 
    language: "en"
);

Console.WriteLine($"Transcription: {result.Text}");
Console.WriteLine($"Confidence: {result.Confidence:P}");
Console.WriteLine($"Language: {result.Language}");
```

**Supported Audio Formats:**
- MP3, WAV, M4A, AAC, OGG, FLAC, WMA

**Whisper Models:**
- `tiny` (75MB) - Fastest, lowest accuracy
- `base` (142MB) - Good balance (recommended)
- `small` (466MB) - Better accuracy
- `medium` (1.5GB) - High accuracy
- `large-v3` (2.9GB) - Highest accuracy

---

### IImageParserService

**Purpose:** OCR text extraction from images using Tesseract

**Namespace:** `SmartRAG.Interfaces.Parser`

Provides optical character recognition (OCR) for extracting text from images. All processing is local using Tesseract.

#### Methods

##### ExtractTextFromImageAsync

Extracts text from an image using OCR.

```csharp
Task<string> ExtractTextFromImageAsync(
    Stream imageStream, 
    string language = "eng"
)
```

**Parameters:**
- `imageStream` (Stream): The image stream to process
- `language` (string, optional): Language code for OCR (default: "eng")
  - English: "eng"
  - Turkish: "tur"
  - German: "deu"
  - Multiple: "eng+tur"

**Returns:** Extracted text as string

**Example:**

```csharp
using var imageStream = File.OpenRead("document.png");

var text = await _imageParser.ExtractTextFromImageAsync(
    imageStream, 
    language: "eng"
);

Console.WriteLine($"Extracted Text: {text}");
```

##### ExtractTextWithConfidenceAsync

Extracts text from an image with confidence scores.

```csharp
Task<OcrResult> ExtractTextWithConfidenceAsync(
    Stream imageStream, 
    string language = null
)
```

**Parameters:**
- `imageStream` (Stream): The image stream to process
- `language` (string, optional): Language code for OCR (e.g., "eng", "tur"). If null, uses system locale automatically

**Returns:** `OcrResult` with extracted text, confidence scores, and text blocks

**Example:**

```csharp
using var imageStream = File.OpenRead("invoice.jpg");

var result = await _imageParser.ExtractTextWithConfidenceAsync(
    imageStream, 
    language: "eng"
);

Console.WriteLine($"Text: {result.ExtractedText}");
Console.WriteLine($"Confidence: {result.Confidence:P}");

foreach (var block in result.TextBlocks)
{
    Console.WriteLine($"Block: {block.Text} (Confidence: {block.Confidence:P})");
}
```

##### PreprocessImageAsync

Preprocesses an image for better OCR results.

```csharp
Task<Stream> PreprocessImageAsync(Stream imageStream)
```

**Parameters:**
- `imageStream` (Stream): The input image stream

**Returns:** Preprocessed image stream

**Preprocessing Steps:**
- Grayscale conversion
- Contrast enhancement
- Noise reduction
- Binarization

**Example:**

```csharp
using var originalStream = File.OpenRead("low-quality.jpg");
using var preprocessedStream = await _imageParser.PreprocessImageAsync(originalStream);

var text = await _imageParser.ExtractTextFromImageAsync(
    preprocessedStream, 
    language: "eng"
);

Console.WriteLine($"Text from preprocessed image: {text}");
```

##### CorrectCurrencySymbols

Corrects currency symbol misreads in text (e.g., % → ₺, $, €). This method applies the same currency correction logic used in OCR results to any text.

```csharp
string CorrectCurrencySymbols(string text, string language = null)
```

**Parameters:**
- `text` (string): Text to correct
- `language` (string, optional): Language code for context (used for logging)

**Returns:** Text with corrected currency symbols

**Example:**

```csharp
var correctedText = _imageParser.CorrectCurrencySymbols("Price: 100%", "tr");
Console.WriteLine(correctedText); // "Price: 100₺"
```

**Supported Image Formats:**
- JPEG, PNG, GIF, BMP, TIFF, WEBP

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

## Strategy Pattern Interfaces 

SmartRAG provides Strategy Pattern for extensibility and customization.

### ISqlDialectStrategy

**Purpose:** Database-specific SQL generation and validation

**Namespace:** `SmartRAG.Interfaces.Database.Strategies`

Enables database-specific SQL optimization and custom database support.

#### Properties

```csharp
DatabaseType DatabaseType { get; }
```

#### Methods

##### BuildSystemPrompt

Build AI system prompt for SQL generation specific to this database dialect.

```csharp
string BuildSystemPrompt(DatabaseSchemaInfo schema, string userQuery)
```

##### ValidateSyntax

Validate SQL syntax for this specific dialect.

```csharp
bool ValidateSyntax(string sql, out string errorMessage)
```

##### FormatSql

Format SQL query according to dialect-specific rules.

```csharp
string FormatSql(string sql)
```

##### GetLimitClause

Get the LIMIT clause format for this dialect.

```csharp
string GetLimitClause(int limit)
```

**Returns:**
- SQLite/MySQL: `LIMIT {limit}`
- SQL Server: `TOP {limit}`
- PostgreSQL: `LIMIT {limit}`

#### Built-in Implementations

- `SqliteDialectStrategy` - SQLite-optimized SQL
- `PostgreSqlDialectStrategy` - PostgreSQL-optimized SQL
- `MySqlDialectStrategy` - MySQL/MariaDB-optimized SQL
- `SqlServerDialectStrategy` - SQL Server-optimized SQL

#### Custom Implementation Example

**Note:** This is a conceptual example. To add support for a new database type, you would need to:
1. Add the database type to the `DatabaseType` enum
2. Implement `ISqlDialectStrategy` for that database
3. Register the strategy in dependency injection

```csharp
// Example: Custom database dialect strategy
public class CustomDialectStrategy : BaseSqlDialectStrategy
{
    public override DatabaseType DatabaseType => DatabaseType.Custom; // Assuming Custom is added to enum
    
    public override string BuildSystemPrompt(DatabaseSchemaInfo schema, string userQuery)
    {
        return $"Generate SQL for: {userQuery}\\nSchema: {schema}";
    }
    
    public override bool ValidateSyntax(string sql, out string errorMessage)
    {
        // Database-specific validation
        errorMessage = null;
        return true;
    }
    
    public override string FormatSql(string sql)
    {
        // Database-specific formatting
        return sql;
    }
    
    public override string GetLimitClause(int limit)
    {
        // Database-specific LIMIT clause format
        return $"LIMIT {limit}";
    }
}
```

---

### IScoringStrategy

**Purpose:** Customizable document relevance scoring

**Namespace:** `SmartRAG.Interfaces.Search.Strategies`

Enables custom scoring algorithms for search results.

#### Methods

##### CalculateScoreAsync

Calculate relevance score for a document chunk.

```csharp
Task<double> CalculateScoreAsync(
    string query, 
    DocumentChunk chunk, 
    List<float> queryEmbedding
)
```

**Parameters:**
- `query` (string): Search query
- `chunk` (DocumentChunk): Document chunk to score
- `queryEmbedding` (List<float>): Query embedding vector

**Returns:** Score between 0.0 and 1.0

#### Built-in Implementation

**HybridScoringStrategy** (default):
- 80% semantic similarity (cosine similarity of embeddings)
- 20% keyword matching (BM25-like scoring)

#### Custom Implementation Example

```csharp
public class SemanticOnlyScoringStrategy : IScoringStrategy
{
    public async Task<double> CalculateScoreAsync(
        string query, 
        DocumentChunk chunk, 
        List<float> queryEmbedding)
    {
        // Pure semantic similarity (100% embedding-based)
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

**Purpose:** Strategy for parsing specific file formats

**Namespace:** `SmartRAG.Interfaces.Parser.Strategies`

Enables custom file format parsers.

#### Methods

##### ParseAsync

Parse a file and extract content.

```csharp
Task<FileParserResult> ParseAsync(Stream fileStream, string fileName)
```

##### CanParse

Check if this parser can handle the given file.

```csharp
bool CanParse(string fileName, string contentType)
```

#### Built-in Implementations

- `PdfFileParser` - PDF documents
- `WordFileParser` - Word documents (.docx)
- `ExcelFileParser` - Excel spreadsheets (.xlsx)
- `TextFileParser` - Plain text files
- `ImageFileParser` - Images with OCR
- `AudioFileParser` - Audio transcription
- `DatabaseFileParser` - SQLite databases

#### Custom Implementation Example

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
        
        // Strip markdown syntax for plain text
        var plainText = StripMarkdownSyntax(content);
        
        return new FileParserResult
        {
            Content = plainText,
            Success = true
        };
    }
    
    private string StripMarkdownSyntax(string markdown)
    {
        // Remove markdown formatting
        return Regex.Replace(markdown, @"[#*`\[\]()]", "");
    }
}
```

---

## Additional Service Interfaces 

### IConversationRepository

**Purpose:** Data access layer for conversation storage

**Namespace:** `SmartRAG.Interfaces.Storage`

Separated from `IDocumentRepository` for better SRP compliance.

#### Methods

```csharp
Task<string> GetConversationHistoryAsync(string sessionId);
Task SaveConversationAsync(string sessionId, string history);
Task DeleteConversationAsync(string sessionId);
Task<bool> ConversationExistsAsync(string sessionId);
```

#### Implementations

- `SqliteConversationRepository`
- `InMemoryConversationRepository`
- `FileSystemConversationRepository`
- `RedisConversationRepository`

---

### IAIConfigurationService

**Purpose:** AI provider configuration management

**Namespace:** `SmartRAG.Interfaces.AI`

Separated configuration from execution for better SRP.

#### Methods

```csharp
AIProvider GetProvider();
string GetModel();
string GetEmbeddingModel();
int GetMaxTokens();
double GetTemperature();
```

---

### IAIRequestExecutor

**Purpose:** AI request execution with retry/fallback

**Namespace:** `SmartRAG.Interfaces.AI`

Handles AI requests with automatic retry and fallback logic.

#### Methods

```csharp
Task<string> ExecuteRequestAsync(string prompt, CancellationToken cancellationToken = default);
Task<List<float>> ExecuteEmbeddingRequestAsync(string text, CancellationToken cancellationToken = default);
```

---

### IQueryIntentAnalyzer

**Purpose:** Query intent analysis for database routing

**Namespace:** `SmartRAG.Interfaces.Database`

Analyzes queries to determine database routing strategy.

#### Methods

```csharp
Task<QueryIntent> AnalyzeQueryIntentAsync(string userQuery);
```

---

### IDatabaseQueryExecutor

**Purpose:** Execute queries across multiple databases

**Namespace:** `SmartRAG.Interfaces.Database`

Parallel query execution across databases.

#### Methods

```csharp
Task<MultiDatabaseQueryResult> ExecuteMultiDatabaseQueryAsync(QueryIntent queryIntent);
```

---

### IResultMerger

**Purpose:** Merge results from multiple databases

**Namespace:** `SmartRAG.Interfaces.Database`

AI-powered result merging.

#### Methods

##### MergeResultsAsync

Merges results from multiple databases into a coherent response.

```csharp
Task<string> MergeResultsAsync(MultiDatabaseQueryResult queryResults, string originalQuery)
```

**Parameters:**
- `queryResults` (MultiDatabaseQueryResult): Results from multiple databases
- `originalQuery` (string): Original user query

**Returns:** Merged and formatted results as string

##### GenerateFinalAnswerAsync

Generates final AI answer from merged database results.

```csharp
Task<RagResponse> GenerateFinalAnswerAsync(
    string userQuery, 
    string mergedData, 
    MultiDatabaseQueryResult queryResults
)
```

**Parameters:**
- `userQuery` (string): Original user query
- `mergedData` (string): Merged data from databases
- `queryResults` (MultiDatabaseQueryResult): Query results

**Returns:** `RagResponse` with AI-generated answer

---

### ISQLQueryGenerator

**Purpose:** Generate and validate SQL queries

**Namespace:** `SmartRAG.Interfaces.Database`

Uses `ISqlDialectStrategy` for database-specific SQL.

#### Methods

```csharp
Task<string> GenerateSqlAsync(string userQuery, DatabaseSchemaInfo schema, DatabaseType databaseType);
bool ValidateSql(string sql, DatabaseSchemaInfo schema, out string errorMessage);
```

---

### IEmbeddingSearchService

**Purpose:** Embedding-based semantic search

**Namespace:** `SmartRAG.Interfaces.Search`

Core embedding search functionality.

#### Methods

```csharp
Task<List<DocumentChunk>> SearchByEmbeddingAsync(List<float> queryEmbedding, int maxResults = 5);
```

---

### ISourceBuilderService

**Purpose:** Build search result sources

**Namespace:** `SmartRAG.Interfaces.Search`

Constructs `SearchSource` objects from chunks.

#### Methods

```csharp
List<SearchSource> BuildSources(List<DocumentChunk> chunks);
```

---

### IAudioParserService

**Purpose:** Audio file parsing and transcription

**Namespace:** `SmartRAG.Interfaces.Parser`

#### Methods

```csharp
Task<AudioTranscriptionResult> TranscribeAudioAsync(Stream audioStream, string fileName, string language = null);
```

**Parameters:**
- `audioStream` (Stream): The audio stream to transcribe
- `fileName` (string): The name of the audio file for format detection
- `language` (string, optional): Language code for transcription (e.g., "tr-TR", "en-US", "auto")

**Returns:** `AudioTranscriptionResult` containing transcribed text, confidence score, and metadata

---

### IImageParserService

**Purpose:** Image OCR processing

**Namespace:** `SmartRAG.Interfaces.Parser`

#### Methods

```csharp
Task<string> ExtractTextFromImageAsync(Stream imageStream, string language = null);
Task<OcrResult> ExtractTextWithConfidenceAsync(Stream imageStream, string language = null);
Task<Stream> PreprocessImageAsync(Stream imageStream);
string CorrectCurrencySymbols(string text, string language = null);
```

**Parameters:**
- `imageStream` (Stream): The image stream to process
- `language` (string, optional): Language code for OCR (e.g., "eng", "tur"). If null, uses system locale automatically
- `text` (string): Text to correct currency symbols in

**Returns:**
- `ExtractTextFromImageAsync`: Extracted text as string
- `ExtractTextWithConfidenceAsync`: `OcrResult` with text, confidence scores, and text blocks
- `PreprocessImageAsync`: Preprocessed image stream
- `CorrectCurrencySymbols`: Text with corrected currency symbols (e.g., % → ₺, $, €)

---

### IAIProvider

**Purpose:** Low-level AI provider interface for text generation and embeddings

**Namespace:** `SmartRAG.Interfaces.AI`

Provider abstraction for multiple AI backends.

#### Methods

```csharp
Task<string> GenerateTextAsync(string prompt, AIProviderConfig config);
Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config);
Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, AIProviderConfig config);
Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000);
```

---

### IAIProviderFactory

**Purpose:** Factory for creating AI provider instances

**Namespace:** `SmartRAG.Interfaces.AI`

Factory pattern for AI provider creation.

#### Methods

```csharp
IAIProvider CreateProvider(AIProvider providerType);
```

---

### IPromptBuilderService

**Purpose:** Service for building AI prompts for different scenarios

**Namespace:** `SmartRAG.Interfaces.AI`

Centralized prompt construction with conversation history support.

#### Methods

```csharp
string BuildDocumentRagPrompt(string query, string context, string? conversationHistory = null, string? preferredLanguage = null);
string BuildHybridMergePrompt(string query, string? databaseContext, string? documentContext, string? conversationHistory = null, string? preferredLanguage = null);
string BuildConversationPrompt(string query, string? conversationHistory = null, string? preferredLanguage = null);
```

**Parameters:**
- `query` (string): User query
- `context` (string): Document context (for BuildDocumentRagPrompt)
- `databaseContext` (string?, optional): Database query results (for BuildHybridMergePrompt)
- `documentContext` (string?, optional): Document search results (for BuildHybridMergePrompt)
- `conversationHistory` (string?, optional): Previous conversation turns
- `preferredLanguage` (string?, optional): Preferred language code (e.g., "tr", "en") for explicit AI response language

---

### IDocumentRepository

**Purpose:** Repository interface for document storage operations

**Namespace:** `SmartRAG.Interfaces.Document`

Separated repository layer from business logic.

#### Methods

##### AddAsync

Adds a new document to storage.

```csharp
Task<Document> AddAsync(Document document)
```

##### GetByIdAsync

Retrieves document by unique identifier.

```csharp
Task<Document> GetByIdAsync(Guid id)
```

##### GetAllAsync

Retrieves all documents from storage.

```csharp
Task<List<Document>> GetAllAsync()
```

##### DeleteAsync

Removes document from storage by ID.

```csharp
Task<bool> DeleteAsync(Guid id)
```

##### GetCountAsync

Gets total count of documents in storage.

```csharp
Task<int> GetCountAsync()
```

##### SearchAsync

Searches documents using query string.

```csharp
Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5)
```

**Parameters:**
- `query` (string): Search query string
- `maxResults` (int): Maximum number of results to return (default: 5)

**Returns:** List of relevant document chunks

##### ClearAllAsync

Clear all documents from storage (efficient bulk delete).

```csharp
Task<bool> ClearAllAsync()
```

**Returns:** True if all documents were cleared successfully

---

### IDocumentScoringService

**Purpose:** Service for scoring document chunks based on query relevance

**Namespace:** `SmartRAG.Interfaces.Document`

Hybrid scoring strategy with keyword and semantic relevance.

#### Methods

```csharp
List<DocumentChunk> ScoreChunks(List<DocumentChunk> chunks, string query, List<string> queryWords, List<string> potentialNames);
double CalculateKeywordRelevanceScore(string query, string content);
```

---

### IAudioParserFactory

**Purpose:** Factory for creating audio parser service instances

**Namespace:** `SmartRAG.Interfaces.Parser`

Factory pattern for audio parser creation.

#### Methods

```csharp
IAudioParserService CreateAudioParser(AudioProvider provider);
```

---

### IStorageFactory

**Purpose:** Factory for creating document and conversation storage repositories

**Namespace:** `SmartRAG.Interfaces.Storage`

Unified factory for all storage operations.

#### Methods

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

**Purpose:** Interface for managing search result caching in Qdrant operations

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Search result caching for performance optimization.

#### Methods

```csharp
List<DocumentChunk> GetCachedResults(string queryHash);
void CacheResults(string queryHash, List<DocumentChunk> results);
void CleanupExpiredCache();
```

---

### IQdrantCollectionManager

**Purpose:** Interface for managing Qdrant collections and document storage

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Collection lifecycle management for Qdrant vector database.

#### Methods

```csharp
Task EnsureCollectionExistsAsync();
Task CreateCollectionAsync(string collectionName, int vectorDimension);
Task EnsureDocumentCollectionExistsAsync(string collectionName, Document document);
Task<int> GetVectorDimensionAsync();
```

---

### IQdrantEmbeddingService

**Purpose:** Interface for generating embeddings for text content

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Embedding generation for Qdrant vector storage.

#### Methods

```csharp
Task<List<float>> GenerateEmbeddingAsync(string text);
Task<int> GetVectorDimensionAsync();
```

---

### IQdrantSearchService

**Purpose:** Interface for performing searches in Qdrant vector database

**Namespace:** `SmartRAG.Interfaces.Storage.Qdrant`

Vector, text, and hybrid search capabilities for Qdrant.

#### Methods

```csharp
Task<List<DocumentChunk>> SearchAsync(List<float> queryEmbedding, int maxResults);
Task<List<DocumentChunk>> FallbackTextSearchAsync(string query, int maxResults);
Task<List<DocumentChunk>> HybridSearchAsync(string query, int maxResults);
```

---

### IQueryIntentClassifierService

**Purpose:** Service for classifying query intent (conversation vs information)

**Namespace:** `SmartRAG.Interfaces.Support`

AI-based query intent classification for hybrid routing.

#### Methods

```csharp
Task<bool> IsGeneralConversationAsync(string query, string? conversationHistory = null);
bool TryParseCommand(string input, out QueryCommandType commandType, out string payload);
```

**Command Types:**
- `QueryCommandType.None`: No command detected
- `QueryCommandType.NewConversation`: `/new` or `/reset` command
- `QueryCommandType.ForceConversation`: `/conv` command

---

### ITextNormalizationService

**Purpose:** Text normalization and cleaning

**Namespace:** `SmartRAG.Interfaces.Support`

#### Methods

```csharp
string NormalizeText(string text);
string RemoveExtraWhitespace(string text);
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
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-code"></i>
            </div>
            <h3>Examples</h3>
            <p>See practical code examples and real-world implementations</p>
            <a href="{{ site.baseurl }}/en/examples" class="btn btn-outline-primary btn-sm mt-3">
                View Examples
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-history"></i>
            </div>
            <h3>Changelog</h3>
            <p>Track new features, improvements, and breaking changes across all versions.</p>
            <a href="{{ site.baseurl }}/en/changelog" class="btn btn-outline-primary btn-sm mt-3">
                View Changelog
            </a>
        </div>
    </div>
</div>

