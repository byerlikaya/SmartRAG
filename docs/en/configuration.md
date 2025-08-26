---
layout: default
title: Configuration
description: Detailed configuration options and best practices for SmartRAG
lang: en
---

<div class="page-header">
    <div class="container">
        <div class="row">
            <div class="col-lg-8 mx-auto text-center">
                <h1 class="page-title">Configuration Guide</h1>
                <p class="page-description">
                    Configure SmartRAG for your specific needs with detailed options and best practices
                </p>
            </div>
        </div>
    </div>
</div>

<div class="page-content">
    <div class="container">
        <!-- Basic Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Basic Configuration</h2>
                    <p>SmartRAG can be configured with various options to suit your needs:</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
});</code></pre>
                    </div>

                    <h3>Configuration Options</h3>
                    <div class="table-responsive">
                        <table class="table table-striped">
                            <thead>
                                <tr>
                                    <th>Option</th>
                                    <th>Type</th>
                                    <th>Default</th>
                                    <th>Description</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td><code>AIProvider</code></td>
                                    <td><code>AIProvider</code></td>
                                    <td><code>Anthropic</code></td>
                                    <td>The AI provider to use for embeddings</td>
                                </tr>
                                <tr>
                                    <td><code>StorageProvider</code></td>
                                    <td><code>StorageProvider</code></td>
                                    <td><code>Qdrant</code></td>
                                    <td>The storage provider for vectors</td>
                                </tr>
                                <tr>
                                    <td><code>ApiKey</code></td>
                                    <td><code>string</code></td>
                                    <td>Required</td>
                                    <td>Your API key for the AI provider</td>
                                </tr>
                                <tr>
                                    <td><code>ModelName</code></td>
                                    <td><code>string</code></td>
                                    <td>Provider default</td>
                                    <td>The specific model to use</td>
                                </tr>
                                <tr>
                                    <td><code>ChunkSize</code></td>
                                    <td><code>int</code></td>
                                    <td>1000</td>
                                    <td>Size of document chunks</td>
                                </tr>
                                <tr>
                                    <td><code>ChunkOverlap</code></td>
                                    <td><code>int</code></td>
                                    <td>200</td>
                                    <td>Overlap between chunks</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </section>

        <!-- AI Providers Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>AI Provider Configuration</h2>
                    <p>Choose from multiple AI providers for embedding generation:</p>
                    
                    <div class="provider-tabs">
                        <div class="provider-tab active" data-tab="anthropic">Anthropic</div>
                        <div class="provider-tab" data-tab="openai">OpenAI</div>
                        <div class="provider-tab" data-tab="azure">Azure OpenAI</div>
                        <div class="provider-tab" data-tab="gemini">Gemini</div>
                        <div class="provider-tab" data-tab="custom">Custom</div>
                    </div>
                    
                    <div class="provider-content">
                        <div class="provider-panel active" id="anthropic">
                            <h3>Anthropic (Claude)</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.ApiKey = "your-anthropic-key";
    options.ModelName = "claude-3-sonnet-20240229";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="provider-panel" id="openai">
                            <h3>OpenAI</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.ApiKey = "your-openai-key";
    options.ModelName = "text-embedding-ada-002";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="provider-panel" id="azure">
                            <h3>Azure OpenAI</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.AzureOpenAI;
    options.ApiKey = "your-azure-key";
    options.Endpoint = "https://your-resource.openai.azure.com/";
    options.ModelName = "text-embedding-ada-002";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="provider-panel" id="gemini">
                            <h3>Google Gemini</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Gemini;
    options.ApiKey = "your-gemini-key";
    options.ModelName = "embedding-001";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="provider-panel" id="custom">
                            <h3>Custom AI Provider</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Custom;
    options.CustomEndpoint = "https://your-custom-api.com/v1/embeddings";
    options.ApiKey = "your-custom-key";
    options.ModelName = "your-custom-model";
});</code></pre>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- Storage Providers Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Storage Provider Configuration</h2>
                    <p>Choose the storage backend that best fits your needs:</p>
                    
                    <div class="storage-tabs">
                        <div class="storage-tab active" data-tab="qdrant">Qdrant</div>
                        <div class="storage-tab" data-tab="redis">Redis</div>
                        <div class="storage-tab" data-tab="sqlite">SQLite</div>
                        <div class="storage-tab" data-tab="memory">In-Memory</div>
                        <div class="storage-tab" data-tab="filesystem">File System</div>
                    </div>
                    
                    <div class="storage-content">
                        <div class="storage-panel active" id="qdrant">
                            <h3>Qdrant (Vector Database)</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Qdrant;
    options.QdrantUrl = "http://localhost:6333";
    options.CollectionName = "smartrag_documents";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="storage-panel" id="redis">
                            <h3>Redis (In-Memory Cache)</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Redis;
    options.RedisConnectionString = "localhost:6379";
    options.DatabaseId = 0;
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="storage-panel" id="sqlite">
                            <h3>SQLite (Local Database)</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.Sqlite;
    options.ConnectionString = "Data Source=smartrag.db";
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="storage-panel" id="memory">
                            <h3>In-Memory (Development)</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.InMemory;
    // No additional configuration needed
});</code></pre>
                            </div>
                        </div>
                        
                        <div class="storage-panel" id="filesystem">
                            <h3>File System (Local Storage)</h3>
                            <div class="code-example">
                                <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.StorageProvider = StorageProvider.FileSystem;
    options.StoragePath = "./data/smartrag";
});</code></pre>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- Advanced Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Advanced Configuration</h2>
                    <p>Fine-tune SmartRAG for your specific requirements:</p>
                    
                    <h3>Custom Chunking</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.ChunkSize = 500;
    options.ChunkOverlap = 100;
    options.ChunkingStrategy = ChunkingStrategy.Sentence;
});</code></pre>
                    </div>
                    
                    <h3>Document Processing</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.SupportedFormats = new[] { ".pdf", ".docx", ".txt" };
    options.MaxFileSize = 10 * 1024 * 1024; // 10MB
    options.EnableTextExtraction = true;
});</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Environment Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Environment Configuration</h2>
                    <p>Configure SmartRAG using environment variables or configuration files:</p>
                    
                    <h3>appsettings.json</h3>
                    <div class="code-example">
                        <pre><code class="language-json">{
  "SmartRAG": {
    "AIProvider": "Anthropic",
    "StorageProvider": "Qdrant",
    "ApiKey": "your-api-key",
    "ChunkSize": 1000,
    "ChunkOverlap": 200
  }
}</code></pre>
                    </div>
                    
                    <h3>Environment Variables</h3>
                    <div class="code-example">
                        <pre><code class="language-bash">export SMARTRAG_AI_PROVIDER=Anthropic
export SMARTRAG_STORAGE_PROVIDER=Qdrant
export SMARTRAG_API_KEY=your-api-key</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Best Practices Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Best Practices</h2>
                    <div class="row g-4">
                        <div class="col-md-6">
                            <div class="alert alert-warning">
                                <h4><i class="fas fa-key me-2"></i>API Keys</h4>
                                <p class="mb-0">Never hardcode API keys in source code. Use environment variables or secure configuration.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-info">
                                <h4><i class="fas fa-balance-scale me-2"></i>Chunk Size</h4>
                                <p class="mb-0">Balance between context and performance. Smaller chunks for precision, larger for context.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-success">
                                <h4><i class="fas fa-database me-2"></i>Storage</h4>
                                <p class="mb-0">Choose storage provider based on your scale and requirements.</p>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="alert alert-primary">
                                <h4><i class="fas fa-shield-alt me-2"></i>Security</h4>
                                <p class="mb-0">Use appropriate access controls and monitoring for production environments.</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>