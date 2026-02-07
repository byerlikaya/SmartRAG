
namespace SmartRAG.Services.Document;



/// <summary>
/// Implementation of document service focused on CRUD operations
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentParserService _documentParserService;
    private readonly IAIService _aiService;
    private readonly SmartRagOptions _options;
    private readonly ILogger<DocumentService> _logger;


    public DocumentService(
        IDocumentRepository documentRepository,
        IDocumentParserService documentParserService,
        IAIService aiService,
        IOptions<SmartRagOptions> options,
        ILogger<DocumentService> logger)
    {
        _documentRepository = documentRepository;
        _documentParserService = documentParserService;
        _aiService = aiService;
        _options = options.Value;
        _logger = logger;
    }

    private const int VoyageAIMaxBatchSize = 128;
    private const int RateLimitDelayMs = 1000;
    private const string UnsupportedFileTypeFormat = "Unsupported file type: {0}. Supported types: {1}";
    private const string UnsupportedContentTypeFormat = "Unsupported content type: {0}. Supported types: {1}";

    /// <summary>
    /// [AI Query] [Document Query] Uploads a document, generates embeddings, and saves it
    /// </summary>
    /// <param name="request">Request containing document upload parameters</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Created document entity</returns>
    public async Task<SmartRAG.Entities.Document> UploadDocumentAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var supportedExtensions = _documentParserService.GetSupportedFileTypes();
        var supportedContentTypes = _documentParserService.GetSupportedContentTypes();

        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(ext) && !supportedExtensions.Contains(ext))
        {
            var list = string.Join(", ", supportedExtensions);
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, UnsupportedFileTypeFormat, ext, list));
        }

        if (!string.IsNullOrWhiteSpace(request.ContentType) && !supportedContentTypes.Any(ct => request.ContentType.StartsWith(ct, StringComparison.OrdinalIgnoreCase)))
        {
            var list = string.Join(", ", supportedContentTypes);
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, UnsupportedContentTypeFormat, request.ContentType, list));
        }

        if (request.AdditionalMetadata != null && request.AdditionalMetadata.TryGetValue("FileHash", out var incomingHash) && incomingHash != null)
        {
            var hashStr = incomingHash.ToString();
            if (!string.IsNullOrEmpty(hashStr))
            {
                var existingDocs = await _documentRepository.GetAllAsync(cancellationToken);
                foreach (var existing in existingDocs)
                {
                    if (!existing.Metadata.TryGetValue("FileHash", out var existingHash) || existingHash == null ||
                        !string.Equals(existingHash.ToString(), hashStr, StringComparison.OrdinalIgnoreCase))
                        continue;
                    _logger.LogInformation("Skipping duplicate upload (same file hash): {FileName} - returning existing document Id: {DocumentId}", request.FileName, existing.Id);
                    return existing;
                }
            }
        }

        var document = await _documentParserService.ParseDocumentAsync(request.FileStream, request.FileName, request.ContentType, request.UploadedBy, request.Language);

        if (request.FileSize.HasValue && request.FileSize.Value > 0)
        {
            document.FileSize = request.FileSize.Value;
        }

        if (request.AdditionalMetadata is { Count: > 0 })
        {
            foreach (var item in request.AdditionalMetadata)
            {
                document.Metadata[item.Key] = item.Value;
            }
        }

        var hasContent = !string.IsNullOrWhiteSpace(document.Content)
                         && document.Chunks is { Count: > 0 }
                         && document.Chunks.Any(c => !string.IsNullOrWhiteSpace(c.Content));
        if (!hasContent)
        {
            throw new Exceptions.DocumentSkippedException(
                string.Format(CultureInfo.InvariantCulture, "Document has no content to index (e.g. audio transcription unavailable). Skipping: {0}", request.FileName));
        }

        var chunks = document.Chunks!;
        var allChunkContents = chunks.Select(c => c.Content).ToList();

        try
        {
            var allEmbeddings = await _aiService.GenerateEmbeddingsBatchAsync(allChunkContents, cancellationToken);

            if (allEmbeddings.Count == chunks.Count)
            {
                for (var i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];
                    chunk.DocumentId = document.Id;

                    if (allEmbeddings[i] != null && allEmbeddings[i].Count > 0)
                    {
                        chunk.Embedding = allEmbeddings[i];
                    }
                    else
                    {
                        chunk.Embedding = new List<float>();
                    }

                    if (chunk.CreatedAt == default)
                        chunk.CreatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                foreach (var chunk in chunks)
                {
                    chunk.DocumentId = document.Id;
                    chunk.Embedding = new List<float>(); // Empty but not null
                    if (chunk.CreatedAt == default)
                        chunk.CreatedAt = DateTime.UtcNow;
                }
            }
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogBatchEmbeddingFailed(_logger, ex.Message, ex);

            foreach (var chunk in chunks)
            {
                chunk.DocumentId = document.Id;
                chunk.Embedding = new List<float>();
                if (chunk.CreatedAt == default)
                    chunk.CreatedAt = DateTime.UtcNow;
            }
        }

        var savedDocument = await _documentRepository.AddAsync(document, cancellationToken);
        ServiceLogMessages.LogDocumentUploaded(_logger, request.FileName, null);

        return savedDocument;
    }

    /// <summary>
    /// [Document Query] Retrieves a document by ID
    /// </summary>
    public async Task<SmartRAG.Entities.Document> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default) => await _documentRepository.GetByIdAsync(id, cancellationToken);

    /// <summary>
    /// [Document Query] Retrieves all documents
    /// </summary>
    public async Task<List<SmartRAG.Entities.Document>> GetAllDocumentsAsync(CancellationToken cancellationToken = default) => await _documentRepository.GetAllAsync(cancellationToken);

    /// <summary>
    /// Retrieves all documents filtered by the enabled search options (text, audio, image)
    /// </summary>
    public async Task<List<SmartRAG.Entities.Document>> GetAllDocumentsFilteredAsync(SearchOptions? options, CancellationToken cancellationToken = default)
    {
        var allDocuments = await _documentRepository.GetAllAsync(cancellationToken);

        if (options == null)
        {
            return allDocuments;
        }

        return allDocuments.Where(d =>
        {
            if (!options.EnableDatabaseSearch && IsSchemaDocument(d))
                return false;
            return (options.EnableDocumentSearch && IsTextDocument(d)) ||
                   (options.EnableAudioSearch && IsAudioDocument(d)) ||
                   (options.EnableImageSearch && IsImageDocument(d));
        }).ToList();
    }

    public async Task<bool> DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default) => await _documentRepository.DeleteAsync(id, cancellationToken);

    public async Task<Dictionary<string, object>> GetStorageStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var count = await _documentRepository.GetCountAsync(cancellationToken);
        var stats = new Dictionary<string, object>
        {
            ["TotalDocuments"] = count,
            ["DocumentCount"] = count,
            ["StorageProvider"] = _options.StorageProvider.ToString(),
            ["MaxChunkSize"] = _options.MaxChunkSize,
            ["ChunkOverlap"] = _options.ChunkOverlap
        };

        return stats;
    }

    /// <summary>
    /// [AI Query] [Document Query] Regenerates embeddings for all documents
    /// </summary>
    public async Task<bool> RegenerateAllEmbeddingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ServiceLogMessages.LogEmbeddingRegenerationStarted(_logger, null);

            var allDocuments = await _documentRepository.GetAllAsync(cancellationToken);
            var totalChunks = allDocuments.Sum(d => d.Chunks!.Count);
            var processedChunks = 0;
            var successCount = 0;

            var chunksToProcess = new List<DocumentChunk>();
            var documentChunkMap = new Dictionary<DocumentChunk, SmartRAG.Entities.Document>();

            foreach (var document in allDocuments)
            {
                foreach (var chunk in document.Chunks!)
                {
                    if (chunk.Embedding.Count > 0)
                    {
                        processedChunks++;
                        continue;
                    }

                    chunksToProcess.Add(chunk);
                    documentChunkMap[chunk] = document;
                }
            }

            if (chunksToProcess.Count == 0)
            {
                return true;
            }

            var totalBatches = (int)Math.Ceiling((double)chunksToProcess.Count / VoyageAIMaxBatchSize);

            for (var batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var startIndex = batchIndex * VoyageAIMaxBatchSize;
                var endIndex = Math.Min(startIndex + VoyageAIMaxBatchSize, chunksToProcess.Count);
                var currentBatch = chunksToProcess.Skip(startIndex).Take(endIndex - startIndex).ToList();

                var batchContents = currentBatch.Select(c => c.Content).ToList();
                var batchEmbeddings = await _aiService.GenerateEmbeddingsBatchAsync(batchContents, cancellationToken);

                if (batchEmbeddings.Count == currentBatch.Count)
                {
                    for (var i = 0; i < currentBatch.Count; i++)
                    {
                        var chunk = currentBatch[i];
                        var embedding = batchEmbeddings[i];

                        if (embedding is { Count: > 0 })
                        {
                            chunk.Embedding = embedding;
                            successCount++;
                        }
                        else
                        {

                            var individualEmbedding = await _aiService.GenerateEmbeddingsAsync(chunk.Content, cancellationToken);
                            if (individualEmbedding.Count > 0)
                            {
                                chunk.Embedding = individualEmbedding;
                                successCount++;
                            }
                        }

                        processedChunks++;
                    }
                }
                else
                {
                    using var semaphore = new SemaphoreSlim(1); // Max 1 concurrent
                    var tasks = currentBatch.Select(async chunk =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            var newEmbedding = await _aiService.GenerateEmbeddingsAsync(chunk.Content, cancellationToken);

                            if (newEmbedding.Count > 0)
                            {
                                chunk.Embedding = newEmbedding;
                                Interlocked.Increment(ref successCount);
                            }

                            Interlocked.Increment(ref processedChunks);
                        }
                        catch (Exception ex)
                        {
                            ServiceLogMessages.LogChunkEmbeddingRegenerationFailed(_logger, chunk.Id, ex);
                            Interlocked.Increment(ref processedChunks);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await Task.WhenAll(tasks);
                }

                if (batchIndex < totalBatches - 1) // Don't wait after last batch
                {
                    await Task.Delay(RateLimitDelayMs, cancellationToken);
                }
            }

            var documentsToUpdate = documentChunkMap.Values.Distinct().ToList();

            foreach (var document in documentsToUpdate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _documentRepository.DeleteAsync(document.Id, cancellationToken);
                await _documentRepository.AddAsync(document, cancellationToken);
            }

            ServiceLogMessages.LogEmbeddingRegenerationCompleted(_logger, successCount, processedChunks, null);
            return successCount > 0;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogEmbeddingRegenerationFailed(_logger, ex);
            return false;
        }
    }

    /// <summary>
    /// Clear all embeddings from all documents
    /// </summary>
    public async Task<bool> ClearAllEmbeddingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ServiceLogMessages.LogEmbeddingClearingStarted(_logger, null);

            var allDocuments = await _documentRepository.GetAllAsync(cancellationToken);
            var clearedChunks = 0;

            foreach (var document in allDocuments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var chunk in document.Chunks!)
                {
                    if (chunk.Embedding.Count <= 0) continue;
                    chunk.Embedding.Clear();
                    clearedChunks++;
                }

                await _documentRepository.DeleteAsync(document.Id, cancellationToken);
                await _documentRepository.AddAsync(document, cancellationToken);
            }

            ServiceLogMessages.LogEmbeddingClearingCompleted(_logger, clearedChunks, null);
            return true;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogEmbeddingClearingFailed(_logger, ex);
            return false;
        }
    }

    /// <summary>
    /// Clear all documents and their embeddings
    /// </summary>
    public async Task<bool> ClearAllDocumentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ServiceLogMessages.LogDocumentDeletionStarted(_logger, null);

            var allDocuments = await _documentRepository.GetAllAsync(cancellationToken);
            var totalDocuments = allDocuments.Count;
            var totalChunks = allDocuments.Sum(d => d.Chunks!.Count);
            var success = await _documentRepository.ClearAllAsync(cancellationToken);

            if (success)
            {
                ServiceLogMessages.LogDocumentDeletionCompleted(_logger, totalDocuments, totalChunks, null);
            }

            return success;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogDocumentDeletionFailed(_logger, ex);
            return false;
        }
    }

    private static bool IsAudioDocument(SmartRAG.Entities.Document doc)
    {
        return !string.IsNullOrEmpty(doc.ContentType) &&
               doc.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImageDocument(SmartRAG.Entities.Document doc)
    {
        return !string.IsNullOrEmpty(doc.ContentType) &&
               doc.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTextDocument(SmartRAG.Entities.Document doc)
    {
        return !IsAudioDocument(doc) && !IsImageDocument(doc);
    }

    private static bool IsSchemaDocument(SmartRAG.Entities.Document doc)
    {
        return doc.Metadata.TryGetValue("documentType", out var dt) &&
               string.Equals(dt?.ToString(), "Schema", StringComparison.OrdinalIgnoreCase);
    }
}

