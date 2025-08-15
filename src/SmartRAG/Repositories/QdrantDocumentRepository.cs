using Qdrant.Client;
using Qdrant.Client.Grpc;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System.Globalization;

namespace SmartRAG.Repositories;
public class QdrantDocumentRepository : IDocumentRepository
{
    private readonly QdrantClient _client;
    private readonly QdrantConfig _config;
    private readonly string _collectionName;
    private static readonly SemaphoreSlim _collectionInitLock = new(1, 1);
    private bool _collectionReady;

    public QdrantDocumentRepository(QdrantConfig config)
    {
        _config = config;
        _collectionName = config.CollectionName;


        string host;
        bool useHttps;

        if (config.Host.StartsWith("http://") || config.Host.StartsWith("https://"))
        {
            var uri = new Uri(config.Host);
            host = uri.Host;
            useHttps = uri.Scheme == "https";
        }
        else
        {
            host = config.Host;
            useHttps = config.UseHttps;
        }

        _client = new QdrantClient(
            host,
            https: useHttps,
            apiKey: config.ApiKey,
            grpcTimeout: TimeSpan.FromMinutes(5)
        );


        Task.Run(async () =>
            {
                try
                {
                    await InitializeCollectionAsync();
                }
                catch (Exception)
                {

                }
            });
    }

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

            Console.WriteLine($"[INFO] Creating main collection {_collectionName} with vector dimension: {vectorDimension}");

