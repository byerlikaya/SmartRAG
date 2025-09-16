---
layout: default
title: API Reference
description: Complete API documentation for SmartRAG services and interfaces
lang: en
---

<div class="page-content">
    <div class="container">
        <!-- Core Interfaces Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Core Interfaces</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>SmartRAG provides several core interfaces for document processing and management.</p>
                    
                    <h3>IDocumentService</h3>
                    <p>The main service for document operations.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentService
{
    Task&lt;Document&gt; UploadDocumentAsync(IFormFile file);
    Task&lt;IEnumerable&lt;Document&gt;&gt; GetAllDocumentsAsync();
    Task&lt;Document&gt; GetDocumentByIdAsync(string id);
    Task&lt;bool&gt; DeleteDocumentAsync(string id);
    Task&lt;IEnumerable&lt;DocumentChunk&gt;&gt; SearchDocumentsAsync(string query, int maxResults = 10);
}</code></pre>
                    </div>

                    <h3>IDocumentSearchService</h3>
                    <p>The service for AI-powered question answering with automatic session management.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentSearchService
{
    Task&lt;List&lt;DocumentChunk&gt;&gt; SearchDocumentsAsync(string query, int maxResults = 5);
    Task&lt;RagResponse&gt; GenerateRagAnswerAsync(string query, int maxResults = 5, bool startNewConversation = false);
}</code></pre>
                    </div>

                    <h4>GenerateRagAnswerAsync</h4>
                    <p>Generates AI-powered answers with automatic session management and conversation history.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">Task&lt;RagResponse&gt; GenerateRagAnswerAsync(string query, int maxResults = 5, bool startNewConversation = false)</code></pre>
                    </div>
                    
                    <p><strong>Parameters:</strong></p>
                    <ul>
                        <li><code>query</code> (string): The user's question</li>
                        <li><code>maxResults</code> (int): Maximum number of document chunks to retrieve (default: 5)</li>
                        <li><code>startNewConversation</code> (bool): Start a new conversation session (default: false)</li>
                    </ul>
                    
                    <p><strong>Returns:</strong> <code>RagResponse</code> with AI answer, sources, and metadata</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Basic usage
var response = await documentSearchService.GenerateRagAnswerAsync("What is the weather?");

// Start new conversation
var response = await documentSearchService.GenerateRagAnswerAsync("/new");</code></pre>
                    </div>

                    <h3>Other Key Interfaces</h3>
                    <p>Additional services for document processing and storage.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Document parsing and processing
IDocumentParserService - Parse documents and extract text
IDocumentRepository - Document storage operations
IAIService - AI provider communication
IAudioParserService - Audio transcription (Google Speech-to-Text)</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Models Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Key Models</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Essential data models for SmartRAG operations.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Main response model
public class RagResponse
{
    public string Query { get; set; }
    public string Answer { get; set; }
    public List&lt;SearchSource&gt; Sources { get; set; }
    public DateTime SearchedAt { get; set; }
}

// Document chunk for search results
public class DocumentChunk
{
    public string Id { get; set; }
    public string DocumentId { get; set; }
    public string Content { get; set; }
    public double RelevanceScore { get; set; }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Configuration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Configuration</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Key configuration options for SmartRAG.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// AI Providers
AIProvider.Anthropic    // Claude models
AIProvider.OpenAI       // GPT models
AIProvider.Gemini       // Google models

// Storage Providers  
StorageProvider.Qdrant  // Vector database
StorageProvider.Redis   // High-performance cache
StorageProvider.Sqlite  // Local database</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Quick Start Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Quick Start</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Get started with SmartRAG in minutes.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// 1. Register services
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
});

// 2. Inject and use
public class MyController : ControllerBase
{
    private readonly IDocumentSearchService _searchService;
    
    public async Task<ActionResult> Ask(string question)
    {
        var response = await _searchService.GenerateRagAnswerAsync(question);
        return Ok(response);
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Common Patterns Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Common Patterns</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Frequently used patterns and configurations.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">// Document upload
var document = await _documentService.UploadDocumentAsync(file);

// Document search  
var results = await _searchService.SearchDocumentsAsync(query, 10);

// RAG conversation
var response = await _searchService.GenerateRagAnswerAsync(question);

// Configuration
services.AddSmartRag(configuration, options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Redis;
});</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Error Handling Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Error Handling</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Common exceptions and error handling patterns.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">try
{
    var response = await _searchService.GenerateRagAnswerAsync(query);
    return Ok(response);
}
catch (SmartRagException ex)
{
    return BadRequest(ex.Message);
}
catch (Exception ex)
{
    return StatusCode(500, "Internal server error");
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Performance Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Performance Tips</h2>
                    <!-- Updated for v2.3.0 -->
                    <p>Optimize SmartRAG performance with these tips.</p>
                    
                    <div class="alert alert-info">
                        <ul class="mb-0">
                            <li><strong>Chunk Size</strong>: 500-1000 characters for optimal balance</li>
                            <li><strong>Batch Operations</strong>: Process multiple documents together</li>
                            <li><strong>Caching</strong>: Use Redis for better performance</li>
                            <li><strong>Vector Storage</strong>: Qdrant for production use</li>
                        </ul>
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
                        <p class="mb-0">If you need assistance with the API:</p>
                        <ul class="mb-0 mt-2">
                            <li><a href="{{ site.baseurl }}/en/getting-started">Getting Started Guide</a></li>
                            <li><a href="{{ site.baseurl }}/en/configuration">Configuration Options</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues" target="_blank">Open an issue on GitHub</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Contact support via email</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>