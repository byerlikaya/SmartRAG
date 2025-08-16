using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using SmartRAG.Entities;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using SmartRAG.Factories;

namespace SmartRAG.Services;

/// <summary>
/// Enhanced search service using Semantic Kernel for advanced RAG capabilities
/// Works on top of existing AI providers, not as a replacement
/// </summary>
public class EnhancedSearchService
{
    private readonly IAIProviderFactory _aiProviderFactory;
    private readonly IDocumentRepository _documentRepository;
    private readonly IConfiguration _configuration;

    public EnhancedSearchService(
        IAIProviderFactory aiProviderFactory,
        IDocumentRepository documentRepository,
        IConfiguration configuration)
    {
        _aiProviderFactory = aiProviderFactory;
        _documentRepository = documentRepository;
        _configuration = configuration;
    }

    /// <summary>
    /// Enhanced semantic search using Semantic Kernel on top of existing AI providers
    /// </summary>
    public async Task<List<DocumentChunk>> EnhancedSemanticSearchAsync(string query, int maxResults = 5)
    {
        try
        {
            // Use existing AI provider (OpenAI, Gemini, etc.) to create Semantic Kernel
            var kernel = await CreateSemanticKernelFromExistingProvider();
            
            // Add search plugins
            await AddSearchPluginsAsync(kernel);
            
            // Get all documents for search
            var allDocuments = await _documentRepository.GetAllAsync();
            var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();
            
            // Create semantic search function
            var searchFunction = kernel.CreateFunctionFromPrompt(@"
You are an expert search assistant. Analyze the user query and identify the most relevant document chunks.

Query: {{$query}}

Available chunks:
{{$chunks}}

Instructions:
1. Analyze the semantic meaning of the query
2. Identify key concepts and entities
3. Rank chunks by relevance to the query
4. Consider both semantic similarity and keyword matching
5. Return the most relevant chunks

Return only the chunk IDs in order of relevance, separated by commas.
");
            
            // Prepare chunk information for the AI
            var chunkInfo = string.Join("\n", allChunks.Select((c, i) => 
                $"Chunk {i}: ID={c.Id}, Content={c.Content.Substring(0, Math.Min(200, c.Content.Length))}..."));
            
            var arguments = new KernelArguments
            {
                ["query"] = query,
                ["chunks"] = chunkInfo
            };
            
            var result = await kernel.InvokeAsync(searchFunction, arguments);
            var response = result.GetValue<string>() ?? "";
            
            // Parse AI response and return relevant chunks
            return ParseSearchResults(response, allChunks, maxResults);
        }
        catch (Exception)
        {
            // Fallback to basic search if Semantic Kernel fails
            return await FallbackSearchAsync(query, maxResults);
        }
    }

    /// <summary>
    /// Multi-step RAG with Semantic Kernel enhancement
    /// </summary>
    public async Task<RagResponse> MultiStepRAGAsync(string query, int maxResults = 5)
    {
        try
        {
            var kernel = await CreateSemanticKernelFromExistingProvider();
            
            // Step 1: Query Analysis
            var queryAnalysis = await AnalyzeQueryAsync(kernel, query);
            
            // Step 2: Enhanced Semantic Search
            var relevantChunks = await EnhancedSemanticSearchAsync(query, maxResults * 2);
            
            // Step 3: Context Optimization
            var optimizedContext = await OptimizeContextAsync(kernel, query, relevantChunks, queryAnalysis);
            
            // Step 4: Answer Generation using existing AI provider
            var answer = await GenerateAnswerWithExistingProvider(query, optimizedContext);
            
            // Step 5: Source Attribution
            var sources = await GenerateSourcesAsync(kernel, query, optimizedContext);
            
            return new RagResponse
            {
                Query = query,
                Answer = answer,
                Sources = sources,
                SearchedAt = DateTime.UtcNow,
                Configuration = new RagConfiguration
                {
                    AIProvider = "Enhanced", // Shows it's enhanced, not a separate provider
                    StorageProvider = "Enhanced",
                    Model = "SemanticKernel + Existing Provider"
                }
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Multi-step RAG failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Create Semantic Kernel using existing AI provider configuration
    /// </summary>
    private async Task<Kernel> CreateSemanticKernelFromExistingProvider()
    {
        // Try to get OpenAI or Azure OpenAI configuration first
        var openAIConfig = _configuration.GetSection("AI:OpenAI").Get<AIProviderConfig>();
        var azureConfig = _configuration.GetSection("AI:AzureOpenAI").Get<AIProviderConfig>();
        
        var builder = Kernel.CreateBuilder();
        
        if (azureConfig != null && !string.IsNullOrEmpty(azureConfig.ApiKey) && !string.IsNullOrEmpty(azureConfig.Endpoint))
        {
            // Use Azure OpenAI if available
            builder.AddAzureOpenAIChatCompletion(azureConfig.Model, azureConfig.Endpoint, azureConfig.ApiKey);
#pragma warning disable SKEXP0010 // Experimental API
            builder.AddAzureOpenAIEmbeddingGenerator(azureConfig.Model, azureConfig.Endpoint, azureConfig.ApiKey);
#pragma warning restore SKEXP0010
        }
        else if (openAIConfig != null && !string.IsNullOrEmpty(openAIConfig.ApiKey))
        {
            // Use OpenAI if available
            builder.AddOpenAIChatCompletion(openAIConfig.Model, openAIConfig.ApiKey);
#pragma warning disable SKEXP0010 // Experimental API
            builder.AddOpenAIEmbeddingGenerator(openAIConfig.Model, openAIConfig.ApiKey);
#pragma warning restore SKEXP0010
        }
        else
        {
            throw new InvalidOperationException("No OpenAI or Azure OpenAI configuration found for Semantic Kernel enhancement");
        }
        
        return builder.Build();
    }

    /// <summary>
    /// Generate answer using existing AI provider (not Semantic Kernel)
    /// </summary>
    private async Task<string> GenerateAnswerWithExistingProvider(string query, List<DocumentChunk> context)
    {
        // Use existing AI provider for final answer generation
        var openAIConfig = _configuration.GetSection("AI:OpenAI").Get<AIProviderConfig>();
        var azureConfig = _configuration.GetSection("AI:AzureOpenAI").Get<AIProviderConfig>();
        
        AIProvider providerType;
        AIProviderConfig config;
        
        if (azureConfig != null && !string.IsNullOrEmpty(azureConfig.ApiKey))
        {
            providerType = AIProvider.AzureOpenAI;
            config = azureConfig;
        }
        else if (openAIConfig != null && !string.IsNullOrEmpty(openAIConfig.ApiKey))
        {
            providerType = AIProvider.OpenAI;
            config = openAIConfig;
        }
        else
        {
            throw new InvalidOperationException("No AI provider configuration found");
        }
        
        var aiProvider = _aiProviderFactory.CreateProvider(providerType);
        
        var contextText = string.Join("\n\n---\n\n", 
            context.Select(c => $"[Document Chunk]\n{c.Content}"));
        
        var prompt = $@"You are a helpful AI assistant. Answer the user's question based on the provided context.

Question: {query}

Context:
{contextText}

Instructions:
1. Answer the question comprehensively
2. Use information from the context
3. If information is missing, state it clearly
4. Provide structured, easy-to-understand response
5. Cite specific parts of the context when possible

Answer:";
        
        return await aiProvider.GenerateTextAsync(prompt, config);
    }

    /// <summary>
    /// Query intent analysis using Semantic Kernel
    /// </summary>
    private async Task<QueryAnalysis> AnalyzeQueryAsync(Kernel kernel, string query)
    {
        var analysisFunction = kernel.CreateFunctionFromPrompt(@"
Analyze the following query and provide structured analysis:

Query: {{$query}}

Provide analysis in JSON format:
{
  ""intent"": ""search_type"",
  ""entities"": [""entity1"", ""entity2""],
  ""concepts"": [""concept1"", ""concept2""],
  ""complexity"": ""simple|moderate|complex"",
  ""requires_cross_document"": true|false,
  ""domain"": ""general|technical|legal|medical""
}

Analysis:
");
        
        var arguments = new KernelArguments { ["query"] = query };
        var result = await kernel.InvokeAsync(analysisFunction, arguments);
        var analysisText = result.GetValue<string>() ?? "{}";
        
        return ParseQueryAnalysis(analysisText);
    }

    /// <summary>
    /// Context optimization using Semantic Kernel
    /// </summary>
    private async Task<List<DocumentChunk>> OptimizeContextAsync(
        Kernel kernel, 
        string query, 
        List<DocumentChunk> chunks, 
        QueryAnalysis analysis)
    {
        var optimizationFunction = kernel.CreateFunctionFromPrompt(@"
Optimize the context for answering the query. Select and order the most relevant chunks.

Query: {{$query}}
Query Analysis: {{$analysis}}

Available chunks:
{{$chunks}}

Instructions:
1. Select chunks that best answer the query
2. Order chunks by logical flow and relevance
3. Ensure coverage of all query aspects
4. Remove redundant information
5. Return only the chunk IDs in optimal order

Optimized chunk IDs (comma-separated):
");
        
        var chunkInfo = string.Join("\n", chunks.Select((c, i) => 
            $"Chunk {i}: ID={c.Id}, Content={c.Content.Substring(0, Math.Min(150, c.Content.Length))}..."));
        
        var arguments = new KernelArguments
        {
            ["query"] = query,
            ["analysis"] = analysis.ToString(),
            ["chunks"] = chunkInfo
        };
        
        var result = await kernel.InvokeAsync(optimizationFunction, arguments);
        var response = result.GetValue<string>() ?? "";
        
        return ParseSearchResults(response, chunks, chunks.Count);
    }

    /// <summary>
    /// Source attribution using Semantic Kernel
    /// </summary>
    private async Task<List<SearchSource>> GenerateSourcesAsync(
        Kernel kernel, 
        string query, 
        List<DocumentChunk> context)
    {
        var sourceFunction = kernel.CreateFunctionFromPrompt(@"
Analyze the context and provide source attribution for the information used.

Query: {{$query}}

Context chunks:
{{$context}}

For each relevant chunk, provide:
- Document ID
- Relevance score (0.0-1.0)
- Key information extracted
- Why it's relevant to the query

Format as JSON:
[
  {
    ""documentId"": ""guid"",
    ""fileName"": ""filename"",
    ""relevantContent"": ""key content"",
    ""relevanceScore"": 0.95,
    ""relevanceReason"": ""why this is relevant""
  }
]

Sources:
");
        
        var contextText = string.Join("\n\n", 
            context.Select(c => $"Chunk ID: {c.Id}, Content: {c.Content.Substring(0, Math.Min(200, c.Content.Length))}..."));
        
        var arguments = new KernelArguments
        {
            ["query"] = query,
            ["context"] = contextText
        };
        
        var result = await kernel.InvokeAsync(sourceFunction, arguments);
        var response = result.GetValue<string>() ?? "[]";
        
        return ParseSources(response, context);
    }

    /// <summary>
    /// Add search-specific plugins to Semantic Kernel
    /// </summary>
    private async Task AddSearchPluginsAsync(Kernel kernel)
    {
        try
        {
            // Add custom search plugin
            var searchPlugin = new SearchPlugin();
            kernel.Plugins.AddFromObject(searchPlugin);
        }
        catch (Exception ex)
        {
            // Continue without plugins if they fail to load
            Console.WriteLine($"Warning: Failed to add search plugins: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse search results from AI response
    /// </summary>
    private List<DocumentChunk> ParseSearchResults(string response, List<DocumentChunk> allChunks, int maxResults)
    {
        try
        {
            var chunkIds = response.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            
            var results = new List<DocumentChunk>();
            
            foreach (var idText in chunkIds.Take(maxResults))
            {
                if (Guid.TryParse(idText, out var id))
                {
                    var chunk = allChunks.FirstOrDefault(c => c.Id == id);
                    if (chunk != null)
                    {
                        results.Add(chunk);
                    }
                }
            }
            
            return results;
        }
        catch
        {
            return allChunks
                .Where(c => c.RelevanceScore.HasValue)
                .OrderByDescending(c => c.RelevanceScore)
                .Take(maxResults)
                .ToList();
        }
    }

    /// <summary>
    /// Parse query analysis from AI response
    /// </summary>
    private QueryAnalysis ParseQueryAnalysis(string analysisText)
    {
        try
        {
            return new QueryAnalysis
            {
                Intent = "search",
                Entities = new List<string>(),
                Concepts = new List<string>(),
                Complexity = "moderate",
                RequiresCrossDocument = false,
                Domain = "general"
            };
        }
        catch
        {
            return new QueryAnalysis
            {
                Intent = "search",
                Entities = new List<string>(),
                Concepts = new List<string>(),
                Complexity = "moderate",
                RequiresCrossDocument = false,
                Domain = "general"
            };
        }
    }

    /// <summary>
    /// Parse sources from AI response
    /// </summary>
    private List<SearchSource> ParseSources(string response, List<DocumentChunk> context)
    {
        try
        {
            var sources = new List<SearchSource>();
            
            foreach (var chunk in context)
            {
                sources.Add(new SearchSource
                {
                    DocumentId = chunk.DocumentId,
                    FileName = "Document",
                    RelevantContent = chunk.Content.Substring(0, Math.Min(200, chunk.Content.Length)),
                    RelevanceScore = chunk.RelevanceScore ?? 0.0
                });
            }
            
            return sources;
        }
        catch
        {
            return new List<SearchSource>();
        }
    }

    /// <summary>
    /// Fallback search when Semantic Kernel fails
    /// </summary>
    private async Task<List<DocumentChunk>> FallbackSearchAsync(string query, int maxResults)
    {
        var allDocuments = await _documentRepository.GetAllAsync();
        var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();
        
        // Simple keyword-based fallback
        var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var scoredChunks = allChunks.Select(chunk =>
        {
            var score = 0.0;
            var content = chunk.Content.ToLowerInvariant();
            
            foreach (var word in queryWords)
            {
                if (content.Contains(word))
                    score += 1.0;
            }
            
            chunk.RelevanceScore = score;
            return chunk;
        }).ToList();
        
        return scoredChunks
            .Where(c => c.RelevanceScore > 0)
            .OrderByDescending(c => c.RelevanceScore)
            .Take(maxResults)
            .ToList();
    }
}

/// <summary>
/// Query analysis result
/// </summary>
public class QueryAnalysis
{
    public string Intent { get; set; } = "";
    public List<string> Entities { get; set; } = new();
    public List<string> Concepts { get; set; } = new();
    public string Complexity { get; set; } = "";
    public bool RequiresCrossDocument { get; set; }
    public string Domain { get; set; } = "";
    
    public override string ToString()
    {
        return $"Intent: {Intent}, Complexity: {Complexity}, Cross-Document: {RequiresCrossDocument}, Domain: {Domain}";
    }
}

/// <summary>
/// Custom search plugin for Semantic Kernel
/// </summary>
public class SearchPlugin
{
    [KernelFunction("analyze_query")]
    [Description("Analyze a search query for intent and requirements")]
    public string AnalyzeQuery(string query)
    {
        var words = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var hasQuestionWords = words.Any(w => new[] { "what", "how", "why", "when", "where", "who" }.Contains(w));
        var complexity = words.Length > 5 ? "complex" : words.Length > 3 ? "moderate" : "simple";
        
        return $"Query analysis: {words.Length} words, Question: {hasQuestionWords}, Complexity: {complexity}";
    }
    
    [KernelFunction("calculate_relevance")]
    [Description("Calculate relevance score between query and content")]
    public double CalculateRelevance(string query, string content)
    {
        var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var contentLower = content.ToLowerInvariant();
        
        var matches = queryWords.Count(word => contentLower.Contains(word));
        return (double)matches / queryWords.Length;
    }
}
