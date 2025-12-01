---
layout: default
title: Quick Examples
description: Simple, practical examples to get started quickly
lang: en
---

## Quick Examples

### 1. Simple Document Search

Upload a document and search it:

```csharp
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _searchService;
    
    // Upload document
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "user-123"
        );
        
        return Ok(new { 
            id = document.Id, 
            fileName = document.FileName,
            chunks = document.Chunks.Count 
        });
    }
    
    // Search documents
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        var response = await _searchService.QueryIntelligenceAsync(
            request.Query,
            maxResults: request.MaxResults
        );
        
        return Ok(response);
    }
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 5;
}
```

---

### 2. Search Options and Flag-Based Filtering

Control which data sources to search using `SearchOptions`:

```csharp
public class IntelligenceController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] QuestionRequest request)
    {
        // Option 1: Use SearchOptions directly
        var options = new SearchOptions
        {
            EnableDatabaseSearch = true,
            EnableDocumentSearch = false,
            EnableAudioSearch = false,
            EnableImageSearch = false,
            PreferredLanguage = "en"
        };
        
        var response = await _searchService.QueryIntelligenceAsync(
            request.Question,
            maxResults: 5,
            options: options
        );
        
        return Ok(response);
    }
    
    [HttpPost("ask-with-flags")]
    public async Task<IActionResult> AskWithFlags([FromBody] string query)
    {
        // Option 2: Parse flags from query string
        var searchOptions = ParseSearchOptions(query, out string cleanQuery);
        
        var response = await _searchService.QueryIntelligenceAsync(
            cleanQuery,
            maxResults: 5,
            options: searchOptions
        );
        
        return Ok(response);
    }
    
    private SearchOptions? ParseSearchOptions(string input, out string cleanQuery)
    {
        cleanQuery = input;
        
        var hasDocumentFlag = input.Contains("-d ", StringComparison.OrdinalIgnoreCase) 
            || input.EndsWith("-d", StringComparison.OrdinalIgnoreCase);
        var hasDatabaseFlag = input.Contains("-db ", StringComparison.OrdinalIgnoreCase) 
            || input.EndsWith("-db", StringComparison.OrdinalIgnoreCase);
        var hasAudioFlag = input.Contains("-a ", StringComparison.OrdinalIgnoreCase) 
            || input.EndsWith("-a", StringComparison.OrdinalIgnoreCase);
        var hasImageFlag = input.Contains("-i ", StringComparison.OrdinalIgnoreCase) 
            || input.EndsWith("-i", StringComparison.OrdinalIgnoreCase);
        
        if (!hasDocumentFlag && !hasDatabaseFlag && !hasAudioFlag && !hasImageFlag)
        {
            return null; // Use default options
        }
        
        var options = new SearchOptions
        {
            EnableDocumentSearch = hasDocumentFlag,
            EnableDatabaseSearch = hasDatabaseFlag,
            EnableAudioSearch = hasAudioFlag,
            EnableImageSearch = hasImageFlag
        };
        
        // Remove flags from query
        var parts = input.Split(' ');
        var cleanParts = parts.Where(p => 
            !p.Equals("-d", StringComparison.OrdinalIgnoreCase) && 
            !p.Equals("-db", StringComparison.OrdinalIgnoreCase) && 
            !p.Equals("-a", StringComparison.OrdinalIgnoreCase) && 
            !p.Equals("-i", StringComparison.OrdinalIgnoreCase));
            
        cleanQuery = string.Join(" ", cleanParts);
        
        return options;
    }
}
```

**Flag Examples:**
- `"-db Show top customers"` → Database search only
- `"-a What was discussed?"` → Audio search only
- `"-i What text is in the image?"` → Image OCR search only
- `"-db -a Show customers and meeting notes"` → Database + audio search
- `"Regular query without flags"` → All search types enabled (default)

---

### 3. Multi-Database Query

Configure databases in `appsettings.json`, then query them:

**Configuration (appsettings.json):**
```json
{
  "SmartRAG": {
    "DatabaseConnections": [
      {
        "Name": "Sales",
        "ConnectionString": "Server=localhost;Database=Sales;...",
        "DatabaseType": "SqlServer"
      },
      {
        "Name": "Inventory",
        "ConnectionString": "Server=localhost;Database=Inventory;...",
        "DatabaseType": "MySQL"
      }
    ]
  }
}
```

