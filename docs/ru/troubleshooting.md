---
layout: default
title: Troubleshooting
description: Common issues and solutions to help you resolve problems with SmartRAG
lang: en
---

# Troubleshooting

Common issues and solutions to help you resolve problems with SmartRAG.

## Common Issues

### Build Errors

#### CS0246: The type or namespace name 'SmartRAG' could not be found

**Problem**: The SmartRAG package is not properly referenced.

**Solution**: 
1. Ensure the package is installed:
   ```bash
   dotnet add package SmartRAG
   ```
2. Check your `.csproj` file includes the reference:
   ```xml
   <PackageReference Include="SmartRAG" Version="1.0.3" />
   ```
3. Restore packages:
   ```bash
   dotnet restore
   ```

#### CS1061: 'IServiceCollection' does not contain a definition for 'AddSmartRAG'

**Problem**: The SmartRAG extension method is not available.

**Solution**:
1. Add the using statement:
   ```csharp
   using SmartRAG.Extensions;
   ```
2. Ensure the package is properly installed and referenced.

### Runtime Errors

#### InvalidOperationException: No AI provider configured

**Problem**: SmartRAG is not properly configured with an AI provider.

**Solution**:
```csharp
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic; // or OpenAI, AzureOpenAI, etc.
    options.ApiKey = "your-api-key";
    options.StorageProvider = StorageProvider.Qdrant; // or Redis, SQLite, etc.
});
```

#### UnauthorizedAccessException: Invalid API key

**Problem**: The API key is invalid or expired.

**Solution**:
1. Verify your API key is correct
2. Check if the API key has expired
3. Ensure the API key has the necessary permissions
4. For OpenAI, verify the key is from the correct organization

#### ConnectionException: Unable to connect to storage provider

**Problem**: Cannot connect to the configured storage provider.

**Solution**:
1. **Qdrant**: Check if Qdrant is running and accessible
   ```bash
   curl http://localhost:6333/collections
   ```
2. **Redis**: Verify Redis connection
   ```bash
   redis-cli ping
   ```
3. **SQLite**: Check file permissions and path
4. **Network**: Verify firewall settings and network connectivity

### Performance Issues

#### Slow Document Processing

**Problem**: Document processing is taking too long.

**Solution**:
1. Reduce chunk size:
   ```csharp
   options.ChunkSize = 500; // Default is 1000
   ```
2. Use smaller overlap:
   ```csharp
   options.ChunkOverlap = 100; // Default is 200
   ```
3. Consider using faster storage providers (Redis over SQLite)
4. Implement caching for frequently accessed documents

#### High Memory Usage

**Problem**: Application is consuming too much memory.

**Solution**:
1. Process documents in smaller batches
2. Implement streaming for large files
3. Use memory-efficient storage providers
4. Monitor and dispose of resources properly

### Configuration Issues

#### Missing Configuration Values

**Problem**: Required configuration values are missing.

**Solution**:
1. Check `appsettings.json`:
   ```json
   {
     "SmartRAG": {
       "AIProvider": "Anthropic",
       "StorageProvider": "Qdrant",
       "ApiKey": "your-api-key"
     }
   }
   ```
2. Use environment variables:
   ```bash
   export SMARTRAG_API_KEY="your-api-key"
   export SMARTRAG_AI_PROVIDER="Anthropic"
   ```

#### Incorrect Provider Configuration

**Problem**: Provider-specific configuration is incorrect.

**Solution**:
1. **Qdrant**:
   ```csharp
   options.QdrantUrl = "http://localhost:6333";
   options.CollectionName = "smartrag_documents";
   ```
2. **Redis**:
   ```csharp
   options.RedisConnectionString = "localhost:6379";
   options.DatabaseId = 0;
   ```
3. **SQLite**:
   ```csharp
   options.ConnectionString = "Data Source=smartrag.db";
   ```

## Debugging

### Enable Logging

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

### Check Service Registration

```csharp
// In Program.cs or Startup.cs
var serviceProvider = services.BuildServiceProvider();

// Check if services are registered
var documentService = serviceProvider.GetService<IDocumentService>();
if (documentService == null)
{
    Console.WriteLine("IDocumentService is not registered!");
}

var aiService = serviceProvider.GetService<IAIService>();
if (aiService == null)
{
    Console.WriteLine("IAIService is not registered!");
}
```

### Validate Configuration

