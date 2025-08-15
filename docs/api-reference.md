# 📚 API Reference

Complete reference for SmartRAG API interfaces and services.

## 🎯 Core Interfaces

### IDocumentService

Main service for document operations and RAG workflows.

```csharp
public interface IDocumentService
{
    Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy);
    Task<List<Document>> SearchDocumentsAsync(string query, int maxResults = 10, float minSimilarity = 0.0f);
    Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5);
    Task<Document?> GetDocumentByIdAsync(Guid documentId);
    Task<bool> DeleteDocumentAsync(Guid documentId);
}
```

#### Methods

##### UploadDocumentAsync
Uploads and processes a document.

**Parameters:**
- `fileStream` (Stream): Document file stream
- `fileName` (string): Original file name
- `contentType` (string): MIME content type
- `uploadedBy` (string): User identifier

**Returns:** `Task<Document>` - Processed document with chunks

**Example:**
```csharp
var document = await documentService.UploadDocumentAsync(
    file.OpenReadStream(),
    "contract.pdf",
    "application/pdf",
    "user-123"
);
```

##### SearchDocumentsAsync
Performs semantic search across documents.

**Parameters:**
- `query` (string): Search query
- `maxResults` (int): Maximum results to return (default: 10)
- `minSimilarity` (float): Minimum similarity threshold (default: 0.0)

**Returns:** `Task<List<Document>>` - Matching documents

**Example:**
```csharp
var results = await documentService.SearchDocumentsAsync(
    "machine learning algorithms",
    maxResults: 5,
    minSimilarity: 0.7f
);
```

##### GenerateRagAnswerAsync
Generates AI-powered answers based on document content.

**Parameters:**
- `query` (string): Question to answer
- `maxResults` (int): Max relevant chunks to consider (default: 5)

**Returns:** `Task<RagResponse>` - AI-generated answer with sources

**Example:**
```csharp
var response = await documentService.GenerateRagAnswerAsync(
    "What are the main risks mentioned in the financial report?"
);
```

### IAIProvider

Interface for AI service providers.

```csharp
public interface IAIProvider
{
    Task<string> GenerateTextAsync(string prompt, AIProviderConfig config);
    Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config);
    Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000);
}
```

### IDocumentRepository

Storage abstraction for documents.

```csharp
public interface IDocumentRepository
{
    Task<Document> SaveDocumentAsync(Document document);
    Task<Document?> GetDocumentByIdAsync(Guid documentId);
    Task<List<Document>> SearchDocumentsAsync(string query, int maxResults = 10, float minSimilarity = 0.0f);
    Task<bool> DeleteDocumentAsync(Guid documentId);
    Task<List<Document>> GetAllDocumentsAsync();
}
```

## 🔧 Configuration Models

### AIProviderConfig

Configuration for AI providers.

```csharp
public class AIProviderConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 4096;
    public float Temperature { get; set; } = 0.7f;
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 3;
}
```

### StorageConfig

Base configuration for storage providers.

```csharp
public class StorageConfig
{
    public StorageProvider Provider { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public Dictionary<string, string> AdditionalSettings { get; set; } = new();
}
```

### SmartRagOptions

Main configuration options.

```csharp
public class SmartRagOptions
{
    public AIProvider AIProvider { get; set; } = AIProvider.OpenAI;
    public StorageProvider StorageProvider { get; set; } = StorageProvider.InMemory;
    public int MaxChunkSize { get; set; } = 1000;
    public int MinChunkSize { get; set; } = 100;
    public int ChunkOverlap { get; set; } = 200;
    public float SemanticSearchThreshold { get; set; } = 0.3f;
    public bool EnableFallbackProviders { get; set; } = false;
    public List<AIProvider> FallbackProviders { get; set; } = new();
}
```

## 📊 Response Models

### RagResponse

Response from RAG query operations.

```csharp
public class RagResponse
{
    public string Query { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<SearchSource> Sources { get; set; } = new();
    public int ProcessingTimeMs { get; set; }
    public float ConfidenceScore { get; set; }
}
```

### SearchSource

Source information for RAG responses.

```csharp
public class SearchSource
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ChunkContent { get; set; } = string.Empty;
    public float RelevanceScore { get; set; }
    public int ChunkIndex { get; set; }
}
```

### Document

Document entity model.

```csharp
public class Document
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<DocumentChunk> Chunks { get; set; } = new();
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long FileSize { get; set; }
}
```

### DocumentChunk

Document chunk entity.

```csharp
public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<float> Embedding { get; set; } = new();
    public int ChunkIndex { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
}
```

## 🔢 Enums

### AIProvider

Available AI providers.

```csharp
public enum AIProvider
{
    OpenAI,
    Anthropic,
    Gemini,
    AzureOpenAI,
    Custom
}
```

### StorageProvider

Available storage providers.

```csharp
public enum StorageProvider
{
    InMemory,
    FileSystem,
    Sqlite,
    Redis,
    Qdrant
}
```

## 🚀 Extension Methods

### UseSmartRAG

Main extension method for service registration.

```csharp
public static IServiceCollection UseSmartRAG(
    this IServiceCollection services,
    IConfiguration configuration,
    StorageProvider storageProvider = StorageProvider.InMemory,
    AIProvider aiProvider = AIProvider.OpenAI
)
```

**Example:**
```csharp
services.UseSmartRAG(configuration,
    storageProvider: StorageProvider.Qdrant,
    aiProvider: AIProvider.OpenAI
);
```

## 🛠️ Factory Interfaces

### IAIProviderFactory

Factory for creating AI providers.

```csharp
public interface IAIProviderFactory
{
    IAIProvider CreateProvider(AIProvider providerType);
}
```

### IStorageFactory

Factory for creating storage repositories.

```csharp
public interface IStorageFactory
{
    IDocumentRepository CreateRepository(StorageProvider providerType, StorageConfig config);
}
```

## 📝 Usage Examples

See [Getting Started](getting-started.md) for complete usage examples and configuration.

## 🆘 Support

For questions about the API, please:
- 📖 Check the [main documentation](../README.md)
- 🐛 [Open an issue](https://github.com/byerlikaya/SmartRAG/issues)
- 📧 [Contact support](mailto:b.yerlikaya@outlook.com)
