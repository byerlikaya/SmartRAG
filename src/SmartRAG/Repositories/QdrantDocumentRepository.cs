using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System.Globalization;


namespace SmartRAG.Repositories;

/// <summary>
/// Qdrant vector database document repository implementation
/// </summary>
public class QdrantDocumentRepository : IDocumentRepository, IDisposable
{
    #region Constants

    // Search and batch constants
    private const int DefaultMaxSearchResults = 5;
    private const int DefaultBatchSize = 200;
    private const int DefaultScrollBatchSize = 25;
    private const int DefaultMaxBatches = 50;
    private const int DefaultMaxDocuments = 1000;
    private const int DefaultMaxResultsMultiplier = 4;
    private const int DefaultPerDocumentTopK = 3;
    private const int DefaultGlobalResultsMultiplier = 3;

    // Cache constants
    private const int CacheExpiryMinutes = 5;
    private const int DefaultScrollLimit = 1000;

    // Vector dimension constants
    private const int DefaultVectorDimension = 768;
    private const int OpenAIVectorDimension = 1536;
    private const int OpenAILargeVectorDimension = 3072;
    private const int SentenceTransformersDimension = 384;
    private const int MPNetDimension = 768;

    // Timeout constants
    private const int DefaultGrpcTimeoutMinutes = 5;
    private const int DefaultDelayMs = 100;

    // Scoring constants
    private const double DefaultTextSearchScore = 0.5;
    private const double MinKeywordMatchScore = 0.1;
    private const int MinWordLength = 2;

    #endregion

    #region Fields

    private readonly QdrantClient _client;
    private readonly QdrantConfig _config;
    private readonly string _collectionName;
    private readonly ILogger<QdrantDocumentRepository> _logger;
    private static readonly SemaphoreSlim _collectionInitLock = new(1, 1);
    private bool _collectionReady;

    #endregion

    #region Properties

    protected ILogger Logger => _logger;

    #endregion

    #region Constructor

