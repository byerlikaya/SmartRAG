---
layout: default
title: Troubleshooting
description: Common issues and solutions for SmartRAG
lang: en
---

<div class="page-content">
    <div class="container">
        <!-- Common Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Common Issues</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Quick solutions to frequent problems.</p>
                    
                    <h3>Configuration Issues</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Invalid API Key</h5>
                        <p><strong>Problem:</strong> "Unauthorized" or "Invalid API key" errors</p>
                        <p><strong>Solution:</strong> Check your API keys in appsettings.json</p>
                    </div>
                    
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Missing Configuration</h5>
                        <p><strong>Problem:</strong> "Configuration not found" errors</p>
                        <p><strong>Solution:</strong> Ensure SmartRAG section exists in appsettings.json</p>
                    </div>

                    <h3>Service Registration Issues</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Service Not Registered</h5>
                        <p><strong>Problem:</strong> "Unable to resolve service" errors</p>
                        <p><strong>Solution:</strong> Add SmartRAG services in Program.cs:</p>
                        <div class="code-example">
                            <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
});</code></pre>
                        </div>
                    </div>

                    <h3>Audio Processing Issues</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Google Speech-to-Text Errors</h5>
                        <p><strong>Problem:</strong> Audio transcription fails</p>
                        <p><strong>Solution:</strong> Verify Google API key and supported audio format</p>
                    </div>

                    <h3>Storage Issues</h3>
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Redis Connection Failed</h5>
                        <p><strong>Problem:</strong> Cannot connect to Redis</p>
                        <p><strong>Solution:</strong> Check Redis connection string and ensure Redis is running</p>
                    </div>
                    
                    <div class="alert alert-warning">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>Qdrant Connection Failed</h5>
                        <p><strong>Problem:</strong> Cannot connect to Qdrant</p>
                        <p><strong>Solution:</strong> Verify Qdrant host and API key configuration</p>
                    </div>
                </div>
            </div>
        </section>

        <!-- Performance Issues Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Performance Issues</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Optimize SmartRAG performance.</p>
                    
                    <h3>Slow Document Processing</h3>
                    <div class="alert alert-info">
                        <h5><i class="fas fa-info-circle me-2"></i>Optimization Tips</h5>
                        <ul class="mb-0">
                            <li>Use appropriate chunk sizes (500-1000 characters)</li>
                            <li>Enable Redis caching for better performance</li>
                            <li>Use Qdrant for production vector storage</li>
                            <li>Process documents in batches</li>
                        </ul>
                    </div>

                    <h3>Memory Issues</h3>
                    <div class="alert alert-info">
                        <h5><i class="fas fa-info-circle me-2"></i>Memory Management</h5>
                        <ul class="mb-0">
                            <li>Limit document size for processing</li>
                            <li>Use streaming for large files</li>
                            <li>Clear embeddings cache periodically</li>
                            <li>Monitor memory usage in production</li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>

        <!-- Debugging Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Debugging</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Enable logging and debugging.</p>
                    
                    <h3>Enable Debug Logging</h3>
                    <div class="code-example">
                        <pre><code class="language-json">{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SmartRAG": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}</code></pre>
                    </div>

                    <h3>Check Service Status</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Check if services are registered
var serviceProvider = services.BuildServiceProvider();
var documentService = serviceProvider.GetService<IDocumentService>();
var searchService = serviceProvider.GetService<IDocumentSearchService>();

if (documentService == null || searchService == null)
{
    Console.WriteLine("SmartRAG services not properly registered!");
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Still Need Help?</h4>
                        <p class="mb-0">If you can't find a solution:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/en/getting-started">Getting Started Guide</a></li>
                            <li><a href="{{ site.baseurl }}/en/configuration">Configuration Guide</a></li>
                            <li><a href="{{ site.baseurl }}/en/api-reference">API Reference</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">Open an issue on GitHub</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Contact support via email</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>