#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Enums;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Search;
using SmartRAG.Interfaces.Support;
using SmartRAG.Interfaces.AI;
using SmartRAG.Services.Shared;
using SmartRAG.Models;
using SmartRAG.Models.RequestResponse;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Mcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SmartRAG.Helpers;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for document search and RAG (Retrieval-Augmented Generation) operations
    /// </summary>
    public class DocumentSearchService : IDocumentSearchService, IRagAnswerGeneratorService
    {
        private const int InitialSearchMultiplier = 2;
        private const int MinSearchResultsCount = 0;
        private const int FallbackSearchMaxResults = 10;
        private const int MinSubstantialContentLength = 50;
        private const int MaxExpandedChunks = 120; // Balanced limit for comprehensive context without overwhelming the AI
        private const int MaxContextSize = 18000;
        // Threshold for text search scores (typically 4.0-6.0 range)
        // For vector search, scores are typically 0.0-1.0, so we use a lower threshold
        // Balanced threshold to include relevant chunks while filtering out noise
        private const double DocumentBoostThreshold = 4.5; // Increased to filter out irrelevant chunks and ensure only highly relevant chunks are used for context expansion
        // Expanded chunks are context-only and should not outrank original search results
        // Expanded chunks use simple word match scoring (0.1 per match) instead of DocumentScoringService
        // This ensures they always rank lower than original search results
        
        // Database query confidence thresholds
        private const double DatabaseQueryRequiredThreshold = 0.7; // Confidence >= 0.7 means query requires database, no early exit allowed

        // Constants still used in PerformBasicSearchAsync (fallback method)
        private const int MinNameChunksCount = 0;
        private const int MinPotentialNamesCount = 2;
        private const int MinWordCountThreshold = 0;
        private const int TopChunksPerDocument = 5;
        private const int ChunksToCheckForKeywords = 30;
        private const double DocumentScoreThreshold = 0.8;
        private const double NumberedListBonusPerItem = 100.0;
        private const double NumberedListWordMatchBonus = 10.0;
        private const double DocumentRelevanceBoost = 200.0;

        // Query intent confidence thresholds for early exit decision
        private const double VeryHighDatabaseConfidenceThreshold = 0.98; // Always check database if query intent confidence >= 0.98 (very strict, only for clear database-only queries)
        private const double HighDatabaseConfidenceThreshold = 0.9; // Check database if query intent confidence >= 0.9 and document search has low confidence
        private const double SkipEagerDocumentAnswerConfidenceThreshold = 0.85; // Skip eager document answer generation if confidence >= 0.85 and database queries exist (saves 1 LLM call)
        private const double StrongDocumentMatchThreshold = 4.8; // If document chunks have score > 4.8, prioritize them over database queries even if DB intent confidence is high

        // Regex patterns for parsing source tags from query
        // Pattern matches: whitespace or punctuation + tag + optional whitespace at end
        // This handles cases like "query? -d", "query! -d", "query -d", etc.
        private const string DocumentTagPattern = @"\s*-d\s*$";
        private const string DatabaseTagPattern = @"\s*-db\s*$";
        private const string McpTagPattern = @"\s*-mcp\s*$";
        private const string AudioTagPattern = @"\s*-a\s*$";
        private const string ImageTagPattern = @"\s*-i\s*$";
        private const RegexOptions TagRegexOptions = RegexOptions.IgnoreCase;

        private readonly IDocumentRepository _documentRepository;
        private readonly IAIService _aiService;
        private readonly IAIProviderFactory _aiProviderFactory;
        private readonly SmartRagOptions _options;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentSearchService> _logger;
        private readonly IMultiDatabaseQueryCoordinator? _multiDatabaseQueryCoordinator;
        private readonly IQueryIntentAnalyzer? _queryIntentAnalyzer;
        private readonly IConversationManagerService _conversationManager;
        private readonly IQueryIntentClassifierService _queryIntentClassifier;
        private readonly IPromptBuilderService _promptBuilder;
        private readonly IDocumentScoringService _documentScoring;
        private readonly ISourceBuilderService _sourceBuilder;
        private readonly IAIConfigurationService _aiConfiguration;
        private readonly IContextExpansionService? _contextExpansion;
        private readonly IMcpIntegrationService? _mcpIntegration;
        private readonly IDocumentRelevanceCalculatorService _relevanceCalculator;
        private readonly IQueryWordMatcherService _queryWordMatcher;
        private readonly IQueryPatternAnalyzerService _queryPatternAnalyzer;
        private readonly IChunkPrioritizerService _chunkPrioritizer;
        private readonly IDocumentService _documentService;
        private readonly IQueryAnalysisService? _queryAnalysis;
        private readonly IResponseBuilderService? _responseBuilder;
        private readonly IQueryStrategyOrchestratorService? _strategyOrchestrator;
        private readonly IQueryStrategyExecutorService? _strategyExecutor;
        private readonly IDocumentSearchStrategyService? _documentSearchStrategy;
        private readonly ISourceSelectionService? _sourceSelectionService;

        /// <summary>
        /// Initializes a new instance of the DocumentSearchService
        /// </summary>
        /// <param name="documentRepository">Repository for document operations</param>
        /// <param name="aiService">AI service for text generation</param>
        /// <param name="aiProviderFactory">Factory for AI provider creation</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="options">SmartRAG configuration options</param>
        /// <param name="logger">Logger instance for this service</param>
        /// <param name="multiDatabaseQueryCoordinator">Optional multi-database query coordinator for database queries</param>
        /// <param name="queryIntentAnalyzer">Service for analyzing database query intent</param>
        /// <param name="conversationManager">Service for managing conversation sessions and history</param>
        /// <param name="queryIntentClassifier">Service for classifying query intent</param>
        /// <param name="promptBuilder">Service for building AI prompts</param>
        /// <param name="documentScoring">Service for scoring document chunks</param>
        /// <param name="sourceBuilder">Service for building search sources</param>
        /// <param name="aiConfiguration">Service for AI provider configuration</param>
        /// <param name="contextExpansion">Service for expanding chunk context with adjacent chunks</param>
        /// <param name="mcpIntegration">Service for integrating MCP server results</param>
        /// <param name="relevanceCalculator">Service for calculating document-level relevance scores</param>
        /// <param name="queryWordMatcher">Service for query word matching operations</param>
        /// <param name="queryPatternAnalyzer">Service for analyzing query patterns and detecting numbered lists</param>
        /// <param name="chunkPrioritizer">Service for prioritizing chunks</param>
        /// <param name="documentService">Service for document operations and filtering</param>
        /// <param name="queryAnalysis">Service for analyzing queries and determining search parameters</param>
        /// <param name="responseBuilder">Service for building RAG responses</param>
        /// <param name="strategyOrchestrator">Service for determining query execution strategy</param>
        /// <param name="strategyExecutor">Service for executing query strategies</param>
        /// <param name="documentSearchStrategy">Service for executing document search strategies</param>
        /// <param name="sourceSelectionService">Service for determining if other sources should be skipped</param>
        public DocumentSearchService(
            IDocumentRepository documentRepository,
            IAIService aiService,
            IAIProviderFactory aiProviderFactory,
            IConfiguration configuration,
            IOptions<SmartRagOptions> options,
            ILogger<DocumentSearchService> logger,
            IMultiDatabaseQueryCoordinator? multiDatabaseQueryCoordinator = null,
            IQueryIntentAnalyzer? queryIntentAnalyzer = null,
            IConversationManagerService? conversationManager = null,
            IQueryIntentClassifierService? queryIntentClassifier = null,
            IPromptBuilderService? promptBuilder = null,
            IDocumentScoringService? documentScoring = null,
            ISourceBuilderService? sourceBuilder = null,
            IAIConfigurationService? aiConfiguration = null,
            IContextExpansionService? contextExpansion = null,
            IMcpIntegrationService? mcpIntegration = null,
            IDocumentRelevanceCalculatorService? relevanceCalculator = null,
            IQueryWordMatcherService? queryWordMatcher = null,
            IQueryPatternAnalyzerService? queryPatternAnalyzer = null,
            IChunkPrioritizerService? chunkPrioritizer = null,
            IDocumentService? documentService = null,
            IQueryAnalysisService? queryAnalysis = null,
            IResponseBuilderService? responseBuilder = null,
            IQueryStrategyOrchestratorService? strategyOrchestrator = null,
            IQueryStrategyExecutorService? strategyExecutor = null,
            IDocumentSearchStrategyService? documentSearchStrategy = null,
            ISourceSelectionService? sourceSelectionService = null)
        {
            _documentRepository = documentRepository;
            _aiService = aiService;
            _aiProviderFactory = aiProviderFactory;
            _configuration = configuration;
            _options = options.Value;
            _logger = logger;
            _multiDatabaseQueryCoordinator = multiDatabaseQueryCoordinator;
            _queryIntentAnalyzer = queryIntentAnalyzer;
            _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
            _queryIntentClassifier = queryIntentClassifier ?? throw new ArgumentNullException(nameof(queryIntentClassifier));
            _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
            _documentScoring = documentScoring ?? throw new ArgumentNullException(nameof(documentScoring));
            _sourceBuilder = sourceBuilder ?? throw new ArgumentNullException(nameof(sourceBuilder));
            _aiConfiguration = aiConfiguration ?? throw new ArgumentNullException(nameof(aiConfiguration));
            _contextExpansion = contextExpansion;
            _mcpIntegration = mcpIntegration;
            _relevanceCalculator = relevanceCalculator ?? throw new ArgumentNullException(nameof(relevanceCalculator));
            _queryWordMatcher = queryWordMatcher ?? throw new ArgumentNullException(nameof(queryWordMatcher));
            _queryPatternAnalyzer = queryPatternAnalyzer ?? throw new ArgumentNullException(nameof(queryPatternAnalyzer));
            _chunkPrioritizer = chunkPrioritizer ?? throw new ArgumentNullException(nameof(chunkPrioritizer));
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
            _queryAnalysis = queryAnalysis;
            _responseBuilder = responseBuilder;
            _strategyOrchestrator = strategyOrchestrator;
            _strategyExecutor = strategyExecutor;
            _documentSearchStrategy = documentSearchStrategy;
            _sourceSelectionService = sourceSelectionService;
        }

        /// <summary>
        /// [Document Query] Searches for relevant document chunks based on the query
        /// </summary>
        /// <param name="query">Search query string</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <param name="options">Optional search options to override global configuration</param>
        /// <param name="queryTokens">Pre-computed query tokens (optional, for performance)</param>
        /// <returns>List of relevant document chunks</returns>
        public async Task<List<DocumentChunk>> SearchDocumentsAsync(
            string query,
            int maxResults = 5,
            SearchOptions? options = null,
            List<string>? queryTokens = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            var searchResults = _documentSearchStrategy != null
                ? await _documentSearchStrategy.SearchDocumentsAsync(query, maxResults * InitialSearchMultiplier, options, queryTokens)
                : await PerformBasicSearchAsync(query, maxResults * InitialSearchMultiplier, options, queryTokens);
            
            // This ensures image chunks and other high-scoring chunks are selected first
            return searchResults
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .ThenBy(c => c.ChunkIndex)
                .Take(maxResults)
                .ToList();
        }

        /// <summary>
        /// [AI Query] Process intelligent query with RAG and automatic session management
        /// Unified approach: searches across documents, images, audio, and databases
        /// </summary>
        /// <param name="query">User query to process</param>
        /// <param name="maxResults">Maximum number of document chunks to use</param>
        /// <param name="startNewConversation">Whether to start a new conversation session</param>
        /// <param name="options">Optional search options to override global configuration</param>
        /// <returns>RAG response with answer and sources from all available data sources</returns>
        public async Task<RagResponse> QueryIntelligenceAsync(
            string query,
            int maxResults = 5,
            bool startNewConversation = false,
            SearchOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            // This prevents processing invalid slash commands as regular queries
            var trimmedQuery = query.Trim();
            if (trimmedQuery.StartsWith("/", StringComparison.Ordinal))
            {
                if (!_queryIntentClassifier.TryParseCommand(trimmedQuery, out var parsedCommandType, out var _))
                {
                    _logger.LogDebug("Skipping unknown slash command");
                    return new RagResponse
                    {
                        Answer = string.Empty,
                        Sources = new List<SearchSource>(),
                        Query = trimmedQuery
                    };
                }
            }

            var searchOptions = options ?? SearchOptions.FromConfig(_options);

            // Only parse tags if options were not provided (to avoid double parsing)
            if (options == null)
            {
                var (cleanedQuery, adjustedOptions) = ParseSourceTags(query, searchOptions);
                searchOptions = adjustedOptions;
                query = cleanedQuery;
            }

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty after removing tags", nameof(query));

            var preferredLanguage = searchOptions.PreferredLanguage;

            var originalQuery = query;
            var hasCommand = _queryIntentClassifier.TryParseCommand(query, out var commandType, out var commandPayload);

            if (hasCommand && commandType == QueryCommandType.ForceConversation)
            {
                query = string.IsNullOrWhiteSpace(commandPayload)
                    ? string.Empty
                    : commandPayload;
            }

            if (startNewConversation || (hasCommand && commandType == QueryCommandType.NewConversation))
            {
                await _conversationManager.StartNewConversationAsync();
                return _responseBuilder?.CreateRagResponse(query, "New conversation started. How can I help you?", new List<SearchSource>()) ?? new RagResponse { Query = query, Answer = "New conversation started. How can I help you?", Sources = new List<SearchSource>(), SearchedAt = DateTime.UtcNow };
            }

            var sessionId = await _conversationManager.GetOrCreateSessionIdAsync();

            var conversationHistory = await _conversationManager.GetConversationHistoryAsync(sessionId);

            if ((hasCommand && commandType == QueryCommandType.ForceConversation) || await _queryIntentClassifier.IsGeneralConversationAsync(query, conversationHistory))
            {
                var conversationQuery = string.IsNullOrWhiteSpace(query)
                    ? originalQuery
                    : query;

                var conversationAnswer = await _conversationManager.HandleGeneralConversationAsync(conversationQuery, conversationHistory, preferredLanguage);

                await _conversationManager.AddToConversationAsync(sessionId, conversationQuery, conversationAnswer);

                return _responseBuilder?.CreateRagResponse(conversationQuery, conversationAnswer, new List<SearchSource>()) ?? new RagResponse { Query = conversationQuery, Answer = conversationAnswer, Sources = new List<SearchSource>(), SearchedAt = DateTime.UtcNow };
            }

            RagResponse response;
            var searchMetadata = new SearchMetadata();

            // Pre-evaluate document availability for smarter strategy selection
            // Compute query tokens once here and pass to all sub-methods to avoid redundant tokenization
            var queryTokens = searchOptions.EnableDocumentSearch ? QueryTokenizer.TokenizeQuery(query) : null;

            var documentSearchTask = searchOptions.EnableDocumentSearch
                ? CanAnswerFromDocumentsAsyncInternal(query, searchOptions, queryTokens)
                : Task.FromResult((CanAnswer: false, Results: new List<DocumentChunk>()));
            
            var queryIntentTask = (_multiDatabaseQueryCoordinator != null && 
                                   searchOptions.EnableDatabaseSearch && 
                                   _queryIntentAnalyzer != null)
                ? _queryIntentAnalyzer.AnalyzeQueryIntentAsync(query)
                : Task.FromResult<QueryIntent?>(null);

            await Task.WhenAll(documentSearchTask, queryIntentTask);

            var (CanAnswer, Results) = await documentSearchTask;
            var preAnalyzedQueryIntent = await queryIntentTask;

            if (searchOptions.EnableDocumentSearch)
            {
                searchMetadata.DocumentSearchPerformed = true;
                searchMetadata.DocumentChunksFound = Results.Count;
            }

            QueryIntent? earlyExitQueryIntent = preAnalyzedQueryIntent;
            
            // Only skip eager document answer if intent strongly suggests a database query 
            // AND we don't have extremely high-scoring document chunks.
            // If we have high-scoring document chunks, prioritize them over database queries
            // to avoid false positive database intent detection when documents contain the answer.
            var topScore = Results.Count > 0 ? Results.Max(r => r.RelevanceScore ?? 0) : 0;
            var hasStrongDocumentMatch = topScore > StrongDocumentMatchThreshold; 

            var skipEagerDocumentAnswer = !hasStrongDocumentMatch &&
                                          earlyExitQueryIntent?.Confidence > SkipEagerDocumentAnswerConfidenceThreshold && 
                                          earlyExitQueryIntent.DatabaseQueries?.Count > 0;

            if (searchOptions.EnableDocumentSearch && CanAnswer && Results.Count > 0 && !skipEagerDocumentAnswer)
            {
                _logger.LogInformation("Document chunks found ({Count}), generating document-first response", Results.Count);
                if (_strategyExecutor == null)
                {
                    throw new InvalidOperationException("IQueryStrategyExecutorService is required for strategy execution");
                }
                
                var docRequest = new Models.RequestResponse.DocumentQueryStrategyRequest
                {
                    Query = query,
                    MaxResults = maxResults,
                    ConversationHistory = conversationHistory,
                    CanAnswerFromDocuments = CanAnswer,
                    PreferredLanguage = preferredLanguage,
                    Options = searchOptions,
                    PreCalculatedResults = Results,
                    QueryTokens = queryTokens
                };
                var documentOnlyResponse = await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(docRequest);
                
                // Use StrongDocumentMatchThreshold for consistency (4.8)
                // If topScore is very high, we trust the document search result unless it's a complete failure ([NO_ANSWER_FOUND])
                // This prevents fallback to DB for questions that are clearly document-centric and have high-confidence matches
                
                if (topScore >= StrongDocumentMatchThreshold && 
                    !string.IsNullOrWhiteSpace(documentOnlyResponse.Answer))
                {
                    // Check if answer is explicitly negative (meaning NO answer was found)
                    // We only block if it contains the specific failure token, otherwise we accept even partial answers
                    var isFailure = _responseBuilder != null && _responseBuilder.IsExplicitlyNegative(documentOnlyResponse.Answer);
                    
                    if (!isFailure)
                    {
                        _logger.LogInformation("High-confidence chunks found (score: {Score:F2} >= {Threshold:F2}) and answer is not a failure, accepting document response. Skip DB.", 
                            topScore, StrongDocumentMatchThreshold);
                        documentOnlyResponse.SearchMetadata = searchMetadata;
                        return documentOnlyResponse;
                    }
                    else
                    {
                         _logger.LogInformation("High-confidence chunks found but AI explicitly returned [NO_ANSWER_FOUND]. Allowing fallback to DB.");
                    }
                }

                if (_responseBuilder != null && 
                    !_responseBuilder.IndicatesMissingData(documentOnlyResponse.Answer, query))
                {
                    _logger.LogInformation("Document response is sufficient, skipping database and MCP search");
                    documentOnlyResponse.SearchMetadata = searchMetadata;
                    return documentOnlyResponse;
                }

                if (searchOptions.EnableDatabaseSearch)
                {
                    _logger.LogInformation("Document response indicates missing data, continuing to database search as fallback");
                }
                else
                {
                    _logger.LogInformation("Document response may be insufficient but database search is disabled, returning document response");
                    documentOnlyResponse.SearchMetadata = searchMetadata;
                    return documentOnlyResponse;
                }
            }

            if (_multiDatabaseQueryCoordinator != null && searchOptions.EnableDatabaseSearch)
            {
                try
                {
                    // Use pre-analyzed query intent from parallel execution
                    // This was already analyzed in parallel with document search above
                    QueryIntent? queryIntent = earlyExitQueryIntent;
                    if (queryIntent == null && _queryIntentAnalyzer != null)
                    {
                        _logger.LogDebug("Query intent not pre-analyzed, analyzing now");
                        queryIntent = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(query);
                    }

                    var hasDatabaseQueries = queryIntent?.DatabaseQueries != null && queryIntent.DatabaseQueries.Count > 0;
                    var confidence = queryIntent?.Confidence ?? 0.0;

                    // Determine query strategy using enum
                    var strategy = _strategyOrchestrator?.DetermineQueryStrategy(confidence, hasDatabaseQueries, CanAnswer) ?? QueryStrategy.DocumentOnly;

                    // Execute strategy using switch-case (Open/Closed Principle)
                    // Pass pre-analyzed queryIntent (may be null) and preferredLanguage to avoid redundant AI calls
                    if (_strategyExecutor == null)
                    {
                        throw new InvalidOperationException("IQueryStrategyExecutorService is required for strategy execution");
                    }

                    response = strategy switch
                    {
                        QueryStrategy.DatabaseOnly => await _strategyExecutor.ExecuteDatabaseOnlyStrategyAsync(new Models.RequestResponse.DatabaseQueryStrategyRequest
                        {
                            Query = query,
                            MaxResults = maxResults,
                            ConversationHistory = conversationHistory,
                            CanAnswerFromDocuments = CanAnswer,
                            QueryIntent = queryIntent,
                            PreferredLanguage = preferredLanguage,
                            Options = searchOptions,
                            QueryTokens = queryTokens
                        }),
                        QueryStrategy.DocumentOnly => await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(new Models.RequestResponse.DocumentQueryStrategyRequest
                        {
                            Query = query,
                            MaxResults = maxResults,
                            ConversationHistory = conversationHistory,
                            CanAnswerFromDocuments = CanAnswer,
                            PreferredLanguage = preferredLanguage,
                            Options = searchOptions,
                            PreCalculatedResults = Results,
                            QueryTokens = queryTokens
                        }),
                        QueryStrategy.Hybrid => await _strategyExecutor.ExecuteHybridStrategyAsync(new Models.RequestResponse.HybridQueryStrategyRequest
                        {
                            Query = query,
                            MaxResults = maxResults,
                            ConversationHistory = conversationHistory,
                            HasDatabaseQueries = hasDatabaseQueries,
                            CanAnswerFromDocuments = CanAnswer,
                            QueryIntent = queryIntent,
                            PreferredLanguage = preferredLanguage,
                            Options = searchOptions,
                            PreCalculatedResults = Results,
                            QueryTokens = queryTokens
                        }),
                        _ => await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(new Models.RequestResponse.DocumentQueryStrategyRequest
                        {
                            Query = query,
                            MaxResults = maxResults,
                            ConversationHistory = conversationHistory,
                            CanAnswerFromDocuments = CanAnswer,
                            PreferredLanguage = preferredLanguage,
                            Options = searchOptions,
                            PreCalculatedResults = Results,
                            QueryTokens = queryTokens
                        })
                    };

                    if (strategy == QueryStrategy.DatabaseOnly || strategy == QueryStrategy.Hybrid)
                    {
                        searchMetadata.DatabaseSearchPerformed = true;
                        searchMetadata.DatabaseResultsFound = response.Sources?.Count(s => s.SourceType == "Database") ?? 0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during query intent analysis, falling back to document-only query");
                    var docRequest = new Models.RequestResponse.DocumentQueryStrategyRequest
                    {
                        Query = query,
                        MaxResults = maxResults,
                        ConversationHistory = conversationHistory,
                        CanAnswerFromDocuments = CanAnswer,
                        PreferredLanguage = preferredLanguage,
                        Options = searchOptions,
                        PreCalculatedResults = Results,
                        QueryTokens = queryTokens
                    };
                    response = _strategyExecutor != null
                        ? await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(docRequest)
                        : throw new InvalidOperationException("IQueryStrategyExecutorService is required for strategy execution");
                }

                if (response != null && response.SearchMetadata == null)
                {
                    response.SearchMetadata = searchMetadata;
                }
                
                // CASCADE: Check if database response is sufficient
                // If database response also indicates missing data, allow MCP fallback
                if (response != null && 
                    _responseBuilder != null && 
                    _responseBuilder.IndicatesMissingData(response.Answer, query))
                {
                    _logger.LogInformation("Database response indicates missing data, cascade to MCP search");
                }
            }
            else
            {
                if (searchOptions.EnableDocumentSearch)
                {
                    var docRequest = new Models.RequestResponse.DocumentQueryStrategyRequest
                    {
                        Query = query,
                        MaxResults = maxResults,
                        ConversationHistory = conversationHistory,
                        CanAnswerFromDocuments = CanAnswer,
                        PreferredLanguage = preferredLanguage,
                        Options = searchOptions,
                        PreCalculatedResults = Results,
                        QueryTokens = queryTokens
                    };
                    response = _strategyExecutor != null
                        ? await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(docRequest)
                        : throw new InvalidOperationException("IQueryStrategyExecutorService is required for strategy execution");
                    
                    if (response != null && response.SearchMetadata == null)
                    {
                        response.SearchMetadata = searchMetadata;
                    }
                }
                else
                {
                    if (_mcpIntegration != null && _options.Features.EnableMcpSearch && searchOptions.EnableMcpSearch)
                    {
                        try
                        {
                                var mcpResults = await _mcpIntegration.QueryWithMcpAsync(query, maxResults, conversationHistory);
                            searchMetadata.McpSearchPerformed = true;
                            searchMetadata.McpResultsFound = mcpResults?.Count(r => r.IsSuccess && !string.IsNullOrWhiteSpace(r.Content)) ?? 0;
                            
                            if (mcpResults != null && mcpResults.Count > 0)
                            {
                                var mcpSources = mcpResults
                                    .Where(r => r.IsSuccess && !string.IsNullOrWhiteSpace(r.Content))
                                    .Select(r => new SearchSource
                                    {
                                        SourceType = "MCP",
                                        FileName = $"{r.ServerId}:{r.ToolName}",
                                        RelevantContent = r.Content,
                                        RelevanceScore = 1.0
                                    })
                                    .ToList();

                                if (mcpSources.Count > 0)
                                {
                                    var mcpContext = string.Join("\n\n", mcpResults.Where(r => r.IsSuccess).Select(r => r.Content));
                                    if (!string.IsNullOrWhiteSpace(mcpContext))
                                    {
                                        var mcpPrompt = _promptBuilder.BuildDocumentRagPrompt(query, mcpContext, conversationHistory, preferredLanguage);
                                        var mcpAnswer = await _aiService.GenerateResponseAsync(mcpPrompt, new List<string> { mcpContext });
                                        if (!string.IsNullOrWhiteSpace(mcpAnswer))
                                        {
                                            response = _responseBuilder?.CreateRagResponse(query, mcpAnswer, mcpSources, searchMetadata) ?? new RagResponse { Query = query, Answer = mcpAnswer, Sources = mcpSources, SearchedAt = DateTime.UtcNow, SearchMetadata = searchMetadata };
                                        }
                                        else
                                        {
                                            _logger.LogInformation("MCP query returned results but AI generated empty response. Falling back to general conversation.");
                                            var chatResponse = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
                                            response = _responseBuilder?.CreateRagResponse(query, chatResponse, mcpSources) ?? new RagResponse { Query = query, Answer = chatResponse, Sources = mcpSources, SearchedAt = DateTime.UtcNow };
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogInformation("MCP query returned empty context. Falling back to general conversation.");
                                        var chatResponse = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
                                        response = _responseBuilder?.CreateRagResponse(query, chatResponse, new List<SearchSource>()) ?? new RagResponse { Query = query, Answer = chatResponse, Sources = new List<SearchSource>(), SearchedAt = DateTime.UtcNow };
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation("MCP query returned no valid results. Falling back to general conversation.");
                                    var chatResponse = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
                                    response = _responseBuilder?.CreateRagResponse(query, chatResponse, new List<SearchSource>()) ?? new RagResponse { Query = query, Answer = chatResponse, Sources = new List<SearchSource>(), SearchedAt = DateTime.UtcNow };
                                }
                            }
                            else
                            {
                                _logger.LogInformation("MCP query returned no results. Falling back to general conversation.");
                                var chatResponse = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
                                response = _responseBuilder?.CreateRagResponse(query, chatResponse, new List<SearchSource>()) ?? new RagResponse { Query = query, Answer = chatResponse, Sources = new List<SearchSource>(), SearchedAt = DateTime.UtcNow };
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error querying MCP servers, falling back to general conversation");
                            var chatResponse = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
                            response = _responseBuilder?.CreateRagResponse(query, chatResponse, new List<SearchSource>()) ?? new RagResponse { Query = query, Answer = chatResponse, Sources = new List<SearchSource>(), SearchedAt = DateTime.UtcNow };
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Both database and document search disabled. Falling back to general conversation.");
                        var chatResponse = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
                        response = _responseBuilder?.CreateRagResponse(query, chatResponse, new List<SearchSource>()) ?? new RagResponse { Query = query, Answer = chatResponse, Sources = new List<SearchSource>(), SearchedAt = DateTime.UtcNow };
                    }
                }
            }

            // Skip MCP search if database response has meaningful data AND answer is sufficient
            // CASCADE: If database response indicates missing data, proceed to MCP search
            var hasMeaningfulDatabaseData = response != null && 
                (response.Sources?.Any(s => s.SourceType == "Database") ?? false) &&
                (!string.IsNullOrWhiteSpace(response.Answer) || (response.Sources?.Any(s => s.SourceType == "Database" && !string.IsNullOrWhiteSpace(s.RelevantContent)) ?? false));
            
            var databaseAnswerIsSufficient = hasMeaningfulDatabaseData && 
                _responseBuilder != null && 
                !_responseBuilder.IndicatesMissingData(response!.Answer, query);
            
            if (databaseAnswerIsSufficient)
            {
                _logger.LogDebug("Database response is sufficient, skipping MCP search");
            }
            else if (_mcpIntegration != null && _options.Features.EnableMcpSearch && searchOptions.EnableMcpSearch)
            {
                _logger.LogDebug("MCP search enabled");
                try
                {
                    var mcpResults = await _mcpIntegration.QueryWithMcpAsync(query, maxResults);
                    searchMetadata.McpSearchPerformed = true;
                    searchMetadata.McpResultsFound = mcpResults?.Count(r => r.IsSuccess && !string.IsNullOrWhiteSpace(r.Content)) ?? 0;
                    
                    if (mcpResults != null && mcpResults.Count > 0)
                    {
                        var mcpSources = mcpResults
                            .Where(r => r.IsSuccess && !string.IsNullOrWhiteSpace(r.Content))
                            .Select(r => new SearchSource
                            {
                                SourceType = "MCP",
                                FileName = $"{r.ServerId}:{r.ToolName}",
                                RelevantContent = r.Content,
                                RelevanceScore = 1.0
                            })
                            .ToList();

                        if (mcpSources.Count > 0)
                        {
                            var mcpContext = string.Join("\n\n", mcpResults.Where(r => r.IsSuccess).Select(r => r.Content));
                            
                            if (response != null)
                            {
                                if (response.Sources == null)
                                {
                                    response.Sources = new List<SearchSource>();
                                }
                                response.Sources.AddRange(mcpSources);
                                
                                if (!string.IsNullOrWhiteSpace(mcpContext))
                                {
                                    var existingContext = response.Sources
                                        .Where(s => s.SourceType != "MCP")
                                        .Select(s => s.RelevantContent)
                                        .Where(c => !string.IsNullOrWhiteSpace(c))
                                        .ToList();

                                    var combinedContext = existingContext.Count > 0
                                        ? string.Join("\n\n", existingContext) + "\n\n[MCP Results]\n" + mcpContext
                                        : mcpContext;

                                    var mergedPrompt = _promptBuilder.BuildDocumentRagPrompt(query, combinedContext, conversationHistory, preferredLanguage);
                                    var mergedAnswer = await _aiService.GenerateResponseAsync(mergedPrompt, new List<string> { combinedContext });
                                    if (!string.IsNullOrWhiteSpace(mergedAnswer))
                                    {
                                        response.Answer = mergedAnswer;
                                    }
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(mcpContext))
                            {
                                var mcpPrompt = _promptBuilder.BuildDocumentRagPrompt(query, mcpContext, conversationHistory, preferredLanguage);
                                var mcpAnswer = await _aiService.GenerateResponseAsync(mcpPrompt, new List<string> { mcpContext });
                                if (!string.IsNullOrWhiteSpace(mcpAnswer))
                                {
                                    response = _responseBuilder?.CreateRagResponse(query, mcpAnswer, mcpSources, searchMetadata) 
                                        ?? new RagResponse { Query = query, Answer = mcpAnswer, Sources = mcpSources, SearchedAt = DateTime.UtcNow, SearchMetadata = searchMetadata };
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error querying MCP servers, continuing without MCP results");
                }
            }

            if (response != null)
            {
                if (response.SearchMetadata == null)
                {
                    response.SearchMetadata = searchMetadata;
                }
                await _conversationManager.AddToConversationAsync(sessionId, query, response.Answer);
                return response;
            }

            if (_responseBuilder != null)
            {
                return await _responseBuilder.CreateFallbackResponseAsync(query, conversationHistory, preferredLanguage);
            }

            return new RagResponse { Query = query, Answer = "Sorry, I cannot process your query right now. Please try again later.", Sources = new List<SearchSource>(), SearchedAt = DateTime.UtcNow };
        }

        /// <summary>
        /// Parses source tags from query and adjusts SearchOptions accordingly
        /// Tags: -d (document), -db (database), -mcp (MCP), -a (audio), -i (image)
        /// Returns cleaned query without tags
        /// </summary>
        private (string cleanedQuery, SearchOptions adjustedOptions) ParseSourceTags(string query, SearchOptions options)
        {
            var cleanedQuery = query;
            var adjustedOptions = new SearchOptions
            {
                EnableDatabaseSearch = options.EnableDatabaseSearch,
                EnableDocumentSearch = options.EnableDocumentSearch,
                EnableAudioSearch = options.EnableAudioSearch,
                EnableImageSearch = options.EnableImageSearch,
                EnableMcpSearch = options.EnableMcpSearch,
                PreferredLanguage = options.PreferredLanguage
            };

            var trimmedQuery = query.TrimEnd();

            // Parse -d (document only)
            var dMatch = Regex.Match(trimmedQuery, DocumentTagPattern, TagRegexOptions);
            
            if (!dMatch.Success && trimmedQuery.Length >= 3)
            {
                var punctuationPattern = @"[\p{P}]\s*-d\s*$";
                dMatch = Regex.Match(trimmedQuery, punctuationPattern, TagRegexOptions);
            }
            
            if (dMatch.Success)
            {
                SetDocumentOnlyOptions(adjustedOptions);
                cleanedQuery = trimmedQuery.Substring(0, dMatch.Index).TrimEnd();
                return (cleanedQuery, adjustedOptions);
            }

            // Parse -db (database only)
            var dbMatch = Regex.Match(trimmedQuery, DatabaseTagPattern, TagRegexOptions);
            if (!dbMatch.Success && trimmedQuery.Length >= 4)
            {
                var punctuationPattern = @"[\p{P}]\s*-db\s*$";
                dbMatch = Regex.Match(trimmedQuery, punctuationPattern, TagRegexOptions);
            }
            if (dbMatch.Success)
            {
                SetDatabaseOnlyOptions(adjustedOptions);
                cleanedQuery = trimmedQuery.Substring(0, dbMatch.Index).TrimEnd();
                return (cleanedQuery, adjustedOptions);
            }

            // Parse -mcp (MCP only)
            var mcpMatch = Regex.Match(trimmedQuery, McpTagPattern, TagRegexOptions);
            if (!mcpMatch.Success && trimmedQuery.Length >= 5)
            {
                var punctuationPattern = @"[\p{P}]\s*-mcp\s*$";
                mcpMatch = Regex.Match(trimmedQuery, punctuationPattern, TagRegexOptions);
            }
            if (mcpMatch.Success)
            {
                SetMcpOnlyOptions(adjustedOptions);
                cleanedQuery = trimmedQuery.Substring(0, mcpMatch.Index).TrimEnd();
                return (cleanedQuery, adjustedOptions);
            }

            // Parse -a (audio only)
            var aMatch = Regex.Match(trimmedQuery, AudioTagPattern, TagRegexOptions);
            if (!aMatch.Success && trimmedQuery.Length >= 3)
            {
                var punctuationPattern = @"[\p{P}]\s*-a\s*$";
                aMatch = Regex.Match(trimmedQuery, punctuationPattern, TagRegexOptions);
            }
            if (aMatch.Success)
            {
                SetAudioOnlyOptions(adjustedOptions);
                cleanedQuery = trimmedQuery.Substring(0, aMatch.Index).TrimEnd();
                return (cleanedQuery, adjustedOptions);
            }

            // Parse -i (image only)
            var iMatch = Regex.Match(trimmedQuery, ImageTagPattern, TagRegexOptions);
            if (!iMatch.Success && trimmedQuery.Length >= 3)
            {
                var punctuationPattern = @"[\p{P}]\s*-i\s*$";
                iMatch = Regex.Match(trimmedQuery, punctuationPattern, TagRegexOptions);
            }
            if (iMatch.Success)
            {
                SetImageOnlyOptions(adjustedOptions);
                cleanedQuery = trimmedQuery.Substring(0, iMatch.Index).TrimEnd();
                return (cleanedQuery, adjustedOptions);
            }

            return (cleanedQuery, adjustedOptions);
        }

        /// <summary>
        /// Sets search options for document-only search
        /// </summary>
        private static void SetDocumentOnlyOptions(SearchOptions options)
        {
            options.EnableDocumentSearch = true;
            options.EnableDatabaseSearch = false;
            options.EnableMcpSearch = false;
            options.EnableAudioSearch = false;
            options.EnableImageSearch = false;
        }

        /// <summary>
        /// Sets search options for database-only search
        /// </summary>
        private static void SetDatabaseOnlyOptions(SearchOptions options)
        {
            options.EnableDatabaseSearch = true;
            options.EnableDocumentSearch = false;
            options.EnableMcpSearch = false;
            options.EnableAudioSearch = false;
            options.EnableImageSearch = false;
        }

        /// <summary>
        /// Sets search options for MCP-only search
        /// </summary>
        private static void SetMcpOnlyOptions(SearchOptions options)
        {
            options.EnableMcpSearch = true;
            options.EnableDocumentSearch = false;
            options.EnableDatabaseSearch = false;
            options.EnableAudioSearch = false;
            options.EnableImageSearch = false;
        }

        /// <summary>
        /// Sets search options for audio-only search
        /// </summary>
        private static void SetAudioOnlyOptions(SearchOptions options)
        {
            options.EnableAudioSearch = true;
            options.EnableDocumentSearch = false;
            options.EnableDatabaseSearch = false;
            options.EnableMcpSearch = false;
            options.EnableImageSearch = false;
        }

        /// <summary>
        /// Sets search options for image-only search
        /// </summary>
        private static void SetImageOnlyOptions(SearchOptions options)
        {
            options.EnableImageSearch = true;
            options.EnableDocumentSearch = false;
            options.EnableDatabaseSearch = false;
            options.EnableMcpSearch = false;
            options.EnableAudioSearch = false;
        }

        /// <summary>
        /// Searches for relevant document chunks using repository's optimized search with keyword-based fallback
        /// </summary>
        /// <param name="query">Search query string</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <param name="options">Optional search options to filter documents</param>
        /// <param name="queryTokens">Pre-computed query tokens for performance</param>
        /// <returns>List of relevant document chunks</returns>
        private async Task<List<DocumentChunk>> PerformBasicSearchAsync(string query, int maxResults, SearchOptions? options = null, List<string>? queryTokens = null)
        {
            try
            {
                var searchResults = await _documentRepository.SearchAsync(query, maxResults * InitialSearchMultiplier);

                if (searchResults.Count > 0)
                {
                    var filteredResults = searchResults;

                    if (options != null)
                    {
                        var filteredDocs = await _documentService.GetAllDocumentsFilteredAsync(options);
                        var allowedDocIds = new HashSet<Guid>(filteredDocs.Select(d => d.Id));
                        
                        _logger.LogDebug("PerformBasicSearchAsync: Filtering search results. Total results: {Total}, Allowed document IDs: {AllowedCount}, EnableDocumentSearch: {EnableDocumentSearch}, EnableAudioSearch: {EnableAudioSearch}, EnableImageSearch: {EnableImageSearch}",
                            searchResults.Count, allowedDocIds.Count, options.EnableDocumentSearch, options.EnableAudioSearch, options.EnableImageSearch);
                        
                        var beforeCount = searchResults.Count;
                        filteredResults = searchResults.Where(c => allowedDocIds.Contains(c.DocumentId)).ToList();
                        var afterCount = filteredResults.Count;
                        
                        _logger.LogDebug("PerformBasicSearchAsync: Filtered search results: {BeforeCount} -> {AfterCount} chunks", beforeCount, afterCount);
                        
                        if (afterCount == 0 && beforeCount > 0)
                        {
                            _logger.LogWarning("PerformBasicSearchAsync: All search results were filtered out. This may indicate a filtering issue. Allowed document IDs: {AllowedIds}",
                                string.Join(", ", allowedDocIds.Take(10)));
                        }
                    }

                    return filteredResults;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Repository search failed, falling back to keyword scoring");
            }

            var allDocuments = await _documentService.GetAllDocumentsFilteredAsync(options);
            var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();

            var queryWords = queryTokens ?? QueryTokenizer.TokenizeQuery(query);
            var potentialNames = QueryTokenizer.ExtractPotentialNames(query);

            var scoredChunks = _documentScoring.ScoreChunks(allChunks, query, queryWords, potentialNames);

            var queryWordDocumentMap = _queryWordMatcher.MapQueryWordsToDocuments(
                queryWords,
                allDocuments,
                scoredChunks,
                ChunksToCheckForKeywords);

            var documentScores = _relevanceCalculator.CalculateDocumentScores(
                allDocuments,
                scoredChunks,
                queryWords,
                queryWordDocumentMap,
                TopChunksPerDocument);

            var relevantDocuments = _relevanceCalculator.IdentifyRelevantDocuments(
                documentScores,
                DocumentScoreThreshold);

            var relevantDocumentChunks = relevantDocuments
                .SelectMany(d => scoredChunks.Where(c => c.DocumentId == d.Id))
                .ToList();

            var otherDocumentChunks = allDocuments
                .Except(relevantDocuments)
                .SelectMany(d => scoredChunks.Where(c => c.DocumentId == d.Id))
                .ToList();

            var relevantDocumentIds = new HashSet<Guid>(relevantDocuments.Select(d => d.Id));
            _relevanceCalculator.ApplyDocumentBoost(
                relevantDocumentChunks,
                relevantDocumentIds,
                DocumentRelevanceBoost);

            var finalScoredChunks = relevantDocumentChunks.Concat(otherDocumentChunks).ToList();

            const int CandidateMultiplier = 20;
            const int CandidateMinCount = 200;

            var relevantChunks = finalScoredChunks
                .Where(c => c.RelevanceScore > MinWordCountThreshold)
                .OrderByDescending(c => c.RelevanceScore)
                .Take(Math.Max(maxResults * CandidateMultiplier, CandidateMinCount))
                .ToList();

            if (potentialNames.Count >= MinPotentialNamesCount)
            {
                // Pre-compute lowercase names once to avoid repeated conversions in the loop
                var lowerNames = potentialNames.Select(n => n.ToLowerInvariant()).ToList();

                var nameChunks = relevantChunks.Where(c =>
                {
                    var contentLower = c.Content.ToLowerInvariant();
                    return lowerNames.Any(name => contentLower.Contains(name));
                }).ToList();

                if (nameChunks.Count > MinNameChunksCount)
                {
                    var chunk0 = nameChunks.FirstOrDefault(c => c.ChunkIndex == 0);
                    var otherChunks = nameChunks.Where(c => c.ChunkIndex != 0).Take(maxResults - (chunk0 != null ? 1 : 0)).ToList();

                    if (chunk0 != null)
                    {
                        return new List<DocumentChunk> { chunk0 }.Concat(otherChunks).ToList();
                    }

                    return otherChunks;
                }
            }

            var prioritizedChunks = _chunkPrioritizer.PrioritizeChunksByRelevanceScore(relevantChunks);

            if (_queryPatternAnalyzer.RequiresComprehensiveSearch(query))
            {
                var comprehensiveQueryWords = QueryTokenizer.TokenizeQuery(query);
                var numberedListChunks = _queryPatternAnalyzer.ScoreChunksByNumberedLists(
                    prioritizedChunks,
                    comprehensiveQueryWords,
                    NumberedListBonusPerItem,
                    NumberedListWordMatchBonus);

                var topNumberedChunks = numberedListChunks
                    .Where(c => _queryPatternAnalyzer.DetectNumberedLists(c.Content))
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .ThenByDescending(c => _queryPatternAnalyzer.CountNumberedListItems(c.Content))
                    .Take(maxResults * 2)
                    .ToList();

                if (topNumberedChunks.Count > 0)
                {
                    var chunk0 = topNumberedChunks.FirstOrDefault(c => c.ChunkIndex == 0)
                        ?? prioritizedChunks.FirstOrDefault(c => c.ChunkIndex == 0);
                    var otherChunks = topNumberedChunks
                        .Where(c => c.ChunkIndex != 0)
                        .Concat(prioritizedChunks.Except(topNumberedChunks).Where(c => c.ChunkIndex != 0))
                        .Take(maxResults - (chunk0 != null ? 1 : 0))
                        .ToList();

                    return _chunkPrioritizer.MergeChunksWithPreservedChunk0(otherChunks, chunk0);
                }
            }

            return prioritizedChunks.Take(maxResults).ToList();
        }

        /// <summary>
        /// Generates RAG answer with automatic session management and context expansion
        /// </summary>
        /// <param name="request">Request containing query parameters</param>
        /// <returns>RAG response with answer and sources</returns>
        async Task<RagResponse> IRagAnswerGeneratorService.GenerateBasicRagAnswerAsync(Models.RequestResponse.GenerateRagAnswerRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var searchMaxResults = _queryAnalysis?.DetermineInitialSearchCount(request.Query, request.MaxResults) ?? request.MaxResults;

            List<DocumentChunk> chunks;
            var queryTokensForPrioritization = request.QueryTokens ?? QueryTokenizer.TokenizeQuery(request.Query);

            DocumentChunk? preservedChunk0 = null;

            if (request.PreCalculatedResults != null && request.PreCalculatedResults.Count > 0)
            {
                var filteredPreCalculatedResults = request.PreCalculatedResults;
                if (request.Options != null)
                {
                    var filteredDocs = await _documentService.GetAllDocumentsFilteredAsync(request.Options);
                    var allowedDocIds = new HashSet<Guid>(filteredDocs.Select(d => d.Id));
                    var beforeCount = request.PreCalculatedResults.Count;
                    filteredPreCalculatedResults = request.PreCalculatedResults.Where(c => allowedDocIds.Contains(c.DocumentId)).ToList();
                    var afterCount = filteredPreCalculatedResults.Count;
                    
                    _logger.LogDebug("Filtered preCalculatedResults: {BeforeCount} -> {AfterCount} chunks (EnableDocumentSearch: {EnableDocumentSearch}, EnableAudioSearch: {EnableAudioSearch}, EnableImageSearch: {EnableImageSearch})",
                        beforeCount, afterCount, request.Options.EnableDocumentSearch, request.Options.EnableAudioSearch, request.Options.EnableImageSearch);
                }
                
                // This ensures image chunks and other high-scoring chunks are selected first
                chunks = filteredPreCalculatedResults
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .ThenBy(c => c.ChunkIndex)
                    .ToList();
                
                _logger.LogDebug("PreCalculatedResults sorted by relevance score. Total: {Count}, Top score: {TopScore}",
                    chunks.Count, chunks.FirstOrDefault()?.RelevanceScore ?? 0.0);
            }
            else
            {
                chunks = await SearchDocumentsAsync(request.Query, searchMaxResults, request.Options, request.QueryTokens);
                preservedChunk0 = chunks.FirstOrDefault(c => c.ChunkIndex == 0);
                var nonZeroChunksForSearch = chunks.Where(c => c.ChunkIndex != 0).ToList();
                chunks = _chunkPrioritizer.PrioritizeChunksByQueryWords(nonZeroChunksForSearch, queryTokensForPrioritization);
                chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(chunks, preservedChunk0);
            }

            var needsAggressiveSearch = chunks.Count < 5 || _queryPatternAnalyzer.RequiresComprehensiveSearch(request.Query);
            if (needsAggressiveSearch)
            {
                preservedChunk0 ??= chunks.FirstOrDefault(c => c.ChunkIndex == 0);

                var allDocuments = await _documentService.GetAllDocumentsFilteredAsync(request.Options);
                var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();
                var queryWords = request.QueryTokens ?? QueryTokenizer.TokenizeQuery(request.Query);
                var potentialNames = QueryTokenizer.ExtractPotentialNames(request.Query);
                var scoredChunks = _documentScoring.ScoreChunks(allChunks, request.Query, queryWords, potentialNames);

                var queryWordDocumentMap = _queryWordMatcher.MapQueryWordsToDocuments(
                    queryWords,
                    allDocuments,
                    scoredChunks,
                    ChunksToCheckForKeywords);

                var documentScores = _relevanceCalculator.CalculateDocumentScores(
                    allDocuments,
                    scoredChunks,
                    queryWords,
                    queryWordDocumentMap,
                    TopChunksPerDocument);

                var relevantDocuments = _relevanceCalculator.IdentifyRelevantDocuments(
                    documentScores,
                    DocumentScoreThreshold);

                var relevantDocumentChunks = relevantDocuments
                    .SelectMany(d => scoredChunks.Where(c => c.DocumentId == d.Id))
                    .ToList();

                var docScoreMap = relevantDocuments.ToDictionary(
                    d => d.Id,
                    d => documentScores.First(ds => ds.Document.Id == d.Id).Score);

                foreach (var chunk in relevantDocumentChunks)
                {
                    if (docScoreMap.TryGetValue(chunk.DocumentId, out var docScore))
                    {
                        chunk.RelevanceScore = (chunk.RelevanceScore ?? 0.0) + docScore;
                    }
                }

                allChunks = relevantDocumentChunks.Concat(
                    allDocuments.Except(relevantDocuments)
                        .SelectMany(d => scoredChunks.Where(c => c.DocumentId == d.Id))
                ).ToList();

                if (preservedChunk0 != null && !allChunks.Any(c => c.Id == preservedChunk0.Id))
                {
                    allChunks.Insert(0, preservedChunk0);
                }

                var numberedListChunks = _queryPatternAnalyzer.ScoreChunksByNumberedLists(
                    allChunks,
                    queryWords,
                    NumberedListBonusPerItem,
                    NumberedListWordMatchBonus);

                if (_queryPatternAnalyzer.RequiresComprehensiveSearch(request.Query))
                {
                    var numberedListWithQueryWords = numberedListChunks
                        .Where(c => _queryPatternAnalyzer.DetectNumberedLists(c.Content) &&
                            queryWords.Any(word => c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0))
                        .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                        .ThenByDescending(c => _queryPatternAnalyzer.CountNumberedListItems(c.Content))
                        .Take(searchMaxResults * 3)
                        .ToList();

                    var numberedListOnly = numberedListChunks
                        .Where(c => _queryPatternAnalyzer.DetectNumberedLists(c.Content) &&
                            !queryWords.Any(word => c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0))
                        .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                        .ThenByDescending(c => _queryPatternAnalyzer.CountNumberedListItems(c.Content))
                        .Take(searchMaxResults * 2)
                        .ToList();

                    var queryWordsOnly = _chunkPrioritizer.PrioritizeChunksByQueryWords(
                        allChunks.Where(c => !_queryPatternAnalyzer.DetectNumberedLists(c.Content)).ToList(),
                        queryWords)
                        .Take(searchMaxResults * 2)
                        .ToList();

                    var mergedChunks = new List<DocumentChunk>();
                    var seenIds = new HashSet<Guid>();

                    foreach (var chunk in numberedListWithQueryWords.Concat(numberedListOnly).Concat(queryWordsOnly))
                    {
                        if (!seenIds.Contains(chunk.Id) && mergedChunks.Count < searchMaxResults * 4)
                        {
                            mergedChunks.Add(chunk);
                            seenIds.Add(chunk.Id);
                        }
                    }

                    mergedChunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(mergedChunks, preservedChunk0);

                    foreach (var chunk in chunks)
                    {
                        if (!seenIds.Contains(chunk.Id) && mergedChunks.Count < searchMaxResults * 4)
                        {
                            mergedChunks.Add(chunk);
                            seenIds.Add(chunk.Id);
                        }
                    }

                    if (mergedChunks.Count > chunks.Count)
                    {
                        chunks = mergedChunks;
                        ServiceLogMessages.LogFallbackSearchUsed(_logger, chunks.Count, null);
                    }
                }
                else
                {
                    var prioritizedChunksForFallback = _chunkPrioritizer.PrioritizeChunksByQueryWords(
                        numberedListChunks.Where(c => queryWords.Any(word => c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)).ToList(),
                        queryWords)
                        .Take(searchMaxResults * 4)
                        .ToList();

                    if (prioritizedChunksForFallback.Count > chunks.Count)
                    {
                        chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(prioritizedChunksForFallback, preservedChunk0);
                        ServiceLogMessages.LogFallbackSearchUsed(_logger, chunks.Count, null);
                    }
                }
            }

            HashSet<Guid>? originalChunkIds = request.PreCalculatedResults != null && request.PreCalculatedResults.Count > 0
                ? new HashSet<Guid>(request.PreCalculatedResults.Select(c => c.Id))
                : new HashSet<Guid>(chunks.Select(c => c.Id));

            chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(chunks, preservedChunk0);

            if (_contextExpansion != null && chunks.Count > 0)
            {
                var relevantDocumentChunks = chunks
                    .Where(c => (c.RelevanceScore ?? 0.0) >= DocumentBoostThreshold)
                    .ToList();

                var otherChunks = chunks
                    .Where(c => (c.RelevanceScore ?? 0.0) < DocumentBoostThreshold)
                    .ToList();

                if (relevantDocumentChunks.Count > 0)
                {
                    var originalScores = relevantDocumentChunks.ToDictionary(c => c.Id, c => c.RelevanceScore ?? 0.0);

                    if (_contextExpansion != null)
                    {
                        var contextWindow = _contextExpansion.DetermineContextWindow(relevantDocumentChunks, request.Query);
                        var expandedChunks = await _contextExpansion.ExpandContextAsync(relevantDocumentChunks, contextWindow);

                        var queryWords = request.QueryTokens ?? QueryTokenizer.TokenizeQuery(request.Query);

                        // Calculate max original score to ensure expanded chunks don't outrank original chunks
                        var maxOriginalScore = originalScores.Values.Any() ? originalScores.Values.Max() : 0.0;
                        var minOriginalScore = originalScores.Values.Any() ? originalScores.Values.Min() : 0.0;

                        // Score expanded chunks with simple word matching (not DocumentScoringService)
                        // Expanded chunks are context-only and should have much lower scores than original search results
                        foreach (var chunk in expandedChunks)
                        {
                            if (originalScores.ContainsKey(chunk.Id))
                            {
                                chunk.RelevanceScore = originalScores[chunk.Id];
                            }
                            else
                            {
                                // Use simple word match scoring for expanded chunks (context-only, low priority)
                                var content = chunk.Content.ToLowerInvariant();
                                var wordMatches = queryWords.Count(word =>
                                    content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

                                // Expanded chunks get minimal score based on word matches only
                                // Score is capped at 30% of minimum original score to ensure original chunks always rank higher
                                // This prevents expanded context chunks from outranking original search results
                                var expandedScore = wordMatches * 0.05; // Very low base score per word match (0.05 instead of 0.1)
                                var maxAllowedScore = minOriginalScore > 0 ? minOriginalScore * 0.3 : maxOriginalScore * 0.05;
                                chunk.RelevanceScore = Math.Min(expandedScore, maxAllowedScore);
                            }
                        }

                        // Sort by relevance score only - no document type prioritization
                        // Highest relevance score wins, regardless of whether it's Image, Document, or Audio
                        // Original chunks (from initial search) are prioritized over expanded context chunks
                        // Chunk 0 (document header) is also prioritized
                        chunks = expandedChunks
                            .OrderByDescending(c => originalChunkIds.Contains(c.Id))
                            .ThenByDescending(c => c.ChunkIndex == 0) // Prioritize chunk 0 (document header)
                            .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                            .ThenBy(c => c.ChunkIndex)
                            .Concat(otherChunks
                                .OrderByDescending(c => c.ChunkIndex == 0) // Prioritize chunk 0 (document header)
                                .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                                .ThenBy(c => c.ChunkIndex))
                            .ToList();

                        if (chunks.Count > MaxExpandedChunks)
                        {
                            // Take top chunks by relevance score (already sorted above)
                            chunks = chunks.Take(MaxExpandedChunks).ToList();
                            ServiceLogMessages.LogContextExpansionLimited(_logger, MaxExpandedChunks, null);
                        }
                    }
                    else
                    {
                        chunks = _chunkPrioritizer.PrioritizeChunksByRelevanceScore(chunks);
                    }
                }
            }

            // After context expansion, chunks are already sorted by relevance score and original chunk priority
            // Do NOT re-prioritize by query words here, as it can incorrectly rank irrelevant Chunk 0s from other documents
            if (preservedChunk0 == null)
            {
                // Find Chunk 0 from the HIGHEST scoring original document (not just the first Chunk 0)
                var topOriginalDocumentId = chunks
                    .Where(c => (c.RelevanceScore ?? 0.0) >= DocumentBoostThreshold)
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .Select(c => c.DocumentId)
                    .FirstOrDefault();

                if (topOriginalDocumentId != Guid.Empty)
                {
                    preservedChunk0 = chunks.FirstOrDefault(c => c.ChunkIndex == 0 && c.DocumentId == topOriginalDocumentId);
                }
            }

            // Do NOT use PrioritizeChunksByQueryWords here - chunks are already correctly sorted by context expansion
            chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(chunks, preservedChunk0);

            // Build context with size limit to prevent timeout
            var context = _contextExpansion != null
                ? _contextExpansion.BuildLimitedContext(chunks, MaxContextSize)
                : string.Join("\n\n", chunks.Select(c => c.Content ?? string.Empty));

            var prompt = _promptBuilder.BuildDocumentRagPrompt(request.Query, context, request.ConversationHistory, request.PreferredLanguage);
            var answer = await _aiService.GenerateResponseAsync(prompt, new List<string> { context });

            var sourcesChunks = originalChunkIds != null && originalChunkIds.Count > 0
                ? chunks.Where(c => originalChunkIds.Contains(c.Id)).ToList()
                : chunks;

            return _responseBuilder?.CreateRagResponse(request.Query, answer, await _sourceBuilder.BuildSourcesAsync(sourcesChunks, _documentRepository)) ?? new RagResponse { Query = request.Query, Answer = answer, Sources = await _sourceBuilder.BuildSourcesAsync(sourcesChunks, _documentRepository), SearchedAt = DateTime.UtcNow };
        }

        /// <summary>
        /// Generates RAG answer with automatic session management and context expansion
        /// </summary>
        /// <param name="query">Natural language query to process</param>
        /// <param name="maxResults">Maximum number of document chunks to use</param>
        /// <param name="conversationHistory">Conversation history</param>
        /// <param name="preferredLanguage">Optional preferred language code for AI response</param>
        /// <param name="options">Optional search options</param>
        /// <param name="preCalculatedResults">Pre-calculated search results to use</param>
        /// <param name="queryTokens">Pre-computed query tokens for performance</param>
        /// <returns>RAG response with answer and sources</returns>
        [Obsolete("Use GenerateBasicRagAnswerAsync(GenerateRagAnswerRequest) instead. This method will be removed in v4.0.0")]
        public async Task<RagResponse> GenerateBasicRagAnswerAsync(string query, int maxResults, string conversationHistory, string? preferredLanguage = null, SearchOptions? options = null, List<DocumentChunk>? preCalculatedResults = null, List<string>? queryTokens = null)
        {
            var request = new Models.RequestResponse.GenerateRagAnswerRequest
            {
                Query = query,
                MaxResults = maxResults,
                ConversationHistory = conversationHistory,
                PreferredLanguage = preferredLanguage,
                Options = options,
                PreCalculatedResults = preCalculatedResults,
                QueryTokens = queryTokens
            };
            return await ((IRagAnswerGeneratorService)this).GenerateBasicRagAnswerAsync(request);
        }


        /// <summary>
        /// Determines if a query can be answered from documents using language-agnostic content-based analysis
        /// </summary>
        async Task<(bool CanAnswer, List<DocumentChunk> Results)> IRagAnswerGeneratorService.CanAnswerFromDocumentsAsync(string query, SearchOptions? options, List<string>? queryTokens)
        {
            return await CanAnswerFromDocumentsAsyncInternal(query, options, queryTokens);
        }

        /// <summary>
        /// Determines if a query can be answered from documents using language-agnostic content-based analysis (internal implementation)
        /// </summary>
        private async Task<(bool CanAnswer, List<DocumentChunk> Results)> CanAnswerFromDocumentsAsyncInternal(string query, SearchOptions? options = null, List<string>? queryTokens = null)
        {
            try
            {
                var searchResults = _documentSearchStrategy != null
                    ? await _documentSearchStrategy.SearchDocumentsAsync(query, FallbackSearchMaxResults, options, queryTokens)
                    : await PerformBasicSearchAsync(query, FallbackSearchMaxResults, options, queryTokens);

                if (searchResults.Count == MinSearchResultsCount)
                {
                    return (false, searchResults);
                }

                var topScores = searchResults.Take(5).Select(c => c.RelevanceScore ?? 0.0).ToList();
                _logger.LogDebug("Top 5 relevance scores: {Scores}", string.Join(", ", topScores.Select(s => s.ToString("F4"))));

                var sortedByScore = searchResults.OrderByDescending(c => c.RelevanceScore ?? 0.0).ToList();
                var maxScore = sortedByScore.FirstOrDefault()?.RelevanceScore ?? 0.0;

                double adaptiveThreshold;
                int percentile;
                if (maxScore > 3.0)
                {
                    // Native text search scores (5.0+ range)
                    // Use top 70% to be more inclusive, but ensure minimum threshold
                    // If scores are very close (e.g., all 5.1-5.2), use a lower relative threshold
                    percentile = 70;
                    var topPercentileCount = Math.Max(1, (int)(sortedByScore.Count * 0.7)); // Top 70%
                    var percentileScore = sortedByScore.Skip(topPercentileCount - 1).FirstOrDefault()?.RelevanceScore ?? 4.0;

                    // If scores are very close together (difference < 0.5), use a more lenient threshold
                    var minScore = sortedByScore.LastOrDefault()?.RelevanceScore ?? 0.0;
                    var scoreRange = maxScore - minScore;
                    if (scoreRange < 0.5 && sortedByScore.Count > 1)
                    {
                        // Scores are very close, use a lower threshold to include more results
                        // Use maxScore - 0.5 to ensure we include chunks with score equal to threshold
                        adaptiveThreshold = Math.Max(4.5, maxScore - 0.5);
                    }
                    else
                    {
                        // Use percentile score but ensure we include chunks at the threshold boundary
                        // Subtract a small epsilon to ensure >= comparison works correctly
                        adaptiveThreshold = Math.Max(4.0, percentileScore - 0.01);
                    }
                }
                else
                {
                    percentile = 40;
                    var topPercentileCount = Math.Max(1, (int)(sortedByScore.Count * 0.4));
                    adaptiveThreshold = Math.Max(0.01, sortedByScore.Skip(topPercentileCount - 1).FirstOrDefault()?.RelevanceScore ?? 0.01);
                }

                _logger.LogDebug("Adaptive threshold: {Threshold:F4} (top {Percentile}% of {Total} results, maxScore: {MaxScore:F4})",
                    adaptiveThreshold, percentile, sortedByScore.Count, maxScore);

                var chunk0FromSearch = searchResults.FirstOrDefault(c => c.ChunkIndex == 0);
                _logger.LogDebug("SearchResults contains chunk 0: {HasChunk0}, Total: {Count}",
                    chunk0FromSearch != null, searchResults.Count);

                var hasRelevantContent = searchResults.Any(chunk =>
                    (chunk.RelevanceScore ?? 0.0) >= adaptiveThreshold || chunk.ChunkIndex == 0);

                if (!hasRelevantContent)
                {
                    _logger.LogDebug("No chunks exceeded adaptive threshold {Threshold:F4}. Max score: {MaxScore:F4}",
                        adaptiveThreshold, searchResults.Max(c => c.RelevanceScore ?? 0.0));
                    return (false, searchResults);
                }

                var totalContentLength = searchResults
                    .Where(c => (c.RelevanceScore ?? 0.0) >= adaptiveThreshold || c.ChunkIndex == 0)
                    .Sum(c => c.Content.Length);

                var hasSubstantialContent = totalContentLength > MinSubstantialContentLength;

                _logger.LogDebug("Content analysis: relevant={HasRelevant}, contentLength={Length}, threshold={Threshold}",
                    hasRelevantContent, totalContentLength, MinSubstantialContentLength);

                var filteredResults = searchResults
                    .Where(c => (c.RelevanceScore ?? 0.0) >= adaptiveThreshold || c.ChunkIndex == 0)
                    .ToList();

                if (chunk0FromSearch != null && !filteredResults.Any(c => c.ChunkIndex == 0))
                {
                    filteredResults = new List<DocumentChunk> { chunk0FromSearch }.Concat(filteredResults).ToList();
                    _logger.LogWarning("Chunk 0 was missing after adaptive threshold, re-added. Total results: {ResultCount}", filteredResults.Count);
                }

                _logger.LogDebug("Final filteredResults count: {Count}, Chunk 0 present: {HasChunk0}",
                    filteredResults.Count, filteredResults.Any(c => c.ChunkIndex == 0));

                return (hasRelevantContent && hasSubstantialContent, filteredResults);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogCanAnswerFromDocumentsError(_logger, ex);
                return (true, new List<DocumentChunk>());
            }
        }


        /// <summary>
        /// Generate RAG answer with automatic session management (Legacy method - use QueryIntelligenceAsync)
        /// </summary>
        /// <param name="query">User query to process</param>
        /// <param name="maxResults">Maximum number of document chunks to use</param>
        /// <param name="startNewConversation">Whether to start a new conversation session</param>
        /// <returns>RAG response with answer and sources</returns>
        [Obsolete("Use QueryIntelligenceAsync instead. This method will be removed in v4.0.0")]
        public async Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5, bool startNewConversation = false)
        {
            return await QueryIntelligenceAsync(query, maxResults, startNewConversation);
        }
    }
}