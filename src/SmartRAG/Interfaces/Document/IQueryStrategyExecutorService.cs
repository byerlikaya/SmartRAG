#nullable enable

using SmartRAG.Entities;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Document
{
    /// <summary>
    /// Service interface for executing query strategies
    /// </summary>
    public interface IQueryStrategyExecutorService
    {
        /// <summary>
        /// Executes a database-only query strategy with fallback to document query
        /// </summary>
        /// <param name="request">Request containing query parameters</param>
        /// <returns>RAG response with answer and sources</returns>
        Task<RagResponse> ExecuteDatabaseOnlyStrategyAsync(Models.RequestResponse.DatabaseQueryStrategyRequest request);

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
        Task<RagResponse> ExecuteDatabaseOnlyStrategyAsync(string query, int maxResults, string conversationHistory, bool canAnswerFromDocuments, QueryIntent? queryIntent, string? preferredLanguage = null, SearchOptions? options = null, List<string>? queryTokens = null);

        /// <summary>
        /// Executes a hybrid query strategy combining both database and document queries
        /// </summary>
        /// <param name="request">Request containing query parameters</param>
        /// <returns>Merged RAG response with answer and sources from both database and documents</returns>
        Task<RagResponse> ExecuteHybridStrategyAsync(Models.RequestResponse.HybridQueryStrategyRequest request);

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
        Task<RagResponse> ExecuteHybridStrategyAsync(
            string query,
            int maxResults,
            string conversationHistory,
            bool hasDatabaseQueries,
            bool canAnswerFromDocuments,
            QueryIntent? queryIntent,
            string? preferredLanguage = null,
            SearchOptions? options = null,
            List<DocumentChunk>? preCalculatedResults = null,
            List<string>? queryTokens = null);

        /// <summary>
        /// Executes a document-only query strategy
        /// </summary>
        /// <param name="request">Request containing query parameters</param>
        /// <returns>RAG response with answer and sources</returns>
        Task<RagResponse> ExecuteDocumentOnlyStrategyAsync(Models.RequestResponse.DocumentQueryStrategyRequest request);

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
        Task<RagResponse> ExecuteDocumentOnlyStrategyAsync(string query, int maxResults, string conversationHistory, bool? canAnswerFromDocuments = null, string? preferredLanguage = null, SearchOptions? options = null, List<DocumentChunk>? preCalculatedResults = null, List<string>? queryTokens = null);
    }
}

