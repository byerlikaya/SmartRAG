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

                    <h3>Build Issues</h3>
                    <div class="alert alert-warning">
                        <h4><i class="fas fa-exclamation-triangle me-2"></i>Warning</h4>
                        <p class="mb-0">Always run a clean solution first to resolve build errors.</p>
                    </div>

                    <h4>NuGet Package Error</h4>
                    <div class="code-example">
                        <pre><code class="language-bash"># Clean solution
dotnet clean
dotnet restore
dotnet build</code></pre>
                    </div>

                    <h4>Dependency Conflict</h4>
                    <div class="code-example">
                        <pre><code class="language-xml"><PackageReference Include="SmartRAG" Version="1.1.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" /></code></pre>
                    </div>

                    <h3>Runtime Issues</h3>
                    <div class="alert alert-info">
                        <h4><i class="fas fa-info-circle me-2"></i>Configuration</h4>
                        <p class="mb-0">Most runtime issues are related to configuration problems.</p>
                    </div>

                    <h4>AI Provider Not Configured</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Ensure proper configuration
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});</code></pre>
                    </div>

                    <h4>API Key Issues</h4>
                    <div class="code-example">
                        <pre><code class="language-bash"># Set environment variable
export SMARTRAG_API_KEY=your-api-key

# Or use appsettings.json
{
  "SmartRAG": {
    "ApiKey": "your-api-key"
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
                        <pre><code class="language-csharp">// Optimize chunk size
services.AddSmartRAG(options =>
{
    options.ChunkSize = 500; // Smaller chunks for faster processing
    options.ChunkOverlap = 100;
});</code></pre>
                    </div>

                    <h4>Memory Usage</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Use appropriate storage provider
services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Redis; // For high memory usage
    // or
    options.StorageProvider = StorageProvider.Qdrant; // For large datasets
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
                    <div class="code-example">
                        <pre><code class="language-csharp">// Configure logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// In your service
private readonly ILogger<DocumentService> _logger;

public async Task<Document> UploadDocumentAsync(IFormFile file)
{
    _logger.LogInformation("Uploading document: {FileName}", file.FileName);
    // ... implementation
}</code></pre>
                    </div>

                    <h3>Exception Handling</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">try
{
    var document = await _documentService.UploadDocumentAsync(file);
    return Ok(document);
}
catch (ArgumentException ex)
{
    _logger.LogError(ex, "Invalid file format");
    return BadRequest("Invalid file format");
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "AI provider error");
    return StatusCode(503, "Service temporarily unavailable");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return StatusCode(500, "Internal server error");
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
    var mockFile = new Mock<IFormFile>();
    var service = new DocumentService(mockLogger.Object);
    
    // Act
    var result = await service.UploadDocumentAsync(mockFile.Object);
    
    // Assert
    Assert.IsNotNull(result);
    Assert.IsNotEmpty(result.Id);
}</code></pre>
                    </div>

                    <h3>Integration Testing</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[Test]
public async Task SearchDocuments_ReturnsRelevantResults()
{
    // Arrange
    var testQuery = "test query";
    
    // Act
    var results = await _documentService.SearchDocumentsAsync(testQuery);
    
    // Assert
    Assert.IsNotNull(results);
    Assert.IsTrue(results.Any());
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
                </div>
            </div>
        </section>
    </div>
</div>
