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
                    <h2>Basic Examples</h2>
                    <p>Simple examples to get you started with SmartRAG.</p>
                    
                    <h3>Simple Document Upload</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("upload")]
public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
{
    try
    {
        using var stream = file.OpenReadStream();
        var document = await _documentService.UploadDocumentAsync(
            stream, file.FileName, file.ContentType, "user123");
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
public async Task<ActionResult<IEnumerable<DocumentChunk>>> SearchDocuments(
    [FromQuery] string query, 
    [FromQuery] int maxResults = 10)
{
    try
    {
        var results = await _documentSearchService.SearchDocumentsAsync(query, maxResults);
        return Ok(results);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>RAG Answer Generation (with Conversation History)</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">    [HttpPost("search")]
    public async Task<ActionResult<object>> Search([FromBody] SearchRequest request)
    {
        string query = request?.Query ?? string.Empty;
        int maxResults = request?.MaxResults ?? 5;

        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query cannot be empty");

        try
        {
            var response = await _documentSearchService.GenerateRagAnswerAsync(query, maxResults);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

public class SearchRequest
{
    [Required]
    public string Query { get; set; } = string.Empty;

    [Range(1, 50)]
    [DefaultValue(5)]
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
                    <h2>Advanced Examples</h2>
                    <p>More complex examples for advanced use cases.</p>
                    
                    <h3>Batch Document Processing</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("upload-multiple")]
public async Task<ActionResult<List<Document>>> UploadMultipleDocuments(
    IEnumerable<IFormFile> files)
{
    try
    {
        var streams = new List<Stream>();
        var fileNames = new List<string>();
        var contentTypes = new List<string>();

        foreach (var file in files)
        {
            streams.Add(file.OpenReadStream());
            fileNames.Add(file.FileName);
            contentTypes.Add(file.ContentType);
        }

        var documents = await _documentService.UploadDocumentsAsync(
            streams, fileNames, contentTypes, "user123");
        
        return Ok(documents);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}</code></pre>
                    </div>

                    <h3>Document Management</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Get all documents
[HttpGet]
public async Task<ActionResult<List<Document>>> GetAllDocuments()
{
    var documents = await _documentService.GetAllDocumentsAsync();
    return Ok(documents);
}

// Get specific document
[HttpGet("{id}")]
public async Task<ActionResult<Document>> GetDocument(Guid id)
{
    var document = await _documentService.GetDocumentAsync(id);
    if (document == null)
        return NotFound();
    
    return Ok(document);
}

// Delete document
[HttpDelete("{id}")]
public async Task<ActionResult> DeleteDocument(Guid id)
{
    var success = await _documentService.DeleteDocumentAsync(id);
    if (!success)
        return NotFound();
    
    return NoContent();
}</code></pre>
                    </div>

                    <h3>Storage Statistics</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpGet("statistics")]
public async Task<ActionResult<Dictionary<string, object>>> GetStorageStatistics()
{
    var stats = await _documentService.GetStorageStatisticsAsync();
    return Ok(stats);
}</code></pre>
                    </div>

                    <h3>Embedding Operations</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Regenerate all embeddings
[HttpPost("regenerate-embeddings")]
public async Task<ActionResult> RegenerateAllEmbeddings()
{
    var success = await _documentService.RegenerateAllEmbeddingsAsync();
    if (success)
        return Ok("All embeddings regenerated successfully");
    else
        return BadRequest("Failed to regenerate embeddings");
}

// Clear all embeddings
[HttpPost("clear-embeddings")]
public async Task<ActionResult> ClearAllEmbeddings()
{
    var success = await _documentService.ClearAllEmbeddingsAsync();
    if (success)
        return Ok("All embeddings cleared successfully");
    else
        return BadRequest("Failed to clear embeddings");
}

// Clear all documents
[HttpPost("clear-all")]
public async Task<ActionResult> ClearAllDocuments()
{
    var success = await _documentService.ClearAllDocumentsAsync();
    if (success)
        return Ok("All documents cleared successfully");
    else
        return BadRequest("Failed to clear documents");
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Web API Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Web API Examples</h2>
                    <p>Complete controller examples based on actual SmartRAG implementation.</p>
                    
                    <h3>SearchController (Actual Implementation)</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class SearchController(IDocumentSearchService documentSearchService) : ControllerBase
{
    /// <summary>
    /// Search documents using RAG (Retrieval-Augmented Generation) with conversation history
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> Search([FromBody] Contracts.SearchRequest request)
    {
        string? query = request?.Query;
        int maxResults = request?.MaxResults ?? 5;

        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query cannot be empty");

        try
        {
            var response = await documentSearchService.GenerateRagAnswerAsync(query, maxResults);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}</code></pre>
                    </div>

                    <h3>SearchRequest Model (Actual Implementation)</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class SearchRequest
{
    [Required]
    public string Query { get; set; } = string.Empty;

    [Range(1, 50)]
    [DefaultValue(5)]
    public int MaxResults { get; set; } = 5;
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Console Application Example Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Console Application Example</h2>
                    <p>A complete console application example.</p>
                    
                    <div class="code-example">
                        <pre><code class="language-csharp">class Program
{
    static async Task Main(string[] args)
    {
        // Create configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
            
        var services = new ServiceCollection();
        
        // Configure services
        services.AddSmartRag(configuration, options =>
        {
            options.AIProvider = AIProvider.Anthropic;
            options.StorageProvider = StorageProvider.Qdrant;
            options.MaxChunkSize = 1000;
            options.ChunkOverlap = 200;
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService<IDocumentService>();
        var documentSearchService = serviceProvider.GetRequiredService<IDocumentSearchService>();
        
        Console.WriteLine("SmartRAG Console Application");
        Console.WriteLine("============================");
        
        while (true)
        {
            Console.WriteLine("\nOptions:");
            Console.WriteLine("1. Upload document");
            Console.WriteLine("2. Search documents");
            Console.WriteLine("3. Chat with documents");
            Console.WriteLine("4. List all documents");
            Console.WriteLine("5. Exit");
            Console.Write("Choose an option: ");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    await UploadDocument(documentService);
                    break;
                case "2":
                    await SearchDocuments(documentSearchService);
                    break;
                case "3":
                    await ChatWithDocuments(documentSearchService);
                    break;
                case "4":
                    await ListDocuments(documentService);
                    break;
                case "5":
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }
    
    static async Task UploadDocument(IDocumentService documentService)
    {
        Console.Write("Enter file path: ");
        var filePath = Console.ReadLine();
        
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File not found.");
            return;
        }
        
        try
        {
            var fileInfo = new FileInfo(filePath);
            using var fileStream = File.OpenRead(filePath);
            
            var document = await documentService.UploadDocumentAsync(
                fileStream, fileInfo.Name, "application/octet-stream", "console-user");
            Console.WriteLine($"Document uploaded successfully. ID: {document.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading document: {ex.Message}");
        }
    }
    
    static async Task SearchDocuments(IDocumentSearchService documentSearchService)
    {
        Console.Write("Enter search query: ");
        var query = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine("Query cannot be empty.");
            return;
        }
        
        try
        {
            var results = await documentSearchService.SearchDocumentsAsync(query, 5);
            Console.WriteLine($"Found {results.Count} results:");
            
            foreach (var result in results)
            {
                Console.WriteLine($"- {result.Content.Substring(0, Math.Min(100, result.Content.Length))}...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching documents: {ex.Message}");
        }
    }
    
    static async Task ChatWithDocuments(IDocumentSearchService documentSearchService)
    {
        Console.Write("Enter your question: ");
        var query = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine("Question cannot be empty.");
            return;
        }
        
        try
        {
            var response = await documentSearchService.GenerateRagAnswerAsync(query, 5);
            Console.WriteLine($"AI Answer: {response.Answer}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error chatting with documents: {ex.Message}");
        }
    }
    
    static async Task ListDocuments(IDocumentService documentService)
    {
        try
        {
            var documents = await documentService.GetAllDocumentsAsync();
            Console.WriteLine($"Total documents: {documents.Count}");
            
            foreach (var doc in documents)
            {
                Console.WriteLine($"- {doc.FileName} (ID: {doc.Id})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing documents: {ex.Message}");
        }
    }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Conversation History Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Conversation History Examples</h2>
                    <p>Examples demonstrating session-based conversation management.</p>
                    
                    <h3>Multi-Turn Conversation</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[HttpPost("conversation")]
public async Task<ActionResult<RagResponse>> StartConversation(
    [FromBody] ConversationRequest request)
{
    try
    {
        var response = await _documentSearchService.GenerateRagAnswerAsync(
            request.Question, 
            maxResults: 5
        );
        
        return Ok(new ConversationResponse
        {
            Answer = response.Answer,
            Sources = response.Sources
        });
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}

public class ConversationRequest
{
    public string Question { get; set; } = string.Empty;
}

public class ConversationResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<SearchSource> Sources { get; set; } = new();
}</code></pre>
                    </div>

                    <h3>Follow-up Questions</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// First question
var firstResponse = await _documentSearchService.GenerateRagAnswerAsync(
    "What is machine learning?", 
    5
);

// Follow-up question (remembers previous context automatically)
var followUpResponse = await _documentSearchService.GenerateRagAnswerAsync(
    "Can you give me more details about supervised learning?", 
    5
);

// Another follow-up
var anotherResponse = await _documentSearchService.GenerateRagAnswerAsync(
    "What are the advantages of deep learning?", 
    5
);</code></pre>
                    </div>

                    <h3>Session Management</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class ConversationController : ControllerBase
{
    private readonly IDocumentSearchService _documentSearchService;
    private readonly ILogger<ConversationController> _logger;
    
    public ConversationController(
        IDocumentSearchService documentSearchService,
        ILogger<ConversationController> logger)
    {
        _documentSearchService = documentSearchService;
        _logger = logger;
    }
    
    [HttpPost("start")]
    public async Task<ActionResult<ConversationSession>> StartNewSession()
    {
        var sessionId = Guid.NewGuid().ToString();
        _logger.LogInformation("Started new conversation session: {SessionId}", sessionId);
        
        return Ok(new ConversationSession { SessionId = sessionId });
    }
    
    [HttpPost("ask")]
    public async Task<ActionResult<RagResponse>> AskQuestion(
        [FromBody] QuestionRequest request)
    {
        try
        {
            var response = await _documentSearchService.GenerateRagAnswerAsync(
                request.Question,
                maxResults: 5
            );
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public class QuestionRequest
{
    public string Question { get; set; } = string.Empty;
}

public class ConversationSession
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}</code></pre>
                    </div>

                    <h3>Frontend Integration Example</h3>
                    <div class="code-example">
                        <pre><code class="language-javascript">// JavaScript frontend example
class ConversationManager {
    constructor(apiBaseUrl) {
        this.apiBaseUrl = apiBaseUrl;
    }
    
    async askQuestion(question) {
        const response = await fetch(`${this.apiBaseUrl}/conversation/ask`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                question: question
            })
        });
        
        return await response.json();
    }
}

// Usage
const conversation = new ConversationManager('https://api.example.com');
await conversation.askQuestion("What is artificial intelligence?");
await conversation.askQuestion("Can you explain machine learning?");  // Remembers previous context
await conversation.askQuestion("What are neural networks?");  // Continues conversation</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Configuration Examples Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <h2>Configuration Examples</h2>
                    <p>Different ways to configure SmartRAG services.</p>
                    
                    <h3>Basic Configuration</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
services.UseSmartRag(configuration,
    storageProvider: StorageProvider.InMemory,
    aiProvider: AIProvider.Gemini
);</code></pre>
                    </div>

                    <h3>Advanced Configuration</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Program.cs
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
    options.EnableFallbackProviders = true;
    options.FallbackProviders = new[] { AIProvider.Gemini, AIProvider.OpenAI };
});</code></pre>
                    </div>

                    <h3>appsettings.json Configuration</h3>
                    <div class="code-example">
                        <pre><code class="language-json">{
  "SmartRAG": {
    "AIProvider": "Anthropic",
    "StorageProvider": "Qdrant",
    "MaxChunkSize": 1000,
    "MinChunkSize": 50,
    "ChunkOverlap": 200,
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000,
    "RetryPolicy": "ExponentialBackoff",
    "EnableFallbackProviders": true,
    "FallbackProviders": ["Gemini", "OpenAI"]
  },
  "Anthropic": {
    "ApiKey": "your-anthropic-api-key"
  },
  "Qdrant": {
    "ApiKey": "your-qdrant-api-key"
  }
}</code></pre>
                    </div>
                </div>
            </div>
        </section>

        <!-- Need Help Section -->
        <section class="content-section">
            <div class="row">
                <div class="col-lg-8 mx-auto">
                    <div class="alert alert-info">
                        <h4><i class="fas fa-question-circle me-2"></i>Need Help?</h4>
                        <p class="mb-2">If you need assistance with examples:</p>
                        <ul class="mb-0">
                            <li><a href="{{ site.baseurl }}/en/getting-started">Getting Started Guide</a></li>
                            <li><a href="{{ site.baseurl }}/en/api-reference">API Reference</a></li>
                            <li><a href="https://github.com/byerlikaya/SmartRAG/issues">Open an issue on GitHub</a></li>
                            <li><a href="mailto:b.yerlikaya@outlook.com">Contact support via email</a></li>
                        </ul>
                    </div>
                </div>
            </div>
        </section>
    </div>
</div>
