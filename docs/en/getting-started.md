---
layout: default
title: Getting Started
description: Install and configure SmartRAG in your .NET application in minutes
lang: en
---

<div class="container">

## Installation

SmartRAG is available as a NuGet package and supports **.NET Standard 2.0/2.1**, making it compatible with:
- ✅ .NET Framework 4.6.1+
- ✅ .NET Core 2.0+
- ✅ .NET 5, 6, 7, 8, 9+

### Installation Methods

<div class="code-tabs">
    <button class="code-tab active" data-tab="cli">.NET CLI</button>
    <button class="code-tab" data-tab="pm">Package Manager</button>
    <button class="code-tab" data-tab="xml">Package Reference</button>
</div>

<div class="code-panel active" data-tab="cli">

```bash
dotnet add package SmartRAG
```

</div>

<div class="code-panel" data-tab="pm">

```bash
Install-Package SmartRAG
```

</div>

<div class="code-panel" data-tab="xml">

```xml
<PackageReference Include="SmartRAG" Version="3.0.0" />
```

</div>

---

## Basic Configuration

Configure SmartRAG in your `Program.cs` or `Startup.cs`:

### Quick Setup (Recommended)

```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Simple one-line configuration
builder.Services.UseSmartRag(builder.Configuration,
    storageProvider: StorageProvider.InMemory,  // Start with in-memory
    aiProvider: AIProvider.Gemini               // Choose your AI provider
);

var app = builder.Build();
app.Run();
```

### Advanced Setup

```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Advanced configuration with options
builder.Services.AddSmartRag(builder.Configuration, options =>
{
    // AI Provider
    options.AIProvider = AIProvider.OpenAI;
    
    // Storage Provider
    options.StorageProvider = StorageProvider.Qdrant;
    
    // Chunking Configuration
    options.MaxChunkSize = 1000;
    options.MinChunkSize = 100;
    options.ChunkOverlap = 200;
    
    // Retry Configuration
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    
    // Fallback Providers
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider> 
    { 
        AIProvider.Anthropic, 
        AIProvider.Gemini 
    };
});

var app = builder.Build();
app.Run();
```

---

## Configuration File

Create `appsettings.json` or `appsettings.Development.json`:

```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-YOUR_API_KEY",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002"
    },
    "Anthropic": {
      "ApiKey": "sk-ant-YOUR_API_KEY",
      "Model": "claude-3-5-sonnet-20241022",
      "EmbeddingApiKey": "pa-YOUR_VOYAGE_KEY",
      "EmbeddingModel": "voyage-large-2"
    },
    "Gemini": {
      "ApiKey": "YOUR_GEMINI_KEY",
      "Model": "gemini-pro",
      "EmbeddingModel": "embedding-001"
    }
  },
  "Storage": {
    "InMemory": {
      "MaxDocuments": 1000
    },
    "Qdrant": {
      "Host": "localhost:6334",
      "UseHttps": false,
      "CollectionName": "smartrag_documents",
      "VectorSize": 1536
    },
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Database": 0,
      "KeyPrefix": "smartrag:"
    }
  }
}
```

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Security Warning</h4>
    <p class="mb-0">
        <strong>Never commit API keys to source control!</strong> 
        Use <code>appsettings.Development.json</code> for local development (add to .gitignore).
        Use environment variables or Azure Key Vault for production.
    </p>
</div>

---

## Quick Usage Example

### 1. Upload Documents

```csharp
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    
    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }
    
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var document = await _documentService.UploadDocumentAsync(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            "user-123"
        );
        
        return Ok(new 
        { 
            id = document.Id,
            fileName = document.FileName,
            chunks = document.Chunks.Count,
            message = "Document processed successfully"
        });
    }
}
```

### 2. Ask Questions with AI

