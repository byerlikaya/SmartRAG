using SmartRAG.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRAG.Interfaces.Search
{
    /// <summary>
    /// Service interface for embedding-based search operations
    /// </summary>
    public interface IEmbeddingSearchService
    {
        /// <summary>
        /// Performs embedding-based search on document chunks
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="allChunks">All available document chunks</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of relevant document chunks</returns>
        Task<List<DocumentChunk>> SearchByEmbeddingAsync(string query, List<DocumentChunk> allChunks, int maxResults);

    }
}

