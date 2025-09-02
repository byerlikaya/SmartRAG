---
layout: default
title: Troubleshooting
nav_order: 5
---

# Troubleshooting

This page provides solutions to common issues you may encounter when using SmartRAG.

<div class="troubleshooting-section">
## Configuration Issues

### API Key Configuration

<div class="problem-solution">
**Problem**: Getting authentication errors with AI or storage providers.

**Solution**: Ensure your API keys are properly configured in `appsettings.json`:
</div>

```json
{
  "SmartRAG": {
    "AIProvider": "Anthropic",
    "StorageProvider": "Qdrant",
    "MaxChunkSize": 1000,
    "ChunkOverlap": 200
  },
  "Anthropic": {
    "ApiKey": "your-anthropic-api-key"
  },
  "Qdrant": {
    "ApiKey": "your-qdrant-api-key"
  }
}
```

Or set environment variables:

```bash
# Set environment variables
export ANTHROPIC_API_KEY=your-anthropic-api-key
export QDRANT_API_KEY=your-qdrant-api-key
```

### Service Registration Issues

<div class="problem-solution">
**Problem**: Getting dependency injection errors.

**Solution**: Ensure SmartRAG services are properly registered in your `Program.cs`:
</div>

```csharp
using SmartRAG.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add SmartRAG services
builder.Services.AddSmartRag(builder.Configuration);

var app = builder.Build();
app.UseSmartRag(builder.Configuration, StorageProvider.Qdrant, AIProvider.Anthropic);
app.Run();
```
</div>

<div class="troubleshooting-section">
## Document Upload Issues

### File Size Limitations

<div class="problem-solution">
**Problem**: Large documents fail to upload or process.

**Solution**: 
</div>

- Check your application's file size limits in `appsettings.json`
- Consider splitting large documents into smaller chunks
- Ensure sufficient memory is available for processing

### Unsupported File Types

<div class="problem-solution">
**Problem**: Getting errors for certain file formats.

**Solution**: SmartRAG supports common text formats. Ensure your files are in supported formats:
</div>

- PDF files
- Text files (.txt)
- Word documents (.docx)
- Markdown files (.md)
</div>

<div class="troubleshooting-section">
## Search and Retrieval Issues

### No Search Results

<div class="problem-solution">
**Problem**: Search queries return no results.

**Possible Solutions**:
</div>

1. **Check document upload**: Ensure documents were successfully uploaded
2. **Verify embeddings**: Check if embeddings were generated properly
3. **Query specificity**: Try more specific search terms
4. **Storage connection**: Verify your storage provider is accessible

### Poor Search Quality

<div class="problem-solution">
**Problem**: Search results are not relevant.

**Solutions**:
</div>

- Adjust `MaxChunkSize` and `ChunkOverlap` settings
- Use more specific search queries
- Ensure documents are properly formatted
- Check if embeddings are up-to-date
</div>

<div class="troubleshooting-section">
## Performance Issues

### Slow Document Processing

<div class="problem-solution">
**Problem**: Document upload and processing takes too long.

**Solutions**:
</div>

- Increase `MaxChunkSize` to reduce the number of chunks
- Use a more powerful AI provider
- Optimize your storage provider configuration
- Consider using async operations throughout your application

### Memory Issues

<div class="problem-solution">
**Problem**: Application runs out of memory during processing.

**Solutions**:
</div>

- Reduce `MaxChunkSize` to create smaller chunks
- Process documents in batches
- Monitor memory usage and optimize accordingly
- Consider using streaming operations for large files
</div>

<div class="troubleshooting-section">
## Storage Provider Issues

### Qdrant Connection Issues

<div class="problem-solution">
**Problem**: Cannot connect to Qdrant.

**Solutions**:
</div>

- Verify Qdrant API key is correct
- Check network connectivity to Qdrant service
- Ensure Qdrant service is running and accessible
- Check firewall settings

### Redis Connection Issues

<div class="problem-solution">
**Problem**: Cannot connect to Redis.

**Solutions**:
</div>

- Verify Redis connection string
- Ensure Redis server is running
- Check network connectivity
- Verify Redis configuration in `appsettings.json`

### SQLite Issues

<div class="problem-solution">
**Problem**: SQLite database errors.

**Solutions**:
</div>

- Check file permissions for database directory
- Ensure sufficient disk space
- Verify database file path is correct
- Check for database corruption
</div>

<div class="troubleshooting-section">
## AI Provider Issues

### Anthropic API Errors

<div class="problem-solution">
**Problem**: Getting errors from Anthropic API.

**Solutions**:
</div>

- Verify API key is valid and has sufficient credits
- Check API rate limits
- Ensure proper API endpoint configuration
- Monitor API usage and quotas

### OpenAI API Errors

<div class="problem-solution">
**Problem**: Getting errors from OpenAI API.

**Solutions**:
</div>

- Verify API key is valid
- Check API rate limits and quotas
- Ensure proper model configuration
- Monitor API usage
</div>

<div class="troubleshooting-section">
## Testing and Debugging

### Unit Testing

<div class="problem-solution">
**Problem**: Tests fail due to SmartRAG dependencies.

**Solution**: Use mocking for SmartRAG services in unit tests:
</div>

```csharp
[Test]
public async Task TestDocumentUpload()
{
    // Arrange
    var mockDocumentService = new Mock<IDocumentService>();
    var mockSearchService = new Mock<IDocumentSearchService>();
    
    var controller = new DocumentsController(
        mockDocumentService.Object, 
        mockSearchService.Object, 
        Mock.Of<ILogger<DocumentsController>>());

    // Act & Assert
    // Your test logic here
}
```

### Integration Testing

<div class="problem-solution">
**Problem**: Integration tests fail.

**Solution**: Use test configuration and ensure proper setup:
</div>

```csharp
[Test]
public async Task TestEndToEndWorkflow()
{
    // Arrange
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.test.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();
    services.AddSmartRag(configuration);
    
    var serviceProvider = services.BuildServiceProvider();
    
    // Act & Assert
    // Your integration test logic here
}
```
</div>

<div class="troubleshooting-section">
## Common Error Messages

### "Document not found"
- Verify the document ID is correct
- Check if the document was successfully uploaded
- Ensure the document hasn't been deleted

### "Storage provider not configured"
- Verify `StorageProvider` setting in configuration
- Ensure all required storage settings are provided
- Check service registration

### "AI provider not configured"
- Verify `AIProvider` setting in configuration
- Ensure API key is provided for the selected provider
- Check service registration

### "Invalid file format"
- Ensure file is in a supported format
- Check file extension and content
- Verify file is not corrupted
</div>

<div class="troubleshooting-section">
## Getting Help

If you're still experiencing issues:

1. **Check the logs**: Review application logs for detailed error messages
2. **Verify configuration**: Double-check all configuration settings
3. **Test with minimal setup**: Try with a simple configuration first
4. **Check dependencies**: Ensure all required services are running
5. **Review documentation**: Check other documentation pages for guidance

For additional support, please refer to the project's GitHub repository or create an issue with detailed information about your problem.
</div>
