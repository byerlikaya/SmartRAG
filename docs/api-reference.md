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
Uploads and processes a document with enhanced chunking.

**Parameters:**
- `fileStream` (Stream): Document file stream
- `fileName` (string): Original file name
- `contentType` (string): MIME content type
- `uploadedBy` (string): User identifier

**Returns:** `Task<Document>` - Processed document with smart chunks

**Example:**
```csharp
var document = await documentService.UploadDocumentAsync(
    file.OpenReadStream(),
    "contract.pdf",
    "application/pdf",
    "user-123"
);
```

**Features:**
- **Smart Chunking**: Word boundary validation and optimal break points
- **Context Preservation**: Maintains semantic continuity between chunks
- **Multi-format Support**: PDF, Word, text files with intelligent parsing

##### SearchDocumentsAsync
Performs enhanced semantic search across documents with hybrid scoring.

**Parameters:**
- `query` (string): Search query
- `maxResults` (int): Maximum results to return (default: 10)
- `minSimilarity` (float): Minimum similarity threshold (default: 0.0)

**Returns:** `Task<List<Document>>` - Matching documents with relevance scores

**Example:**
```csharp
var results = await documentService.SearchDocumentsAsync(
    "machine learning algorithms",
    maxResults: 5,
    minSimilarity: 0.7f
);
```

**Enhanced Features:**
- **Hybrid Scoring**: 80% semantic similarity + 20% keyword relevance
- **Contextual Analysis**: Semantic coherence and contextual keyword detection
- **Word Boundary Protection**: Never cuts words in the middle

##### GenerateRagAnswerAsync
Generates AI-powered answers based on document content with enhanced search.

**Parameters:**
- `query` (string): Question to answer
- `maxResults` (int): Max relevant chunks to consider (default: 5)

**Returns:** `Task<RagResponse>` - AI-generated answer with sources and relevance scores

**Example:**
```csharp
var response = await documentService.GenerateRagAnswerAsync(
    "What are the main risks mentioned in the financial report?"
);
```

### SemanticSearchService

Advanced semantic search service for enhanced relevance scoring.

```csharp
public class SemanticSearchService
{
    public async Task<double> CalculateEnhancedSemanticSimilarityAsync(string query, string content);
}
```

#### Methods

##### CalculateEnhancedSemanticSimilarityAsync
Calculates enhanced semantic similarity using advanced text analysis.

**Parameters:**
- `query` (string): Search query
- `content` (string): Document chunk content

**Returns:** `Task<double>` - Enhanced semantic similarity score (0.0 - 1.0)

**Features:**
- **Token-based Analysis**: Intelligent text chunking for analysis
- **Contextual Enhancement**: Semantic coherence and contextual keyword detection
- **Domain Independence**: Generic scoring without hardcoded patterns
- **Performance Optimized**: Efficient algorithms for real-time search

**Example:**
```csharp
var semanticScore = await _semanticSearchService
    .CalculateEnhancedSemanticSimilarityAsync(query, chunk.Content);
```

### IAIProvider

Interface for AI service providers.

```csharp
public interface IAIProvider
{
    Task<string> GenerateTextAsync(string prompt, AIProviderConfig config);
    Task<List<float>> GenerateEmbeddingAsync(string text, AIProviderConfig config);
    Task<List<List<float>>> GenerateEmbeddingsBatchAsync(List<string> texts, AIProviderConfig config);
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
    
    // Anthropic-specific: VoyageAI embedding configuration
    public string? EmbeddingApiKey { get; set; }
    public string? EmbeddingEndpoint { get; set; }
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

Main configuration options with enhanced features.

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
    
    // Enhanced chunking options
    public bool EnableWordBoundaryValidation { get; set; } = true;
    public bool EnableOptimalBreakPoints { get; set; } = true;
    
    // Hybrid scoring weights
    public float SemanticScoringWeight { get; set; } = 0.8f;
    public float KeywordScoringWeight { get; set; } = 0.2f;
}
```

## 📊 Response Models

### RagResponse

Response from RAG query operations with enhanced metadata.

```csharp
public class RagResponse
{
    public string Query { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<SearchSource> Sources { get; set; } = new();
    public int ProcessingTimeMs { get; set; }
    public float ConfidenceScore { get; set; }
    
    // Enhanced scoring information
    public float AverageSemanticScore { get; set; }
    public float AverageKeywordScore { get; set; }
    public float HybridScore { get; set; }
    public string UsedAIProvider { get; set; } = string.Empty;
    public string UsedStorageProvider { get; set; } = string.Empty;
}
```

