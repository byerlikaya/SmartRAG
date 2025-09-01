---
layout: default
title: Troubleshooting
description: Common issues and solutions for SmartRAG implementation
lang: en
---

<div class="page-content">
    <div class="container">
        <!-- Common Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Common Issues</h2>
                    <p>Common issues and solutions you may encounter when using SmartRAG.</p>

                    <h3>Service Registration Issues</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-exclamation-triangle me-2"></i>Warning</h4>
                        <p class="mb-0">Always ensure proper service registration and dependency injection setup.</p>
                    </div>

                    <h4>Service Not Registered</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Ensure services are properly registered
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// Get required services
var documentService = serviceProvider.GetRequiredService<IDocumentService>();
var documentSearchService = serviceProvider.GetRequiredService<IDocumentSearchService>();</code></pre>
                    </div>

                    <h4>Configuration Issues</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Ensure proper configuration
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.MinChunkSize = 50;
    options.ChunkOverlap = 200;
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
});</code></pre>
                    </div>

                    <h3>API Key Configuration</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Configuration</h4>
                        <p class="mb-0">API keys should be configured in appsettings.json or environment variables.</p>
                    </div>

                    <h4>Environment Variables</h4>
                    <div class="code-example">
                        <pre><code class="language-bash"># Set environment variables
export ANTHROPIC_API_KEY=your-anthropic-api-key
export QDRANT_API_KEY=your-qdrant-api-key

# Or use appsettings.json
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
}</code></pre>
                    </div>

                    <h3>Performance Issues</h3>
                    <div class="alert alert-success">
                        <h4><i class="fas fa-tachometer-alt me-2"></i>Optimization</h4>
                        <p class="mb-0">Performance can be improved with proper configuration.</p>
                    </div>

                    <h4>Slow Document Processing</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Optimize chunk size for faster processing
services.AddSmartRag(configuration, options =>
{
    options.MaxChunkSize = 500; // Smaller chunks for faster processing
    options.MinChunkSize = 50;
    options.ChunkOverlap = 100;
    options.MaxRetryAttempts = 2; // Reduce retries for faster failure
});</code></pre>
                    </div>

                    <h4>Memory Usage Optimization</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Use appropriate storage provider
services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.InMemory; // For small datasets
    // or
    options.StorageProvider = StorageProvider.Qdrant; // For large datasets
    options.EnableFallbackProviders = true; // Enable fallback for reliability
});</code></pre>
                    </div>

                    <h3>Retry Configuration</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Configure retry policies
services.AddSmartRag(configuration, options =>
{
    options.MaxRetryAttempts = 3;
    options.RetryDelayMs = 1000;
    options.RetryPolicy = RetryPolicy.ExponentialBackoff;
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new[] { AIProvider.Gemini, AIProvider.OpenAI };
});</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Debugging Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Debugging</h2>
                    <p>Tools and techniques to help you debug SmartRAG applications.</p>

                    <h3>Enable Logging</h3>
                    
                    <h4>Logging Configuration</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Configure logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Add SmartRAG specific logging
builder.Logging.AddFilter("SmartRAG", LogLevel.Debug);</code></pre>
                    </div>

                    <h4>Service Implementation</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">private readonly ILogger<DocumentsController> _logger;

