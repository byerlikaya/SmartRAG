---
layout: default
title: IDocumentSearchService
description: IDocumentSearchService interface documentation
lang: en
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


## Related Interfaces

- [Core Interfaces]({{ site.baseurl }}/en/api-reference/core) - Browse all core interfaces
- [API Reference]({{ site.baseurl }}/en/api-reference) - Back to API Reference index