### SearchSource

Source information for RAG responses with enhanced scoring.

```csharp
public class SearchSource
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ChunkContent { get; set; } = string.Empty;
    public float RelevanceScore { get; set; }
    public int ChunkIndex { get; set; }
    
    // Enhanced scoring breakdown
    public float SemanticScore { get; set; }
    public float KeywordScore { get; set; }
    public float HybridScore { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
}
```

### Document

Document entity model with enhanced chunking information.

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
    
    // Enhanced chunking metadata
    public int TotalChunks { get; set; }
    public int AverageChunkSize { get; set; }
    public bool HasEmbeddings { get; set; }
}
```

### DocumentChunk

Document chunk entity with enhanced positioning and validation.

```csharp
public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<float>? Embedding { get; set; }
    public int ChunkIndex { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    public DateTime CreatedAt { get; set; }
    public double? RelevanceScore { get; set; }
    
    // Enhanced chunking validation
    public bool IsWordBoundaryValid { get; set; }
    public string BreakPointType { get; set; } = string.Empty; // "sentence", "paragraph", "word"
    public int OverlapWithPrevious { get; set; }
    public int OverlapWithNext { get; set; }
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

### RetryPolicy

Retry policies for AI provider operations.

```csharp
public enum RetryPolicy
{
    None,
    ExponentialBackoff,
    LinearBackoff,
    FixedDelay
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

### AddSmartRAG

Advanced configuration with custom options.

```csharp
public static IServiceCollection AddSmartRAG(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<SmartRagOptions> configureOptions
)
```

**Example:**
```csharp
services.AddSmartRAG(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
    options.MaxChunkSize = 1200;
    options.EnableWordBoundaryValidation = true;
    options.SemanticScoringWeight = 0.8f;
    options.KeywordScoringWeight = 0.2f;
});
```

## 🛠️ Factory Interfaces

### IAIProviderFactory

Factory for creating AI providers.

```csharp
public interface IAIProviderFactory
{
    IAIProvider CreateProvider(AIProvider providerType);
    IAIProvider CreateProvider(AIProvider providerType, AIProviderConfig config);
}
```

### IStorageFactory

Factory for creating storage repositories.

```csharp
public interface IStorageFactory
{
    IDocumentRepository CreateRepository(StorageProvider providerType, StorageConfig config);
    IDocumentRepository GetCurrentRepository();
}
```

## 🔍 Enhanced Search & Chunking

### Hybrid Scoring Algorithm

SmartRAG uses a sophisticated hybrid scoring system:

```csharp
// 1. Semantic Similarity (80% weight)
var semanticScore = await _semanticSearchService
    .CalculateEnhancedSemanticSimilarityAsync(query, chunk.Content);

// 2. Keyword Relevance (20% weight)
var keywordScore = CalculateKeywordRelevanceScore(query, chunk.Content);

// 3. Hybrid Score
var hybridScore = (semanticScore * 0.8) + (keywordScore * 0.2);

// 4. Apply additional enhancements
if (ContainsContextualKeywords(query, chunk.Content))
    hybridScore *= 1.2;
if (HasSemanticCoherence(query, chunk.Content))
    hybridScore *= 1.15;
```

### Smart Chunking Algorithm

Advanced chunking with word boundary validation:

```csharp
// 1. Find optimal break point
var breakPoint = FindOptimalBreakPoint(content, startIndex, maxChunkSize);

// 2. Validate word boundaries
var validatedBreakPoint = ValidateWordBoundary(content, breakPoint);

// 3. Validate chunk boundaries (start and end)
var (validatedStart, validatedEnd) = ValidateChunkBoundaries(
    content, startIndex, endIndex);

// 4. Create chunk with validated boundaries
var chunkContent = content.Substring(validatedStart, validatedEnd - validatedStart);
```

## 📝 Usage Examples

See [Getting Started](getting-started.md) for complete usage examples and configuration.

## 🆘 Support

For questions about the API, please:
- 📖 Check the [main documentation](../README.md)
- 🐛 [Open an issue](https://github.com/byerlikaya/SmartRAG/issues)
- 📧 [Contact support](mailto:b.yerlikaya@outlook.com)
