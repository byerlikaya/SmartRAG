#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Enums;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartRAG.Services
{
    /// <summary>
    /// Service for document search and RAG (Retrieval-Augmented Generation) operations
    /// </summary>
    public class DocumentSearchService : IDocumentSearchService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IAIService _aiService;
        private readonly IAIProviderFactory _aiProviderFactory;


        /// <summary>
        /// Initializes a new instance of the DocumentSearchService
        /// </summary>
        /// <param name="documentRepository">Repository for document operations</param>
        /// <param name="aiService">AI service for text generation</param>
        /// <param name="aiProviderFactory">Factory for AI provider creation</param>
        /// <param name="semanticSearchService">Service for semantic search operations</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="options">SmartRAG configuration options</param>
        /// <param name="logger">Logger instance for this service</param>
        /// <param name="multiDatabaseQueryCoordinator">Optional multi-database query coordinator for database queries</param>
        public DocumentSearchService(
            IDocumentRepository documentRepository,
            IAIService aiService,
            IAIProviderFactory aiProviderFactory,
            SemanticSearchService semanticSearchService,
            IConfiguration configuration,
            IOptions<SmartRagOptions> options,
            ILogger<DocumentSearchService> logger,
            IMultiDatabaseQueryCoordinator? multiDatabaseQueryCoordinator = null)
        {
            _documentRepository = documentRepository;
            _aiService = aiService;
            _aiProviderFactory = aiProviderFactory;
            _semanticSearchService = semanticSearchService;
            _configuration = configuration;
            _options = options.Value;
            _logger = logger;
            _multiDatabaseQueryCoordinator = multiDatabaseQueryCoordinator;
        }

        #region Constants

        // Scoring weights
        private const double FullNameMatchScoreBoost = 200.0;
        private const double PartialNameMatchScoreBoost = 100.0;
        private const double WordMatchScore = 2.0;
        private const double WordCountScoreBoost = 5.0;
        private const double PunctuationScoreBoost = 2.0;
        private const double NumberScoreBoost = 2.0;

        // Thresholds
        private const int WordCountMin = 10;
        private const int WordCountMax = 100;
        private const int PunctuationCountThreshold = 3;
        private const int NumberCountThreshold = 2;
        private const double RelevanceThreshold = 0.1;
        private const int ChunkPreviewLength = 100;

        // Selection multipliers and minimums
        private const int InitialSearchMultiplier = 2;
        private const int CandidateMultiplier = 3;
        private const int CandidateMinCount = 30;
        private const int FinalTakeMultiplier = 2;
        private const int FinalMinCount = 20;

        // Query processing constants
        private const int MinWordLength = 2;
        private const int DefaultScore = 0;
        private const double HybridSemanticWeight = 0.8;
        private const double HybridKeywordWeight = 0.2;
        private const int EmptyEmbeddingCount = 0;
        private const int MinQueryWordsCount = 0;
        private const double DefaultScoreValue = 0.0;
        private const double NormalizedScoreMax = 1.0;
        private const int MinVectorCount = 0;
        private const int MinChunksWithEmbeddingsCount = 0;
        private const int MinSearchResultsCount = 0;
        private const int MinNameChunksCount = 0;
        private const int MinPotentialNamesCount = 2;
        private const int MinWordCountThreshold = 0;
        // Fallback search and content
        private const int FallbackSearchMaxResults = 5;
        private const int MinSubstantialContentLength = 50;

        // Confidence thresholds for Smart Hybrid approach
        private const double HighConfidenceThreshold = 0.7;
        private const double MediumConfidenceMin = 0.3;
        private const double MediumConfidenceMax = 0.7;

        // Generic messages
        private const string ChatUnavailableMessage = "Sorry, I cannot chat right now. Please try again later.";

        #endregion

        #region Fields

        private readonly SmartRagOptions _options;
        private readonly SemanticSearchService _semanticSearchService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentSearchService> _logger;
        private readonly IMultiDatabaseQueryCoordinator? _multiDatabaseQueryCoordinator;

        // Conversation management using existing storage
        private readonly ConcurrentDictionary<string, string> _conversationCache = new ConcurrentDictionary<string, string>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Searches for relevant document chunks based on the query
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
                ServiceLogMessages.LogSearchResults(_logger, searchResults.Count, searchResults.Select(c => c.DocumentId).Distinct().Count(), null);

                // Apply diversity selection to ensure chunks from different documents
                var diverseResults = ApplyDiversityAndSelect(searchResults, maxResults);

                ServiceLogMessages.LogDiverseResults(_logger, diverseResults.Count, diverseResults.Select(c => c.DocumentId).Distinct().Count(), null);

                return diverseResults;
            }

            return searchResults;
        }

        /// <summary>
        /// Process intelligent query with RAG and automatic session management
        /// Unified approach: searches across documents, images, audio, and databases
        /// </summary>
        /// <param name="query">User query to process</param>
        /// <param name="maxResults">Maximum number of document chunks to use</param>
        /// <param name="startNewConversation">Whether to start a new conversation session</param>
        /// <returns>RAG response with answer and sources from all available data sources</returns>
        public async Task<RagResponse> QueryIntelligenceAsync(string query, int maxResults = 5, bool startNewConversation = false)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));

            var originalQuery = query;
            var forceConversation = TryExtractConversationCommand(query, out var conversationPayload);

            if (forceConversation)
            {
                query = string.IsNullOrWhiteSpace(conversationPayload)
                    ? string.Empty
                    : conversationPayload;
            }

            // Check for new conversation command or parameter
            if (startNewConversation || IsNewConversationCommand(query))
            {
                await StartNewConversationAsync();
                return new RagResponse
                {
                    Query = query,
                    Answer = "New conversation started. How can I help you?",
                    Sources = new List<SearchSource>(),
                    SearchedAt = DateTime.UtcNow,
                    Configuration = GetRagConfiguration()
                };
            }

            // Generate or retrieve session ID automatically
            var sessionId = await GetOrCreateSessionIdAsync();

            // Get conversation history from session
            var conversationHistory = await GetConversationHistoryAsync(sessionId);

            // Extract clean query without language instructions for conversation detection
            var cleanQuery = ExtractCleanQuery(query);

            // Handle forced conversation or detected general conversation upfront
            if (forceConversation || await IsGeneralConversationAsync(cleanQuery, conversationHistory))
            {
                ServiceLogMessages.LogGeneralConversationQuery(_logger, null);
                var conversationQuery = string.IsNullOrWhiteSpace(query)
                    ? originalQuery
                    : query;
                var conversationAnswer = await HandleGeneralConversationAsync(conversationQuery, conversationHistory);
                await AddToConversationAsync(sessionId, conversationQuery, conversationAnswer);

                return new RagResponse
                {
                    Query = conversationQuery,
                    Answer = conversationAnswer,
                    Sources = new List<SearchSource>(),
                    SearchedAt = DateTime.UtcNow,
                    Configuration = GetRagConfiguration()
                };
            }

            RagResponse response;

            // Pre-evaluate document availability for smarter strategy selection
            var canAnswerFromDocuments = await CanAnswerFromDocumentsAsync(query);

            // Smart Hybrid Approach: Check if database coordinator is available
            if (_multiDatabaseQueryCoordinator != null)
            {
                try
                {
                    // Analyze query intent using AI
                    var queryIntent = await _multiDatabaseQueryCoordinator.AnalyzeQueryIntentAsync(query);

                    var hasDatabaseQueries = queryIntent.DatabaseQueries != null && queryIntent.DatabaseQueries.Count > 0;
                    var confidence = queryIntent.Confidence;

                    // Determine query strategy using enum
                    var strategy = DetermineQueryStrategy(confidence, hasDatabaseQueries, canAnswerFromDocuments);

                    // Execute strategy using switch-case (Open/Closed Principle)
                    response = strategy switch
                    {
                        QueryStrategy.DatabaseOnly => await ExecuteDatabaseOnlyStrategyAsync(query, maxResults, conversationHistory, canAnswerFromDocuments),
                        QueryStrategy.DocumentOnly => await ExecuteDocumentOnlyStrategyAsync(query, maxResults, conversationHistory, canAnswerFromDocuments),
                        QueryStrategy.Hybrid => await ExecuteHybridStrategyAsync(query, maxResults, conversationHistory, hasDatabaseQueries, canAnswerFromDocuments),
                        _ => await ExecuteDocumentOnlyStrategyAsync(query, maxResults, conversationHistory, canAnswerFromDocuments) // Fallback
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during query intent analysis, falling back to document-only query");
                    response = await ExecuteDocumentFallbackAsync(query, maxResults, conversationHistory, canAnswerFromDocuments);
                }
            }
            else
            {
                // Database coordinator not available → Fallback to document-only logic
                _logger.LogInformation("Database coordinator not available. Using document-only query.");
                response = await ExecuteDocumentOnlyStrategyAsync(query, maxResults, conversationHistory, canAnswerFromDocuments);
            }

            // Add to conversation history
            await AddToConversationAsync(sessionId, query, response.Answer);

            return response;
        }

        private async Task<string> StartNewConversationAsync()
        {
            try
            {
                // Clear current session from cache
                var currentSession = _conversationCache.Keys.FirstOrDefault();
                if (!string.IsNullOrEmpty(currentSession))
                {
                    _conversationCache.TryRemove(currentSession, out _);
                }

                // Clear persistent session key
                const string PersistentSessionKey = "smartrag-current-session";
                try
                {
                    await _documentRepository.ClearConversationAsync(PersistentSessionKey);
                }
                catch (Exception ex)
                {
                    ServiceLogMessages.LogConversationStorageFailed(_logger, PersistentSessionKey, ex);
                }

                // Create new session
                var newSessionId = $"session-{Guid.NewGuid():N}";

                // Store the new session ID in persistent storage
                await _documentRepository.AddToConversationAsync(PersistentSessionKey, "", $"session-id:{newSessionId}");
                await _documentRepository.AddToConversationAsync(newSessionId, "", "");

                // Add to cache
                _conversationCache.TryAdd(newSessionId, string.Empty);

                ServiceLogMessages.LogSessionCreated(_logger, newSessionId, null);

                return newSessionId;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationStorageFailed(_logger, "new-session", ex);
                // Fallback: create session without persistence
                var fallbackSessionId = $"session-{Guid.NewGuid():N}";
                _conversationCache.TryAdd(fallbackSessionId, string.Empty);
                return fallbackSessionId;
            }
        }

        #endregion

        #region Private Helper Methods

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
            return new RagResponse
            {
                Query = query,
                Answer = chatResponse,
                Sources = new List<SearchSource>(),
                SearchedAt = DateTime.UtcNow,
                Configuration = GetRagConfiguration()
            };
        }

        /// <summary>
        /// Executes a database-only query strategy
        /// </summary>
        private async Task<RagResponse> ExecuteDatabaseOnlyStrategyAsync(string query, int maxResults, string conversationHistory, bool canAnswerFromDocuments)
        {
            _logger.LogInformation("Executing database-only query strategy");
            
            try
            {
                var databaseResponse = await _multiDatabaseQueryCoordinator!.QueryMultipleDatabasesAsync(query, maxResults);
                if (HasMeaningfulData(databaseResponse))
                {
                    return databaseResponse;
                }

                _logger.LogInformation("Database query returned no meaningful data, falling back to document search");
                return await ExecuteDocumentFallbackAsync(query, maxResults, conversationHistory, canAnswerFromDocuments);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database query failed, falling back to document query");
                return await ExecuteDocumentFallbackAsync(query, maxResults, conversationHistory, canAnswerFromDocuments);
            }
        }

        /// <summary>
        /// Executes a document-only query strategy
        /// </summary>
        private async Task<RagResponse> ExecuteDocumentOnlyStrategyAsync(string query, int maxResults, string conversationHistory, bool? canAnswerFromDocuments = null)
        {
            _logger.LogInformation("Executing document-only query strategy");
            
            var canAnswer = canAnswerFromDocuments ?? await CanAnswerFromDocumentsAsync(query);
            if (canAnswer)
            {
                return await GenerateBasicRagAnswerAsync(query, maxResults, conversationHistory);
            }
            
            return await CreateFallbackResponseAsync(query, conversationHistory);
        }

        /// <summary>
        /// Executes a hybrid query strategy (both database and document queries)
        /// </summary>
        private async Task<RagResponse> ExecuteHybridStrategyAsync(string query, int maxResults, string conversationHistory, bool hasDatabaseQueries, bool canAnswerFromDocuments)
        {
            _logger.LogInformation("Executing hybrid query strategy (database + documents)");
            
            RagResponse? databaseResponse = null;
            RagResponse? documentResponse = null;

            // Execute database query if available
            if (hasDatabaseQueries)
            {
                try
                {
                    var candidateDatabaseResponse = await _multiDatabaseQueryCoordinator!.QueryMultipleDatabasesAsync(query, maxResults);

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
            var indicators = new[]
            {
                "unable to find",
                "cannot find",
                "no data",
                "no information",
                "not available",
                "not found",
                "sorry",
                "üzgünüm",
                "maalesef",
                "bulunmamaktadır",
                "bulamadım",
                "bulamıyorum",
                "veri bulunamadı",
                "kayıt yok"
            };

            return indicators.Any(normalized.Contains);
        }

        /// <summary>
        /// Merges results from database and document queries into a unified response
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
                ? $"=== DATABASE INFORMATION ===\n{databaseResponse.Answer}"
                : "";
            var documentContext = !string.IsNullOrEmpty(documentResponse.Answer)
                ? $"=== DOCUMENT INFORMATION ===\n{documentResponse.Answer}"
                : "";

            var combinedContext = new List<string>();
            if (!string.IsNullOrEmpty(databaseContext))
                combinedContext.Add(databaseContext);
            if (!string.IsNullOrEmpty(documentContext))
                combinedContext.Add(documentContext);

            var historyContext = !string.IsNullOrEmpty(conversationHistory)
                ? $"\n\nRecent context:\n{TruncateConversationHistory(conversationHistory, maxTurns: 2)}\n"
                : "";

            // Concise prompt for merging results
            var mergePrompt = $@"Answer the user's question using the provided information.

CRITICAL RULES:
- Provide DIRECT, CONCISE answer to the question
- Use information from the sources below (database OR documents)
- Do NOT explain where information came from
- Do NOT mention missing information or unavailable data
- Do NOT add unnecessary explanations
- Do NOT include irrelevant information
- Keep response SHORT and TO THE POINT

{historyContext}Question: {query}

Available Information:
{string.Join("\n\n", combinedContext)}

Direct Answer:";

            var mergedAnswer = await _aiService.GenerateResponseAsync(mergePrompt, combinedContext);

            _logger.LogInformation("Hybrid search completed. Combined {DatabaseSources} database sources and {DocumentSources} document sources",
                databaseResponse.Sources.Count, documentResponse.Sources.Count);

            return new RagResponse
            {
                Query = query,
                Answer = mergedAnswer,
                Sources = combinedSources,
                SearchedAt = DateTime.UtcNow,
                Configuration = GetRagConfiguration()
            };
        }

        /// <summary>
        /// Executes document fallback when database query fails
        /// </summary>
        private async Task<RagResponse> ExecuteDocumentFallbackAsync(string query, int maxResults, string conversationHistory, bool? canAnswerFromDocuments = null)
        {
            var canAnswer = canAnswerFromDocuments ?? await CanAnswerFromDocumentsAsync(query);
            if (canAnswer)
            {
                return await GenerateBasicRagAnswerAsync(query, maxResults, conversationHistory);
            }

            return await CreateFallbackResponseAsync(query, conversationHistory);
        }

        /// <summary>
        /// Gets or creates a session ID automatically for conversation continuity
        /// Uses a persistent session key that survives application restarts
        /// </summary>
        private async Task<string> GetOrCreateSessionIdAsync()
        {
            const string PersistentSessionKey = "smartrag-current-session";

            // First, try to get existing session from storage
            try
            {
                var existingSessionData = await _documentRepository.GetConversationHistoryAsync(PersistentSessionKey);
                if (!string.IsNullOrEmpty(existingSessionData))
                {
                    // Extract session ID from stored data (format: "session-id:actual-session-id")
                    var lines = existingSessionData.Split('\n');
                    var sessionLine = lines.FirstOrDefault(l => l.StartsWith("session-id:"));
                    if (sessionLine != null)
                    {
                        var sessionId = sessionLine.Substring("session-id:".Length).Trim();

                        // Verify session still exists and has conversation data
                        var sessionExists = await _documentRepository.SessionExistsAsync(sessionId);
                        if (sessionExists)
                        {
                            // Get the actual conversation history from the session
                            var conversationHistory = await _documentRepository.GetConversationHistoryAsync(sessionId);

                            // Add to cache for faster access
                            _conversationCache.TryAdd(sessionId, conversationHistory ?? string.Empty);
                            ServiceLogMessages.LogSessionRetrieved(_logger, sessionId, null);
                            return sessionId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationRetrievalFailed(_logger, PersistentSessionKey, ex);
            }

            // Create new session ID
            var newSessionId = $"session-{Guid.NewGuid():N}";

            // Store the session ID in persistent storage for future retrieval
            try
            {
                await _documentRepository.AddToConversationAsync(PersistentSessionKey, "", $"session-id:{newSessionId}");
                await _documentRepository.AddToConversationAsync(newSessionId, "", "");

                // Add to cache
                _conversationCache.TryAdd(newSessionId, string.Empty);

                ServiceLogMessages.LogSessionCreated(_logger, newSessionId, null);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationStorageFailed(_logger, newSessionId, ex);
            }

            return newSessionId;
        }

        /// <summary>
        /// Check if the query is a new conversation command
        /// </summary>
        private static bool IsNewConversationCommand(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            var lowerQuery = query.ToLowerInvariant().Trim();

            if (lowerQuery == "/new" || lowerQuery == "/reset" || lowerQuery == "/clear")
                return true;

            // Commands with parameters
            if (lowerQuery.StartsWith("/new ") || lowerQuery.StartsWith("/reset ") || lowerQuery.StartsWith("/clear "))
                return true;

            return false;
        }

        private static bool TryExtractConversationCommand(string input, out string payload)
        {
            payload = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var trimmed = input.Trim();

            if (trimmed.StartsWith("/chat", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("/talk", StringComparison.OrdinalIgnoreCase))
            {
                payload = trimmed.Length > 5 ? trimmed[5..].TrimStart() : string.Empty;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the conversation storage provider (uses ConversationStorageProvider if specified, otherwise StorageProvider with fallback)
        /// </summary>
        private StorageProvider GetConversationStorageProvider()
        {
            if (_options.ConversationStorageProvider.HasValue)
            {
                // Convert ConversationStorageProvider to StorageProvider
                switch (_options.ConversationStorageProvider.Value)
                {
                    case ConversationStorageProvider.Redis:
                        return StorageProvider.Redis;
                    case ConversationStorageProvider.SQLite:
                        return StorageProvider.SQLite;
                    case ConversationStorageProvider.FileSystem:
                        return StorageProvider.FileSystem;
                    case ConversationStorageProvider.InMemory:
                        return StorageProvider.InMemory;
                    default:
                        return StorageProvider.InMemory; // Fallback
                }
            }

            // If not specified, use StorageProvider but exclude Qdrant
            switch (_options.StorageProvider)
            {
                case StorageProvider.Qdrant:
                    return StorageProvider.InMemory; // Fallback for Qdrant
                default:
                    return _options.StorageProvider;
            }
        }

        /// <summary>
        /// Sanitizes user input for safe logging by removing newlines and carriage returns.
        /// </summary>
        private static string SanitizeForLog(string input)
        {
            if (input == null) return string.Empty;
            return input.Replace("\r", "").Replace("\n", "");
        }

        /// <summary>
        /// Enhanced search with intelligent filtering and name detection
        /// </summary>
        private async Task<List<DocumentChunk>> PerformBasicSearchAsync(string query, int maxResults)
        {
            var allDocuments = await _documentRepository.GetAllAsync();
            var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();

            ServiceLogMessages.LogSearchInDocuments(_logger, allDocuments.Count, allChunks.Count, null);

            // Try embedding-based search first if available
            try
            {
                var embeddingResults = await TryEmbeddingBasedSearchAsync(query, allChunks, maxResults);
                if (embeddingResults.Count > 0)
                {
                    ServiceLogMessages.LogEmbeddingSearchSuccessful(_logger, embeddingResults.Count, null);
                    return embeddingResults;
                }
            }
            catch (Exception)
            {
                ServiceLogMessages.LogEmbeddingSearchFailed(_logger, null);
            }

            // Enhanced keyword-based fallback for global content
            var queryWords = query.ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > MinWordLength)
                .ToList();

            // Extract potential names from ORIGINAL query (not lowercase) - language agnostic
            var potentialNames = query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > MinWordLength && char.IsUpper(w[0]))
                .ToList();

            ServiceLogMessages.LogQueryWords(_logger, string.Join(", ", queryWords.Select(SanitizeForLog)), null);
            ServiceLogMessages.LogPotentialNames(_logger, string.Join(", ", potentialNames.Select(SanitizeForLog)), null);

            var scoredChunks = allChunks.Select(chunk =>
            {
                var score = DefaultScoreValue;
                var content = chunk.Content.ToLowerInvariant();

                // Special handling for names like "John Smith" - HIGHEST PRIORITY (language agnostic)
                if (potentialNames.Count >= MinPotentialNamesCount)
                {
                    var fullName = string.Join(" ", potentialNames);
                    if (ContainsNormalizedName(content, fullName))
                    {
                        score += FullNameMatchScoreBoost;
                        ServiceLogMessages.LogFullNameMatch(_logger, SanitizeForLog(fullName), chunk.Content.Substring(0, Math.Min(ChunkPreviewLength, chunk.Content.Length)), null);
                    }
                    else if (potentialNames.Any(name => ContainsNormalizedName(content, name)))
                    {
                        score += PartialNameMatchScoreBoost;
                        var foundNames = potentialNames.Where(name => ContainsNormalizedName(content, name)).ToList();
                        ServiceLogMessages.LogPartialNameMatches(_logger, string.Join(", ", foundNames.Select(SanitizeForLog)), chunk.Content.Substring(0, Math.Min(ChunkPreviewLength, chunk.Content.Length)), null);
                    }
                }

                // Exact word matches
                foreach (var word in queryWords)
                {
                    if (content.ToLowerInvariant().Contains(word.ToLowerInvariant()))
                        score += WordMatchScore;
                }

                // Generic content quality scoring (language and content agnostic)
                var wordCount = content.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (wordCount >= WordCountMin && wordCount <= WordCountMax) score += WordCountScoreBoost;

                // Bonus for chunks with punctuation (indicates structured content)
                var punctuationCount = content.Count(c => ".,;:!?()[]{}".Contains(c));
                if (punctuationCount >= PunctuationCountThreshold) score += PunctuationScoreBoost;

                // Bonus for chunks with numbers (often indicates factual information)
                var numberCount = content.Count(c => char.IsDigit(c));
                if (numberCount >= NumberCountThreshold) score += NumberScoreBoost;

                chunk.RelevanceScore = score;
                return chunk;
            }).ToList();

            var relevantChunks = scoredChunks
                .Where(c => c.RelevanceScore > MinWordCountThreshold)
                .OrderByDescending(c => c.RelevanceScore)
                .Take(Math.Max(maxResults * CandidateMultiplier, CandidateMinCount))
                .ToList();

            ServiceLogMessages.LogRelevantChunksFound(_logger, relevantChunks.Count, null);

            // If we found chunks with names, prioritize them
            if (potentialNames.Count >= MinPotentialNamesCount)
            {
                var nameChunks = relevantChunks.Where(c =>
                    potentialNames.Any(name => c.Content.ToLowerInvariant().Contains(name.ToLowerInvariant()))).ToList();

                if (nameChunks.Count > MinNameChunksCount)
                {
                    ServiceLogMessages.LogNameChunksFound(_logger, nameChunks.Count, null);
                    return nameChunks.Take(maxResults).ToList();
                }
            }

            return relevantChunks.Take(maxResults).ToList();
        }

        private async Task<RagResponse> GenerateBasicRagAnswerAsync(string query, int maxResults, string? conversationHistory = null)
        {
            var chunks = await SearchDocumentsAsync(query, maxResults);
            var context = string.Join("\n\n", chunks.Select(c => c.Content));

            // Build prompt with LIMITED conversation history (only recent context)
            var historyContext = !string.IsNullOrEmpty(conversationHistory)
                ? $"\n\nRecent conversation context:\n{TruncateConversationHistory(conversationHistory, maxTurns: 2)}\n"
                : "";

            // Enhanced prompt for better AI understanding with LIMITED conversation history
            var enhancedPrompt = $@"You are a helpful document analysis assistant. Answer questions based ONLY on the provided document context.

CRITICAL RULES: 
- Base your answer ONLY on the document context provided below
- Do NOT use information from previous conversations unless it's in the current document context
- If you find the information in documents, provide a clear and concise answer
- If you cannot find it in the documents, simply say 'I cannot find this information in the provided documents'
- Be precise and use exact information from documents
- Keep responses focused on the current question

{historyContext}Current question: {query}

Document context: {context}

Answer:";

            var answer = await _aiService.GenerateResponseAsync(enhancedPrompt, new List<string> { context });

            return new RagResponse
            {
                Query = query,
                Answer = answer,
                Sources = await BuildDocumentSourcesAsync(chunks),
                SearchedAt = DateTime.UtcNow,
                Configuration = GetRagConfiguration()
            };
        }

        private async Task<List<SearchSource>> BuildDocumentSourcesAsync(List<DocumentChunk> chunks)
        {
            var sources = new List<SearchSource>();
            if (chunks.Count == 0)
            {
                return sources;
            }

            var documentCache = new Dictionary<Guid, SmartRAG.Entities.Document?>();

            foreach (var chunk in chunks)
            {
                var document = await GetDocumentForChunkAsync(chunk.DocumentId, documentCache);
                var sourceType = DetermineDocumentSourceType(document);
                var (startTime, endTime) = CalculateAudioTimestampRange(document, chunk);
                var location = BuildDocumentLocationDescription(chunk, document, startTime, endTime);

                sources.Add(new SearchSource
                {
                    SourceType = sourceType,
                    DocumentId = chunk.DocumentId,
                    FileName = document?.FileName ?? "Document",
                    RelevantContent = chunk.Content,
                    RelevanceScore = chunk.RelevanceScore ?? 0.0,
                    ChunkIndex = chunk.ChunkIndex,
                    StartPosition = chunk.StartPosition,
                    EndPosition = chunk.EndPosition,
                    StartTimeSeconds = startTime,
                    EndTimeSeconds = endTime,
                    Location = location
                });
            }

            return sources;
        }

        private async Task<SmartRAG.Entities.Document?> GetDocumentForChunkAsync(Guid documentId, Dictionary<Guid, SmartRAG.Entities.Document?> cache)
        {
            if (cache.TryGetValue(documentId, out var cachedDocument))
            {
                return cachedDocument;
            }

            var document = await _documentRepository.GetByIdAsync(documentId);
            cache[documentId] = document;
            return document;
        }

        private static string DetermineDocumentSourceType(SmartRAG.Entities.Document? document)
        {
            if (document == null)
            {
                return "Document";
            }

            if (!string.IsNullOrWhiteSpace(document.ContentType) &&
                document.ContentType.StartsWith("audio", StringComparison.OrdinalIgnoreCase))
            {
                return "Audio";
            }

            var extension = Path.GetExtension(document.FileName)?.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(extension))
            {
                switch (extension)
                {
                    case ".wav":
                    case ".mp3":
                    case ".m4a":
                    case ".flac":
                    case ".ogg":
                        return "Audio";
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".gif":
                    case ".bmp":
                    case ".tiff":
                    case ".webp":
                        return "Image";
                }
            }

            return "Document";
        }

        private static (double? Start, double? End) CalculateAudioTimestampRange(SmartRAG.Entities.Document? document, DocumentChunk chunk)
        {
            var segments = ExtractAudioSegments(document);
            if (segments.Count == 0)
            {
                return (null, null);
            }

            var chunkStart = chunk.StartPosition;
            var chunkEnd = chunk.EndPosition;
            var chunkNormalized = NormalizeForMatching(chunk.Content);

            var overlappingSegments = new List<AudioSegmentMetadata>();

            foreach (var segment in segments)
            {
                var hasCharacterMapping = segment.EndCharIndex > 0;
                if (hasCharacterMapping &&
                    segment.StartCharIndex < chunkEnd &&
                    segment.EndCharIndex > chunkStart)
                {
                    overlappingSegments.Add(segment);
                    continue;
                }

                var normalizedSegment = !string.IsNullOrWhiteSpace(segment.NormalizedText)
                    ? segment.NormalizedText
                    : NormalizeForMatching(segment.Text);

                if (string.IsNullOrWhiteSpace(normalizedSegment) || string.IsNullOrWhiteSpace(chunkNormalized))
                {
                    continue;
                }

                if (chunkNormalized.IndexOf(normalizedSegment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    overlappingSegments.Add(segment);
                }
            }

            if (overlappingSegments.Count == 0)
            {
                return (null, null);
            }

            var start = overlappingSegments.First().Start;
            var end = overlappingSegments.Last().End;

            return (start, end);
        }

        private static List<AudioSegmentMetadata> ExtractAudioSegments(SmartRAG.Entities.Document? document)
        {
            if (document?.Metadata == null)
            {
                return new List<AudioSegmentMetadata>();
            }

            if (!document.Metadata.TryGetValue("Segments", out var segmentsObj))
            {
                return new List<AudioSegmentMetadata>();
            }

            if (segmentsObj is List<AudioSegmentMetadata> typedList)
            {
                return typedList;
            }

            if (segmentsObj is AudioSegmentMetadata[] typedArray)
            {
                return new List<AudioSegmentMetadata>(typedArray);
            }

            if (segmentsObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                try
                {
                    var json = jsonElement.GetRawText();
                    var deserialized = JsonSerializer.Deserialize<List<AudioSegmentMetadata>>(json);
                    return deserialized ?? new List<AudioSegmentMetadata>();
                }
                catch
                {
                    return new List<AudioSegmentMetadata>();
                }
            }

            return new List<AudioSegmentMetadata>();
        }

        private static string NormalizeForMatching(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = Regex.Replace(value, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);
            normalized = Regex.Replace(normalized, @"\s+", " ");
            return normalized.Trim();
        }

        private static string BuildDocumentLocationDescription(DocumentChunk chunk, SmartRAG.Entities.Document? document, double? startTimeSeconds, double? endTimeSeconds)
        {
            var builder = new StringBuilder();
            builder.Append($"Chunk #{chunk.ChunkIndex + 1}");
            builder.Append($" | Characters {chunk.StartPosition}-{chunk.EndPosition}");

            if (startTimeSeconds.HasValue || endTimeSeconds.HasValue)
            {
                builder.Append(" | Audio ");
                builder.Append(FormatTimeRange(startTimeSeconds, endTimeSeconds));
            }

            if (document != null && !string.IsNullOrWhiteSpace(document.FileName))
            {
                builder.Append($" | Source: {document.FileName}");
            }

            return builder.ToString();
        }

        private static string FormatTimeRange(double? startSeconds, double? endSeconds)
        {
            if (!startSeconds.HasValue && !endSeconds.HasValue)
            {
                return "timestamp unavailable";
            }

            if (startSeconds.HasValue && endSeconds.HasValue)
            {
                return $"{FormatSeconds(startSeconds.Value)} - {FormatSeconds(endSeconds.Value)}";
            }

            if (startSeconds.HasValue)
            {
                return $"{FormatSeconds(startSeconds.Value)} →";
            }

            return $"← {FormatSeconds(endSeconds!.Value)}";
        }

        private static string FormatSeconds(double seconds)
        {
            if (seconds < 0)
            {
                seconds = 0;
            }

            var timeSpan = TimeSpan.FromSeconds(seconds);

            if (timeSpan.TotalHours >= 1)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}", (int)timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
        }

        private static List<DocumentChunk> ApplyDiversityAndSelect(List<DocumentChunk> chunks, int maxResults)
        {
            return chunks.Take(maxResults).ToList();
        }

        private RagConfiguration GetRagConfiguration()
        {
            return new RagConfiguration
            {
                AIProvider = _options.AIProvider.ToString(),
                StorageProvider = _options.StorageProvider.ToString(),
                Model = _configuration["AI:OpenAI:Model"] ?? "gpt-3.5-turbo"
            };
        }

        /// <summary>
        /// Try embedding-based search using configured AI provider
        /// </summary>
        private async Task<List<DocumentChunk>> TryEmbeddingBasedSearchAsync(string query, List<DocumentChunk> allChunks, int maxResults)
        {
            try
            {
                var aiProvider = _aiProviderFactory.CreateProvider(_options.AIProvider);
                var providerKey = _options.AIProvider.ToString();
                var providerConfig = _configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

                if (providerConfig == null || string.IsNullOrEmpty(providerConfig.ApiKey))
                {
                    return new List<DocumentChunk>();
                }

                // Generate embedding for query
                var queryEmbedding = await aiProvider.GenerateEmbeddingAsync(query, providerConfig);
                if (queryEmbedding == null || queryEmbedding.Count == EmptyEmbeddingCount)
                {
                    return new List<DocumentChunk>();
                }

                // Calculate similarity for all chunks that have embeddings
                var chunksWithEmbeddings = allChunks.Where(c => c.Embedding != null && c.Embedding.Count > EmptyEmbeddingCount).ToList();

                if (chunksWithEmbeddings.Count == MinChunksWithEmbeddingsCount)
                {
                    return new List<DocumentChunk>();
                }

                // Enhanced semantic search with hybrid scoring
                var scoredChunks = await Task.WhenAll(chunksWithEmbeddings.Select(async chunk =>
                {
                    var semanticSimilarity = CalculateCosineSimilarity(queryEmbedding, chunk.Embedding);
                    var enhancedSemanticScore = await _semanticSearchService.CalculateEnhancedSemanticSimilarityAsync(query, chunk.Content);

                    // Hybrid scoring: Combine enhanced semantic similarity with keyword matching
                    var keywordScore = CalculateKeywordRelevanceScore(query, chunk.Content);
                    var hybridScore = (enhancedSemanticScore * HybridSemanticWeight) + (keywordScore * HybridKeywordWeight);

                    chunk.RelevanceScore = hybridScore;
                    return chunk;
                }));

                // Get top chunks based on hybrid scoring
                var relevantChunks = scoredChunks.ToList()
                    .Where(c => c.RelevanceScore > RelevanceThreshold)
                    .OrderByDescending(c => c.RelevanceScore)
                    .Take(Math.Max(maxResults * CandidateMultiplier, CandidateMinCount))
                    .ToList();

                return relevantChunks
                    .OrderByDescending(c => c.RelevanceScore)
                    .Take(Math.Max(maxResults * FinalTakeMultiplier, FinalMinCount))
                    .ToList();
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogEmbeddingSearchFailedError(_logger, ex);
                return new List<DocumentChunk>();
            }
        }

        /// <summary>
        /// Calculate keyword relevance score for better hybrid search
        /// </summary>
        private static double CalculateKeywordRelevanceScore(string query, string content)
        {
            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(content))
                return DefaultScoreValue;

            var queryWords = query.ToLowerInvariant()
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > MinWordLength)
                .ToList();

            if (queryWords.Count == MinQueryWordsCount)
                return DefaultScoreValue;

            var contentLower = content.ToLowerInvariant();
            var score = DefaultScoreValue;

            foreach (var word in queryWords)
            {
                // Exact word match (highest score)
                if (contentLower.Contains($" {word} ") || contentLower.StartsWith($"{word} ", StringComparison.OrdinalIgnoreCase) || contentLower.EndsWith($" {word}", StringComparison.OrdinalIgnoreCase))
                {
                    score += WordMatchScore;
                }
                // Partial word match (medium score)
                else if (contentLower.Contains(word))
                {
                    score += WordMatchScore / 2;
                }
            }

            // Normalize score
            return Math.Min(score / queryWords.Count, NormalizedScoreMax);
        }

        /// <summary>
        /// Calculate cosine similarity between two vectors
        /// </summary>
        private static double CalculateCosineSimilarity(List<float> a, List<float> b)
        {
            if (a == null || b == null || a.Count == MinVectorCount || b.Count == MinVectorCount) return DefaultScoreValue;

            var n = Math.Min(a.Count, b.Count);
            double dot = DefaultScore, na = DefaultScore, nb = DefaultScore;

            for (int i = 0; i < n; i++)
            {
                double va = a[i];
                double vb = b[i];
                dot += va * vb;
                na += va * va;
                nb += vb * vb;
            }

            if (na == DefaultScore || nb == DefaultScore) return DefaultScoreValue;
            return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
        }

        /// <summary>
        /// Normalize text for better search matching (handles Unicode encoding issues)
        /// </summary>
        private static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Decode Unicode escape sequences
            var decoded = System.Text.RegularExpressions.Regex.Unescape(text);

            // Normalize Unicode characters
            var normalized = decoded.Normalize(System.Text.NormalizationForm.FormC);

            // Handle common character variations for multiple languages (Turkish, German, etc.)
            var characterMappings = new Dictionary<string, string>
        {
            {"ı", "i"}, {"İ", "I"}, {"ğ", "g"}, {"Ğ", "G"},
            {"ü", "u"}, {"Ü", "U"}, {"ş", "s"}, {"Ş", "S"},
            {"ö", "o"}, {"Ö", "O"}, {"ç", "c"}, {"Ç", "C"}
        };

            foreach (var mapping in characterMappings)
            {
                normalized = normalized.Replace(mapping.Key, mapping.Value);
            }

            return normalized;
        }

        /// <summary>
        /// Check if content contains normalized name (handles encoding issues)
        /// </summary>
        private static bool ContainsNormalizedName(string content, string searchName)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(searchName))
                return false;

            var normalizedContent = NormalizeText(content);
            var normalizedSearchName = NormalizeText(searchName);

            // Try exact match first
            if (normalizedContent.ToLowerInvariant().Contains(normalizedSearchName.ToLowerInvariant()))
                return true;

            // Try partial matches for each word
            var searchWords = normalizedSearchName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var contentWords = normalizedContent.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Check if all search words are present in content
            return searchWords.All(searchWord =>
                contentWords.Any(contentWord =>
                    contentWord.ToLowerInvariant().Contains(searchWord.ToLowerInvariant())));
        }

        /// <summary>
        /// Ultimate language-agnostic approach: ONLY check if documents contain relevant information
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
        /// Determines whether the query should be treated as general conversation using AI intent detection.
        /// </summary>
        private async Task<bool> IsGeneralConversationAsync(string query, string? conversationHistory)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            var trimmedQuery = string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();

            // Fast pre-check: Obvious information queries (skip AI call)
            if (IsLikelyInformationQuery(trimmedQuery))
            {
                return false;
            }

            // Fast pre-check: Short simple queries (likely conversation)
            if (IsObviousConversation(trimmedQuery))
            {
                _logger.LogDebug("Query classified as CONVERSATION by fast pre-check: {Query}", trimmedQuery);
                return true;
            }

            // AI classification for ambiguous cases
            _logger.LogDebug("Query passed pre-checks, sending to AI for classification: {Query}", trimmedQuery);
            try
            {
                var historySnippet = string.Empty;
                if (!string.IsNullOrWhiteSpace(conversationHistory))
                {
                    const int maxHistoryLength = 400;
                    var normalizedHistory = conversationHistory.Trim();
                    historySnippet = normalizedHistory.Length > maxHistoryLength
                        ? normalizedHistory.Substring(normalizedHistory.Length - maxHistoryLength, maxHistoryLength)
                        : normalizedHistory;
                }

                var classificationPrompt = string.Format(
                    CultureInfo.InvariantCulture,
@"You MUST classify the input as CONVERSATION or INFORMATION.

🚨 CRITICAL: Classify as CONVERSATION if:
- Greeting (any language): Hello, Hi, Hey, Hola, Bonjour, Merhaba, Selam
- About the AI: Who are you, What are you, What model, What can you do
- Small talk: How are you, What's your name, Where are you from, Are you ok
- Polite chat: Thank you, Thanks, Goodbye, See you, Nice to meet you

✓ Classify as INFORMATION ONLY if:
- Contains data request words: show, list, find, calculate, total, count, sum
- Contains question words: what IS/WAS, which one, how many, when did
- Contains numbers/dates: 2023, last year, top 10, over 1000
- Specific entity queries: record 123, item X, reference number Y

User: ""{0}""
{1}

CRITICAL: If unsure, default to CONVERSATION. Answer with ONE word only: CONVERSATION or INFORMATION", 
                    trimmedQuery, 
                    string.IsNullOrWhiteSpace(historySnippet) ? "" : $"Context: \"{historySnippet}\"");

                var classification = await _aiService.GenerateResponseAsync(classificationPrompt, Array.Empty<string>());
                _logger.LogDebug("AI classification result: {Classification}", classification);

                if (!string.IsNullOrWhiteSpace(classification))
                {
                    var normalizedResult = classification.Trim().ToUpperInvariant();

                    if (normalizedResult.Contains("CONVERSATION", StringComparison.Ordinal))
                    {
                        _logger.LogDebug("Query classified as CONVERSATION by AI: {Query}", trimmedQuery);
                        return true;
                    }

                    if (normalizedResult.Contains("INFORMATION", StringComparison.Ordinal))
                    {
                        _logger.LogDebug("Query classified as INFORMATION by AI: {Query}", trimmedQuery);
                        return false;
                    }

                    _logger.LogWarning("AI returned unclear classification: {Classification}", classification);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "AI classification failed. Defaulting to conversation.");
                return true; // Safe default: treat as conversation
            }

            // Final fallback: if AI gave unclear response, default to conversation
            return true;
        }

        /// <summary>
        /// Extracts clean query by removing language instruction suffixes
        /// </summary>
        private static string ExtractCleanQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return query;
            }

            // Remove common language instruction patterns
            var patterns = new[]
            {
                "[IMPORTANT: Respond in ",
                "\n\n[IMPORTANT:",
                "[CRITICAL: Answer in ",
                "\n\n[CRITICAL:"
            };

            var cleanQuery = query;
            foreach (var pattern in patterns)
            {
                var index = cleanQuery.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    cleanQuery = cleanQuery.Substring(0, index).Trim();
                    break;
                }
            }

            return cleanQuery;
        }

        /// <summary>
        /// Fast heuristic check for obvious conversation patterns (no AI call needed)
        /// </summary>
        private static bool IsObviousConversation(string query)
        {
            var trimmed = query.Trim();

            // Very short input (1-2 chars) is likely conversation
            if (trimmed.Length <= 2)
            {
                return true;
            }

            var tokens = trimmed.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Short queries (1-2 words) without question mark or numbers are likely chat
            if (tokens.Length <= 2 &&
                !trimmed.Contains('?', StringComparison.Ordinal) &&
                !trimmed.Any(char.IsDigit))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Fast heuristic check for obvious information queries (no AI call needed)
        /// </summary>
        private static bool IsLikelyInformationQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            var trimmed = query.Trim();

            // Contains question mark → likely information query
            if (trimmed.Contains('?', StringComparison.Ordinal))
            {
                return true;
            }

            // Contains numbers → likely data query (e.g., "record 123", "top 10")
            if (trimmed.Any(char.IsDigit))
            {
                return true;
            }

            // Long queries (5+ words) are usually information requests
            var tokens = trimmed.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 5)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handle general conversation queries with conversation history
        /// </summary>
        private async Task<string> HandleGeneralConversationAsync(string query, string? conversationHistory = null)
        {
            try
            {
                // Use the configured AI provider from options
                var aiProvider = _aiProviderFactory.CreateProvider(_options.AIProvider);
                var providerKey = _options.AIProvider.ToString();
                var providerConfig = _configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

                if (providerConfig == null || string.IsNullOrEmpty(providerConfig.ApiKey))
                {
                    return ChatUnavailableMessage;
                }

                // Build prompt with LIMITED conversation history (only recent messages)
                var historyContext = !string.IsNullOrEmpty(conversationHistory)
                    ? $"\n\nRecent conversation context:\n{TruncateConversationHistory(conversationHistory, maxTurns: 3)}\n"
                    : "";

                var prompt = $@"You are a helpful AI assistant. Answer the user's question naturally and friendly.
Keep responses concise and relevant to the current question.

{historyContext}Current question: {query}

Answer:";

                return await aiProvider.GenerateTextAsync(prompt, providerConfig);
            }
            catch (Exception)
            {
                // Log error using structured logging
                return ChatUnavailableMessage;
            }
        }
        
        /// <summary>
        /// Truncates conversation history to keep only the most recent turns
        /// </summary>
        private static string TruncateConversationHistory(string history, int maxTurns = 3)
        {
            if (string.IsNullOrWhiteSpace(history))
            {
                return string.Empty;
            }

            // Split by conversation turns (User: ... Assistant: ...)
            var lines = history.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var turns = new List<string>();
            var currentTurn = new StringBuilder();
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Detect start of a new turn
                if (trimmed.StartsWith("User:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Assistant:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("A:", StringComparison.OrdinalIgnoreCase))
                {
                    // Save previous turn if exists
                    if (currentTurn.Length > 0)
                    {
                        turns.Add(currentTurn.ToString());
                        currentTurn.Clear();
                    }
                }
                
                currentTurn.AppendLine(trimmed);
            }
            
            // Add last turn
            if (currentTurn.Length > 0)
            {
                turns.Add(currentTurn.ToString());
            }
            
            // Keep only last N turns
            var recentTurns = turns.TakeLast(maxTurns * 2).ToList(); // *2 because each turn is User + Assistant
            
            if (recentTurns.Count == 0)
            {
                return string.Empty;
            }
            
            return string.Join("\n", recentTurns);
        }

        #endregion

        #region Conversation Management

        /// <summary>
        /// Get conversation history for a session using existing storage provider
        /// </summary>
        private async Task<string> GetConversationHistoryAsync(string sessionId)
        {
            try
            {
                // Always get fresh data from storage to ensure conversation continuity
                var history = await GetConversationFromStorageAsync(sessionId);

                // Update cache with fresh data
                _conversationCache.AddOrUpdate(sessionId, history, (key, oldValue) => history);

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history for session {SessionId}", sessionId);
                return string.Empty;
            }
        }

        /// <summary>
        /// Add conversation to storage using existing storage provider
        /// </summary>
        private async Task AddToConversationAsync(string sessionId, string question, string answer)
        {
            try
            {
                // Get current history from storage (not cache)
                var currentHistory = await GetConversationFromStorageAsync(sessionId);

                // Build new conversation entry
                var newEntry = string.IsNullOrEmpty(currentHistory)
                    ? $"User: {question}\nAssistant: {answer}"
                    : $"{currentHistory}\nUser: {question}\nAssistant: {answer}";

                // No automatic truncation - keep full conversation history
                // Conversation will only be cleared when user starts a new session

                // Store in persistent storage first
                await StoreConversationToStorageAsync(sessionId, newEntry);

                // Then update cache
                _conversationCache.AddOrUpdate(sessionId, newEntry, (key, oldValue) => newEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to conversation for session {SessionId}", sessionId);
            }
        }

        /// <summary>
        /// Get conversation from storage based on conversation storage provider
        /// </summary>
        private async Task<string> GetConversationFromStorageAsync(string sessionId)
        {
            try
            {
                var conversationStorageProvider = GetConversationStorageProvider();

                switch (conversationStorageProvider)
                {
                    case StorageProvider.Redis:
                    case StorageProvider.SQLite:
                    case StorageProvider.InMemory:
                    case StorageProvider.FileSystem:
                        // Use the existing document repository for conversation storage
                        return await _documentRepository.GetConversationHistoryAsync(sessionId);

                    case StorageProvider.Qdrant:
                        // Qdrant doesn't support conversation history yet
                        return string.Empty;

                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationRetrievalFailed(_logger, sessionId, ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Store conversation to storage based on conversation storage provider
        /// </summary>
        private async Task StoreConversationToStorageAsync(string sessionId, string conversation)
        {
            try
            {
                var conversationStorageProvider = GetConversationStorageProvider();

                switch (conversationStorageProvider)
                {
                    case StorageProvider.Redis:
                    case StorageProvider.SQLite:
                    case StorageProvider.InMemory:
                    case StorageProvider.FileSystem:
                        // Use the existing document repository for conversation storage
                        await _documentRepository.AddToConversationAsync(sessionId, "", conversation);
                        break;

                    case StorageProvider.Qdrant:
                        // Qdrant doesn't support conversation history yet
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogConversationStorageFailed(_logger, sessionId, ex);
            }
        }

        // TruncateConversation method removed - no automatic conversation truncation
        // Conversations are only cleared when user starts a new session

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
