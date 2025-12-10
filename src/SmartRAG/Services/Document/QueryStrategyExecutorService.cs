#nullable enable

using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Support;
using SmartRAG.Models;
using SmartRAG.Models.RequestResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for executing query strategies
    /// </summary>
    public class QueryStrategyExecutorService : IQueryStrategyExecutorService
    {
        private readonly IMultiDatabaseQueryCoordinator? _multiDatabaseQueryCoordinator;
        private readonly ILogger<QueryStrategyExecutorService> _logger;
        private readonly Lazy<IRagAnswerGeneratorService> _ragAnswerGenerator;
        private readonly IResponseBuilderService? _responseBuilder;
        private readonly IConversationManagerService _conversationManager;

        /// <summary>
        /// Initializes a new instance of the QueryStrategyExecutorService
        /// </summary>
        /// <param name="multiDatabaseQueryCoordinator">Optional multi-database query coordinator</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="ragAnswerGenerator">Service for generating RAG answers (lazy to break circular dependency)</param>
        /// <param name="responseBuilder">Service for building RAG responses</param>
        /// <param name="conversationManager">Service for managing conversations</param>
        public QueryStrategyExecutorService(
            IMultiDatabaseQueryCoordinator? multiDatabaseQueryCoordinator,
            ILogger<QueryStrategyExecutorService> logger,
            Lazy<IRagAnswerGeneratorService> ragAnswerGenerator,
            IResponseBuilderService? responseBuilder = null,
            IConversationManagerService? conversationManager = null)
        {
            _multiDatabaseQueryCoordinator = multiDatabaseQueryCoordinator;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ragAnswerGenerator = ragAnswerGenerator ?? throw new ArgumentNullException(nameof(ragAnswerGenerator));
            _responseBuilder = responseBuilder;
            _conversationManager = conversationManager ?? throw new ArgumentNullException(nameof(conversationManager));
        }

        /// <summary>
        /// Executes a database-only query strategy with fallback to document query
        /// </summary>
        /// <param name="request">Request containing query parameters</param>
        /// <returns>RAG response with answer and sources</returns>
        public async Task<RagResponse> ExecuteDatabaseOnlyStrategyAsync(Models.RequestResponse.DatabaseQueryStrategyRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                if (request.QueryIntent == null)
                {
                    var docRequest = new DocumentQueryStrategyRequest
                    {
                        Query = request.Query,
                        MaxResults = request.MaxResults,
                        ConversationHistory = request.ConversationHistory,
                        CanAnswerFromDocuments = request.CanAnswerFromDocuments,
                        PreferredLanguage = request.PreferredLanguage,
                        Options = request.Options,
                        QueryTokens = request.QueryTokens
                    };
                    return await ExecuteDocumentOnlyStrategyAsync(docRequest);
                }

                var databaseResponse = await _multiDatabaseQueryCoordinator!.QueryMultipleDatabasesAsync(request.Query, request.QueryIntent, request.MaxResults, request.PreferredLanguage);

                if (_responseBuilder?.HasMeaningfulData(databaseResponse) ?? HasMeaningfulDataFallback(databaseResponse))
                {
                    return databaseResponse;
                }

                _logger.LogInformation("Database query returned no meaningful data, falling back to document search");
                var fallbackRequest = new DocumentQueryStrategyRequest
                {
                    Query = request.Query,
                    MaxResults = request.MaxResults,
                    ConversationHistory = request.ConversationHistory,
                    CanAnswerFromDocuments = request.CanAnswerFromDocuments,
                    PreferredLanguage = request.PreferredLanguage,
                    Options = request.Options,
                    QueryTokens = request.QueryTokens
                };
                return await ExecuteDocumentOnlyStrategyAsync(fallbackRequest);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database query failed, falling back to document query");
                var fallbackRequest = new DocumentQueryStrategyRequest
                {
                    Query = request.Query,
                    MaxResults = request.MaxResults,
                    ConversationHistory = request.ConversationHistory,
                    CanAnswerFromDocuments = request.CanAnswerFromDocuments,
                    PreferredLanguage = request.PreferredLanguage,
                    Options = request.Options,
                    QueryTokens = request.QueryTokens
                };
                return await ExecuteDocumentOnlyStrategyAsync(fallbackRequest);
            }
        }

        /// <summary>
        /// Executes a database-only query strategy with fallback to document query
        /// </summary>
        /// <param name="query">User query to process</param>
        /// <param name="maxResults">Maximum number of results</param>
        /// <param name="conversationHistory">Conversation history</param>
        /// <param name="canAnswerFromDocuments">Flag indicating if documents can answer</param>
        /// <param name="queryIntent">Query intent analysis result</param>
        /// <param name="preferredLanguage">Optional preferred language code for AI response</param>
        /// <param name="options">Optional search options</param>
        /// <param name="queryTokens">Pre-computed query tokens for performance</param>
        /// <returns>RAG response with answer and sources</returns>
        [Obsolete("Use ExecuteDatabaseOnlyStrategyAsync(DatabaseQueryStrategyRequest) instead. This method will be removed in v4.0.0")]
        public async Task<RagResponse> ExecuteDatabaseOnlyStrategyAsync(string query, int maxResults, string conversationHistory, bool canAnswerFromDocuments, QueryIntent? queryIntent, string? preferredLanguage = null, SearchOptions? options = null, List<string>? queryTokens = null)
        {
            var request = new Models.RequestResponse.DatabaseQueryStrategyRequest
            {
                Query = query,
                MaxResults = maxResults,
                ConversationHistory = conversationHistory,
                CanAnswerFromDocuments = canAnswerFromDocuments,
                QueryIntent = queryIntent,
                PreferredLanguage = preferredLanguage,
                Options = options,
                QueryTokens = queryTokens
            };
            return await ExecuteDatabaseOnlyStrategyAsync(request);
        }

        /// <summary>
        /// Executes a hybrid query strategy combining both database and document queries
        /// </summary>
        /// <param name="request">Request containing query parameters</param>
        /// <returns>Merged RAG response with answer and sources from both database and documents</returns>
        public async Task<RagResponse> ExecuteHybridStrategyAsync(Models.RequestResponse.HybridQueryStrategyRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            RagResponse? databaseResponse = null;
            RagResponse? documentResponse = null;

            if (request.HasDatabaseQueries)
            {
                try
                {
                    if (request.QueryIntent != null)
                    {
                        var candidateDatabaseResponse = await _multiDatabaseQueryCoordinator!.QueryMultipleDatabasesAsync(request.Query, request.QueryIntent, request.MaxResults, request.PreferredLanguage);
                        if (_responseBuilder?.HasMeaningfulData(candidateDatabaseResponse) ?? HasMeaningfulDataFallback(candidateDatabaseResponse))
                        {
                            // CRITICAL: For database responses, use CONSERVATIVE quality check
                            // Only reject if answer explicitly indicates an error or missing data
                            // Short direct answers (e.g., "Eva De Vries") are VALID and should be accepted
                            var isExplicitError = IsExplicitDatabaseError(candidateDatabaseResponse.Answer, request.Query);
                            
                            if (isExplicitError)
                            {
                                // Database has data but AI explicitly cannot answer (e.g., "no information found")
                                // Allow document search as fallback
                                _logger.LogInformation("Database returned {RowCount} rows but AI explicitly indicates missing data - allowing document search as fallback", 
                                    candidateDatabaseResponse.Sources?.Sum(s => {
                                        if (s.FileName?.Contains(" rows)") == true)
                                        {
                                            var match = System.Text.RegularExpressions.Regex.Match(s.FileName, @"\((\d+) rows\)");
                                            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
                                        }
                                        return 0;
                                    }) ?? 0);
                                // Keep canAnswerFromDocuments = true to allow document search
                            }
                            else
                            {
                                databaseResponse = candidateDatabaseResponse;
                                // CRITICAL: ALWAYS perform document search in hybrid mode
                                // Even if database has an answer, document might have a BETTER answer
                                _logger.LogInformation("Database query returned answer, also performing document search for true hybrid strategy");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Database query failed in hybrid mode, continuing with document query only");
                }
            }

            if (request.CanAnswerFromDocuments)
            {
                var ragRequest = new Models.RequestResponse.GenerateRagAnswerRequest
                {
                    Query = request.Query,
                    MaxResults = request.MaxResults,
                    ConversationHistory = request.ConversationHistory,
                    PreferredLanguage = request.PreferredLanguage,
                    Options = request.Options,
                    PreCalculatedResults = request.PreCalculatedResults,
                    QueryTokens = request.QueryTokens
                };
                documentResponse = await _ragAnswerGenerator.Value.GenerateBasicRagAnswerAsync(ragRequest);
            }

            if (databaseResponse != null && documentResponse != null)
            {
                if (_responseBuilder != null)
                {
                    return await _responseBuilder.MergeHybridResultsAsync(request.Query, databaseResponse, documentResponse, request.ConversationHistory, request.PreferredLanguage);
                }

                var combinedSources = new List<SearchSource>();
                combinedSources.AddRange(databaseResponse.Sources);
                combinedSources.AddRange(documentResponse.Sources);
                var mergedAnswer = !string.IsNullOrEmpty(databaseResponse.Answer) && !string.IsNullOrEmpty(documentResponse.Answer)
                    ? $"{databaseResponse.Answer}\n\n{documentResponse.Answer}"
                    : databaseResponse.Answer ?? documentResponse.Answer ?? "No data available";
                return _responseBuilder?.CreateRagResponse(request.Query, mergedAnswer, combinedSources) ?? new RagResponse { Query = request.Query, Answer = mergedAnswer, Sources = combinedSources, SearchedAt = DateTime.UtcNow };
            }

            if (databaseResponse != null)
                return databaseResponse;

            if (documentResponse != null)
                return documentResponse;

            if (_responseBuilder != null)
            {
                return await _responseBuilder.CreateFallbackResponseAsync(request.Query, request.ConversationHistory, request.PreferredLanguage);
            }

            return new RagResponse { Query = request.Query, Answer = "Sorry, I cannot chat right now. Please try again later.", Sources = new List<SearchSource>(), SearchedAt = DateTime.UtcNow };
        }

        /// <summary>
        /// Executes a hybrid query strategy combining both database and document queries
        /// </summary>
        /// <param name="query">User query to process</param>
        /// <param name="maxResults">Maximum number of results</param>
        /// <param name="conversationHistory">Conversation history</param>
        /// <param name="hasDatabaseQueries">Flag indicating if database queries are available</param>
        /// <param name="canAnswerFromDocuments">Flag indicating if documents can answer</param>
        /// <param name="queryIntent">Query intent analysis result</param>
        /// <param name="preferredLanguage">Optional preferred language code for AI response</param>
        /// <param name="options">Optional search options</param>
        /// <param name="preCalculatedResults">Pre-calculated search results to use</param>
        /// <param name="queryTokens">Pre-computed query tokens for performance</param>
        /// <returns>Merged RAG response with answer and sources from both database and documents</returns>
        [Obsolete("Use ExecuteHybridStrategyAsync(HybridQueryStrategyRequest) instead. This method will be removed in v4.0.0")]
        public async Task<RagResponse> ExecuteHybridStrategyAsync(
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
            var request = new Models.RequestResponse.HybridQueryStrategyRequest
            {
                Query = query,
                MaxResults = maxResults,
                ConversationHistory = conversationHistory,
                HasDatabaseQueries = hasDatabaseQueries,
                CanAnswerFromDocuments = canAnswerFromDocuments,
                QueryIntent = queryIntent,
                PreferredLanguage = preferredLanguage,
                Options = options,
                PreCalculatedResults = preCalculatedResults,
                QueryTokens = queryTokens
            };
            return await ExecuteHybridStrategyAsync(request);
        }

        /// <summary>
        /// Executes a document-only query strategy
        /// </summary>
        /// <param name="request">Request containing query parameters</param>
        /// <returns>RAG response with answer and sources</returns>
        public async Task<RagResponse> ExecuteDocumentOnlyStrategyAsync(Models.RequestResponse.DocumentQueryStrategyRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var canAnswer = false;
            List<DocumentChunk>? results = request.PreCalculatedResults;

            if (request.CanAnswerFromDocuments.HasValue)
            {
                canAnswer = request.CanAnswerFromDocuments.Value;
            }
            else
            {
                var (CanAnswer, Results) = await _ragAnswerGenerator.Value.CanAnswerFromDocumentsAsync(request.Query, request.Options, request.QueryTokens);
                canAnswer = CanAnswer;
                results = Results;
            }

            if (canAnswer)
            {
                var ragRequest = new Models.RequestResponse.GenerateRagAnswerRequest
                {
                    Query = request.Query,
                    MaxResults = request.MaxResults,
                    ConversationHistory = request.ConversationHistory,
                    PreferredLanguage = request.PreferredLanguage,
                    Options = request.Options,
                    PreCalculatedResults = results,
                    QueryTokens = request.QueryTokens
                };
                return await _ragAnswerGenerator.Value.GenerateBasicRagAnswerAsync(ragRequest);
            }

            if (_responseBuilder != null)
            {
                return await _responseBuilder.CreateFallbackResponseAsync(request.Query, request.ConversationHistory, request.PreferredLanguage);
            }

            return new RagResponse { Query = request.Query, Answer = "Sorry, I cannot chat right now. Please try again later.", Sources = new List<SearchSource>(), SearchedAt = DateTime.UtcNow };
        }

        /// <summary>
        /// Executes a document-only query strategy
        /// </summary>
        /// <param name="query">User query to process</param>
        /// <param name="maxResults">Maximum number of results</param>
        /// <param name="conversationHistory">Conversation history</param>
        /// <param name="canAnswerFromDocuments">Flag indicating if documents can answer</param>
        /// <param name="preferredLanguage">Optional preferred language code for AI response</param>
        /// <param name="options">Optional search options</param>
        /// <param name="preCalculatedResults">Pre-calculated search results to use</param>
        /// <param name="queryTokens">Pre-computed query tokens for performance</param>
        /// <returns>RAG response with answer and sources</returns>
        [Obsolete("Use ExecuteDocumentOnlyStrategyAsync(DocumentQueryStrategyRequest) instead. This method will be removed in v4.0.0")]
        public async Task<RagResponse> ExecuteDocumentOnlyStrategyAsync(string query, int maxResults, string conversationHistory, bool? canAnswerFromDocuments = null, string? preferredLanguage = null, SearchOptions? options = null, List<DocumentChunk>? preCalculatedResults = null, List<string>? queryTokens = null)
        {
            var request = new Models.RequestResponse.DocumentQueryStrategyRequest
            {
                Query = query,
                MaxResults = maxResults,
                ConversationHistory = conversationHistory,
                CanAnswerFromDocuments = canAnswerFromDocuments,
                PreferredLanguage = preferredLanguage,
                Options = options,
                PreCalculatedResults = preCalculatedResults,
                QueryTokens = queryTokens
            };
            return await ExecuteDocumentOnlyStrategyAsync(request);
        }

        /// <summary>
        /// Fallback method for HasMeaningfulData when IResponseBuilderService is not available
        /// </summary>
        private static bool HasMeaningfulDataFallback(RagResponse? response)
        {
            if (response == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(response.Answer))
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

        /// <summary>
        /// CONSERVATIVE check for database responses: Only reject if explicitly indicates error or missing data
        /// This prevents false positives on short, direct answers (e.g., "Eva De Vries", "42", "Tokyo")
        /// Complies with Rule 6: Uses generic heuristics instead of hardcoded language lists
        /// </summary>
        /// <param name="answer">AI-generated answer from database query</param>
        /// <param name="query">The original user query for echo detection</param>
        /// <returns>True if answer explicitly indicates error or missing data, False otherwise</returns>
        private static bool IsExplicitDatabaseError(string? answer, string query)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return true; // Empty answer is an error
            }

            var lowerAnswer = answer.ToLowerInvariant();

            // 1. Universal/English explicit negative patterns (acceptable as code lingua franca)
            var universalNegativePatterns = new[]
            {
                "no information", "not found", "not available", "cannot find", "unable to find",
                "not provided", "no data", "cannot answer", "cannot determine", "could not find",
                "no results", "not present", "does not exist", "error"
            };

            if (universalNegativePatterns.Any(p => lowerAnswer.Contains(p)))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var queryWords = query.ToLowerInvariant()
                    .Split(new[] { ' ', ',', '.', '?', '!', ':', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 3)
                    .ToList();

                if (queryWords.Count > 0)
                {
                    int matchCount = queryWords.Count(w => lowerAnswer.Contains(w));
                    double matchRatio = (double)matchCount / queryWords.Count;

                    if (matchRatio >= 0.5 && !answer.Any(char.IsDigit))
                    {
                        if (answer.Length > 30)
                        {
                            return true; 
                        }
                    }
                }
            }

            return false;
        }
    }
}

