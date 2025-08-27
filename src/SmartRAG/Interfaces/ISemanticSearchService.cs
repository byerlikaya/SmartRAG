using System.Threading.Tasks;

namespace SmartRAG.Interfaces
{
    /// <summary>
    /// Interface for semantic search service
    /// </summary>
    public interface ISemanticSearchService
    {
        /// <summary>
        /// Calculate enhanced semantic similarity using advanced text analysis
        /// </summary>
        Task<double> CalculateEnhancedSemanticSimilarityAsync(string query, string content);
    }
}
