---
layout: default
title: Configuration
description: Complete configuration guide for SmartRAG - AI providers, storage, databases, and advanced options
lang: en
---

<div class="container">

## Basic Configuration

SmartRAG offers two configuration methods:

### Method 1: UseSmartRag (Simple)

```csharp
builder.Services.UseSmartRag(configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.Gemini
);
```

### Method 2: AddSmartRag (Advanced)

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;
    // ... additional options
});
```

---

## SmartRagOptions - Complete Reference

All available configuration options:

### Core Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `AIProvider` | `AIProvider` | `OpenAI` | AI provider for embeddings and text generation |
| `StorageProvider` | `StorageProvider` | `InMemory` | Storage backend for documents and vectors |
| `ConversationStorageProvider` | `ConversationStorageProvider?` | `null` | Separate storage for conversation history (optional) |

### Chunking Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `MaxChunkSize` | `int` | `1000` | Maximum size of each document chunk in characters |
| `MinChunkSize` | `int` | `100` | Minimum size of each document chunk in characters |
| `ChunkOverlap` | `int` | `200` | Number of characters to overlap between adjacent chunks |

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Chunking Best Practices</h4>
    <ul class="mb-0">
        <li><strong>MaxChunkSize:</strong> 500-1000 characters for optimal balance</li>
        <li><strong>ChunkOverlap:</strong> 15-20% of MaxChunkSize for context preservation</li>
        <li><strong>Larger chunks:</strong> Better context, but slower search</li>
        <li><strong>Smaller chunks:</strong> More precise results, but less context</li>
    </ul>
</div>

### Retry & Resilience Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `MaxRetryAttempts` | `int` | `3` | Maximum number of retry attempts for AI provider requests |
| `RetryDelayMs` | `int` | `1000` | Delay between retry attempts in milliseconds |
| `RetryPolicy` | `RetryPolicy` | `ExponentialBackoff` | Retry policy for failed requests |
| `EnableFallbackProviders` | `bool` | `false` | Enable fallback to alternative AI providers on failure |
| `FallbackProviders` | `List<AIProvider>` | `[]` | List of fallback AI providers to try sequentially |

**RetryPolicy Enum Values:**
- `RetryPolicy.None` - No retries
- `RetryPolicy.FixedDelay` - Fixed delay between retries
- `RetryPolicy.LinearBackoff` - Linearly increasing delay
- `RetryPolicy.ExponentialBackoff` - Exponentially increasing delay (recommended)

### Database Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `DatabaseConnections` | `List<DatabaseConnectionConfig>` | `[]` | Multi-database connections for intelligent cross-database querying |
| `EnableAutoSchemaAnalysis` | `bool` | `true` | Automatically analyze database schemas on startup |
| `EnablePeriodicSchemaRefresh` | `bool` | `true` | Periodically refresh database schemas |
| `DefaultSchemaRefreshIntervalMinutes` | `int` | `60` | Default schema refresh interval (0 = use per-connection settings) |

### Audio Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `GoogleSpeechConfig` | `GoogleSpeechConfig` | `null` | Google Speech-to-Text configuration for audio transcription |

---

## AI Provider Configuration

### OpenAI

```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-YOUR_KEY",
      "Endpoint": "https://api.openai.com/v1",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
});
```

**Models:**
- `gpt-4`, `gpt-4-turbo`, `gpt-4o` - Advanced reasoning
- `gpt-3.5-turbo` - Fast and cost-effective
- `text-embedding-ada-002`, `text-embedding-3-small`, `text-embedding-3-large` - Embeddings

---

### Anthropic (Claude)

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Important: VoyageAI Required</h4>
    <p>
        Anthropic Claude models require a <strong>separate VoyageAI API key</strong> for embeddings because Anthropic doesn't provide embedding models.
    </p>
    <ul class="mb-0">
        <li><strong>Get VoyageAI Key:</strong> <a href="https://console.voyageai.com/" target="_blank">console.voyageai.com</a></li>
        <li><strong>Documentation:</strong> <a href="https://docs.anthropic.com/en/docs/build-with-claude/embeddings" target="_blank">Anthropic Embeddings Guide</a></li>
    </ul>
</div>

```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "sk-ant-YOUR_ANTHROPIC_KEY",
      "Model": "claude-3-5-sonnet-20241022",
      "MaxTokens": 4096,
      "Temperature": 0.3,
      "EmbeddingApiKey": "pa-YOUR_VOYAGE_KEY",
      "EmbeddingModel": "voyage-large-2"
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
});
```

**Claude Models:**
- `claude-3-5-sonnet-20241022` - Most intelligent (recommended)
- `claude-3-opus-20240229` - Highest capability
- `claude-3-haiku-20240307` - Fastest

**VoyageAI Embedding Models:**
- `voyage-large-2` - High quality (recommended)
- `voyage-code-2` - Optimized for code
- `voyage-2` - General purpose

---

### Google Gemini

```json
{
  "AI": {
    "Gemini": {
      "ApiKey": "YOUR_GEMINI_KEY",
      "Model": "gemini-pro",
      "EmbeddingModel": "embedding-001",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Gemini;
});
```

**Models:**
- `gemini-pro` - Text generation
- `gemini-pro-vision` - Multimodal (text + images)
- `embedding-001` - Text embeddings

---

### Azure OpenAI

```json
{
  "AI": {
    "AzureOpenAI": {
      "ApiKey": "YOUR_AZURE_KEY",
      "Endpoint": "https://your-resource.openai.azure.com/",
      "Model": "gpt-4",
      "EmbeddingModel": "text-embedding-ada-002",
      "DeploymentName": "gpt-4-deployment",
      "MaxTokens": 4096,
      "Temperature": 0.7
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.AzureOpenAI;
});
```

---

### Custom Provider (Ollama / LM Studio)

<div class="alert alert-success">
    <h4><i class="fas fa-server me-2"></i> 100% Local AI with Ollama / LM Studio</h4>
    <p>Run AI models completely locally for total data privacy - perfect for on-premise deployments, GDPR/KVKK/HIPAA compliance.</p>
</div>

#### Ollama (Local Models)

```json
{
  "AI": {
    "Custom": {
      "ApiKey": "not-needed",
      "Endpoint": "http://localhost:11434/v1/chat/completions",
      "Model": "llama2",
      "EmbeddingModel": "nomic-embed-text"
    }
  }
}
```

#### LM Studio (Local Models)

```json
{
  "AI": {
    "Custom": {
      "ApiKey": "not-needed",
      "Endpoint": "http://localhost:1234/v1/chat/completions",
      "Model": "local-model",
      "EmbeddingModel": "local-embedding"
    }
  }
}
```

#### OpenRouter / Groq / Together AI

```json
{
  "AI": {
    "Custom": {
      "ApiKey": "your-api-key",
      "Endpoint": "https://api.openrouter.ai/v1/chat/completions",
      "Model": "anthropic/claude-3.5-sonnet",
      "EmbeddingModel": "text-embedding-ada-002"
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Custom;
});
```

**Supported Custom APIs:**
- 🦙 Ollama - Local models
- 🏠 LM Studio - Local AI playground
- 🔗 OpenRouter - Access 100+ models
- ⚡ Groq - Lightning-fast inference
- 🌐 Together AI - Open source models
- 🚀 Perplexity - Search + AI
- 🇫🇷 Mistral AI - European AI leader
- Any OpenAI-compatible API

---

## Storage Provider Configuration

### Qdrant (Vector Database)

```json
{
  "Storage": {
    "Qdrant": {
      "Host": "localhost:6334",
      "UseHttps": false,
      "ApiKey": "your-qdrant-key",
      "CollectionName": "smartrag_documents",
      "VectorSize": 1536,
      "DistanceMetric": "Cosine"
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.Qdrant;
});
```

**When to use:**
- Production environments
- Large document collections (10,000+ documents)
- High-performance similarity search
- Scalable deployments

---

### Redis (High-Performance Cache)

```json
{
  "Storage": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Password": "",
      "Database": 0,
      "KeyPrefix": "smartrag:",
      "ConnectionTimeout": 30,
      "EnableSsl": false
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.Redis;
});
```

**When to use:**
- Fast read/write operations
- Distributed deployments
- Session-based applications
- Conversation history storage

---

### SQLite (Embedded Database)

```json
{
  "Storage": {
    "Sqlite": {
      "DatabasePath": "smartrag.db",
      "EnableForeignKeys": true
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.Sqlite;
});
```

**When to use:**
- Desktop applications
- Single-user scenarios
- No external dependencies
- Simple deployments

---

### FileSystem (File-Based Storage)

```json
{
  "Storage": {
    "FileSystem": {
      "FileSystemPath": "./smartrag_storage"
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.FileSystem;
});
```

**When to use:**
- Development and testing
- Simple backup/restore scenarios
- No database infrastructure available

---

### InMemory (RAM Storage)

```json
{
  "Storage": {
    "InMemory": {
      "MaxDocuments": 1000
    }
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.InMemory;
});
```

**When to use:**
- Development and testing
- Proof of concept
- Unit tests
- Temporary data

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Important</h4>
    <p class="mb-0">InMemory storage loses all data when application restarts. Not suitable for production!</p>
</div>

---

## Database Configuration

### Multi-Database Connections

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.DatabaseConnections = new List<DatabaseConnectionConfig>
    {
        new DatabaseConnectionConfig
        {
            Name = "Sales Database",
            Type = DatabaseType.SqlServer,
            ConnectionString = "Server=localhost;Database=Sales;...",
            IncludedTables = new List<string> { "Orders", "Customers" },
            MaxRowsPerTable = 1000,
            SanitizeSensitiveData = true
        },
        new DatabaseConnectionConfig
        {
            Name = "Inventory Database",
            Type = DatabaseType.MySQL,
            ConnectionString = "Server=localhost;Database=Inventory;...",
            MaxRowsPerTable = 1000
        }
    };
    
    options.EnableAutoSchemaAnalysis = true;
    options.EnablePeriodicSchemaRefresh = true;
    options.DefaultSchemaRefreshIntervalMinutes = 60;
});
```

### DatabaseConfig Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Name` | `string` | - | Friendly name for the database connection |
| `Type` | `DatabaseType` | - | Database type (SqlServer, MySql, PostgreSql, Sqlite) |
| `ConnectionString` | `string` | - | Database connection string |
| `IncludedTables` | `List<string>` | `[]` | Specific tables to include (empty = all tables) |
| `ExcludedTables` | `List<string>` | `[]` | Tables to exclude from analysis |
| `MaxRowsPerTable` | `int` | `1000` | Maximum rows to extract per table |
| `SanitizeSensitiveData` | `bool` | `true` | Automatically sanitize sensitive columns |
| `SensitiveColumns` | `List<string>` | See below | Column names to sanitize |

**Default Sensitive Columns:**
- `password`, `pwd`, `pass`
- `ssn`, `social_security`
- `credit_card`, `creditcard`, `cc_number`
- `email`, `mail`
- `phone`, `telephone`
- `salary`, `compensation`

### Connection String Examples

**SQL Server:**
```
Server=localhost;Database=MyDb;Trusted_Connection=true;
Server=localhost;Database=MyDb;User Id=sa;Password=YourPassword;
```

**MySQL:**
```
Server=localhost;Database=MyDb;Uid=root;Pwd=password;
Server=localhost;Port=3306;Database=MyDb;Uid=user;Pwd=pass;
```

**PostgreSQL:**
```
Host=localhost;Database=MyDb;Username=postgres;Password=password;
Host=localhost;Port=5432;Database=MyDb;Username=user;Password=pass;
```

**SQLite:**
```
Data Source=./mydb.db;
Data Source=C:\Databases\mydb.db;
```

---

## Audio Configuration

### Google Speech-to-Text

```json
{
  "GoogleSpeech": {
    "CredentialsPath": "./path/to/google-credentials.json",
    "DefaultLanguageCode": "en-US",
    "EnableAutomaticPunctuation": true,
    "Model": "default"
  }
}
```

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.GoogleSpeechConfig = new GoogleSpeechConfig
    {
        CredentialsPath = "./google-credentials.json",
        DefaultLanguageCode = "en-US",
        EnableAutomaticPunctuation = true
    };
});
```

**Supported Language Codes:**
- `en-US` - English (United States)
- `tr-TR` - Turkish (Turkey)
- `de-DE` - German (Germany)
- `fr-FR` - French (France)
- `es-ES` - Spanish (Spain)
- `zh-CN` - Chinese (Simplified)
- `ja-JP` - Japanese (Japan)
- 100+ languages supported - [View all](https://cloud.google.com/speech-to-text/docs/languages)

<div class="alert alert-warning">
    <h4><i class="fas fa-exclamation-triangle me-2"></i> Privacy Note</h4>
    <p class="mb-0">
        Audio files are sent to Google Cloud for transcription. For complete data privacy, avoid uploading audio files or use alternative local solutions.
    </p>
</div>

---

## OCR Configuration

### Tesseract Language Support

```csharp
// When uploading images, specify language for OCR
var document = await _documentService.UploadDocumentAsync(
    imageStream,
    "invoice.jpg",
    "image/jpeg",
    "user-id",
    language: "eng"  // English OCR
);

// Turkish OCR
language: "tur"

// Multiple languages
language: "eng+tur"
```

**Supported Languages:**
- `eng` - English
- `tur` - Turkish
- `deu` - German
- `fra` - French
- `spa` - Spanish
- `rus` - Russian
- `ara` - Arabic
- `chi_sim` - Chinese (Simplified)
- 100+ languages - [View all](https://github.com/tesseract-ocr/tessdata)

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> OCR Capabilities</h4>
    <ul class="mb-0">
        <li><strong>✅ Works perfectly:</strong> Printed documents, scanned text, digital screenshots</li>
        <li><strong>⚠️ Limited support:</strong> Handwritten text (very low accuracy)</li>
        <li><strong>💡 Best results:</strong> High-quality scans of printed documents</li>
        <li><strong>🔒 100% Local:</strong> No data sent to cloud - Tesseract runs locally</li>
    </ul>
</div>

---

## Advanced Configuration Examples

### Fallback Providers

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // Primary provider
    options.AIProvider = AIProvider.OpenAI;
    
    // Enable fallback
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider> 
    { 
        AIProvider.Anthropic,  // Try Anthropic if OpenAI fails
        AIProvider.Gemini,     // Try Gemini if Anthropic fails
        AIProvider.Custom      // Try custom provider as last resort
    };
    
    // Retry configuration
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
});
```

**Execution Flow:**
1. Try OpenAI (primary)
2. If fails, try Anthropic (fallback 1)
3. If fails, try Gemini (fallback 2)
4. If fails, try Custom (fallback 3)
5. If all fail, throw exception

---

### Multi-Database Setup

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    options.DatabaseConnections = new List<DatabaseConnectionConfig>
    {
        // SQL Server - Sales data
        new DatabaseConnectionConfig
        {
            Name = "Sales",
            Type = DatabaseType.SqlServer,
            ConnectionString = "Server=localhost;Database=Sales;Trusted_Connection=true;",
            IncludedTables = new List<string> { "Orders", "OrderDetails", "Payments" },
            MaxRowsPerTable = 1000,
            SanitizeSensitiveData = true,
            SensitiveColumns = new List<string> { "CreditCard", "SSN", "Email" }
        },
        
        // MySQL - Inventory data
        new DatabaseConnectionConfig
        {
            Name = "Inventory",
            Type = DatabaseType.MySQL,
            ConnectionString = "Server=localhost;Database=Inventory;Uid=root;Pwd=password;",
            IncludedTables = new List<string> { "Products", "Stock", "Suppliers" },
            MaxRowsPerTable = 1000
        },
        
        // PostgreSQL - Customer data
        new DatabaseConnectionConfig
        {
            Name = "Customers",
            Type = DatabaseType.PostgreSql,
            ConnectionString = "Host=localhost;Database=CRM;Username=postgres;Password=password;",
            IncludedTables = new List<string> { "Customers", "Contacts" },
            MaxRowsPerTable = 500
        },
        
        // SQLite - Product catalog
        new DatabaseConnectionConfig
        {
            Name = "Catalog",
            Type = DatabaseType.Sqlite,
            ConnectionString = "Data Source=./catalog.db;",
            MaxRowsPerTable = 5000
        }
    };
    
    // Schema analysis
    options.EnableAutoSchemaAnalysis = true;          // Analyze on startup
    options.EnablePeriodicSchemaRefresh = true;       // Periodic refresh
    options.DefaultSchemaRefreshIntervalMinutes = 60; // Refresh every hour
});
```

---

## Complete Configuration Example

```csharp
using SmartRAG.Extensions;
using SmartRAG.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSmartRag(builder.Configuration, options =>
{
    // ===== AI Provider =====
    options.AIProvider = AIProvider.OpenAI;
    
    // ===== Storage Provider =====
    options.StorageProvider = StorageProvider.Qdrant;
    options.ConversationStorageProvider = ConversationStorageProvider.Redis;
    
    // ===== Chunking =====
    options.MaxChunkSize = 1000;
    options.MinChunkSize = 100;
    options.ChunkOverlap = 200;
    
    // ===== Retry & Resilience =====
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider> 
    { 
        AIProvider.Anthropic, 
        AIProvider.Gemini 
    };
    
    // ===== Database Connections =====
    options.DatabaseConnections = new List<DatabaseConnectionConfig>
    {
        new DatabaseConnectionConfig
        {
            Name = "Primary Database",
            Type = DatabaseType.SqlServer,
            ConnectionString = configuration.GetConnectionString("SqlServer"),
            MaxRowsPerTable = 1000,
            SanitizeSensitiveData = true
        }
    };
    
    options.EnableAutoSchemaAnalysis = true;
    options.EnablePeriodicSchemaRefresh = true;
    options.DefaultSchemaRefreshIntervalMinutes = 60;
    
    // ===== Audio Processing =====
    options.GoogleSpeechConfig = new GoogleSpeechConfig
    {
        CredentialsPath = "./google-speech-credentials.json",
        DefaultLanguageCode = "en-US",
        EnableAutomaticPunctuation = true
    };
});

var app = builder.Build();
app.Run();
```

---

## Environment Variables

For production deployments, use environment variables:

```bash
# Linux / macOS
export OPENAI_API_KEY="sk-proj-YOUR_KEY"
export ANTHROPIC_API_KEY="sk-ant-YOUR_KEY"
export VOYAGE_API_KEY="pa-YOUR_KEY"
export QDRANT_API_KEY="your-qdrant-key"
export REDIS_CONNECTION="localhost:6379"

# Windows PowerShell
$env:OPENAI_API_KEY="sk-proj-YOUR_KEY"
$env:ANTHROPIC_API_KEY="sk-ant-YOUR_KEY"
$env:VOYAGE_API_KEY="pa-YOUR_KEY"
```

```csharp
// Access environment variables in configuration
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    // API key automatically loaded from environment or appsettings.json
});
```

---

## Best Practices

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="alert alert-success">
            <h4><i class="fas fa-check-circle me-2"></i> Security</h4>
            <ul class="mb-0">
                <li>Never commit API keys to source control</li>
                <li>Use environment variables for production</li>
                <li>Enable SanitizeSensitiveData for databases</li>
                <li>Use HTTPS for external services</li>
            </ul>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="alert alert-info">
            <h4><i class="fas fa-bolt me-2"></i> Performance</h4>
            <ul class="mb-0">
                <li>Use Qdrant or Redis for production</li>
                <li>Configure appropriate chunk sizes</li>
                <li>Enable fallback providers for reliability</li>
                <li>Set reasonable MaxRowsPerTable limits</li>
            </ul>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="alert alert-warning">
            <h4><i class="fas fa-database me-2"></i> Database</h4>
            <ul class="mb-0">
                <li>Test connections before deployment</li>
                <li>Use read-only database users when possible</li>
                <li>Monitor schema refresh intervals</li>
                <li>Configure connection timeouts appropriately</li>
            </ul>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="alert alert-primary">
            <h4><i class="fas fa-shield-alt me-2"></i> Privacy</h4>
            <ul class="mb-0">
                <li>Use Ollama/LM Studio for 100% local AI</li>
                <li>Avoid audio files if privacy is critical</li>
                <li>OCR is 100% local (Tesseract)</li>
                <li>Keep databases on-premise when needed</li>
            </ul>
        </div>
    </div>
</div>

---

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-code"></i>
            </div>
            <h3>API Reference</h3>
            <p>Explore complete API documentation with all interfaces and methods</p>
            <a href="{{ site.baseurl }}/en/api-reference" class="btn btn-outline-primary btn-sm mt-3">
                View API Docs
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-lightbulb"></i>
            </div>
            <h3>Examples</h3>
            <p>See practical examples and real-world use cases</p>
            <a href="{{ site.baseurl }}/en/examples" class="btn btn-outline-primary btn-sm mt-3">
                See Examples
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="feature-card text-center">
            <div class="feature-icon mx-auto">
                <i class="fas fa-book"></i>
            </div>
            <h3>Changelog</h3>
            <p>Track version history and migration guides</p>
            <a href="{{ site.baseurl }}/en/changelog" class="btn btn-outline-primary btn-sm mt-3">
                View Changelog
            </a>
        </div>
    </div>
</div>

</div>

