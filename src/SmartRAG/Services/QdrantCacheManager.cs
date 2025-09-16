using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartRAG.Services
{
    /// <summary>
    /// Service for managing search result caching in Qdrant operations
    /// </summary>
    public class QdrantCacheManager : IQdrantCacheManager
    {
        #region Constants

        private const int CacheExpiryMinutes = 5;

        #endregion

        #region Fields

        private readonly ILogger<QdrantCacheManager> _logger;
        private static readonly Dictionary<string, (List<DocumentChunk> Chunks, DateTime Expiry)> _searchCache = new Dictionary<string, (List<DocumentChunk> Chunks, DateTime Expiry)>();
        private static readonly object _cacheLock = new object();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the QdrantCacheManager
        /// </summary>
        /// <param name="logger">Logger instance for this service</param>
        public QdrantCacheManager(ILogger<QdrantCacheManager> logger)
        {
            _logger = logger;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets cached search results if available and not expired
        /// </summary>
        /// <param name="queryHash">Hash of the search query</param>
        /// <returns>Cached results or null if not found/expired</returns>
        public List<DocumentChunk> GetCachedResults(string queryHash)
        {
            lock (_cacheLock)
            {
                if (_searchCache.TryGetValue(queryHash, out var cached) && cached.Expiry > DateTime.UtcNow)
                {
                    return cached.Chunks.ToList(); // Return copy to avoid modification
                }
                return null;
            }
        }

        /// <summary>
        /// Caches search results for future use
        /// </summary>
        /// <param name="queryHash">Hash of the search query</param>
        /// <param name="results">Results to cache</param>
        public void CacheResults(string queryHash, List<DocumentChunk> results)
        {
            lock (_cacheLock)
            {
                _searchCache[queryHash] = (results.ToList(), DateTime.UtcNow.AddMinutes(CacheExpiryMinutes));

                // Clean up expired cache entries
                CleanupExpiredCache();

                _logger.LogDebug("Search results cached for query hash: {QueryHash}, cache size: {CacheSize}", 
                    queryHash, _searchCache.Count);
            }
        }

        /// <summary>
        /// Cleans up expired cache entries
        /// </summary>
        public void CleanupExpiredCache()
        {
            var expiredKeys = _searchCache.Where(kvp => kvp.Value.Expiry <= DateTime.UtcNow).Select(kvp => kvp.Key).ToList();
            foreach (var key in expiredKeys)
            {
                _searchCache.Remove(key);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogDebug("Cleaned up {ExpiredCount} expired cache entries", expiredKeys.Count);
            }
        }

        #endregion
    }
}
