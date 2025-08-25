---
layout: default
title: Examples
description: Real-world examples and sample applications for SmartRAG implementation
nav_order: 5
---

# SmartRAG Examples

This page contains practical examples of how to use SmartRAG in different scenarios.

## Web API Example

The Web API example demonstrates how to integrate SmartRAG into an ASP.NET Core application.

### Project Structure

```
examples/WebAPI/
├── Controllers/
│   └── DocumentsController.cs
├── Filters/
│   └── MultipartFileUploadFilter.cs
├── Contracts/
│   └── SearchRequest.cs
├── Program.cs
└── GlobalUsings.cs
```

### Key Features

- **File Upload**: Handle document uploads with validation
- **Document Processing**: Parse and store documents using SmartRAG
- **Search API**: Provide semantic search capabilities
- **Error Handling**: Comprehensive error handling and validation

### Usage

```csharp
// Upload a document
POST /api/documents/upload
Content-Type: multipart/form-data

// Search documents
POST /api/documents/search
{
    "query": "your search query",
    "maxResults": 10
}
```

## Console Application Example

A simple console application demonstrating basic SmartRAG functionality.

### Features

- Document processing from local files
- Basic search functionality
- Configuration management
- Logging and error handling

### Code Example

```csharp
var services = new ServiceCollection();
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.InMemory;
    options.ApiKey = "your-api-key";
});

var serviceProvider = services.BuildServiceProvider();
var documentService = serviceProvider.GetRequiredService<IDocumentService>();

// Process a document
var document = await documentService.UploadDocumentAsync(filePath);
Console.WriteLine($"Document processed: {document.Title}");
```

## Custom Implementation Example

How to extend SmartRAG with custom providers and services.

### Custom AI Provider

```csharp
public class CustomAIProvider : BaseAIProvider
{
    public override async Task<string> GenerateEmbeddingAsync(string text)
    {
        // Your custom embedding logic
        return "custom-embedding";
    }
    
    public override async Task<string> GenerateResponseAsync(string prompt)
    {
        // Your custom response generation logic
        return "custom-response";
    }
}
```

### Custom Storage Provider

```csharp
public class CustomStorageProvider : IDocumentRepository
{
    public async Task<SmartRAG.Entities.Document> AddAsync(SmartRAG.Entities.Document document)
    {
        // Your custom storage logic
        return document;
    }
    
    public async Task<SmartRAG.Entities.Document?> GetByIdAsync(Guid id)
    {
        // Your custom retrieval logic
        return null;
    }
    
    public async Task<List<SmartRAG.Entities.Document>> GetAllAsync()
    {
        // Your custom retrieval logic
        return new List<SmartRAG.Entities.Document>();
    }
}
```

## Integration Examples

### Entity Framework Integration

```csharp
public class SmartRAGDbContext : DbContext
{
    public DbSet<SmartRAG.Entities.Document> Documents { get; set; }
    public DbSet<DocumentChunk> DocumentChunks { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SmartRAG.Entities.Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Content).IsRequired();
        });
        
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne<SmartRAG.Entities.Document>()
                  .WithMany()
                  .HasForeignKey(e => e.DocumentId);
        });
    }
}
```

### Dependency Injection

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmartRAGWithCustomProviders(
        this IServiceCollection services,
        Action<SmartRagOptions> configureOptions)
    {
        var options = new SmartRagOptions();
        configureOptions(options);
        
        services.AddSingleton(options);
        services.AddScoped<IDocumentRepository, CustomStorageProvider>();
        services.AddScoped<IAIService, CustomAIProvider>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentParserService, DocumentParserService>();
        
        return services;
    }
}
```

## Performance Optimization

### Batch Processing

```csharp
public async Task<List<SmartRAG.Entities.Document>> ProcessBatchAsync(
    IEnumerable<string> filePaths)
{
    var tasks = filePaths.Select(filePath => 
        ProcessSingleFileAsync(filePath));
    
    return await Task.WhenAll(tasks);
}

private async Task<SmartRAG.Entities.Document> ProcessSingleFileAsync(string filePath)
{
    // Process individual file
    return await _documentService.UploadDocumentAsync(filePath);
}
```

### Caching

```csharp
public class CachedDocumentService : IDocumentService
{
    private readonly IDocumentService _innerService;
    private readonly IMemoryCache _cache;
    
    public async Task<SmartRAG.Entities.Document?> GetDocumentAsync(Guid id)
    {
        var cacheKey = $"document_{id}";
        
        if (_cache.TryGetValue(cacheKey, out SmartRAG.Entities.Document? cached))
        {
            return cached;
        }
        
        var document = await _innerService.GetDocumentAsync(id);
        if (document != null)
        {
            _cache.Set(cacheKey, document, TimeSpan.FromMinutes(30));
        }
        
        return document;
    }
}
```

## Testing Examples

### Unit Tests

```csharp
[Fact]
public async Task UploadDocument_ValidFile_ReturnsDocument()
{
    // Arrange
    var mockService = new Mock<IDocumentService>();
    var expectedDocument = new SmartRAG.Entities.Document
    {
        Id = Guid.NewGuid(),
        Title = "Test Document",
        Content = "Test content"
    };
    
    mockService.Setup(s => s.UploadDocumentAsync(It.IsAny<IFormFile>()))
               .ReturnsAsync(expectedDocument);
    
    // Act
    var result = await mockService.Object.UploadDocumentAsync(null);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedDocument.Title, result.Title);
}
```

### Integration Tests

```csharp
public class DocumentServiceIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    
    public DocumentServiceIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task UploadAndSearch_CompleteWorkflow_Success()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act - Upload
        var uploadResponse = await UploadTestDocument(client);
        
        // Act - Search
        var searchResponse = await SearchDocuments(client, "test");
        
        // Assert
        Assert.True(uploadResponse.IsSuccessStatusCode);
        Assert.True(searchResponse.IsSuccessStatusCode);
    }
}
```

## Next Steps

- [Getting Started Guide](getting-started.md) - Learn the basics
- [Configuration](configuration.md) - Configure your setup
- [API Reference](api-reference.md) - Detailed API documentation
- [Troubleshooting](troubleshooting.md) - Solve common issues

For more examples and community contributions, visit our [GitHub repository](https://github.com/yourusername/SmartRAG).
