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
using SmartRAG.Mcp.Integration;
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
    public class DocumentSearchService : IDocumentSearchService
    {
        // Selection multipliers
        private const int InitialSearchMultiplier = 2;

        // Thresholds
        private const double RelevanceThreshold = 0.03;  // Lowered for Qdrant cosine similarity scores
        private const int MinSearchResultsCount = 0;
        private const int MinNameChunksCount = 0;
        private const int MinPotentialNamesCount = 2;
        private const int MinWordCountThreshold = 0;
        // Fallback search and content
        private const int FallbackSearchMaxResults = 10; // Increased from 5 to get more chunks for better relevance detection
        private const int MinSubstantialContentLength = 50;

        // Context expansion limits to prevent excessive chunk retrieval and timeout
        private const int MaxExpandedChunks = 50; // Increased for comprehensive list retrieval
        private const int MaxContextSize = 18000; // Increased to accommodate full numbered lists

        // Confidence thresholds for Smart Hybrid approach
        private const double HighConfidenceThreshold = 0.7;
        private const double MediumConfidenceMin = 0.3;
        private const double MediumConfidenceMax = 0.7;

        // Generic messages
        private const string ChatUnavailableMessage = "Sorry, I cannot chat right now. Please try again later.";

        // Compiled Regex Patterns for Performance
        private static readonly Regex NumberedListPattern1 = new Regex(@"\b\d+\.\s", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static readonly Regex NumberedListPattern2 = new Regex(@"\b\d+\)\s", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static readonly Regex NumberedListPattern3 = new Regex(@"\b\d+-\s", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static readonly Regex NumberedListPattern4 = new Regex(@"\b\d+\s+[A-Z]", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static readonly Regex NumberedListPattern5 = new Regex(@"^\d+\.\s", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        private static readonly Regex NumericPattern = new Regex(@"\p{Nd}+", RegexOptions.Compiled);
        private static readonly Regex ListIndicatorPattern = new Regex(@"\d+[\.\)]\s", RegexOptions.Compiled);


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
            IMcpIntegrationService? mcpIntegration = null)
        {
            _documentRepository = documentRepository;
            _aiService = aiService;
            _aiProviderFactory = aiProviderFactory;
            _configuration = configuration;
            _options = options.Value;
            _logger = logger;
            _multiDatabaseQueryCoordinator = multiDatabaseQueryCoordinator;
            _queryIntentAnalyzer = queryIntentAnalyzer; // may be null when database features disabled
            _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
            _queryIntentClassifier = queryIntentClassifier ?? throw new ArgumentNullException(nameof(queryIntentClassifier));
            _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
            _documentScoring = documentScoring ?? throw new ArgumentNullException(nameof(documentScoring));
            _sourceBuilder = sourceBuilder ?? throw new ArgumentNullException(nameof(sourceBuilder));
            _aiConfiguration = aiConfiguration ?? throw new ArgumentNullException(nameof(aiConfiguration));
            _contextExpansion = contextExpansion;
            _mcpIntegration = mcpIntegration;
        }

        /// <summary>
        /// [Document Query] Searches for relevant document chunks based on the query
        /// </summary>
        /// <param name="query">Search query string</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <param name="options">Optional search options to override global configuration</param>
        /// <param name="queryTokens">Pre-computed query tokens (optional, for performance)</param>
        /// <returns>List of relevant document chunks</returns>
        public async Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5, SearchOptions? options = null, List<string>? queryTokens = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            var searchResults = await PerformBasicSearchAsync(query, maxResults * InitialSearchMultiplier, options, queryTokens);
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
        public async Task<RagResponse> QueryIntelligenceAsync(string query, int maxResults = 5, bool startNewConversation = false, SearchOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            var searchOptions = options ?? SearchOptions.FromConfig(_options);

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
                return CreateRagResponse(query, "New conversation started. How can I help you?", new List<SearchSource>());
            }

            var sessionId = await _conversationManager.GetOrCreateSessionIdAsync();

            var conversationHistory = await _conversationManager.GetConversationHistoryAsync(sessionId);

            if ((hasCommand && commandType == QueryCommandType.ForceConversation) || await _queryIntentClassifier.IsGeneralConversationAsync(query, conversationHistory))
            {
                var conversationQuery = string.IsNullOrWhiteSpace(query)
                    ? originalQuery
                    : query;

                var conversationAnswer = await HandleGeneralConversationAsync(conversationQuery, conversationHistory, preferredLanguage);

                await _conversationManager.AddToConversationAsync(sessionId, conversationQuery, conversationAnswer);

                return CreateRagResponse(conversationQuery, conversationAnswer, new List<SearchSource>());
            }

            RagResponse response;

            // Pre-evaluate document availability for smarter strategy selection
            // Only check if document search is enabled
            // Compute query tokens once here and pass to all sub-methods to avoid redundant tokenization
            var queryTokens = searchOptions.EnableDocumentSearch ? QueryTokenizer.TokenizeQuery(query) : null;

            var (CanAnswer, Results) = searchOptions.EnableDocumentSearch
                ? await CanAnswerFromDocumentsAsync(query, searchOptions, queryTokens)
                : (CanAnswer: false, Results: new List<DocumentChunk>());

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
                    var strategy = DetermineQueryStrategy(confidence, hasDatabaseQueries, CanAnswer);

                    // Execute strategy using switch-case (Open/Closed Principle)
                    // Pass pre-analyzed queryIntent (may be null) and preferredLanguage to avoid redundant AI calls
                    response = strategy switch
                    {
                        QueryStrategy.DatabaseOnly => await ExecuteDatabaseOnlyStrategyAsync(query, maxResults, conversationHistory, CanAnswer, queryIntent, preferredLanguage, searchOptions, queryTokens),
                        QueryStrategy.DocumentOnly => await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, CanAnswer, preferredLanguage, searchOptions, Results, queryTokens),
                        QueryStrategy.Hybrid => await ExecuteHybridStrategyAsync(query, maxResults, conversationHistory, hasDatabaseQueries, CanAnswer, queryIntent, preferredLanguage, searchOptions, Results, queryTokens),
                        _ => await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, CanAnswer, preferredLanguage, searchOptions, Results, queryTokens) // Fallback
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during query intent analysis, falling back to document-only query");
                    response = await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, CanAnswer, preferredLanguage, searchOptions, Results, queryTokens);
                }
            }
            else
            {
                if (searchOptions.EnableDocumentSearch)
                {
                    response = await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, CanAnswer, preferredLanguage, searchOptions, Results, queryTokens);
                }
                else
                {
                    // Both disabled? Check MCP first, then fallback to chat
                    if (_mcpIntegration != null && _options.Features.EnableMcpClient)
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
                                            response = CreateRagResponse(query, mcpAnswer, mcpSources);
                                        }
                                        else
                                        {
                                            _logger.LogInformation("MCP query returned results but AI generated empty response. Falling back to general conversation.");
                                            var chatResponse = await HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
                                            response = CreateRagResponse(query, chatResponse, mcpSources);
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogInformation("MCP query returned empty context. Falling back to general conversation.");
                                        var chatResponse = await HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
                                        response = CreateRagResponse(query, chatResponse, new List<SearchSource>());
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation("MCP query returned no valid results. Falling back to general conversation.");
                                    var chatResponse = await HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
                                    response = CreateRagResponse(query, chatResponse, new List<SearchSource>());
                                }
                            }
                            else
                            {
                                _logger.LogInformation("MCP query returned no results. Falling back to general conversation.");
                                var chatResponse = await HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
                                response = CreateRagResponse(query, chatResponse, new List<SearchSource>());
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error querying MCP servers, falling back to general conversation");
                            var chatResponse = await HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
                            response = CreateRagResponse(query, chatResponse, new List<SearchSource>());
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Both database and document search disabled. Falling back to general conversation.");
                        var chatResponse = await HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
                        response = CreateRagResponse(query, chatResponse, new List<SearchSource>());
                    }
                }
            }

            if (_mcpIntegration != null && _options.Features.EnableMcpClient && (searchOptions.EnableDocumentSearch || searchOptions.EnableDatabaseSearch))
            {
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


        /// <summary>
        /// Determines the query strategy based on confidence score and retrieved signals
        /// </summary>
        private QueryStrategy DetermineQueryStrategy(double confidence, bool hasDatabaseQueries, bool hasDocumentMatches)
        {
            if (hasDatabaseQueries && hasDocumentMatches)
                return QueryStrategy.Hybrid;

            if (confidence > HighConfidenceThreshold && hasDatabaseQueries)
                return QueryStrategy.DatabaseOnly;

            if (confidence > HighConfidenceThreshold && !hasDatabaseQueries)
                return QueryStrategy.DocumentOnly;

            if (confidence >= MediumConfidenceMin && confidence <= MediumConfidenceMax)
                return hasDocumentMatches ? QueryStrategy.Hybrid : QueryStrategy.DatabaseOnly;

            if (hasDocumentMatches)
                return QueryStrategy.DocumentOnly;

            // Low confidence (<0.3) → Fallback to document-only
            return QueryStrategy.DocumentOnly;
        }

        /// <summary>
        /// Creates a fallback response when document query cannot answer the question
        /// </summary>
        /// <param name="query">User query</param>
        /// <param name="conversationHistory">Conversation history</param>
        /// <param name="preferredLanguage">Optional preferred language code for AI response</param>
        /// <returns>Fallback RAG response</returns>
        private async Task<RagResponse> CreateFallbackResponseAsync(string query, string conversationHistory, string? preferredLanguage = null)
        {
            ServiceLogMessages.LogGeneralConversationQuery(_logger, null);
            var chatResponse = await HandleGeneralConversationAsync(query, conversationHistory, preferredLanguage);
            return CreateRagResponse(query, chatResponse, new List<SearchSource>());
        }

        /// <summary>
        /// [AI Query] [DB Query] Executes a database-only query strategy
        /// </summary>
        private async Task<RagResponse> ExecuteDatabaseOnlyStrategyAsync(string query, int maxResults, string conversationHistory, bool canAnswerFromDocuments, QueryIntent? queryIntent, string? preferredLanguage = null, SearchOptions? options = null, List<string>? queryTokens = null)
        {
            try
            {
                if (queryIntent == null)
                {
                    // No intent analysis, fallback to document query
                    return await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, canAnswerFromDocuments, preferredLanguage, options, null, queryTokens);
                }

                var databaseResponse = await _multiDatabaseQueryCoordinator!.QueryMultipleDatabasesAsync(query, queryIntent, maxResults);

                if (HasMeaningfulData(databaseResponse))
                {
                    return databaseResponse;
                }

                _logger.LogInformation("Database query returned no meaningful data, falling back to document search");
                return await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, canAnswerFromDocuments, preferredLanguage, options, null, queryTokens);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database query failed, falling back to document query");
                return await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, canAnswerFromDocuments, preferredLanguage, options, null, queryTokens);
            }
        }



        /// <summary>
        /// [AI Query] [DB Query] [Document Query] Executes a hybrid query strategy (both database and document queries)
        /// </summary>
        private async Task<RagResponse> ExecuteHybridStrategyAsync(
            string query,
            int maxResults,
            string conversationHistory,
            bool hasDatabaseQueries,
            bool canAnswerFromDocuments,
            QueryIntent? queryIntent,
            string? preferredLanguage = null,
            SearchOptions? options = null,
            List<DocumentChunk>? preCalculatedResults = null,
            List<string>? queryTokens = null)
        {
            RagResponse? databaseResponse = null;
            RagResponse? documentResponse = null;

            // Execute database query if available
            if (hasDatabaseQueries)
            {
                try
                {
                    if (queryIntent == null)
                    {
                        // No intent, skip database part
                        // Continue to document query if enabled
                    }
                    else
                    {
                        var candidateDatabaseResponse = await _multiDatabaseQueryCoordinator!.QueryMultipleDatabasesAsync(query, queryIntent, maxResults);
                        if (HasMeaningfulData(candidateDatabaseResponse))
                        {
                            databaseResponse = candidateDatabaseResponse;
                        }
                        else
                        { }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Database query failed in hybrid mode, continuing with document query only");
                }
            }

            // Execute document query
            if (canAnswerFromDocuments)
            {
                documentResponse = await GenerateBasicRagAnswerAsync(query, maxResults, conversationHistory, preferredLanguage, options, preCalculatedResults, queryTokens);
            }

            // Merge results if both queries executed
            if (databaseResponse != null && documentResponse != null)
            {
                return await MergeHybridResultsAsync(query, databaseResponse, documentResponse, conversationHistory, preferredLanguage);
            }

            if (databaseResponse != null)
                return databaseResponse;

            if (documentResponse != null)
                return documentResponse;

            return await CreateFallbackResponseAsync(query, conversationHistory, preferredLanguage);
        }

        private static bool HasMeaningfulData(RagResponse? response)
        {
            if (response == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(response.Answer) && !IndicatesMissingData(response.Answer))
            {
                return true;
            }

            if (response.Sources != null && response.Sources.Any(source =>
                source.DocumentId != Guid.Empty && !string.IsNullOrWhiteSpace(source.RelevantContent)))
            {
                return true;
            }

            return false;
        }

        private static bool IndicatesMissingData(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return true;
            }

            var normalized = answer.ToLowerInvariant();
            // Generic indicators that work for all languages (language-agnostic approach)
            var indicators = new[]
            {
                "unable to find",
                "cannot find",
                "no data",
                "no information",
                "not available",
                "not found",
                "sorry"
            };

            return indicators.Any(normalized.Contains);
        }

        /// <summary>
        /// [AI Query] Merges results from database and document queries into a unified response
        /// </summary>
        /// <param name="query">Original user query</param>
        /// <param name="databaseResponse">Database query response</param>
        /// <param name="documentResponse">Document query response</param>
        /// <param name="conversationHistory">Conversation history</param>
        /// <param name="preferredLanguage">Optional preferred language code for AI response</param>
        /// <returns>Merged RAG response</returns>
        private async Task<RagResponse> MergeHybridResultsAsync(string query, RagResponse databaseResponse, RagResponse documentResponse, string conversationHistory, string? preferredLanguage = null)
        {
            // Combine sources
            var combinedSources = new List<SearchSource>();
            combinedSources.AddRange(databaseResponse.Sources);
            combinedSources.AddRange(documentResponse.Sources);

            // Build combined context for AI
            var databaseContext = !string.IsNullOrEmpty(databaseResponse.Answer)
                ? databaseResponse.Answer
                : null;
            var documentContext = !string.IsNullOrEmpty(documentResponse.Answer)
                ? documentResponse.Answer
                : null;

            var combinedContext = new List<string>();
            if (!string.IsNullOrEmpty(databaseContext))
                combinedContext.Add(databaseContext);
            if (!string.IsNullOrEmpty(documentContext))
                combinedContext.Add(documentContext);

            var mergePrompt = _promptBuilder.BuildHybridMergePrompt(query, databaseContext, documentContext, conversationHistory, preferredLanguage);
            var mergedAnswer = await _aiService.GenerateResponseAsync(mergePrompt, combinedContext);
            return CreateRagResponse(query, mergedAnswer, combinedSources);
        }


        /// <summary>
        /// [Document Query] Common method for executing document-based queries (used by both document-only and fallback strategies)
        /// </summary>
        private async Task<RagResponse> ExecuteDocumentQueryAsync(string query, int maxResults, string conversationHistory, bool? canAnswerFromDocuments = null, string? preferredLanguage = null, SearchOptions? options = null, List<DocumentChunk>? preCalculatedResults = null, List<string>? queryTokens = null)
        {
            var canAnswer = false;
            List<DocumentChunk>? results = preCalculatedResults;

            if (canAnswerFromDocuments.HasValue)
            {
                canAnswer = canAnswerFromDocuments.Value;
            }
            else
            {
                var (CanAnswer, Results) = await CanAnswerFromDocumentsAsync(query, options, queryTokens);
                canAnswer = CanAnswer;
                results = Results;
            }

            if (canAnswer)
            {
                return await GenerateBasicRagAnswerAsync(query, maxResults, conversationHistory, preferredLanguage, options, results, queryTokens);
            }

            return await CreateFallbackResponseAsync(query, conversationHistory, preferredLanguage);
        }

        /// <summary>
        /// [Document Query] Searches for relevant document chunks using repository's optimized search
        /// Uses vector DB search (Qdrant) or keyword search (Redis) depending on repository implementation
        /// </summary>
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
                        var filteredDocs = await GetAllDocumentsFilteredAsync(options);
                        var allowedDocIds = new HashSet<Guid>(filteredDocs.Select(d => d.Id));
                        filteredResults = searchResults.Where(c => allowedDocIds.Contains(c.DocumentId)).ToList();
                    }

                    return filteredResults;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Repository search failed, falling back to keyword scoring");
            }

            // Fallback: If repository search failed or returned no results, use keyword-based scoring
            // This ensures backward compatibility and handles edge cases
            var allDocuments = await GetAllDocumentsFilteredAsync(options);
            var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();

            var queryWords = queryTokens ?? QueryTokenizer.TokenizeQuery(query);
            var potentialNames = QueryTokenizer.ExtractPotentialNames(query);

            var scoredChunks = _documentScoring.ScoreChunks(allChunks, query, queryWords, potentialNames);

            // Calculate document-level relevance: average of top N chunks per document
            // Also consider how many query words are matched across the document
            // CRITICAL: Count document-specific keywords (words that appear in only one document)
            const int TopChunksPerDocument = 5;

            // First, identify which query words appear in which documents
            var queryWordDocumentMap = new Dictionary<string, HashSet<Guid>>();
            foreach (var word in queryWords)
            {
                queryWordDocumentMap[word] = new HashSet<Guid>();
            }

            // Map each query word to documents that contain it
            // CRITICAL: Check more chunks (not just top 5) to avoid missing keywords
            // CRITICAL: Use word boundaries, not substring Contains(), to avoid false matches
            const int ChunksToCheckForKeywords = 30; // Check top 30 chunks per document
            foreach (var doc in allDocuments)
            {
                var docChunks = scoredChunks.Where(c => c.DocumentId == doc.Id).ToList();
                if (docChunks.Count == 0) continue;

                // Check top N chunks for keywords (not just top 5)
                var chunksToCheck = docChunks
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .Take(ChunksToCheckForKeywords)
                    .ToList();

                var docContent = string.Join(" ", chunksToCheck.Select(c => c.Content)).ToLowerInvariant();

                foreach (var word in queryWords)
                {
                    var wordLower = word.ToLowerInvariant();

                    if (IsWordInText(docContent, wordLower))
                    {
                        queryWordDocumentMap[word].Add(doc.Id);
                    }
                }
            }


            var documentScores = allDocuments.Select(doc =>
            {
                var docChunks = scoredChunks.Where(c => c.DocumentId == doc.Id).ToList();
                if (docChunks.Count == 0)
                    return new { Document = doc, Score = 0.0, QueryWordMatches = 0, UniqueKeywords = 0 };

                var topChunks = docChunks
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .Take(TopChunksPerDocument)
                    .ToList();

                var avgScore = topChunks.Average(c => c.RelevanceScore ?? 0.0);
                var docContent = string.Join(" ", topChunks.Select(c => c.Content)).ToLowerInvariant();
                var queryWordMatches = 0;
                var totalQueryWordOccurrences = 0;

                foreach (var word in queryWords)
                {
                    var wordLower = word.ToLowerInvariant();
                    var wordFound = false;
                    var occurrences = 0;
                    var exactMatches = (docContent.Length - docContent.Replace(wordLower, "").Length) / wordLower.Length;
                    if (exactMatches > 0)
                    {
                        wordFound = true;
                        occurrences += exactMatches;
                    }

                    if (wordLower.Length >= 4)
                    {
                        for (int len = Math.Min(wordLower.Length, 8); len >= 4; len--)
                        {
                            for (int start = 0; start <= wordLower.Length - len; start++)
                            {
                                var substring = wordLower.Substring(start, len);
                                var substringMatches = (docContent.Length - docContent.Replace(substring, "").Length) / substring.Length;
                                if (substringMatches > 0)
                                {
                                    wordFound = true;
                                    occurrences += substringMatches;
                                    break; // Found a match, no need to check shorter substrings
                                }
                            }
                            if (wordFound && exactMatches == 0) break; // Found substring match, stop
                        }
                    }

                    if (wordFound)
                    {
                        queryWordMatches++;
                        totalQueryWordOccurrences += occurrences;
                    }
                }

                // CRITICAL FIX: Prioritize documents containing ALL query words
                // Query coverage (what % of query words are in this document) is MORE important than frequency
                // This ensures documents with "kasko + cam" beat documents with only "cam" (even if "cam" appears 100 times)
                var queryCoverageRatio = queryWords.Count > 0 ? (double)queryWordMatches / queryWords.Count : 0.0;

                // MASSIVE bonus for high query coverage
                // 100% coverage (all query words present) = 5000+ bonus
                // 50% coverage = 1250 bonus  
                // Exponential bonus rewards documents that contain ALL query terms
                var queryCoverageBonus = queryCoverageRatio * queryCoverageRatio * 5000.0;

                // CRITICAL: Document-specific keyword bonus
                var uniqueKeywordCount = 0;
                foreach (var word in queryWords)
                {
                    if (queryWordDocumentMap.TryGetValue(word, out var docsWithWord))
                    {
                        if (docsWithWord.Count == 1 && docsWithWord.Contains(doc.Id))
                        {
                            uniqueKeywordCount++;
                        }
                    }
                }

                var uniqueKeywordBonus = uniqueKeywordCount * 2500.0;
                var frequencyBonus = totalQueryWordOccurrences * 75.0;
                var queryWordMatchBonus = uniqueKeywordBonus + queryCoverageBonus + frequencyBonus;
                var finalScore = avgScore + queryWordMatchBonus;

                return new { Document = doc, Score = finalScore, QueryWordMatches = queryWordMatches, UniqueKeywords = uniqueKeywordCount };
            })
            .OrderByDescending(x => x.Score)
            .ToList();

            var topDocument = documentScores.FirstOrDefault();
            var secondDocument = documentScores.Skip(1).FirstOrDefault();

            var relevantDocuments = new List<Entities.Document>();
            if (topDocument != null && topDocument.Score > 0)
            {
                relevantDocuments.Add(topDocument.Document);

                if (secondDocument != null && secondDocument.Score > 0 &&
                    secondDocument.Score >= topDocument.Score * 0.8)
                {
                    relevantDocuments.Add(secondDocument.Document);
                }
            }

            var relevantDocumentChunks = relevantDocuments
                .SelectMany(d => scoredChunks.Where(c => c.DocumentId == d.Id))
                .ToList();

            var otherDocumentChunks = allDocuments
                .Except(relevantDocuments)
                .SelectMany(d => scoredChunks.Where(c => c.DocumentId == d.Id))
                .ToList();

            const double DocumentRelevanceBoost = 200.0;
            foreach (var chunk in relevantDocumentChunks)
            {
                chunk.RelevanceScore = (chunk.RelevanceScore ?? 0.0) + DocumentRelevanceBoost;
            }

            var finalScoredChunks = relevantDocumentChunks.Concat(otherDocumentChunks).ToList();

            const int CandidateMultiplier = 20;
            const int CandidateMinCount = 200;

            var relevantChunks = finalScoredChunks
                .Where(c => c.RelevanceScore > MinWordCountThreshold)
                .OrderByDescending(c => c.RelevanceScore)
                .Take(Math.Max(maxResults * CandidateMultiplier, CandidateMinCount))
                .ToList();

            // Log document distribution for debugging
            var docDistribution = relevantChunks
                .GroupBy(c => c.DocumentId)
                .Select(g => new { DocId = g.Key, Count = g.Count(), AvgScore = g.Average(c => c.RelevanceScore ?? 0.0) })
                .OrderByDescending(x => x.AvgScore)
                .ToList();

            // If we found chunks with names, prioritize them
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

            var numberedListPatterns = new[]
            {
                NumberedListPattern1,
                NumberedListPattern2,
                NumberedListPattern3,
                NumberedListPattern4,
                NumberedListPattern5,
            };

            if (RequiresComprehensiveSearch(query))
            {
                var comprehensiveQueryWords = QueryTokenizer.TokenizeQuery(query);
                var allNumberedListChunks = finalScoredChunks
                    .Where(c => numberedListPatterns.Any(pattern =>
                        pattern.IsMatch(c.Content)))
                    .Select(c =>
                    {
                        var baseScore = c.RelevanceScore ?? 0.0;
                        var numberedListCount = numberedListPatterns.Sum(pattern =>
                            pattern.Matches(c.Content).Count);

                        var wordMatches = comprehensiveQueryWords.Count(word =>
                            c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

                        // Add numbered list bonus to existing score (preserves document-level boost)
                        c.RelevanceScore = baseScore + (numberedListCount * 100.0) + (wordMatches * 10.0);

                        return new
                        {
                            Chunk = c,
                            NumberedListCount = numberedListCount,
                            WordMatches = wordMatches,
                            TotalScore = c.RelevanceScore ?? 0.0
                        };
                    })
                    .OrderByDescending(x => x.TotalScore)
                    .ThenByDescending(x => x.NumberedListCount)
                    .ThenByDescending(x => x.WordMatches)
                    .Select(x => x.Chunk)
                    .Take(maxResults * 2)
                    .ToList();

                if (allNumberedListChunks.Count > 0)
                {
                    var chunk0 = allNumberedListChunks.FirstOrDefault(c => c.ChunkIndex == 0)
                        ?? finalScoredChunks.FirstOrDefault(c => c.ChunkIndex == 0);
                    var otherChunks = allNumberedListChunks
                        .Where(c => c.ChunkIndex != 0)
                        .Concat(relevantChunks.Except(allNumberedListChunks).Where(c => c.ChunkIndex != 0))
                        .Take(maxResults - (chunk0 != null ? 1 : 0))
                        .ToList();

                    if (chunk0 != null)
                    {
                        return new List<DocumentChunk> { chunk0 }.Concat(otherChunks).ToList();
                    }

                    return otherChunks;
                }
            }

            return relevantChunks.Take(maxResults).ToList();
        }

        /// <summary>
        /// [AI Query] Generate RAG answer with automatic session management
        /// </summary>
        private async Task<RagResponse> GenerateBasicRagAnswerAsync(string query, int maxResults, string conversationHistory, string? preferredLanguage = null, SearchOptions? options = null, List<DocumentChunk>? preCalculatedResults = null, List<string>? queryTokens = null)
        {
            var searchMaxResults = DetermineInitialSearchCount(query, maxResults);

            List<DocumentChunk> chunks;
            var queryTokensForPrioritization = queryTokens ?? QueryTokenizer.TokenizeQuery(query);

            DocumentChunk? preservedChunk0 = null;

            if (preCalculatedResults != null && preCalculatedResults.Count > 0)
            {
                preservedChunk0 = preCalculatedResults.FirstOrDefault(c => c.ChunkIndex == 0);
                _logger.LogDebug("PreCalculatedResults contains chunk 0: {HasChunk0}, Total: {Count}",
                    preservedChunk0 != null, preCalculatedResults.Count);

                chunks = preCalculatedResults.Where(c => c.ChunkIndex != 0).ToList();

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
                chunks = chunks
                    .Where(c => c.ChunkIndex != 0)
                    .OrderByDescending(c =>
                    {
                        if (queryTokensForPrioritization.Count == 0) return 0;
                        var contentLower = c.Content?.ToLowerInvariant() ?? string.Empty;
                        return queryTokensForPrioritization.Count(token =>
                            token.Length >= 3 &&
                            contentLower.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
                    })
                    .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                    .ThenBy(c => c.ChunkIndex)
                    .ToList();

                if (preservedChunk0 != null)
                {
                    chunks = new List<DocumentChunk> { preservedChunk0 }.Concat(chunks).ToList();
                }
            }

            var needsAggressiveSearch = chunks.Count < 5 || RequiresComprehensiveSearch(query);
            if (needsAggressiveSearch)
            {
                preservedChunk0 ??= chunks.FirstOrDefault(c => c.ChunkIndex == 0);

                var allDocuments = await GetAllDocumentsFilteredAsync(options);
                var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();
                var queryWords = queryTokens ?? QueryTokenizer.TokenizeQuery(query);
                var potentialNames = QueryTokenizer.ExtractPotentialNames(query);
                var scoredChunks = _documentScoring.ScoreChunks(allChunks, query, queryWords, potentialNames);

                const int TopChunksPerDocument = 5;
                const int ChunksToCheckForKeywords = 30;

                var queryWordDocumentMap = new Dictionary<string, HashSet<Guid>>();
                foreach (var word in queryWords)
                {
                    queryWordDocumentMap[word] = new HashSet<Guid>();
                }

                // Map each query word to documents that contain it
                // CRITICAL: Check more chunks (not just top 5) to avoid missing keywords
                // CRITICAL: Use word boundaries, not substring Contains(), to avoid false matches
                foreach (var doc in allDocuments)
                {
                    var docChunks = scoredChunks.Where(c => c.DocumentId == doc.Id).ToList();
                    if (docChunks.Count == 0) continue;

                    // Check top N chunks for keywords (not just top 5)
                    var chunksToCheck = docChunks
                        .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                        .Take(ChunksToCheckForKeywords)
                        .ToList();

                    var docContent = string.Join(" ", chunksToCheck.Select(c => c.Content)).ToLowerInvariant();

                    foreach (var word in queryWords)
                    {
                        var wordLower = word.ToLowerInvariant();

                        // Use word boundary check instead of simple Contains()
                        // This avoids false matches like "kasko" matching "kaskoda"
                        if (IsWordInText(docContent, wordLower))
                        {
                            queryWordDocumentMap[word].Add(doc.Id);
                        }
                    }
                }

                var documentScores = allDocuments.Select(doc =>
                {
                    var docChunks = scoredChunks.Where(c => c.DocumentId == doc.Id).ToList();
                    if (docChunks.Count == 0)
                        return new { Document = doc, Score = 0.0, QueryWordMatches = 0, UniqueKeywords = 0 };

                    // Use average of top N chunks as document relevance score
                    var topChunks = docChunks
                        .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                        .Take(TopChunksPerDocument)
                        .ToList();

                    var avgScore = topChunks.Average(c => c.RelevanceScore ?? 0.0);

                    // Count how many unique query words are matched in this document's TOP CHUNKS
                    // CRITICAL: Use only top chunks content, NOT all chunks
                    // This prevents large documents from getting unfair advantage due to size
                    // and ensures we're measuring relevance based on the most relevant content
                    var docContent = string.Join(" ", topChunks.Select(c => c.Content)).ToLowerInvariant();
                    var queryWordMatches = 0;
                    var totalQueryWordOccurrences = 0;

                    foreach (var word in queryWords)
                    {
                        var wordLower = word.ToLowerInvariant();
                        var wordFound = false;
                        var occurrences = 0;

                        var exactMatches = (docContent.Length - docContent.Replace(wordLower, "").Length) / wordLower.Length;
                        if (exactMatches > 0)
                        {
                            wordFound = true;
                            occurrences += exactMatches;
                        }

                        if (wordLower.Length >= 4)
                        {
                            for (int len = Math.Min(wordLower.Length, 8); len >= 4; len--)
                            {
                                for (int start = 0; start <= wordLower.Length - len; start++)
                                {
                                    var substring = wordLower.Substring(start, len);
                                    var substringMatches = (docContent.Length - docContent.Replace(substring, "").Length) / substring.Length;
                                    if (substringMatches > 0)
                                    {
                                        wordFound = true;
                                        occurrences += substringMatches;
                                        break;
                                    }
                                }
                                if (wordFound && exactMatches == 0) break;
                            }
                        }

                        if (wordFound)
                        {
                            queryWordMatches++;
                            totalQueryWordOccurrences += occurrences;
                        }
                    }


                    // CRITICAL FIX: Prioritize documents containing ALL query words (same as PerformBasicSearchAsync)
                    // Query coverage (what % of query words are in this document) is MORE important than frequency
                    var queryCoverageRatio = queryWords.Count > 0 ? (double)queryWordMatches / queryWords.Count : 0.0;

                    // MASSIVE bonus for high query coverage
                    // 100% coverage (all query words present) = 5000+ bonus
                    // 50% coverage = 1250 bonus  
                    // Exponential bonus rewards documents that contain ALL query terms
                    var queryCoverageBonus = queryCoverageRatio * queryCoverageRatio * 5000.0;

                    // CRITICAL: Document-specific keyword bonus
                    var uniqueKeywordCount = 0;
                    foreach (var word in queryWords)
                    {
                        if (queryWordDocumentMap.TryGetValue(word, out var docsWithWord))
                        {
                            if (docsWithWord.Count == 1 && docsWithWord.Contains(doc.Id))
                            {
                                uniqueKeywordCount++;
                            }
                        }
                    }

                    // HUGE bonus for unique keywords: 2500 points per unique keyword (Increased from 1500)
                    // This ensures "kasko" in KASKO doc gives advantage but doesn't completely dominate high-frequency matches
                    var uniqueKeywordBonus = uniqueKeywordCount * 2500.0;

                    // High bonus for word frequency (helps with high-frequency terms like "hava yastığı")
                    var frequencyBonus = totalQueryWordOccurrences * 75.0;

                    // Combine: unique keywords FIRST, coverage SECOND, frequency THIRD
                    var queryWordMatchBonus = uniqueKeywordBonus + queryCoverageBonus + frequencyBonus;
                    var finalScore = avgScore + queryWordMatchBonus;

                    return new { Document = doc, Score = finalScore, QueryWordMatches = queryWordMatches, UniqueKeywords = uniqueKeywordCount };
                })
                .OrderByDescending(x => x.Score)
                .ToList();

                // Prioritize chunks from most relevant documents
                // CRITICAL: Take top document if it has significantly higher score than others
                // This ensures the most relevant document is always selected
                var topDocument = documentScores.FirstOrDefault();
                var secondDocument = documentScores.Skip(1).FirstOrDefault();

                var relevantDocuments = new List<Entities.Document>();
                if (topDocument != null && topDocument.Score > 0)
                {
                    relevantDocuments.Add(topDocument.Document);

                    // Only add second document if its score is close to top document (within 20%)
                    // This prevents irrelevant documents from being included
                    if (secondDocument != null && secondDocument.Score > 0 &&
                        secondDocument.Score >= topDocument.Score * 0.8)
                    {
                        relevantDocuments.Add(secondDocument.Document);
                    }
                }

                var relevantDocumentChunks = relevantDocuments
                    .SelectMany(d => scoredChunks.Where(c => c.DocumentId == d.Id))
                    .ToList();

                // Apply document-level boost to chunks from relevant documents
                // CRITICAL: Use the ACTUAL document score (which includes unique keyword bonuses)
                var docScoreMap = relevantDocuments.ToDictionary(d => d.Id, d => documentScores.First(ds => ds.Document.Id == d.Id).Score);

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

                var numberedListPatterns = new[]
                {
                    NumberedListPattern1,
                    NumberedListPattern2,
                    NumberedListPattern3,
                    NumberedListPattern4,
                    NumberedListPattern5,
                };

                var allChunksWithNumberedLists = allChunks
                    .Select(c =>
                    {
                        var numberedListCount = numberedListPatterns.Sum(pattern =>
                            pattern.Matches(c.Content).Count);

                        var wordMatches = queryWords.Count(word =>
                            c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
                        var hasNumbers = c.Content.Any(char.IsDigit);
                        var hasNumberedList = numberedListCount > 0;

                        return new
                        {
                            Chunk = c,
                            WordMatches = wordMatches,
                            HasNumbers = hasNumbers,
                            HasNumberedList = hasNumberedList,
                            NumberedListCount = numberedListCount // Count of numbered items
                        };
                    })
                    .ToList();

                if (RequiresComprehensiveSearch(query))
                {
                    var numberedListWithQueryWords = allChunksWithNumberedLists
                        .Where(x => x.HasNumberedList && x.WordMatches > 0)
                        .OrderByDescending(x => x.Chunk.RelevanceScore ?? 0.0)
                        .ThenByDescending(x => x.NumberedListCount)
                        .ThenByDescending(x => x.WordMatches)
                        .Select(x => x.Chunk)
                        .Take(searchMaxResults * 3)
                        .ToList();

                    var numberedListOnly = allChunksWithNumberedLists
                        .Where(x => x.HasNumberedList && x.WordMatches == 0)
                        .OrderByDescending(x => x.Chunk.RelevanceScore ?? 0.0)
                        .ThenByDescending(x => x.NumberedListCount)
                        .Select(x => x.Chunk)
                        .Take(searchMaxResults * 2)
                        .ToList();

                    var queryWordsOnly = allChunksWithNumberedLists
                        .Where(x => !x.HasNumberedList && x.WordMatches > 0)
                        .OrderByDescending(x => x.Chunk.RelevanceScore ?? 0.0)
                        .ThenByDescending(x => x.WordMatches)
                        .ThenByDescending(x => x.HasNumbers)
                        .Select(x => x.Chunk)
                        .Take(searchMaxResults * 2)
                        .ToList();

                    var mergedChunks = new List<DocumentChunk>();
                    var seenIds = new HashSet<Guid>();

                    foreach (var chunk in numberedListWithQueryWords)
                    {
                        if (!seenIds.Contains(chunk.Id))
                        {
                            mergedChunks.Add(chunk);
                            seenIds.Add(chunk.Id);
                        }
                    }

                    foreach (var chunk in numberedListOnly)
                    {
                        if (!seenIds.Contains(chunk.Id) && mergedChunks.Count < searchMaxResults * 4)
                        {
                            mergedChunks.Add(chunk);
                            seenIds.Add(chunk.Id);
                        }
                    }

                    foreach (var chunk in queryWordsOnly)
                    {
                        if (!seenIds.Contains(chunk.Id) && mergedChunks.Count < searchMaxResults * 4)
                        {
                            mergedChunks.Add(chunk);
                            seenIds.Add(chunk.Id);
                        }
                    }

                    if (preservedChunk0 != null && !seenIds.Contains(preservedChunk0.Id))
                    {
                        mergedChunks.Insert(0, preservedChunk0);
                        seenIds.Add(preservedChunk0.Id);
                    }

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
                    var fallbackChunks = allChunksWithNumberedLists
                        .Where(x => x.WordMatches > 0)
                        .OrderByDescending(x => x.Chunk.RelevanceScore ?? 0.0) // Document-level boost preserved - HIGHEST PRIORITY
                        .ThenByDescending(x => x.WordMatches) // Then by word matches
                        .ThenByDescending(x => x.NumberedListCount) // Then by numbered list count
                        .ThenByDescending(x => x.HasNumberedList)
                        .ThenByDescending(x => x.HasNumbers)
                        .Select(x => x.Chunk)
                        .Take(searchMaxResults * 4)
                        .ToList();

                    if (fallbackChunks.Count > chunks.Count)
                    {
                        var mergedChunks = new List<DocumentChunk>();
                        var seenIds = new HashSet<Guid>();

                        foreach (var chunk in fallbackChunks)
                        {
                            if (!seenIds.Contains(chunk.Id))
                            {
                                mergedChunks.Add(chunk);
                                seenIds.Add(chunk.Id);
                            }
                        }

                        if (preservedChunk0 != null && !seenIds.Contains(preservedChunk0.Id))
                        {
                            mergedChunks.Insert(0, preservedChunk0);
                            seenIds.Add(preservedChunk0.Id);
                        }

                        foreach (var chunk in chunks)
                        {
                            if (!seenIds.Contains(chunk.Id))
                            {
                                mergedChunks.Add(chunk);
                                seenIds.Add(chunk.Id);
                            }
                        }

                        var chunk0InMerged = mergedChunks.FirstOrDefault(c => c.ChunkIndex == 0);
                        if (preservedChunk0 != null && chunk0InMerged == null)
                        {
                            chunks = new List<DocumentChunk> { preservedChunk0 }.Concat(mergedChunks).ToList();
                        }
                        else
                        {
                            chunks = mergedChunks;
                        }
                        ServiceLogMessages.LogFallbackSearchUsed(_logger, chunks.Count, null);
                    }
                }
            }

            if (preservedChunk0 != null && !chunks.Any(c => c.ChunkIndex == 0))
            {
                chunks = new List<DocumentChunk> { preservedChunk0 }.Concat(chunks).ToList();
            }
            else preservedChunk0 ??= chunks.FirstOrDefault(c => c.ChunkIndex == 0);

            // CRITICAL: For counting/listing questions, find ALL numbered list chunks from the same documents
            // This ensures we get the complete list even if it's split across multiple chunks
            // DISABLED: Numbered list prioritization is too aggressive and overrides document-level boost
            // Instead, rely on document-level scoring and query word matching
            var numberedListChunkIds = new HashSet<Guid>();
            preservedChunk0 ??= chunks.FirstOrDefault(c => c.ChunkIndex == 0);

            if (false && RequiresComprehensiveSearch(query) && chunks.Count > 0) // Disabled numbered list prioritization
            {
                var numberedListPatterns = new[]
                {
                    NumberedListPattern1,
                    NumberedListPattern2,
                    NumberedListPattern3,
                    NumberedListPattern4,
                    NumberedListPattern5,
                };

                // Get all documents that contain the found chunks
                var documentIds = chunks.Select(c => c.DocumentId).Distinct().ToList();
                var allNumberedListChunks = new List<DocumentChunk>();

                // CRITICAL: First, check chunks that are already in the list (they have document-level boost)
                var existingChunksMap = chunks.ToDictionary(c => c.Id, c => c);
                var localQueryWords = queryTokens ?? QueryTokenizer.TokenizeQuery(query); // Use parameter tokens if available

                foreach (var chunk in chunks)
                {
                    var numberedListCount = numberedListPatterns.Sum(pattern =>
                        pattern.Matches(chunk.Content).Count);

                    if (numberedListCount > 0)
                    {
                        var wordMatches = localQueryWords.Count(word =>
                            chunk.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

                        // Only include chunks that have query word matches OR have many numbered items
                        if (wordMatches > 0 || numberedListCount >= 3)
                        {
                            // Preserve existing score (includes document-level boost) and add numbered list bonus
                            var baseScore = chunk.RelevanceScore ?? 0.0;
                            var numberedListBonus = wordMatches > 0
                                ? 1000.0 + (numberedListCount * 100.0) + (wordMatches * 50.0)
                                : 500.0 + (numberedListCount * 50.0);

                            // Add numbered list bonus to existing score (preserves document-level boost)
                            chunk.RelevanceScore = baseScore + numberedListBonus;

                            numberedListChunkIds.Add(chunk.Id);
                            allNumberedListChunks.Add(chunk);
                        }
                    }
                }

                // Then, find additional numbered list chunks from documents that aren't in the original chunks list
                foreach (var documentId in documentIds)
                {
                    var document = await _documentRepository.GetByIdAsync(documentId);
                    if (document?.Chunks == null) continue;

                    // Find chunks in this document that contain numbered lists but aren't already in chunks list
                    var documentNumberedChunks = document.Chunks
                        .Where(c => !existingChunksMap.ContainsKey(c.Id) &&
                            numberedListPatterns.Any(pattern =>
                                pattern.IsMatch(c.Content)))
                        .ToList();

                    if (documentNumberedChunks.Count > 0)
                    {
                        var relevantNumberedChunks = new List<DocumentChunk>();

                        foreach (var chunk in documentNumberedChunks)
                        {
                            var numberedListCount = numberedListPatterns.Sum(pattern =>
                                pattern.Matches(chunk.Content).Count);

                            var wordMatches = localQueryWords.Count(word =>
                                chunk.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

                            if (wordMatches > 0 || numberedListCount >= 3)
                            {
                                // For chunks not in original list, calculate score without document-level boost
                                // (they weren't selected by document-level scoring, so lower priority)
                                chunk.RelevanceScore = wordMatches > 0
                                    ? 1000.0 + (numberedListCount * 100.0) + (wordMatches * 50.0)
                                    : 500.0 + (numberedListCount * 50.0);

                                numberedListChunkIds.Add(chunk.Id);
                                relevantNumberedChunks.Add(chunk);
                            }
                        }

                        allNumberedListChunks.AddRange(relevantNumberedChunks);
                    }
                }

                var finalQueryWords = QueryTokenizer.TokenizeQuery(query);
                var prioritizedNumberedChunks = allNumberedListChunks
                    .Select(c => new
                    {
                        Chunk = c,
                        HasQueryWords = finalQueryWords.Any(word =>
                            c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0),
                        QueryWordMatches = finalQueryWords.Count(word =>
                            c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0),
                        Score = c.RelevanceScore ?? 0.0,
                        IsFromOriginalChunks = existingChunksMap.ContainsKey(c.Id) // Chunks from original list have document-level boost
                    })
                    .OrderByDescending(x => x.Score) // Total score first (document-level boost preserved)
                    .ThenByDescending(x => x.HasQueryWords) // Query words second
                    .ThenByDescending(x => x.QueryWordMatches) // More matches = better
                    .ThenByDescending(x => x.IsFromOriginalChunks) // Original chunks (with document-level boost) prioritized
                    .Select(x => x.Chunk)
                    .ToList();

                var originalChunkIds = new HashSet<Guid>(chunks.Select(c => c.Id));
                var maxAdditionalNumberedChunks = 5;

                var newChunks = new List<DocumentChunk>(chunks);
                var seenIds = new HashSet<Guid>(chunks.Select(c => c.Id));
                var minRelevanceForNewChunks = chunks
                    .Where(c => c.RelevanceScore.HasValue)
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .Skip(chunks.Count / 2) // Median score
                    .FirstOrDefault()?.RelevanceScore ?? 0.0;

                var newNumberedChunks = prioritizedNumberedChunks
                    .Where(c => !seenIds.Contains(c.Id) && (c.RelevanceScore ?? 0.0) >= minRelevanceForNewChunks * 0.8)
                    .Take(maxAdditionalNumberedChunks)
                    .ToList();

                foreach (var chunk in newNumberedChunks)
                {
                    newChunks.Add(chunk);
                    seenIds.Add(chunk.Id);
                }

                chunks = newChunks
                    .OrderByDescending(c => c.ChunkIndex == 0)
                    .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                    .ThenBy(c => c.ChunkIndex)
                    .ToList();

                var originalChunksPreserved = chunks.Count(c => originalChunkIds.Contains(c.Id));
                var newChunksAdded = chunks.Count - originalChunksPreserved;
            }

            if (_contextExpansion != null && chunks.Count > 0 && numberedListChunkIds.Count == 0)
            {
                const double DocumentBoostThreshold = 200.0;
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

                    var contextWindow = RequiresComprehensiveSearch(query)
                        ? 3
                        : DetermineContextWindow(relevantDocumentChunks, query);

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

                            var numberedListPatterns = new[]
                            {
                                NumberedListPattern1,
                                NumberedListPattern2,
                                NumberedListPattern3,
                                NumberedListPattern4,
                                NumberedListPattern5,
                            };

                            var numberedListCount = numberedListPatterns.Sum(pattern =>
                                pattern.Matches(chunk.Content).Count);
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
                    chunks = chunks
                        .OrderByDescending(c => c.ChunkIndex == 0)
                        .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                        .ThenBy(c => c.ChunkIndex)
                        .ToList();
                }
            }
            else if (numberedListChunkIds.Count > 0)
            {
                chunks = chunks
                    .OrderByDescending(c => c.ChunkIndex == 0)
                    .ThenByDescending(c => numberedListChunkIds.Contains(c.Id))
                    .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                    .ThenBy(c => c.ChunkIndex)
                    .ToList();
                if (chunks.Count > MaxExpandedChunks)
                {
                    var chunk0Preserved = chunks.FirstOrDefault(c => c.ChunkIndex == 0);
                    var numberedListCount = chunks.Count(c => numberedListChunkIds.Contains(c.Id));
                    var numberedListChunks = chunks.Where(c => numberedListChunkIds.Contains(c.Id)).ToList();
                    var remainingSlots = MaxExpandedChunks - numberedListCount - (chunk0Preserved != null ? 1 : 0);
                    var otherChunks = chunks
                        .Where(c => !numberedListChunkIds.Contains(c.Id) && c.ChunkIndex != 0)
                        .Take(remainingSlots)
                        .ToList();

                    var finalChunks = new List<DocumentChunk>();
                    if (chunk0Preserved != null) finalChunks.Add(chunk0Preserved);
                    finalChunks.AddRange(numberedListChunks);
                    finalChunks.AddRange(otherChunks);
                    chunks = finalChunks;
                }
            }

            preservedChunk0 ??= chunks.FirstOrDefault(c => c.ChunkIndex == 0);

            var chunk0 = preservedChunk0 ?? chunks.FirstOrDefault(c => c.ChunkIndex == 0);
            var nonZeroChunks = chunks.Where(c => c.ChunkIndex != 0).ToList();

            var prioritizedNonZeroChunks = nonZeroChunks
                .OrderByDescending(c =>
                {
                    if (queryTokensForPrioritization.Count == 0) return 0;
                    var contentLower = c.Content?.ToLowerInvariant() ?? string.Empty;
                    return queryTokensForPrioritization.Count(token =>
                        token.Length >= 3 &&
                        contentLower.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
                })
                .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                .ThenBy(c => c.ChunkIndex)
                .ToList();

            chunks = chunk0 != null
                ? new List<DocumentChunk> { chunk0 }.Concat(prioritizedNonZeroChunks).ToList()
                : prioritizedNonZeroChunks;

            // Build context with size limit to prevent timeout
            var context = BuildLimitedContext(chunks);

            var prompt = _promptBuilder.BuildDocumentRagPrompt(query, context, conversationHistory, preferredLanguage);
            var answer = await _aiService.GenerateResponseAsync(prompt, new List<string> { context });

            return CreateRagResponse(query, answer, await _sourceBuilder.BuildSourcesAsync(chunks, _documentRepository));
        }

        private RagConfiguration GetRagConfiguration()
        {
            return new RagConfiguration
            {
                AIProvider = _options.AIProvider.ToString(),
                StorageProvider = _options.StorageProvider.ToString(),
                Model = _configuration["AI:OpenAI:Model"]
            };
        }

        /// <summary>
        /// Creates a RagResponse with standard configuration
        /// </summary>
        /// <param name="query">User query</param>
        /// <param name="answer">AI-generated answer</param>
        /// <param name="sources">List of search sources</param>
        /// <returns>Configured RagResponse</returns>
        private RagResponse CreateRagResponse(string query, string answer, List<SearchSource> sources)
        {
            return new RagResponse
            {
                Query = query,
                Answer = answer,
                Sources = sources,
                SearchedAt = DateTime.UtcNow,
                Configuration = GetRagConfiguration()
            };
        }

        /// <summary>
        /// [Document Query] Ultimate language-agnostic approach: ONLY check if documents contain relevant information
        /// No word patterns, no language detection, no grammar analysis, no greeting detection
        /// Pure content-based decision making
        /// Returns both the boolean decision and the found chunks to avoid redundant searches
        /// </summary>
        private async Task<(bool CanAnswer, List<DocumentChunk> Results)> CanAnswerFromDocumentsAsync(string query, SearchOptions? options = null, List<string>? queryTokens = null)
        {
            try
            {
                var searchResults = await PerformBasicSearchAsync(query, FallbackSearchMaxResults, options, queryTokens);

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
                // If there's an error, be conservative and assume it's document search
                ServiceLogMessages.LogCanAnswerFromDocumentsError(_logger, ex);
                return (true, new List<DocumentChunk>());
            }
        }


        /// <summary>
        /// Determines initial search count based on query structure
        /// Uses language-agnostic pattern detection (question marks, numeric patterns, query complexity)
        /// </summary>
        private int DetermineInitialSearchCount(string query, int defaultMaxResults)
        {
            // Detect if query needs comprehensive search using structural patterns
            if (RequiresComprehensiveSearch(query))
            {
                return defaultMaxResults * 3;
            }

            return defaultMaxResults;
        }

        /// <summary>
        /// Determines appropriate context window based on query structure
        /// Uses language-agnostic pattern detection
        /// </summary>
        private int DetermineContextWindow(List<DocumentChunk> chunks, string query)
        {
            const int DefaultWindow = 2;
            const int ComprehensiveWindow = 8;

            // Detect if query needs comprehensive context using structural patterns
            if (RequiresComprehensiveSearch(query))
            {
                return ComprehensiveWindow;
            }

            if (chunks.Count <= 3)
            {
                return 3;
            }

            return DefaultWindow;
        }

        /// <summary>
        /// Builds context string from chunks with size limit to prevent timeout
        /// </summary>
        private string BuildLimitedContext(List<DocumentChunk> chunks)
        {
            if (chunks == null || chunks.Count == 0)
            {
                return string.Empty;
            }

            var sortedChunks = chunks
                .OrderByDescending(c => c.ChunkIndex == 0)
                .ThenBy(c => c.ChunkIndex)
                .ToList();

            var chunk0Included = sortedChunks.Any(c => c.ChunkIndex == 0);
            if (chunk0Included)
            {
                var chunk0 = sortedChunks.First(c => c.ChunkIndex == 0);
                _logger.LogDebug("Chunk 0 included in context (first position). Content preview: {Preview}",
                    chunk0.Content?[..Math.Min(200, chunk0.Content?.Length ?? 0)] ?? "empty");
            }
            else
            {
                _logger.LogWarning("Chunk 0 NOT found in chunks list! Total chunks: {Count}", sortedChunks.Count);
            }

            var contextBuilder = new System.Text.StringBuilder();
            var totalSize = 0;

            foreach (var chunk in sortedChunks)
            {
                if (chunk?.Content == null)
                {
                    continue;
                }

                var chunkSize = chunk.Content.Length;
                var separatorSize = 2;

                if (totalSize + chunkSize + separatorSize > MaxContextSize)
                {
                    var remainingSize = MaxContextSize - totalSize - separatorSize;
                    if (remainingSize > 100)
                    {
                        var partialContent = chunk.Content[..Math.Min(remainingSize, chunk.Content.Length)];
                        if (contextBuilder.Length > 0)
                        {
                            contextBuilder.Append("\n\n");
                        }
                        contextBuilder.Append(partialContent);
                        ServiceLogMessages.LogContextSizeLimited(_logger, chunks.Count, totalSize + partialContent.Length, MaxContextSize, null);
                    }
                    break;
                }

                if (contextBuilder.Length > 0)
                {
                    contextBuilder.Append("\n\n");
                    totalSize += separatorSize;
                }

                contextBuilder.Append(chunk.Content);
                totalSize += chunkSize;
            }

            return contextBuilder.ToString();
        }

        /// <summary>
        /// Detects if query requires comprehensive search using language-agnostic patterns
        /// Checks for: question punctuation, numeric patterns, query complexity, list indicators
        /// </summary>
        private static bool RequiresComprehensiveSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            var trimmed = query.Trim();
            var tokens = trimmed.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Pattern 1: Question punctuation (works for all languages)
            if (HasQuestionPunctuation(trimmed))
            {
                // Check if it's a counting/listing question by structure
                if (HasNumericPattern(trimmed) || HasListIndicators(trimmed))
                {
                    return true;
                }
            }

            // Pattern 2: Numeric patterns (numbers, digits) - often indicates counting questions
            if (HasNumericPattern(trimmed))
            {
                return true;
            }

            // Pattern 3: Query complexity (longer queries often need more context)
            if (tokens.Length >= 6)
            {
                return true;
            }

            // Pattern 4: List indicators (structural patterns that suggest enumeration)
            if (HasListIndicators(trimmed))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if query contains question punctuation (language-agnostic)
        /// </summary>
        private static bool HasQuestionPunctuation(string input)
        {
            return input.IndexOf('?', StringComparison.Ordinal) >= 0 ||
                   input.IndexOf('¿', StringComparison.Ordinal) >= 0 ||
                   input.IndexOf('؟', StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// Checks if query contains numeric patterns (digits, numbers)
        /// </summary>
        private static bool HasNumericPattern(string input)
        {
            // Check for Unicode digits (works for all languages)
            if (input.Any(c => char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.DecimalDigitNumber))
            {
                return true;
            }

            // Check for multiple numeric groups (e.g., "1. Item 2. Item 3. Item")
            var numericMatches = NumericPattern.Matches(input);
            return numericMatches.Count >= 2;
        }

        /// <summary>
        /// Checks if query has structural patterns indicating list/enumeration needs
        /// </summary>
        private static bool HasListIndicators(string input)
        {
            // Pattern: Numbered lists (1. 2. 3. or 1) 2) 3))
            if (ListIndicatorPattern.IsMatch(input))
            {
                return true;
            }

            // Pattern: Multiple question marks or enumeration markers
            var questionCount = input.Count(c => c == '?' || c == '¿' || c == '؟');
            if (questionCount >= 2)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// [AI Query] Handle general conversation queries with conversation history
        /// </summary>
        private async Task<string> HandleGeneralConversationAsync(string query, string? conversationHistory = null, string? preferredLanguage = null)
        {
            try
            {
                var providerConfig = _aiConfiguration.GetAIProviderConfig();

                if (providerConfig == null)
                {
                    return ChatUnavailableMessage;
                }

                var aiProvider = _aiProviderFactory.CreateProvider(_options.AIProvider);

                var prompt = _promptBuilder.BuildConversationPrompt(query, conversationHistory, preferredLanguage);

                return await aiProvider.GenerateTextAsync(prompt, providerConfig);
            }
            catch (Exception)
            {
                // Log error using structured logging
                return ChatUnavailableMessage;
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

        /// <summary>
        /// Checks if a word exists in text with word boundaries (not as substring)
        /// </summary>
        private static bool IsWordInText(string text, string word)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(word))
                return false;

            // Check if word exists as a whole word (with word boundaries)
            // This prevents "kasko" from matching "kaskoda" or "laskos"
            var index = 0;
            while ((index = text.IndexOf(word, index, StringComparison.Ordinal)) >= 0)
            {
                // Check if this is a whole word match
                var isStartBoundary = (index == 0) || !char.IsLetterOrDigit(text[index - 1]);
                var endIndex = index + word.Length;
                var isEndBoundary = (endIndex >= text.Length) || !char.IsLetterOrDigit(text[endIndex]);

                if (isStartBoundary && isEndBoundary)
                {
                    return true; // Found whole word match
                }

                index++; // Continue searching
            }

            return false; // No whole word match found
        }
        /// <summary>
        /// Retrieves all documents filtered by the enabled search options (text, audio, image)
        /// </summary>
        private async Task<List<Entities.Document>> GetAllDocumentsFilteredAsync(SearchOptions? options)
        {
            var allDocuments = await _documentRepository.GetAllAsync();

            if (options == null)
            {
                return allDocuments;
            }

            return allDocuments.Where(d =>
                (options.EnableDocumentSearch && IsTextDocument(d)) ||
                (options.EnableAudioSearch && IsAudioDocument(d)) ||
                (options.EnableImageSearch && IsImageDocument(d))
            ).ToList();
        }

        private static bool IsAudioDocument(Entities.Document doc)
        {
            return !string.IsNullOrEmpty(doc.ContentType) &&
                   doc.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsImageDocument(Entities.Document doc)
        {
            return !string.IsNullOrEmpty(doc.ContentType) &&
                   doc.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTextDocument(Entities.Document doc)
        {
            // If it's not audio and not image, treat as text document
            return !IsAudioDocument(doc) && !IsImageDocument(doc);
        }
    }
}