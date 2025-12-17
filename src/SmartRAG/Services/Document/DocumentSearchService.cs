#nullable enable

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
        private const int InitialSearchMultiplier = 2;
        private const int MinSearchResultsCount = 0;
        private const int FallbackSearchMaxResults = 10;
        private const int MinSubstantialContentLength = 50;
        private const int MaxExpandedChunks = 50;
        private const int MaxContextSize = 18000;
        private const double DocumentBoostThreshold = 4.5;
        private const int TopChunksPerDocument = 5;
        private const int ChunksToCheckForKeywords = 30;
        private const double DocumentScoreThreshold = 0.8;
        private const double NumberedListBonusPerItem = 100.0;
        private const double NumberedListWordMatchBonus = 10.0;
        private const double SkipEagerDocumentAnswerConfidenceThreshold = 0.85;
        private const double StrongDocumentMatchThreshold = 4.8;

        private const string DocumentTagPattern = @"\s*-d\s*$";
        private const string DatabaseTagPattern = @"\s*-db\s*$";
        private const string McpTagPattern = @"\s*-mcp\s*$";
        private const string AudioTagPattern = @"\s*-a\s*$";
        private const string ImageTagPattern = @"\s*-i\s*$";
        private const string PunctuationPrefix = @"[\p{P}]";
        private const RegexOptions TagRegexOptions = RegexOptions.IgnoreCase;

        private readonly IDocumentRepository _documentRepository;
        private readonly IAIService _aiService;
        private readonly SmartRagOptions _options;
        private readonly ILogger<DocumentSearchService> _logger;
        private readonly IQueryIntentAnalyzer _queryIntentAnalyzer;
        private readonly IConversationManagerService _conversationManager;
        private readonly IQueryIntentClassifierService _queryIntentClassifier;
        private readonly IPromptBuilderService _promptBuilder;
        private readonly IDocumentScoringService _documentScoring;
        private readonly ISourceBuilderService _sourceBuilder;
        private readonly IContextExpansionService _contextExpansion;
        private readonly IMcpIntegrationService _mcpIntegration;
        private readonly IDocumentRelevanceCalculatorService _relevanceCalculator;
        private readonly IQueryWordMatcherService _queryWordMatcher;
        private readonly IQueryPatternAnalyzerService _queryPatternAnalyzer;
        private readonly IChunkPrioritizerService _chunkPrioritizer;
        private readonly IDocumentService _documentService;
        private readonly IQueryAnalysisService _queryAnalysis;
        private readonly IResponseBuilderService _responseBuilder;
        private readonly IQueryStrategyOrchestratorService _strategyOrchestrator;
        private readonly IQueryStrategyExecutorService _strategyExecutor;
        private readonly IDocumentSearchStrategyService _documentSearchStrategy;

        /// <summary>
        /// Initializes a new instance of the DocumentSearchService
        /// </summary>
        /// <param name="documentRepository">Repository for document operations</param>
        /// <param name="aiService">AI service for text generation</param>
        /// <param name="options">SmartRAG configuration options</param>
        /// <param name="logger">Logger instance for this service</param>
        /// <param name="queryIntentAnalyzer">Service for analyzing database query intent</param>
        /// <param name="conversationManager">Service for managing conversation sessions and history</param>
        /// <param name="queryIntentClassifier">Service for classifying query intent</param>
        /// <param name="promptBuilder">Service for building AI prompts</param>
        /// <param name="documentScoring">Service for scoring document chunks</param>
        /// <param name="sourceBuilder">Service for building search sources</param>
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
        public DocumentSearchService(
            IDocumentRepository documentRepository,
            IAIService aiService,
            IOptions<SmartRagOptions> options,
            ILogger<DocumentSearchService> logger,
            IResponseBuilderService responseBuilder,
            IQueryIntentAnalyzer queryIntentAnalyzer,
            IContextExpansionService contextExpansion,
            IMcpIntegrationService mcpIntegration,
            IDocumentRelevanceCalculatorService relevanceCalculator,
            IQueryWordMatcherService queryWordMatcher,
            IQueryPatternAnalyzerService queryPatternAnalyzer,
            IChunkPrioritizerService chunkPrioritizer,
            IDocumentService documentService,
            IQueryAnalysisService queryAnalysis,
            IQueryStrategyOrchestratorService strategyOrchestrator,
            IQueryStrategyExecutorService strategyExecutor,
            IDocumentSearchStrategyService documentSearchStrategy,
            IConversationManagerService? conversationManager = null,
            IQueryIntentClassifierService? queryIntentClassifier = null,
            IPromptBuilderService? promptBuilder = null,
            IDocumentScoringService? documentScoring = null,
            ISourceBuilderService? sourceBuilder = null)
        {
            _documentRepository = documentRepository;
            _aiService = aiService;
            _options = options.Value;
            _logger = logger;
            _queryIntentAnalyzer = queryIntentAnalyzer ?? throw new ArgumentNullException(nameof(queryIntentAnalyzer));
            _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
            _queryIntentClassifier = queryIntentClassifier ?? throw new ArgumentNullException(nameof(queryIntentClassifier));
            _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
            _documentScoring = documentScoring ?? throw new ArgumentNullException(nameof(documentScoring));
            _sourceBuilder = sourceBuilder ?? throw new ArgumentNullException(nameof(sourceBuilder));
            _contextExpansion = contextExpansion ?? throw new ArgumentNullException(nameof(contextExpansion));
            _mcpIntegration = mcpIntegration ?? throw new ArgumentNullException(nameof(mcpIntegration));
            _relevanceCalculator = relevanceCalculator ?? throw new ArgumentNullException(nameof(relevanceCalculator));
            _queryWordMatcher = queryWordMatcher ?? throw new ArgumentNullException(nameof(queryWordMatcher));
            _queryPatternAnalyzer = queryPatternAnalyzer ?? throw new ArgumentNullException(nameof(queryPatternAnalyzer));
            _chunkPrioritizer = chunkPrioritizer ?? throw new ArgumentNullException(nameof(chunkPrioritizer));
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
            _queryAnalysis = queryAnalysis ?? throw new ArgumentNullException(nameof(queryAnalysis));
            _responseBuilder = responseBuilder ?? throw new ArgumentNullException(nameof(responseBuilder));
            _strategyOrchestrator = strategyOrchestrator ?? throw new ArgumentNullException(nameof(strategyOrchestrator));
            _strategyExecutor = strategyExecutor ?? throw new ArgumentNullException(nameof(strategyExecutor));
            _documentSearchStrategy = documentSearchStrategy ?? throw new ArgumentNullException(nameof(documentSearchStrategy));
        }

        /// <summary>
        /// [Document Query] Searches for relevant document chunks based on the query
        /// </summary>
        /// <param name="query">Search query string (supports tags: -d, -db, -i, -a, -mcp)</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <param name="queryTokens">Pre-computed query tokens (optional, for performance)</param>
        /// <returns>List of relevant document chunks</returns>
        public async Task<List<DocumentChunk>> SearchDocumentsAsync(
            string query,
            int maxResults = 5,
            List<string>? queryTokens = null)
        {
            var (cleanedQuery, searchOptions) = ParseSourceTags(query);
            query = cleanedQuery;

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            var searchResults = await _documentSearchStrategy.SearchDocumentsAsync(query, maxResults * InitialSearchMultiplier, searchOptions, queryTokens);

            return searchResults.Take(maxResults).ToList();
        }

        /// <summary>
        /// [AI Query] Process intelligent query with RAG and automatic session management
        /// Unified approach: searches across documents, images, audio, and databases
        /// </summary>
        /// <param name="query">User query to process (supports tags: -d, -db, -i, -a, -mcp)</param>
        /// <param name="maxResults">Maximum number of document chunks to use</param>
        /// <param name="startNewConversation">Whether to start a new conversation session</param>
        /// <returns>RAG response with answer and sources from all available data sources</returns>
        public async Task<RagResponse> QueryIntelligenceAsync(
            string query,
            int maxResults = 5,
            bool startNewConversation = false)
        {
            var trimmedQuery = query.Trim();
            var hasCommand = _queryIntentClassifier.TryParseCommand(trimmedQuery, out var commandType, out var commandPayload);

            if (startNewConversation || (hasCommand && commandType == QueryCommandType.NewConversation))
            {
                await _conversationManager.StartNewConversationAsync();
                return _responseBuilder.CreateRagResponse(query, "New conversation started. How can I help you?", new List<SearchSource>());
            }

            if (trimmedQuery.StartsWith("/", StringComparison.Ordinal) && !hasCommand)
            {
                return _responseBuilder.CreateRagResponse(trimmedQuery, string.Empty, new List<SearchSource>());
            }

            var (cleanedQuery, searchOptions) = ParseSourceTags(query);
            query = cleanedQuery;

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            var originalQuery = query;
            var sessionId = await _conversationManager.GetOrCreateSessionIdAsync();
            var conversationHistory = await _conversationManager.GetConversationHistoryAsync(sessionId);

            if (hasCommand && commandType == QueryCommandType.ForceConversation)
            {
                var conversationQuery = string.IsNullOrWhiteSpace(commandPayload)
                    ? originalQuery
                    : commandPayload;

                return await HandleConversationQueryAsync(conversationQuery, sessionId, conversationHistory);
            }

            var intentAnalysis = await _queryIntentClassifier.AnalyzeQueryAsync(query, conversationHistory, default);

            if (intentAnalysis.IsConversation)
            {
                return await HandleConversationQueryAsync(query, sessionId, conversationHistory);
            }

            RagResponse? response = null;
            var searchMetadata = new SearchMetadata();

            var queryTokens = intentAnalysis.Tokens.ToList();

            var (CanAnswer, Results) = await CanAnswerFromDocumentsAsync(query, searchOptions, queryTokens, default);

            QueryIntent? preAnalyzedQueryIntent = null;
            
            if (searchOptions.EnableDatabaseSearch)
            {
                preAnalyzedQueryIntent = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(query);
            }

            if (searchOptions.EnableDocumentSearch)
            {
                searchMetadata.DocumentSearchPerformed = true;
                searchMetadata.DocumentChunksFound = Results.Count;
            }

            var topScore = Results.Count > 0 ? Results.Max(r => r.RelevanceScore ?? 0) : 0;
            var hasStrongDocumentMatch = topScore > StrongDocumentMatchThreshold;

            var skipEagerDocumentAnswer = !hasStrongDocumentMatch &&
                                          preAnalyzedQueryIntent?.Confidence > SkipEagerDocumentAnswerConfidenceThreshold &&
                                          preAnalyzedQueryIntent.DatabaseQueries?.Count > 0;

            RagResponse? earlyDocumentResponse = null;
            if (searchOptions.EnableDocumentSearch && CanAnswer && Results.Count > 0 && !skipEagerDocumentAnswer)
            {
                var docRequest = CreateStrategyRequest(query, maxResults, conversationHistory, CanAnswer, searchOptions, queryTokens, preCalculatedResults: Results);
                earlyDocumentResponse = await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(docRequest);

                if (topScore >= StrongDocumentMatchThreshold && !string.IsNullOrWhiteSpace(earlyDocumentResponse.Answer))
                {
                    var isFailure = _responseBuilder.IsExplicitlyNegative(earlyDocumentResponse.Answer);

                    if (!isFailure)
                    {
                        earlyDocumentResponse.SearchMetadata = searchMetadata;
                        return earlyDocumentResponse;
                    }
                }

                if (!_responseBuilder.IndicatesMissingData(earlyDocumentResponse.Answer, query))
                {
                    earlyDocumentResponse.SearchMetadata = searchMetadata;
                    return earlyDocumentResponse;
                }

                if (!searchOptions.EnableDatabaseSearch)
                {
                    var fallbackAnswer = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory);
                    var fallbackResponse = _responseBuilder.CreateRagResponse(
                        query,
                        fallbackAnswer,
                        new List<SearchSource>(),
                        searchMetadata);
                    return fallbackResponse;
                }
            }

            if (searchOptions.EnableDatabaseSearch)
            {
                try
                {
                    QueryIntent? queryIntent = preAnalyzedQueryIntent;
                    if (queryIntent == null)
                    {
                        queryIntent = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(query);
                    }

                    var hasDatabaseQueries = queryIntent?.DatabaseQueries != null && queryIntent.DatabaseQueries.Count > 0;
                    var confidence = queryIntent?.Confidence ?? 0.0;

                    var strategy = _strategyOrchestrator.DetermineQueryStrategy(confidence, hasDatabaseQueries, CanAnswer);

                    bool? hasDatabaseQueriesForRequest = strategy == QueryStrategy.Hybrid ? (bool?)hasDatabaseQueries : null;
                    
                    var strategyRequest = CreateStrategyRequest(
                        query, maxResults, conversationHistory, CanAnswer, searchOptions, queryTokens,
                        queryIntent: queryIntent,
                        preCalculatedResults: Results,
                        hasDatabaseQueries: hasDatabaseQueriesForRequest);

                    response = strategy switch
                    {
                        QueryStrategy.DatabaseOnly => await _strategyExecutor.ExecuteDatabaseOnlyStrategyAsync(strategyRequest),
                        QueryStrategy.DocumentOnly => earlyDocumentResponse ?? await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(strategyRequest),
                        QueryStrategy.Hybrid => await _strategyExecutor.ExecuteHybridStrategyAsync(strategyRequest),
                        _ => earlyDocumentResponse ?? await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(strategyRequest)
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
                    response = earlyDocumentResponse;
                    if (response == null)
                    {
                        var docRequest = CreateStrategyRequest(query, maxResults, conversationHistory, CanAnswer, searchOptions, queryTokens, preCalculatedResults: Results);
                        response = await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(docRequest);
                    }
                }

                if (response != null && response.SearchMetadata == null)
                {
                    response.SearchMetadata = searchMetadata;
                }
            }
            else
            {
                if (searchOptions.EnableDocumentSearch)
                {
                    var docRequest = CreateStrategyRequest(query, maxResults, conversationHistory, CanAnswer, searchOptions, queryTokens, preCalculatedResults: Results);
                    response = await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(docRequest);

                    if (response != null && response.SearchMetadata == null)
                    {
                        response.SearchMetadata = searchMetadata;
                    }
                }
                else
                {
                    if (searchOptions.EnableMcpSearch)
                    {
                        response = await ExecuteMcpSearchAsync(query, maxResults, conversationHistory, searchMetadata, existingResponse: null);
                        if (response != null)
                        {
                            await _conversationManager.AddToConversationAsync(sessionId, query, response.Answer);
                            return response;
                        }
                    }
                    else
                    {
                        var chatResponse = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory);
                        response = _responseBuilder.CreateRagResponse(query, chatResponse, new List<SearchSource>());
                    }
                }
            }

            // Skip MCP search if database response has meaningful data AND answer is sufficient
            // CASCADE: If database response indicates missing data, proceed to MCP search
            // Also skip if MCP search was already performed in the else block above
            var hasMeaningfulDatabaseData = response != null &&
                (response.Sources?.Any(s => s.SourceType == "Database") ?? false) &&
                (!string.IsNullOrWhiteSpace(response.Answer) || (response.Sources?.Any(s => s.SourceType == "Database" && !string.IsNullOrWhiteSpace(s.RelevantContent)) ?? false));

            var databaseAnswerIsSufficient = hasMeaningfulDatabaseData && response != null &&
                !_responseBuilder!.IndicatesMissingData(response.Answer, query);

            var mcpAlreadyPerformed = searchMetadata.McpSearchPerformed;

            if (searchOptions.EnableMcpSearch && !mcpAlreadyPerformed)
            {
                var mcpResponse = await ExecuteMcpSearchAsync(query, maxResults, conversationHistory, searchMetadata, existingResponse: response);
                if (mcpResponse != null)
                {
                    response = mcpResponse;
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

            return await _responseBuilder!.CreateFallbackResponseAsync(query, conversationHistory);
        }

        /// <summary>
        /// Parses source tags from query and adjusts SearchOptions accordingly
        /// Tags: -d (document), -db (database), -mcp (MCP), -a (audio), -i (image)
        /// Returns cleaned query without tags
        /// </summary>
        private (string cleanedQuery, SearchOptions adjustedOptions) ParseSourceTags(string query)
        {
            var options = SearchOptions.FromConfig(_options);
            var cleanedQuery = query.TrimEnd();

            var tagHandlers = new[]
            {
                (Pattern: DocumentTagPattern, Factory: (Func<SearchOptions, SearchOptions>)SearchOptions.CreateDocumentOnly),
                (Pattern: DatabaseTagPattern, Factory: SearchOptions.CreateDatabaseOnly),
                (Pattern: McpTagPattern, Factory: SearchOptions.CreateMcpOnly),
                (Pattern: AudioTagPattern, Factory: SearchOptions.CreateAudioOnly),
                (Pattern: ImageTagPattern, Factory: SearchOptions.CreateImageOnly)
            };

            foreach (var (pattern, factory) in tagHandlers)
            {
                var match = Regex.Match(cleanedQuery, CreateTagPatternWithOptionalPunctuation(pattern), TagRegexOptions);
                if (match.Success)
                {
                    var adjustedOptions = factory(options);
                    cleanedQuery = cleanedQuery.Substring(0, match.Index).TrimEnd();
                    return (cleanedQuery, adjustedOptions);
                }
            }

            return (cleanedQuery, options);
        }


        /// <summary>
        /// Creates a tag pattern that matches both with and without punctuation prefix
        /// </summary>
        private static string CreateTagPatternWithOptionalPunctuation(string baseTagPattern)
        {
            return $@"(?:{PunctuationPrefix})?{baseTagPattern}";
        }

        /// <summary>
        /// Generates RAG answer with automatic session management and context expansion
        /// </summary>
        /// <param name="request">Request containing query parameters</param>
        /// <returns>RAG response with answer and sources</returns>
        public async Task<RagResponse> GenerateBasicRagAnswerAsync(Models.RequestResponse.GenerateRagAnswerRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var searchMaxResults = _queryAnalysis.DetermineInitialSearchCount(request.Query, request.MaxResults);

            List<DocumentChunk> chunks;
            var queryTokens = request.QueryTokens ?? QueryTokenizer.TokenizeQuery(request.Query);

            DocumentChunk? preservedChunk0 = null;
            List<Entities.Document>? allDocuments = null;

            if (request.PreCalculatedResults != null && request.PreCalculatedResults.Count > 0)
            {
                var filteredPreCalculatedResults = request.PreCalculatedResults;
                if (request.Options != null)
                {
                    allDocuments = await _documentService.GetAllDocumentsFilteredAsync(request.Options);
                    var allowedDocIds = new HashSet<Guid>(allDocuments.Select(d => d.Id));
                    filteredPreCalculatedResults = request.PreCalculatedResults.Where(c => allowedDocIds.Contains(c.DocumentId)).ToList();
                }

                chunks = filteredPreCalculatedResults
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .ThenBy(c => c.ChunkIndex)
                    .ToList();
            }
            else
            {
                chunks = await SearchDocumentsAsync(request.Query, searchMaxResults, request.QueryTokens);
                preservedChunk0 = chunks.FirstOrDefault(c => c.ChunkIndex == 0);
                var nonZeroChunksForSearch = chunks.Where(c => c.ChunkIndex != 0).ToList();
                chunks = _chunkPrioritizer.PrioritizeChunksByQueryWords(nonZeroChunksForSearch, queryTokens);
                chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(chunks, preservedChunk0);
            }

            var topOriginalChunks = chunks
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .Take(Math.Min(chunks.Count, Math.Max(10, request.MaxResults * 2)))
                .ToList();

            var originalChunkIds = new HashSet<Guid>(topOriginalChunks.Select(c => c.Id));

            var needsAggressiveSearch = chunks.Count < 5 || _queryPatternAnalyzer.RequiresComprehensiveSearch(request.Query);
            if (needsAggressiveSearch)
            {
                preservedChunk0 ??= chunks.FirstOrDefault(c => c.ChunkIndex == 0);

                allDocuments = await EnsureAllDocumentsLoadedAsync(allDocuments, request.Options);
                var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();
                var queryWords = queryTokens;
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
                    var numberedListWithQueryWords = GetFilteredAndSortedNumberedListChunks(
                        numberedListChunks, queryWords, hasQueryWords: true, takeCount: searchMaxResults * 3);

                    var numberedListOnly = GetFilteredAndSortedNumberedListChunks(
                        numberedListChunks, queryWords, hasQueryWords: false, takeCount: searchMaxResults * 2);

                    var queryWordsOnly = _chunkPrioritizer.PrioritizeChunksByQueryWords(
                        allChunks.Where(c => !_queryPatternAnalyzer.DetectNumberedLists(c.Content)).ToList(),
                        queryWords)
                        .Take(searchMaxResults * 2)
                        .ToList();

                    var mergedChunks = new List<DocumentChunk>();
                    var seenIds = new HashSet<Guid>();

                    foreach (var chunk in numberedListWithQueryWords.Concat(numberedListOnly).Concat(queryWordsOnly).Concat(chunks))
                    {
                        if (!seenIds.Contains(chunk.Id) && mergedChunks.Count < searchMaxResults * 4)
                        {
                            mergedChunks.Add(chunk);
                            seenIds.Add(chunk.Id);
                        }
                    }

                    mergedChunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(mergedChunks, preservedChunk0);

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

            chunks = FilterChunksByOriginalIds(chunks, originalChunkIds);

            if (chunks.Count > 0)
            {
                // Calculate adaptive threshold based on score range
                // Vector search scores: 0.0-1.0 range, Text search scores: 4.0-6.0+ range
                var maxScore = chunks.Max(c => c.RelevanceScore ?? 0.0);
                double contextExpansionThreshold;

                if (maxScore > 3.0)
                {
                    contextExpansionThreshold = DocumentBoostThreshold;
                }
                else
                {
                    var sortedByScore = chunks.OrderByDescending(c => c.RelevanceScore ?? 0.0).ToList();
                    var topPercentileCount = Math.Max(1, (int)(sortedByScore.Count * 0.4));
                    contextExpansionThreshold = Math.Max(0.01, sortedByScore.Skip(topPercentileCount - 1).FirstOrDefault()?.RelevanceScore ?? 0.01);
                }

                var relevantDocumentChunks = chunks
                    .Where(c => (c.RelevanceScore ?? 0.0) >= contextExpansionThreshold)
                    .ToList();

                var otherChunks = chunks
                    .Where(c => (c.RelevanceScore ?? 0.0) < contextExpansionThreshold)
                    .ToList();

                if (relevantDocumentChunks.Count > 0)
                {
                    var originalScores = relevantDocumentChunks.ToDictionary(c => c.Id, c => c.RelevanceScore ?? 0.0);

                    var contextWindow = _contextExpansion.DetermineContextWindow(relevantDocumentChunks, request.Query);
                    var expandedChunks = await _contextExpansion.ExpandContextAsync(relevantDocumentChunks, contextWindow);

                    var queryWords = queryTokens;

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
                            var content = chunk.Content.ToLowerInvariant();
                            var wordMatches = queryWords.Count(word =>
                                content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

                            var expandedScore = wordMatches * 0.05;
                            var maxAllowedScore = minOriginalScore > 0 ? minOriginalScore * 0.3 : maxOriginalScore * 0.05;
                            chunk.RelevanceScore = Math.Min(expandedScore, maxAllowedScore);
                        }
                    }

                    chunks = expandedChunks
                        .OrderByDescending(c => originalChunkIds != null && originalChunkIds.Contains(c.Id))
                        .ThenByDescending(c => c.ChunkIndex == 0)
                        .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                        .ThenBy(c => c.ChunkIndex)
                        .Concat(otherChunks
                            .OrderByDescending(c => c.ChunkIndex == 0)
                            .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                            .ThenBy(c => c.ChunkIndex))
                        .ToList();

                    if (chunks.Count > MaxExpandedChunks)
                    {
                        chunks = chunks.Take(MaxExpandedChunks).ToList();
                        ServiceLogMessages.LogContextExpansionLimited(_logger, MaxExpandedChunks, null);
                    }
                }
                else
                {
                    chunks = _chunkPrioritizer.PrioritizeChunksByRelevanceScore(chunks);
                }
            }

            if (preservedChunk0 == null)
            {
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

            chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(chunks, preservedChunk0);

            var documentChunks = chunks.Where(c => c.DocumentType == "Document" || c.DocumentType == "Audio").ToList();
            var imageChunks = chunks.Where(c => c.DocumentType == "Image").ToList();

            chunks = documentChunks
                .OrderByDescending(c => originalChunkIds != null && originalChunkIds.Contains(c.Id))
                .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                .Concat(imageChunks
                    .OrderByDescending(c => originalChunkIds != null && originalChunkIds.Contains(c.Id))
                    .ThenByDescending(c => c.RelevanceScore ?? 0.0))
                .ToList();

            var context = _contextExpansion.BuildLimitedContext(chunks, MaxContextSize);

            var prompt = _promptBuilder.BuildDocumentRagPrompt(request.Query, context, request.ConversationHistory);
            var answer = await _aiService.GenerateResponseAsync(prompt, new List<string> { context });

            var sourcesChunks = FilterChunksByOriginalIds(chunks, originalChunkIds);

            var sources = await _sourceBuilder.BuildSourcesAsync(sourcesChunks, _documentRepository);
            return _responseBuilder.CreateRagResponse(request.Query, answer, sources);
        }

        /// <summary>
        /// Ensures all documents are loaded, loading them if necessary
        /// </summary>
        private async Task<List<Entities.Document>> EnsureAllDocumentsLoadedAsync(List<Entities.Document>? allDocuments, SearchOptions? options)
        {
            if (allDocuments == null)
            {
                return await _documentService.GetAllDocumentsFilteredAsync(options);
            }
            return allDocuments;
        }

        /// <summary>
        /// Filters chunks to include only those from the original search results
        /// </summary>
        private List<DocumentChunk> FilterChunksByOriginalIds(List<DocumentChunk> chunks, HashSet<Guid>? originalChunkIds)
        {
            if (originalChunkIds == null || originalChunkIds.Count == 0)
            {
                return chunks;
            }

            var filteredChunks = chunks.Where(c => originalChunkIds.Contains(c.Id)).ToList();

            if (filteredChunks.Count > 0)
            {
                return filteredChunks;
            }

            return chunks;
        }

        /// <summary>
        /// Filters and sorts numbered list chunks based on query word matching
        /// </summary>
        private List<DocumentChunk> GetFilteredAndSortedNumberedListChunks(
            List<DocumentChunk> numberedListChunks,
            List<string> queryWords,
            bool hasQueryWords,
            int takeCount)
        {
            return numberedListChunks
                .Where(c => _queryPatternAnalyzer.DetectNumberedLists(c.Content) &&
                    (hasQueryWords
                        ? queryWords.Any(word => c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                        : !queryWords.Any(word => c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)))
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .ThenByDescending(c => _queryPatternAnalyzer.CountNumberedListItems(c.Content))
                .Take(takeCount)
                .ToList();
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
            return await GenerateBasicRagAnswerAsync(request);
        }


        /// <summary>
        /// Determines if a query can be answered from documents using language-agnostic content-based analysis
        /// </summary>
        /// <param name="query">User query to analyze</param>
        /// <param name="searchOptions">Search options</param>
        /// <param name="queryTokens">Pre-computed query tokens for performance</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Tuple containing whether documents can answer and the found chunks</returns>
        public async Task<(bool CanAnswer, List<DocumentChunk> Results)> CanAnswerFromDocumentsAsync(string query, SearchOptions searchOptions, List<string>? queryTokens = null, System.Threading.CancellationToken cancellationToken = default)
        {
            if (!searchOptions.EnableDocumentSearch)
            {
                return (false, new List<DocumentChunk>());
            }

            try
            {

                var searchResults = await _documentSearchStrategy.SearchDocumentsAsync(query, FallbackSearchMaxResults, searchOptions, queryTokens);

                if (searchResults.Count == MinSearchResultsCount)
                {
                    return (false, searchResults);
                }

                var sortedByScore = searchResults.OrderByDescending(c => c.RelevanceScore ?? 0.0).ToList();
                var maxScore = sortedByScore.FirstOrDefault()?.RelevanceScore ?? 0.0;

                double adaptiveThreshold;

                if (maxScore > 3.0)
                {
                    var topPercentileCount = Math.Max(1, (int)(sortedByScore.Count * 0.7));
                    var percentileScore = sortedByScore.Skip(topPercentileCount - 1).FirstOrDefault()?.RelevanceScore ?? 4.0;

                    var minScore = sortedByScore.LastOrDefault()?.RelevanceScore ?? 0.0;
                    var scoreRange = maxScore - minScore;
                    if (scoreRange < 0.5 && sortedByScore.Count > 1)
                    {
                        adaptiveThreshold = Math.Max(4.5, maxScore - 0.5);
                    }
                    else
                    {
                        adaptiveThreshold = Math.Max(4.0, percentileScore - 0.01);
                    }
                }
                else
                {
                    var topPercentileCount = Math.Max(1, (int)(sortedByScore.Count * 0.4));
                    adaptiveThreshold = Math.Max(0.01, sortedByScore.Skip(topPercentileCount - 1).FirstOrDefault()?.RelevanceScore ?? 0.01);
                }

                var chunk0FromSearch = searchResults.FirstOrDefault(c => c.ChunkIndex == 0);

                var hasRelevantContent = searchResults.Any(chunk =>
                    (chunk.RelevanceScore ?? 0.0) >= adaptiveThreshold || chunk.ChunkIndex == 0);

                if (!hasRelevantContent)
                {
                    return (false, searchResults);
                }

                var totalContentLength = searchResults
                    .Where(c => (c.RelevanceScore ?? 0.0) >= adaptiveThreshold || c.ChunkIndex == 0)
                    .Sum(c => c.Content.Length);

                var hasSubstantialContent = totalContentLength > MinSubstantialContentLength;

                var filteredResults = searchResults
                    .Where(c => (c.RelevanceScore ?? 0.0) >= adaptiveThreshold || c.ChunkIndex == 0)
                    .ToList();

                if (chunk0FromSearch != null && !filteredResults.Any(c => c.ChunkIndex == 0))
                {
                    filteredResults = new List<DocumentChunk> { chunk0FromSearch }.Concat(filteredResults).ToList();
                }

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

        private async Task<RagResponse> HandleConversationQueryAsync(string query, string sessionId, string? conversationHistory)
        {
            var conversationAnswer = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory);
            await _conversationManager.AddToConversationAsync(sessionId, query, conversationAnswer);
            return _responseBuilder.CreateRagResponse(query, conversationAnswer, new List<SearchSource>());
        }

        private Models.RequestResponse.QueryStrategyRequest CreateStrategyRequest(
            string query,
            int maxResults,
            string conversationHistory,
            bool? canAnswer,
            SearchOptions options,
            List<string>? queryTokens,
            QueryIntent? queryIntent = null,
            List<DocumentChunk>? preCalculatedResults = null,
            bool? hasDatabaseQueries = null)
        {
            return new Models.RequestResponse.QueryStrategyRequest
            {
                Query = query,
                MaxResults = maxResults,
                ConversationHistory = conversationHistory,
                CanAnswerFromDocuments = canAnswer,
                HasDatabaseQueries = hasDatabaseQueries,
                QueryIntent = queryIntent,
                PreferredLanguage = _options.DefaultLanguage,
                Options = options,
                PreCalculatedResults = preCalculatedResults,
                QueryTokens = queryTokens
            };
        }

        private async Task<RagResponse?> ExecuteMcpSearchAsync(
            string query,
            int maxResults,
            string conversationHistory,
            SearchMetadata searchMetadata,
            RagResponse? existingResponse)
        {
            try
            {
                var mcpResults = await _mcpIntegration.QueryWithMcpAsync(query, maxResults, conversationHistory);
                searchMetadata.McpSearchPerformed = true;
                searchMetadata.McpResultsFound = mcpResults?.Count(r => r.IsSuccess && !string.IsNullOrWhiteSpace(r.Content)) ?? 0;

                if (mcpResults == null || mcpResults.Count == 0)
                {
                    return existingResponse;
                }

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

                if (mcpSources.Count == 0)
                {
                    return existingResponse;
                }

                var mcpContext = string.Join("\n\n", mcpResults.Where(r => r.IsSuccess).Select(r => r.Content));

                if (existingResponse != null)
                {
                    if (existingResponse.Sources == null)
                    {
                        existingResponse.Sources = new List<SearchSource>();
                    }
                    existingResponse.Sources.AddRange(mcpSources);

                    if (!string.IsNullOrWhiteSpace(mcpContext))
                    {
                        var existingContext = existingResponse.Sources
                            .Where(s => s.SourceType != "MCP")
                            .Select(s => s.RelevantContent)
                            .Where(c => !string.IsNullOrWhiteSpace(c))
                            .ToList();

                        var combinedContext = existingContext.Count > 0
                            ? string.Join("\n\n", existingContext) + "\n\n[MCP Results]\n" + mcpContext
                            : mcpContext;

                        var mergedPrompt = _promptBuilder.BuildDocumentRagPrompt(query, combinedContext, conversationHistory);
                        var mergedAnswer = await _aiService.GenerateResponseAsync(mergedPrompt, new List<string> { combinedContext });
                        if (!string.IsNullOrWhiteSpace(mergedAnswer))
                        {
                            existingResponse.Answer = mergedAnswer;
                        }
                    }

                    return existingResponse;
                }

                if (string.IsNullOrWhiteSpace(mcpContext))
                {
                    var chatResponse = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory);
                    return _responseBuilder.CreateRagResponse(query, chatResponse, new List<SearchSource>(), searchMetadata);
                }

                var mcpPrompt = _promptBuilder.BuildDocumentRagPrompt(query, mcpContext, conversationHistory);
                var mcpAnswer = await _aiService.GenerateResponseAsync(mcpPrompt, new List<string> { mcpContext });
                if (!string.IsNullOrWhiteSpace(mcpAnswer))
                {
                    return _responseBuilder.CreateRagResponse(query, mcpAnswer, mcpSources, searchMetadata);
                }

                var fallbackAnswer = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory);
                return _responseBuilder.CreateRagResponse(query, fallbackAnswer, mcpSources, searchMetadata);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error querying MCP servers");
                return existingResponse;
            }
        }

    }
}