public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
{
    _logger.LogInformation("Uploading document: {FileName}", file.FileName);
    try
    {
        using var stream = file.OpenReadStream();
        var document = await _documentService.UploadDocumentAsync(
            stream, file.FileName, file.ContentType, "user123");
        _logger.LogInformation("Document uploaded successfully: {DocumentId}", document.Id);
        return Ok(document);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to upload document: {FileName}", file.FileName);
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>Exception Handling</h3>
                    
                    <h4>Basic Error Handling</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">try
{
    using var stream = file.OpenReadStream();
    var document = await _documentService.UploadDocumentAsync(
        stream, file.FileName, file.ContentType, "user123");
    return Ok(document);
}
catch (ArgumentException ex)
{
    _logger.LogError(ex, "Invalid file format: {FileName}", file.FileName);
    return BadRequest("Invalid file format");
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "AI provider error: {Message}", ex.Message);
    return StatusCode(503, "Service temporarily unavailable");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error during upload: {FileName}", file.FileName);
    return StatusCode(500, "Internal server error");
}</code></pre>
                    </div>

                    <h4>Service-Level Error Handling</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy)
{
    try
    {
        _logger.LogInformation("Starting document upload: {FileName}", fileName);
        
        // Validate input
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("File stream is null or empty");
        
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required");
        
        // Process document
        var document = await ProcessDocumentAsync(fileStream, fileName, contentType, uploadedBy);
        
        _logger.LogInformation("Document uploaded successfully: {DocumentId}", document.Id);
        return document;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to upload document: {FileName}", fileName);
        throw;
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Testing Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Testing</h2>
                    <p>How to test your SmartRAG implementation.</p>

                    <h3>Unit Testing</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task UploadDocument_ValidFile_ReturnsDocument()
{
    // Arrange
    var mockDocumentService = new Mock<IDocumentService>();
    var mockDocumentSearchService = new Mock<IDocumentSearchService>();
    var mockLogger = new Mock<ILogger<DocumentsController>>();
    
    var controller = new DocumentsController(
        mockDocumentService.Object, 
        mockDocumentSearchService.Object, 
        mockLogger.Object);
    
    var mockFile = new Mock<IFormFile>();
    mockFile.Setup(f => f.FileName).Returns("test.pdf");
    mockFile.Setup(f => f.ContentType).Returns("application/pdf");
    mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
    
    var expectedDocument = new Document 
    { 
        Id = Guid.NewGuid(), 
        FileName = "test.pdf" 
    };
    
    mockDocumentService.Setup(s => s.UploadDocumentAsync(
        It.IsAny<Stream>(), 
        It.IsAny<string>(), 
        It.IsAny<string>(), 
        It.IsAny<string>()))
        .ReturnsAsync(expectedDocument);
    
    // Act
    var result = await controller.UploadDocument(mockFile.Object);
    
    // Assert
    var okResult = result as OkObjectResult;
    Assert.IsNotNull(okResult);
    Assert.AreEqual(expectedDocument, okResult.Value);
}</code></pre>
                    </div>

                    <h3>Integration Testing</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task SearchDocuments_ReturnsRelevantResults()
{
    // Arrange
    var mockDocumentSearchService = new Mock<IDocumentSearchService>();
    var mockDocumentService = new Mock<IDocumentService>();
    var mockLogger = new Mock<ILogger<DocumentsController>>();
    
    var controller = new DocumentsController(
        mockDocumentService.Object,
        mockDocumentSearchService.Object,
        mockLogger.Object);
    
    var testQuery = "test query";
    var expectedResults = new List<DocumentChunk>
    {
        new DocumentChunk { Content = "Test content 1" },
        new DocumentChunk { Content = "Test content 2" }
    };
    
    mockDocumentSearchService.Setup(s => s.SearchDocumentsAsync(testQuery, 10))
        .ReturnsAsync(expectedResults);
    
    // Act
    var result = await controller.SearchDocuments(testQuery);
    
    // Assert
    var okResult = result as OkObjectResult;
    Assert.IsNotNull(okResult);
    var results = okResult.Value as IEnumerable<DocumentChunk>;
    Assert.IsNotNull(results);
    Assert.AreEqual(expectedResults.Count, results.Count());
}</code></pre>
                    </div>

                    <h3>End-to-End Testing</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task CompleteWorkflow_UploadSearchChat_WorksCorrectly()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddSmartRag(configuration, options =>
    {
        options.AIProvider = AIProvider.Anthropic;
        options.StorageProvider = StorageProvider.InMemory;
        options.MaxChunkSize = 1000;
        options.ChunkOverlap = 200;
    });
    
    var serviceProvider = services.BuildServiceProvider();
    var documentService = serviceProvider.GetRequiredService<IDocumentService>();
    var documentSearchService = serviceProvider.GetRequiredService<IDocumentSearchService>();
    
    // Create test file
    var testContent = "This is a test document about artificial intelligence.";
    var testStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testContent));
    
    // Act - Upload
    var document = await documentService.UploadDocumentAsync(
        testStream, "test.txt", "text/plain", "test-user");
    
    // Assert - Upload
    Assert.IsNotNull(document);
    Assert.AreEqual("test.txt", document.FileName);
    
    // Act - Search
    var searchResults = await documentSearchService.SearchDocumentsAsync("artificial intelligence", 5);
    
    // Assert - Search
    Assert.IsNotNull(searchResults);
    Assert.IsTrue(searchResults.Count > 0);
    
    // Act - Chat
    var chatResponse = await documentSearchService.GenerateRagAnswerAsync("What is this document about?", 5);
    
    // Assert - Chat
    Assert.IsNotNull(chatResponse);
    Assert.IsFalse(string.IsNullOrWhiteSpace(chatResponse.Answer));
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Getting Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Getting Help</h2>
                    <p>If you're still having issues, here's how to get help.</p>

                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-github me-2"></i>GitHub Issues</h4>
                                <p class="mb-0">Report bugs and request features on GitHub.</p>
                                <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank" class="btn btn-sm btn-outline-info mt-2">Open Issue</a>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-envelope me-2"></i>Email Support</h4>
                                <p class="mb-0">Get direct help via email.</p>
                                <a href="mailto:b.yerlikaya@outlook.com" class="btn btn-sm btn-outline-success mt-2">Contact</a>
                            </div>
                        </div>
                    </div>

                    <h3>Before Asking for Help</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-list me-2"></i>Checklist</h4>
                        <ul class="mb-0">
                            <li>Check the <a href="{{ site.baseurl }}/en/getting-started">Getting Started</a> guide</li>
                            <li>Review the <a href="{{ site.baseurl }}/en/configuration">Configuration</a> documentation</li>
                            <li>Search existing <a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">GitHub Issues</a></li>
                            <li>Include error messages and configuration details</li>
                            <li>Check the <a href="{{ site.baseurl }}/en/api-reference">API Reference</a> for correct method signatures</li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>

        <!-- Prevention Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Prevention</h2>
                    <p>Best practices to avoid common issues.</p>

                    <h3>Configuration Best Practices</h3>
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="alert alert-primary">
                                <h4><i class="fas fa-key me-2"></i>API Keys</h4>
                                <p class="mb-0">Never hardcode API keys. Use environment variables or secure configuration.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-database me-2"></i>Storage</h4>
                                <p class="mb-0">Choose the right storage provider for your use case.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-shield-alt me-2"></i>Error Handling</h4>
                                <p class="mb-0">Implement proper error handling and logging.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-warning">
                                <h4><i class="fas fa-balance-scale me-2"></i>Performance</h4>
                                <p class="mb-0">Monitor performance and optimize chunk sizes.</p>
                            </div>
                        </div>
                    </div>

                    <h3>Development vs Production Configuration</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Development configuration
if (builder.Environment.IsDevelopment())
{
    services.AddSmartRag(configuration, options =>
    {
        options.AIProvider = AIProvider.Gemini; // Free tier for development
        options.StorageProvider = StorageProvider.InMemory; // Fast for development
        options.MaxChunkSize = 500;
        options.ChunkOverlap = 100;
        options.MaxRetryAttempts = 1; // Fast failure in development
    });
}
else
{
    // Production configuration
    services.AddSmartRag(configuration, options =>
    {
        options.AIProvider = AIProvider.Anthropic; // Better quality for production
        options.StorageProvider = StorageProvider.Qdrant; // Persistent storage
        options.MaxChunkSize = 1000;
        options.ChunkOverlap = 200;
        options.MaxRetryAttempts = 3;
        options.RetryDelayMs = 1000;
        options.RetryPolicy = RetryPolicy.ExponentialBackoff;
        options.EnableFallbackProviders = true;
    });
}</code></pre>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
