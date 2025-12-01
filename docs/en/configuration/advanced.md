---
layout: default
title: Advanced Configuration
description: SmartRAG advanced configuration options - fallback providers, best practices and next steps
lang: en
---

## Advanced Configuration

<p>Use SmartRAG's advanced features to create more reliable and performant systems:</p>

## Fallback Providers

### Fallback Provider Configuration

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // Primary AI provider
    options.AIProvider = AIProvider.OpenAI;
    
    // Enable fallback providers
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider>
    {
        AIProvider.Anthropic,    // First fallback
        AIProvider.Gemini,       // Second fallback
        AIProvider.Custom        // Last fallback (Ollama)
    };
    
    // Retry configuration
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
});
```

### Fallback Scenarios

```csharp
// Scenario 1: OpenAI → Anthropic → Gemini
options.FallbackProviders = new List<AIProvider>
{
    AIProvider.Anthropic,
    AIProvider.Gemini
};

// Scenario 2: Azure OpenAI → OpenAI → Anthropic
options.FallbackProviders = new List<AIProvider>
{
    AIProvider.OpenAI,
    AIProvider.Anthropic
};

// Scenario 3: Cloud → On-premise
options.FallbackProviders = new List<AIProvider>
{
    AIProvider.Custom  // Ollama/LM Studio
};

// Scenario 4: Premium → Budget
options.FallbackProviders = new List<AIProvider>
{
    AIProvider.Gemini  // More cost-effective
};
```

## Retry Policies

### RetryPolicy Options

```csharp
// Scenario 1: Fast retry
options.RetryPolicy = RetryPolicy.FixedDelay;
options.RetryDelayMs = 500;  // Fixed 500ms wait

// Scenario 2: Linear increasing wait
options.RetryPolicy = RetryPolicy.LinearBackoff;
options.RetryDelayMs = 1000;  // 1s, 2s, 3s, 4s...

// Scenario 3: Exponential increasing wait (recommended)
options.RetryPolicy = RetryPolicy.ExponentialBackoff;
options.RetryDelayMs = 1000;  // 1s, 2s, 4s, 8s...

// Scenario 4: No retry
options.RetryPolicy = RetryPolicy.None;
```

### Custom Retry Logic

```csharp
// Aggressive retry for critical applications
options.MaxRetryAttempts = 5;
options.RetryDelayMs = 200;
options.RetryPolicy = RetryPolicy.ExponentialBackoff;

// Minimal retry for test environments
options.MaxRetryAttempts = 1;
options.RetryDelayMs = 1000;
options.RetryPolicy = RetryPolicy.FixedDelay;
```

## Performance Optimization

### Chunk Size Optimization

```csharp
// Scenario 1: Fast search (small chunks)
options.MaxChunkSize = 500;
options.MinChunkSize = 100;
options.ChunkOverlap = 75;

// Scenario 2: Context preservation (large chunks)
options.MaxChunkSize = 2000;
options.MinChunkSize = 200;
options.ChunkOverlap = 400;

// Scenario 3: Balance (recommended)
options.MaxChunkSize = 1000;
options.MinChunkSize = 100;
options.ChunkOverlap = 200;
```

### Storage Optimization

```csharp
// Scenario 1: Maximum performance
options.StorageProvider = StorageProvider.Qdrant;
options.ConversationStorageProvider = ConversationStorageProvider.Redis;

// Scenario 2: Cost optimization
options.StorageProvider = StorageProvider.Redis;
options.ConversationStorageProvider = ConversationStorageProvider.InMemory;

// Scenario 3: Hybrid approach
options.StorageProvider = StorageProvider.Redis;
options.ConversationStorageProvider = ConversationStorageProvider.SQLite;
```

## Security Configuration

### API Key Management

```csharp
// Use environment variables (recommended)
builder.Services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    // API key automatically loaded from environment
});

// Use appsettings.json (for development)
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-proj-YOUR_KEY"
    }
  }
}
```

### Database Connection Configuration

```csharp
// Database connection configuration
options.DatabaseConnections = new List<DatabaseConnectionConfig>
{
    new DatabaseConnectionConfig
    {
        Name = "Secure Database",
        DatabaseType = DatabaseType.SqlServer,
        ConnectionString = "Server=localhost;Database=SecureDB;...",
        Description = "Production database with sensitive data",
        Enabled = true,
        MaxRowsPerQuery = 1000,
        QueryTimeoutSeconds = 30,
        SchemaRefreshIntervalMinutes = 60,
        IncludedTables = new string[] { "Orders", "Customers" },
        ExcludedTables = new string[] { "Logs", "TempData" }
    }
};
```

**Note:** `SanitizeSensitiveData` and `MaxRowsPerTable` are configured in `DatabaseConfig`, not in `DatabaseConnectionConfig`. These settings apply globally to all database connections.

## Monitoring and Logging

### Detailed Logging

```csharp
// Logging configuration
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

