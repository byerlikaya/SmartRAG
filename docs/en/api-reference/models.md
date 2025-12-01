---
layout: default
title: Data Models
description: SmartRAG data models - RagResponse, Document, DocumentChunk, DatabaseConfig and other data structures
lang: en
---

## Data Models

### RagResponse

AI-generated response with sources and configuration metadata.

```csharp
public class RagResponse
{
    public string Query { get; set; }                    // Original query
    public string Answer { get; set; }                    // AI-generated answer
    public List<SearchSource> Sources { get; set; }       // Source documents
    public DateTime SearchedAt { get; set; }             // Timestamp
    public RagConfiguration Configuration { get; set; }  // Provider configuration
}
```

**RagConfiguration:**

```csharp
public class RagConfiguration
{
    public string AIProvider { get; set; }      // AI provider used
    public string StorageProvider { get; set; }  // Storage provider used
    public string Model { get; set; }            // Model name used
}
```

**Example Response:**

```json
{
  "query": "What is RAG?",
  "answer": "RAG (Retrieval-Augmented Generation) is...",
  "sources": [
    {
      "sourceType": "Document",
      "documentId": "abc-123",
      "fileName": "ml-guide.pdf",
      "relevantContent": "RAG combines retrieval...",
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

Document entity with metadata and chunks.

```csharp
public class Document
{
    public Guid Id { get; set; }                          // Document ID
    public string FileName { get; set; }                  // Original file name
    public string ContentType { get; set; }               // MIME type
    public string Content { get; set; }                    // Extracted text content
    public string UploadedBy { get; set; }                // User identifier
    public DateTime UploadedAt { get; set; }              // Upload timestamp
    public List<DocumentChunk> Chunks { get; set; }       // Document chunks
    public Dictionary<string, object> Metadata { get; set; } // Optional metadata
    public long FileSize { get; set; }                     // File size in bytes
}
```

### DocumentChunk

Document chunk with embedding and relevance score.

```csharp
public class DocumentChunk
{
    public Guid Id { get; set; }                          // Chunk ID
    public Guid DocumentId { get; set; }                 // Parent document ID
    public string Content { get; set; }                   // Chunk text content
    public int ChunkIndex { get; set; }                    // Position in document
    public List<float> Embedding { get; set; }             // Vector embedding
    public double? RelevanceScore { get; set; }            // Similarity score (0-1)
    public DateTime CreatedAt { get; set; }                // Creation timestamp
    public int StartPosition { get; set; }                 // Start position in document
    public int EndPosition { get; set; }                  // End position in document
}
```

### SearchSource

Represents a search result source with document information and relevance score.

```csharp
public class SearchSource
{
    public string SourceType { get; set; }                // Type: Document, Audio, Database, Image, System
    public Guid DocumentId { get; set; }                  // Document ID (if applicable)
    public string FileName { get; set; }                  // File name
    public string RelevantContent { get; set; }           // Relevant content excerpt
    public double RelevanceScore { get; set; }             // Relevance score (0-1)
    public string? Location { get; set; }                  // Location metadata
    public string? DatabaseId { get; set; }                // Database ID (if applicable)
    public string? DatabaseName { get; set; }              // Database name (if applicable)
    public List<string> Tables { get; set; }               // Tables referenced (if applicable)
    public string? ExecutedQuery { get; set; }             // Executed query (if applicable)
    public int? RowNumber { get; set; }                    // Row number (if applicable)
    public double? StartTimeSeconds { get; set; }          // Start time for audio (if applicable)
    public double? EndTimeSeconds { get; set; }            // End time for audio (if applicable)
    public int? ChunkIndex { get; set; }                   // Chunk index (if applicable)
    public int? StartPosition { get; set; }                // Start position (if applicable)
    public int? EndPosition { get; set; }                  // End position (if applicable)
}
```

### SearchOptions

Options for configuring a specific search request.

```csharp
public class SearchOptions
{
    public bool EnableDatabaseSearch { get; set; } = true;    // Enable database search
    public bool EnableDocumentSearch { get; set; } = true;    // Enable document search
    public bool EnableAudioSearch { get; set; } = true;        // Enable audio search
    public bool EnableImageSearch { get; set; } = true;       // Enable image search
    public string? PreferredLanguage { get; set; }             // Preferred language (ISO 639-1)
    
