using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SmartRAG.Entities;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Storage.Qdrant;
using SmartRAG.Interfaces.AI;
using SmartRAG.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Repositories
{

    /// <summary>
    /// Qdrant vector database document repository implementation
    /// </summary>
    public class QdrantDocumentRepository : IDocumentRepository, IDisposable
    {
        private const int DefaultMaxSearchResults = 5;
        private const int DefaultBatchSize = 200;
        private const int DefaultGrpcTimeoutMinutes = 5;

        private readonly QdrantClient _client;
        private readonly QdrantConfig _config;
        private readonly string _collectionName;
        private readonly ILogger<QdrantDocumentRepository> _logger;

        private readonly IQdrantCollectionManager _collectionManager;
        private readonly IQdrantEmbeddingService _embeddingService;
        private readonly IAIService _aiService;
        private readonly IQdrantSearchService _searchService;

        protected ILogger Logger => _logger;

        public QdrantDocumentRepository(
            IOptions<QdrantConfig> config,
            ILogger<QdrantDocumentRepository> logger,
            IQdrantCollectionManager collectionManager,
            IQdrantEmbeddingService embeddingService,
            IAIService aiService,
            IQdrantSearchService searchService)
        {
            _config = config.Value;
            _collectionName = _config.CollectionName;
            _logger = logger;
            _collectionManager = collectionManager;
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));

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

        /// <summary>
        /// Determines document type from ContentType and file extension
        /// </summary>
        private static string DetermineDocumentType(SmartRAG.Entities.Document document)
        {
            if (document == null)
            {
                return "Document";
            }

            if (!string.IsNullOrWhiteSpace(document.ContentType) &&
                document.ContentType.StartsWith("audio", StringComparison.OrdinalIgnoreCase))
            {
                return "Audio";
            }

            var extension = System.IO.Path.GetExtension(document.FileName)?.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(extension))
            {
                switch (extension)
                {
                    case ".wav":
                    case ".mp3":
                    case ".m4a":
                    case ".flac":
                    case ".ogg":
                        return "Audio";
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".gif":
                    case ".bmp":
                    case ".tiff":
                    case ".webp":
                        return "Image";
                }
            }

            return "Document";
        }

        private static string NormalizeContentForStorage(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            return content.Normalize(System.Text.NormalizationForm.FormC);
        }

        private static string GetPayloadString(Google.Protobuf.Collections.MapField<string, Value> payload, string key)
        {
            if (payload == null) return string.Empty;

            if (!payload.TryGetValue(key, out Value value) || value == null)
                return string.Empty;

            string result;
            switch (value.KindCase)
            {
                case Value.KindOneofCase.StringValue:
                    result = value.StringValue ?? string.Empty;
                    if (!string.IsNullOrEmpty(result))
                    {
                        result = result.Normalize(System.Text.NormalizationForm.FormC);
                    }
                    return result;
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
        /// Extracts document metadata from Qdrant point payload
        /// </summary>
        private static DocumentMetadata ExtractDocumentMetadata(Google.Protobuf.Collections.MapField<string, Value> payload)
        {
            var fileName = GetPayloadString(payload, "fileName");
            var contentType = GetPayloadString(payload, "contentType");
            var fileSizeStr = GetPayloadString(payload, "fileSize");
            var uploadedAtStr = GetPayloadString(payload, "uploadedAt");
            var uploadedBy = GetPayloadString(payload, "uploadedBy");
            var content = GetPayloadString(payload, "content");
            var idStr = GetPayloadString(payload, "documentId");

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

        private static Dictionary<string, object> ExtractAdditionalMetadata(Google.Protobuf.Collections.MapField<string, Value> payload)
        {
            var metadata = new Dictionary<string, object>();

            foreach (var item in payload)
            {
                if (item.Key.StartsWith("metadata_", StringComparison.OrdinalIgnoreCase))
                {
                    var key = item.Key["metadata_".Length..];
                    var value = GetPayloadString(payload, item.Key);
                    if (!string.IsNullOrEmpty(value))
                    {
                        metadata[key] = value;
                    }
                }
            }

            return metadata;
        }

        /// <summary>
        /// Creates Document from metadata
        /// </summary>
        private static SmartRAG.Entities.Document CreateDocumentFromMetadata(DocumentMetadata metadata, Dictionary<string, object> additionalMetadata = null)
        {
            var document = new SmartRAG.Entities.Document()
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

            if (additionalMetadata != null && additionalMetadata.Count > 0)
            {
                document.Metadata = new Dictionary<string, object>(additionalMetadata);
            }

            return document;
        }

        /// <summary>
        /// Creates DocumentChunk from Qdrant point
        /// </summary>
        private static DocumentChunk CreateDocumentChunk(RetrievedPoint point, Guid documentId, DateTime fallbackCreatedAt, string fileName = null)
        {
            var chunkContent = GetPayloadString(point.Payload, "content");
            var chunkUploadedAtStr = GetPayloadString(point.Payload, "uploadedAt");
            var documentType = GetPayloadString(point.Payload, "documentType");
            var chunkIdStr = GetPayloadString(point.Payload, "chunkId");
            var chunkIndexStr = GetPayloadString(point.Payload, "chunkIndex");
            var payloadFileName = GetPayloadString(point.Payload, "fileName");

            if (!DateTime.TryParse(chunkUploadedAtStr, null, DateTimeStyles.RoundtripKind, out DateTime chunkCreatedAt))
                chunkCreatedAt = fallbackCreatedAt;

            if (string.IsNullOrWhiteSpace(documentType))
                documentType = "Document";

            Guid chunkId;
            if (!string.IsNullOrWhiteSpace(chunkIdStr) && Guid.TryParse(chunkIdStr, out var parsedChunkId))
            {
                chunkId = parsedChunkId;
            }
            else if (point.Id?.Uuid != null && Guid.TryParse(point.Id.Uuid, out var pointIdGuid))
            {
                chunkId = pointIdGuid;
            }
            else
            {
                chunkId = Guid.NewGuid();
            }

            int chunkIndex = 0;
            if (!string.IsNullOrWhiteSpace(chunkIndexStr) && int.TryParse(chunkIndexStr, out var parsedChunkIndex))
            {
                chunkIndex = parsedChunkIndex;
            }

            return new DocumentChunk
            {
                Id = chunkId,
                DocumentId = documentId,
                FileName = payloadFileName ?? fileName ?? string.Empty,
                Content = chunkContent,
                ChunkIndex = chunkIndex,
                Embedding = point.Vectors?.Vector?.Dense?.Data?.ToList() ?? new List<float>(),
                CreatedAt = chunkCreatedAt,
                DocumentType = documentType,
                StartPosition = 0,
                EndPosition = chunkContent?.Length ?? 0
            };
        }

        /// <summary>
        /// Document metadata extraction helper class
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

        public async Task<SmartRAG.Entities.Document> AddAsync(SmartRAG.Entities.Document document, CancellationToken cancellationToken = default)
        {
            try
            {
                await _collectionManager.EnsureCollectionExistsAsync(cancellationToken);

                var documentCollectionName = $"{_collectionName}_doc_{document.Id:N}".Replace("-", "");
                RepositoryLogMessages.LogQdrantDocumentCollectionCreating(Logger, documentCollectionName, _collectionName, document.Id, null);

                var allCollections = await _client.ListCollectionsAsync();
                if (allCollections.Contains(documentCollectionName))
                    await _collectionManager.DeleteCollectionAsync(documentCollectionName, cancellationToken);

                await _collectionManager.EnsureDocumentCollectionExistsAsync(documentCollectionName, document, cancellationToken);

                SmartRAG.Services.Helpers.DocumentValidator.ValidateDocument(document);
                SmartRAG.Services.Helpers.DocumentValidator.ValidateChunks(document);

                RepositoryLogMessages.LogQdrantEmbeddingsGenerationStarted(Logger, document.Chunks.Count, null);
                var embeddingTasks = document.Chunks.Select(async (chunk, index) =>
                {
                    if (chunk.Embedding == null || chunk.Embedding.Count == 0)
                    {
                        var contentForEmbedding = NormalizeContentForStorage(chunk.Content);
                        chunk.Embedding = await _embeddingService.GenerateEmbeddingAsync(contentForEmbedding, cancellationToken) ?? new List<float>();
                    }
                    return chunk;
                }).ToList();

                await Task.WhenAll(embeddingTasks);
                RepositoryLogMessages.LogQdrantEmbeddingsGenerationCompleted(Logger, document.Chunks.Count, null);

                var allPoints = new List<PointStruct>();

                RepositoryLogMessages.LogQdrantPointsCreationStarted(Logger, document.Chunks.Count, null);
                foreach (var chunk in document.Chunks)
                {
                    var point = new PointStruct
                    {
                        Id = new PointId { Uuid = Guid.NewGuid().ToString() },
                        Vectors = chunk.Embedding.ToArray()
                    };

                    var normalizedContent = NormalizeContentForStorage(chunk.Content);

                    point.Payload.Add("chunkId", chunk.Id.ToString());
                    point.Payload.Add("chunkIndex", chunk.ChunkIndex);
                    point.Payload.Add("content", normalizedContent);
                    point.Payload.Add("documentId", document.Id.ToString());
                    point.Payload.Add("fileName", chunk.FileName ?? document.FileName);
                    point.Payload.Add("contentType", document.ContentType);
                    point.Payload.Add("fileSize", document.FileSize);
                    point.Payload.Add("uploadedAt", document.UploadedAt.ToString("O"));
                    point.Payload.Add("uploadedBy", document.UploadedBy);
                    point.Payload.Add("documentType", chunk.DocumentType ?? DetermineDocumentType(document));

                    if (document.Metadata != null)
                    {
                        foreach (var metadataItem in document.Metadata)
                        {
                            if (metadataItem.Value != null)
                            {
                                point.Payload.Add($"metadata_{metadataItem.Key}", metadataItem.Value.ToString());
                            }
                        }
                    }

                    allPoints.Add(point);
                }

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

        public async Task<SmartRAG.Entities.Document> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var documentCollectionName = $"{_collectionName}_doc_{id:N}".Replace("-", "");
                
                var allCollections = await _client.ListCollectionsAsync();
                if (!allCollections.Contains(documentCollectionName))
                {
                    Logger.LogDebug("Document collection {Collection} not found for document {DocumentId}", documentCollectionName, id);
                    return null;
                }

                var result = await _client.ScrollAsync(documentCollectionName, limit: 10000);

                if (result.Result.Count == 0)
                {
                    Logger.LogDebug("No chunks found in collection {Collection} for document {DocumentId}", documentCollectionName, id);
                    return null;
                }

                var firstPoint = result.Result.First();
                var metadata = ExtractDocumentMetadata(firstPoint.Payload);
                var additionalMetadata = ExtractAdditionalMetadata(firstPoint.Payload);

                if (metadata.Id == Guid.Empty)
                {
                    Logger.LogWarning("Invalid metadata for document {DocumentId}", id);
                    return null;
                }

                var document = CreateDocumentFromMetadata(metadata, additionalMetadata);
                document.Metadata ??= new Dictionary<string, object>();
                document.Metadata["CollectionName"] = documentCollectionName;
                
                foreach (var point in result.Result)
                {
                    var chunk = CreateDocumentChunk(point, document.Id, metadata.UploadedAt, document.FileName);
                    document.Chunks.Add(chunk);
                }

                Logger.LogDebug("Retrieved document {DocumentId} with {ChunkCount} chunks from collection {Collection}", 
                    id, document.Chunks.Count, documentCollectionName);

                return document;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to retrieve document {DocumentId}", id);
                return null;
            }
        }

        public async Task<List<SmartRAG.Entities.Document>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var documents = new List<SmartRAG.Entities.Document>();
                var allCollections = await _client.ListCollectionsAsync();
                var documentCollections = allCollections
                    .Where(c => c.StartsWith($"{_collectionName}_doc_", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var docCollection in documentCollections)
                {
                    try
                    {
                        var result = await _client.ScrollAsync(
                            docCollection,
                            limit: 1);

                        if (result.Result.Count == 0)
                            continue;

                        var firstPoint = result.Result.First();
                        var metadata = ExtractDocumentMetadata(firstPoint.Payload);
                        var additionalMetadata = ExtractAdditionalMetadata(firstPoint.Payload);

                        if (metadata.Id == Guid.Empty)
                            continue;

                        var document = CreateDocumentFromMetadata(metadata, additionalMetadata);
                        document.Metadata ??= new Dictionary<string, object>();
                        document.Metadata["CollectionName"] = docCollection;

                        var collectionInfo = await _client.GetCollectionInfoAsync(docCollection);
                        var chunkCount = (int)collectionInfo.PointsCount;

                        for (int i = 0; i < chunkCount; i++)
                        {
                            document.Chunks.Add(new DocumentChunk
                            {
                                Id = Guid.NewGuid(),
                                DocumentId = document.Id,
                                FileName = document.FileName,
                                Content = string.Empty,
                                ChunkIndex = i,
                                CreatedAt = metadata.UploadedAt
                            });
                        }

                        documents.Add(document);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to retrieve document from collection: {Collection}", docCollection);
                        continue;
                    }
                }

                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all documents");
                return new List<SmartRAG.Entities.Document>();
            }
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var documentCollectionName = $"{_collectionName}_doc_{id:N}".Replace("-", "");
                var allCollections = await _client.ListCollectionsAsync();
                if (!allCollections.Contains(documentCollectionName))
                    return true;

                await _collectionManager.DeleteCollectionAsync(documentCollectionName, cancellationToken);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Clears all documents by deleting all document collections and recreating main collection
        /// </summary>
        public async Task<bool> ClearAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var allCollections = await _client.ListCollectionsAsync();
                var documentCollections = allCollections
                    .Where(c => c.StartsWith($"{_collectionName}_doc_", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var docCollection in documentCollections)
                {
                    try
                    {
                        await _collectionManager.DeleteCollectionAsync(docCollection);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete document collection: {Collection}", docCollection);
                    }
                }

                await _collectionManager.RecreateCollectionAsync(_collectionName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear all documents from Qdrant");
                return false;
            }
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var collectionInfo = await _client.GetCollectionInfoAsync(_collectionName);
                return (int)collectionInfo.PointsCount;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private const int DefaultTopPerDocumentK = 3;

        /// <summary>
        /// Searches documents using query string with business logic (embedding generation, caching, deduplication, prioritization)
        /// </summary>
        /// <param name="query">Search query string</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>List of relevant document chunks</returns>
        public async Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = DefaultMaxSearchResults, CancellationToken cancellationToken = default)
        {
            try
            {
                await _collectionManager.EnsureCollectionExistsAsync(cancellationToken);

                var normalizedQuery = NormalizeContentForStorage(query);

                // Use AI embedding service for query embeddings (semantic similarity)
                // Document embeddings are already stored using AI embeddings from DocumentService
                // Hash-based embedding would break semantic similarity
                var queryEmbedding = await _aiService.GenerateEmbeddingsAsync(normalizedQuery, cancellationToken);
                if (queryEmbedding == null || queryEmbedding.Count == 0)
                {
                    _logger.LogWarning("AI embedding generation failed, falling back to text search");
                    var embeddingFallback = await _searchService.FallbackTextSearchAsync(query, maxResults, cancellationToken);
                    RepositoryLogMessages.LogQdrantFinalResultsReturned(Logger, embeddingFallback.Count, null);
                    return embeddingFallback;
                }

                var vectorTask = _searchService.SearchAsync(queryEmbedding, maxResults, cancellationToken);
                var fallbackTask = _searchService.FallbackTextSearchAsync(query, maxResults * 2, cancellationToken);
                await Task.WhenAll(vectorTask, fallbackTask).ConfigureAwait(false);

                var vectorResults = await vectorTask.ConfigureAwait(false);
                var fallbackResults = await fallbackTask.ConfigureAwait(false);

                var vectorIds = new HashSet<(Guid DocId, int ChunkIdx)>(
                    vectorResults.Select(c => (c.DocumentId, c.ChunkIndex)));
                var supplementalFromFallback = fallbackResults
                    .Where(c => !vectorIds.Contains((c.DocumentId, c.ChunkIndex)))
                    .ToList();

                var mergedRaw = vectorResults.Concat(supplementalFromFallback).ToList();
                var processedResults = ProcessSearchResults(mergedRaw, maxResults);

                RepositoryLogMessages.LogQdrantFinalResultsReturned(Logger, processedResults.Count, null);
                return processedResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vector search failed, falling back to text search");
                var errorFallback = await _searchService.FallbackTextSearchAsync(query, maxResults, cancellationToken);
                RepositoryLogMessages.LogQdrantFinalResultsReturned(Logger, errorFallback.Count, null);
                return errorFallback;
            }
        }

        /// <summary>
        /// Processes raw search results by applying business logic: deduplication, top-per-document, chunk 0 priority
        /// </summary>
        /// <param name="rawResults">Raw search results from vector search</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>Processed results with business logic applied</returns>
        private List<DocumentChunk> ProcessSearchResults(List<DocumentChunk> rawResults, int maxResults)
        {
            var allChunks = rawResults.ToList();

            var deduped = allChunks
                .GroupBy(c => new { c.DocumentId, c.ChunkIndex })
                .Select(g => g.OrderByDescending(c => c.RelevanceScore ?? 0.0).First())
                .ToList();

            var chunk0 = deduped.FirstOrDefault(c => c.ChunkIndex == 0);

            var perDocTopK = Math.Max(1, Math.Min(DefaultTopPerDocumentK, maxResults));
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

            if (chunk0 != null && !finalResults.Any(c => c.ChunkIndex == 0))
            {
                finalResults = new List<DocumentChunk> { chunk0 }.Concat(finalResults).ToList();
            }

            return finalResults;
        }

        public void Dispose()
        {
            _client?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
