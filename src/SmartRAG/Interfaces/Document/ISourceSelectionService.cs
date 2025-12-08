#nullable enable

using SmartRAG.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Document
{
    /// <summary>
    /// Service for determining whether document search results are sufficient to skip other sources
    /// </summary>
    public interface ISourceSelectionService
    {
        /// <summary>
        /// Determines if other sources should be skipped based on document search results
        /// </summary>
        /// <param name="canAnswer">Whether documents can answer the query</param>
        /// <param name="results">Document chunks from search</param>
        /// <param name="minRelevanceScore">Optional minimum relevance score threshold (null uses adaptive threshold)</param>
        /// <returns>True if other sources should be skipped, false otherwise</returns>
        Task<bool> ShouldSkipOtherSourcesAsync(bool canAnswer, List<DocumentChunk> results, double? minRelevanceScore = null);

        /// <summary>
        /// Calculates confidence score based on document search results
        /// </summary>
        /// <param name="results">Document chunks from search</param>
        /// <returns>Confidence score (0.0-1.0 for vector search, 0.0+ for text search)</returns>
        double CalculateConfidenceScore(List<DocumentChunk> results);
    }
}
