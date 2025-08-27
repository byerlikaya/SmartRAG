---
layout: default
title: Examples
description: Real-world examples and sample applications to learn from SmartRAG
lang: en
---

<div class="page-header">
    <div class="container">
        <div class="row">
            <div class="col-lg-8 mx-auto text-center">
                <h1 class="page-title">Examples</h1>
                <p class="page-description">
                    Real-world examples and sample applications to learn from SmartRAG
                </p>
            </div>
        </div>
    </div>
</div>

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
                        <pre><code class="language-csharp">public async Task&lt;IEnumerable&lt;Document&gt;&gt; ProcessMultipleDocumentsAsync(
    IEnumerable&lt;IFormFile&gt; files)
{
    var tasks = files.Select(async file =>
    {
        try
        {
            return await _documentService.UploadDocumentAsync(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process file: {FileName}", file.FileName);
            return null;
        }
    });

    var results = await Task.WhenAll(tasks);
    return results.Where(d => d != null);
}</code></pre>
                    </div>

                    <h3>Smart Query Intent Detection</h3>
                    <p>Automatically route queries to chat or document search based on intent analysis:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task&lt;QueryResult&gt; ProcessQueryAsync(string query)
{
    // Analyze query intent
    var intent = await _queryIntentService.AnalyzeIntentAsync(query);
    
    switch (intent.Type)
    {
        case QueryIntentType.Chat:
            // Route to conversational AI
            return await _chatService.ProcessChatQueryAsync(query);
            
        case QueryIntentType.DocumentSearch:
            // Route to document search
            var searchResults = await _documentService.SearchDocumentsAsync(query);
            return new QueryResult 
            { 
                Type = QueryResultType.DocumentSearch,
                Results = searchResults 
            };
            
        case QueryIntentType.Mixed:
            // Combine both approaches
            var chatResponse = await _chatService.ProcessChatQueryAsync(query);
            var docResults = await _documentService.SearchDocumentsAsync(query);
            
            return new QueryResult 
            { 
                Type = QueryResultType.Mixed,
                ChatResponse = chatResponse,
                DocumentResults = docResults 
            };
            
        default:
            throw new ArgumentException($"Unknown intent type: {intent.Type}");
    }
}</code></pre>
                    </div>

                    <h4>Enhanced Semantic Search</h4>
                    <p>Advanced search with hybrid scoring (80% semantic + 20% keyword) and context awareness:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">public async Task&lt;IEnumerable&lt;SearchResult&gt;&gt; EnhancedSearchAsync(
    string query, 
    SearchOptions options = null)
{
    // Configure hybrid scoring weights
    var searchConfig = new EnhancedSearchConfiguration
    {
        SemanticWeight = 0.8,        // 80% semantic similarity
        KeywordWeight = 0.2,          // 20% keyword matching
        ContextWindowSize = 512,      // Context awareness window
        MinSimilarityThreshold = 0.6, // Minimum similarity score
        EnableFuzzyMatching = true,   // Fuzzy keyword matching
        MaxResults = options?.MaxResults ?? 20
    };

    // Perform hybrid search
    var results = await _searchService.EnhancedSearchAsync(query, searchConfig);
    
    // Apply context-aware ranking
    var rankedResults = await _rankingService.RankByContextAsync(results, query);
    
    return rankedResults;
}</code></pre>
                    </div>

                    <h4>VoyageAI Integration</h4>
                    <p>High-quality embeddings for Anthropic Claude models using VoyageAI:</p>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Configure VoyageAI integration
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-anthropic-api-key";
    
    // Enable VoyageAI for high-quality embeddings
    options.EnableVoyageAI = true;
    options.VoyageAI.ApiKey = "your-voyageai-api-key";
    options.VoyageAI.Model = "voyage-large-2"; // Latest model
    options.VoyageAI.Dimensions = 1536; // Embedding dimensions
    options.VoyageAI.BatchSize = 100; // Batch processing
});

