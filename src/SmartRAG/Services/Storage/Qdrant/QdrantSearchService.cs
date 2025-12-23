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
        private const double DefaultTextSearchScore = 0.5;
        private const double BaseRelevanceScore = 1.0;
        private const double TokenMatchScoreMultiplier = 0.1;
        private const double AllTokensMatchBoost = 2.0;
        private const double ExactPhraseMatchBoost = 1.5;
        private const double AllTokensPresentBoost = 1.0;
        private const double HybridSearchBaseScore = 4.0;
        private const double HybridSearchDefaultScore = 5.0;

        private readonly QdrantClient _client;
        private readonly QdrantConfig _config;
        private readonly string _collectionName;
        private readonly ILogger<QdrantSearchService> _logger;
        private readonly IQdrantEmbeddingService _embeddingService;
        private bool _isDisposed;

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

                // Calculate per-collection limit: distribute maxResults across collections
                // If single collection, use maxResults directly; otherwise divide evenly
                var collectionCount = Math.Max(1, documentCollections.Count);
                var perCollectionLimit = collectionCount == 1 
                    ? maxResults 
                    : Math.Max(10, (int)Math.Ceiling((double)maxResults / collectionCount));

                foreach (var collectionName in documentCollections)
                {
                    try
                    {
                        var searchResults = await _client.SearchAsync(
                            collectionName: collectionName,
                            vector: queryEmbedding.ToArray(),
                            limit: (ulong)Math.Max(20, perCollectionLimit)
                        );

                        foreach (var result in searchResults)
                        {
                            var payload = result.Payload;

                            if (payload != null)
                            {
                                var content = GetPayloadString(payload, "content");
                                var docId = GetPayloadString(payload, "documentId");
                                var chunkIndex = GetPayloadString(payload, "chunkIndex");
                                var documentType = GetPayloadString(payload, "documentType");
                                var chunkIdStr = GetPayloadString(payload, "chunkId");
                                var fileName = GetPayloadString(payload, "fileName");

                                if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(docId) && !string.IsNullOrEmpty(chunkIndex))
                                {
                                    if (string.IsNullOrWhiteSpace(documentType))
                                        documentType = "Document";

                                    // Use chunkId from payload if available to ensure consistency with GetByIdAsync
                                    Guid chunkId;
                                    if (!string.IsNullOrWhiteSpace(chunkIdStr) && Guid.TryParse(chunkIdStr, out var parsedChunkId))
                                    {
                                        chunkId = parsedChunkId;
                                    }
                                    else
                                    {
                                        chunkId = Guid.NewGuid();
                                    }

                                    var chunk = new DocumentChunk
                                    {
                                        Id = chunkId, // Use original chunk ID from payload
                                        DocumentId = Guid.Parse(docId),
                                        FileName = fileName ?? string.Empty,
                                        Content = content,
                                        ChunkIndex = int.Parse(chunkIndex, CultureInfo.InvariantCulture),
                                        RelevanceScore = result.Score,
                                        StartPosition = 0,
                                        EndPosition = content.Length,
                                        DocumentType = documentType
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
                                var documentType = GetPayloadString(payload, "documentType");
                                var chunkIdStr = GetPayloadString(payload, "chunkId");
                                var fileName = GetPayloadString(payload, "fileName");

                                if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(docId) && !string.IsNullOrEmpty(chunkIndex))
                                {
                                    if (string.IsNullOrWhiteSpace(documentType))
                                        documentType = "Document";

                                    var contentStr = content.ToLowerInvariant();

                                    if (contentStr.Contains(queryLower))
                                    {
                                        // Use chunkId from payload if available to ensure consistency
                                        Guid chunkId;
                                        if (!string.IsNullOrWhiteSpace(chunkIdStr) && Guid.TryParse(chunkIdStr, out var parsedChunkId))
                                        {
                                            chunkId = parsedChunkId;
                                        }
                                        else
                                        {
                                            chunkId = Guid.NewGuid();
                                        }

                                        var chunk = new DocumentChunk
                                        {
                                            Id = chunkId, // Use original chunk ID from payload
                                            DocumentId = Guid.Parse(docId),
                                            FileName = fileName ?? string.Empty,
                                            Content = content,
                                            ChunkIndex = int.Parse(chunkIndex, CultureInfo.InvariantCulture),
                                            RelevanceScore = DefaultTextSearchScore,
                                            StartPosition = 0,
                                            EndPosition = content.Length,
                                            DocumentType = documentType
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
                var collections = await _client.ListCollectionsAsync();
                var documentCollections = collections.Where(c => c.StartsWith($"{_collectionName}_doc_", StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var collectionName in documentCollections)
                {
                    try
                    {
                        var chunks = await FallbackTextSearchForCollectionAsync(collectionName, query, maxResults * 2);

                        var queryLower = query.ToLowerInvariant();
                        var queryTokens = query.Split(new[] { ' ', '.', ',', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Where(t => t.Length >= 3)
                                              .ToList();

                        foreach (var chunk in chunks)
                        {
                            var baseScore = chunk.RelevanceScore.HasValue ? chunk.RelevanceScore.Value + HybridSearchBaseScore : HybridSearchDefaultScore;
                            var contentLower = chunk.Content.ToLowerInvariant();

                            if (contentLower.Contains(queryLower))
                            {
                                baseScore += ExactPhraseMatchBoost;
                            }

                            if (queryTokens.Count > 1 && queryTokens.All(t => contentLower.Contains(t, StringComparison.OrdinalIgnoreCase)))
                            {
                                baseScore += AllTokensPresentBoost;
                            }

                            chunk.RelevanceScore = baseScore;
                            hybridResults.Add(chunk);
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
                    if (!string.IsNullOrEmpty(result))
                    {
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

                var tokens = query.Split(new[] { ' ', '.', ',', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Where(t => t.Length >= 3)
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

                var scrollResult = await _client.ScrollAsync(collectionName, filter: filter, limit: (uint)maxResults * 2);

                if (scrollResult.Result.Count == 0)
                {
                    scrollResult = await _client.ScrollAsync(collectionName, limit: (uint)maxResults * 10);
                }

                var queryLower = query.ToLowerInvariant();
                var normalizedQuery = NormalizeQueryForFuzzyMatching(queryLower);

                foreach (var point in scrollResult.Result)
                {
                    var payload = point.Payload;

                    if (payload != null)
                    {
                        var content = GetPayloadString(payload, "content");
                        var docId = GetPayloadString(payload, "documentId");
                        var chunkIndex = GetPayloadString(payload, "chunkIndex");
                        var documentType = GetPayloadString(payload, "documentType");
                        var chunkIdStr = GetPayloadString(payload, "chunkId");
                        var fileName = GetPayloadString(payload, "fileName");

                        if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(docId) && !string.IsNullOrEmpty(chunkIndex))
                        {
                            if (string.IsNullOrWhiteSpace(documentType))
                                documentType = "Document";

                            var contentLower = content.ToLowerInvariant();
                            var normalizedContent = NormalizeQueryForFuzzyMatching(contentLower);

                            var matchCount = tokens.Count(t => contentLower.Contains(t, StringComparison.OrdinalIgnoreCase));

                            var allTokensMatch = tokens.All(t => contentLower.Contains(t, StringComparison.OrdinalIgnoreCase));
                            var baseScore = BaseRelevanceScore + (matchCount * TokenMatchScoreMultiplier);

                            if (allTokensMatch && tokens.Count > 1)
                            {
                                baseScore += AllTokensMatchBoost;
                            }

                            if (contentLower.Contains(queryLower))
                            {
                                baseScore += ExactPhraseMatchBoost;
                            }

                            if (normalizedContent.Contains(normalizedQuery) && !contentLower.Contains(queryLower))
                            {
                                baseScore += ExactPhraseMatchBoost * 0.7;
                            }

                            // Use chunkId from payload if available to ensure consistency
                            Guid chunkId;
                            if (!string.IsNullOrWhiteSpace(chunkIdStr) && Guid.TryParse(chunkIdStr, out var parsedChunkId))
                            {
                                chunkId = parsedChunkId;
                            }
                            else
                            {
                                chunkId = Guid.NewGuid();
                            }

                            var chunk = new DocumentChunk
                            {
                                Id = chunkId, // Use original chunk ID from payload
                                DocumentId = Guid.Parse(docId),
                                FileName = fileName ?? string.Empty,
                                Content = content,
                                ChunkIndex = int.Parse(chunkIndex, CultureInfo.InvariantCulture),
                                RelevanceScore = baseScore,
                                StartPosition = 0,
                                EndPosition = content.Length,
                                DocumentType = documentType
                            };
                            relevantChunks.Add(chunk);
                        }
                    }
                }

                return relevantChunks.OrderByDescending(c => c.RelevanceScore).Take(maxResults).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Native text search failed for collection: {Collection}", collectionName);
                return new List<DocumentChunk>();
            }
        }



        private static string NormalizeQueryForFuzzyMatching(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);

            var sb = new System.Text.StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == System.Globalization.UnicodeCategory.NonSpacingMark)
                    continue;

                var lower = char.ToLowerInvariant(c);
                sb.Append(lower);
            }

            return sb.ToString();
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                _client?.Dispose();
                _isDisposed = true;
            }
        }
    }
}