```csharp
public class ConfigurationValidator
{
    public static bool ValidateSmartRagOptions(SmartRagOptions options)
    {
        if (string.IsNullOrEmpty(options.ApiKey))
        {
            throw new ArgumentException("API key is required");
        }
        
        if (options.ChunkSize <= 0)
        {
            throw new ArgumentException("Chunk size must be positive");
        }
        
        if (options.ChunkOverlap < 0)
        {
            throw new ArgumentException("Chunk overlap cannot be negative");
        }
        
        return true;
    }
}
```

## Testing

### Unit Test Setup

```csharp
[TestFixture]
public class SmartRAGTests
{
    private ServiceCollection _services;
    private ServiceProvider _serviceProvider;
    
    [SetUp]
    public void Setup()
    {
        _services = new ServiceCollection();
        
        // Add test configuration
        _services.AddSmartRAG(options =>
        {
            options.AIProvider = AIProvider.InMemory; // Use in-memory for testing
            options.StorageProvider = StorageProvider.InMemory;
            options.ApiKey = "test-key";
        });
        
        _serviceProvider = _services.BuildServiceProvider();
    }
    
    [Test]
    public async Task UploadDocument_ValidFile_ReturnsDocument()
    {
        // Arrange
        var documentService = _serviceProvider.GetRequiredService<IDocumentService>();
        var file = CreateTestFile();
        
        // Act
        var result = await documentService.UploadDocumentAsync(file);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotEmpty(result.Id);
    }
}
```

### Integration Test Setup

```csharp
[TestFixture]
public class SmartRAGIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    
    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["SmartRAG:AIProvider"] = "InMemory",
                        ["SmartRAG:StorageProvider"] = "InMemory",
                        ["SmartRAG:ApiKey"] = "test-key"
                    });
                });
            });
    }
    
    [Test]
    public async Task UploadEndpoint_ValidFile_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var file = CreateTestFile();
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);
        
        // Act
        var response = await client.PostAsync("/api/documents/upload", content);
        
        // Assert
        Assert.IsTrue(response.IsSuccessStatusCode);
    }
}
```

## Getting Help

### Check Documentation

- [Getting Started]({{ site.baseurl }}/en/getting-started) - Basic setup guide
- [Configuration]({{ site.baseurl }}/en/configuration) - Configuration options
- [API Reference]({{ site.baseurl }}/en/api-reference) - API documentation

### Community Support

- [GitHub Issues](https://github.com/byerlikaya/SmartRAG/issues) - Report bugs and request features
- [GitHub Discussions](https://github.com/byerlikaya/SmartRAG/discussions) - Ask questions and share solutions

### Contact Support

- **Email**: [b.yerlikaya@outlook.com](mailto:b.yerlikaya@outlook.com)
- **Response Time**: Usually within 24 hours

### Provide Information

When reporting an issue, please include:

1. **Environment**: .NET version, OS, SmartRAG version
2. **Configuration**: Your SmartRAG configuration
3. **Error Details**: Full error message and stack trace
4. **Steps to Reproduce**: Clear steps to reproduce the issue
5. **Expected vs Actual**: What you expected vs what happened

## Prevention

### Best Practices

1. **Always validate configuration** before starting the application
2. **Use environment-specific settings** for different deployment environments
3. **Implement proper error handling** and logging
4. **Test with small documents first** before processing large files
5. **Monitor performance metrics** in production
6. **Keep dependencies updated** to the latest stable versions

### Configuration Validation

```csharp
public static class SmartRAGConfigurationValidator
{
    public static void ValidateConfiguration(SmartRagOptions options)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(options.ApiKey))
            errors.Add("API key is required");
            
        if (options.ChunkSize <= 0)
            errors.Add("Chunk size must be positive");
            
        if (options.ChunkOverlap < 0)
            errors.Add("Chunk overlap cannot be negative");
            
        if (options.ChunkOverlap >= options.ChunkSize)
            errors.Add("Chunk overlap must be less than chunk size");
        
        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"SmartRAG configuration validation failed: {string.Join(", ", errors)}");
        }
    }
}
```

## Need Help?

If you're still experiencing issues:

- [Back to Documentation]({{ site.baseurl }}/en/) - Main documentation
- [Open an issue](https://github.com/byerlikaya/SmartRAG/issues) - GitHub Issues
- [Contact support](mailto:b.yerlikaya@outlook.com) - Email support
