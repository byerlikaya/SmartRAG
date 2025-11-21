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
                    
                    // Count exact matches
                    var exactMatches = (docContent.Length - docContent.Replace(wordLower, "").Length) / wordLower.Length;
                    if (exactMatches > 0)
                    {
                        wordFound = true;
                        occurrences += exactMatches;
                    }
                    
                    // Also check for substring matches (for agglutinative languages)
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
                // Count how many query words are UNIQUE to this document (discriminators)
                // Example: "kasko" appears only in KASKO doc, not in IONIQ5 manual → very valuable!
                var uniqueKeywordCount = 0;
                foreach (var word in queryWords)
                {
                    if (queryWordDocumentMap.TryGetValue(word, out var docsWithWord))
                    {
                        // Check if this query word appears ONLY in this document
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
            
            var otherDocumentChunks = allDocuments
                .Except(relevantDocuments)
                .SelectMany(d => scoredChunks.Where(c => c.DocumentId == d.Id))
                .ToList();
            
            // Apply document-level boost to chunks from relevant documents
            // CRITICAL: Very high boost to ensure relevant documents are strongly prioritized
            const double DocumentRelevanceBoost = 200.0; // Significantly increased from 50.0
            foreach (var chunk in relevantDocumentChunks)
            {
                chunk.RelevanceScore = (chunk.RelevanceScore ?? 0.0) + DocumentRelevanceBoost;
            }
            
            // Combine: relevant document chunks first, then others
            var finalScoredChunks = relevantDocumentChunks.Concat(otherDocumentChunks).ToList();

            const int CandidateMultiplier = 20; // Significantly increased to get many more chunks from relevant documents
            const int CandidateMinCount = 200; // Significantly increased to get many more chunks
            
            // CRITICAL: Prioritize chunks from relevant documents (they have document-level boost)
            // Take more chunks from relevant documents to ensure we get the right content
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
            
            // Enhanced logging to show document scores and query coverage for debugging
            var topDoc = documentScores.FirstOrDefault();
            var secondDoc = documentScores.Skip(1).FirstOrDefault();
            var topDocName = topDoc != null ? allDocuments.FirstOrDefault(d => d.Id == topDoc.Document.Id)?.FileName : "Unknown";
            var secondDocName = secondDoc != null ? allDocuments.FirstOrDefault(d => d.Id == secondDoc.Document.Id)?.FileName : null;
            
            if (topDoc != null)
            {
                // Helper to get unique keywords for logging
                Func<Entities.Document, string> getUniqueWords = (d) => {
                    var words = new List<string>();
                    foreach(var kvp in queryWordDocumentMap) {
                        if(kvp.Value.Count == 1 && kvp.Value.Contains(d.Id)) words.Add(kvp.Key);
                    }
                    return string.Join(",", words);
                };

                if (secondDoc != null)
                {
                    _logger.LogInformation("Document scoring: Top={TopDocName} (Score={TopScore:F1}, QueryMatches={TopMatches}/{TotalQueryWords}, UniqueKeywords={TopUnique} [{TopUniqueWords}]), Second={SecondDocName} (Score={SecondScore:F1}, QueryMatches={SecondMatches}/{TotalQueryWords}, UniqueKeywords={SecondUnique} [{SecondUniqueWords}])",
                        topDocName, topDoc.Score, topDoc.QueryWordMatches, queryWords.Count, topDoc.UniqueKeywords, getUniqueWords(topDoc.Document),
                        secondDocName, secondDoc.Score, secondDoc.QueryWordMatches, queryWords.Count, secondDoc.UniqueKeywords, getUniqueWords(secondDoc.Document));
                }
                else
                {
                    _logger.LogInformation("Document scoring: Top={TopDocName} (Score={TopScore:F1}, QueryMatches={TopMatches}/{TotalQueryWords}, UniqueKeywords={TopUnique} [{TopUniqueWords}])",
                        topDocName, topDoc.Score, topDoc.QueryWordMatches, queryWords.Count, topDoc.UniqueKeywords, getUniqueWords(topDoc.Document));
                }
            }
            
            _logger.LogInformation("Selected {Count} chunks from {DocCount} documents. Top document: {TopDocId} with {TopCount} chunks (avg score: {TopScore})",
                relevantChunks.Count, docDistribution.Count, 
                docDistribution.FirstOrDefault()?.DocId, 
                docDistribution.FirstOrDefault()?.Count ?? 0,
                docDistribution.FirstOrDefault()?.AvgScore ?? 0.0);

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
                // But prioritize chunks from relevant documents
                var allNumberedListChunks = finalScoredChunks
                    .Where(c => numberedListPatterns.Any(pattern => 
                        Regex.IsMatch(c.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase)))
                    .Select(c =>
                    {
                        // Preserve existing relevance score (includes document-level boost)
                        var baseScore = c.RelevanceScore ?? 0.0;
                        
                        // Count numbered items and calculate additional score
                        var numberedListCount = numberedListPatterns.Sum(pattern => 
                            Regex.Matches(c.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase).Count);
                        
                        var wordMatches = queryWords.Count(word => 
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
                    .OrderByDescending(x => x.TotalScore) // Sort by total score (includes document-level boost)
                    .ThenByDescending(x => x.NumberedListCount) // Then by numbered list count
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
                var potentialNames = QueryTokenizer.ExtractPotentialNames(query);
                
                // Apply document-level scoring first (same as PerformBasicSearchAsync)
                var scoredChunks = _documentScoring.ScoreChunks(allChunks, query, queryWords, potentialNames);
                
                // Calculate document-level relevance: average of top N chunks per document
                // Also consider how many query words are matched across the document
                // CRITICAL: This must match PerformBasicSearchAsync logic for consistency
                // CRITICAL: Count document-specific keywords (words that appear in only one document)
                const int TopChunksPerDocument = 5;
                const int ChunksToCheckForKeywords = 30; // Check top 30 chunks per document for unique keywords
                
                // First, identify which query words appear in which documents
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
                        
                        // Count exact matches
                        var exactMatches = (docContent.Length - docContent.Replace(wordLower, "").Length) / wordLower.Length;
                        if (exactMatches > 0)
                        {
                            wordFound = true;
                            occurrences += exactMatches;
                        }
                        
                        // Also check for substring matches (for agglutinative languages)
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
                    
                    
                    // CRITICAL FIX: Prioritize documents containing ALL query words (same as PerformBasicSearchAsync)
                    // Query coverage (what % of query words are in this document) is MORE important than frequency
                    var queryCoverageRatio = queryWords.Count > 0 ? (double)queryWordMatches / queryWords.Count : 0.0;
                    
                    // MASSIVE bonus for high query coverage
                    // 100% coverage (all query words present) = 5000+ bonus
                    // 50% coverage = 1250 bonus  
                    // Exponential bonus rewards documents that contain ALL query terms
                    var queryCoverageBonus = queryCoverageRatio * queryCoverageRatio * 5000.0;
                    
                    // CRITICAL: Document-specific keyword bonus
                    // Count how many query words are UNIQUE to this document (discriminators)
                    // Example: "kasko" appears only in KASKO doc, not in IONIQ5 manual → very valuable!
                    var uniqueKeywordCount = 0;
                    foreach (var word in queryWords)
                    {
                        if (queryWordDocumentMap.TryGetValue(word, out var docsWithWord))
                        {
                            // Check if this query word appears ONLY in this document
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
                // This ensures that if a document has a huge bonus (e.g. 2500 for unique keyword),
                // its chunks get that same advantage and aren't crowded out by other docs' chunks
                var docScoreMap = relevantDocuments.ToDictionary(d => d.Id, d => documentScores.First(ds => ds.Document.Id == d.Id).Score);
                
                foreach (var chunk in relevantDocumentChunks)
                {
                    if (docScoreMap.TryGetValue(chunk.DocumentId, out var docScore))
                    {
                        // Add the full document score to the chunk score
                        // This makes the chunk score effectively: LocalRelevance + DocumentRelevance
                        chunk.RelevanceScore = (chunk.RelevanceScore ?? 0.0) + docScore;
                    }
                }
                
                // Use scored chunks with document-level boost
                allChunks = relevantDocumentChunks.Concat(
                    allDocuments.Except(relevantDocuments)
                        .SelectMany(d => scoredChunks.Where(c => c.DocumentId == d.Id))
                ).ToList();
                
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
                    // Sort by relevance score (includes document-level boost) first, then by numbered list count
                    var numberedListWithQueryWords = allChunksWithNumberedLists
                        .Where(x => x.HasNumberedList && x.WordMatches > 0)
                        .OrderByDescending(x => x.Chunk.RelevanceScore ?? 0.0) // Document-level boost preserved
                        .ThenByDescending(x => x.NumberedListCount)
                        .ThenByDescending(x => x.WordMatches)
                        .Select(x => x.Chunk)
                        .Take(searchMaxResults * 3)
                        .ToList();
                    
                    // Second: Chunks with numbered lists even without query words (for comprehensive coverage)
                    var numberedListOnly = allChunksWithNumberedLists
                        .Where(x => x.HasNumberedList && x.WordMatches == 0)
                        .OrderByDescending(x => x.Chunk.RelevanceScore ?? 0.0) // Document-level boost preserved
                        .ThenByDescending(x => x.NumberedListCount)
                        .Select(x => x.Chunk)
                        .Take(searchMaxResults * 2)
                        .ToList();
                    
                    // Third: Chunks with query words but no numbered lists
                    var queryWordsOnly = allChunksWithNumberedLists
                        .Where(x => !x.HasNumberedList && x.WordMatches > 0)
                        .OrderByDescending(x => x.Chunk.RelevanceScore ?? 0.0) // Document-level boost preserved
                        .ThenByDescending(x => x.WordMatches)
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
                    // CRITICAL: Prioritize chunks by RelevanceScore (includes document-level boost)
                    // This ensures we select chunks from the most relevant documents first
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
            // DISABLED: Numbered list prioritization is too aggressive and overrides document-level boost
            // Instead, rely on document-level scoring and query word matching
            var numberedListChunkIds = new HashSet<Guid>();
            if (false && RequiresComprehensiveSearch(query) && chunks.Count > 0) // Disabled numbered list prioritization
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
                
                // CRITICAL: First, check chunks that are already in the list (they have document-level boost)
                var existingChunksMap = chunks.ToDictionary(c => c.Id, c => c);
                var localQueryWords = QueryTokenizer.TokenizeQuery(query);
                
                foreach (var chunk in chunks)
                {
                    var numberedListCount = numberedListPatterns.Sum(pattern => 
                        Regex.Matches(chunk.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase).Count);
                    
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
                                Regex.IsMatch(c.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase)))
                        .ToList();
                    
                    if (documentNumberedChunks.Count > 0)
                    {
                        var relevantNumberedChunks = new List<DocumentChunk>();
                        
                        foreach (var chunk in documentNumberedChunks)
                        {
                            var numberedListCount = numberedListPatterns.Sum(pattern => 
                                Regex.Matches(chunk.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase).Count);
                            
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
                        _logger.LogInformation("Found {TotalCount} numbered list chunks in document {DocumentId}, {RelevantCount} relevant (with query words or 3+ items)", 
                            documentNumberedChunks.Count, documentId, relevantNumberedChunks.Count);
                    }
                }
                
                _logger.LogInformation("Total numbered list chunks found: {Count}", allNumberedListChunks.Count);
                
                // CRITICAL: Prioritize chunks with query words - these are most relevant
                // Sort by total score first (includes document-level boost), then by query word matches
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
                
                // CRITICAL: Preserve ALL original chunks (they have document-level boost)
                // Only add additional numbered list chunks that aren't already in the original list
                var originalChunkIds = new HashSet<Guid>(chunks.Select(c => c.Id));
                var maxAdditionalNumberedChunks = 5; // Reduced from 10 - less aggressive
                
                // Start with ALL original chunks (preserve document-level boost)
                var newChunks = new List<DocumentChunk>(chunks);
                var seenIds = new HashSet<Guid>(chunks.Select(c => c.Id));
                
                // Add only NEW numbered list chunks (not in original list) as supplements
                // These are lower priority since they don't have document-level boost
                // Only add if they have high relevance score (from original chunks with document-level boost)
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
                
                // Re-sort by relevance score to ensure best chunks are first
                // Keep more chunks to preserve document-level boost
                // Don't limit too aggressively - preserve all high-scoring chunks
                chunks = newChunks
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .ToList(); // Don't limit - let the final selection handle it
                
                var originalChunksPreserved = chunks.Count(c => originalChunkIds.Contains(c.Id));
                var newChunksAdded = chunks.Count - originalChunksPreserved;
                _logger.LogInformation("Preserved {OriginalCount} original chunks + added {NewCount} new numbered list chunks (from {TotalNumbered} found)", 
                    originalChunksPreserved, newChunksAdded, allNumberedListChunks.Count);
            }
            
            // Expand context by including adjacent chunks from the same document
            // This ensures that if a heading is in one chunk and content is in the next, both are included
            // CRITICAL: Only expand context for chunks from relevant documents (with document-level boost)
            // This prevents expanding chunks from wrong documents
            if (_contextExpansion != null && chunks.Count > 0 && numberedListChunkIds.Count == 0)
            {
                // Identify chunks from relevant documents (they have higher scores due to document-level boost)
                // Document-level boost is 200.0, so chunks with score > 200 are from relevant documents
                const double DocumentBoostThreshold = 200.0;
                var relevantDocumentChunks = chunks
                    .Where(c => (c.RelevanceScore ?? 0.0) >= DocumentBoostThreshold)
                    .ToList();
                
                var otherChunks = chunks
                    .Where(c => (c.RelevanceScore ?? 0.0) < DocumentBoostThreshold)
                    .ToList();
                
                // Only expand context for relevant document chunks
                if (relevantDocumentChunks.Count > 0)
                {
                    // Store original chunk IDs and their relevance scores before expansion
                    var originalChunkIds = new HashSet<Guid>(relevantDocumentChunks.Select(c => c.Id));
                    var originalScores = relevantDocumentChunks.ToDictionary(c => c.Id, c => c.RelevanceScore ?? 0.0);
                    
                    // For counting questions, use smaller context window to avoid too many chunks
                    var contextWindow = RequiresComprehensiveSearch(query) 
                        ? 3 // Smaller window for comprehensive queries
                        : DetermineContextWindow(relevantDocumentChunks, query);
                    
                    var expandedChunks = await _contextExpansion.ExpandContextAsync(relevantDocumentChunks, contextWindow);
                    
                    // Re-score expanded chunks that don't have scores (adjacent chunks added by expansion)
                    // Use query words to calculate basic relevance for expanded chunks
                    var queryWords = QueryTokenizer.TokenizeQuery(query);
                    foreach (var chunk in expandedChunks)
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
                    
                    // CRITICAL: Combine expanded relevant chunks with other chunks
                    // Sort by relevance score to ensure best chunks are first
                    chunks = expandedChunks
                        .OrderByDescending(c => originalChunkIds.Contains(c.Id)) // Original chunks first
                        .ThenByDescending(c => c.RelevanceScore ?? 0.0) // Then by relevance score
                        .Concat(otherChunks.OrderByDescending(c => c.RelevanceScore ?? 0.0))
                        .ToList();
                    
                    // Limit expanded chunks to prevent excessive context and timeout
                    if (chunks.Count > MaxExpandedChunks)
                    {
                        chunks = chunks.Take(MaxExpandedChunks).ToList();
                        ServiceLogMessages.LogContextExpansionLimited(_logger, MaxExpandedChunks, null);
                    }
                }
                else
                {
                    // No relevant document chunks to expand, keep original chunks
                    chunks = chunks.OrderByDescending(c => c.RelevanceScore ?? 0.0).ToList();
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
    }
}