    public QdrantDocumentRepository(IOptions<QdrantConfig> config, ILogger<QdrantDocumentRepository> logger)
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
            grpcTimeout: TimeSpan.FromMinutes(DefaultGrpcTimeoutMinutes)
        );


        Task.Run(async () =>
            {
                try
                {
                    await InitializeCollectionAsync();
                }
                catch (Exception ex)
                {
                    RepositoryLogMessages.LogQdrantCollectionInitFailed(Logger, ex);
                }
            });
    }

    #endregion

    #region Public Methods

    private async Task InitializeCollectionAsync()
    {
        if (_collectionReady)
            return;

        await _collectionInitLock.WaitAsync();

        try
        {
            if (_collectionReady)
                return;

            var collections = await _client.ListCollectionsAsync();

            if (!collections.Contains(_collectionName))
            {
                await CreateCollectionAsync();
            }

            _collectionReady = true;
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            _collectionInitLock.Release();
        }
    }

    private async Task EnsureCollectionExistsAsync()
    {
        if (_collectionReady)
            return;

        await InitializeCollectionAsync();
    }

    private async Task CreateCollectionAsync()
    {
        try
        {
            // Get vector dimension dynamically
            var vectorDimension = await GetVectorDimensionAsync();

            var vectorParams = new VectorParams
            {
                Size = (ulong)vectorDimension,
                Distance = GetDistanceMetric(_config.DistanceMetric)
            };

            RepositoryLogMessages.LogQdrantCollectionCreated(Logger, _collectionName, vectorDimension, null);

            // Create collection with proper configuration - use correct Qdrant API
            await _client.CreateCollectionAsync(_collectionName, vectorParams);
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogQdrantCollectionCreationFailed(Logger, _collectionName, ex);
            throw;
        }
    }

    private async Task EnsureDocumentCollectionExistsAsync(string collectionName, SmartRAG.Entities.Document document)
    {
        try
        {
            // Check if collection already exists (fast check)
            if (_collectionReady)
            {
                var collections = await _client.ListCollectionsAsync();
                if (collections.Contains(collectionName))
                    return;
            }

            // Get vector dimension dynamically
            var vectorDimension = await GetVectorDimensionAsync();

            // Create collection with detected dimension
            var vectorParams = new VectorParams
            {
                Size = (ulong)vectorDimension,
                Distance = GetDistanceMetric(_config.DistanceMetric)
            };

            RepositoryLogMessages.LogQdrantCollectionCreated(Logger, collectionName, vectorDimension, null);

            // Create collection with proper configuration - use correct Qdrant API
            await _client.CreateCollectionAsync(collectionName, vectorParams);
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogQdrantCollectionCreationFailed(Logger, collectionName, ex);
            throw;
        }
    }

    private static Distance GetDistanceMetric(string metric)
        => metric.ToLower(CultureInfo.InvariantCulture) switch
        {
            "cosine" => Distance.Cosine,
            "dot" => Distance.Dot,
            "euclidean" => Distance.Euclid,
            _ => Distance.Cosine
        };

    private static string GetPayloadString(Google.Protobuf.Collections.MapField<string, Value> payload, string key)
    {
        if (payload == null) return string.Empty;

        if (!payload.TryGetValue(key, out Value value) || value == null)
            return string.Empty;

        return value.KindCase switch
        {
            Value.KindOneofCase.StringValue => value.StringValue ?? string.Empty,
            Value.KindOneofCase.DoubleValue => value.DoubleValue.ToString(CultureInfo.InvariantCulture),
            Value.KindOneofCase.IntegerValue => value.IntegerValue.ToString(CultureInfo.InvariantCulture),
            Value.KindOneofCase.BoolValue => value.BoolValue.ToString(),
            Value.KindOneofCase.StructValue => value.StructValue.ToString(),
            Value.KindOneofCase.ListValue => string.Join(",", value.ListValue.Values.Select(v => v.ToString())),
            _ => value.ToString(),
        };
    }

    /// <summary>
    /// Extract document metadata from Qdrant point payload
    /// </summary>
    private static DocumentMetadata ExtractDocumentMetadata(Google.Protobuf.Collections.MapField<string, Value> payload)
    {
        var fileName = GetPayloadString(payload, "fileName");
        var contentType = GetPayloadString(payload, "contentType");
        var fileSizeStr = GetPayloadString(payload, "fileSize");
        var uploadedAtStr = GetPayloadString(payload, "uploadedAt");
        var uploadedBy = GetPayloadString(payload, "uploadedBy");
        var content = GetPayloadString(payload, "content");
        var idStr = GetPayloadString(payload, "id");

        long.TryParse(fileSizeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out long fileSize);

        if (!DateTime.TryParse(uploadedAtStr, null, DateTimeStyles.RoundtripKind, out DateTime uploadedAt))
            uploadedAt = DateTime.UtcNow;

        if (!Guid.TryParse(idStr, out var docGuid))
            docGuid = Guid.Empty;

        return new DocumentMetadata
        {
            Id = docGuid,
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            UploadedAt = uploadedAt,
            UploadedBy = uploadedBy,
            Content = content
        };
    }

    /// <summary>
    /// Create Document from metadata
    /// </summary>
    private static SmartRAG.Entities.Document CreateDocumentFromMetadata(DocumentMetadata metadata)
        => new SmartRAG.Entities.Document()
        {
            Id = metadata.Id,
            FileName = metadata.FileName,
            ContentType = metadata.ContentType,
            FileSize = metadata.FileSize,
            UploadedAt = metadata.UploadedAt,
            UploadedBy = metadata.UploadedBy,
            Content = metadata.Content,
            Chunks = []
        };

    /// <summary>
    /// Create DocumentChunk from Qdrant point
    /// </summary>
    private static DocumentChunk CreateDocumentChunk(RetrievedPoint point, Guid documentId, DateTime fallbackCreatedAt)
    {
        var chunkContent = GetPayloadString(point.Payload, "content");
        var chunkUploadedAtStr = GetPayloadString(point.Payload, "uploadedAt");

        if (!DateTime.TryParse(chunkUploadedAtStr, null, DateTimeStyles.RoundtripKind, out DateTime chunkCreatedAt))
            chunkCreatedAt = fallbackCreatedAt;

        return new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Content = chunkContent,
            Embedding = point.Vectors?.Vector?.Data?.ToList() ?? [],
            CreatedAt = chunkCreatedAt
        };
    }

    /// <summary>
    /// Helper class for document metadata extraction
    /// </summary>
    private sealed class DocumentMetadata
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public async Task<SmartRAG.Entities.Document> AddAsync(SmartRAG.Entities.Document document)
    {
        try
        {
            await EnsureCollectionExistsAsync();

            // Create unique collection name for each document - Qdrant naming rules
            var documentCollectionName = $"{_collectionName}_doc_{document.Id:N}".Replace("-", ""); // Remove hyphens for Qdrant
            RepositoryLogMessages.LogQdrantDocumentCollectionCreating(Logger, documentCollectionName, _collectionName, document.Id, null);

            await EnsureDocumentCollectionExistsAsync(documentCollectionName, document);

            // Generate embeddings for all chunks in parallel with progress tracking
            RepositoryLogMessages.LogQdrantEmbeddingsGenerationStarted(Logger, document.Chunks.Count, null);
            var embeddingTasks = document.Chunks.Select(async (chunk, index) =>
            {
                if (chunk.Embedding == null || chunk.Embedding.Count == 0)
                {
                    chunk.Embedding = await GenerateEmbeddingAsync(chunk.Content) ?? new List<float>();
                    if (index % 10 == 0) // Progress every 10 chunks
                    {
                        RepositoryLogMessages.LogQdrantEmbeddingsProgress(Logger, index + 1, document.Chunks.Count, null);
                    }
                }
                return chunk;
            }).ToList();

            await Task.WhenAll(embeddingTasks);
            RepositoryLogMessages.LogQdrantEmbeddingsGenerationCompleted(Logger, document.Chunks.Count, null);

            // Batch process all chunks with larger batch size
            var allPoints = new List<PointStruct>();

            RepositoryLogMessages.LogQdrantPointsCreationStarted(Logger, document.Chunks.Count, null);
            foreach (var chunk in document.Chunks)
            {
                var point = new PointStruct
                {
                    Id = new PointId { Uuid = Guid.NewGuid().ToString() }, // Use UUID string instead of Num
                    Vectors = new Vectors
                    {
                        Vector = new Vector
                        {
                            Data = { chunk.Embedding }
                        }
                    }
                };

                // Add chunk-specific payload with consistent field names
                point.Payload.Add("chunkId", chunk.Id.ToString());
                point.Payload.Add("chunkIndex", chunk.ChunkIndex);
                point.Payload.Add("content", chunk.Content);
                point.Payload.Add("documentId", document.Id.ToString());
                point.Payload.Add("fileName", document.FileName);
                point.Payload.Add("contentType", document.ContentType);
                point.Payload.Add("fileSize", document.FileSize);
                point.Payload.Add("uploadedAt", document.UploadedAt.ToString("O"));
                point.Payload.Add("uploadedBy", document.UploadedBy);

                allPoints.Add(point);
            }

            // Process in batches for better performance
            for (int i = 0; i < allPoints.Count; i += DefaultBatchSize)
            {
                var batch = allPoints.Skip(i).Take(DefaultBatchSize).ToList();
                await _client.UpsertAsync(documentCollectionName, batch);
                var batchNumber = i / DefaultBatchSize + 1;
                var totalBatches = (allPoints.Count + DefaultBatchSize - 1) / DefaultBatchSize;
                RepositoryLogMessages.LogQdrantBatchUploadProgress(Logger, batchNumber, totalBatches, batch.Count, null);
            }

            RepositoryLogMessages.LogQdrantDocumentUploadSuccess(Logger, document.FileName, documentCollectionName, null);
            return document;
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogQdrantDocumentUploadFailed(Logger, document.FileName, ex);
            throw;
        }
    }

    public async Task<SmartRAG.Entities.Document?> GetByIdAsync(Guid id)
    {
        try
        {
            var filter = new Filter
            {
                Must = {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "id",
                            Match = new Match
                            {
                                Keyword = id.ToString()
                            }
                        }
                    }
                }
            };

            var result = await _client.ScrollAsync(_collectionName, filter, limit: DefaultMaxSearchResults * 10);

            if (result.Result.Count == 0)
                return null;

            var firstPoint = result.Result.First();
            var metadata = ExtractDocumentMetadata(firstPoint.Payload);

            if (metadata.Id == Guid.Empty)
            {
                return null;
            }

            var document = CreateDocumentFromMetadata(metadata);

            foreach (var point in result.Result)
            {
                var chunk = CreateDocumentChunk(point, document.Id, metadata.UploadedAt);
                document.Chunks.Add(chunk);
            }

            return document;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<List<SmartRAG.Entities.Document>> GetAllAsync()
    {
        try
        {
            var documents = new List<SmartRAG.Entities.Document>();
            var processedIds = new HashSet<string>();
            var lastPointId = (PointId?)null;
            var batchCount = 0;

            while (batchCount < DefaultMaxBatches)
            {
                try
                {
                    var result = await _client.ScrollAsync(
                        _collectionName,
                        limit: DefaultScrollBatchSize,
                        offset: lastPointId);

                    if (result.Result.Count == 0)
                        break;

                    var documentGroups = result.Result.GroupBy(p => GetPayloadString(p.Payload, "id"));

                    foreach (var group in documentGroups)
                    {
                        var documentId = group.Key;

                        if (processedIds.Contains(documentId))
                            continue;

                        processedIds.Add(documentId);

                        var firstPoint = group.First();
                        var metadata = ExtractDocumentMetadata(firstPoint.Payload);

                        if (metadata.Id == Guid.Empty)
                        {
                            continue;
                        }

                        var document = CreateDocumentFromMetadata(metadata);

                        foreach (var point in group)
                        {
                            var chunk = CreateDocumentChunk(point, document.Id, metadata.UploadedAt);
                            document.Chunks.Add(chunk);
                        }

                        documents.Add(document);
                    }


                    lastPointId = result.Result.Last().Id;

                    if (documents.Count > DefaultMaxDocuments)
                    {
                        break;
                    }

                    batchCount++;

                    await Task.Delay(DefaultDelayMs);
                }
                catch (Exception ex) when (ex.Message.Contains("ResourceExhausted") || ex.Message.Contains("message size"))
                {
                    batchCount++;
                    continue;
                }
                catch (Exception)
                {

                    break;
                }
            }

            return documents;
        }
        catch (Exception)
        {
            return new List<SmartRAG.Entities.Document>();
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var filter = new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "id",
                            Match = new Match
                            {
                                Keyword = id.ToString()
                            }
                        }
                    }
                }
            };

            await _client.DeleteAsync(_collectionName, filter);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<int> GetCountAsync()
    {
        try
        {
            var collectionInfo = await _client.GetCollectionInfoAsync(_collectionName);
            return (int)collectionInfo.VectorsCount;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    // Cache for duplicate request prevention
    private static readonly Dictionary<string, (List<DocumentChunk> Chunks, DateTime Expiry)> _searchCache = new();
    private static readonly object _cacheLock = new object();

    public async Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = DefaultMaxSearchResults)
    {
        try
        {
            // Check cache for duplicate requests
            var queryHash = $"{query}_{maxResults}";
            lock (_cacheLock)
            {
                            if (_searchCache.TryGetValue(queryHash, out var cached) && cached.Expiry > DateTime.UtcNow)
            {
                return cached.Chunks.ToList(); // Return copy to avoid modification
            }
            }

            await EnsureCollectionExistsAsync();

            // Log that we're processing a new search
            RepositoryLogMessages.LogQdrantSearchStarted(Logger, query, null);

            // Enable combined vector + keyword-assisted (hybrid) search

            // FALLBACK: Generate embedding for semantic search
            var queryEmbedding = await GenerateEmbeddingAsync(query);
            if (queryEmbedding == null || queryEmbedding.Count == 0)
            {
                // Fallback to text search if embedding fails
                return await FallbackTextSearchAsync(query, maxResults);
            }

            // Search in all document collections
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
                        limit: (ulong)Math.Max(20, maxResults * DefaultMaxResultsMultiplier)
                    );

                    RepositoryLogMessages.LogQdrantSearchResultsFound(Logger, collectionName, searchResults.Count, null);

                    // Convert to DocumentChunk objects
                    foreach (var result in searchResults)
                    {
                        var payload = result.Payload;

                        if (payload != null)
                        {
                            // Use GetPayloadString helper method for consistent parsing
                            var content = GetPayloadString(payload, "content");
                            var docId = GetPayloadString(payload, "documentId");
                            var chunkIndex = GetPayloadString(payload, "chunkIndex");

                            if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(docId) && !string.IsNullOrEmpty(chunkIndex))
                            {
                                var chunk = new DocumentChunk
                                {
                                    Id = Guid.NewGuid(), // Generate new ID since we can't parse PointId
                                    DocumentId = Guid.Parse(docId),
                                    Content = content,
                                    ChunkIndex = int.Parse(chunkIndex, CultureInfo.InvariantCulture),
                                    RelevanceScore = result.Score // Score is already float
                                };
                                allChunks.Add(chunk);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other collections
                    RepositoryLogMessages.LogQdrantSearchFailed(Logger, collectionName, ex);

                    // If vector search fails, try fallback immediately for this collection
                    try
                    {
                        RepositoryLogMessages.LogQdrantFallbackSearchStarted(Logger, collectionName, null);
                        var fallbackChunks = await FallbackTextSearchForCollectionAsync(collectionName, query, maxResults);
                        allChunks.AddRange(fallbackChunks);
                    }
                    catch (Exception fallbackEx)
                    {
                        RepositoryLogMessages.LogQdrantFallbackSearchFailed(Logger, collectionName, fallbackEx);
                    }
                }
            }

            // Also gather keyword-based matches via hybrid path
            try
            {
                var hybridResults = await HybridSearchAsync(query, maxResults * DefaultMaxResultsMultiplier);
                allChunks.AddRange(hybridResults);
            }
            catch (Exception ex)
            {
                RepositoryLogMessages.LogQdrantHybridSearchFailed(Logger, "global", ex);
            }

            // Deduplicate by (DocumentId, ChunkIndex)
            var deduped = allChunks
                .GroupBy(c => new { c.DocumentId, c.ChunkIndex })
                .Select(g => g.OrderByDescending(c => c.RelevanceScore ?? 0.0).First())
                .ToList();

            // Ensure we don't lose underrepresented documents before higher-level diversity
            RepositoryLogMessages.LogQdrantTotalChunksFound(Logger, deduped.Count, null);
            // Take top K per document to improve coverage of key fields
            var perDocTopK = Math.Max(1, Math.Min(DefaultPerDocumentTopK, maxResults));
            var topPerDocument = deduped
                .GroupBy(c => c.DocumentId)
                .SelectMany(g => g.OrderByDescending(c => c.RelevanceScore ?? 0.0).Take(perDocTopK))
                .ToList();

            var remainingSlots = Math.Max(0, (maxResults * DefaultGlobalResultsMultiplier) - topPerDocument.Count);
            var topGlobal = deduped
                .Except(topPerDocument)
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .Take(remainingSlots)
                .ToList();

            var finalResults = topPerDocument
                .Concat(topGlobal)
                .Distinct() // same references from allChunks; removes duplicates
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .ToList();

            RepositoryLogMessages.LogQdrantFinalResultsReturned(Logger, finalResults.Count, null);
            RepositoryLogMessages.LogQdrantUniqueDocumentsFound(Logger, finalResults.Select(c => c.DocumentId).Distinct().Count(), null);

            // Cache the results to prevent duplicate processing
            lock (_cacheLock)
            {
                _searchCache[queryHash] = (finalResults.ToList(), DateTime.UtcNow.AddMinutes(CacheExpiryMinutes));

                // Clean up expired cache entries
                var expiredKeys = _searchCache.Where(kvp => kvp.Value.Expiry <= DateTime.UtcNow).Select(kvp => kvp.Key).ToList();
                foreach (var key in expiredKeys)
                {
                    _searchCache.Remove(key);
                }

                RepositoryLogMessages.LogQdrantSearchResultsCached(Logger, query, _searchCache.Count, null);
            }

            return finalResults;
        }
        catch (Exception ex)
        {
            // Log error and fallback to text search
            RepositoryLogMessages.LogQdrantVectorSearchFailed(Logger, ex.Message, null);
            return await FallbackTextSearchAsync(query, maxResults);
        }
    }

    private async Task<List<DocumentChunk>> FallbackTextSearchAsync(string query, int maxResults)
    {
        try
        {
            RepositoryLogMessages.LogQdrantGlobalFallbackStarted(Logger, query, null);
            var queryLower = query.ToLowerInvariant();
            var relevantChunk = new List<DocumentChunk>();

            // Get all collections to search in
            var collections = await _client.ListCollectionsAsync();

            // Look for collections that match our document collection pattern
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
                    var scrollResult = await _client.ScrollAsync(collectionName, limit: DefaultScrollLimit);

                    foreach (var point in scrollResult.Result)
                    {
                        var payload = point.Payload;
                        if (payload != null)
                        {
                            // Use GetPayloadString helper method for consistent parsing
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
                                    relevantChunk.Add(chunk);

                                    if (relevantChunk.Count >= maxResults)
                                        break;
                                }
                            }

                        }
                    }

                    if (relevantChunk.Count >= maxResults)
                        break;
                }
                catch (Exception ex)
                {
                    RepositoryLogMessages.LogQdrantFallbackSearchFailed(Logger, collectionName, ex);
                }
            }

            RepositoryLogMessages.LogQdrantGlobalFallbackResults(Logger, relevantChunk.Count, null);
            return relevantChunk.Take(maxResults).ToList();
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogQdrantGlobalFallbackFailed(Logger, ex);
            return new List<DocumentChunk>();
        }
    }

    private async Task<List<DocumentChunk>> FallbackTextSearchForCollectionAsync(string collectionName, string query, int maxResults)
    {
        try
        {
            RepositoryLogMessages.LogQdrantFallbackSearchStarted(Logger, collectionName, null);
            var queryLower = query.ToLowerInvariant();
            var relevantChunks = new List<DocumentChunk>();

            // Get all points from collection
            var scrollResult = await _client.ScrollAsync(collectionName, limit: DefaultScrollLimit);

            foreach (var point in scrollResult.Result)
            {
                var payload = point.Payload;

                if (payload != null)
                {
                    // Use GetPayloadString helper method for consistent parsing
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

            RepositoryLogMessages.LogQdrantFallbackSearchResults(Logger, relevantChunks.Count, null);
            return relevantChunks.Take(maxResults).ToList();
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogQdrantFallbackSearchFailed(Logger, collectionName, ex);
            return new List<DocumentChunk>();
        }
    }

    private async Task<List<float>?> GenerateEmbeddingAsync(string text)
    {
        try
        {
            // TODO: Inject IAIService to generate real embeddings
            // For now, use a faster hash-based approach with dynamic dimension

            // Get vector dimension from config or detect from existing collections
            var vectorDimension = await GetVectorDimensionAsync();

            var hash = text.GetHashCode();
            var random = new Random(hash);
            var embedding = new List<float>(vectorDimension);

            // Generate embedding with correct dimension
            for (int i = 0; i < vectorDimension; i++)
            {
                embedding.Add((float)(random.NextDouble() * 2 - 1)); // -1 to 1
            }

            // Fast normalization using SIMD-friendly approach
            var sumSquares = 0.0f;
            for (int i = 0; i < embedding.Count; i++)
            {
                sumSquares += embedding[i] * embedding[i];
            }

            var magnitude = (float)Math.Sqrt(sumSquares);
            if (magnitude > 0.001f) // Avoid division by very small numbers
            {
                var invMagnitude = 1.0f / magnitude;
                for (int i = 0; i < embedding.Count; i++)
                {
                    embedding[i] *= invMagnitude;
                }
            }

            return embedding;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<int> GetVectorDimensionAsync()
    {
        try
        {
            // First try to get from config
            if (_config.VectorSize > 0)
            {
                return _config.VectorSize;
            }

            // If config doesn't have it, detect from existing collections
            var collections = await _client.ListCollectionsAsync();
            var documentCollections = collections.Where(c => c.StartsWith(_collectionName + "_doc_", StringComparison.OrdinalIgnoreCase)).ToList();

            // If no document collections, check main collection
            if (documentCollections.Count == 0 && collections.Contains(_collectionName))
            {
                documentCollections.Add(_collectionName);
            }

            if (documentCollections.Count > 0)
            {
                // Get dimension from first available collection
                var firstCollection = documentCollections.Count > 0 ? documentCollections.First() : _collectionName;
                var collectionInfo = await _client.GetCollectionInfoAsync(firstCollection);

                // Try to get dimension from collection info
                if (collectionInfo.Config?.Params?.VectorsConfig != null)
                {
                    // Qdrant API structure might be different, try multiple approaches
                    var config = collectionInfo.Config.Params.VectorsConfig;

                    // Try to access size property (might be named differently)
                    var sizeProperty = config.GetType().GetProperty("Size");
                    if (sizeProperty != null)
                    {
                        var sizeValue = sizeProperty.GetValue(config);
                        if (sizeValue is ulong size)
                        {
                            RepositoryLogMessages.LogQdrantVectorDimensionDetected(Logger, (int)size, null);
                            return (int)size;
                        }
                    }
                }
            }

            // Default fallback dimensions based on common models
            var defaultDimensions = new Dictionary<string, int>
            {
                { "text-embedding-ada-002", 1536 },    // OpenAI Ada
                { "text-embedding-3-small", 1536 },    // OpenAI 3 Small
                { "text-embedding-3-large", 3072 },    // OpenAI 3 Large
                { "all-MiniLM-L6-v2", 384 },          // Sentence Transformers
                { "all-mpnet-base-v2", 768 },          // MPNet
                { "multi-qa-MiniLM-L6-cos-v1", 384 }, // MultiQA
                { "paraphrase-multilingual-MiniLM-L12-v2", 384 } // Multilingual
            };

            // Try to guess based on collection name or other hints
            // For now, use the most common dimension
            RepositoryLogMessages.LogQdrantDefaultVectorDimensionUsed(Logger, DefaultVectorDimension, null);
            return DefaultVectorDimension;
        }
        catch (Exception ex)
        {
            RepositoryLogMessages.LogQdrantVectorDimensionDetectionFailed(Logger, ex);
            return DefaultVectorDimension;
        }
    }


    /// <summary>
    /// Hybrid search: combines keyword matching with vector search for better accuracy
    /// </summary>
    private async Task<List<DocumentChunk>> HybridSearchAsync(string query, int maxResults)
    {
        var hybridResults = new List<DocumentChunk>();

        try
        {
            // Extract meaningful keywords from query
            var keywords = ExtractImportantKeywords(query);
            RepositoryLogMessages.LogQdrantHybridSearchStarted(Logger, query, null);

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
                    // Use existing fallback method to get chunks from this collection
                    var chunks = await FallbackTextSearchForCollectionAsync(collectionName, query, maxResults * 2);

                    // Score chunks based on keyword matches
                    foreach (var chunk in chunks)
                    {
                        var score = CalculateKeywordMatchScore(chunk.Content, keywords);
                        if (score > MinKeywordMatchScore)
                        {
                            chunk.RelevanceScore = score;
                            hybridResults.Add(chunk);
                            RepositoryLogMessages.LogQdrantHybridMatchFound(Logger, collectionName, score, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    RepositoryLogMessages.LogQdrantHybridSearchFailed(Logger, collectionName, ex);
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
            RepositoryLogMessages.LogQdrantHybridSearchFailed(Logger, "global", ex);
            return new List<DocumentChunk>();
        }
    }

    /// <summary>
    /// Extract important keywords from query (names, technical terms, etc.)
    /// </summary>
    private static List<string> ExtractImportantKeywords(string query)
    {
        var keywords = new List<string>();
        var words = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

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

                // Give full points for any match (simplify for now)
                totalScore += 1.0;
            }
        }

        // Normalize score by number of keywords
        return totalScore / keywords.Count;
    }



    #endregion

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
