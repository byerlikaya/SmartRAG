#nullable enable

using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Database;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Support;
using SmartRAG.Models;
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
        #region Fields

        private readonly IMultiDatabaseQueryCoordinator? _multiDatabaseQueryCoordinator;
        private readonly ILogger<QueryStrategyExecutorService> _logger;
        private readonly Lazy<IRagAnswerGeneratorService> _ragAnswerGenerator;
        private readonly IResponseBuilderService? _responseBuilder;
        private readonly IConversationManagerService _conversationManager;

        #endregion

        #region Constructor

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

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes a database-only query strategy with fallback to document query
        /// </summary>
        public async Task<RagResponse> ExecuteDatabaseOnlyStrategyAsync(string query, int maxResults, string conversationHistory, bool canAnswerFromDocuments, QueryIntent? queryIntent, string? preferredLanguage = null, SearchOptions? options = null, List<string>? queryTokens = null)
        {
            try
            {
                if (queryIntent == null)
                {
                    return await ExecuteDocumentOnlyStrategyAsync(query, maxResults, conversationHistory, canAnswerFromDocuments, preferredLanguage, options, null, queryTokens);
                }

                var databaseResponse = await _multiDatabaseQueryCoordinator!.QueryMultipleDatabasesAsync(query, queryIntent, maxResults);

                if (_responseBuilder?.HasMeaningfulData(databaseResponse) ?? HasMeaningfulDataFallback(databaseResponse))
                {
                    return databaseResponse;
                }

                _logger.LogInformation("Database query returned no meaningful data, falling back to document search");
                return await ExecuteDocumentOnlyStrategyAsync(query, maxResults, conversationHistory, canAnswerFromDocuments, preferredLanguage, options, null, queryTokens);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database query failed, falling back to document query");
                return await ExecuteDocumentOnlyStrategyAsync(query, maxResults, conversationHistory, canAnswerFromDocuments, preferredLanguage, options, null, queryTokens);
            }
        }

        /// <summary>
        /// Executes a hybrid query strategy combining both database and document queries
        /// </summary>
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
            RagResponse? databaseResponse = null;
            RagResponse? documentResponse = null;

            if (hasDatabaseQueries)
            {
                try
                {
                    if (queryIntent != null)
                    {
                        var candidateDatabaseResponse = await _multiDatabaseQueryCoordinator!.QueryMultipleDatabasesAsync(query, queryIntent, maxResults);
                        if (_responseBuilder?.HasMeaningfulData(candidateDatabaseResponse) ?? HasMeaningfulDataFallback(candidateDatabaseResponse))
                        {
                            databaseResponse = candidateDatabaseResponse;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Database query failed in hybrid mode, continuing with document query only");
                }
            }

            if (canAnswerFromDocuments)
            {
                documentResponse = await _ragAnswerGenerator.Value.GenerateBasicRagAnswerAsync(query, maxResults, conversationHistory, preferredLanguage, options, preCalculatedResults, queryTokens);
            }

            if (databaseResponse != null && documentResponse != null)
            {
                if (_responseBuilder != null)
                {
                    return await _responseBuilder.MergeHybridResultsAsync(query, databaseResponse, documentResponse, conversationHistory, preferredLanguage);
                }

                // Fallback: Simple merge without AI processing
                var combinedSources = new List<SearchSource>();
                combinedSources.AddRange(databaseResponse.Sources);
                combinedSources.AddRange(documentResponse.Sources);
                var mergedAnswer = !string.IsNullOrEmpty(databaseResponse.Answer) && !string.IsNullOrEmpty(documentResponse.Answer)
                    ? $"{databaseResponse.Answer}\n\n{documentResponse.Answer}"
                    : databaseResponse.Answer ?? documentResponse.Answer ?? "No data available";
                return _responseBuilder?.CreateRagResponse(query, mergedAnswer, combinedSources) ?? new RagResponse { Query = query, Answer = mergedAnswer, Sources = combinedSources, SearchedAt = DateTime.UtcNow };
            }

            if (databaseResponse != null)
                return databaseResponse;

            if (documentResponse != null)
                return documentResponse;

            if (_responseBuilder != null)
            {
                return await _responseBuilder.CreateFallbackResponseAsync(query, conversationHistory, preferredLanguage);
            }

            return new RagResponse { Query = query, Answer = "Sorry, I cannot chat right now. Please try again later.", Sources = new List<SearchSource>(), SearchedAt = DateTime.UtcNow };
        }

        /// <summary>
        /// Executes a document-only query strategy
        /// </summary>
        public async Task<RagResponse> ExecuteDocumentOnlyStrategyAsync(string query, int maxResults, string conversationHistory, bool? canAnswerFromDocuments = null, string? preferredLanguage = null, SearchOptions? options = null, List<DocumentChunk>? preCalculatedResults = null, List<string>? queryTokens = null)
        {
            var canAnswer = false;
            List<DocumentChunk>? results = preCalculatedResults;

            if (canAnswerFromDocuments.HasValue)
            {
                canAnswer = canAnswerFromDocuments.Value;
            }
            else
            {
                var (CanAnswer, Results) = await _ragAnswerGenerator.Value.CanAnswerFromDocumentsAsync(query, options, queryTokens);
                canAnswer = CanAnswer;
                results = Results;
            }

            if (canAnswer)
            {
                return await _ragAnswerGenerator.Value.GenerateBasicRagAnswerAsync(query, maxResults, conversationHistory, preferredLanguage, options, results, queryTokens);
            }

            if (_responseBuilder != null)
            {
                return await _responseBuilder.CreateFallbackResponseAsync(query, conversationHistory, preferredLanguage);
            }

            return new RagResponse { Query = query, Answer = "Sorry, I cannot chat right now. Please try again later.", Sources = new List<SearchSource>(), SearchedAt = DateTime.UtcNow };
        }

        #endregion

        #region Private Methods

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


        #endregion
    }
}

