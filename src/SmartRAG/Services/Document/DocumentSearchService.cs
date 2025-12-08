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
        #region Constants

        // DocumentSearchService-specific constants (not moved to other services)
        private const int InitialSearchMultiplier = 2;
        private const int MinSearchResultsCount = 0;
        private const int FallbackSearchMaxResults = 10;
        private const int MinSubstantialContentLength = 50;
        private const int MaxExpandedChunks = 50;
        private const int MaxContextSize = 18000;
        private const double DocumentBoostThreshold = 200.0;

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

        // Regex patterns for parsing source tags from query
        // Pattern matches: whitespace or punctuation + tag + optional whitespace at end
        // This handles cases like "query? -d", "query! -d", "query -d", etc.
        private const string DocumentTagPattern = @"\s*-d\s*$";
        private const string DatabaseTagPattern = @"\s*-db\s*$";
        private const string McpTagPattern = @"\s*-mcp\s*$";
        private const string AudioTagPattern = @"\s*-a\s*$";
        private const string ImageTagPattern = @"\s*-i\s*$";
        private const RegexOptions TagRegexOptions = RegexOptions.IgnoreCase;

        #endregion

        #region Fields

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

        #endregion

        #region Constructor

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

        #endregion

        #region Public Methods

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
            var chunk0 = searchResults.FirstOrDefault(c => c.ChunkIndex == 0);
            var otherChunks = searchResults.Where(c => c.ChunkIndex != 0).Take(maxResults - (chunk0 != null ? 1 : 0)).ToList();

            if (chunk0 != null)
            {
                return new List<DocumentChunk> { chunk0 }.Concat(otherChunks).ToList();
            }

            return otherChunks;
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

            // Skip queries that start with / but are not recognized commands
            // This prevents processing invalid slash commands as regular queries
            var trimmedQuery = query.Trim();
            if (trimmedQuery.StartsWith("/", StringComparison.Ordinal))
            {
                // Check if it's a recognized command
                if (!_queryIntentClassifier.TryParseCommand(trimmedQuery, out var parsedCommandType, out var _))
                {
                    // Unknown slash command - skip processing
                    _logger.LogDebug("Skipping unknown slash command: {Query}", trimmedQuery);
                    return new RagResponse
                    {
                        Answer = string.Empty,
                        Sources = new List<SearchSource>(),
                        Query = trimmedQuery
                    };
                }
                // If it's a recognized command, continue processing (it will be handled later in the method)
            }

            var searchOptions = options ?? SearchOptions.FromConfig(_options);

            // Parse source tags from query (-d, -db, -mcp, -a, -i)
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

            // Pre-evaluate document availability for smarter strategy selection
            // Only check if document search is enabled
            // Compute query tokens once here and pass to all sub-methods to avoid redundant tokenization
            var queryTokens = searchOptions.EnableDocumentSearch ? QueryTokenizer.TokenizeQuery(query) : null;

            var (CanAnswer, Results) = searchOptions.EnableDocumentSearch
                ? await CanAnswerFromDocumentsAsyncInternal(query, searchOptions, queryTokens)
                : (CanAnswer: false, Results: new List<DocumentChunk>());

            // Early exit optimization: if documents provide high-confidence results, skip other sources
            if (_sourceSelectionService != null &&
                _options.Features.SourceSelection.EnableEarlyExit &&
                searchOptions.EnableDocumentSearch &&
                CanAnswer &&
                await _sourceSelectionService.ShouldSkipOtherSourcesAsync(CanAnswer, Results, _options.Features.SourceSelection.EarlyExitRelevanceThreshold))
            {
                _logger.LogInformation("High-confidence document results found, skipping other sources for faster response");
                if (_strategyExecutor == null)
                {
                    throw new InvalidOperationException("IQueryStrategyExecutorService is required for strategy execution");
                }
                return await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(query, maxResults, conversationHistory, CanAnswer, preferredLanguage, searchOptions, Results, queryTokens);
            }

            if (_multiDatabaseQueryCoordinator != null && searchOptions.EnableDatabaseSearch)
            {
                try
                {
                    // Analyze query intent using AI if analyzer is available
                    QueryIntent? queryIntent = null;
                    if (_queryIntentAnalyzer != null)
                    {
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
                        QueryStrategy.DatabaseOnly => await _strategyExecutor.ExecuteDatabaseOnlyStrategyAsync(query, maxResults, conversationHistory, CanAnswer, queryIntent, preferredLanguage, searchOptions, queryTokens),
                        QueryStrategy.DocumentOnly => await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(query, maxResults, conversationHistory, CanAnswer, preferredLanguage, searchOptions, Results, queryTokens),
                        QueryStrategy.Hybrid => await _strategyExecutor.ExecuteHybridStrategyAsync(query, maxResults, conversationHistory, hasDatabaseQueries, CanAnswer, queryIntent, preferredLanguage, searchOptions, Results, queryTokens),
                        _ => await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(query, maxResults, conversationHistory, CanAnswer, preferredLanguage, searchOptions, Results, queryTokens) // Fallback
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during query intent analysis, falling back to document-only query");
                    response = _strategyExecutor != null
                        ? await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(query, maxResults, conversationHistory, CanAnswer, preferredLanguage, searchOptions, Results, queryTokens)
                        : throw new InvalidOperationException("IQueryStrategyExecutorService is required for strategy execution");
                }
            }
            else
            {
                if (searchOptions.EnableDocumentSearch)
                {
                    response = _strategyExecutor != null
                        ? await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(query, maxResults, conversationHistory, CanAnswer, preferredLanguage, searchOptions, Results, queryTokens)
                        : throw new InvalidOperationException("IQueryStrategyExecutorService is required for strategy execution");
                }
                else
                {
                    // Both disabled? Check MCP first, then fallback to chat
                    if (_mcpIntegration != null && _options.Features.EnableMcpSearch && searchOptions.EnableMcpSearch)
                    {
                        try
                        {
                            var mcpResults = await _mcpIntegration.QueryWithMcpAsync(query, maxResults, conversationHistory);
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
                                            response = _responseBuilder?.CreateRagResponse(query, mcpAnswer, mcpSources) ?? new RagResponse { Query = query, Answer = mcpAnswer, Sources = mcpSources, SearchedAt = DateTime.UtcNow };
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

            if (_mcpIntegration != null && _options.Features.EnableMcpSearch && searchOptions.EnableMcpSearch)
            {
                _logger.LogDebug("MCP search enabled. Query: {Query}", query);
                try
                {
                    var mcpResults = await _mcpIntegration.QueryWithMcpAsync(query, maxResults);
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
                            response.Sources.AddRange(mcpSources);
                            var mcpContext = string.Join("\n\n", mcpResults.Where(r => r.IsSuccess).Select(r => r.Content));
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
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error querying MCP servers, continuing without MCP results");
                }
            }

            await _conversationManager.AddToConversationAsync(sessionId, query, response.Answer);

            return response;
        }

        #endregion

        #region Private Methods

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
        public async Task<RagResponse> GenerateBasicRagAnswerAsync(string query, int maxResults, string conversationHistory, string? preferredLanguage = null, SearchOptions? options = null, List<DocumentChunk>? preCalculatedResults = null, List<string>? queryTokens = null)
        {
            var searchMaxResults = _queryAnalysis?.DetermineInitialSearchCount(query, maxResults) ?? maxResults;

            List<DocumentChunk> chunks;
            var queryTokensForPrioritization = queryTokens ?? QueryTokenizer.TokenizeQuery(query);

            DocumentChunk? preservedChunk0 = null;

            if (preCalculatedResults != null && preCalculatedResults.Count > 0)
            {
                // Filter preCalculatedResults by document type if options are provided
                var filteredPreCalculatedResults = preCalculatedResults;
                if (options != null)
                {
                    var filteredDocs = await _documentService.GetAllDocumentsFilteredAsync(options);
                    var allowedDocIds = new HashSet<Guid>(filteredDocs.Select(d => d.Id));
                    var beforeCount = preCalculatedResults.Count;
                    filteredPreCalculatedResults = preCalculatedResults.Where(c => allowedDocIds.Contains(c.DocumentId)).ToList();
                    var afterCount = filteredPreCalculatedResults.Count;
                    
                    _logger.LogDebug("Filtered preCalculatedResults: {BeforeCount} -> {AfterCount} chunks (EnableDocumentSearch: {EnableDocumentSearch}, EnableAudioSearch: {EnableAudioSearch}, EnableImageSearch: {EnableImageSearch})",
                        beforeCount, afterCount, options.EnableDocumentSearch, options.EnableAudioSearch, options.EnableImageSearch);
                }
                
                preservedChunk0 = filteredPreCalculatedResults.FirstOrDefault(c => c.ChunkIndex == 0);
                _logger.LogDebug("PreCalculatedResults contains chunk 0: {HasChunk0}, Total: {Count}",
                    preservedChunk0 != null, filteredPreCalculatedResults.Count);

                chunks = filteredPreCalculatedResults.Where(c => c.ChunkIndex != 0).ToList();

                if (preservedChunk0 != null)
                {
                    chunks = new List<DocumentChunk> { preservedChunk0 }.Concat(chunks).ToList();
                    _logger.LogDebug("Chunk 0 re-added from preCalculatedResults. Total chunks: {Count}", chunks.Count);
                }
            }
            else
            {
                chunks = await SearchDocumentsAsync(query, searchMaxResults, options, queryTokens);
                preservedChunk0 = chunks.FirstOrDefault(c => c.ChunkIndex == 0);
                var nonZeroChunksForSearch = chunks.Where(c => c.ChunkIndex != 0).ToList();
                chunks = _chunkPrioritizer.PrioritizeChunksByQueryWords(nonZeroChunksForSearch, queryTokensForPrioritization);
                chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(chunks, preservedChunk0);
            }

            var needsAggressiveSearch = chunks.Count < 5 || _queryPatternAnalyzer.RequiresComprehensiveSearch(query);
            if (needsAggressiveSearch)
            {
                preservedChunk0 ??= chunks.FirstOrDefault(c => c.ChunkIndex == 0);

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

                if (_queryPatternAnalyzer.RequiresComprehensiveSearch(query))
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
                    var originalChunkIds = new HashSet<Guid>(relevantDocumentChunks.Select(c => c.Id));
                    var originalScores = relevantDocumentChunks.ToDictionary(c => c.Id, c => c.RelevanceScore ?? 0.0);

                    if (_contextExpansion != null)
                    {
                        var contextWindow = _contextExpansion.DetermineContextWindow(relevantDocumentChunks, query);
                        var expandedChunks = await _contextExpansion.ExpandContextAsync(relevantDocumentChunks, contextWindow);

                        var queryWords = QueryTokenizer.TokenizeQuery(query);
                        foreach (var chunk in expandedChunks)
                        {
                            if (!originalScores.ContainsKey(chunk.Id))
                            {
                                var content = chunk.Content.ToLowerInvariant();
                                var wordMatches = queryWords.Count(word =>
                                    content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
                                var hasNumbers = chunk.Content.Any(char.IsDigit);

                                var numberedListCount = _queryPatternAnalyzer.CountNumberedListItems(chunk.Content);
                                var hasNumberedList = numberedListCount > 0;

                                chunk.RelevanceScore = (wordMatches * 0.1) +
                                    (hasNumbers ? 0.2 : 0.0) +
                                    (hasNumberedList ? 0.5 + (numberedListCount * 0.1) : 0.0);
                            }
                            else
                            {
                                chunk.RelevanceScore = originalScores[chunk.Id];
                            }
                        }

                        chunks = expandedChunks
                            .OrderByDescending(c => c.ChunkIndex == 0)
                            .ThenByDescending(c => originalChunkIds.Contains(c.Id))
                            .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                            .Concat(otherChunks
                                .OrderByDescending(c => c.ChunkIndex == 0)
                                .ThenByDescending(c => c.RelevanceScore ?? 0.0))
                            .ToList();

                        if (chunks.Count > MaxExpandedChunks)
                        {
                            var chunk0Preserved = chunks.FirstOrDefault(c => c.ChunkIndex == 0);
                            var otherChunksForLimit = chunks.Where(c => c.ChunkIndex != 0).Take(MaxExpandedChunks - (chunk0Preserved != null ? 1 : 0)).ToList();
                            chunks = chunk0Preserved != null
                                ? new List<DocumentChunk> { chunk0Preserved }.Concat(otherChunksForLimit).ToList()
                                : otherChunksForLimit;
                            ServiceLogMessages.LogContextExpansionLimited(_logger, MaxExpandedChunks, null);
                        }
                    }
                    else
                    {
                        chunks = _chunkPrioritizer.PrioritizeChunksByRelevanceScore(chunks);
                    }
                }
            }

            preservedChunk0 ??= chunks.FirstOrDefault(c => c.ChunkIndex == 0);
            var nonZeroChunks = chunks.Where(c => c.ChunkIndex != 0).ToList();
            var prioritizedNonZeroChunks = _chunkPrioritizer.PrioritizeChunksByQueryWords(nonZeroChunks, queryTokensForPrioritization);
            chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(prioritizedNonZeroChunks, preservedChunk0);

            // Build context with size limit to prevent timeout
            var context = _contextExpansion != null
                ? _contextExpansion.BuildLimitedContext(chunks, MaxContextSize)
                : string.Join("\n\n", chunks.Select(c => c.Content ?? string.Empty));

            var prompt = _promptBuilder.BuildDocumentRagPrompt(query, context, conversationHistory, preferredLanguage);
            var answer = await _aiService.GenerateResponseAsync(prompt, new List<string> { context });

            return _responseBuilder?.CreateRagResponse(query, answer, await _sourceBuilder.BuildSourcesAsync(chunks, _documentRepository)) ?? new RagResponse { Query = query, Answer = answer, Sources = await _sourceBuilder.BuildSourcesAsync(chunks, _documentRepository), SearchedAt = DateTime.UtcNow };
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

        #endregion
    }
}