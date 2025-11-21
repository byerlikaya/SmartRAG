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
        private const double RelevanceThreshold = 0.1;
        private const int MinSearchResultsCount = 0;
        private const int MinNameChunksCount = 0;
        private const int MinPotentialNamesCount = 2;
        private const int MinWordCountThreshold = 0;
        // Fallback search and content
        private const int FallbackSearchMaxResults = 5;
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
        private readonly IEmbeddingSearchService _embeddingSearch;
        private readonly ISourceBuilderService _sourceBuilder;
        private readonly IAIConfigurationService _aiConfiguration;
        private readonly IContextExpansionService? _contextExpansion;

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
        /// <param name="embeddingSearch">Service for embedding-based search</param>
        /// <param name="sourceBuilder">Service for building search sources</param>
        /// <param name="aiConfiguration">Service for AI provider configuration</param>
        /// <param name="contextExpansion">Service for expanding chunk context with adjacent chunks</param>
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
            IEmbeddingSearchService? embeddingSearch = null,
            ISourceBuilderService? sourceBuilder = null,
            IAIConfigurationService? aiConfiguration = null,
            IContextExpansionService? contextExpansion = null)
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
            _embeddingSearch = embeddingSearch ?? throw new ArgumentNullException(nameof(embeddingSearch));
            _sourceBuilder = sourceBuilder ?? throw new ArgumentNullException(nameof(sourceBuilder));
            _aiConfiguration = aiConfiguration ?? throw new ArgumentNullException(nameof(aiConfiguration));
            _contextExpansion = contextExpansion;
        }

        /// <summary>
        /// [Document Query] Searches for relevant document chunks based on the query
        /// </summary>
        /// <param name="query">Search query string</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of relevant document chunks</returns>
        public async Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            // Use our integrated search algorithm with diversity selection
            var searchResults = await PerformBasicSearchAsync(query, maxResults * InitialSearchMultiplier);

            if (searchResults.Count > 0)
            {
                // Apply diversity selection to ensure chunks from different documents (simple cap for now)
                return searchResults.Take(maxResults).ToList();
            }

            return searchResults;
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

            // Use provided options or fall back to global config
            var searchOptions = options ?? SearchOptions.FromConfig(_options);

            var originalQuery = query;
            var hasCommand = _queryIntentClassifier.TryParseCommand(query, out var commandType, out var commandPayload);

            // Handle force conversation command
            if (hasCommand && commandType == QueryCommandType.ForceConversation)
            {
                query = string.IsNullOrWhiteSpace(commandPayload)
                    ? string.Empty
                    : commandPayload;
            }

            // Check for new conversation command or parameter
            if (startNewConversation || (hasCommand && commandType == QueryCommandType.NewConversation))
            {
                await _conversationManager.StartNewConversationAsync();
                return CreateRagResponse(query, "New conversation started. How can I help you?", new List<SearchSource>());
            }

            // Generate or retrieve session ID automatically
            var sessionId = await _conversationManager.GetOrCreateSessionIdAsync();

            // Get conversation history from session
            var conversationHistory = await _conversationManager.GetConversationHistoryAsync(sessionId);

            // Handle forced conversation or detected general conversation upfront
            if ((hasCommand && commandType == QueryCommandType.ForceConversation) || await _queryIntentClassifier.IsGeneralConversationAsync(query, conversationHistory))
            {
                var conversationQuery = string.IsNullOrWhiteSpace(query)
                    ? originalQuery
                    : query;
                var conversationAnswer = await HandleGeneralConversationAsync(conversationQuery, conversationHistory);

                await _conversationManager.AddToConversationAsync(sessionId, conversationQuery, conversationAnswer);

                return CreateRagResponse(conversationQuery, conversationAnswer, new List<SearchSource>());
            }

            RagResponse response;

            // Pre-evaluate document availability for smarter strategy selection
            // Only check if document search is enabled
            var canAnswerFromDocuments = searchOptions.EnableDocumentSearch && await CanAnswerFromDocumentsAsync(query);

            // Smart Hybrid Approach: Check if database coordinator is available AND enabled
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
                    var strategy = DetermineQueryStrategy(confidence, hasDatabaseQueries, canAnswerFromDocuments);

                    // Execute strategy using switch-case (Open/Closed Principle)
                    // Pass pre-analyzed queryIntent (may be null) to avoid redundant AI calls
                    response = strategy switch
                    {
                        QueryStrategy.DatabaseOnly => await ExecuteDatabaseOnlyStrategyAsync(query, maxResults, conversationHistory, canAnswerFromDocuments, queryIntent),
                        QueryStrategy.DocumentOnly => await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, canAnswerFromDocuments),
                        QueryStrategy.Hybrid => await ExecuteHybridStrategyAsync(query, maxResults, conversationHistory, hasDatabaseQueries, canAnswerFromDocuments, queryIntent),
                        _ => await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, canAnswerFromDocuments) // Fallback
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during query intent analysis, falling back to document-only query");
                    response = await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, canAnswerFromDocuments);
                }
            }
            else
            {
                // Database coordinator not available or disabled → Fallback to document-only logic (if enabled)
                if (searchOptions.EnableDocumentSearch)
                {
                    _logger.LogInformation("Database search disabled or coordinator not available. Using document-only query.");
                    response = await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, canAnswerFromDocuments);
                }
                else
                {
                    // Both disabled? Fallback to chat
                     _logger.LogInformation("Both database and document search disabled. Falling back to general conversation.");
                    var chatResponse = await HandleGeneralConversationAsync(query, conversationHistory);
                    response = CreateRagResponse(query, chatResponse, new List<SearchSource>());
                }
            }

            // Add to conversation history
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
        /// <returns>Fallback RAG response</returns>
        private async Task<RagResponse> CreateFallbackResponseAsync(string query, string conversationHistory)
        {
            ServiceLogMessages.LogGeneralConversationQuery(_logger, null);
            var chatResponse = await HandleGeneralConversationAsync(query, conversationHistory);
            return CreateRagResponse(query, chatResponse, new List<SearchSource>());
        }

        /// <summary>
        /// [AI Query] [DB Query] Executes a database-only query strategy
        /// </summary>
        private async Task<RagResponse> ExecuteDatabaseOnlyStrategyAsync(string query, int maxResults, string conversationHistory, bool canAnswerFromDocuments, QueryIntent? queryIntent)
        {
            _logger.LogInformation("Executing database-only query strategy");
            
            try
            {
                if (queryIntent == null)
            {
                // No intent analysis, fallback to document query
                return await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, canAnswerFromDocuments);
            }
            var databaseResponse = await _multiDatabaseQueryCoordinator!.QueryMultipleDatabasesAsync(query, queryIntent, maxResults);
                if (HasMeaningfulData(databaseResponse))
                {
                    return databaseResponse;
                }

                _logger.LogInformation("Database query returned no meaningful data, falling back to document search");
                return await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, canAnswerFromDocuments);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database query failed, falling back to document query");
                return await ExecuteDocumentQueryAsync(query, maxResults, conversationHistory, canAnswerFromDocuments);
            }
        }



        /// <summary>
        /// [AI Query] [DB Query] [Document Query] Executes a hybrid query strategy (both database and document queries)
        /// </summary>
        private async Task<RagResponse> ExecuteHybridStrategyAsync(string query, int maxResults, string conversationHistory, bool hasDatabaseQueries, bool canAnswerFromDocuments, QueryIntent? queryIntent)
        {
            _logger.LogInformation("Executing hybrid query strategy (database + documents)");
            
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
                            _logger.LogInformation("Database query completed successfully with meaningful data");
                        }
                        else
                        {
                            _logger.LogInformation("Database query completed without meaningful data, excluding from hybrid merge");
                        }
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
                documentResponse = await GenerateBasicRagAnswerAsync(query, maxResults, conversationHistory);
                _logger.LogInformation("Document query completed successfully");
            }

            // Merge results if both queries executed
            if (databaseResponse != null && documentResponse != null)
            {
                return await MergeHybridResultsAsync(query, databaseResponse, documentResponse, conversationHistory);
            }

            // Return available response or fallback
            if (databaseResponse != null)
                return databaseResponse;

            if (documentResponse != null)
                return documentResponse;

            return await CreateFallbackResponseAsync(query, conversationHistory);
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
        /// <returns>Merged RAG response</returns>
        private async Task<RagResponse> MergeHybridResultsAsync(string query, RagResponse databaseResponse, RagResponse documentResponse, string conversationHistory)
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

            var mergePrompt = _promptBuilder.BuildHybridMergePrompt(query, databaseContext, documentContext, conversationHistory);
            var mergedAnswer = await _aiService.GenerateResponseAsync(mergePrompt, combinedContext);

            _logger.LogInformation("Hybrid search completed. Combined {DatabaseSources} database sources and {DocumentSources} document sources",
                databaseResponse.Sources.Count, documentResponse.Sources.Count);

            return CreateRagResponse(query, mergedAnswer, combinedSources);
        }


        /// <summary>
        /// [Document Query] Common method for executing document-based queries (used by both document-only and fallback strategies)
        /// </summary>
        private async Task<RagResponse> ExecuteDocumentQueryAsync(string query, int maxResults, string conversationHistory, bool? canAnswerFromDocuments = null)
        {
            var canAnswer = canAnswerFromDocuments ?? await CanAnswerFromDocumentsAsync(query);

            if (canAnswer)
            {
                return await GenerateBasicRagAnswerAsync(query, maxResults, conversationHistory);
            }

            return await CreateFallbackResponseAsync(query, conversationHistory);
        }

        /// <summary>
        /// [Document Query] Enhanced search with intelligent filtering and name detection
        /// </summary>
        private async Task<List<DocumentChunk>> PerformBasicSearchAsync(string query, int maxResults)
        {
            var allDocuments = await _documentRepository.GetAllAsync();
            var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();

            // Try embedding-based search first if available
            try
            {
                var embeddingResults = await _embeddingSearch.SearchByEmbeddingAsync(query, allChunks, maxResults);
                if (embeddingResults.Count > 0)
                {
                    return embeddingResults;
                }
            }
            catch (Exception)
            {
                // Fallback to keyword-based scoring
            }

            // Enhanced keyword-based fallback for global content
            var queryWords = QueryTokenizer.TokenizeQuery(query);
            var potentialNames = QueryTokenizer.ExtractPotentialNames(query);

            var scoredChunks = _documentScoring.ScoreChunks(allChunks, query, queryWords, potentialNames);

            const int CandidateMultiplier = 3;
            const int CandidateMinCount = 30;
            
            var relevantChunks = scoredChunks
                .Where(c => c.RelevanceScore > MinWordCountThreshold)
                .OrderByDescending(c => c.RelevanceScore)
                .Take(Math.Max(maxResults * CandidateMultiplier, CandidateMinCount))
                .ToList();

            // If we found chunks with names, prioritize them
            if (potentialNames.Count >= MinPotentialNamesCount)
            {
                var nameChunks = relevantChunks.Where(c =>
                    potentialNames.Any(name => c.Content.ToLowerInvariant().Contains(name.ToLowerInvariant()))).ToList();

                if (nameChunks.Count > MinNameChunksCount)
                {
                    return nameChunks.Take(maxResults).ToList();
                }
            }

            // For counting/listing questions, prioritize chunks with numbered lists
            if (RequiresComprehensiveSearch(query))
            {
                var numberedListPatterns = new[]
                {
                    @"\b\d+\.\s",      // "1. Item"
                    @"\b\d+\)\s",      // "1) Item"
                    @"\b\d+-\s",       // "1- Item"
                    @"\b\d+\s+[A-Z]",  // "1 Item" (number followed by capital letter)
                    @"^\d+\.\s",       // "1. Item" at start of line
                };
                
                // CRITICAL: Search in ALL chunks, not just relevantChunks
                // Numbered list chunks might not have high relevance score but contain the answer
                var allNumberedListChunks = allChunks
                    .Where(c => numberedListPatterns.Any(pattern => 
                        Regex.IsMatch(c.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase)))
                    .Select(c =>
                    {
                        // Count numbered items and calculate score
                        var numberedListCount = numberedListPatterns.Sum(pattern => 
                            Regex.Matches(c.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase).Count);
                        
                        var wordMatches = queryWords.Count(word => 
                            c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
                        
                        // Score: numbered list count (high priority) + word matches
                        c.RelevanceScore = (numberedListCount * 100.0) + (wordMatches * 10.0);
                        
                        return new
                        {
                            Chunk = c,
                            NumberedListCount = numberedListCount,
                            WordMatches = wordMatches
                        };
                    })
                    .OrderByDescending(x => x.NumberedListCount) // More items = higher priority
                    .ThenByDescending(x => x.WordMatches) // Then by word matches
                    .Select(x => x.Chunk)
                    .Take(maxResults * 2) // Take more numbered list chunks
                    .ToList();
                
                if (allNumberedListChunks.Count > 0)
                {
                    // Return numbered list chunks first (even if they weren't in relevantChunks)
                    // Then add relevant chunks that aren't numbered lists
                    var result = allNumberedListChunks
                        .Concat(relevantChunks.Except(allNumberedListChunks))
                        .Take(maxResults)
                        .ToList();
                    return result;
                }
            }

            return relevantChunks.Take(maxResults).ToList();
        }

        /// <summary>
        /// [AI Query] Generate RAG answer with automatic session management
        /// </summary>
        private async Task<RagResponse> GenerateBasicRagAnswerAsync(string query, int maxResults, string conversationHistory)
        {
            // For questions asking "how many", "which", "where" etc., search for more chunks initially
            // These questions often need information from multiple chunks (e.g., numbered lists)
            var searchMaxResults = DetermineInitialSearchCount(query, maxResults);
            var chunks = await SearchDocumentsAsync(query, searchMaxResults);
            
            // If initial search found few chunks OR if this is a counting/listing question, try more aggressive search
            // This is especially important for "how many" type questions that need list enumeration
            var needsAggressiveSearch = chunks.Count < 5 || RequiresComprehensiveSearch(query);
            if (needsAggressiveSearch)
            {
                // Try with even more chunks using direct repository search
                var allDocuments = await _documentRepository.GetAllAsync();
                var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();
                
                // Use keyword-based fallback search with more aggressive matching
                var queryWords = QueryTokenizer.TokenizeQuery(query);
                
                // For counting questions, prioritize chunks with numbers (likely contain numbered lists)
                // Use multiple patterns to detect numbered lists: "1.", "1)", "1-", "1 ", etc.
                var numberedListPatterns = new[]
                {
                    @"\b\d+\.\s",      // "1. Item"
                    @"\b\d+\)\s",      // "1) Item"
                    @"\b\d+-\s",       // "1- Item"
                    @"\b\d+\s+[A-Z]",  // "1 Item" (number followed by capital letter)
                    @"^\d+\.\s",       // "1. Item" at start of line
                };
                
                // CRITICAL: For counting questions, first find ALL chunks with numbered lists
                // Then filter by query words, prioritizing chunks with both numbered lists AND query words
                var allChunksWithNumberedLists = allChunks
                    .Select(c =>
                    {
                        // Count how many numbered list items are in this chunk
                        var numberedListCount = numberedListPatterns.Sum(pattern => 
                            Regex.Matches(c.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase).Count);
                        
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
                
                // If this is a counting question, prioritize numbered list chunks even if they don't match query words perfectly
                if (RequiresComprehensiveSearch(query))
                {
                    // First: Chunks with numbered lists AND query words (highest priority)
                    var numberedListWithQueryWords = allChunksWithNumberedLists
                        .Where(x => x.HasNumberedList && x.WordMatches > 0)
                        .OrderByDescending(x => x.NumberedListCount)
                        .ThenByDescending(x => x.WordMatches)
                        .Select(x => x.Chunk)
                        .Take(searchMaxResults * 3)
                        .ToList();
                    
                    // Second: Chunks with numbered lists even without query words (for comprehensive coverage)
                    var numberedListOnly = allChunksWithNumberedLists
                        .Where(x => x.HasNumberedList && x.WordMatches == 0)
                        .OrderByDescending(x => x.NumberedListCount)
                        .Select(x => x.Chunk)
                        .Take(searchMaxResults * 2)
                        .ToList();
                    
                    // Third: Chunks with query words but no numbered lists
                    var queryWordsOnly = allChunksWithNumberedLists
                        .Where(x => !x.HasNumberedList && x.WordMatches > 0)
                        .OrderByDescending(x => x.WordMatches)
                        .ThenByDescending(x => x.HasNumbers)
                        .Select(x => x.Chunk)
                        .Take(searchMaxResults * 2)
                        .ToList();
                    
                    // Combine: numbered lists first, then others
                    var mergedChunks = new List<DocumentChunk>();
                    var seenIds = new HashSet<Guid>();
                    
                    // Add numbered list chunks with query words first (highest priority)
                    foreach (var chunk in numberedListWithQueryWords)
                    {
                        if (!seenIds.Contains(chunk.Id))
                        {
                            mergedChunks.Add(chunk);
                            seenIds.Add(chunk.Id);
                        }
                    }
                    
                    // Add numbered list chunks without query words (for comprehensive coverage)
                    foreach (var chunk in numberedListOnly)
                    {
                        if (!seenIds.Contains(chunk.Id) && mergedChunks.Count < searchMaxResults * 4)
                        {
                            mergedChunks.Add(chunk);
                            seenIds.Add(chunk.Id);
                        }
                    }
                    
                    // Add query word chunks if we still have space
                    foreach (var chunk in queryWordsOnly)
                    {
                        if (!seenIds.Contains(chunk.Id) && mergedChunks.Count < searchMaxResults * 4)
                        {
                            mergedChunks.Add(chunk);
                            seenIds.Add(chunk.Id);
                        }
                    }
                    
                    // Add original chunks that weren't already included
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
                    // For non-counting questions, use standard fallback search
                    var fallbackChunks = allChunksWithNumberedLists
                        .Where(x => x.WordMatches > 0)
                        .OrderByDescending(x => x.NumberedListCount)
                        .ThenByDescending(x => x.HasNumberedList)
                        .ThenByDescending(x => x.HasNumbers)
                        .ThenByDescending(x => x.WordMatches)
                        .Select(x => x.Chunk)
                        .Take(searchMaxResults * 4)
                        .ToList();
                    
                    if (fallbackChunks.Count > chunks.Count)
                    {
                        // Merge with original chunks, prioritizing fallback results
                        var mergedChunks = new List<DocumentChunk>();
                        var seenIds = new HashSet<Guid>();
                        
                        // Add fallback chunks first (they're prioritized)
                        foreach (var chunk in fallbackChunks)
                        {
                            if (!seenIds.Contains(chunk.Id))
                            {
                                mergedChunks.Add(chunk);
                                seenIds.Add(chunk.Id);
                            }
                        }
                        
                        // Add original chunks that weren't already included
                        foreach (var chunk in chunks)
                        {
                            if (!seenIds.Contains(chunk.Id))
                            {
                                mergedChunks.Add(chunk);
                                seenIds.Add(chunk.Id);
                            }
                        }
                        
                        chunks = mergedChunks;
                        ServiceLogMessages.LogFallbackSearchUsed(_logger, chunks.Count, null);
                    }
                }
            }
            
            // CRITICAL: For counting/listing questions, find ALL numbered list chunks from the same documents
            // This ensures we get the complete list even if it's split across multiple chunks
            var numberedListChunkIds = new HashSet<Guid>();
            if (RequiresComprehensiveSearch(query) && chunks.Count > 0)
            {
                var numberedListPatterns = new[]
                {
                    @"\b\d+\.\s",      // "1. Item"
                    @"\b\d+\)\s",      // "1) Item"
                    @"\b\d+-\s",       // "1- Item"
                    @"\b\d+\s+[A-Z]",  // "1 Item" (number followed by capital letter)
                    @"^\d+\.\s",       // "1. Item" at start of line
                };
                
                // Get all documents that contain the found chunks
                var documentIds = chunks.Select(c => c.DocumentId).Distinct().ToList();
                var allNumberedListChunks = new List<DocumentChunk>();
                
                foreach (var documentId in documentIds)
                {
                    var document = await _documentRepository.GetByIdAsync(documentId);
                    if (document?.Chunks == null) continue;
                    
                    // Find ALL chunks in this document that contain numbered lists
                    var documentNumberedChunks = document.Chunks
                        .Where(c => numberedListPatterns.Any(pattern => 
                            Regex.IsMatch(c.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase)))
                        .ToList();
                    
                    if (documentNumberedChunks.Count > 0)
                    {
                        // Score these chunks based on numbered list count and query word matches
                        var localQueryWords = QueryTokenizer.TokenizeQuery(query);
                        
                        // CRITICAL: Filter and prioritize chunks that contain BOTH numbered lists AND query words
                        // This prevents including irrelevant numbered lists (like page numbers, references, etc.)
                        var relevantNumberedChunks = new List<DocumentChunk>();
                        
                        foreach (var chunk in documentNumberedChunks)
                        {
                            var numberedListCount = numberedListPatterns.Sum(pattern => 
                                Regex.Matches(chunk.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase).Count);
                            
                            var wordMatches = localQueryWords.Count(word => 
                                chunk.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
                            
                            // CRITICAL: Only include chunks that have query word matches OR have many numbered items (likely a list)
                            // This filters out page numbers, references, etc. that are numbered but not relevant
                            if (wordMatches > 0 || numberedListCount >= 3)
                            {
                                // VERY HIGH score for numbered lists with query words
                                // Lower score for numbered lists without query words (but still include if many items)
                                chunk.RelevanceScore = wordMatches > 0
                                    ? 1000.0 + (numberedListCount * 100.0) + (wordMatches * 50.0) // High priority: numbered list + query words
                                    : 500.0 + (numberedListCount * 50.0); // Lower priority: numbered list only (but many items)
                                
                                numberedListChunkIds.Add(chunk.Id);
                                relevantNumberedChunks.Add(chunk);
                            }
                        }
                        
                        allNumberedListChunks.AddRange(relevantNumberedChunks);
                        _logger.LogInformation("Found {TotalCount} numbered list chunks in document {DocumentId}, {RelevantCount} relevant (with query words or 3+ items)", 
                            documentNumberedChunks.Count, documentId, relevantNumberedChunks.Count);
                    }
                }
                
                _logger.LogInformation("Total numbered list chunks found: {Count}", allNumberedListChunks.Count);
                
                // CRITICAL: Prioritize chunks with query words - these are most relevant
                var finalQueryWords = QueryTokenizer.TokenizeQuery(query);
                var prioritizedNumberedChunks = allNumberedListChunks
                    .Select(c => new
                    {
                        Chunk = c,
                        HasQueryWords = finalQueryWords.Any(word => 
                            c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0),
                        QueryWordMatches = finalQueryWords.Count(word => 
                            c.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0),
                        Score = c.RelevanceScore ?? 0.0
                    })
                    .OrderByDescending(x => x.HasQueryWords) // Query words first
                    .ThenByDescending(x => x.QueryWordMatches) // More matches = better
                    .ThenByDescending(x => x.Score) // Then by score
                    .Select(x => x.Chunk)
                    .ToList();
                
                // CRITICAL: Replace chunks with numbered list chunks (they're more relevant for counting questions)
                // Keep original chunks only if they're not in numbered list chunks
                var originalChunkIds = new HashSet<Guid>(chunks.Select(c => c.Id));
                var maxNumberedChunks = 30; // Limit to top 30 numbered list chunks
                var topNumberedChunks = prioritizedNumberedChunks.Take(maxNumberedChunks).ToList();
                
                // Create new chunk list: numbered list chunks first, then original chunks that aren't numbered lists
                var newChunks = new List<DocumentChunk>();
                var seenIds = new HashSet<Guid>();
                
                // Add numbered list chunks first (highest priority)
                foreach (var numberedChunk in topNumberedChunks)
                {
                    newChunks.Add(numberedChunk);
                    seenIds.Add(numberedChunk.Id);
                }
                
                // Add original chunks that aren't numbered lists
                foreach (var originalChunk in chunks)
                {
                    if (!seenIds.Contains(originalChunk.Id) && !numberedListChunkIds.Contains(originalChunk.Id))
                    {
                        newChunks.Add(originalChunk);
                        seenIds.Add(originalChunk.Id);
                    }
                }
                
                chunks = newChunks;
                
                _logger.LogInformation("Replaced chunks with {NumberedCount} numbered list chunks + {OriginalCount} original chunks (from {TotalNumbered} found)", 
                    topNumberedChunks.Count, chunks.Count - topNumberedChunks.Count, allNumberedListChunks.Count);
            }
            
            // Expand context by including adjacent chunks from the same document
            // This ensures that if a heading is in one chunk and content is in the next, both are included
            // For questions asking "how many", "which", "where" etc., use larger context window
            // CRITICAL: If we found numbered list chunks, skip context expansion to preserve them
            if (_contextExpansion != null && chunks.Count > 0 && numberedListChunkIds.Count == 0)
            {
                // Store original chunk IDs and their relevance scores before expansion
                var originalChunkIds = new HashSet<Guid>(chunks.Select(c => c.Id));
                var originalScores = chunks.ToDictionary(c => c.Id, c => c.RelevanceScore ?? 0.0);
                
                // For counting questions, use smaller context window to avoid too many chunks
                // We already found all numbered list chunks above, so we just need adjacent context
                var contextWindow = RequiresComprehensiveSearch(query) 
                    ? 3 // Smaller window for comprehensive queries (we already have numbered list chunks)
                    : DetermineContextWindow(chunks, query);
                chunks = await _contextExpansion.ExpandContextAsync(chunks, contextWindow);
                
                // Re-score expanded chunks that don't have scores (adjacent chunks added by expansion)
                // Use query words to calculate basic relevance for expanded chunks
                var queryWords = QueryTokenizer.TokenizeQuery(query);
                foreach (var chunk in chunks)
                {
                    if (!originalScores.ContainsKey(chunk.Id))
                    {
                        // Calculate basic relevance score for expanded chunks
                        var content = chunk.Content.ToLowerInvariant();
                        var wordMatches = queryWords.Count(word => 
                            content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
                        var hasNumbers = chunk.Content.Any(char.IsDigit);
                        
                        // Use multiple patterns to detect numbered lists
                        var numberedListPatterns = new[]
                        {
                            @"\b\d+\.\s",      // "1. Item"
                            @"\b\d+\)\s",      // "1) Item"
                            @"\b\d+-\s",       // "1- Item"
                            @"\b\d+\s+[A-Z]",  // "1 Item" (number followed by capital letter)
                            @"^\d+\.\s",       // "1. Item" at start of line
                        };
                        
                        var numberedListCount = numberedListPatterns.Sum(pattern => 
                            Regex.Matches(chunk.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase).Count);
                        var hasNumberedList = numberedListCount > 0;
                        
                        // Score: word matches + bonus for numbers + HIGH bonus for numbered lists (especially multiple items)
                        chunk.RelevanceScore = (wordMatches * 0.1) + 
                                             (hasNumbers ? 0.2 : 0.0) + 
                                             (hasNumberedList ? 0.5 + (numberedListCount * 0.1) : 0.0); // Higher bonus for numbered lists, even more for multiple items
                    }
                    else
                    {
                        // Preserve original score
                        chunk.RelevanceScore = originalScores[chunk.Id];
                    }
                }
                
                // CRITICAL: Sort chunks with numbered lists at the top (highest priority)
                // Then original chunks, then expanded chunks
                chunks = chunks
                    .OrderByDescending(c => numberedListChunkIds.Contains(c.Id)) // Numbered list chunks FIRST (highest priority)
                    .ThenByDescending(c => originalChunkIds.Contains(c.Id)) // Then original chunks
                    .ThenByDescending(c => c.RelevanceScore ?? 0.0) // Then by relevance score
                    .ToList();
                
                // Limit expanded chunks to prevent excessive context and timeout
                // But ALWAYS keep numbered list chunks (they're already at the top)
                if (chunks.Count > MaxExpandedChunks)
                {
                    // Count how many numbered list chunks we have
                    var numberedListCount = chunks.Count(c => numberedListChunkIds.Contains(c.Id));
                    
                    // Keep all numbered list chunks + top other chunks
                    var numberedListChunks = chunks.Where(c => numberedListChunkIds.Contains(c.Id)).ToList();
                    var otherChunks = chunks.Where(c => !numberedListChunkIds.Contains(c.Id))
                        .Take(MaxExpandedChunks - numberedListCount)
                        .ToList();
                    
                    chunks = numberedListChunks.Concat(otherChunks).ToList();
                    ServiceLogMessages.LogContextExpansionLimited(_logger, MaxExpandedChunks, null);
                }
            }
            else if (numberedListChunkIds.Count > 0)
            {
                // We have numbered list chunks, skip context expansion to preserve them
                // Just ensure they're at the top
                chunks = chunks
                    .OrderByDescending(c => numberedListChunkIds.Contains(c.Id))
                    .ThenByDescending(c => c.RelevanceScore ?? 0.0)
                    .ToList();
                
                _logger.LogInformation("Skipped context expansion to preserve {Count} numbered list chunks", numberedListChunkIds.Count);
                
                // Limit chunks but keep all numbered list chunks
                if (chunks.Count > MaxExpandedChunks)
                {
                    var numberedListCount = chunks.Count(c => numberedListChunkIds.Contains(c.Id));
                    var numberedListChunks = chunks.Where(c => numberedListChunkIds.Contains(c.Id)).ToList();
                    var otherChunks = chunks.Where(c => !numberedListChunkIds.Contains(c.Id))
                        .Take(MaxExpandedChunks - numberedListCount)
                        .ToList();
                    
                    chunks = numberedListChunks.Concat(otherChunks).ToList();
                    _logger.LogInformation("Limited to {Count} chunks, kept {NumberedCount} numbered list chunks", 
                        chunks.Count, numberedListCount);
                }
            }
            
            // Build context with size limit to prevent timeout
            var context = BuildLimitedContext(chunks);

            var prompt = _promptBuilder.BuildDocumentRagPrompt(query, context, conversationHistory);
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
        /// </summary>
        private async Task<bool> CanAnswerFromDocumentsAsync(string query)
        {
            try
            {
                // Step 1: Search documents for any content related to the query
                // This works regardless of the language of the query
                var searchResults = await PerformBasicSearchAsync(query, FallbackSearchMaxResults);

                if (searchResults.Count == MinSearchResultsCount)
                {
                    // No content found that matches the query in any way
                    return false;
                }

                // Step 2: Check if we found meaningful content with decent relevance
                var hasRelevantContent = searchResults.Any(chunk =>
                    chunk.RelevanceScore > RelevanceThreshold);

                if (!hasRelevantContent)
                {
                    // Found some content but it's not relevant enough
                    return false;
                }

                // Step 3: Check if the total content is substantial enough to potentially answer
                var totalContentLength = searchResults
                    .Where(c => c.RelevanceScore > RelevanceThreshold)
                    .Sum(c => c.Content.Length);

                var hasSubstantialContent = totalContentLength > MinSubstantialContentLength;

                // Final decision: If we have relevant and substantial content, use document search
                // No other checks - let the content decide!
                return hasRelevantContent && hasSubstantialContent;
            }
            catch (Exception ex)
            {
                // If there's an error, be conservative and assume it's document search
                ServiceLogMessages.LogCanAnswerFromDocumentsError(_logger, ex);
                return true;
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
                // Search for 3x more chunks for comprehensive questions (especially "how many" type questions)
                // This ensures we find all relevant chunks including numbered lists
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
            // Default context window for regular documents
            const int DefaultWindow = 2;
            // Larger context window for questions that need comprehensive information
            const int ComprehensiveWindow = 8; // Increased to 8 for better list enumeration (to capture full numbered lists)

            // Detect if query needs comprehensive context using structural patterns
            if (RequiresComprehensiveSearch(query))
            {
                return ComprehensiveWindow;
            }

            // If initial search found few chunks, use larger window to catch more context
            if (chunks.Count <= 3)
            {
                return 3; // Medium window when few chunks found
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

            var contextBuilder = new System.Text.StringBuilder();
            var totalSize = 0;

            foreach (var chunk in chunks)
            {
                if (chunk?.Content == null)
                {
                    continue;
                }

                var chunkSize = chunk.Content.Length;
                var separatorSize = 2; // "\n\n" separator

                // Check if adding this chunk would exceed the limit
                if (totalSize + chunkSize + separatorSize > MaxContextSize)
                {
                    // Try to add partial chunk if there's room
                    var remainingSize = MaxContextSize - totalSize - separatorSize;
                    if (remainingSize > 100) // Only add if there's meaningful space (at least 100 chars)
                    {
                        var partialContent = chunk.Content.Substring(0, Math.Min(remainingSize, chunk.Content.Length));
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
            var numericMatches = System.Text.RegularExpressions.Regex.Matches(input, @"\p{Nd}+");
            return numericMatches.Count >= 2;
        }

        /// <summary>
        /// Checks if query has structural patterns indicating list/enumeration needs
        /// </summary>
        private static bool HasListIndicators(string input)
        {
            // Pattern: Numbered lists (1. 2. 3. or 1) 2) 3))
            if (System.Text.RegularExpressions.Regex.IsMatch(input, @"\d+[\.\)]\s"))
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
        private async Task<string> HandleGeneralConversationAsync(string query, string? conversationHistory = null)
        {
            try
            {
                var providerConfig = _aiConfiguration.GetAIProviderConfig();
                
                if (providerConfig == null)
                {
                    return ChatUnavailableMessage;
                }

                var aiProvider = _aiProviderFactory.CreateProvider(_options.AIProvider);

                var prompt = _promptBuilder.BuildConversationPrompt(query, conversationHistory);

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
    }
}