    public static SearchOptions Default => new SearchOptions();
    public static SearchOptions FromConfig(SmartRagOptions options) { ... }
}
```

### QueryIntent

Represents AI-analyzed query intent for multi-database querying.

```csharp
public class QueryIntent
{
    public string OriginalQuery { get; set; }                    // Original user query
    public string QueryUnderstanding { get; set; }               // AI's understanding
    public List<DatabaseQueryIntent> DatabaseQueries { get; set; } // Required queries
    public double Confidence { get; set; }                        // Confidence level (0-1)
    public bool RequiresCrossDatabaseJoin { get; set; }          // Cross-DB join needed
    public string Reasoning { get; set; }                        // AI reasoning
}
```

**DatabaseQueryIntent:**

```csharp
public class DatabaseQueryIntent
{
    public string DatabaseId { get; set; }                       // Database ID
    public string DatabaseName { get; set; }                     // Database name
    public List<string> RequiredTables { get; set; }             // Tables to query
    public Dictionary<string, List<string>> RequiredColumns { get; set; } // Columns needed
    public string GeneratedQuery { get; set; }                   // Generated SQL
    public string Purpose { get; set; }                           // Query purpose
    public int Priority { get; set; } = 1;                      // Priority (higher = more important)
}
```

### DatabaseConfig

Configuration for database parsing operations.

```csharp
public class DatabaseConfig
{
    public DatabaseType Type { get; set; }                      // Database type
    public string ConnectionString { get; set; }                 // Connection string
    public List<string> IncludedTables { get; set; }             // Tables to include
    public List<string> ExcludedTables { get; set; }             // Tables to exclude
    public int MaxRowsPerTable { get; set; } = 1000;             // Max rows per table
    public bool IncludeSchema { get; set; } = true;              // Include schema info
    public bool IncludeIndexes { get; set; } = false;            // Include index info
    public bool IncludeForeignKeys { get; set; } = true;         // Include foreign keys
    public int QueryTimeoutSeconds { get; set; } = 30;           // Query timeout
    public bool SanitizeSensitiveData { get; set; } = true;     // Sanitize sensitive data
    public List<string> SensitiveColumns { get; set; }            // Sensitive column patterns
    public bool EnableConnectionPooling { get; set; } = true;     // Enable connection pooling
    public int MaxPoolSize { get; set; } = 10;                   // Max pool size
    public int MinPoolSize { get; set; } = 2;                    // Min pool size
    public bool EnableQueryCaching { get; set; } = true;        // Enable query caching
    public int CacheDurationMinutes { get; set; } = 30;         // Cache duration
    public bool EnableParallelProcessing { get; set; } = true;   // Enable parallel processing
    public int MaxDegreeOfParallelism { get; set; } = 3;        // Max parallelism
    public bool EnableStreaming { get; set; } = true;            // Enable streaming
    public int StreamingBatchSize { get; set; } = 1000;         // Streaming batch size
    public int MaxMemoryThresholdMB { get; set; } = 500;         // Memory threshold
    public bool EnableAutoGarbageCollection { get; set; } = true; // Auto GC
    public bool ForceStreamingMode { get; set; } = false;       // Force streaming
    public int MaxStringBuilderCapacity { get; set; } = 65536;  // String builder capacity
}
```

### DatabaseConnectionConfig

Configuration for a database connection.

```csharp
public class DatabaseConnectionConfig
{
    public string Name { get; set; }                            // Optional connection name
    public string ConnectionString { get; set; }                // Connection string
    public DatabaseType DatabaseType { get; set; }               // Database type
    public string Description { get; set; }                       // Optional description
    public bool Enabled { get; set; } = true;                    // Whether enabled
    public int MaxRowsPerQuery { get; set; }                     // Max rows per query
    public int QueryTimeoutSeconds { get; set; }                 // Query timeout
    public int SchemaRefreshIntervalMinutes { get; set; } = 0;   // Auto-refresh interval
    public string[] IncludedTables { get; set; }                 // Tables to include
    public string[] ExcludedTables { get; set; }                 // Tables to exclude
}
```

### DatabaseConnectionRequest

Request model for database connection operations.

```csharp
public class DatabaseConnectionRequest
{
    public string ConnectionString { get; set; }                // Connection string
    public DatabaseType DatabaseType { get; set; }               // Database type
    public List<string> IncludedTables { get; set; }              // Tables to include
    public List<string> ExcludedTables { get; set; }              // Tables to exclude
    public int MaxRows { get; set; } = 1000;                      // Max rows
    public bool IncludeSchema { get; set; } = true;              // Include schema
    public bool IncludeForeignKeys { get; set; } = true;          // Include foreign keys
    public bool SanitizeSensitiveData { get; set; } = true;      // Sanitize sensitive data
}
```

### DatabaseSchemaInfo

Comprehensive schema information for a database.

```csharp
public class DatabaseSchemaInfo
{
    public string DatabaseId { get; set; }                      // Unique database ID
    public string DatabaseName { get; set; }                     // Database name
    public DatabaseType DatabaseType { get; set; }               // Database type
    public string Description { get; set; }                      // Description
    public DateTime LastAnalyzed { get; set; }                   // Last analysis time
    public List<TableSchemaInfo> Tables { get; set; }           // Tables
    public string AISummary { get; set; }                        // AI-generated summary
    public long TotalRowCount { get; set; }                      // Total row count
    public SchemaAnalysisStatus Status { get; set; }              // Analysis status
    public string ErrorMessage { get; set; }                     // Error message (if failed)
}
```

**TableSchemaInfo:**

```csharp
public class TableSchemaInfo
{
    public string TableName { get; set; }                        // Table name
    public List<ColumnSchemaInfo> Columns { get; set; }          // Columns
    public List<string> PrimaryKeys { get; set; }                 // Primary keys
    public List<ForeignKeyInfo> ForeignKeys { get; set; }         // Foreign keys
    public long RowCount { get; set; }                           // Row count
    public string AIDescription { get; set; }                    // AI description
    public string SampleData { get; set; }                       // Sample data
}
```

**ColumnSchemaInfo:**

```csharp
public class ColumnSchemaInfo
{
    public string ColumnName { get; set; }                       // Column name
    public string DataType { get; set; }                         // Data type
    public bool IsNullable { get; set; }                          // Is nullable
    public bool IsPrimaryKey { get; set; }                       // Is primary key
    public bool IsForeignKey { get; set; }                       // Is foreign key
    public int? MaxLength { get; set; }                          // Max length (for strings)
}
```

**ForeignKeyInfo:**

```csharp
public class ForeignKeyInfo
{
    public string ForeignKeyName { get; set; }                  // Foreign key name
    public string ColumnName { get; set; }                       // Column in current table
    public string ReferencedTable { get; set; }                  // Referenced table
    public string ReferencedColumn { get; set; }                 // Referenced column
}
```

**SchemaAnalysisStatus:**

```csharp
public enum SchemaAnalysisStatus
{
    Pending,        // Analysis pending
    InProgress,     // Analysis in progress
    Completed,      // Analysis completed
    Failed,         // Analysis failed
    RefreshNeeded   // Schema refresh needed
}
```

### SmartRagOptions

Configuration options for SmartRag library.

```csharp
public class SmartRagOptions
{
    public AIProvider AIProvider { get; set; }                   // AI provider
    public StorageProvider StorageProvider { get; set; }         // Storage provider
    public ConversationStorageProvider? ConversationStorageProvider { get; set; } // Conversation storage
    public int MaxChunkSize { get; set; } = 1000;                 // Max chunk size
    public int MinChunkSize { get; set; } = 100;                  // Min chunk size
    public int ChunkOverlap { get; set; } = 200;                  // Chunk overlap
    public int MaxRetryAttempts { get; set; } = 3;                // Max retry attempts
    public int RetryDelayMs { get; set; } = 1000;                 // Retry delay
    public RetryPolicy RetryPolicy { get; set; }                 // Retry policy
    public bool EnableFallbackProviders { get; set; }            // Enable fallback
    public List<AIProvider> FallbackProviders { get; set; }     // Fallback providers
    public AudioProvider AudioProvider { get; set; }              // Audio provider
    public WhisperConfig WhisperConfig { get; set; }             // Whisper config
    public List<DatabaseConnectionConfig> DatabaseConnections { get; set; } // DB connections
    public bool EnableAutoSchemaAnalysis { get; set; } = true;   // Auto schema analysis
    public bool EnablePeriodicSchemaRefresh { get; set; } = true; // Periodic refresh
    public int DefaultSchemaRefreshIntervalMinutes { get; set; } = 60; // Refresh interval
    public FeatureToggles Features { get; set; }                 // Feature toggles
}
```

**FeatureToggles:**

```csharp
public class FeatureToggles
{
    public bool EnableDatabaseSearch { get; set; } = true;       // Enable database search
    public bool EnableDocumentSearch { get; set; } = true;      // Enable document search
    public bool EnableAudioParsing { get; set; } = true;         // Enable audio parsing
    public bool EnableImageParsing { get; set; } = true;         // Enable image parsing
}
```

### AudioTranscriptionResult

Represents the result of audio transcription processing.

```csharp
public class AudioTranscriptionResult
{
    public string Text { get; set; }                             // Transcribed text
    public double Confidence { get; set; }                        // Confidence score (0-1)
    public string Language { get; set; }                         // Detected language
    public Dictionary<string, object> Metadata { get; set; }      // Additional metadata
}
```

### AudioTranscriptionOptions

Configuration options for audio transcription processing.

```csharp
public class AudioTranscriptionOptions
{
    public string Language { get; set; } = "tr-TR";              // Language code
    public double MinConfidenceThreshold { get; set; } = 0.5;    // Min confidence (0-1)
    public bool IncludeWordTimestamps { get; set; } = false;     // Include word timestamps
}
```

### AudioSegmentMetadata

Represents metadata for a single audio transcription segment.

```csharp
public class AudioSegmentMetadata
{
    public double Start { get; set; }                           // Start time (seconds)
    public double End { get; set; }                             // End time (seconds)
    public string Text { get; set; }                             // Transcribed text
    public double Probability { get; set; }                     // Confidence probability
    public string NormalizedText { get; set; }                   // Normalized text
    public int StartCharIndex { get; set; }                      // Start character index
    public int EndCharIndex { get; set; }                        // End character index
}
```

### OcrResult

Represents the result of an OCR operation.

```csharp
public class OcrResult
{
    public string Text { get; set; }                             // Extracted text
    public float Confidence { get; set; }                        // Confidence score
    public long ProcessingTimeMs { get; set; }                   // Processing time (ms)
    public int WordCount { get; set; }                           // Word count
    public string Language { get; set; }                         // Language used
}
```

### AIProviderConfig

Configuration for AI providers.

```csharp
public class AIProviderConfig
{
    public string ApiKey { get; set; }                            // API key
    public string EmbeddingApiKey { get; set; }                  // Embedding API key (optional)
    public string Endpoint { get; set; }                         // Custom endpoint (optional)
    public string EmbeddingEndpoint { get; set; }                // Embedding endpoint (optional)
    public string ApiVersion { get; set; }                       // API version (optional)
    public string Model { get; set; }                            // Model name
    public string EmbeddingModel { get; set; }                   // Embedding model (optional)
    public int MaxTokens { get; set; } = 4096;                   // Max tokens
    public double Temperature { get; set; } = 0.7;               // Temperature (0-1)
    public string SystemMessage { get; set; }                    // System message (optional)
    public int? EmbeddingMinIntervalMs { get; set; }             // Embedding min interval (ms)
}
```

### WhisperConfig

Configuration for Whisper audio transcription.

```csharp
public class WhisperConfig
{
    public string ModelPath { get; set; } = "models/ggml-base.bin"; // Model file path
    public string DefaultLanguage { get; set; } = "auto";         // Default language
    public double MinConfidenceThreshold { get; set; } = 0.3;     // Min confidence (0-1)
    public bool IncludeWordTimestamps { get; set; } = false;       // Include word timestamps
    public string PromptHint { get; set; } = string.Empty;        // Context hint
    public int MaxThreads { get; set; } = 0;                      // Max threads (0 = auto)
}
```

### GoogleSpeechConfig

Configuration options for Google Speech-to-Text service.

```csharp
public class GoogleSpeechConfig
{
    public string ApiKey { get; set; }                           // API key or service account JSON path
    public string DefaultLanguage { get; set; } = "tr-TR";      // Default language
    public double MinConfidenceThreshold { get; set; } = 0.5;    // Min confidence (0-1)
    public bool IncludeWordTimestamps { get; set; } = false;      // Include word timestamps
    public bool EnableAutomaticPunctuation { get; set; } = true;  // Enable auto punctuation
    public bool EnableSpeakerDiarization { get; set; } = false;   // Enable speaker diarization
    public int MaxSpeakerCount { get; set; } = 2;                 // Max speaker count
}
```

### RedisConfig

Redis storage configuration.

```csharp
public class RedisConfig
{
    public string ConnectionString { get; set; } = "localhost:6379"; // Connection string
    public string Password { get; set; }                            // Password
    public string Username { get; set; }                             // Username
    public int Database { get; set; }                                // Database number
    public string KeyPrefix { get; set; } = "smartrag:doc:";        // Key prefix
    public int ConnectionTimeout { get; set; } = 30;                // Connection timeout (seconds)
    public bool EnableSsl { get; set; }                              // Enable SSL
    public bool UseSsl { get; set; }                                 // Use SSL (alias)
    public bool EnableVectorSearch { get; set; } = true;             // Enable vector search
    public string VectorIndexAlgorithm { get; set; } = "HNSW";      // Vector index algorithm
    public string DistanceMetric { get; set; } = "COSINE";          // Distance metric
    public int VectorDimension { get; set; } = 768;                  // Vector dimension
    public string VectorIndexName { get; set; } = "smartrag_vector_idx"; // Vector index name
    public int RetryCount { get; set; } = 3;                         // Retry count
    public int RetryDelay { get; set; } = 1000;                      // Retry delay (ms)
}
```

### QdrantConfig

Configuration settings for Qdrant vector database storage.

```csharp
public class QdrantConfig
{
    public string Host { get; set; } = "localhost";              // Server host
    public bool UseHttps { get; set; }                            // Use HTTPS
    public string ApiKey { get; set; } = string.Empty;            // API key
    public string CollectionName { get; set; } = "smartrag_documents"; // Collection name
    public int VectorSize { get; set; } = 768;                     // Vector size
    public string DistanceMetric { get; set; } = "Cosine";        // Distance metric
}
```

### SqliteConfig

SQLite storage configuration.

```csharp
public class SqliteConfig
{
    public string DatabasePath { get; set; } = "SmartRag.db";     // Database file path
    public bool EnableForeignKeys { get; set; } = true;            // Enable foreign keys
}
```

### InMemoryConfig

In-memory storage configuration.

```csharp
public class InMemoryConfig
{
    public int MaxDocuments { get; set; } = 1000;                  // Max documents in memory
}
```

### StorageConfig

Storage configuration for different storage providers.

```csharp
public class StorageConfig
{
    public StorageProvider Provider { get; set; } = StorageProvider.InMemory; // Storage provider
    public RedisConfig Redis { get; set; } = new RedisConfig();   // Redis config
    public InMemoryConfig InMemory { get; set; } = new InMemoryConfig(); // InMemory config
    public QdrantConfig Qdrant { get; set; } = new QdrantConfig(); // Qdrant config
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
