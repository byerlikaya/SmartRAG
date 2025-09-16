using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services
{
    /// <summary>
    /// Service for performing searches in Qdrant vector database
    /// </summary>
    public class QdrantSearchService : IQdrantSearchService, IDisposable
    {
        #region Constants

        private const int DefaultMaxSearchResults = 5;
        private const int DefaultScrollBatchSize = 25;
        private const double DefaultTextSearchScore = 0.5;
        private const double MinKeywordMatchScore = 0.1;
        private const int MinWordLength = 2;

        #endregion

        #region Fields

        private readonly QdrantClient _client;
        private readonly QdrantConfig _config;
        private readonly string _collectionName;
        private readonly ILogger<QdrantSearchService> _logger;
        private readonly IQdrantEmbeddingService _embeddingService;
        private bool _isDisposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the QdrantSearchService
        /// </summary>
        /// <param name="config">Qdrant configuration options</param>
        /// <param name="logger">Logger instance for this service</param>
        /// <param name="embeddingService">Embedding service for generating query embeddings</param>
        public QdrantSearchService(
            IOptions<QdrantConfig> config,
            ILogger<QdrantSearchService> logger,
            IQdrantEmbeddingService embeddingService)
        {
            _config = config.Value;
            _collectionName = _config.CollectionName;
            _logger = logger;
            _embeddingService = embeddingService;

            string host;
            bool useHttps;

            if (config.Value.Host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || config.Value.Host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(config.Value.Host);
                host = uri.Host;
                useHttps = uri.Scheme == "https";
            }
            else
            {
                host = config.Value.Host;
                useHttps = config.Value.UseHttps;
            }

            _client = new QdrantClient(
                host,
                https: useHttps,
                apiKey: config.Value.ApiKey,
                grpcTimeout: TimeSpan.FromMinutes(5)
            );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs vector search across all document collections
        /// </summary>
        /// <param name="queryEmbedding">Embedding vector for the search query</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of relevant document chunks</returns>
        public async Task<List<DocumentChunk>> SearchAsync(List<float> queryEmbedding, int maxResults)
        {
            try
            {
                _logger.LogDebug("Starting vector search with {MaxResults} max results", maxResults);

                var allChunks = new List<DocumentChunk>();
                var collections = await _client.ListCollectionsAsync();

                // Look for collections that match our document collection pattern
                var documentCollections = collections.Where(c => c.StartsWith(_collectionName + "_doc_", StringComparison.OrdinalIgnoreCase)).ToList();

                // If no document collections found, check main collection
                if (documentCollections.Count == 0)
                {
                    if (collections.Contains(_collectionName))
                    {
                        documentCollections.Add(_collectionName);
                    }
                }

                foreach (var collectionName in documentCollections)
                {
                    try
                    {
                        var searchResults = await _client.SearchAsync(
                            collectionName: collectionName,
                            vector: queryEmbedding.ToArray(),
                            limit: (ulong)Math.Max(20, maxResults * 4)
                        );

                        _logger.LogDebug("Found {Count} results in collection {Collection}", searchResults.Count, collectionName);

                        // Convert to DocumentChunk objects
                        foreach (var result in searchResults)
                        {
                            var payload = result.Payload;

                            if (payload != null)
                            {
                                var content = GetPayloadString(payload, "content");
                                var docId = GetPayloadString(payload, "documentId");
                                var chunkIndex = GetPayloadString(payload, "chunkIndex");

                                if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(docId) && !string.IsNullOrEmpty(chunkIndex))
                                {
                                    var chunk = new DocumentChunk
                                    {
                                        Id = Guid.NewGuid(),
                                        DocumentId = Guid.Parse(docId),
                                        Content = content,
                                        ChunkIndex = int.Parse(chunkIndex, CultureInfo.InvariantCulture),
                                        RelevanceScore = result.Score
                                    };
                                    allChunks.Add(chunk);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Vector search failed for collection: {Collection}", collectionName);
                    }
                }

                return allChunks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vector search failed");
                return new List<DocumentChunk>();
            }
        }

        /// <summary>
        /// Performs fallback text search when vector search fails
        /// </summary>
        /// <param name="query">Text query to search for</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of relevant document chunks</returns>
        public async Task<List<DocumentChunk>> FallbackTextSearchAsync(string query, int maxResults)
        {
            try
            {
                _logger.LogDebug("Starting fallback text search for query: {Query}", query);
                var queryLower = query.ToLowerInvariant();
                var relevantChunks = new List<DocumentChunk>();

                // Get all collections to search in
                var collections = await _client.ListCollectionsAsync();
                var documentCollections = collections.Where(c => c.StartsWith(_collectionName + "_doc_", StringComparison.OrdinalIgnoreCase)).ToList();

                if (documentCollections.Count == 0)
                {
                    return new List<DocumentChunk>();
                }

                foreach (var collectionName in documentCollections)
                {
                    try
                    {
                        // Get all points from collection
                        var scrollResult = await _client.ScrollAsync(collectionName, limit: 1000);

                        foreach (var point in scrollResult.Result)
                        {
                            var payload = point.Payload;
                            if (payload != null)
                            {
                                var content = GetPayloadString(payload, "content");
                                var docId = GetPayloadString(payload, "documentId");
                                var chunkIndex = GetPayloadString(payload, "chunkIndex");

                                if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(docId) && !string.IsNullOrEmpty(chunkIndex))
                                {
                                    var contentStr = content.ToLowerInvariant();

                                    // Simple text matching
                                    if (contentStr.Contains(queryLower))
                                    {
                                        var chunk = new DocumentChunk
                                        {
                                            Id = Guid.NewGuid(),
                                            DocumentId = Guid.Parse(docId),
                                            Content = content,
                                            ChunkIndex = int.Parse(chunkIndex, CultureInfo.InvariantCulture),
                                            RelevanceScore = DefaultTextSearchScore
                                        };
                                        relevantChunks.Add(chunk);

                                        if (relevantChunks.Count >= maxResults)
                                            break;
                                    }
                                }
                            }
                        }

                        if (relevantChunks.Count >= maxResults)
                            break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fallback search failed for collection: {Collection}", collectionName);
                    }
                }

                _logger.LogDebug("Fallback text search found {Count} results", relevantChunks.Count);
                return relevantChunks.Take(maxResults).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallback text search failed");
                return new List<DocumentChunk>();
            }
        }

        /// <summary>
        /// Performs hybrid search combining vector and keyword matching
        /// </summary>
        /// <param name="query">Text query to search for</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of relevant document chunks</returns>
        public async Task<List<DocumentChunk>> HybridSearchAsync(string query, int maxResults)
        {
            var hybridResults = new List<DocumentChunk>();

            try
            {
                _logger.LogDebug("Starting hybrid search for query: {Query}", query);

                // Extract meaningful keywords from query
                var keywords = ExtractImportantKeywords(query);

                if (keywords.Count == 0)
                {
                    return hybridResults;
                }

                var collections = await _client.ListCollectionsAsync();
                var documentCollections = collections.Where(c => c.StartsWith($"{_collectionName}_doc_", StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var collectionName in documentCollections)
                {
                    try
                    {
                        // Use fallback method to get chunks from this collection
                        var chunks = await FallbackTextSearchForCollectionAsync(collectionName, query, maxResults * 2);

                        // Score chunks based on keyword matches
                        foreach (var chunk in chunks)
                        {
                            var score = CalculateKeywordMatchScore(chunk.Content, keywords);
                            if (score > MinKeywordMatchScore)
                            {
                                chunk.RelevanceScore = score;
                                hybridResults.Add(chunk);
                                _logger.LogDebug("Hybrid match found in {Collection} with score {Score}", collectionName, score);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Hybrid search failed for collection: {Collection}", collectionName);
                    }
                }

                // Sort by keyword match score and return top results
                return hybridResults
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .Take(maxResults)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hybrid search failed");
                return new List<DocumentChunk>();
            }
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Methods

        private static string GetPayloadString(Google.Protobuf.Collections.MapField<string, Qdrant.Client.Grpc.Value> payload, string key)
        {
            if (payload == null) return string.Empty;

            if (!payload.TryGetValue(key, out Qdrant.Client.Grpc.Value value) || value == null)
                return string.Empty;

            switch (value.KindCase)
            {
                case Qdrant.Client.Grpc.Value.KindOneofCase.StringValue:
                    return value.StringValue ?? string.Empty;
                case Qdrant.Client.Grpc.Value.KindOneofCase.DoubleValue:
                    return value.DoubleValue.ToString(CultureInfo.InvariantCulture);
                case Qdrant.Client.Grpc.Value.KindOneofCase.IntegerValue:
                    return value.IntegerValue.ToString(CultureInfo.InvariantCulture);
                case Qdrant.Client.Grpc.Value.KindOneofCase.BoolValue:
                    return value.BoolValue.ToString();
                case Qdrant.Client.Grpc.Value.KindOneofCase.StructValue:
                    return value.StructValue.ToString();
                case Qdrant.Client.Grpc.Value.KindOneofCase.ListValue:
                    return string.Join(",", value.ListValue.Values.Select(v => v.ToString()));
                default:
                    return value.ToString();
            }
        }

        private async Task<List<DocumentChunk>> FallbackTextSearchForCollectionAsync(string collectionName, string query, int maxResults)
        {
            try
            {
                var queryLower = query.ToLowerInvariant();
                var relevantChunks = new List<DocumentChunk>();

                // Get all points from collection
                var scrollResult = await _client.ScrollAsync(collectionName, limit: 1000);

                foreach (var point in scrollResult.Result)
                {
                    var payload = point.Payload;

                    if (payload != null)
                    {
                        var content = GetPayloadString(payload, "content");
                        var docId = GetPayloadString(payload, "documentId");
                        var chunkIndex = GetPayloadString(payload, "chunkIndex");

                        if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(docId) && !string.IsNullOrEmpty(chunkIndex))
                        {
                            var contentStr = content.ToLowerInvariant();

                            // Simple text matching
                            if (contentStr.Contains(queryLower))
                            {
                                var chunk = new DocumentChunk
                                {
                                    Id = Guid.NewGuid(),
                                    DocumentId = Guid.Parse(docId),
                                    Content = content,
                                    ChunkIndex = int.Parse(chunkIndex, CultureInfo.InvariantCulture),
                                    RelevanceScore = DefaultTextSearchScore
                                };
                                relevantChunks.Add(chunk);

                                if (relevantChunks.Count >= maxResults)
                                    break;
                            }
                        }
                    }
                }

                return relevantChunks.Take(maxResults).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallback search failed for collection: {Collection}", collectionName);
                return new List<DocumentChunk>();
            }
        }

        /// <summary>
        /// Extract important keywords from query (names, technical terms, etc.)
        /// </summary>
        private static List<string> ExtractImportantKeywords(string query)
        {
            var keywords = new List<string>();
            var words = query.ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                // Skip very short words only - no domain-specific stop words
                if (word.Length > MinWordLength)
                {
                    keywords.Add(word);
                }
            }

            return keywords;
        }

        /// <summary>
        /// Calculate keyword match score for a chunk
        /// </summary>
        private static double CalculateKeywordMatchScore(string content, List<string> keywords)
        {
            if (keywords.Count == 0) return 0.0;

            var contentLower = content.ToLowerInvariant();
            var matches = 0;
            var totalScore = 0.0;

            foreach (var keyword in keywords)
            {
                if (contentLower.Contains(keyword))
                {
                    matches++;
                    totalScore += 1.0;
                }
            }

            // Normalize score by number of keywords
            return totalScore / keywords.Count;
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                _client?.Dispose();
                _isDisposed = true;
            }
        }

        #endregion
    }
}