**Query Controller:**
```csharp
public class DatabaseController : ControllerBase
{
    private readonly IMultiDatabaseQueryCoordinator _multiDbCoordinator;
    
    public DatabaseController(IMultiDatabaseQueryCoordinator multiDbCoordinator)
    {
        _multiDbCoordinator = multiDbCoordinator;
    }
    
    // Query across multiple databases
    [HttpPost("query")]
    public async Task<IActionResult> QueryDatabases([FromBody] MultiDbQueryRequest request)
    {
        var response = await _multiDbCoordinator.QueryMultipleDatabasesAsync(
            request.Query,
            maxResults: request.MaxResults
        );
        
        return Ok(response);
    }
}

public class MultiDbQueryRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 5;
}
```

**Example Query:**
```
"Show me total sales from SQL Server with current inventory levels from MySQL"
```

SmartRAG will:
1. Analyze query intent
2. Identify relevant databases and tables
3. Generate appropriate SQL for each database
4. Execute queries in parallel
5. Merge results intelligently
6. Return unified AI-powered answer

---

### 3. OCR Document Processing

Process images with Tesseract OCR:

```csharp
public class OcrController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _searchService;
    
    // Upload image for OCR processing
    [HttpPost("upload/image")]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string language = "eng")
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "user-123",
            language: language  // OCR language: eng, tur, deu, etc.
        );
        
        return Ok(new { 
            id = document.Id,
            extractedText = document.Content,
            confidence = "OCR completed successfully"
        });
    }
    
    // Query OCR-processed documents
    [HttpPost("query/image-content")]
    public async Task<IActionResult> QueryImageContent([FromBody] string query)
    {
        var response = await _searchService.QueryIntelligenceAsync(query);
        return Ok(response);
    }
}
```

**Supported Image Formats:**
- JPEG/JPG, PNG, GIF, BMP, TIFF, WebP

**Example Usage:**
```bash
# Upload invoice image
curl -X POST "http://localhost:5000/api/ocr/upload/image?language=eng" \
  -F "file=@invoice.jpg"

# Query: "What is the total amount on this invoice?"
```

---

### 4. Audio Transcription

Transcribe audio files with Whisper.net:

```csharp
public class AudioController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentSearchService _searchService;
    
    // Upload audio for transcription
    [HttpPost("upload/audio")]
    public async Task<IActionResult> UploadAudio(IFormFile file, [FromQuery] string language = "en")
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "user-123",
            language: language  // Speech language: en, tr, auto, etc.
        );
        
        return Ok(new { 
            id = document.Id,
            transcription = document.Content,
            message = "Audio transcribed successfully"
        });
    }
    
    // Query transcription content
    [HttpPost("query/audio-content")]
    public async Task<IActionResult> QueryAudioContent([FromBody] string query)
    {
        var response = await _searchService.QueryIntelligenceAsync(query);
        return Ok(response);
    }
}
```

**Supported Audio Formats:**
- MP3, WAV, M4A, AAC, OGG, FLAC, WMA

**Language Codes:**
- `en` - English
- `tr` - Turkish
- `de` - German
- `fr` - French
- `auto` - Automatic detection (recommended)
- 100+ languages supported

<div class="alert alert-success">
    <h4><i class="fas fa-lock me-2"></i> Privacy Note</h4>
    <p class="mb-0">
        All processing is done 100% locally. Audio transcription uses Whisper.net, OCR uses Tesseract. No data is sent to external services.
    </p>
</div>

---

### 5. Conversation History

Natural multi-turn conversations:

```csharp
public class ConversationController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var response = await _searchService.QueryIntelligenceAsync(
            request.Message,
            maxResults: 5,
            startNewConversation: request.StartNew
        );
        
        return Ok(new {
            answer = response.Answer,
            sources = response.Sources.Count,
            timestamp = response.SearchedAt
        });
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public bool StartNew { get; set; } = false;
}
```

**Conversation Flow Example:**

```
User: "What is machine learning?"
AI: "Machine learning is a subset of artificial intelligence..."

User: "Can you explain supervised learning?"  // AI remembers context
AI: "Based on our previous discussion about machine learning, supervised learning is..."

User: "What are some common algorithms?"  // Maintains conversation context
AI: "Common supervised learning algorithms include..."

User: "/new"  // Start fresh conversation
AI: "Started new conversation. How can I help you?"
```

---

## Related Examples

- [Examples Index]({{ site.baseurl }}/en/examples) - Back to Examples categories
