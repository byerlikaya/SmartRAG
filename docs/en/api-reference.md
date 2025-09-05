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
                    
                    <p><strong>Returns:</strong></p>
                    <ul>
                        <li><code>RagResponse</code>: Contains the AI answer, sources, and metadata</li>
                    </ul>
                    
                    <p><strong>Special Commands:</strong></p>
                    <ul>
                        <li><code>/new</code>, <code>/reset</code>, <code>/clear</code> - Start a new conversation</li>
                    </ul>

                    <p><strong>Usage Examples:</strong></p>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Normal conversation (continues existing session)
var response = await documentSearchService.GenerateRagAnswerAsync("What is the weather?");

// Start new conversation programmatically
var response = await documentSearchService.GenerateRagAnswerAsync("Hello", startNewConversation: true);

// Start new conversation with command
var response = await documentSearchService.GenerateRagAnswerAsync("/new");</code></pre>
                    </div>

                    <h3>IDocumentParserService</h3>
                    <p>Service for parsing and processing documents.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentParserService
{
    Task&lt;string&gt; ExtractTextAsync(IFormFile file);
    Task&lt;IEnumerable&lt;DocumentChunk&gt;&gt; ParseDocumentAsync(string text, string documentId);
    Task&lt;IEnumerable&lt;DocumentChunk&gt;&gt; ParseDocumentAsync(Stream stream, string fileName, string documentId);
}</code></pre>
                    </div>

                    <h3>IDocumentRepository</h3>
                    <p>Repository for document storage operations.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">public interface IDocumentRepository
{
    Task&lt;Document&gt; AddAsync(Document document);
    Task&lt;Document&gt; GetByIdAsync(string id);
    Task&lt;IEnumerable&lt;Document&gt;&gt; GetAllAsync();
    Task&lt;bool&gt; DeleteAsync(string id);
    Task&lt;IEnumerable&lt;DocumentChunk&gt;&gt; SearchAsync(string query, int maxResults = 10);
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Models Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Models</h2>
                    <p>Core data models used throughout SmartRAG.</p>
                    
                    <h3>Document</h3>
                    <p>Represents a document in the system.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">public class Document
{
    public string Id { get; set; }
    public string FileName { get; set; }
    public string FileType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadDate { get; set; }
    public string Content { get; set; }
    public IEnumerable&lt;DocumentChunk&gt; Chunks { get; set; }
    public Dictionary&lt;string, object&gt; Metadata { get; set; }
}</code></pre>
                    </div>

                    <h3>DocumentChunk</h3>
                    <p>Represents a chunk of a document.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">public class DocumentChunk
{
    public string Id { get; set; }
    public string DocumentId { get; set; }
    public string Content { get; set; }
    public int ChunkIndex { get; set; }
    public float[] Embedding { get; set; }
    public Dictionary&lt;string, object&gt; Metadata { get; set; }
}</code></pre>
                    </div>

                    <h3>RagResponse</h3>
                    <p>Response model for AI-powered question answering with conversation context.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">public class RagResponse
{
    public string Query { get; set; }
    public string Answer { get; set; }
    public List&lt;SearchSource&gt; Sources { get; set; }
    public DateTime SearchedAt { get; set; }
    public RagConfiguration Configuration { get; set; }
}

public class SearchSource
{
    public string DocumentId { get; set; }
    public string FileName { get; set; }
    public string RelevantContent { get; set; }
    public double RelevanceScore { get; set; }
}

public class RagConfiguration
{
    public string AIProvider { get; set; }
    public string StorageProvider { get; set; }
    public string Model { get; set; }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Enums Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Enums</h2>
                    <p>Enumeration types used for configuration.</p>
                    
                    <h3>AIProvider</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public enum AIProvider
{
    Anthropic,
    OpenAI,
    AzureOpenAI,
    Gemini,
    Custom
}</code></pre>
                    </div>

                    <h3>StorageProvider</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public enum StorageProvider
{
    Qdrant,
    Redis,
    Sqlite,
    InMemory,
    FileSystem,
    Custom
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Service Registration Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Service Registration</h2>
                    <p>How to register SmartRAG services in your application.</p>
                    
                    <h3>AddSmartRAG Extension</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmartRAG(
        this IServiceCollection services,
        Action&lt;SmartRagOptions&gt; configureOptions)
    {
        var options = new SmartRagOptions();
        configureOptions(options);
        
        services.Configure&lt;SmartRagOptions&gt;(opt => 
        {
            opt.AIProvider = options.AIProvider;
            opt.StorageProvider = options.StorageProvider;
            opt.ApiKey = options.ApiKey;
            // ... other options
        });
        
        // Register services based on configuration
        services.AddScoped&lt;IDocumentService, DocumentService&gt;();
        services.AddScoped&lt;IDocumentParserService, DocumentParserService&gt;();
        
        // Register appropriate repository
        switch (options.StorageProvider)
        {
            case StorageProvider.Qdrant:
                services.AddScoped&lt;IDocumentRepository, QdrantDocumentRepository&gt;();
                break;
            case StorageProvider.Redis:
                services.AddScoped&lt;IDocumentRepository, RedisDocumentRepository&gt;();
                break;
            // ... other cases
        }
        
        return services;
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Usage Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Usage Examples</h2>
                    <p>Common usage patterns and examples.</p>
                    
                    <h3>Basic Document Upload</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("upload")]
public async Task&lt;ActionResult&lt;Document&gt;&gt; UploadDocument(IFormFile file)
{
    try
    {
        var document = await _documentService.UploadDocumentAsync(file);
        return Ok(document);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>Document Search</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpGet("search")]
public async Task&lt;ActionResult&lt;IEnumerable&lt;DocumentChunk&gt;&gt;&gt; SearchDocuments(
    [FromQuery] string query, 
    [FromQuery] int maxResults = 10)
{
    try
    {
        var results = await _documentService.SearchDocumentsAsync(query, maxResults);
        return Ok(results);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>Custom Configuration</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = Configuration["SmartRAG:ApiKey"];
    options.ChunkSize = 800;
    options.ChunkOverlap = 150;
    options.QdrantUrl = "http://localhost:6333";
    options.CollectionName = "my_documents";
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
                    <p>Common exceptions and error handling patterns.</p>
                    
                    <h3>Common Exceptions</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class SmartRagException : Exception
{
    public SmartRagException(string message) : base(message) { }
    public SmartRagException(string message, Exception innerException) 
        : base(message, innerException) { }
}

public class DocumentProcessingException : SmartRagException
{
    public DocumentProcessingException(string message) : base(message) { }
}

public class StorageException : SmartRagException
{
    public StorageException(string message) : base(message) { }
}</code></pre>
                    </div>

                    <h3>Error Response Model</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class ErrorResponse
{
    public string Message { get; set; }
    public string ErrorCode { get; set; }
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Performance Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Performance Considerations</h2>
                    <p>Tips for optimizing SmartRAG performance.</p>
                    
                    <h3>Chunking Strategy</h3>
                    <div class="alert alert-info">
                        <ul class="mb-0">
                            <li><strong>Small chunks</strong>: Better for precise search, more API calls</li>
                            <li><strong>Large chunks</strong>: Better context, fewer API calls</li>
                            <li><strong>Overlap</strong>: Ensures important information isn't split</li>
                        </ul>
                    </div>

                    <h3>Batch Operations</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task&lt;IEnumerable&lt;Document&gt;&gt; UploadDocumentsAsync(IEnumerable&lt;IFormFile&gt; files)
{
    var tasks = files.Select(file => UploadDocumentAsync(file));
    return await Task.WhenAll(tasks);
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