// Use VoyageAI embeddings in your service
public async Task&lt;IEnumerable&lt;float[]&gt;&gt; GenerateEmbeddingsAsync(
    IEnumerable&lt;string&gt; texts)
{
    var embeddingService = serviceProvider.GetRequiredService&lt;IVoyageAIEmbeddingService&gt;();
    
    // Generate high-quality embeddings
    var embeddings = await embeddingService.GenerateEmbeddingsAsync(texts);
    
    return embeddings;
}</code></pre>
                    </div>

                    <h4>Advanced VoyageAI Configuration</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Advanced VoyageAI configuration with custom settings
var voyageAIConfig = new VoyageAIConfiguration
{
    ApiKey = "your-voyageai-api-key",
    Model = "voyage-large-2",
    Dimensions = 1536,
    BatchSize = 100,
    MaxRetries = 3,
    Timeout = TimeSpan.FromSeconds(30),
    EnableCompression = true,
    CustomHeaders = new Dictionary&lt;string, string&gt;
    {
        ["User-Agent"] = "SmartRAG/1.1.0"
    }
};

services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-anthropic-api-key";
    
    // Configure VoyageAI with custom settings
    options.EnableVoyageAI = true;
    options.VoyageAI = voyageAIConfig;
});

// Controller implementation
[HttpPost("generate-embeddings")]
public async Task&lt;ActionResult&lt;EmbeddingResponse&gt;&gt; GenerateEmbeddings(
    [FromBody] EmbeddingRequest request)
{
    try
    {
        var embeddingService = _serviceProvider.GetRequiredService&lt;IVoyageAIEmbeddingService&gt;();
        
        var embeddings = await embeddingService.GenerateEmbeddingsAsync(request.Texts);
        
        return Ok(new EmbeddingResponse
        {
            Embeddings = embeddings,
            Model = "voyage-large-2",
            Dimensions = embeddings.FirstOrDefault()?.Length ?? 0,
            TotalTokens = request.Texts.Sum(t => t.Split(' ').Length)
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error generating embeddings with VoyageAI");
        return StatusCode(500, "Failed to generate embeddings");
    }
}</code></pre>
                    </div>

                    <h4>Search Configuration</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Configure enhanced semantic search
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
    
    // Enable enhanced semantic search
    options.EnableEnhancedSearch = true;
    options.SemanticWeight = 0.8;
    options.KeywordWeight = 0.2;
    options.ContextAwareness = true;
    options.FuzzyMatching = true;
});

// Use in your controller
[HttpGet("enhanced-search")]
public async Task&lt;ActionResult&lt;IEnumerable&lt;SearchResult&gt;&gt;&gt; EnhancedSearch(
    [FromQuery] string query,
    [FromQuery] int maxResults = 20)
{
    var options = new SearchOptions { MaxResults = maxResults };
    var results = await _searchService.EnhancedSearchAsync(query, options);
    return Ok(results);
}</code></pre>
                    </div>

                    <h4>Intent Analysis Configuration</h4>
                    <div class="code-example">
                        <pre><code class="language-csharp">// Configure intent detection
services.AddSmartRAG(options =>
{
    options.AIProvider = AIProvider.Anthropic;
    options.StorageProvider = StorageProvider.Qdrant;
    options.ApiKey = "your-api-key";
    
    // Enable smart query intent detection
    options.EnableQueryIntentDetection = true;
    options.IntentDetectionThreshold = 0.7; // Confidence threshold
    options.LanguageAgnostic = true; // Works with any language
});

