using SmartRAG.Entities;
using SmartRAG.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Search.Strategies
{
    /// <summary>
    /// Strategy for calculating relevance scores for document chunks
    /// </summary>
    public interface IScoringStrategy
    {
        /// <summary>
        /// Calculates the relevance score for a document chunk based on the query
        /// </summary>
        /// <param name="query">The search query</param>
        /// <param name="chunk">The document chunk to score</param>
        /// <param name="queryEmbedding">The embedding vector of the query</param>
        /// <returns>A score between 0 and 1</returns>
        Task<double> CalculateScoreAsync(string query, DocumentChunk chunk, List<float> queryEmbedding);
    }
}
