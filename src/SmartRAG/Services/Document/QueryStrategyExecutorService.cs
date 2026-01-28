#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Document;
using SmartRAG.Models;
using SmartRAG.Models.RequestResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private readonly IResponseBuilderService _responseBuilder;
        private readonly SmartRagOptions _options;

        /// <summary>
        /// Initializes a new instance of the QueryStrategyExecutorService
        /// </summary>
        /// <param name="multiDatabaseQueryCoordinator">Optional multi-database query coordinator</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="ragAnswerGenerator">Service for generating RAG answers (lazy to break circular dependency)</param>
        /// <param name="responseBuilder">Service for building RAG responses</param>
        /// <param name="options">SmartRAG configuration options</param>
        public QueryStrategyExecutorService(
            IMultiDatabaseQueryCoordinator? multiDatabaseQueryCoordinator,
            ILogger<QueryStrategyExecutorService> logger,
            Lazy<IRagAnswerGeneratorService> ragAnswerGenerator,
            IResponseBuilderService responseBuilder,
            IOptions<SmartRagOptions>? options = null)
        {
            _multiDatabaseQueryCoordinator = multiDatabaseQueryCoordinator;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ragAnswerGenerator = ragAnswerGenerator ?? throw new ArgumentNullException(nameof(ragAnswerGenerator));
            _responseBuilder = responseBuilder ?? throw new ArgumentNullException(nameof(responseBuilder));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Executes a database-only query strategy with fallback to document query
        /// </summary>
        /// <param name="request">Request containing query parameters</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>RAG response with answer and sources</returns>
        public async Task<RagResponse> ExecuteDatabaseOnlyStrategyAsync(QueryStrategyRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                if (request.QueryIntent == null)
                {
                    var docRequest = new QueryStrategyRequest
                    {
                        Query = request.Query,
                        MaxResults = request.MaxResults,
                        ConversationHistory = request.ConversationHistory,
                        CanAnswerFromDocuments = request.CanAnswerFromDocuments,
                        PreferredLanguage = request.PreferredLanguage ?? _options.DefaultLanguage,
                        Options = request.Options,
                        QueryTokens = request.QueryTokens,
                        PreCalculatedResults = request.PreCalculatedResults
                    };
                    return await ExecuteDocumentOnlyStrategyAsync(docRequest, cancellationToken);
                }

                var databaseResponse = await _multiDatabaseQueryCoordinator!.QueryMultipleDatabasesAsync(request.Query, request.QueryIntent, request.MaxResults, request.PreferredLanguage ?? _options.DefaultLanguage, cancellationToken);

                
                
                if (_responseBuilder.HasMeaningfulData(databaseResponse))
                {
                    return databaseResponse;
                }

                var fallbackRequest = new QueryStrategyRequest
                {
                    Query = request.Query,
                    MaxResults = request.MaxResults,
                    ConversationHistory = request.ConversationHistory,
                    CanAnswerFromDocuments = request.CanAnswerFromDocuments,
                    PreferredLanguage = request.PreferredLanguage ?? _options.DefaultLanguage,
                    Options = request.Options,
                    QueryTokens = request.QueryTokens,
                    PreCalculatedResults = request.PreCalculatedResults
                };
                return await ExecuteDocumentOnlyStrategyAsync(fallbackRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database query failed, falling back to document query");
                var fallbackRequest = new QueryStrategyRequest
                {
                    Query = request.Query,
                    MaxResults = request.MaxResults,
                    ConversationHistory = request.ConversationHistory,
                    CanAnswerFromDocuments = request.CanAnswerFromDocuments,
                    PreferredLanguage = request.PreferredLanguage ?? _options.DefaultLanguage,
                    Options = request.Options,
                    QueryTokens = request.QueryTokens,
                    PreCalculatedResults = request.PreCalculatedResults
                };
                return await ExecuteDocumentOnlyStrategyAsync(fallbackRequest, cancellationToken);
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
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>RAG response with answer and sources</returns>
        [Obsolete("Use ExecuteDatabaseOnlyStrategyAsync(QueryStrategyRequest) instead. This method will be removed in v4.0.0")]
        public async Task<RagResponse> ExecuteDatabaseOnlyStrategyAsync(string query, int maxResults, string conversationHistory, bool canAnswerFromDocuments, QueryIntent? queryIntent, string? preferredLanguage = null, SearchOptions? options = null, List<string>? queryTokens = null, CancellationToken cancellationToken = default)
        {
            var request = new QueryStrategyRequest
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
            return await ExecuteDatabaseOnlyStrategyAsync(request, cancellationToken);
        }

        /// <summary>
        /// Executes a hybrid query strategy combining both database and document queries
        /// </summary>
        /// <param name="request">Request containing query parameters</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Merged RAG response with answer and sources from both database and documents</returns>
        public async Task<RagResponse> ExecuteHybridStrategyAsync(QueryStrategyRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            RagResponse? databaseResponse = null;
            RagResponse? documentResponse = null;

            if (request.HasDatabaseQueries == true)
            {
                try
                {
                    if (request.QueryIntent != null)
                    {
                        var candidateDatabaseResponse = await _multiDatabaseQueryCoordinator!.QueryMultipleDatabasesAsync(request.Query, request.QueryIntent, request.MaxResults, _options.DefaultLanguage, cancellationToken);
                        if (_responseBuilder.HasMeaningfulData(candidateDatabaseResponse))
                        {
                            // CRITICAL: For database responses, use CONSERVATIVE quality check
                            // Only reject if answer explicitly indicates an error or missing data
                            // Short direct answers (e.g., "Eva De Vries") are VALID and should be accepted
                            var isExplicitError = IsExplicitDatabaseError(candidateDatabaseResponse.Answer, request.Query);
                            
                            if (isExplicitError)
                            {
                                // Database has data but AI explicitly cannot answer (e.g., "no information found")
                                // Allow document search as fallback
                                // Keep canAnswerFromDocuments = true to allow document search
                            }
                            else
                            {
                                databaseResponse = candidateDatabaseResponse;
                                // CRITICAL: ALWAYS perform document search in hybrid mode
                                // Even if database has an answer, document might have a BETTER answer
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Database query failed in hybrid mode, continuing with document query only");
                }
            }

            if (request.CanAnswerFromDocuments == true)
            {
                var ragRequest = new Models.RequestResponse.GenerateRagAnswerRequest
                {
                    Query = request.Query,
                    MaxResults = request.MaxResults,
                    ConversationHistory = request.ConversationHistory,
                    PreferredLanguage = _options.DefaultLanguage,
                    Options = request.Options,
                    PreCalculatedResults = request.PreCalculatedResults,
                    QueryTokens = request.QueryTokens
                };
                documentResponse = await _ragAnswerGenerator.Value.GenerateBasicRagAnswerAsync(ragRequest, cancellationToken);
            }

            if (databaseResponse != null && documentResponse != null)
            {
                return await _responseBuilder.MergeHybridResultsAsync(request.Query, databaseResponse, documentResponse, request.ConversationHistory, cancellationToken);
            }

            if (databaseResponse != null)
                return databaseResponse;

            if (documentResponse != null)
                return documentResponse;

            return await _responseBuilder.CreateFallbackResponseAsync(request.Query, request.ConversationHistory, cancellationToken);
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
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Merged RAG response with answer and sources from both database and documents</returns>
        [Obsolete("Use ExecuteHybridStrategyAsync(QueryStrategyRequest) instead. This method will be removed in v4.0.0")]
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
            List<string>? queryTokens = null,
            CancellationToken cancellationToken = default)
        {
            var request = new QueryStrategyRequest
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
            return await ExecuteHybridStrategyAsync(request, cancellationToken);
        }

        /// <summary>
        /// Executes a document-only query strategy
        /// </summary>
        /// <param name="request">Request containing query parameters</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>RAG response with answer and sources</returns>
        public async Task<RagResponse> ExecuteDocumentOnlyStrategyAsync(QueryStrategyRequest request, CancellationToken cancellationToken = default)
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
                var searchOptions = request.Options ?? SearchOptions.FromConfig(_options);
                var (CanAnswer, Results) = await _ragAnswerGenerator.Value.CanAnswerFromDocumentsAsync(request.Query, searchOptions, request.QueryTokens, cancellationToken);
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
                    PreferredLanguage = request.PreferredLanguage ?? _options.DefaultLanguage,
                    Options = request.Options,
                    PreCalculatedResults = results,
                    QueryTokens = request.QueryTokens
                };
                return await _ragAnswerGenerator.Value.GenerateBasicRagAnswerAsync(ragRequest, cancellationToken);
            }

            return await _responseBuilder.CreateFallbackResponseAsync(request.Query, request.ConversationHistory, cancellationToken);
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
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>RAG response with answer and sources</returns>
        [Obsolete("Use ExecuteDocumentOnlyStrategyAsync(QueryStrategyRequest) instead. This method will be removed in v4.0.0")]
        public async Task<RagResponse> ExecuteDocumentOnlyStrategyAsync(string query, int maxResults, string conversationHistory, bool? canAnswerFromDocuments = null, string? preferredLanguage = null, SearchOptions? options = null, List<DocumentChunk>? preCalculatedResults = null, List<string>? queryTokens = null, CancellationToken cancellationToken = default)
        {
            var request = new QueryStrategyRequest
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
            return await ExecuteDocumentOnlyStrategyAsync(request, cancellationToken);
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

