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
        Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5);

        /// <summary>
        /// Process intelligent query with RAG and automatic session management
        /// </summary>
        Task<RagResponse> QueryIntelligenceAsync(string query, int maxResults = 5, bool startNewConversation = false);

        /// <summary>
        /// Generate RAG answer with automatic session management (Legacy method - use QueryIntelligenceAsync)
        /// </summary>
        [Obsolete("Use QueryIntelligenceAsync instead. This method will be removed in v4.0.0")]
        Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5, bool startNewConversation = false);
    }
}