// SmartRAG logging automatically enabled
builder.Services.AddSmartRag(configuration, options =>
{
    // Logging automatically enabled
});
```

### Performance Monitoring

```csharp
// For performance metrics
public class SmartRagMetrics
{
    public int TotalQueries { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int FallbackUsageCount { get; set; }
    public int RetryCount { get; set; }
}
```

## Best Practices

### Security

<div class="alert alert-success">
    <h4><i class="fas fa-check-circle me-2"></i> Security Best Practices</h4>
    <ul class="mb-0">
        <li>Never commit API keys to source control</li>
        <li>Use environment variables for production</li>
        <li>Configure database connections securely</li>
        <li>Use HTTPS for external services</li>
        <li>Prefer on-premise AI providers for sensitive data</li>
    </ul>
</div>

### Performance

<div class="alert alert-info">
    <h4><i class="fas fa-info-circle me-2"></i> Performance Best Practices</h4>
    <ul class="mb-0">
        <li>Use Qdrant or Redis for production</li>
        <li>Configure appropriate chunk sizes</li>
        <li>Enable fallback providers for reliability</li>
        <li>Set reasonable MaxRowsPerQuery limits for database connections</li>
        <li>Use ExponentialBackoff retry policy</li>
    </ul>
</div>

### Cost Optimization

<div class="alert alert-warning">
    <h4><i class="fas fa-dollar-sign me-2"></i> Cost Optimization</h4>
    <ul class="mb-0">
        <li>Use Gemini or Custom providers for development</li>
        <li>Prefer OpenAI, Azure OpenAI, or Anthropic for production</li>
        <li>Use InMemory storage only for testing</li>
        <li>Use Redis for cost-effective production storage</li>
        <li>Ollama/LM Studio for 100% on-premise solutions</li>
    </ul>
</div>

## Custom Strategies

SmartRAG provides the Strategy Pattern for key components, allowing you to inject custom logic.

### Registering Custom Strategies

You can implement your own strategies by implementing the corresponding interfaces. Here are examples of how you would register them:

```csharp
// Example: Registering a custom SQL Dialect (e.g., if you implemented EnhancedPostgreSqlDialectStrategy)
services.AddSingleton<ISqlDialectStrategy, EnhancedPostgreSqlDialectStrategy>();

// Example: Registering a custom Scoring Strategy
services.AddSingleton<IScoringStrategy, CustomScoringStrategy>();

// Example: Registering a custom File Parser (e.g., for Markdown files)
services.AddSingleton<IFileParser, MarkdownFileParser>();
```

## Example Configurations

### Development Environment

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // For fast development
    options.AIProvider = AIProvider.Gemini;
    options.StorageProvider = StorageProvider.InMemory;
    options.MaxChunkSize = 500;
    options.ChunkOverlap = 100;
    options.MaxRetryAttempts = 1;
    options.RetryPolicy = RetryPolicy.FixedDelay;
});
```

### Test Environment

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // Reliable configuration for testing
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Redis;
    options.ConversationStorageProvider = ConversationStorageProvider.SQLite;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
    options.MaxRetryAttempts = 3;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider> { AIProvider.Gemini };
});
```

### Production Environment

```csharp
builder.Services.AddSmartRag(configuration, options =>
{
    // Reliable production configuration
    options.AIProvider = AIProvider.OpenAI;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ConversationStorageProvider = ConversationStorageProvider.Redis;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
    options.MaxRetryAttempts = 5;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new List<AIProvider> 
    { 
        AIProvider.AzureOpenAI,  // First fallback (Azure)
        AIProvider.Anthropic,    // Second fallback
        AIProvider.Custom        // Last fallback (Ollama)
    };
    
    // Database configuration
    options.DatabaseConnections = new List<DatabaseConnectionConfig>
    {
        new DatabaseConnectionConfig
        {
            Name = "Production DB",
            DatabaseType = DatabaseType.SqlServer,
            ConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING"),
            Description = "Production database",
            Enabled = true,
            MaxRowsPerQuery = 5000,
            QueryTimeoutSeconds = 30,
            SchemaRefreshIntervalMinutes = 60
        }
    };
});
```

## Next Steps

<div class="row g-4 mt-4">
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-book"></i>
            </div>
            <h3>API Reference</h3>
            <p>Detailed API documentation and method references</p>
            <a href="{{ site.baseurl }}/en/api-reference" class="btn btn-outline-primary btn-sm mt-3">
                API Reference
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
                <i class="fas fa-code"></i>
            </div>
            <h3>Examples</h3>
            <p>See practical examples and real-world usage scenarios</p>
            <a href="{{ site.baseurl }}/en/examples" class="btn btn-outline-primary btn-sm mt-3">
                View Examples
            </a>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card card-accent text-center">
            <div class="icon icon-lg icon-gradient mx-auto">
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
