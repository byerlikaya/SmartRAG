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
using SmartRAG.Helpers;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for document search and RAG (Retrieval-Augmented Generation) operations
    /// </summary>
    public class DocumentSearchService : IDocumentSearchService, IRagAnswerGeneratorService
    {
        private const int MinSearchResultsCount = 0;
        private const int FallbackSearchMaxResults = 30;
        private const int MinSubstantialContentLength = 30;
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
        private const int PreviousQuerySearchMaxResults = 15;
        private const double PreviousQueryChunkScoreBoost = 0.5;

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
            IDocumentRelevanceCalculatorService relevanceCalculator,
            IQueryWordMatcherService queryWordMatcher,
            IQueryPatternAnalyzerService queryPatternAnalyzer,
            IChunkPrioritizerService chunkPrioritizer,
            IDocumentService documentService,
            IQueryAnalysisService queryAnalysis,
            IQueryStrategyOrchestratorService strategyOrchestrator,
            IQueryStrategyExecutorService strategyExecutor,
            IDocumentSearchStrategyService documentSearchStrategy,
            IMcpIntegrationService mcpIntegration,
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
        /// [AI Query] Process intelligent query with RAG and automatic session management
        /// Unified approach: searches across documents, images, audio, and databases
        /// </summary>
        public async Task<RagResponse> QueryIntelligenceAsync(
            string query,
            int maxResults = 5,
            bool startNewConversation = false,
            CancellationToken cancellationToken = default)
        {
            if (startNewConversation)
            {
                await _conversationManager.StartNewConversationAsync(cancellationToken);
                return _responseBuilder.CreateRagResponse(query, "New conversation started. How can I help you?", new List<SearchSource>());
            }
            var sessionId = await _conversationManager.GetOrCreateSessionIdAsync(cancellationToken);
            var conversationHistory = await _conversationManager.GetConversationHistoryAsync(sessionId, cancellationToken);
            return await QueryIntelligenceCoreAsync(query, maxResults, sessionId, conversationHistory, cancellationToken);
        }

        /// <summary>
        /// [AI Query] Process intelligent query with RAG using explicit session context
        /// </summary>
        public async Task<RagResponse> QueryIntelligenceAsync(
            string query,
            int maxResults,
            string sessionId,
            string conversationHistory,
            CancellationToken cancellationToken = default)
        {
            return await QueryIntelligenceCoreAsync(query, maxResults, sessionId, conversationHistory, cancellationToken);
        }

        private async Task<RagResponse> QueryIntelligenceCoreAsync(
            string query,
            int maxResults,
            string sessionId,
            string conversationHistory,
            CancellationToken cancellationToken)
        {
            var trimmedQuery = query.Trim();
            var hasCommand = _queryIntentClassifier.TryParseCommand(trimmedQuery, out var commandType, out var commandPayload);

            if (hasCommand && commandType == QueryCommandType.NewConversation)
            {
                await _conversationManager.StartNewConversationAsync(cancellationToken);
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

            if (hasCommand && commandType == QueryCommandType.ForceConversation)
            {
                var conversationQuery = string.IsNullOrWhiteSpace(commandPayload)
                    ? originalQuery
                    : commandPayload;

                return await HandleConversationQueryAsync(conversationQuery, sessionId, conversationHistory, cancellationToken);
            }

            if (searchOptions.EnableDocumentSearch)
            {
                var coreFilenameWords = QueryTokenizer.GetWordsForPhraseExtraction(query).Where(w => w.Length >= 4).ToList();
                var coreAllDocs = await _documentService.GetAllDocumentsFilteredAsync(searchOptions, cancellationToken);
                var coreFilenameMatched = coreAllDocs
                    .Where(d =>
                    {
                        var fn = (d.FileName ?? string.Empty).ToLowerInvariant();
                        if (string.IsNullOrWhiteSpace(fn))
                            return false;
                        return coreFilenameWords.Any(w =>
                        {
                            var wl = w.ToLowerInvariant();
                            if (fn.IndexOf(wl, StringComparison.OrdinalIgnoreCase) >= 0)
                                return true;
                            for (var len = 4; len < wl.Length; len++)
                            {
                                if (fn.IndexOf(wl.Substring(0, len), StringComparison.OrdinalIgnoreCase) >= 0)
                                    return true;
                            }
                            return false;
                        });
                    })
                    .ToList();
                if (coreFilenameMatched.Count > 0)
                {
                    var coreChunks = await LoadChunksFromEntityMatchedDocumentsAsync(
                        coreFilenameMatched.Select(d => d.Id).ToHashSet(),
                        80,
                        cancellationToken);
                    if (coreChunks.Count > 0)
                    {
                        foreach (var c in coreChunks)
                            c.RelevanceScore = DocumentBoostThreshold + 2.0;
                        var coreContext = _contextExpansion.BuildLimitedContext(coreChunks, MaxContextSize);
                        var coreAnswer = await GenerateRagAnswerFromContextAsync(query, coreContext, conversationHistory, cancellationToken);
                        var coreSources = await _sourceBuilder.BuildSourcesAsync(coreChunks, _documentRepository);
                        var coreResponse = _responseBuilder.CreateRagResponse(query, coreAnswer, coreSources);
                        await _conversationManager.AddToConversationAsync(sessionId, query, coreResponse.Answer, cancellationToken);
                        return coreResponse;
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            var intentAnalysis = await _queryIntentClassifier.AnalyzeQueryAsync(query, conversationHistory, cancellationToken);

            if (intentAnalysis.IsConversation)
            {
                // If answer is already provided by the intent classifier, use it directly to avoid redundant LLM call
                if (string.IsNullOrWhiteSpace(intentAnalysis.Answer))
                    return await HandleConversationQueryAsync(query, sessionId, conversationHistory, cancellationToken);
              
                await _conversationManager.AddToConversationAsync(sessionId, query, intentAnalysis.Answer, cancellationToken);
                return _responseBuilder.CreateRagResponse(query, intentAnalysis.Answer, new List<SearchSource>());
            }

            RagResponse? response = null;
            var searchMetadata = new SearchMetadata();

            var queryTokens = intentAnalysis.Tokens.ToList();

            cancellationToken.ThrowIfCancellationRequested();
            var (CanAnswer, Results) = await CanAnswerFromDocumentsAsync(query, searchOptions, queryTokens, cancellationToken);

            QueryIntent? preAnalyzedQueryIntent = null;
            
            if (searchOptions.EnableDatabaseSearch)
            {
                preAnalyzedQueryIntent = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(query, cancellationToken);
            }

            if (searchOptions.EnableDocumentSearch)
            {
                searchMetadata.DocumentSearchPerformed = true;
                searchMetadata.DocumentChunksFound = Results.Count;
            }

            var topScore = Results.Count > 0 ? Results.Max(r => r.RelevanceScore ?? 0) : 0;
            var hasStrongDocumentMatch = topScore > StrongDocumentMatchThreshold;

            var hasHighConfidenceForSkip = preAnalyzedQueryIntent?.Confidence > SkipEagerDocumentAnswerConfidenceThreshold;
            var skipEagerDocumentAnswer = !hasStrongDocumentMatch && hasHighConfidenceForSkip;

            RagResponse? earlyDocumentResponse = null;
            if (searchOptions.EnableDocumentSearch && CanAnswer && Results.Count > 0 && !skipEagerDocumentAnswer)
            {
                var docRequest = CreateStrategyRequest(query, maxResults, conversationHistory, CanAnswer, searchOptions, queryTokens, preCalculatedResults: Results);
                earlyDocumentResponse = await _strategyExecutor.ExecuteDocumentOnlyStrategyAsync(docRequest, cancellationToken);

                QueryIntent? queryIntentForCheck = preAnalyzedQueryIntent;
                if (queryIntentForCheck == null && searchOptions.EnableDatabaseSearch)
                {
                    queryIntentForCheck = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(query, cancellationToken);
                    preAnalyzedQueryIntent = queryIntentForCheck;
                }
                else if (queryIntentForCheck != null && 
                        (queryIntentForCheck.DatabaseQueries == null || queryIntentForCheck.DatabaseQueries.Count == 0) && 
                        searchOptions.EnableDatabaseSearch)
                {
                    queryIntentForCheck = await _queryIntentAnalyzer.AnalyzeQueryIntentAsync(query, cancellationToken);
                    preAnalyzedQueryIntent = queryIntentForCheck;
                }
                
                var indicatesMissingData = _responseBuilder.IndicatesMissingData(
                    earlyDocumentResponse.Answer,
                    query,
                    earlyDocumentResponse.Sources);

                if (!indicatesMissingData)
                {
                    earlyDocumentResponse.SearchMetadata = searchMetadata;
                    await _conversationManager.AddToConversationAsync(sessionId, query, earlyDocumentResponse.Answer, cancellationToken);
                    return earlyDocumentResponse;
                }

                var documentSourcesWithContent = earlyDocumentResponse.Sources?
                    .Where(s => s.SourceType == "Document" && !string.IsNullOrWhiteSpace(s.RelevantContent))
                    .ToList() ?? new List<SearchSource>();
                var totalSourceContentLength = documentSourcesWithContent.Sum(s => (s.RelevantContent?.Length ?? 0));

                if (documentSourcesWithContent.Count > 0 && totalSourceContentLength >= 50)
                {
                    var extractionContext = string.Join("\n\n", documentSourcesWithContent.Select(s => s.RelevantContent));
                    var extractionPrompt = _promptBuilder.BuildDocumentRagPrompt(query, extractionContext, extractionRetryMode: true);
                    var retryAnswer = await _aiService.GenerateResponseAsync(extractionPrompt, new List<string> { extractionContext }, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(retryAnswer) &&
                        !_responseBuilder.IndicatesMissingData(retryAnswer, query, earlyDocumentResponse.Sources))
                    {
                        var retryResponse = _responseBuilder.CreateRagResponse(query, retryAnswer.Trim(), earlyDocumentResponse.Sources ?? new List<SearchSource>(), searchMetadata);
                        await _conversationManager.AddToConversationAsync(sessionId, query, retryResponse.Answer, cancellationToken);
                        return retryResponse;
                    }
                }

                if (!searchOptions.EnableDatabaseSearch)
                {
                    var fallbackAnswer = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, cancellationToken);
                    var fallbackResponse = _responseBuilder.CreateRagResponse(
                        query,
                        fallbackAnswer,
                        new List<SearchSource>(),
                        searchMetadata);
                    await _conversationManager.AddToConversationAsync(sessionId, query, fallbackResponse.Answer, cancellationToken);
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

                    var confidence = queryIntent?.Confidence ?? 0.0;
                    var hasDatabaseQueries = queryIntent?.DatabaseQueries != null && queryIntent.DatabaseQueries.Count > 0;

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
                    _logger?.LogError(ex, "Error during query intent analysis, falling back to document-only query");
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
                        response = await ExecuteMcpSearchAsync(query, maxResults, conversationHistory, searchMetadata, existingResponse: null, cancellationToken);
                        if (response != null)
                        {
                            await _conversationManager.AddToConversationAsync(sessionId, query, response.Answer, cancellationToken);
                            return response;
                        }
                    }
                    else
                    {
                        var chatResponse = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, cancellationToken);
                        response = _responseBuilder.CreateRagResponse(query, chatResponse, new List<SearchSource>());
                    }
                }
            }

            var hasMeaningfulDatabaseData = response != null &&
                (response.Sources?.Any(s => s.SourceType == "Database") ?? false) &&
                (!string.IsNullOrWhiteSpace(response.Answer) || (response.Sources?.Any(s => s.SourceType == "Database" && !string.IsNullOrWhiteSpace(s.RelevantContent)) ?? false));

            // If database has meaningful data, consider answer sufficient (don't check IndicatesMissingData)
            // Database results exist, so we don't need MCP search even if AI gives wrong answer
            var databaseAnswerIsSufficient = hasMeaningfulDatabaseData && response != null &&
                !string.IsNullOrWhiteSpace(response.Answer);

            var hasDocumentAnswer = response != null && !string.IsNullOrWhiteSpace(response.Answer) &&
                (response.Sources?.Any(s => s.SourceType == "Document") ?? false);

            var documentAnswerIsSufficient = hasDocumentAnswer && response != null &&
                !_responseBuilder!.IndicatesMissingData(response.Answer, query);

            var answerIsSufficient = databaseAnswerIsSufficient || documentAnswerIsSufficient;

            var mcpAlreadyPerformed = searchMetadata.McpSearchPerformed;

            if (searchOptions.EnableMcpSearch && !mcpAlreadyPerformed && !answerIsSufficient)
            {
                var mcpResponse = await ExecuteMcpSearchAsync(query, maxResults, conversationHistory, searchMetadata, existingResponse: response, cancellationToken);
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

                await _conversationManager.AddToConversationAsync(sessionId, query, response.Answer, cancellationToken);

                return response;
            }

            var finalFallbackResponse = await _responseBuilder!.CreateFallbackResponseAsync(query, conversationHistory, cancellationToken);
            await _conversationManager.AddToConversationAsync(sessionId, query, finalFallbackResponse.Answer, cancellationToken);
            return finalFallbackResponse;
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
                    if (pattern == McpTagPattern)
                        adjustedOptions.EnableMcpSearch = true;
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
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>RAG response with answer and sources</returns>
        public async Task<RagResponse> GenerateBasicRagAnswerAsync(Models.RequestResponse.GenerateRagAnswerRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var trimmedQuery = request.Query?.Trim() ?? string.Empty;

            var filenameMatchWords = QueryTokenizer.GetWordsForPhraseExtraction(trimmedQuery).Where(w => w.Length >= 4).ToList();
            var allDocsForFilename = await _documentService.GetAllDocumentsFilteredAsync(request.Options, cancellationToken);
            var filenameMatchedDocs = allDocsForFilename
                .Where(d =>
                {
                    var fn = (d.FileName ?? string.Empty).ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(fn))
                        return false;
                    return filenameMatchWords.Any(w =>
                    {
                        var wl = w.ToLowerInvariant();
                        if (fn.IndexOf(wl, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                        for (var len = 4; len < wl.Length; len++)
                        {
                            if (fn.IndexOf(wl.Substring(0, len), StringComparison.OrdinalIgnoreCase) >= 0)
                                return true;
                        }
                        return false;
                    });
                })
                .ToList();
            if (filenameMatchedDocs.Count > 0 && !string.IsNullOrWhiteSpace(trimmedQuery))
            {
                var directChunks = await LoadChunksFromEntityMatchedDocumentsAsync(
                    filenameMatchedDocs.Select(d => d.Id).ToHashSet(),
                    80,
                    cancellationToken);
                if (directChunks.Count > 0)
                {
                    foreach (var c in directChunks)
                        c.RelevanceScore = DocumentBoostThreshold + 2.0;
                    var directContext = _contextExpansion.BuildLimitedContext(directChunks, MaxContextSize);
                    var directAnswer = await GenerateRagAnswerFromContextAsync(trimmedQuery, directContext, request.ConversationHistory, cancellationToken);
                    var directSources = await _sourceBuilder.BuildSourcesAsync(directChunks, _documentRepository);
                    return _responseBuilder.CreateRagResponse(trimmedQuery, directAnswer, directSources);
                }
            }

            if (!string.IsNullOrWhiteSpace(trimmedQuery))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var intentAnalysis = await _queryIntentClassifier.AnalyzeQueryAsync(trimmedQuery, request.ConversationHistory, cancellationToken).ConfigureAwait(false);
                if (intentAnalysis.IsConversation)
                {
                    var conversationAnswer = !string.IsNullOrWhiteSpace(intentAnalysis.Answer)
                        ? intentAnalysis.Answer
                        : await _conversationManager.HandleGeneralConversationAsync(trimmedQuery, request.ConversationHistory ?? string.Empty, cancellationToken).ConfigureAwait(false);
                    return _responseBuilder.CreateRagResponse(trimmedQuery, conversationAnswer ?? string.Empty, new List<SearchSource>());
                }
            }

            var (cleanedForTags, tagOptions) = ParseSourceTags(request.Query ?? string.Empty);
            if (tagOptions.EnableMcpSearch && !tagOptions.EnableDocumentSearch && !string.IsNullOrWhiteSpace(cleanedForTags))
            {
                var meta = new SearchMetadata();
                var mcpResponse = await ExecuteMcpSearchAsync(cleanedForTags, request.MaxResults, request.ConversationHistory ?? string.Empty, meta, null, cancellationToken).ConfigureAwait(false);
                if (mcpResponse != null)
                    return mcpResponse;
            }

            var queryForSearch = request.Query ?? string.Empty;
            var searchMaxResults = _queryAnalysis.DetermineInitialSearchCount(queryForSearch, request.MaxResults);

            List<DocumentChunk> chunks;
            var queryTokens = request.QueryTokens ?? QueryTokenizer.TokenizeQuery(queryForSearch);
            var previousQueryChunkIds = new HashSet<Guid>();

            DocumentChunk? preservedChunk0 = null;
            List<Entities.Document>? allDocuments = null;
            SearchOptions? effectiveOptions = request.Options;

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
                var (cleanedQuery, searchOptions) = ParseSourceTags(queryForSearch);
                effectiveOptions = searchOptions;

                if (string.IsNullOrWhiteSpace(cleanedQuery))
                    throw new ArgumentException("Query cannot be empty", nameof(request.Query));

                cancellationToken.ThrowIfCancellationRequested();
                var searchResults = await _documentSearchStrategy.SearchDocumentsAsync(cleanedQuery, searchMaxResults, searchOptions, request.QueryTokens, cancellationToken);
                chunks = searchResults.ToList();

                var previousUserQuery = GetLastUserQueryDistinctFrom(request.ConversationHistory, cleanedQuery);
                const int MinSubstantiveQueryLength = 12;
                var isSubstantivePreviousQuery = !string.IsNullOrWhiteSpace(previousUserQuery) &&
                    previousUserQuery.Trim().Length >= MinSubstantiveQueryLength &&
                    !string.Equals(previousUserQuery.Trim(), cleanedQuery.Trim(), StringComparison.OrdinalIgnoreCase);

                if (isSubstantivePreviousQuery)
                {
                    var extraResults = await _documentSearchStrategy.SearchDocumentsAsync(
                        previousUserQuery!.Trim(),
                        Math.Min(searchMaxResults, PreviousQuerySearchMaxResults),
                        searchOptions,
                        null,
                        cancellationToken);
                    var currentIds = new HashSet<Guid>(chunks.Select(c => c.Id));
                    var maxScore = chunks.Count > 0 ? chunks.Max(c => c.RelevanceScore ?? 0.0) : 0.0;
                    foreach (var c in extraResults)
                    {
                        if (!currentIds.Contains(c.Id))
                        {
                            c.RelevanceScore = maxScore + PreviousQueryChunkScoreBoost;
                            previousQueryChunkIds.Add(c.Id);
                            chunks.Add(c);
                            currentIds.Add(c.Id);
                        }
                    }
                }

                var earlyPotentialNames = QueryTokenizer.ExtractPotentialNames(queryForSearch);
                preservedChunk0 = chunks.FirstOrDefault(c => c.ChunkIndex == 0);
                if (preservedChunk0 != null && !Chunk0IsQueryRelevant(preservedChunk0, queryTokens, earlyPotentialNames))
                    preservedChunk0 = null;
                var nonZeroChunksForSearch = chunks.Where(c => c.ChunkIndex != 0).ToList();
                var earlyPhraseWords = QueryTokenizer.GetWordsForPhraseExtraction(queryForSearch);
                chunks = _chunkPrioritizer.PrioritizeChunksByQueryWords(nonZeroChunksForSearch, queryTokens, earlyPhraseWords);
                chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(chunks, preservedChunk0);

                if (previousQueryChunkIds.Count > 0)
                {
                    var previousQueryChunks = chunks.Where(c => previousQueryChunkIds.Contains(c.Id)).ToList();
                    var prevTopDocIds = previousQueryChunks
                        .GroupBy(c => c.DocumentId)
                        .Select(g => new { DocumentId = g.Key, Score = g.Sum(c => c.RelevanceScore ?? 0.0) })
                        .OrderByDescending(x => x.Score)
                        .Take(1)
                        .Select(x => x.DocumentId)
                        .ToHashSet();

                    var currentMainChunks = chunks.Where(c => !previousQueryChunkIds.Contains(c.Id)).ToList();
                    var currentTopDocId = currentMainChunks
                        .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                        .Select(c => c.DocumentId)
                        .FirstOrDefault();
                    var prevTopDocId = prevTopDocIds.FirstOrDefault();
                    var sameTopic = prevTopDocIds.Count > 0 && currentTopDocId != Guid.Empty &&
                        currentTopDocId == prevTopDocId;
                    if (sameTopic)
                    {
                        chunks = chunks.Where(c => prevTopDocIds.Contains(c.DocumentId)).ToList();
                    }
                }
            }

            var currentTopDocIdForOrdering = Guid.Empty;
            if (chunks.Count > 0)
            {
                var mainChunks = previousQueryChunkIds.Count > 0
                    ? chunks.Where(c => !previousQueryChunkIds.Contains(c.Id)).ToList()
                    : chunks;
                currentTopDocIdForOrdering = mainChunks
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .Select(c => c.DocumentId)
                    .FirstOrDefault();
            }

            var preferPreviousDoc = previousQueryChunkIds.Count > 0 && currentTopDocIdForOrdering != Guid.Empty &&
                chunks.Any(c => previousQueryChunkIds.Contains(c.Id) && c.DocumentId == currentTopDocIdForOrdering);

            var topOriginalChunks = chunks
                .OrderByDescending(c => preferPreviousDoc && previousQueryChunkIds.Contains(c.Id))
                .ThenByDescending(c => c.ChunkIndex == 0)
                .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                .ThenBy(c => c.ChunkIndex)
                .Take(Math.Min(chunks.Count, Math.Max(10, request.MaxResults * 2)))
                .ToList();

            var originalChunkIds = new HashSet<Guid>(topOriginalChunks.Select(c => c.Id));
            HashSet<Guid>? overlapChunkIds = null;
            HashSet<Guid>? entityChunkIdsFromComprehensive = null;

            var isFollowUpWithContext = previousQueryChunkIds.Count > 0;
            var requiresComprehensive = _queryPatternAnalyzer.RequiresComprehensiveSearch(queryForSearch);
            var needsAggressiveSearch = !isFollowUpWithContext &&
                (chunks.Count < 5 || requiresComprehensive);
            if (needsAggressiveSearch)
            {
                var potentialNames = QueryTokenizer.ExtractPotentialNames(queryForSearch);
                preservedChunk0 ??= chunks.FirstOrDefault(c => c.ChunkIndex == 0);
                if (preservedChunk0 != null && !Chunk0IsQueryRelevant(preservedChunk0, queryTokens, potentialNames))
                    preservedChunk0 = null;

                allDocuments = await EnsureAllDocumentsLoadedAsync(allDocuments, effectiveOptions, cancellationToken);
                var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();
                var queryWords = queryTokens;
                var scoredChunks = _documentScoring.ScoreChunks(allChunks, queryForSearch, queryWords, potentialNames);

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
                    TopChunksPerDocument,
                    queryForSearch,
                    potentialNames);

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

                if (_queryPatternAnalyzer.RequiresComprehensiveSearch(queryForSearch))
                {
                    var words = queryWords ?? new List<string>();
                    var phraseWords = QueryTokenizer.GetWordsForPhraseExtraction(queryForSearch);
                    var docIdsFromFilenameMatch = GetEntityMatchedDocumentIds(allDocuments, words, potentialNames, phraseWords);
                    var entityFileNameChunks = GetEntityFileNameChunks(allChunks, words, potentialNames, phraseWords)
                        .Take(Math.Max(searchMaxResults * 2, 50))
                        .ToList();

                    var entityMatchedDocIds = entityFileNameChunks.Select(c => c.DocumentId).Distinct().ToHashSet();
                    if (entityMatchedDocIds.Count == 0 && docIdsFromFilenameMatch.Count > 0)
                    {
                        var loadedFromMatch = await LoadChunksFromEntityMatchedDocumentsAsync(
                            docIdsFromFilenameMatch,
                            Math.Max(searchMaxResults * 2, 50),
                            cancellationToken);
                        entityFileNameChunks = loadedFromMatch;
                        entityMatchedDocIds = docIdsFromFilenameMatch;
                    }
                    else if (docIdsFromFilenameMatch.Count > 0)
                    {
                        var loadedWithTail = await LoadChunksFromEntityMatchedDocumentsAsync(
                            docIdsFromFilenameMatch,
                            Math.Max(searchMaxResults * 2, 50),
                            cancellationToken);
                        var existingIds = new HashSet<Guid>(entityFileNameChunks.Select(c => c.Id));
                        var tailChunks = loadedWithTail.Where(c => !existingIds.Contains(c.Id)).ToList();
                        entityFileNameChunks = entityFileNameChunks.Concat(tailChunks).ToList();
                        entityMatchedDocIds = entityMatchedDocIds.Union(docIdsFromFilenameMatch).ToHashSet();
                    }
                    
                    // For entity-matched documents, ensure longest header chunk (ChunkIndex 0) is included
                    var entityHeaderChunks = new List<DocumentChunk>();
                    foreach (var docId in entityMatchedDocIds)
                    {
                        var longestHeader = allChunks
                            .Where(c => c.DocumentId == docId && c.ChunkIndex == 0)
                            .OrderByDescending(c => c.Content?.Length ?? 0)
                            .FirstOrDefault();
                        if (longestHeader != null)
                            entityHeaderChunks.Add(longestHeader);
                    }
                    
                    var existingChunkIds = new HashSet<Guid>(entityFileNameChunks.Concat(chunks).Concat(entityHeaderChunks).Select(c => c.Id));
                    var overlapChunks = new List<DocumentChunk>();
                    if (entityMatchedDocIds.Count > 0 && words.Count > 0)
                    {
                        overlapChunks = await LoadChunksWithQueryWordOverlapAsync(
                            entityMatchedDocIds,
                            words,
                            existingChunkIds,
                            Math.Min(10, searchMaxResults),
                            cancellationToken);
                        overlapChunkIds = new HashSet<Guid>(overlapChunks.Concat(entityHeaderChunks).Select(c => c.Id));
                    }
                    else if (entityHeaderChunks.Any())
                    {
                        overlapChunkIds = new HashSet<Guid>(entityHeaderChunks.Select(c => c.Id));
                    }

                    var numberedListWithQueryWords = GetFilteredAndSortedNumberedListChunks(
                        numberedListChunks, words, hasQueryWords: true, takeCount: searchMaxResults * 3);

                    var numberedListOnly = GetFilteredAndSortedNumberedListChunks(
                        numberedListChunks, words, hasQueryWords: false, takeCount: searchMaxResults * 2);

                    var queryWordsOnly = _chunkPrioritizer.PrioritizeChunksByQueryWords(
                        allChunks.Where(c => !_queryPatternAnalyzer.DetectNumberedLists(c.Content)).ToList(),
                        words)
                        .Take(searchMaxResults * 2)
                        .ToList();

                    var mergedChunks = new List<DocumentChunk>();
                    var seenIds = new HashSet<Guid>();

                    foreach (var chunk in entityHeaderChunks.Concat(overlapChunks).Concat(entityFileNameChunks).Concat(queryWordsOnly).Concat(numberedListWithQueryWords).Concat(numberedListOnly).Concat(chunks))
                    {
                        if (!seenIds.Contains(chunk.Id) && mergedChunks.Count < searchMaxResults * 4)
                        {
                            mergedChunks.Add(chunk);
                            seenIds.Add(chunk.Id);
                        }
                    }

                    mergedChunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(mergedChunks, preservedChunk0);

                    if (mergedChunks.Count > 0 && (overlapChunks.Count > 0 || entityFileNameChunks.Count > 0 || mergedChunks.Count > chunks.Count))
                    {
                        chunks = mergedChunks;
                        var entityChunkIds = new HashSet<Guid>(
                            entityHeaderChunks.Concat(overlapChunks).Concat(entityFileNameChunks).Select(c => c.Id));
                        if (entityChunkIds.Count > 0)
                            entityChunkIdsFromComprehensive = entityChunkIds;
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
                    }
                }
            }
            else if (requiresComprehensive)
            {
                allDocuments = await EnsureAllDocumentsLoadedAsync(allDocuments, effectiveOptions, cancellationToken);
                var potentialNames = QueryTokenizer.ExtractPotentialNames(queryForSearch);
                var words = queryTokens ?? new List<string>();
                var phraseWords = QueryTokenizer.GetWordsForPhraseExtraction(queryForSearch);
                var docIdsFromFilenameMatch = GetEntityMatchedDocumentIds(allDocuments, words, potentialNames, phraseWords);
                if (docIdsFromFilenameMatch.Count > 0)
                {
                    var entityChunks = await LoadChunksFromEntityMatchedDocumentsAsync(
                        docIdsFromFilenameMatch,
                        Math.Max(searchMaxResults * 2, 50),
                        cancellationToken);
                    var existingIds = new HashSet<Guid>(chunks.Select(c => c.Id));
                    var newEntityChunks = entityChunks.Where(c => !existingIds.Contains(c.Id)).ToList();
                    if (newEntityChunks.Count > 0)
                    {
                        var entityChunkScore = 150.0;
                        foreach (var c in newEntityChunks)
                        {
                            c.RelevanceScore = entityChunkScore;
                        }
                        entityChunkIdsFromComprehensive = new HashSet<Guid>(newEntityChunks.Select(c => c.Id));
                        chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(
                            newEntityChunks.Concat(chunks).ToList(),
                            preservedChunk0);
                    }
                }
            }

            chunks = _chunkPrioritizer.MergeChunksWithPreservedChunk0(chunks, preservedChunk0);

            if (entityChunkIdsFromComprehensive != null && entityChunkIdsFromComprehensive.Count > 0)
            {
                foreach (var c in chunks.Where(c => entityChunkIdsFromComprehensive.Contains(c.Id)))
                {
                    var current = c.RelevanceScore ?? 0.0;
                    if (current < DocumentBoostThreshold)
                        c.RelevanceScore = DocumentBoostThreshold + 1.0;
                }
            }

            if (chunks.Count > 0)
            {
                // Calculate adaptive threshold for context expansion
                // Vector search scores: 0.0-1.0 range, Text search scores: 4.0-6.0+ range
                // For high scores, use fixed DocumentBoostThreshold; for low scores, use percentile-based threshold
                var contextExpansionThreshold = CalculateAdaptiveThreshold(
                    chunks,
                    highScoreThreshold: 3.0,
                    highScorePercentile: 1.0,
                    lowScorePercentile: 0.4,
                    useScoreRangeCheck: false,
                    fixedHighScoreThreshold: DocumentBoostThreshold);

                var relevantDocumentChunks = chunks
                    .Where(c => (c.RelevanceScore ?? 0.0) >= contextExpansionThreshold)
                    .ToList();

                var otherChunks = chunks
                    .Where(c => (c.RelevanceScore ?? 0.0) < contextExpansionThreshold)
                    .ToList();

                if (relevantDocumentChunks.Count > 0)
                {
                    var originalScores = relevantDocumentChunks.ToDictionary(c => c.Id, c => c.RelevanceScore ?? 0.0);

                    var topChunk = relevantDocumentChunks
                        .OrderByDescending(c => c.ChunkIndex == 0)
                        .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                        .ThenBy(c => c.ChunkIndex)
                        .FirstOrDefault();

                    var topChunksForExpansion = topChunk != null
                        ? relevantDocumentChunks
                            .Where(c => c.DocumentId == topChunk.DocumentId)
                            .OrderByDescending(c => c.ChunkIndex == 0)
                            .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                            .ThenBy(c => c.ChunkIndex)
                            .Take(Math.Min(5, relevantDocumentChunks.Count(c => c.DocumentId == topChunk.DocumentId)))
                            .ToList()
                        : relevantDocumentChunks
                            .OrderByDescending(c => c.ChunkIndex == 0)
                            .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                            .ThenBy(c => c.ChunkIndex)
                            .Take(Math.Min(5, relevantDocumentChunks.Count))
                            .ToList();

                    var contextWindow = _contextExpansion.DetermineContextWindow(topChunksForExpansion, queryForSearch);
                    var expandedChunks = await _contextExpansion.ExpandContextAsync(topChunksForExpansion, contextWindow);

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

                    var relevantNotExpanded = relevantDocumentChunks
                        .Where(c => !expandedChunks.Any(e => e.Id == c.Id))
                        .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                        .ThenBy(c => c.ChunkIndex)
                        .ToList();
                    chunks = expandedChunks
                        .OrderByDescending(c => originalChunkIds != null && originalChunkIds.Contains(c.Id))
                        .ThenByDescending(c => c.ChunkIndex == 0)
                        .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                        .ThenBy(c => c.ChunkIndex)
                        .Concat(relevantNotExpanded)
                        .Concat(otherChunks
                            .OrderByDescending(c => c.ChunkIndex == 0)
                            .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                            .ThenBy(c => c.ChunkIndex))
                        .ToList();

                    if (chunks.Count > MaxExpandedChunks)
                    {
                        chunks = chunks.Take(MaxExpandedChunks).ToList();
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

            var potentialNamesForOrdering = QueryTokenizer.ExtractPotentialNames(queryForSearch);
            var entityHeaderChunkIds = GetEntityHeaderChunkIds(documentChunks, potentialNamesForOrdering);
            var entityMatchedChunkIds = entityChunkIdsFromComprehensive ?? new HashSet<Guid>();

            // If we have entity-matched chunks from comprehensive search, restrict the
            // primary document set to those documents. This guarantees that when the
            // query clearly points to a specific entity (for example, a particular
            // contract or policy identified via file name), the final context and
            // answer are focused on that document instead of being diluted by
            // unrelated documents from previous turns or generic matches.
            if (entityMatchedChunkIds.Count > 0)
            {
                var entityMatchedDocumentIds = documentChunks
                    .Where(c => entityMatchedChunkIds.Contains(c.Id))
                    .Select(c => c.DocumentId)
                    .Distinct()
                    .ToHashSet();

                if (entityMatchedDocumentIds.Count > 0)
                {
                    documentChunks = documentChunks
                        .Where(c => entityMatchedDocumentIds.Contains(c.DocumentId))
                        .ToList();

                    // When the user query clearly targets a specific document (for example,
                    // via strong file-name entity match), treat all chunks from that document
                    // as highly relevant. This ensures that important structured fields such
                    // as dates or validity periods that may appear in low-text / numeric
                    // regions are still surfaced in the context window, rather than being
                    // filtered out by adaptive thresholds that favor only text-heavy chunks.
                    foreach (var c in documentChunks)
                    {
                        var currentScore = c.RelevanceScore ?? 0.0;
                        if (currentScore < DocumentBoostThreshold)
                        {
                            c.RelevanceScore = DocumentBoostThreshold + 1.0;
                        }
                    }

                    chunks = documentChunks
                        .Concat(imageChunks)
                        .ToList();
                }
            }

            var orderedDocumentChunks = documentChunks
                .OrderByDescending(c => entityMatchedChunkIds.Contains(c.Id))
                .ThenByDescending(c => entityHeaderChunkIds.Contains(c.Id))
                .ThenByDescending(c => originalChunkIds != null && originalChunkIds.Contains(c.Id))
                .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                .ToList();

            var finalQueryTokens = queryTokens ?? QueryTokenizer.TokenizeQuery(queryForSearch);
            var finalPhraseWords = QueryTokenizer.GetWordsForPhraseExtraction(queryForSearch);
            var reRankedDocumentChunks = _chunkPrioritizer.PrioritizeChunksByQueryWords(
                orderedDocumentChunks,
                finalQueryTokens,
                finalPhraseWords);

            chunks = reRankedDocumentChunks
                .Concat(imageChunks
                    .OrderByDescending(c => originalChunkIds != null && originalChunkIds.Contains(c.Id))
                    .ThenByDescending(c => c.RelevanceScore ?? 0.0))
                .ToList();

            if (chunks.Count == 0)
            {
                var fallbackWords = QueryTokenizer.GetWordsForPhraseExtraction(queryForSearch)
                    .Where(w => w.Length >= 4).ToList();
                var fallbackDocs = await _documentService.GetAllDocumentsFilteredAsync(effectiveOptions, cancellationToken);
                var filenameMatched = fallbackDocs
                    .Where(d => fallbackWords.Any(w =>
                        (d.FileName ?? string.Empty).IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (w.Length > 4 && Enumerable.Range(4, w.Length - 3).Any(len =>
                            (d.FileName ?? string.Empty).IndexOf(w.Substring(0, len), StringComparison.OrdinalIgnoreCase) >= 0))))
                    .ToList();
                if (filenameMatched.Count > 0)
                {
                    var fallbackChunks = await LoadChunksFromEntityMatchedDocumentsAsync(
                        filenameMatched.Select(d => d.Id).ToHashSet(),
                        50,
                        cancellationToken);
                    if (fallbackChunks.Count > 0)
                        chunks = fallbackChunks;
                }
            }

            var context = _contextExpansion.BuildLimitedContext(chunks, MaxContextSize);

            if (previousQueryChunkIds.Count > 0 && !string.IsNullOrWhiteSpace(request.ConversationHistory))
            {
                var lastAssistantAnswer = ExtractLastAssistantAnswer(request.ConversationHistory);
                if (!string.IsNullOrWhiteSpace(lastAssistantAnswer))
                    context = "[Previous turn answer from conversation]\n" + lastAssistantAnswer + "\n\n" + context;
            }

            var answer = await GenerateRagAnswerFromContextAsync(queryForSearch, context, request.ConversationHistory, cancellationToken);

            var sourcesChunkIds = originalChunkIds != null ? new HashSet<Guid>(originalChunkIds) : new HashSet<Guid>();
            foreach (var id in entityHeaderChunkIds)
                sourcesChunkIds.Add(id);
            if (overlapChunkIds != null)
                foreach (var id in overlapChunkIds)
                    sourcesChunkIds.Add(id);
            if (entityChunkIdsFromComprehensive != null)
                foreach (var id in entityChunkIdsFromComprehensive)
                    sourcesChunkIds.Add(id);
            var sourcesChunks = sourcesChunkIds.Count > 0
                ? chunks.Where(c => sourcesChunkIds.Contains(c.Id)).ToList()
                : chunks;
            if (sourcesChunks.Count == 0 && chunks.Count > 0)
                sourcesChunks = chunks;

            var sources = await _sourceBuilder.BuildSourcesAsync(sourcesChunks, _documentRepository);
            return _responseBuilder.CreateRagResponse(queryForSearch, answer, sources);
        }

        /// <summary>
        /// Generates RAG answer from context using AI service
        /// </summary>
        /// <param name="query">User query</param>
        /// <param name="context">Context content for RAG</param>
        /// <param name="conversationHistory">Conversation history</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Generated answer from AI</returns>
        private async Task<string> GenerateRagAnswerFromContextAsync(string query, string context, string? conversationHistory, CancellationToken cancellationToken = default)
        {
            var prompt = _promptBuilder.BuildDocumentRagPrompt(query, context, conversationHistory);
            return await _aiService.GenerateResponseAsync(prompt, new List<string> { context }, cancellationToken);
        }

        private static bool Chunk0IsQueryRelevant(DocumentChunk chunk0, List<string> queryTokens, List<string>? potentialNames = null)
        {
            if (chunk0 == null)
                return false;
            if (queryTokens == null || queryTokens.Count == 0)
                return true;
            var searchableText = string.Concat(chunk0.Content ?? string.Empty, " ", chunk0.FileName ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(searchableText.Trim()))
                return false;
            if (potentialNames != null && potentialNames.Count >= 2)
            {
                var entityPhrase = string.Join(" ", potentialNames.Select(n => n.ToLowerInvariant()));
                if (searchableText.Contains(entityPhrase))
                    return true;
            }
            var significantWords = queryTokens.Where(w => w.Length >= 4).ToList();
            if (significantWords.Count == 0)
                return true;
            return significantWords.Any(w => searchableText.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string? GetLastUserQueryDistinctFrom(string? conversationHistory, string currentQuery)
        {
            if (string.IsNullOrWhiteSpace(conversationHistory))
                return null;
            var current = currentQuery?.Trim() ?? string.Empty;
            const string userPrefix = "User: ";
            var lines = conversationHistory.Split('\n');
            for (var i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i];
                if (!line.StartsWith(userPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                var question = line.Substring(userPrefix.Length).Trim();
                if (string.IsNullOrWhiteSpace(question))
                    continue;
                if (!string.Equals(question, current, StringComparison.OrdinalIgnoreCase))
                    return question;
            }
            return null;
        }

        private static string? ExtractLastAssistantAnswer(string? conversationHistory)
        {
            if (string.IsNullOrWhiteSpace(conversationHistory))
                return null;
            const string assistantPrefix = "Assistant: ";
            var lines = conversationHistory.Split('\n');
            for (var i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i];
                if (!line.StartsWith(assistantPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                var answer = line.Substring(assistantPrefix.Length).Trim();
                return string.IsNullOrWhiteSpace(answer) ? null : answer;
            }
            return null;
        }

        /// <summary>
        /// Ensures all documents are loaded with full chunk content. Some repositories (e.g. Qdrant)
        /// return documents with empty chunk content from GetAllAsync; this loads each document
        /// via GetDocumentAsync to populate chunk content for aggressive search scoring.
        /// </summary>
        private async Task<List<Entities.Document>> EnsureAllDocumentsLoadedAsync(List<Entities.Document>? allDocuments, SearchOptions? options, CancellationToken cancellationToken = default)
        {
            if (allDocuments == null)
            {
                allDocuments = await _documentService.GetAllDocumentsFilteredAsync(options, cancellationToken);
            }

            var loaded = new List<Entities.Document>();
            foreach (var doc in allDocuments)
            {
                var fullDoc = await _documentService.GetDocumentAsync(doc.Id, cancellationToken);
                if (fullDoc != null && fullDoc.Chunks != null && fullDoc.Chunks.Count > 0)
                {
                    loaded.Add(fullDoc);
                }
                else
                {
                    loaded.Add(doc);
                }
            }
            return loaded;
        }

        /// <summary>
        /// Calculates adaptive threshold based on score distribution in chunks
        /// Handles different score ranges: vector search (0.0-1.0) vs text search (4.0-6.0+)
        /// </summary>
        /// <param name="chunks">List of document chunks to analyze</param>
        /// <param name="highScoreThreshold">Threshold to distinguish high-score vs low-score ranges (default: 3.0)</param>
        /// <param name="highScorePercentile">Percentile to use for high-score range (default: 0.7 for CanAnswer, 1.0 for ContextExpansion)</param>
        /// <param name="lowScorePercentile">Percentile to use for low-score range (default: 0.4)</param>
        /// <param name="useScoreRangeCheck">Whether to apply score range validation for high-score range (default: true for CanAnswer)</param>
        /// <param name="fixedHighScoreThreshold">Fixed threshold to use when maxScore > highScoreThreshold and useScoreRangeCheck is false (default: null, uses percentile)</param>
        /// <returns>Adaptive threshold value</returns>
        private double CalculateAdaptiveThreshold(
            List<DocumentChunk> chunks,
            double highScoreThreshold = 3.0,
            double highScorePercentile = 0.7,
            double lowScorePercentile = 0.4,
            bool useScoreRangeCheck = true,
            double? fixedHighScoreThreshold = null)
        {
            if (chunks == null || chunks.Count == 0)
                return 0.0;

            var sortedByScore = chunks.OrderByDescending(c => c.RelevanceScore ?? 0.0).ToList();
            var maxScore = sortedByScore.FirstOrDefault()?.RelevanceScore ?? 0.0;

            if (maxScore > highScoreThreshold)
            {
                // High-score range (text search: 4.0-6.0+)
                if (fixedHighScoreThreshold.HasValue)
                {
                    // Use fixed threshold (e.g., DocumentBoostThreshold for context expansion)
                    return fixedHighScoreThreshold.Value;
                }

                if (useScoreRangeCheck)
                {
                    // CanAnswer logic: Check score range to handle edge cases
                    var topPercentileCount = Math.Max(1, (int)(sortedByScore.Count * highScorePercentile));
                    var percentileScore = sortedByScore.Skip(topPercentileCount - 1).FirstOrDefault()?.RelevanceScore ?? 4.0;

                    var minScore = sortedByScore.LastOrDefault()?.RelevanceScore ?? 0.0;
                    var scoreRange = maxScore - minScore;
                    
                    // If scores are very close together, use stricter threshold
                    if (scoreRange < 0.5 && sortedByScore.Count > 1)
                    {
                        return Math.Max(4.5, maxScore - 0.5);
                    }
                    else
                    {
                        return Math.Max(4.0, percentileScore - 0.01);
                    }
                }
                else
                {
                    // Simple percentile-based threshold
                    var topPercentileCount = Math.Max(1, (int)(sortedByScore.Count * highScorePercentile));
                    var percentileScore = sortedByScore.Skip(topPercentileCount - 1).FirstOrDefault()?.RelevanceScore ?? 4.0;
                    return Math.Max(4.0, percentileScore - 0.01);
                }
            }
            else
            {
                // Low-score range (vector search: 0.0-1.0)
                var topPercentileCount = Math.Max(1, (int)(sortedByScore.Count * lowScorePercentile));
                return Math.Max(0.01, sortedByScore.Skip(topPercentileCount - 1).FirstOrDefault()?.RelevanceScore ?? 0.01);
            }
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
        /// Returns chunk IDs to prioritize: header chunks (index 0-2) from documents whose filename matches potential name phrases from the query.
        /// </summary>
        private static HashSet<Guid> GetEntityHeaderChunkIds(List<DocumentChunk> chunks, List<string> potentialNames)
        {
            var ids = new HashSet<Guid>();
            if (chunks == null || potentialNames == null || potentialNames.Count < 2)
                return ids;
            var directPhrases = new List<string>();
            for (int i = 0; i < potentialNames.Count - 1; i++)
            {
                var phrase = $"{potentialNames[i].ToLowerInvariant()} {potentialNames[i + 1].ToLowerInvariant()}";
                if (phrase.Length >= 4)
                    directPhrases.Add(phrase);
            }
            if (directPhrases.Count == 0)
                return ids;
            foreach (var c in chunks)
            {
                if (c.ChunkIndex <= 2)
                {
                    var fn = (c.FileName ?? string.Empty).ToLowerInvariant();
                    if (directPhrases.Any(p => fn.Contains(p)))
                        ids.Add(c.Id);
                }
            }
            return ids;
        }

        private const int MinSingleWordLengthForFileNameMatch = 4;

        private static HashSet<Guid> GetEntityMatchedDocumentIds(
            List<Entities.Document> documents,
            List<string> queryWords,
            List<string> potentialNames,
            List<string>? phraseWords)
        {
            if (documents == null || documents.Count == 0)
                return new HashSet<Guid>();

            var phrases = new List<(string W1, string W2)>();
            if (potentialNames != null && potentialNames.Count >= 2)
            {
                for (int i = 0; i < potentialNames.Count - 1; i++)
                {
                    var w1 = potentialNames[i].ToLowerInvariant();
                    var w2 = potentialNames[i + 1].ToLowerInvariant();
                    if (w1.Length >= 1 && w2.Length >= 3)
                        phrases.Add((w1, w2));
                }
            }
            if (phraseWords != null && phraseWords.Count >= 2)
            {
                for (int i = 0; i < phraseWords.Count - 1; i++)
                {
                    var w1 = phraseWords[i].ToLowerInvariant();
                    var w2 = phraseWords[i + 1].ToLowerInvariant();
                    if (w1.Length >= 1 && w2.Length >= 3)
                        phrases.Add((w1, w2));
                }
            }
            if (queryWords != null && queryWords.Count >= 2)
            {
                for (int i = 0; i < queryWords.Count - 1; i++)
                {
                    var w1 = queryWords[i].ToLowerInvariant();
                    var w2 = queryWords[i + 1].ToLowerInvariant();
                    if (w1.Length >= 1 && w2.Length >= 3)
                        phrases.Add((w1, w2));
                }
            }
            phrases = phrases.Distinct().ToList();

            var singleWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (phraseWords != null)
            {
                foreach (var w in phraseWords)
                {
                    var lower = w.ToLowerInvariant();
                    if (lower.Length >= MinSingleWordLengthForFileNameMatch)
                        singleWords.Add(lower);
                }
            }
            if (queryWords != null)
            {
                foreach (var w in queryWords)
                {
                    var lower = w.ToLowerInvariant();
                    if (lower.Length >= MinSingleWordLengthForFileNameMatch)
                        singleWords.Add(lower);
                }
            }

            var result = new HashSet<Guid>();
            foreach (var doc in documents)
            {
                var fn = (doc.FileName ?? string.Empty).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(fn))
                    continue;

                var phraseMatch = phrases.Count > 0 && phrases.Any(p =>
                {
                    if (fn.IndexOf(p.W1, StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                    if (fn.IndexOf(p.W2, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                    for (var len = Math.Min(4, p.W2.Length); len < p.W2.Length; len++)
                    {
                        var prefix = p.W2.Substring(0, len);
                        if (fn.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }
                    return false;
                });

                var singleWordMatch = singleWords.Count > 0 && singleWords.Any(word =>
                {
                    if (fn.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                    for (var len = MinSingleWordLengthForFileNameMatch; len < word.Length; len++)
                    {
                        var prefix = word.Substring(0, len);
                        if (fn.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }
                    return false;
                });

                if (phraseMatch || singleWordMatch)
                    result.Add(doc.Id);
            }
            return result;
        }

        private const int EntityDocTailChunkCount = 25;

        private async Task<List<DocumentChunk>> LoadChunksFromEntityMatchedDocumentsAsync(
            HashSet<Guid> documentIds,
            int maxChunks,
            CancellationToken cancellationToken)
        {
            if (documentIds == null || documentIds.Count == 0)
                return new List<DocumentChunk>();

            var result = new List<DocumentChunk>();
            var seenIds = new HashSet<Guid>();
            foreach (var docId in documentIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var doc = await _documentRepository.GetByIdAsync(docId, cancellationToken);
                if (doc?.Chunks == null || doc.Chunks.Count == 0)
                    continue;

                var ordered = doc.Chunks
                    .OrderBy(c => c.ChunkIndex)
                    .ToList();

                var headerChunks = ordered
                    .Where(c => c.ChunkIndex <= 2 || c.ChunkIndex < 0)
                    .OrderBy(c => c.ChunkIndex)
                    .ThenByDescending(c => c.Content?.Length ?? 0)
                    .ToList();
                var restChunks = ordered
                    .Where(c => c.ChunkIndex > 2)
                    .ToList();

                var headLimit = Math.Max(0, maxChunks - EntityDocTailChunkCount);
                var headChunks = headerChunks.Concat(restChunks).Take(headLimit).ToList();
                var tailChunks = restChunks.Count > EntityDocTailChunkCount
                    ? restChunks.Skip(restChunks.Count - EntityDocTailChunkCount).ToList()
                    : new List<DocumentChunk>();

                foreach (var c in headChunks.Concat(tailChunks))
                {
                    if (seenIds.Contains(c.Id))
                        continue;
                    seenIds.Add(c.Id);
                    result.Add(c);
                    if (result.Count >= maxChunks)
                        break;
                }
                if (result.Count >= maxChunks)
                    break;
            }
            return result.Take(maxChunks).ToList();
        }

        /// <summary>
        /// Returns chunks from documents whose filename matches query-derived phrases (potentialNames, queryWords, or phraseWords).
        /// Also matches single significant words with prefix matching for morphological variants.
        /// Header chunks (index 0-2) are prioritized, then remaining chunks by relevance score.
        /// </summary>
        private static List<DocumentChunk> GetEntityFileNameChunks(List<DocumentChunk> chunks, List<string> queryWords, List<string> potentialNames, List<string>? phraseWords = null)
        {
            if (chunks == null || chunks.Count == 0)
                return new List<DocumentChunk>();

            var entityFileNameChunks = new List<DocumentChunk>();
            var phrases = new List<(string W1, string W2)>();

            if (potentialNames != null && potentialNames.Count >= 2)
            {
                for (int i = 0; i < potentialNames.Count - 1; i++)
                {
                    var w1 = potentialNames[i].ToLowerInvariant();
                    var w2 = potentialNames[i + 1].ToLowerInvariant();
                    if (w1.Length >= 1 && w2.Length >= 3)
                        phrases.Add((w1, w2));
                }
            }
            if (phraseWords != null && phraseWords.Count >= 2)
            {
                for (int i = 0; i < phraseWords.Count - 1; i++)
                {
                    var w1 = phraseWords[i].ToLowerInvariant();
                    var w2 = phraseWords[i + 1].ToLowerInvariant();
                    if (w1.Length >= 1 && w2.Length >= 3)
                        phrases.Add((w1, w2));
                }
            }
            if (queryWords != null && queryWords.Count >= 2)
            {
                for (int i = 0; i < queryWords.Count - 1; i++)
                {
                    var w1 = queryWords[i].ToLowerInvariant();
                    var w2 = queryWords[i + 1].ToLowerInvariant();
                    if (w1.Length >= 1 && w2.Length >= 3)
                        phrases.Add((w1, w2));
                }
            }
            phrases = phrases.Distinct().ToList();

            var singleWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (phraseWords != null)
            {
                foreach (var w in phraseWords)
                {
                    var lower = w.ToLowerInvariant();
                    if (lower.Length >= MinSingleWordLengthForFileNameMatch)
                        singleWords.Add(lower);
                }
            }
            if (queryWords != null)
            {
                foreach (var w in queryWords)
                {
                    var lower = w.ToLowerInvariant();
                    if (lower.Length >= MinSingleWordLengthForFileNameMatch)
                        singleWords.Add(lower);
                }
            }

            foreach (var c in chunks)
            {
                var fn = (c.FileName ?? string.Empty).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(fn))
                    continue;

                var phraseMatch = phrases.Count > 0 && phrases.Any(p =>
                {
                    if (fn.IndexOf(p.W1, StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                    if (fn.IndexOf(p.W2, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                    for (var len = Math.Min(4, p.W2.Length); len < p.W2.Length; len++)
                    {
                        var prefix = p.W2.Substring(0, len);
                        if (fn.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }
                    return false;
                });

                var singleWordMatch = singleWords.Count > 0 && singleWords.Any(word =>
                {
                    if (fn.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                    for (var len = MinSingleWordLengthForFileNameMatch; len < word.Length; len++)
                    {
                        var prefix = word.Substring(0, len);
                        if (fn.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }
                    return false;
                });

                if (phraseMatch || singleWordMatch)
                    entityFileNameChunks.Add(c);
            }
            var deduped = entityFileNameChunks.GroupBy(c => c.Id).Select(g => g.First()).ToList();
            // Prioritize chunks with index <= 2 or invalid index (-1), then by relevance and content length (longer = more info)
            var headerChunks = deduped.Where(c => c.ChunkIndex <= 2 || c.ChunkIndex < 0)
                .OrderBy(c => c.DocumentId)
                .ThenByDescending(c => c.Content?.Length ?? 0)
                .ThenBy(c => c.ChunkIndex)
                .ToList();
            var rest = deduped.Where(c => c.ChunkIndex > 2).OrderByDescending(c => c.RelevanceScore ?? 0.0).ThenBy(c => c.ChunkIndex).ToList();
            return headerChunks.Concat(rest).ToList();
        }

        private int CountQueryWordMatches(string content, List<string> queryWords, int chunkIndex = -1)
        {
            if (string.IsNullOrEmpty(content) || queryWords == null || queryWords.Count == 0)
                return 0;
            var contentLower = content.ToLowerInvariant();
            var count = 0;
            var matchedWords = new List<string>();
            foreach (var w in queryWords)
            {
                if (w.Length < 3)
                    continue;
                if (contentLower.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    count++;
                    matchedWords.Add(w);
                    continue;
                }
                var prefixLen = Math.Min(5, w.Length);
                if (prefixLen >= 4 && contentLower.IndexOf(w.Substring(0, prefixLen), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    count++;
                    matchedWords.Add($"{w}[prefix:{w.Substring(0, prefixLen)}]");
                }
            }
            return count;
        }

        private async Task<List<DocumentChunk>> LoadChunksWithQueryWordOverlapAsync(
            HashSet<Guid> documentIds,
            List<string> queryWords,
            HashSet<Guid> existingChunkIds,
            int maxChunksPerDocument,
            CancellationToken cancellationToken)
        {
            if (documentIds == null || documentIds.Count == 0 || queryWords == null || queryWords.Count == 0)
                return new List<DocumentChunk>();
            var result = new List<DocumentChunk>();
            foreach (var docId in documentIds)
            {
                var document = await _documentRepository.GetByIdAsync(docId, cancellationToken);
                if (document?.Chunks == null || document.Chunks.Count == 0)
                    continue;
                var scoredWithAll = document.Chunks
                    .Where(c => !existingChunkIds.Contains(c.Id))
                    .Select(c => new { Chunk = c, MatchCount = CountQueryWordMatches(c.Content, queryWords, c.ChunkIndex) })
                    .OrderByDescending(x => x.MatchCount)
                    .ThenBy(x => x.Chunk.ChunkIndex)
                    .ToList();
                
                var scored = scoredWithAll
                    .Where(x => x.MatchCount > 0)
                    .Take(maxChunksPerDocument)
                    .Select(x => x.Chunk)
                    .ToList();
                foreach (var c in scored)
                {
                    c.RelevanceScore = (c.RelevanceScore ?? 0.0) + 5.0;
                    result.Add(c);
                }
            }
            return result.OrderBy(c => c.DocumentId).ThenBy(c => c.ChunkIndex).ToList();
        }

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
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>RAG response with answer and sources</returns>
        [Obsolete("Use GenerateBasicRagAnswerAsync(GenerateRagAnswerRequest) instead. This method will be removed in v4.0.0")]
        public async Task<RagResponse> GenerateBasicRagAnswerAsync(string query, int maxResults, string conversationHistory, string? preferredLanguage = null, SearchOptions? options = null, List<DocumentChunk>? preCalculatedResults = null, List<string>? queryTokens = null, CancellationToken cancellationToken = default)
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
            return await GenerateBasicRagAnswerAsync(request, cancellationToken);
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
                var searchResults = await _documentSearchStrategy.SearchDocumentsAsync(query, FallbackSearchMaxResults, searchOptions, queryTokens, cancellationToken);

                if (searchResults.Count == MinSearchResultsCount)
                {
                    return (false, searchResults);
                }

                if (searchResults.Count == 0)
                {
                    return (false, searchResults);
                }

                var totalContentLength = searchResults.Sum(c => c.Content.Length);
                var hasSubstantialContent = totalContentLength > MinSubstantialContentLength;

                return (hasSubstantialContent, searchResults);
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
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>RAG response with answer and sources</returns>
        [Obsolete("Use QueryIntelligenceAsync instead. This method will be removed in v4.0.0")]
        public async Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5, bool startNewConversation = false, CancellationToken cancellationToken = default)
        {
            return await QueryIntelligenceAsync(query, maxResults, startNewConversation, cancellationToken);
        }
        
        

        private async Task<RagResponse> HandleConversationQueryAsync(string query, string sessionId, string? conversationHistory, CancellationToken cancellationToken = default)
        {
            var conversationAnswer = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, cancellationToken);
            await _conversationManager.AddToConversationAsync(sessionId, query, conversationAnswer, cancellationToken);
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
            RagResponse? existingResponse,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mcpResults = await _mcpIntegration.QueryWithMcpAsync(query, maxResults, conversationHistory, cancellationToken);
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

                        var mergedAnswer = await GenerateRagAnswerFromContextAsync(query, combinedContext, conversationHistory, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(mergedAnswer))
                        {
                            existingResponse.Answer = mergedAnswer;
                        }
                    }

                    return existingResponse;
                }

                var chatResponse = await _conversationManager.HandleGeneralConversationAsync(query, conversationHistory, cancellationToken);

                if (string.IsNullOrWhiteSpace(mcpContext))
                {                 
                    return _responseBuilder.CreateRagResponse(query, chatResponse, new List<SearchSource>(), searchMetadata);
                }

                var mcpAnswer = await GenerateRagAnswerFromContextAsync(query, mcpContext, conversationHistory, cancellationToken);
                if (!string.IsNullOrWhiteSpace(mcpAnswer))
                {
                    return _responseBuilder.CreateRagResponse(query, mcpAnswer, mcpSources, searchMetadata);
                }
                
                return _responseBuilder.CreateRagResponse(query, chatResponse, mcpSources, searchMetadata);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error querying MCP servers");
                return existingResponse;
            }
        }

    }
}