// Use in your controller
[HttpPost("query")]
public async Task&lt;ActionResult&lt;QueryResult&gt;&gt; ProcessQuery([FromBody] QueryRequest request)
{
    var result = await _queryProcessor.ProcessQueryAsync(request.Query);
    return Ok(result);
}</code></pre>
                    </div>

                    <h3>Custom Chunking Strategy</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class CustomChunkingStrategy : IChunkingStrategy
{
    public IEnumerable&lt;string&gt; ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List&lt;string&gt;();
        var sentences = text.Split(new[] { '.', '!', '?' }, 
            StringSplitOptions.RemoveEmptyEntries);
        
        var currentChunk = new StringBuilder();
        
        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > chunkSize)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }
            }
            currentChunk.AppendLine(sentence.Trim() + ".");
        }
        
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }
        
        return chunks;
    }
}</code></pre>
                    </div>

                    <h3>Custom AI Provider</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">public class CustomAIProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    public CustomAIProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["CustomAI:ApiKey"];
    }
    
    public async Task&lt;float[]&gt; GenerateEmbeddingAsync(string text)
    {
        var request = new
        {
            text = text,
            model = "custom-embedding-model"
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            "https://api.customai.com/embeddings", request);
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync&lt;EmbeddingResponse&gt;();
        return result.Embedding;
    }
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
                    <p>Complete controller examples for web applications.</p>
                    
                    <h3>Complete Controller</h3>
                    <div class="code-example">
                        <pre><code class="language-csharp">[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger&lt;DocumentsController&gt; _logger;
    
    public DocumentsController(
        IDocumentService documentService,
        ILogger&lt;DocumentsController&gt; logger)
    {
        _documentService = documentService;
        _logger = logger;
    }
    
    [HttpPost("upload")]
    public async Task&lt;ActionResult&lt;Document&gt;&gt; UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");
            
        try
        {
            var document = await _documentService.UploadDocumentAsync(file);
            _logger.LogInformation("Document uploaded: {DocumentId}", document.Id);
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document: {FileName}", file.FileName);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("search")]
    public async Task&lt;ActionResult&lt;IEnumerable&lt;DocumentChunk&gt;&gt;&gt; SearchDocuments(
        [FromQuery] string query, 
        [FromQuery] int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query parameter is required");
            
        try
        {
            var results = await _documentService.SearchDocumentsAsync(query, maxResults);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query: {Query}", query);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("{id}")]
    public async Task&lt;ActionResult&lt;Document&gt;&gt; GetDocument(string id)
    {
        try
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound();
                
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document: {DocumentId}", id);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpDelete("{id}")]
    public async Task&lt;ActionResult&gt; DeleteDocument(string id)
    {
        try
        {
            var success = await _documentService.DeleteDocumentAsync(id);
            if (!success)
                return NotFound();
                
            _logger.LogInformation("Document deleted: {DocumentId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document: {DocumentId}", id);
            return BadRequest(ex.Message);
        }
    }
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
        var services = new ServiceCollection();
        
        // Configure services
        services.AddSmartRAG(options =>
        {
            options.AIProvider = AIProvider.Anthropic;
            options.StorageProvider = StorageProvider.Qdrant;
            options.ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            options.ChunkSize = 1000;
            options.ChunkOverlap = 200;
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var documentService = serviceProvider.GetRequiredService&lt;IDocumentService&gt;();
        
        Console.WriteLine("SmartRAG Console Application");
        Console.WriteLine("============================");
        
        while (true)
        {
            Console.WriteLine("\nOptions:");
            Console.WriteLine("1. Upload document");
            Console.WriteLine("2. Search documents");
            Console.WriteLine("3. List all documents");
            Console.WriteLine("4. Exit");
            Console.Write("Choose an option: ");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    await UploadDocument(documentService);
                    break;
                case "2":
                    await SearchDocuments(documentService);
                    break;
                case "3":
                    await ListDocuments(documentService);
                    break;
                case "4":
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
            var fileStream = File.OpenRead(filePath);
            
            // Create a mock IFormFile
            var formFile = new FormFile(fileStream, 0, fileInfo.Length, 
                fileInfo.Name, fileInfo.Name);
            
            var document = await documentService.UploadDocumentAsync(formFile);
            Console.WriteLine($"Document uploaded successfully. ID: {document.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading document: {ex.Message}");
        }
    }
    
    static async Task SearchDocuments(IDocumentService documentService)
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
            var results = await documentService.SearchDocumentsAsync(query, 5);
            Console.WriteLine($"Found {results.Count()} results:");
            
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
    
    static async Task ListDocuments(IDocumentService documentService)
    {
        try
        {
            var documents = await documentService.GetAllDocumentsAsync();
            Console.WriteLine($"Total documents: {documents.Count()}");
            
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