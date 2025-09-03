---
layout: default
title: Configuration
description: Configure SmartRAG with your preferred AI and storage providers
lang: en
---

<div class="page-content">
    <div class="container">
        <!-- Basic Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Basic Configuration</h2>
                    <p>SmartRAG can be configured with various options to suit your needs:</p>
                    
                                         <div class="code-example">
                         <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
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
                                     <td><code>OpenAI</code></td>
                                     <td>The AI provider to use for embeddings</td>
                                 </tr>
                                 <tr>
                                     <td><code>StorageProvider</code></td>
                                     <td><code>StorageProvider</code></td>
                                     <td><code>InMemory</code></td>
                                     <td>The storage provider for vectors</td>
                                 </tr>
                                 <tr>
                                     <td><code>MaxChunkSize</code></td>
                                     <td><code>int</code></td>
                                     <td>1000</td>
                                     <td>Maximum size of document chunks</td>
                                 </tr>
                                 <tr>
                                     <td><code>MinChunkSize</code></td>
                                     <td><code>int</code></td>
                                     <td>100</td>
                                     <td>Minimum size of document chunks</td>
                                 </tr>
                                 <tr>
                                     <td><code>ChunkOverlap</code></td>
                                     <td><code>int</code></td>
                                     <td>200</td>
                                     <td>Overlap between chunks</td>
                                 </tr>
                                 <tr>
                                     <td><code>MaxRetryAttempts</code></td>
                                     <td><code>int</code></td>
                                     <td>3</td>
                                     <td>Maximum retry attempts</td>
                                 </tr>
                                 <tr>
                                     <td><code>RetryDelayMs</code></td>
                                     <td><code>int</code></td>
                                     <td>1000</td>
                                     <td>Delay between retry attempts (ms)</td>
                                 </tr>
                                 <tr>
                                     <td><code>RetryPolicy</code></td>
                                     <td><code>RetryPolicy</code></td>
                                     <td><code>ExponentialBackoff</code></td>
                                     <td>Retry policy for failed requests</td>
                                 </tr>
                                 <tr>
                                     <td><code>EnableFallbackProviders</code></td>
                                     <td><code>bool</code></td>
                                     <td>false</td>
                                     <td>Enable fallback providers</td>
                                 </tr>
                                 <tr>
                                     <td><code>FallbackProviders</code></td>
                                     <td><code>AIProvider[]</code></td>
                                     <td>[]</td>
                                     <td>List of fallback AI providers</td>
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
                    
                    <div class="code-example">
                        <div class="code-tabs">
                            <button class="code-tab active" data-tab="ai-anthropic">Anthropic</button>
                            <button class="code-tab" data-tab="ai-openai">OpenAI</button>
                            <button class="code-tab" data-tab="ai-azure">Azure OpenAI</button>
                            <button class="code-tab" data-tab="ai-gemini">Gemini</button>
                            <button class="code-tab" data-tab="ai-custom">Custom</button>
                        </div>
                        
                                                 <div class="code-panel active" data-tab="ai-anthropic">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Anthropic": {
    "ApiKey": "your-anthropic-key",
    "Model": "claude-3-sonnet-20240229"
  }
}</code></pre>
                         </div>
                         
                         <div class="code-panel" data-tab="ai-openai">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.OpenAI;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "OpenAI": {
    "ApiKey": "your-openai-key",
    "Model": "gpt-4",
    "EmbeddingModel": "text-embedding-ada-002"
  }
}</code></pre>
                         </div>
                         
                         <div class="code-panel" data-tab="ai-azure">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.AzureOpenAI;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "AzureOpenAI": {
    "ApiKey": "your-azure-key",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "Model": "gpt-4",
    "EmbeddingModel": "text-embedding-ada-002"
  }
}</code></pre>
                         </div>
                         
                         <div class="code-panel" data-tab="ai-gemini">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Gemini;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Gemini": {
    "ApiKey": "your-gemini-key",
    "Model": "gemini-pro",
    "EmbeddingModel": "embedding-001"
  }
}</code></pre>
                         </div>
                         
                         <div class="code-panel" data-tab="ai-custom">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Custom;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Custom": {
    "ApiKey": "your-custom-key",
    "Endpoint": "https://your-custom-api.com/v1",
    "Model": "your-custom-model"
  }
}</code></pre>
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
                    
                    <div class="code-example">
                        <div class="code-tabs">
                            <button class="code-tab active" data-tab="storage-qdrant">Qdrant</button>
                            <button class="code-tab" data-tab="storage-redis">Redis</button>
                            <button class="code-tab" data-tab="storage-sqlite">SQLite</button>
                            <button class="code-tab" data-tab="storage-memory">In-Memory</button>
                            <button class="code-tab" data-tab="storage-filesystem">File System</button>
                        </div>
                        
                                                 <div class="code-panel active" data-tab="storage-qdrant">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.Qdrant;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Storage": {
    "Qdrant": {
      "Host": "localhost",
      "ApiKey": "your-qdrant-key",
      "CollectionName": "smartrag_documents",
      "VectorSize": 768
    }
  }
}</code></pre>
                         </div>
                         
                         <div class="code-panel" data-tab="storage-redis">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.Redis;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Storage": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Database": 0,
      "KeyPrefix": "smartrag:doc:"
    }
  }
}</code></pre>
                         </div>
                         
                         <div class="code-panel" data-tab="storage-sqlite">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.Sqlite;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Storage": {
    "Sqlite": {
      "DatabasePath": "SmartRag.db",
      "EnableForeignKeys": true
    }
  }
}</code></pre>
                         </div>
                         
                         <div class="code-panel" data-tab="storage-memory">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.InMemory;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});
// No additional configuration needed</code></pre>
                         </div>
                         
                         <div class="code-panel" data-tab="storage-filesystem">
                             <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.StorageProvider = StorageProvider.FileSystem;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});

// appsettings.json
{
  "Storage": {
    "FileSystem": {
      "FileSystemPath": "./smartrag_storage"
    }
  }
}</code></pre>
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
                         <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
{
    options.MaxChunkSize = 500;
    options.MinChunkSize = 50;
    options.ChunkOverlap = 100;
});</code></pre>
                     </div>
                     
                     <h3>Retry Configuration</h3>
                     <div class="code-example">
                         <pre><code class="language-csharp">services.AddSmartRag(configuration, options =>
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

        <!-- Environment Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Environment Configuration</h2>
                    <p>Configure SmartRAG using environment variables or configuration files:</p>
                    
                                         <h3>appsettings.json</h3>
                     <div class="code-example">
                         <pre><code class="language-json">{
  "Anthropic": {
    "ApiKey": "your-anthropic-key",
    "Model": "claude-3-sonnet-20240229"
  },
  "Storage": {
    "Qdrant": {
      "Host": "localhost",
      "ApiKey": "your-qdrant-key",
      "CollectionName": "smartrag_documents"
    }
  }
}</code></pre>
                     </div>
                     
                     <h3>Environment Variables</h3>
                     <div class="code-example">
                         <pre><code class="language-bash">export ANTHROPIC_API_KEY=your-anthropic-key
export QDRANT_API_KEY=your-qdrant-key</code></pre>
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