            // Create collection with proper configuration - use correct Qdrant API
            await _client.CreateCollectionAsync(_collectionName, vectorParams);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to create collection: {ex.Message}");
            throw;
        }
    }

    private async Task EnsureDocumentCollectionExistsAsync(string collectionName, Document document)
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

            Console.WriteLine($"[INFO] Creating collection {collectionName} with vector dimension: {vectorDimension}");

            // Create collection with proper configuration - use correct Qdrant API
            await _client.CreateCollectionAsync(collectionName, vectorParams);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to create document collection {collectionName}: {ex.Message}");
            throw;
        }
    }

    private static Distance GetDistanceMetric(string metric)
        => metric.ToLower() switch
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
    private static Document CreateDocumentFromMetadata(DocumentMetadata metadata)
        => new()
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
    private class DocumentMetadata
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public async Task<Document> AddAsync(Document document)
    {
        try
        {
            await EnsureCollectionExistsAsync();

            // Create unique collection name for each document - Qdrant naming rules
            var documentCollectionName = $"{_collectionName}_doc_{document.Id:N}".Replace("-", ""); // Remove hyphens for Qdrant
            Console.WriteLine($"[DEBUG] Creating document collection: {documentCollectionName}");
            Console.WriteLine($"[DEBUG] Base collection name: {_collectionName}");
            Console.WriteLine($"[DEBUG] Document ID: {document.Id}");

            await EnsureDocumentCollectionExistsAsync(documentCollectionName, document);

            // Generate embeddings for all chunks in parallel with progress tracking
            Console.WriteLine($"[INFO] Generating embeddings for {document.Chunks.Count} chunks...");
            var embeddingTasks = document.Chunks.Select(async (chunk, index) =>
            {
                if (chunk.Embedding == null || !chunk.Embedding.Any())
                {
                    chunk.Embedding = await GenerateEmbeddingAsync(chunk.Content) ?? new List<float>();
                    if (index % 10 == 0) // Progress every 10 chunks
                    {
                        Console.WriteLine($"[INFO] Generated embeddings for {index + 1}/{document.Chunks.Count} chunks");
                    }
                }
                return chunk;
            }).ToList();

            await Task.WhenAll(embeddingTasks);
            Console.WriteLine($"[INFO] All embeddings generated successfully!");

            // Batch process all chunks with larger batch size
            const int batchSize = 200; // Increased batch size for better performance
            var allPoints = new List<PointStruct>();

            Console.WriteLine($"[INFO] Creating {document.Chunks.Count} Qdrant points...");
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

                // Debug: Log payload creation
                Console.WriteLine($"[DEBUG] Created payload for chunk {chunk.ChunkIndex}: chunkIndex={chunk.ChunkIndex}, documentId={document.Id}, contentLength={chunk.Content.Length}");

                allPoints.Add(point);
            }

            // Process in batches for better performance
            Console.WriteLine($"[INFO] Uploading {allPoints.Count} points in batches of {batchSize}...");
            for (int i = 0; i < allPoints.Count; i += batchSize)
            {
                var batch = allPoints.Skip(i).Take(batchSize).ToList();
                await _client.UpsertAsync(documentCollectionName, batch);
                Console.WriteLine($"[INFO] Uploaded batch {i / batchSize + 1}/{(allPoints.Count + batchSize - 1) / batchSize}");
            }

            Console.WriteLine($"[INFO] Document '{document.FileName}' uploaded successfully to Qdrant collection: {documentCollectionName}!");
            return document;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to upload document: {ex.Message}");
            throw;
        }
    }

    public async Task<Document?> GetByIdAsync(Guid id)
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

            var result = await _client.ScrollAsync(_collectionName, filter, limit: 50);

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

    public async Task<List<Document>> GetAllAsync()
    {
        try
        {
            var documents = new List<Document>();
            var processedIds = new HashSet<string>();
            var lastPointId = (PointId?)null;
            const int batchSize = 25;
            var batchCount = 0;
            const int maxBatches = 50;

            while (batchCount < maxBatches)
            {
                try
                {
                    var result = await _client.ScrollAsync(
                        _collectionName,
                        limit: batchSize,
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

                    if (documents.Count > 1000)
                    {
                        break;
                    }

                    batchCount++;

                    await Task.Delay(100);
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
            return [];
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
    private const int CACHE_EXPIRY_MINUTES = 5;

    public async Task<List<DocumentChunk>> SearchAsync(string query, int maxResults = 5)
    {
        try
        {
            // Check cache for duplicate requests
            var queryHash = $"{query}_{maxResults}";
            lock (_cacheLock)
            {
                if (_searchCache.TryGetValue(queryHash, out var cached) && cached.Expiry > DateTime.UtcNow)
                {
                    Console.WriteLine($"[INFO] Returning cached results for query: {query.Substring(0, Math.Min(50, query.Length))}...");
                    return cached.Chunks.ToList(); // Return copy to avoid modification
                }
            }

            await EnsureCollectionExistsAsync();

            // Log that we're processing a new search
            Console.WriteLine($"[INFO] Processing new search query: {query.Substring(0, Math.Min(50, query.Length))}...");

            // Enable combined vector + keyword-assisted (hybrid) search
            Console.WriteLine("[INFO] Using vector + keyword-assisted (hybrid) search");

            // FALLBACK: Generate embedding for semantic search
            var queryEmbedding = await GenerateEmbeddingAsync(query);
            if (queryEmbedding == null || !queryEmbedding.Any())
            {
                // Fallback to text search if embedding fails
                return await FallbackTextSearchAsync(query, maxResults);
            }

            // Search in all document collections
            var allChunks = new List<DocumentChunk>();
            var collections = await _client.ListCollectionsAsync();

            Console.WriteLine($"[INFO] Processing new search query: {query.Substring(0, Math.Min(50, query.Length))}...");
            Console.WriteLine($"[DEBUG] All available collections: {string.Join(", ", collections)}");
            Console.WriteLine($"[DEBUG] Base collection name: {_collectionName}");
            Console.WriteLine($"[DEBUG] Looking for collections starting with: {_collectionName}_doc_");

            // Look for collections that match our document collection pattern
            var documentCollections = collections.Where(c => c.StartsWith(_collectionName + "_doc_")).ToList();

            Console.WriteLine($"[INFO] Found {documentCollections.Count} document collections: {string.Join(", ", documentCollections)}");

            // If no document collections found, check main collection
            if (!documentCollections.Any())
            {
                Console.WriteLine($"[WARNING] No document collections found, checking main collection: {_collectionName}");
                if (collections.Contains(_collectionName))
                {
                    documentCollections.Add(_collectionName);
                    Console.WriteLine($"[INFO] Using main collection: {_collectionName}");
                }
            }

            foreach (var collectionName in documentCollections)
            {
                try
                {
                    Console.WriteLine($"[INFO] Searching in collection: {collectionName}");
                    var searchResults = await _client.SearchAsync(
                        collectionName: collectionName,
                        vector: queryEmbedding.ToArray(),
                        limit: (ulong)Math.Max(20, maxResults * 4) // Daha fazla sonuç al!
                    );

                    Console.WriteLine($"[INFO] Found {searchResults.Count} results in collection {collectionName}");

                    // Convert to DocumentChunk objects
                    foreach (var result in searchResults)
                    {
                        var payload = result.Payload;
                        Console.WriteLine($"[DEBUG] Processing search result with score {result.Score}, Payload keys: {string.Join(", ", payload?.Keys ?? new List<string>())}");

                        if (payload != null)
                        {
                            // Use GetPayloadString helper method for consistent parsing
                            var content = GetPayloadString(payload, "content");
                            var docId = GetPayloadString(payload, "documentId");
                            var chunkIndex = GetPayloadString(payload, "chunkIndex");

                            Console.WriteLine($"[DEBUG] Parsed payload - Content: {(string.IsNullOrEmpty(content) ? "NO" : "YES")}, DocId: {(string.IsNullOrEmpty(docId) ? "NO" : "YES")}, ChunkIndex: {(string.IsNullOrEmpty(chunkIndex) ? "NO" : "YES")}");

                            if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(docId) && !string.IsNullOrEmpty(chunkIndex))
                            {
                                Console.WriteLine($"[DEBUG] Successfully created chunk from search result: {chunkIndex}");
                                var chunk = new DocumentChunk
                                {
                                    Id = Guid.NewGuid(), // Generate new ID since we can't parse PointId
                                    DocumentId = Guid.Parse(docId),
                                    Content = content,
                                    ChunkIndex = int.Parse(chunkIndex),
                                    RelevanceScore = result.Score // Score is already float
                                };
                                allChunks.Add(chunk);
                            }
                            else
                            {
                                Console.WriteLine($"[DEBUG] Payload parsing failed for search result. Content: {(string.IsNullOrEmpty(content) ? "NO" : "YES")}, DocId: {(string.IsNullOrEmpty(docId) ? "NO" : "YES")}, ChunkIndex: {(string.IsNullOrEmpty(chunkIndex) ? "NO" : "YES")}");

                                // Show actual payload content for debugging
                                foreach (var kvp in payload)
                                {
                                    Console.WriteLine($"[DEBUG] Payload field: {kvp.Key} = {kvp.Value}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other collections
                    Console.WriteLine($"[WARNING] Search failed in collection {collectionName}: {ex.Message}");

                    // If vector search fails, try fallback immediately for this collection
                    try
                    {
                        Console.WriteLine($"[INFO] Trying fallback text search for collection {collectionName}");
                        var fallbackChunks = await FallbackTextSearchForCollectionAsync(collectionName, query, maxResults);
                        allChunks.AddRange(fallbackChunks);
                    }
                    catch (Exception fallbackEx)
                    {
                        Console.WriteLine($"[WARNING] Fallback also failed for collection {collectionName}: {fallbackEx.Message}");
                    }
                }
            }

            // Also gather keyword-based matches via hybrid path
            try
            {
                var hybridResults = await HybridSearchAsync(query, maxResults * 2);
                allChunks.AddRange(hybridResults);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Hybrid search failed: {ex.Message}");
            }

            // Deduplicate by (DocumentId, ChunkIndex)
            var deduped = allChunks
                .GroupBy(c => new { c.DocumentId, c.ChunkIndex })
                .Select(g => g.OrderByDescending(c => c.RelevanceScore ?? 0.0).First())
                .ToList();

            // Ensure we don't lose underrepresented documents before higher-level diversity
            Console.WriteLine($"[INFO] Total chunks found across all collections: {deduped.Count}");
            // Take top K per document to improve coverage of key fields (e.g., acente/sahibi)
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
                .Distinct() // same references from allChunks; removes duplicates
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .ToList();

            Console.WriteLine($"[INFO] Returning {finalResults.Count} chunks to DocumentService for final selection");
            Console.WriteLine($"[DEBUG] Repository final unique documents: {finalResults.Select(c => c.DocumentId).Distinct().Count()}");

            // Cache the results to prevent duplicate processing
            lock (_cacheLock)
            {
                _searchCache[queryHash] = (finalResults.ToList(), DateTime.UtcNow.AddMinutes(CACHE_EXPIRY_MINUTES));

                // Clean up expired cache entries
                var expiredKeys = _searchCache.Where(kvp => kvp.Value.Expiry <= DateTime.UtcNow).Select(kvp => kvp.Key).ToList();
                foreach (var key in expiredKeys)
                {
                    _searchCache.Remove(key);
                }

                Console.WriteLine($"[INFO] Cached search results for query: {query.Substring(0, Math.Min(50, query.Length))}... (Cache size: {_searchCache.Count})");
            }

            return finalResults;
        }
        catch (Exception ex)
        {
            // Log error and fallback to text search
            Console.WriteLine($"[WARNING] Vector search failed: {ex.Message}, falling back to text search");
            return await FallbackTextSearchAsync(query, maxResults);
        }
    }

    private async Task<List<DocumentChunk>> FallbackTextSearchAsync(string query, int maxResults)
    {
        try
        {
            Console.WriteLine($"[INFO] Using global fallback text search for query: {query}");
            var queryLower = query.ToLowerInvariant();
            var relevantChunks = new List<DocumentChunk>();

            // Get all collections to search in
            var collections = await _client.ListCollectionsAsync();
            Console.WriteLine($"[DEBUG] All collections for fallback search: {string.Join(", ", collections)}");

            // Look for collections that match our document collection pattern
            var documentCollections = collections.Where(c => c.StartsWith(_collectionName + "_doc_")).ToList();

            Console.WriteLine($"[INFO] Found {documentCollections.Count} document collections for fallback search: {string.Join(", ", documentCollections)}");
            Console.WriteLine($"[DEBUG] Looking for collections starting with: {_collectionName}_doc_");

            if (!documentCollections.Any())
            {
                Console.WriteLine($"[WARNING] No document collections found for fallback search");
                Console.WriteLine($"[DEBUG] Available collections that might match: {string.Join(", ", collections.Where(c => c.Contains("doc_")))}");
                return new List<DocumentChunk>();
            }

            foreach (var collectionName in documentCollections)
            {
                try
                {
                    Console.WriteLine($"[INFO] Searching in collection: {collectionName} for global fallback");

                    // Get all points from collection
                    var scrollResult = await _client.ScrollAsync(collectionName, limit: 1000);
                    Console.WriteLine($"[DEBUG] ScrollAsync returned {scrollResult.Result.Count} points in collection {collectionName}");

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
                                        ChunkIndex = int.Parse(chunkIndex),
                                        RelevanceScore = 0.5 // Default score for text search
                                    };
                                    relevantChunks.Add(chunk);

                                    if (relevantChunks.Count >= maxResults)
                                        break;
                                }
                            }
                            else
                            {
                                var hasContent = !string.IsNullOrEmpty(content);
                                var hasDocId = !string.IsNullOrEmpty(docId);
                                var hasChunkIndex = !string.IsNullOrEmpty(chunkIndex);
                                Console.WriteLine($"[DEBUG] Payload parsing failed for point in collection {collectionName}. Content: {(hasContent ? "YES" : "NO")}, DocId: {(hasDocId ? "YES" : "NO")}, ChunkIndex: {(hasChunkIndex ? "YES" : "NO")}");
                            }
                        }
                    }

                    if (relevantChunks.Count >= maxResults)
                        break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] Global fallback search failed in collection {collectionName}: {ex.Message}");
                }
            }

            Console.WriteLine($"[INFO] Global fallback text search found {relevantChunks.Count} chunks");
            return relevantChunks.Take(maxResults).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Global fallback text search failed: {ex.Message}");
            return new List<DocumentChunk>();
        }
    }

    private async Task<List<DocumentChunk>> FallbackTextSearchForCollectionAsync(string collectionName, string query, int maxResults)
    {
        try
        {
            Console.WriteLine($"[INFO] Using fallback text search for collection: {collectionName}");
            var queryLower = query.ToLowerInvariant();
            var relevantChunks = new List<DocumentChunk>();

            // Get all points from collection
            var scrollResult = await _client.ScrollAsync(collectionName, limit: 1000);
            Console.WriteLine($"[DEBUG] ScrollAsync returned {scrollResult.Result.Count} points");

            foreach (var point in scrollResult.Result)
            {
                var payload = point.Payload;
                Console.WriteLine($"[DEBUG] Processing point {point.Id}, Payload keys: {string.Join(", ", payload?.Keys ?? new List<string>())}");

                if (payload != null)
                {
                    // Use GetPayloadString helper method for consistent parsing
                    var content = GetPayloadString(payload, "content");
                    var docId = GetPayloadString(payload, "documentId");
                    var chunkIndex = GetPayloadString(payload, "chunkIndex");

                    if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(docId) && !string.IsNullOrEmpty(chunkIndex))
                    {
                        var contentStr = content.ToLowerInvariant();
                        Console.WriteLine($"[DEBUG] Processing chunk {chunkIndex}: {contentStr.Substring(0, Math.Min(100, contentStr.Length))}...");

                        // Simple text matching
                        if (contentStr.Contains(queryLower))
                        {
                            Console.WriteLine($"[DEBUG] Found matching chunk: {chunkIndex}");
                            var chunk = new DocumentChunk
                            {
                                Id = Guid.NewGuid(),
                                DocumentId = Guid.Parse(docId),
                                Content = content,
                                ChunkIndex = int.Parse(chunkIndex),
                                RelevanceScore = 0.5 // Default score for text search
                            };
                            relevantChunks.Add(chunk);

                            if (relevantChunks.Count >= maxResults)
                                break;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] Payload parsing failed for point. Content: {(string.IsNullOrEmpty(content) ? "NO" : "YES")}, DocId: {(string.IsNullOrEmpty(docId) ? "NO" : "YES")}, ChunkIndex: {(string.IsNullOrEmpty(chunkIndex) ? "NO" : "YES")}");

                        // Show actual payload content for debugging
                        foreach (var kvp in payload)
                        {
                            Console.WriteLine($"[DEBUG] Payload field: {kvp.Key} = {kvp.Value}");
                        }
                    }
                }
            }

            Console.WriteLine($"[INFO] Fallback text search found {relevantChunks.Count} chunks in collection {collectionName}");
            return relevantChunks.Take(maxResults).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Fallback text search failed for collection {collectionName}: {ex.Message}");
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
            var documentCollections = collections.Where(c => c.StartsWith(_collectionName + "_doc_")).ToList();

            // If no document collections, check main collection
            if (!documentCollections.Any() && collections.Contains(_collectionName))
            {
                documentCollections.Add(_collectionName);
            }

            if (documentCollections.Any())
            {
                // Get dimension from first available collection
                var firstCollection = documentCollections.First();
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
                            Console.WriteLine($"[INFO] Detected vector dimension: {size} from collection {firstCollection}");
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
            Console.WriteLine($"[INFO] Using default vector dimension: 768");
            return 768;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] Failed to detect vector dimension: {ex.Message}, using default: 768");
            return 768;
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
            Console.WriteLine($"[DEBUG] Extracted keywords: {string.Join(", ", keywords)}");

            if (!keywords.Any())
            {
                Console.WriteLine("[DEBUG] No meaningful keywords found, skipping hybrid search");
                return hybridResults;
            }

            var collections = await _client.ListCollectionsAsync();
            var documentCollections = collections.Where(c => c.StartsWith($"{_collectionName}_doc_")).ToList();

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
                        if (score > 0.1) // Lower threshold for keyword matches
                        {
                            chunk.RelevanceScore = score;
                            hybridResults.Add(chunk);
                            Console.WriteLine($"[DEBUG] Hybrid match found in {collectionName} with score {score:F3}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] Hybrid search failed for collection {collectionName}: {ex.Message}");
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
            Console.WriteLine($"[ERROR] Hybrid search failed: {ex.Message}");
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
            // Skip stop words and very short words
            if (word.Length > 2 && !IsStopWord(word))
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
        if (!keywords.Any()) return 0.0;

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

    /// <summary>
    /// Simple stop word check for Turkish and English
    /// </summary>
    private static bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string>
        {
            "ve", "bir", "bu", "da", "de", "için", "ile", "var", "olan", "gibi", "daha", "çok",
            "and", "the", "a", "an", "is", "are", "was", "were", "be", "been", "being", "have", "has", "had",
            "nedir", "midir", "ne", "kaç", "adı", "olarak", "için", "ile", "mi", "mı"
        };
        return stopWords.Contains(word);
    }
}
