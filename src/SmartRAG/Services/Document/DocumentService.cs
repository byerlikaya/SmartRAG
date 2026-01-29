#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces.AI;
using SmartRAG.Interfaces.Document;
using SmartRAG.Models;
using SmartRAG.Models.RequestResponse;
using SmartRAG.Services.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRAG.Services.Document
{

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
        public async Task<SmartRAG.Entities.Document> UploadDocumentAsync(Models.RequestResponse.UploadDocumentRequest request, CancellationToken cancellationToken = default)
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

            var document = await _documentParserService.ParseDocumentAsync(request.FileStream, request.FileName, request.ContentType, request.UploadedBy, request.Language);

            if (request.FileSize.HasValue && request.FileSize.Value > 0)
            {
                document.FileSize = request.FileSize.Value;
            }

            if (request.AdditionalMetadata != null && request.AdditionalMetadata.Count > 0)
            {
                document.Metadata ??= new Dictionary<string, object>();
                foreach (var item in request.AdditionalMetadata)
                {
                    document.Metadata[item.Key] = item.Value;
                }
            }

            var allChunkContents = document.Chunks.Select(c => c.Content).ToList();

            var successCount = 0;

            try
            {
                var allEmbeddings = await _aiService.GenerateEmbeddingsBatchAsync(allChunkContents, cancellationToken);

                if (allEmbeddings != null && allEmbeddings.Count == document.Chunks.Count)
                {
                    for (int i = 0; i < document.Chunks.Count; i++)
                    {
                        var chunk = document.Chunks[i];
                        chunk.DocumentId = document.Id;

                        if (allEmbeddings[i] != null && allEmbeddings[i].Count > 0)
                        {
                            chunk.Embedding = allEmbeddings[i];
                            successCount++;
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

                    foreach (var chunk in document.Chunks)
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

                foreach (var chunk in document.Chunks)
                {
                    chunk.DocumentId = document.Id;
                    chunk.Embedding = new List<float>(); // Empty but not null
                    if (chunk.CreatedAt == default)
                        chunk.CreatedAt = DateTime.UtcNow;
                }
            }

            var savedDocument = await _documentRepository.AddAsync(document, cancellationToken);
            ServiceLogMessages.LogDocumentUploaded(_logger, request.FileName, null);

            return savedDocument;
        }

        /// <summary>
        /// [AI Query] [Document Query] Uploads a document, generates embeddings, and saves it
        /// </summary>
        /// <param name="fileStream">File stream containing document content</param>
        /// <param name="fileName">Name of the file</param>
        /// <param name="contentType">MIME content type of the file</param>
        /// <param name="uploadedBy">Identifier of the user uploading the document</param>
        /// <param name="language">Language code for document processing (optional)</param>
        /// <param name="fileSize">File size in bytes (optional, will be calculated from stream if not provided)</param>
        /// <param name="additionalMetadata">Additional metadata to add to document (optional)</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Created document entity</returns>
        [Obsolete("Use UploadDocumentAsync(UploadDocumentRequest) instead. This method will be removed in v4.0.0")]
        public async Task<SmartRAG.Entities.Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy, string? language = null, long? fileSize = null, Dictionary<string, object>? additionalMetadata = null, CancellationToken cancellationToken = default)
        {
            var request = new Models.RequestResponse.UploadDocumentRequest
            {
                FileStream = fileStream,
                FileName = fileName,
                ContentType = contentType,
                UploadedBy = uploadedBy,
                Language = language,
                FileSize = fileSize,
                AdditionalMetadata = additionalMetadata
            };
            return await UploadDocumentAsync(request, cancellationToken);
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
        public async Task<List<SmartRAG.Entities.Document>> GetAllDocumentsFilteredAsync(Models.SearchOptions? options, CancellationToken cancellationToken = default)
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
                var totalChunks = allDocuments.Sum(d => d.Chunks.Count);
                var processedChunks = 0;
                var successCount = 0;

                var chunksToProcess = new List<DocumentChunk>();
                var documentChunkMap = new Dictionary<DocumentChunk, SmartRAG.Entities.Document>();

                foreach (var document in allDocuments)
                {
                    foreach (var chunk in document.Chunks)
                    {
                        if (chunk.Embedding != null && chunk.Embedding.Count > 0)
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

                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var startIndex = batchIndex * VoyageAIMaxBatchSize;
                    var endIndex = Math.Min(startIndex + VoyageAIMaxBatchSize, chunksToProcess.Count);
                    var currentBatch = chunksToProcess.Skip(startIndex).Take(endIndex - startIndex).ToList();

                    var batchContents = currentBatch.Select(c => c.Content).ToList();
                    var batchEmbeddings = await _aiService.GenerateEmbeddingsBatchAsync(batchContents, cancellationToken);

                    if (batchEmbeddings != null && batchEmbeddings.Count == currentBatch.Count)
                    {
                        for (int i = 0; i < currentBatch.Count; i++)
                        {
                            var chunk = currentBatch[i];
                            var embedding = batchEmbeddings[i];

                            if (embedding != null && embedding.Count > 0)
                            {
                                chunk.Embedding = embedding;
                                successCount++;
                            }
                            else
                            {

                                var individualEmbedding = await _aiService.GenerateEmbeddingsAsync(chunk.Content, cancellationToken);
                                if (individualEmbedding != null && individualEmbedding.Count > 0)
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
                        using var semaphore = new System.Threading.SemaphoreSlim(1); // Max 1 concurrent
                        var tasks = currentBatch.Select(async chunk =>
                        {
                            await semaphore.WaitAsync(cancellationToken);
                            try
                            {
                                var newEmbedding = await _aiService.GenerateEmbeddingsAsync(chunk.Content, cancellationToken);

                                if (newEmbedding != null && newEmbedding.Count > 0)
                                {
                                    chunk.Embedding = newEmbedding;
                                    System.Threading.Interlocked.Increment(ref successCount);
                                }

                                System.Threading.Interlocked.Increment(ref processedChunks);
                            }
                            catch (Exception ex)
                            {
                                ServiceLogMessages.LogChunkEmbeddingRegenerationFailed(_logger, chunk.Id, ex);
                                System.Threading.Interlocked.Increment(ref processedChunks);
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
                var totalChunks = allDocuments.Sum(d => d.Chunks.Count);
                var clearedChunks = 0;

                foreach (var document in allDocuments)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    foreach (var chunk in document.Chunks)
                    {
                        if (chunk.Embedding != null && chunk.Embedding.Count > 0)
                        {
                            chunk.Embedding.Clear();
                            clearedChunks++;
                        }
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
                var totalChunks = allDocuments.Sum(d => d.Chunks.Count);
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

        private bool IsAudioDocument(SmartRAG.Entities.Document doc)
        {
            return !string.IsNullOrEmpty(doc.ContentType) &&
                   doc.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsImageDocument(SmartRAG.Entities.Document doc)
        {
            return !string.IsNullOrEmpty(doc.ContentType) &&
                   doc.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsTextDocument(SmartRAG.Entities.Document doc)
        {
            return !IsAudioDocument(doc) && !IsImageDocument(doc);
        }

        private static bool IsSchemaDocument(SmartRAG.Entities.Document doc)
        {
            return doc.Metadata != null &&
                   doc.Metadata.TryGetValue("documentType", out var dt) &&
                   string.Equals(dt?.ToString(), "Schema", StringComparison.OrdinalIgnoreCase);
        }
    }
}
