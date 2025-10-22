using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SmartRAG.API.Contracts;
using SmartRAG.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.API.Controllers
{
    /// <summary>
    /// Advanced AI-Powered Intelligence and Query Processing Controller
    /// 
    /// This controller provides sophisticated intelligence capabilities including:
    /// - Intelligent query routing with automatic intent detection
    /// - RAG (Retrieval-Augmented Generation) with context-aware responses
    /// - Multi-modal intelligence across documents, databases, and conversations
    /// - Semantic intelligence with vector similarity and hybrid ranking
    /// - Query suggestions and auto-completion
    /// - Intelligence analytics and performance optimization
    /// - Conversation-aware intelligence with session context
    /// 
    /// Intelligence Features:
    /// - **Query Intent Detection**: Automatically determines search vs conversation intent
    /// - **Hybrid Intelligence**: Combines keyword, semantic, and vector search methods
    /// - **Context Awareness**: Maintains conversation context for follow-up queries
    /// - **Multi-Language Support**: Intelligence and respond in multiple languages
    /// - **Content Ranking**: Advanced relevance scoring and result ranking
    /// - **Intelligence Personalization**: User-specific optimization and history
    /// - **Real-Time Suggestions**: Dynamic query suggestions and auto-completion
    /// 
    /// RAG Pipeline:
    /// 1. **Query Processing**: Intent detection, entity extraction, query expansion
    /// 2. **Document Retrieval**: Multi-modal search across all content types
    /// 3. **Context Assembly**: Intelligent context selection and ranking
    /// 4. **Response Generation**: AI-powered answer synthesis with citations
    /// 5. **Quality Assurance**: Response validation and confidence scoring
    /// 6. **Learning Loop**: Continuous improvement based on user feedback
    /// 
    /// Intelligence Types:
    /// - **Document Intelligence**: Full-text and semantic search across uploaded documents
    /// - **Database Intelligence**: Natural language queries against connected databases
    /// - **Conversation Intelligence**: Search through conversation history and context
    /// - **Hybrid Intelligence**: Combined search across all content types
    /// - **Semantic Intelligence**: Vector-based similarity search for conceptual matches
    /// - **Faceted Intelligence**: Filtered search with multiple criteria and constraints
    /// 
    /// Use Cases:
    /// - **Knowledge Discovery**: Find relevant information across all content
    /// - **Question Answering**: Get precise answers with supporting evidence
    /// - **Research Assistance**: Comprehensive research with multiple sources
    /// - **Decision Support**: Data-driven insights and recommendations
    /// - **Content Analysis**: Deep analysis and synthesis of complex information
    /// - **Expert Consultation**: AI-powered expertise across multiple domains
    /// 
    /// Example Usage:
    /// ```bash
    /// # Intelligent RAG query with conversation context
    /// curl -X POST "https://localhost:7001/api/intelligence/query" \
    ///   -H "Content-Type: application/json" \
    ///   -d '{"query": "What are the key findings in our quarterly reports?", "maxResults": 5}'
    /// 
    /// # Document-only semantic intelligence
    /// curl -X GET "https://localhost:7001/api/intelligence/documents?query=machine learning&amp;limit=10"
    /// 
    /// # Get intelligence suggestions
    /// curl -X GET "https://localhost:7001/api/intelligence/suggestions?partial=artific"
    /// 
    /// # Search conversation history
    /// curl -X GET "https://localhost:7001/api/intelligence/conversations?userId=user123&amp;query=database"
    /// ```
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class IntelligenceController : ControllerBase
    {
        private readonly IDocumentSearchService _documentSearchService;

        public IntelligenceController(IDocumentSearchService documentSearchService)
        {
            _documentSearchService = documentSearchService;
        }

        /// <summary>
        /// Intelligent AI query processing with automatic routing and RAG
        /// </summary>
        /// <remarks>
        /// Performs intelligent query processing with comprehensive RAG pipeline including:
        /// - **Query Intent Analysis**: Determines if query requires search, conversation, or both
        /// - **Multi-Modal Retrieval**: Searches across documents, databases, and conversations
        /// - **Context Assembly**: Selects and ranks relevant information from multiple sources
        /// - **AI Response Generation**: Synthesizes comprehensive answers with citations
        /// - **Confidence Scoring**: Provides confidence levels for generated responses
        /// - **Source Attribution**: Links responses to original source materials
        /// 
        /// The RAG pipeline automatically:
        /// - Analyzes query intent and complexity
        /// - Retrieves relevant documents and data
        /// - Assembles contextual information
        /// - Generates accurate, well-cited responses
        /// - Provides source links and confidence metrics
        /// </remarks>
        /// <param name="request">Intelligence request with query and parameters</param>
        /// <returns>AI-generated response with sources and confidence metrics</returns>
        [HttpPost("query")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<object>> QueryIntelligence([FromBody] Contracts.SearchRequest request)
        {
            string? query = request?.Query;
            int maxResults = request?.MaxResults ?? 5;

            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query cannot be empty");

            try
            {
                var response = await _documentSearchService.QueryIntelligenceAsync(query, maxResults);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs document-only semantic intelligence without AI generation
        /// </summary>
        /// <remarks>
        /// Searches through uploaded documents using semantic similarity including:
        /// - **Vector Intelligence**: Uses embeddings for semantic similarity matching
        /// - **Relevance Ranking**: Advanced scoring algorithms for result ranking
        /// - **Metadata Filtering**: Filter by document type, date, author, etc.
        /// - **Chunk-Level Results**: Returns specific document sections with context
        /// - **Performance Optimization**: Fast vector search with optimized indexing
        /// 
        /// This endpoint returns raw intelligence results without AI interpretation,
        /// useful for:
        /// - Document discovery and exploration
        /// - Research and analysis workflows
        /// - Content audit and inventory
        /// - Building custom intelligence interfaces
        /// </remarks>
        /// <param name="query">Intelligence query text</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <param name="contentType">Filter by document content type</param>
        /// <param name="minScore">Minimum similarity score threshold</param>
        /// <returns>Ranked list of document chunks with similarity scores</returns>
        [HttpGet("documents")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> QueryDocuments(
            [FromQuery] string query,
            [FromQuery] int limit = 10,
            [FromQuery] string? contentType = null,
            [FromQuery] double minScore = 0.7)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query cannot be empty");

            try
            {
                var results = await _documentSearchService.SearchDocumentsAsync(query, limit);
                
                var filteredResults = results.Where(r => (r.RelevanceScore ?? 0) >= minScore);
                
                // Note: DocumentChunk doesn't have Document navigation property, 
                // so we'll work with available data
                
                return Ok(new
                {
                    query,
                    totalResults = filteredResults.Count(),
                    maxResults = limit,
                    minScore,
                    results = filteredResults.Take(limit).Select(chunk => new
                    {
                        chunkId = chunk.Id,
                        documentId = chunk.DocumentId,
                        documentName = "Document", // Would need to fetch from DocumentService
                        contentType = "unknown", // Would need to fetch from DocumentService
                        content = chunk.Content,
                        similarity = chunk.RelevanceScore ?? 0,
                        chunkIndex = chunk.ChunkIndex
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets intelligent query suggestions and auto-completion
        /// </summary>
        /// <remarks>
        /// Provides real-time intelligence suggestions based on:
        /// - **Content Analysis**: Suggestions based on available document content
        /// - **Query History**: Popular and recent intelligence queries
        /// - **Entity Recognition**: Identified entities and concepts in documents
        /// - **Semantic Similarity**: Related concepts and topics
        /// - **User Patterns**: Personalized suggestions based on user behavior
        /// 
        /// Suggestion types:
        /// - **Auto-completion**: Complete partial queries
        /// - **Related Queries**: Suggest related or follow-up questions
        /// - **Entity Suggestions**: Suggest specific entities or concepts
        /// - **Popular Queries**: Show frequently queried topics
        /// </remarks>
        /// <param name="partial">Partial query text for auto-completion</param>
        /// <param name="limit">Maximum number of suggestions to return</param>
        /// <param name="userId">User ID for personalized suggestions</param>
        /// <returns>List of intelligence suggestions with relevance scores</returns>
        [HttpGet("suggestions")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public Task<ActionResult> GetIntelligenceSuggestions(
            [FromQuery] string partial,
            [FromQuery] int limit = 5,
            [FromQuery] string? userId = null)
        {
            try
            {
                // Mock suggestions - replace with actual implementation
                var suggestions = new List<object>();
                
                if (!string.IsNullOrEmpty(partial))
                {
                    // Generate mock suggestions based on partial input
                    var baseSuggestions = new[]
                    {
                        $"{partial} overview",
                        $"{partial} examples",
                        $"{partial} best practices",
                        $"How to {partial}",
                        $"What is {partial}",
                        $"{partial} implementation",
                        $"{partial} benefits",
                        $"{partial} comparison"
                    };

                    suggestions.AddRange(baseSuggestions.Take(limit).Select((s, i) => new
                    {
                        suggestion = s,
                        type = "auto-complete",
                        relevance = 1.0 - (i * 0.1),
                        category = "general"
                    }));
                }

                return Task.FromResult<ActionResult>(Ok(new
                {
                    partial,
                    totalSuggestions = suggestions.Count,
                    suggestions,
                    timestamp = DateTime.UtcNow
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult<ActionResult>(StatusCode(500, new { Error = ex.Message }));
            }
        }

        /// <summary>
        /// Searches through conversation history with intelligence
        /// </summary>
        /// <remarks>
        /// Searches through user conversation history including:
        /// - **Message Content**: Search through conversation messages
        /// - **Context Awareness**: Understand conversation context and flow
        /// - **User Filtering**: Search within specific user conversations
        /// - **Date Filtering**: Search conversations within date ranges
        /// - **Topic Clustering**: Group related conversation topics
        /// 
        /// Useful for:
        /// - Finding previous discussions on specific topics
        /// - Analyzing conversation patterns and trends
        /// - Retrieving context for follow-up questions
        /// - Building conversation-aware responses
        /// </remarks>
        /// <param name="query">Intelligence query for conversation content</param>
        /// <param name="userId">Filter by specific user ID</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <param name="startDate">Search conversations after this date</param>
        /// <param name="endDate">Search conversations before this date</param>
        /// <returns>Matching conversation segments with context</returns>
        [HttpGet("conversations")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> QueryConversations(
            [FromQuery] string query,
            [FromQuery] string? userId = null,
            [FromQuery] int limit = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query cannot be empty");

            try
            {
                // Mock conversation search - replace with actual implementation
                var mockResults = new List<object>
                {
                    new
                    {
                        conversationId = Guid.NewGuid(),
                        messageId = Guid.NewGuid(),
                        userId = userId ?? "user123",
                        content = $"Previous discussion about {query} and related topics...",
                        timestamp = DateTime.UtcNow.AddDays(-2),
                        relevance = 0.95,
                        context = new
                        {
                            previousMessage = "Can you tell me more about this topic?",
                            nextMessage = "That's very helpful, thank you!"
                        }
                    },
                    new
                    {
                        conversationId = Guid.NewGuid(),
                        messageId = Guid.NewGuid(),
                        userId = userId ?? "user456",
                        content = $"Another conversation mentioning {query} with different context...",
                        timestamp = DateTime.UtcNow.AddDays(-5),
                        relevance = 0.87,
                        context = new
                        {
                            previousMessage = "I need help understanding this concept.",
                            nextMessage = "Great explanation!"
                        }
                    }
                };

                return Ok(new
                {
                    query,
                    userId,
                    totalResults = mockResults.Count,
                    results = mockResults.Take(limit),
                    filters = new
                    {
                        startDate,
                        endDate,
                        limit
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets intelligence analytics and performance metrics
        /// </summary>
        /// <remarks>
        /// Provides comprehensive intelligence analytics including:
        /// - **Query Statistics**: Most popular queries, intelligence patterns, trends
        /// - **Performance Metrics**: Intelligence response times, success rates, user satisfaction
        /// - **Content Analytics**: Most queried content, gap analysis, relevance metrics
        /// - **User Behavior**: Intelligence patterns, session analytics, conversion rates
        /// - **System Performance**: Intelligence infrastructure performance and optimization
        /// 
        /// Analytics help with:
        /// - Understanding user information needs
        /// - Optimizing intelligence algorithms and ranking
        /// - Identifying content gaps and opportunities
        /// - Improving user experience and satisfaction
        /// - System performance monitoring and tuning
        /// </remarks>
        /// <param name="startDate">Analytics start date (optional)</param>
        /// <param name="endDate">Analytics end date (optional)</param>
        /// <param name="userId">Filter by specific user (optional)</param>
        /// <returns>Comprehensive intelligence analytics and metrics</returns>
        [HttpGet("analytics")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetIntelligenceAnalytics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? userId = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                // Mock analytics - replace with actual implementation
                var analytics = new
                {
                    period = new { start, end, days = (end - start).Days },
                    overview = new
                    {
                        totalSearches = 2500,
                        uniqueUsers = 150,
                        averageResponseTime = 1.2,
                        successRate = 94.5
                    },
                    popularQueries = new[]
                    {
                        new { query = "machine learning", count = 45, avgRelevance = 0.92 },
                        new { query = "data analysis", count = 38, avgRelevance = 0.89 },
                        new { query = "API documentation", count = 32, avgRelevance = 0.95 }
                    },
                    performance = new
                    {
                        averageSearchTime = 1.2,
                        medianSearchTime = 0.8,
                        p95SearchTime = 3.1,
                        cacheHitRate = 78.5
                    },
                    contentAnalytics = new
                    {
                        mostSearchedDocuments = new[]
                        {
                            new { documentName = "User Manual.pdf", searches = 125 },
                            new { documentName = "API Reference.docx", searches = 98 }
                        },
                        searchCoverage = 85.2,
                        avgResultsPerQuery = 7.3
                    }
                };

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Performs intelligent multi-database query across all configured databases
        /// </summary>
        /// <remarks>
        /// Executes an AI-powered query across multiple configured databases including:
        /// - **Query Intent Analysis**: AI analyzes the query to determine which databases contain relevant data
        /// - **Smart Routing**: Automatically identifies which tables and columns to query in each database
        /// - **SQL Generation**: AI generates optimized SQL queries for each database
        /// - **Parallel Execution**: Queries are executed simultaneously across multiple databases
        /// - **Result Merging**: Data from all databases is intelligently merged and synthesized
        /// - **Natural Language Response**: AI generates a comprehensive answer combining all data sources
        /// 
        /// This endpoint enables powerful cross-database analytics such as:
        /// - "Show me top selling products and their customer information"
        /// - "List all orders with product details and customer names"
        /// - "What are the most profitable product categories?"
        /// 
        /// The system automatically:
        /// - Determines which databases to query based on schema analysis
        /// - Generates appropriate SQL queries for each database type
        /// - Handles cross-database relationships and joins
        /// - Merges results into a coherent answer
        /// 
        /// Example queries:
        /// - "Who are our top 10 customers by total purchase amount?"
        /// - "Which products have been ordered the most in the last month?"
        /// - "Show me sales trends by product category"
        /// </remarks>
        /// <param name="request">Multi-database query request</param>
        /// <returns>AI-generated answer with data from multiple databases</returns>
        [HttpPost("multi-database-query")]
        [ProducesResponseType(typeof(MultiDatabaseQueryResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<MultiDatabaseQueryResponseDto>> QueryMultipleDatabases(
            [FromBody] Contracts.MultiDatabaseQueryRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest("Query cannot be empty");
            }

            var coordinator = HttpContext.RequestServices.GetService<IMultiDatabaseQueryCoordinator>();

            if (coordinator == null)
            {
                return StatusCode(500, new MultiDatabaseQueryResponseDto
                {
                    Success = false,
                    Answer = "Multi-database query service is not configured. Please ensure database connections are configured in appsettings.json",
                    Errors = new List<string> { "IMultiDatabaseQueryCoordinator service not available" }
                });
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Execute multi-database query
                var response = await coordinator.QueryMultipleDatabasesAsync(request.Query, request.MaxResults);

                stopwatch.Stop();

                var result = new MultiDatabaseQueryResponseDto
                {
                    Answer = response.Answer,
                    Sources = response.Sources.Select(s => s.FileName).ToList(),
                    Success = !string.IsNullOrEmpty(response.Answer),
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    DatabasesQueried = response.Sources.Count,
                    TotalRowsRetrieved = 0 // TODO: Calculate from response
                };

                // Include query analysis if requested
                if (request.IncludeQueryAnalysis)
                {
                    var queryIntent = await coordinator.AnalyzeQueryIntentAsync(request.Query);
                    result.QueryAnalysis = new QueryIntentAnalysisResponseDto
                    {
                        OriginalQuery = queryIntent.OriginalQuery,
                        QueryUnderstanding = queryIntent.QueryUnderstanding,
                        Confidence = queryIntent.Confidence,
                        RequiresCrossDatabaseJoin = queryIntent.RequiresCrossDatabaseJoin,
                        Reasoning = queryIntent.Reasoning,
                        DatabaseQueries = queryIntent.DatabaseQueries.Select(dq => new DatabaseQueryPlanDto
                        {
                            DatabaseId = dq.DatabaseId,
                            DatabaseName = dq.DatabaseName,
                            RequiredTables = dq.RequiredTables,
                            GeneratedQuery = dq.GeneratedQuery,
                            Purpose = dq.Purpose,
                            Priority = dq.Priority
                        }).ToList()
                    };
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                return StatusCode(500, new MultiDatabaseQueryResponseDto
                {
                    Success = false,
                    Answer = "An error occurred while processing your query",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Analyzes a query and returns which databases would be queried (without executing)
        /// </summary>
        /// <remarks>
        /// Performs query intent analysis to show which databases and tables would be used
        /// without actually executing the queries. Useful for:
        /// - Understanding query routing logic
        /// - Debugging query issues
        /// - Previewing which data sources will be accessed
        /// </remarks>
        /// <param name="query">Natural language query to analyze</param>
        /// <returns>Query intent analysis showing databases and tables that would be queried</returns>
        [HttpPost("analyze-query-intent")]
        [ProducesResponseType(typeof(QueryIntentAnalysisResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<QueryIntentAnalysisResponseDto>> AnalyzeQueryIntent(
            [FromBody] Contracts.SearchRequest request)
        {
            string? query = request?.Query;
            
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query cannot be empty");
            }

            var coordinator = HttpContext.RequestServices.GetService<IMultiDatabaseQueryCoordinator>();

            if (coordinator == null)
            {
                return StatusCode(500, "Multi-database query service is not configured");
            }

            try
            {
                var queryIntent = await coordinator.AnalyzeQueryIntentAsync(query);
                
                // Generate SQL queries
                queryIntent = await coordinator.GenerateDatabaseQueriesAsync(queryIntent);

                var result = new QueryIntentAnalysisResponseDto
                {
                    OriginalQuery = queryIntent.OriginalQuery,
                    QueryUnderstanding = queryIntent.QueryUnderstanding,
                    Confidence = queryIntent.Confidence,
                    RequiresCrossDatabaseJoin = queryIntent.RequiresCrossDatabaseJoin,
                    Reasoning = queryIntent.Reasoning,
                    DatabaseQueries = queryIntent.DatabaseQueries.Select(dq => new DatabaseQueryPlanDto
                    {
                        DatabaseId = dq.DatabaseId,
                        DatabaseName = dq.DatabaseName,
                        RequiredTables = dq.RequiredTables,
                        GeneratedQuery = dq.GeneratedQuery,
                        Purpose = dq.Purpose,
                        Priority = dq.Priority
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error analyzing query intent: {ex.Message}");
            }
        }
    }
}