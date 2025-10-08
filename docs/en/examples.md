---
layout: default
title: Examples
description: Practical examples and code samples for SmartRAG integration
lang: en
---

<div class="page-content">
    <div class="container">
        <!-- Basic Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Quick Examples</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Get started with SmartRAG in minutes.</p>
                    
                    <h3>Basic Usage</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// 1. Upload document
var document = await _documentService.UploadDocumentAsync(file);

// 2. Search documents
var results = await _searchService.SearchDocumentsAsync(query, 10);

// 3. Generate RAG answer with conversation history
var response = await _searchService.GenerateRagAnswerAsync(question);</code></pre>
                    </div>

                    <h3>Controller Example</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    [HttpPost("search")]
    public async Task<ActionResult> Search([FromBody] SearchRequest request)
    {
        var response = await _searchService.GenerateRagAnswerAsync(
            request.Query, request.MaxResults);
        return Ok(response);
    }
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 5;
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Advanced Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Advanced Usage</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Advanced examples for production use.</p>
                    
                    <h3>Batch Processing</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Upload multiple documents
var documents = await _documentService.UploadDocumentsAsync(files);

// Get storage statistics
var stats = await _documentService.GetStorageStatisticsAsync();

// Manage documents
var allDocs = await _documentService.GetAllDocumentsAsync();
await _documentService.DeleteDocumentAsync(documentId);</code></pre>
                    </div>

                    <h3>Maintenance Operations</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Regenerate embeddings
await _documentService.RegenerateAllEmbeddingsAsync();

// Clear data
await _documentService.ClearAllEmbeddingsAsync();
await _documentService.ClearAllDocumentsAsync();</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Configuration</h2>
                    <!-- Updated for v2.3.1 -->
                    <p>Configure SmartRAG for your needs.</p>
                    
                    <h3>Service Registration</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
    options.MaxChunkSize = 1000;
    options.ChunkOverlap = 200;
});</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Need Help?</h4>
                        <p class="mb-0">If you need assistance with examples:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/en/getting-started">Getting Started Guide</a></li>
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