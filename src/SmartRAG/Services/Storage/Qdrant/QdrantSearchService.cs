using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace SmartRAG.Services.Storage.Qdrant;


/// <summary>
/// Service for performing searches in Qdrant vector database
/// </summary>
public class QdrantSearchService : IQdrantSearchService, IDisposable
{
    private const double DefaultTextSearchScore = 70.0;
    private const double TextSearchScorePerMatch = 8.0;

    private readonly QdrantClient _client;
    private readonly QdrantConfig _config;
    private readonly string _collectionName;
    private readonly ILogger<QdrantSearchService> _logger;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the QdrantSearchService
    /// </summary>
    /// <param name="config">Qdrant configuration options</param>
    /// <param name="logger">Logger instance for this service</param>
    public QdrantSearchService(
        IOptions<QdrantConfig> config,
        ILogger<QdrantSearchService> logger)
    {
        _config = config.Value;
        _collectionName = _config.CollectionName;
        _logger = logger;

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
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>List of relevant document chunks</returns>
    public async Task<List<DocumentChunk>> SearchAsync(List<float> queryEmbedding, int maxResults, CancellationToken cancellationToken = default)
    {
        try
        {
            var allChunks = new List<DocumentChunk>();
            var collections = await _client.ListCollectionsAsync(cancellationToken);

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
                : Math.Max(15, (int)Math.Ceiling((double)(maxResults * 2) / collectionCount));

            foreach (var collectionName in documentCollections)
            {
                try
                {
                    var searchResults = await _client.SearchAsync(
                        collectionName: collectionName,
                        vector: queryEmbedding.ToArray(),
                        limit: (ulong)Math.Max(30, perCollectionLimit),
                        cancellationToken: cancellationToken
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
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>List of relevant document chunks</returns>
    public async Task<List<DocumentChunk>> FallbackTextSearchAsync(string query, int maxResults, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryWords = QueryTokenizer.TokenizeQuery(query);
            var queryWordsLower = queryWords.Select(w => w.ToLowerInvariant()).ToList();
            var significantWords = queryWordsLower.Where(w => w.Length >= 4).ToList();
            var searchTerms = significantWords.Count > 0 ? significantWords : queryWordsLower;
            var minMatchCount = Math.Max(1, searchTerms.Count / 4);

            var fileNamePhrases = new List<string>();
            for (int i = 0; i < queryWordsLower.Count - 1; i++)
            {
                var w1 = queryWordsLower[i];
                var w2 = queryWordsLower[i + 1];
                if (w1.Length >= 1 && w2.Length >= 3)
                    fileNamePhrases.Add($"{w1} {w2}");
            }
            fileNamePhrases = fileNamePhrases.Distinct().Take(5).ToList();

            var relevantChunks = new List<DocumentChunk>();

            var collections = await _client.ListCollectionsAsync(cancellationToken);
            var documentCollections = collections.Where(c => c.StartsWith(_collectionName + "_doc_", StringComparison.OrdinalIgnoreCase)).ToList();

            if (documentCollections.Count == 0)
            {
                return new List<DocumentChunk>();
            }

            foreach (var collectionName in documentCollections)
            {
                try
                {
                    PointId? nextOffset = null;
                    do
                    {
                        var scrollResult = await _client.ScrollAsync(collectionName, offset: nextOffset, limit: 1000, cancellationToken: cancellationToken);

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

                                    var searchableNormalized = string.Concat(content, " ", fileName ?? string.Empty).NormalizeForOcrTolerantMatch();
                                    var fileNameLower = (fileName ?? string.Empty).ToLowerInvariant();
                                    var matchCount = searchTerms.Count(term =>
                                    {
                                        var normalizedTerm = term.NormalizeForOcrTolerantMatch();
                                        if (string.IsNullOrEmpty(normalizedTerm))
                                            return false;
                                        if (searchableNormalized.IndexOf(normalizedTerm, StringComparison.Ordinal) >= 0)
                                            return true;
                                        foreach (var variant in normalizedTerm.GetSearchTermVariants(4))
                                        {
                                            if (searchableNormalized.IndexOf(variant, StringComparison.Ordinal) >= 0)
                                                return true;
                                        }
                                        return false;
                                    });
                                    var hasFileNamePhraseMatch = fileNamePhrases.Count > 0 && fileNamePhrases.Any(p => fileNameLower.Contains(p));

                                    if (matchCount >= minMatchCount || hasFileNamePhraseMatch)
                                    {
                                        Guid chunkId;
                                        if (!string.IsNullOrWhiteSpace(chunkIdStr) && Guid.TryParse(chunkIdStr, out var parsedChunkId))
                                        {
                                            chunkId = parsedChunkId;
                                        }
                                        else
                                        {
                                            chunkId = Guid.NewGuid();
                                        }

                                        var score = DefaultTextSearchScore + (matchCount * TextSearchScorePerMatch);
                                        var chunk = new DocumentChunk
                                        {
                                            Id = chunkId,
                                            DocumentId = Guid.Parse(docId),
                                            FileName = fileName ?? string.Empty,
                                            Content = content,
                                            ChunkIndex = int.Parse(chunkIndex, CultureInfo.InvariantCulture),
                                            RelevanceScore = score,
                                            StartPosition = 0,
                                            EndPosition = content.Length,
                                            DocumentType = documentType
                                        };
                                        relevantChunks.Add(chunk);
                                    }
                                }
                            }
                        }
                        
                        nextOffset = scrollResult.NextPageOffset;
                    } while (nextOffset != null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fallback search failed for collection: {Collection}", collectionName);
                }
            }

            return relevantChunks
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .ThenBy(c => c.ChunkIndex)
                .Take(maxResults)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback text search failed");
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

    private void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing)
        {
            _client?.Dispose();
            _isDisposed = true;
        }
    }
}