```csharp
public class IntelligenceController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    public IntelligenceController(IDocumentSearchService searchService)
    {
        _searchService = searchService;
    }
    
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] QuestionRequest request)
    {
        var response = await _searchService.QueryIntelligenceAsync(
            request.Question,
            maxResults: 5
        );
        
        return Ok(response);
    }
}

public class QuestionRequest
{
    public string Question { get; set; } = string.Empty;
}
```

### 3. Response Example

```json
{
  "query": "What are the main benefits?",
  "answer": "Based on the contract document, the main benefits include: 1) 24/7 customer support, 2) 30-day money-back guarantee, 3) Free updates for lifetime...",
  "sources": [
    {
      "documentId": "abc-123",
      "fileName": "contract.pdf",
      "chunkContent": "Our service includes 24/7 customer support...",
      "relevanceScore": 0.94
    }
  ],
  "searchedAt": "2025-10-18T14:30:00Z"
}
```

---

## Conversation History

SmartRAG automatically manages conversation history:

```csharp
// First question
var q1 = await _searchService.QueryIntelligenceAsync("What is machine learning?");

// Follow-up question - AI remembers previous context
var q2 = await _searchService.QueryIntelligenceAsync("Can you explain supervised learning?");

// Start new conversation
var newConv = await _searchService.QueryIntelligenceAsync(
    "New topic", 
    startNewConversation: true
);
```

<div class="alert alert-success">
    <h4><i class="fas fa-lightbulb me-2"></i> Pro Tip</h4>
    <p class="mb-0">
        SmartRAG automatically manages session IDs and conversation context. 
        No manual session handling required!
    </p>
</div>

---

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="feature-card">
            <div class="feature-icon">
                <i class="fas fa-cog"></i>
            </div>
            <h3>Configuration</h3>
            <p>Explore all configuration options, AI providers, storage backends, and advanced settings.</p>
            <a href="{{ site.baseurl }}/en/configuration" class="btn btn-outline-primary btn-sm mt-3">
                Configure SmartRAG <i class="fas fa-arrow-right ms-2"></i>
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card">
            <div class="feature-icon">
                <i class="fas fa-code"></i>
            </div>
            <h3>API Reference</h3>
            <p>Complete API documentation with all interfaces, methods, parameters, and examples.</p>
            <a href="{{ site.baseurl }}/en/api-reference" class="btn btn-outline-primary btn-sm mt-3">
                View API Docs <i class="fas fa-arrow-right ms-2"></i>
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card">
            <div class="feature-icon">
                <i class="fas fa-lightbulb"></i>
            </div>
            <h3>Examples</h3>
            <p>Real-world examples including multi-database queries, OCR processing, and audio transcription.</p>
            <a href="{{ site.baseurl }}/en/examples" class="btn btn-outline-primary btn-sm mt-3">
                See Examples <i class="fas fa-arrow-right ms-2"></i>
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="feature-card">
            <div class="feature-icon">
                <i class="fas fa-history"></i>
            </div>
            <h3>Changelog</h3>
            <p>Track new features, improvements, and breaking changes across all versions.</p>
            <a href="{{ site.baseurl }}/en/changelog" class="btn btn-outline-primary btn-sm mt-3">
                View Changelog <i class="fas fa-arrow-right ms-2"></i>
            </a>
        </div>
    </div>
</div>

---

## Need Help?

<div class="alert alert-info mt-5">
    <h4><i class="fas fa-question-circle me-2"></i> Support & Community</h4>
    <p>If you encounter issues or need assistance:</p>
    <ul class="mb-0">
        <li><strong>GitHub Issues:</strong> <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">Report bugs or request features</a></li>
        <li><strong>Email Support:</strong> <a href="mailto:b.yerlikaya@outlook.com">b.yerlikaya@outlook.com</a></li>
        <li><strong>LinkedIn:</strong> <a href="https://www.linkedin.com/in/barisyerlikaya/" target="_blank">Connect for professional inquiries</a></li>
        <li><strong>Documentation:</strong> Explore full documentation on this site</li>
    </ul>
</div>

</div>

