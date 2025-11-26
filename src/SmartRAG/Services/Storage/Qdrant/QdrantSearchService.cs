using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Storage.Qdrant;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services.Storage.Qdrant
{
    /// <summary>
    /// Service for performing searches in Qdrant vector database
    /// </summary>
    public class QdrantSearchService : IQdrantSearchService, IDisposable
    {
        #region Constants

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
        /// [Document Query] Performs vector search across all document collections
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

                var documentCollections = collections.Where(c => c.StartsWith(_collectionName + "_doc_", StringComparison.OrdinalIgnoreCase)).ToList();

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
                                        RelevanceScore = result.Score,
                                        StartPosition = 0,  // Qdrant doesn't store positions, content is already extracted
                                        EndPosition = content.Length
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
        /// [Document Query] Performs fallback text search when vector search fails
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

                                    if (contentStr.Contains(queryLower))
                                    {
                                        var chunk = new DocumentChunk
                                        {
                                            Id = Guid.NewGuid(),
                                            DocumentId = Guid.Parse(docId),
                                            Content = content,
                                            ChunkIndex = int.Parse(chunkIndex, CultureInfo.InvariantCulture),
                                            RelevanceScore = DefaultTextSearchScore,
                                            StartPosition = 0,
                                            EndPosition = content.Length
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
        /// [Document Query] Performs hybrid search combining vector and keyword matching
        /// </summary>
        /// <param name="query">Text query to search for</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of relevant document chunks</returns>
        public async Task<List<DocumentChunk>> HybridSearchAsync(string query, int maxResults)
        {
            var hybridResults = new List<DocumentChunk>();

            try
            {
                _logger.LogDebug("Starting native hybrid search for query: {Query}", query);

                var collections = await _client.ListCollectionsAsync();
                var documentCollections = collections.Where(c => c.StartsWith($"{_collectionName}_doc_", StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var collectionName in documentCollections)
                {
                    try
                    {
                        var chunks = await FallbackTextSearchForCollectionAsync(collectionName, query, maxResults * 2);

                        foreach (var chunk in chunks)
            {
                // Use the score calculated during search (based on match count)
                // We add a base boost because these are text matches
                chunk.RelevanceScore = chunk.RelevanceScore.HasValue ? chunk.RelevanceScore.Value + 4.0 : 5.0;
                hybridResults.Add(chunk);
                _logger.LogDebug("Native text match found in chunk {ChunkIndex} with score {Score}", chunk.ChunkIndex, chunk.RelevanceScore);
            }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Hybrid search failed for collection: {Collection}", collectionName);
                    }
                }

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

        private static string GetPayloadString(Google.Protobuf.Collections.MapField<string, global::Qdrant.Client.Grpc.Value> payload, string key)
        {
            if (payload == null) return string.Empty;

            if (!payload.TryGetValue(key, out global::Qdrant.Client.Grpc.Value value) || value == null)
                return string.Empty;

            string result;
            switch (value.KindCase)
            {
                case global::Qdrant.Client.Grpc.Value.KindOneofCase.StringValue:
                    result = value.StringValue ?? string.Empty;
                    // CRITICAL: Ensure UTF-8 encoding is preserved for all languages (Turkish, German, Russian, etc.)
                    // Protobuf strings are UTF-8 by default, but normalize to ensure consistency
                    if (!string.IsNullOrEmpty(result))
                    {
                        // Normalize Unicode characters to ensure proper encoding (handles Turkish ğ, ü, ş, etc.)
                        result = result.Normalize(System.Text.NormalizationForm.FormC);
                    }
                    return result;
                case global::Qdrant.Client.Grpc.Value.KindOneofCase.DoubleValue:
                    return value.DoubleValue.ToString(CultureInfo.InvariantCulture);
                case global::Qdrant.Client.Grpc.Value.KindOneofCase.IntegerValue:
                    return value.IntegerValue.ToString(CultureInfo.InvariantCulture);
                case global::Qdrant.Client.Grpc.Value.KindOneofCase.BoolValue:
                    return value.BoolValue.ToString();
                case global::Qdrant.Client.Grpc.Value.KindOneofCase.StructValue:
                    return value.StructValue.ToString();
                case global::Qdrant.Client.Grpc.Value.KindOneofCase.ListValue:
                    return string.Join(",", value.ListValue.Values.Select(v => v.ToString()));
                default:
                    return value.ToString();
            }
        }

        private async Task<List<DocumentChunk>> FallbackTextSearchForCollectionAsync(string collectionName, string query, int maxResults)
        {
            try
            {
                var relevantChunks = new List<DocumentChunk>();

                // NATIVE QDRANT TEXT SEARCH (Token-Based OR)
                // We split the query into tokens and use a 'Should' (OR) filter.
                // This allows finding documents that contain ANY of the significant words.
                // We filter out very short words (< 3 chars) to avoid noise (natural stopword filtering).
                
                var tokens = query.Split(new[] { ' ', '.', ',', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Where(t => t.Length >= 3) // Natural stopword filter (skips 've', 'bu', 'ne', etc.)
                                  .ToList();

                if (tokens.Count == 0)
                {
                    return relevantChunks;
                }

                var shouldConditions = tokens.Select(token => new global::Qdrant.Client.Grpc.Condition
                {
                    Field = new global::Qdrant.Client.Grpc.FieldCondition
                    {
                        Key = "content",
                        Match = new global::Qdrant.Client.Grpc.Match { Text = token }
                    }
                }).ToList();

                var filter = new global::Qdrant.Client.Grpc.Filter
                {
                    Should = { shouldConditions }
                };

                // Use ScrollAsync with the filter
                // We fetch more than maxResults to allow for client-side re-ranking if needed,
                // but for now we trust Qdrant to return matching documents.
                var scrollResult = await _client.ScrollAsync(collectionName, filter: filter, limit: (uint)maxResults * 2);

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
                            // Simple Re-ranking: Count how many tokens appear in the content
                            // This helps prioritize chunks that contain "SRS" AND "nedir" over just "nedir"
                            var matchCount = tokens.Count(t => content.Contains(t, StringComparison.OrdinalIgnoreCase));
                            
                            var chunk = new DocumentChunk
                            {
                                Id = Guid.NewGuid(),
                                DocumentId = Guid.Parse(docId),
                                Content = content,
                                ChunkIndex = int.Parse(chunkIndex, CultureInfo.InvariantCulture),
                                RelevanceScore = 1.0 + (matchCount * 0.1), // Base score + boost for multiple matches
                                StartPosition = 0,
                                EndPosition = content.Length
                            };
                            relevantChunks.Add(chunk);

                            // DEBUG: Log content of top matches to verify data integrity
                            if (relevantChunks.Count <= 3)
                            {
                                _logger.LogDebug("Native Search Chunk {Index} Content Preview: {Content}", chunk.ChunkIndex, content.Substring(0, Math.Min(content.Length, 200)));
                            }
                        }
                    }
                }

                // Sort by score descending and take maxResults
                return relevantChunks.OrderByDescending(c => c.RelevanceScore).Take(maxResults).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Native text search failed for collection: {Collection}", collectionName);
                return new List<DocumentChunk>();
            }
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
