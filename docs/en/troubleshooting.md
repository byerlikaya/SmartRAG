---
layout: default
title: Troubleshooting
description: Common issues and solutions for SmartRAG
lang: en
---

<!-- Page Header -->
<div class="page-header">
    <div class="container">
        <h1 class="page-title">Troubleshooting</h1>
        <p class="page-description">
            Solutions to common issues you may encounter when using SmartRAG
        </p>
    </div>
</div>

<!-- Main Content -->
<div class="main-content">
    <div class="container">
        
        <!-- Quick Navigation -->
        <div class="content-section">
            <div class="row">
                <div class="col-12">
                    <div class="alert alert-info" role="alert">
                        <i class="fas fa-info-circle me-2"></i>
                        <strong>Need help?</strong> If you can't find a solution here, check our 
                        <a href="{{ site.baseurl }}/en/getting-started" class="alert-link">Getting Started</a> guide 
                        or create an issue on <a href="https://github.com/byerlikaya/SmartRAG" class="alert-link" target="_blank">GitHub</a>.
                    </div>
                </div>
            </div>
        </div>

        <!-- Configuration Issues -->
        <div class="content-section">
            <h2>Configuration Issues</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-key"></i>
                        </div>
                        <h3>API Key Configuration</h3>
                        <p><strong>Problem:</strong> Getting authentication errors with AI or storage providers.</p>
                        <p><strong>Solution:</strong> Ensure your API keys are properly configured in <code>appsettings.json</code>:</p>
                        
                        <div class="code-example">
                            <pre><code class="language-json">{
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
                        
                        <p>Or set environment variables:</p>
                        <div class="code-example">
                            <pre><code class="language-bash"># Set environment variables
export ANTHROPIC_API_KEY=your-anthropic-api-key
export QDRANT_API_KEY=your-qdrant-api-key</code></pre>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-cogs"></i>
                        </div>
                        <h3>Service Registration Issues</h3>
                        <p><strong>Problem:</strong> Getting dependency injection errors.</p>
                        <p><strong>Solution:</strong> Ensure SmartRAG services are properly registered in your <code>Program.cs</code>:</p>
                        
                        <div class="code-example">
                            <pre><code class="language-csharp">using SmartRAG.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add SmartRAG services
builder.Services.AddSmartRag(builder.Configuration);

var app = builder.Build();
app.UseSmartRag(builder.Configuration, StorageProvider.Qdrant, AIProvider.Anthropic);
app.Run();</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Document Upload Issues -->
        <div class="content-section">
            <h2>Document Upload Issues</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-file-upload"></i>
                        </div>
                        <h3>File Size Limitations</h3>
                        <p><strong>Problem:</strong> Large documents fail to upload or process.</p>
                        <p><strong>Solutions:</strong></p>
                        <ul>
                            <li>Check your application's file size limits in <code>appsettings.json</code></li>
                            <li>Consider splitting large documents into smaller chunks</li>
                            <li>Ensure sufficient memory is available for processing</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-file-alt"></i>
                        </div>
                        <h3>Unsupported File Types</h3>
                        <p><strong>Problem:</strong> Getting errors for certain file formats.</p>
                        <p><strong>Solution:</strong> SmartRAG supports common text formats:</p>
                        <ul>
                            <li>PDF files (.pdf)</li>
                            <li>Text files (.txt)</li>
                            <li>Word documents (.docx)</li>
                            <li>Markdown files (.md)</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Search and Retrieval Issues -->
        <div class="content-section">
            <h2>Search and Retrieval Issues</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-search"></i>
                        </div>
                        <h3>No Search Results</h3>
                        <p><strong>Problem:</strong> Search queries return no results.</p>
                        <p><strong>Possible Solutions:</strong></p>
                        <ol>
                            <li><strong>Check document upload:</strong> Ensure documents were successfully uploaded</li>
                            <li><strong>Verify embeddings:</strong> Check if embeddings were generated properly</li>
                            <li><strong>Query specificity:</strong> Try more specific search terms</li>
                            <li><strong>Storage connection:</strong> Verify your storage provider is accessible</li>
                        </ol>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-chart-line"></i>
                        </div>
                        <h3>Poor Search Quality</h3>
                        <p><strong>Problem:</strong> Search results are not relevant.</p>
                        <p><strong>Solutions:</strong></p>
                        <ul>
                            <li>Adjust <code>MaxChunkSize</code> and <code>ChunkOverlap</code> settings</li>
                            <li>Use more specific search queries</li>
                            <li>Ensure documents are properly formatted</li>
                            <li>Check if embeddings are up-to-date</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Performance Issues -->
        <div class="content-section">
            <h2>Performance Issues</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-tachometer-alt"></i>
                        </div>
                        <h3>Slow Document Processing</h3>
                        <p><strong>Problem:</strong> Document upload and processing takes too long.</p>
                        <p><strong>Solutions:</strong></p>
                        <ul>
                            <li>Increase <code>MaxChunkSize</code> to reduce the number of chunks</li>
                            <li>Use a more powerful AI provider</li>
                            <li>Optimize your storage provider configuration</li>
                            <li>Consider using async operations throughout your application</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-memory"></i>
                        </div>
                        <h3>Memory Issues</h3>
                        <p><strong>Problem:</strong> Application runs out of memory during processing.</p>
                        <p><strong>Solutions:</strong></p>
                        <ul>
                            <li>Reduce <code>MaxChunkSize</code> to create smaller chunks</li>
                            <li>Process documents in batches</li>
                            <li>Monitor memory usage and optimize accordingly</li>
                            <li>Consider using streaming operations for large files</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Storage Provider Issues -->
        <div class="content-section">
            <h2>Storage Provider Issues</h2>
            
            <div class="row g-4">
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-database"></i>
                        </div>
                        <h3>Qdrant Connection Issues</h3>
                        <p><strong>Problem:</strong> Cannot connect to Qdrant.</p>
                        <p><strong>Solutions:</strong></p>
                        <ul>
                            <li>Verify Qdrant API key is correct</li>
                            <li>Check network connectivity to Qdrant service</li>
                            <li>Ensure Qdrant service is running and accessible</li>
                            <li>Check firewall settings</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-redis"></i>
                        </div>
                        <h3>Redis Connection Issues</h3>
                        <p><strong>Problem:</strong> Cannot connect to Redis.</p>
                        <p><strong>Solutions:</strong></p>
                        <ul>
                            <li>Verify Redis connection string</li>
                            <li>Ensure Redis server is running</li>
                            <li>Check network connectivity</li>
                            <li>Verify Redis configuration in <code>appsettings.json</code></li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-hdd"></i>
                        </div>
                        <h3>SQLite Issues</h3>
                        <p><strong>Problem:</strong> SQLite database errors.</p>
                        <p><strong>Solutions:</strong></p>
                        <ul>
                            <li>Check file permissions for database directory</li>
                            <li>Ensure sufficient disk space</li>
                            <li>Verify database file path is correct</li>
                            <li>Check for database corruption</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- AI Provider Issues -->
        <div class="content-section">
            <h2>AI Provider Issues</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-robot"></i>
                        </div>
                        <h3>Anthropic API Errors</h3>
                        <p><strong>Problem:</strong> Getting errors from Anthropic API.</p>
                        <p><strong>Solutions:</strong></p>
                        <ul>
                            <li>Verify API key is valid and has sufficient credits</li>
                            <li>Check API rate limits</li>
                            <li>Ensure proper API endpoint configuration</li>
                            <li>Monitor API usage and quotas</li>
                        </ul>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-brain"></i>
                        </div>
                        <h3>OpenAI API Errors</h3>
                        <p><strong>Problem:</strong> Getting errors from OpenAI API.</p>
                        <p><strong>Solutions:</strong></p>
                        <ul>
                            <li>Verify API key is valid</li>
                            <li>Check API rate limits and quotas</li>
                            <li>Ensure proper model configuration</li>
                            <li>Monitor API usage</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>

        <!-- Testing and Debugging -->
        <div class="content-section">
            <h2>Testing and Debugging</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-vial"></i>
                        </div>
                        <h3>Unit Testing</h3>
                        <p><strong>Problem:</strong> Tests fail due to SmartRAG dependencies.</p>
                        <p><strong>Solution:</strong> Use mocking for SmartRAG services in unit tests:</p>
                        
                        <div class="code-example">
                            <pre><code class="language-csharp">[Test]
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
}</code></pre>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-cogs"></i>
                        </div>
                        <h3>Integration Testing</h3>
                        <p><strong>Problem:</strong> Integration tests fail.</p>
                        <p><strong>Solution:</strong> Use test configuration and ensure proper setup:</p>
                        
                        <div class="code-example">
                            <pre><code class="language-csharp">[Test]
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
}</code></pre>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Common Error Messages -->
        <div class="content-section">
            <h2>Common Error Messages</h2>
            
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-exclamation-triangle"></i>
                        </div>
                        <h3>Common Errors</h3>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"Document not found"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Verify the document ID is correct</li>
                                <li>Check if the document was successfully uploaded</li>
                                <li>Ensure the document hasn't been deleted</li>
                            </ul>
                        </div>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"Storage provider not configured"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Verify <code>StorageProvider</code> setting in configuration</li>
                                <li>Ensure all required storage settings are provided</li>
                                <li>Check service registration</li>
                            </ul>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-6">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-exclamation-circle"></i>
                        </div>
                        <h3>More Errors</h3>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"AI provider not configured"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Verify <code>AIProvider</code> setting in configuration</li>
                                <li>Ensure API key is provided for the selected provider</li>
                                <li>Check service registration</li>
                            </ul>
                        </div>
                        
                        <div class="alert alert-warning" role="alert">
                            <strong>"Invalid file format"</strong>
                            <ul class="mb-0 mt-2">
                                <li>Ensure file is in a supported format</li>
                                <li>Check file extension and content</li>
                                <li>Verify file is not corrupted</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Getting Help -->
        <div class="content-section">
            <h2>Getting Help</h2>
            
            <div class="row g-4">
                <div class="col-lg-8">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fas fa-question-circle"></i>
                        </div>
                        <h3>Still Need Help?</h3>
                        <p>If you're still experiencing issues, follow these steps:</p>
                        
                        <div class="row g-3">
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-file-alt"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Check the logs</h5>
                                        <p class="text-muted">Review application logs for detailed error messages</p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-cog"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Verify configuration</h5>
                                        <p class="text-muted">Double-check all configuration settings</p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-play"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Test with minimal setup</h5>
                                        <p class="text-muted">Try with a simple configuration first</p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="col-md-6">
                                <div class="d-flex align-items-start">
                                    <div class="flex-shrink-0">
                                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;">
                                            <i class="fas fa-book"></i>
                                        </div>
                                    </div>
                                    <div class="flex-grow-1 ms-3">
                                        <h5>Review documentation</h5>
                                        <p class="text-muted">Check other documentation pages for guidance</p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                
                <div class="col-lg-4">
                    <div class="feature-card">
                        <div class="feature-icon">
                            <i class="fab fa-github"></i>
                        </div>
                        <h3>Additional Support</h3>
                        <p>For additional support, please refer to:</p>
                        
                        <div class="d-grid gap-3">
                            <a href="https://github.com/byerlikaya/SmartRAG" class="btn btn-primary" target="_blank">
                                <i class="fab fa-github me-2"></i>
                                GitHub Repository
                            </a>
                            <a href="https://github.com/byerlikaya/SmartRAG/issues" class="btn btn-outline-primary" target="_blank">
                                <i class="fas fa-bug me-2"></i>
                                Create an Issue
                            </a>
                            <a href="{{ site.baseurl }}/en/getting-started" class="btn btn-outline-primary">
                                <i class="fas fa-rocket me-2"></i>
                                Getting Started
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>

    </div>
</div>
