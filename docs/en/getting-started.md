---
layout: default
title: Getting Started
description: Install and configure SmartRAG in your .NET application in minutes
lang: en
---

## Installation

<div class="card">
    <div class="card-body">
        <p>SmartRAG is available as a NuGet package and supports <strong>.NET Standard 2.1</strong>, making it compatible with:</p>
        <ul>
            <li>✅ .NET Core 3.0+</li>
            <li>✅ .NET 5, 6, 7, 8, 9+</li>
        </ul>

        <h3 class="card-title">Installation Methods</h3>

<div class="code-tabs">
    <button class="code-tab active" data-tab="cli">.NET CLI</button>
    <button class="code-tab" data-tab="pm">Package Manager</button>
    <button class="code-tab" data-tab="xml">Package Reference</button>
</div>

<div class="code-panel active" data-tab="cli">
<pre><code class="language-bash">dotnet add package SmartRAG</code></pre>
</div>

<div class="code-panel" data-tab="pm">
<pre><code class="language-bash">Install-Package SmartRAG</code></pre>
</div>

<div class="code-panel" data-tab="xml">
<pre><code class="language-xml">&lt;PackageReference Include="SmartRAG" Version="3.4.0" /&gt;</code></pre>
</div>
    </div>
</div>

## Basic Configuration

<p>Configure SmartRAG in your <code>Program.cs</code> or <code>Startup.cs</code>:</p>

### Quick Setup (Recommended)

**For Web API Applications:**

```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

// Simple configuration
builder.Services.AddSmartRag(builder.Configuration, options =>
{
    options.StorageProvider = StorageProvider.InMemory;  // Start with in-memory
    options.AIProvider = AIProvider.Gemini;              // Choose your AI provider
});

var app = builder.Build();
app.Run();
```

**For Console Applications:**

```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var services = new ServiceCollection();
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// UseSmartRag returns IServiceProvider with auto-started services
var serviceProvider = services.UseSmartRag(
    configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.Gemini,
    defaultLanguage: "en"  // Optional
);

// Use the service provider
var documentService = serviceProvider.GetRequiredService<IDocumentService>();
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

## Configuration File

<p>Create <code>appsettings.json</code> or <code>appsettings.Development.json</code>:</p>

```json
{
  "SmartRAG": {
    "AIProvider": "OpenAI",
    "StorageProvider": "InMemory",
    "MaxChunkSize": 1000,
    "MinChunkSize": 100,
    "ChunkOverlap": 200,
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000,
    "RetryPolicy": "ExponentialBackoff",
    "EnableFallbackProviders": false
  },
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-YOUR_API_KEY",
      "Endpoint": "https://api.openai.com/v1",
      "Model": "gpt-5.1",
      "EmbeddingModel": "text-embedding-3-small",
      "MaxTokens": 4096,
      "Temperature": 0.7
    },
    "Anthropic": {
      "ApiKey": "sk-ant-YOUR_API_KEY",
      "Endpoint": "https://api.anthropic.com",
      "Model": "claude-sonnet-4-5",
      "MaxTokens": 4096,
      "Temperature": 0.3,
      "EmbeddingApiKey": "pa-YOUR_VOYAGE_KEY",
      "EmbeddingModel": "voyage-3.5"
    },
    "Gemini": {
      "ApiKey": "YOUR_GEMINI_KEY",
      "Endpoint": "https://generativelanguage.googleapis.com/v1beta",
      "Model": "gemini-2.5-pro",
      "EmbeddingModel": "embedding-001",
      "MaxTokens": 4096,
      "Temperature": 0.3
    },
    "AzureOpenAI": {
      "ApiKey": "your-azure-openai-api-key",
      "Endpoint": "https://your-resource.openai.azure.com/",
      "Model": "gpt-5.1",
      "EmbeddingModel": "text-embedding-3-small",
      "ApiVersion": "2024-10-21",
      "MaxTokens": 4096,
      "Temperature": 0.7
    },
    "Custom": {
      "ApiKey": "your-custom-api-key",
      "Endpoint": "https://api.yourprovider.com/v1/chat/completions",
      "Model": "your-model-name",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  },
  "Storage": {
    "InMemory": {
      "MaxDocuments": 1000
    },
    "Qdrant": {
      "Host": "localhost",
      "UseHttps": false,
      "ApiKey": "",
      "CollectionName": "smartrag_documents",
      "VectorSize": 768,
      "DistanceMetric": "Cosine"
    },
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Password": "",
      "Username": "",
      "Database": 0,
      "KeyPrefix": "smartrag:doc:",
      "ConnectionTimeout": 30,
      "EnableSsl": false
    }
  },
  "SmartRAG": {
    "Features": {
      "EnableMcpSearch": false
    },
    "McpServers": [],
    "EnableFileWatcher": false,
    "WatchedFolders": []
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
      "sourceType": "Document",
      "documentId": "00000000-0000-0000-0000-000000000000",
      "fileName": "contract.pdf",
      "relevantContent": "Our service includes 24/7 customer support...",
      "relevanceScore": 0.94,
      "location": null
    }
  ],
  "searchedAt": "2025-10-18T14:30:00Z",
  "configuration": {
    "aiProvider": "OpenAI",
    "storageProvider": "Redis",
    "model": "gpt-5.1"
  }
}
```

## Conversation History

<p>SmartRAG automatically manages conversation history:</p>

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

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-cog"></i>
            </div>
            <h3>Configuration</h3>
            <p>Explore all configuration options, AI providers, storage backends, and advanced settings.</p>
            <a href="{{ site.baseurl }}/en/configuration/basic" class="btn btn-outline-primary btn-sm mt-3">
                Configure SmartRAG <i class="fas fa-arrow-right ms-2"></i>
            </a>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
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
        <div class="card card-accent">
            <div class="icon icon-lg icon-gradient">
                <i class="fas fa-lightbulb"></i>
            </div>
            <h3>Examples</h3>
            <p>Real-world examples including multi-database queries, OCR processing, and audio transcription.</p>
            <a href="{{ site.baseurl }}/en/examples/quick" class="btn btn-outline-primary btn-sm mt-3">
                See Examples <i class="fas fa-arrow-right ms-2"></i>
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
            <a href="{{ site.baseurl }}/en/changelog/version-history" class="btn btn-outline-primary btn-sm mt-3">
                View Changelog <i class="fas fa-arrow-right ms-2"></i>
            </a>
        </div>
    </div>
</div>

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

