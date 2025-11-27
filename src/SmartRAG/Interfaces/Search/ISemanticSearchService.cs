using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Search
{
    /// <summary>
    /// Interface for semantic search service
    /// </summary>
    public interface ISemanticSearchService
    {
        /// <summary>
        /// Calculate enhanced semantic similarity using advanced text analysis
        /// </summary>
        /// <param name="query">Search query text</param>
        /// <param name="content">Content text to compare against</param>
        /// <returns>Similarity score between 0.0 and 1.0</returns>
        Task<double> CalculateEnhancedSemanticSimilarityAsync(string query, string content);
    }
}
