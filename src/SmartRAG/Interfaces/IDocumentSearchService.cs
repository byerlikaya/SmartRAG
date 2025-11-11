using SmartRAG.Entities;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces
{

    /// <summary>
    /// Service interface for AI-powered intelligence and RAG operations
    /// </summary>
    public interface IDocumentSearchService
    {
        /// <summary>
        /// Search documents semantically
        /// </summary>
        /// <param name="query">Natural language query to search for</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of relevant document chunks</returns>
        Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5);

        /// <summary>
        /// Process intelligent query with RAG and automatic session management
        /// </summary>
        /// <param name="query">Natural language query to process</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <param name="startNewConversation">Whether to start a new conversation session</param>
        /// <returns>RAG response with AI-generated answer and relevant sources</returns>
        Task<RagResponse> QueryIntelligenceAsync(string query, int maxResults = 5, bool startNewConversation = false);

        /// <summary>
        /// Generate RAG answer with automatic session management (Legacy method - use QueryIntelligenceAsync)
        /// </summary>
        /// <param name="query">Natural language query to process</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <param name="startNewConversation">Whether to start a new conversation session</param>
        /// <returns>RAG response with AI-generated answer and relevant sources</returns>
        [Obsolete("Use QueryIntelligenceAsync instead. This method will be removed in v4.0.0")]
        Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5, bool startNewConversation = false);
    }
}
