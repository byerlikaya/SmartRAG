#nullable enable

using SmartRAG.Entities;
using SmartRAG.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Document
{
    /// <summary>
    /// Service interface for executing document search strategies
    /// </summary>
    public interface IDocumentSearchStrategyService
    {
        /// <summary>
        /// Searches for relevant document chunks using repository's optimized search with keyword-based fallback
        /// </summary>
        /// <param name="query">Search query string</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <param name="options">Optional search options to filter documents</param>
        /// <param name="queryTokens">Pre-computed query tokens for performance</param>
        /// <returns>List of relevant document chunks</returns>
        Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults, SearchOptions? options = null, List<string>? queryTokens = null);
    }
}

