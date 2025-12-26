#nullable enable

using SmartRAG.Entities;
using SmartRAG.Models;
using SmartRAG.Models.RequestResponse;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Document
{
    /// <summary>
    /// Service interface for generating RAG answers from documents
    /// </summary>
    public interface IRagAnswerGeneratorService
    {
        /// <summary>
        /// Generates RAG answer with automatic session management and context expansion
        /// </summary>
        /// <param name="request">Request containing query parameters</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>RAG response with answer and sources</returns>
        Task<RagResponse> GenerateBasicRagAnswerAsync(Models.RequestResponse.GenerateRagAnswerRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates RAG answer with automatic session management and context expansion
        /// </summary>
        /// <param name="query">User query to process</param>
        /// <param name="maxResults">Maximum number of document chunks to use</param>
        /// <param name="conversationHistory">Conversation history</param>
        /// <param name="preferredLanguage">Optional preferred language code for AI response</param>
        /// <param name="options">Optional search options</param>
        /// <param name="preCalculatedResults">Pre-calculated search results to use</param>
        /// <param name="queryTokens">Pre-computed query tokens for performance</param>
        /// <returns>RAG response with answer and sources</returns>
        [Obsolete("Use GenerateBasicRagAnswerAsync(GenerateRagAnswerRequest) instead. This method will be removed in v4.0.0")]
        Task<RagResponse> GenerateBasicRagAnswerAsync(string query, int maxResults, string conversationHistory, string? preferredLanguage = null, SearchOptions? options = null, List<DocumentChunk>? preCalculatedResults = null, List<string>? queryTokens = null);

        /// <summary>
        /// Determines if a query can be answered from documents using language-agnostic content-based analysis
        /// </summary>
        /// <param name="query">User query to analyze</param>
        /// <param name="searchOptions">Search options (tag parsing should be done before calling this method)</param>
        /// <param name="queryTokens">Pre-computed query tokens for performance</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Tuple containing whether documents can answer and the found chunks</returns>
        Task<(bool CanAnswer, List<DocumentChunk> Results)> CanAnswerFromDocumentsAsync(string query, SearchOptions searchOptions, List<string>? queryTokens = null, System.Threading.CancellationToken cancellationToken = default);
    }
}

