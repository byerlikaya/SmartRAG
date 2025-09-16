using SmartRAG.Entities;
using System.Collections.Generic;

namespace SmartRAG.Interfaces
{
    /// <summary>
    /// Interface for managing search result caching in Qdrant operations
    /// </summary>
    public interface IQdrantCacheManager
    {
        /// <summary>
        /// Gets cached search results if available and not expired
        /// </summary>
        /// <param name="queryHash">Hash of the search query</param>
        /// <returns>Cached results or null if not found/expired</returns>
        List<DocumentChunk> GetCachedResults(string queryHash);

        /// <summary>
        /// Caches search results for future use
        /// </summary>
        /// <param name="queryHash">Hash of the search query</param>
        /// <param name="results">Results to cache</param>
        void CacheResults(string queryHash, List<DocumentChunk> results);

        /// <summary>
        /// Cleans up expired cache entries
        /// </summary>
        void CleanupExpiredCache();
    }
}
