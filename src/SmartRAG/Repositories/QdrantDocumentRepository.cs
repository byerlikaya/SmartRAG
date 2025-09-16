using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Repositories
{

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

        // Timeout constants
        private const int DefaultGrpcTimeoutMinutes = 5;

        #endregion

        #region Fields

        private readonly QdrantClient _client;
        private readonly QdrantConfig _config;
        private readonly string _collectionName;
        private readonly ILogger<QdrantDocumentRepository> _logger;
        // Collection management moved to QdrantCollectionManager

        // New injected services
        private readonly IQdrantCollectionManager _collectionManager;
        private readonly IQdrantEmbeddingService _embeddingService;
        private readonly IQdrantCacheManager _cacheManager;
        private readonly IQdrantSearchService _searchService;

        #endregion

        #region Properties

        protected ILogger Logger => _logger;

        #endregion

        #region Constructor

        public QdrantDocumentRepository(
            IOptions<QdrantConfig> config,
            ILogger<QdrantDocumentRepository> logger,
            IQdrantCollectionManager collectionManager,
            IQdrantEmbeddingService embeddingService,
            IQdrantCacheManager cacheManager,
            IQdrantSearchService searchService)
        {
            _config = config.Value;
            _collectionName = _config.CollectionName;
            _logger = logger;
            _collectionManager = collectionManager;
            _embeddingService = embeddingService;
            _cacheManager = cacheManager;
            _searchService = searchService;

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
                        await _collectionManager.EnsureCollectionExistsAsync();
                    }
                    catch (Exception ex)
                    {
                        RepositoryLogMessages.LogQdrantCollectionInitFailed(Logger, ex);
                    }
                });
        }

        #endregion

        #region Public Methods

        // Collection management methods moved to QdrantCollectionManager

        // GetDistanceMetric moved to QdrantCollectionManager

        private static string GetPayloadString(Google.Protobuf.Collections.MapField<string, Value> payload, string key)
        {
            if (payload == null) return string.Empty;

            if (!payload.TryGetValue(key, out Value value) || value == null)
                return string.Empty;

            switch (value.KindCase)
            {
                case Value.KindOneofCase.StringValue:
                    return value.StringValue ?? string.Empty;
                case Value.KindOneofCase.DoubleValue:
                    return value.DoubleValue.ToString(CultureInfo.InvariantCulture);
                case Value.KindOneofCase.IntegerValue:
                    return value.IntegerValue.ToString(CultureInfo.InvariantCulture);
                case Value.KindOneofCase.BoolValue:
                    return value.BoolValue.ToString();
                case Value.KindOneofCase.StructValue:
                    return value.StructValue.ToString();
                case Value.KindOneofCase.ListValue:
                    return string.Join(",", value.ListValue.Values.Select(v => v.ToString()));
                default:
                    return value.ToString();
            }
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
                Chunks = new List<DocumentChunk>()
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
                Embedding = point.Vectors?.Vector?.Data?.ToList() ?? new List<float>(),
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
                await _collectionManager.EnsureCollectionExistsAsync();

                // Create unique collection name for each document - Qdrant naming rules
                var documentCollectionName = $"{_collectionName}_doc_{document.Id:N}".Replace("-", ""); // Remove hyphens for Qdrant
                RepositoryLogMessages.LogQdrantDocumentCollectionCreating(Logger, documentCollectionName, _collectionName, document.Id, null);

                await _collectionManager.EnsureDocumentCollectionExistsAsync(documentCollectionName, document);

                // Generate embeddings for all chunks in parallel with progress tracking
                RepositoryLogMessages.LogQdrantEmbeddingsGenerationStarted(Logger, document.Chunks.Count, null);
                var embeddingTasks = document.Chunks.Select(async (chunk, index) =>
                {
                    if (chunk.Embedding == null || chunk.Embedding.Count == 0)
                    {
                        chunk.Embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content) ?? new List<float>();
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

        public async Task<SmartRAG.Entities.Document> GetByIdAsync(Guid id)
        {
            try
            {
                var filter = new Qdrant.Client.Grpc.Filter
                {
                    Must = {
                    new Qdrant.Client.Grpc.Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "id",
                            Match = new Qdrant.Client.Grpc.Match
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
                var lastPointId = (PointId)null;
                var batchCount = 0;

                while (batchCount < 50) // DefaultMaxBatches
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

                        if (documents.Count > 1000) // DefaultMaxDocuments
                        {
                            break;
                        }

                        batchCount++;

                        await Task.Delay(100); // DefaultDelayMs
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
                var filter = new Qdrant.Client.Grpc.Filter
                {
                    Must =
                {
                    new Qdrant.Client.Grpc.Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "id",
                            Match = new Qdrant.Client.Grpc.Match
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

        public async Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = DefaultMaxSearchResults)
        {
            try
            {
                // Check cache for duplicate requests
                var queryHash = $"{query}_{maxResults}";
                var cachedResults = _cacheManager.GetCachedResults(queryHash);
                if (cachedResults != null)
                {
                    return cachedResults;
                }

                await _collectionManager.EnsureCollectionExistsAsync();

                // Log that we're processing a new search
                RepositoryLogMessages.LogQdrantSearchStarted(Logger, query, null);

                // Generate embedding for semantic search
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
                if (queryEmbedding == null || queryEmbedding.Count == 0)
                {
                    // Fallback to text search if embedding fails
                    return await _searchService.FallbackTextSearchAsync(query, maxResults);
                }

                // Perform vector search using the search service
                var vectorResults = await _searchService.SearchAsync(queryEmbedding, maxResults);

                // Also gather keyword-based matches via hybrid search
                var hybridResults = await _searchService.HybridSearchAsync(query, maxResults * 4);

                // Combine and deduplicate results
                var allChunks = vectorResults.Concat(hybridResults).ToList();

                // Deduplicate by (DocumentId, ChunkIndex)
                var deduped = allChunks
                    .GroupBy(c => new { c.DocumentId, c.ChunkIndex })
                    .Select(g => g.OrderByDescending(c => c.RelevanceScore ?? 0.0).First())
                    .ToList();

                // Take top K per document to improve coverage
                var perDocTopK = Math.Max(1, Math.Min(3, maxResults));
                var topPerDocument = deduped
                    .GroupBy(c => c.DocumentId)
                    .SelectMany(g => g.OrderByDescending(c => c.RelevanceScore ?? 0.0).Take(perDocTopK))
                    .ToList();

                var remainingSlots = Math.Max(0, (maxResults * 3) - topPerDocument.Count);
                var topGlobal = deduped
                    .Except(topPerDocument)
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .Take(remainingSlots)
                    .ToList();

                var finalResults = topPerDocument
                    .Concat(topGlobal)
                    .Distinct()
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .ToList();

                RepositoryLogMessages.LogQdrantFinalResultsReturned(Logger, finalResults.Count, null);

                // Cache the results to prevent duplicate processing
                _cacheManager.CacheResults(queryHash, finalResults);

                return finalResults;
            }
            catch (Exception ex)
            {
                // Log error and fallback to text search
                RepositoryLogMessages.LogQdrantVectorSearchFailed(Logger, ex.Message, null);
                return await _searchService.FallbackTextSearchAsync(query, maxResults);
            }
        }

        #endregion

        #region Conversation Methods

        public async Task<string> GetConversationHistoryAsync(string sessionId)
        {
            // Qdrant conversation support - simplified implementation
            return await Task.FromResult(string.Empty);
        }

        public async Task AddToConversationAsync(string sessionId, string question, string answer)
        {
            // Qdrant conversation support - simplified implementation
            await Task.CompletedTask;
        }

        public async Task ClearConversationAsync(string sessionId)
        {
            // Qdrant conversation support - simplified implementation
            await Task.CompletedTask;
        }

        public async Task<bool> SessionExistsAsync(string sessionId)
        {
            // Qdrant conversation support - simplified implementation
            return await Task.FromResult(false);
        }

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
