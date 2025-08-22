using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System.Globalization;
using System.Text;

namespace SmartRAG.Services;

/// <summary>
/// Implementation of document service focused on CRUD operations
/// </summary>
public class DocumentService(
    IDocumentRepository documentRepository,
    IDocumentParserService documentParserService,
    IAIService aiService,
    IOptions<SmartRagOptions> options,
    ILogger<DocumentService> logger) : IDocumentService
{
    #region Constants

    // Batch processing constants
    private const int VoyageAIMaxBatchSize = 128;
    private const int RateLimitDelayMs = 1000;

    // Error messages
    private const string NoFileStreamsMessage = "No file streams provided";
    private const string NoFileNamesMessage = "No file names provided";
    private const string NoContentTypesMessage = "No content types provided";
    private const string MismatchedCountsMessage = "Number of file streams, names, and content types must match";

    // CompositeFormat for repeated formatting (CA1863)
    private static readonly CompositeFormat UnsupportedFileTypeFormat = CompositeFormat.Parse("Unsupported file type: {0}. Supported types: {1}");
    private static readonly CompositeFormat UnsupportedContentTypeFormat = CompositeFormat.Parse("Unsupported content type: {0}. Supported types: {1}");

    #endregion

    #region Fields

    private readonly SmartRagOptions _options = options.Value;

    #endregion

    #region Public Methods

    public async Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy)
    {
        var supportedExtensions = documentParserService.GetSupportedFileTypes();
        var supportedContentTypes = documentParserService.GetSupportedContentTypes();

        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(ext) && !supportedExtensions.Contains(ext))
        {
            var list = string.Join(", ", supportedExtensions);
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, UnsupportedFileTypeFormat, ext, list));
        }

        if (!string.IsNullOrWhiteSpace(contentType) && !supportedContentTypes.Any(ct => contentType.StartsWith(ct, StringComparison.OrdinalIgnoreCase)))
        {
            var list = string.Join(", ", supportedContentTypes);
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, UnsupportedContentTypeFormat, contentType, list));
        }

        var document = await documentParserService.ParseDocumentAsync(fileStream, fileName, contentType, uploadedBy);

        ServiceLogMessages.LogDocumentUploaded(logger, fileName, null);

        // Generate embeddings for all chunks in batch using AI Service
        var allChunkContents = document.Chunks.Select(c => c.Content).ToList();

        ServiceLogMessages.LogBatchEmbeddingAttempt(logger, allChunkContents.Count, null);

        try
        {
            // Delegate embedding generation to AI Service (which will use the appropriate provider)
            var allEmbeddings = await aiService.GenerateEmbeddingsBatchAsync(allChunkContents);

            if (allEmbeddings != null && allEmbeddings.Count == document.Chunks.Count)
            {
                // Apply embeddings to chunks
                for (int i = 0; i < document.Chunks.Count; i++)
                {
                    var chunk = document.Chunks[i];
                    chunk.DocumentId = document.Id;

                    if (allEmbeddings[i] != null && allEmbeddings[i].Count > 0)
                    {
                        chunk.Embedding = allEmbeddings[i];
                        ServiceLogMessages.LogChunkEmbeddingSuccess(logger, i, allEmbeddings[i].Count, null);
                    }
                    else
                    {
                        chunk.Embedding = new List<float>(); // Create empty list
                        ServiceLogMessages.LogChunkEmbeddingFailed(logger, i, null);
                    }

                    if (chunk.CreatedAt == default)
                        chunk.CreatedAt = DateTime.UtcNow;
                }

                ServiceLogMessages.LogBatchEmbeddingSuccess(logger, allChunkContents.Count, null);
            }
            else
            {
                ServiceLogMessages.LogBatchEmbeddingIncomplete(logger, allEmbeddings?.Count ?? 0, document.Chunks.Count, null);

                // Set empty embeddings for all chunks
                foreach (var chunk in document.Chunks)
                {
                    chunk.DocumentId = document.Id;
                    chunk.Embedding = []; // Empty but not null
                    if (chunk.CreatedAt == default)
                        chunk.CreatedAt = DateTime.UtcNow;
                }
            }
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogBatchEmbeddingFailed(logger, ex.Message, ex);

            // Set empty embeddings for all chunks on error
            foreach (var chunk in document.Chunks)
            {
                chunk.DocumentId = document.Id;
                chunk.Embedding = []; // Empty but not null
                if (chunk.CreatedAt == default)
                    chunk.CreatedAt = DateTime.UtcNow;
            }
        }

        ServiceLogMessages.LogDocumentProcessing(logger, fileName, document.Chunks.Count, null);

        // Save document to repository
        var savedDocument = await documentRepository.AddAsync(document);
        ServiceLogMessages.LogDocumentUploaded(logger, fileName, null);

        return savedDocument;
    }

    public async Task<List<Document>> UploadDocumentsAsync(IEnumerable<Stream> fileStreams, IEnumerable<string> fileNames, IEnumerable<string> contentTypes, string uploadedBy)
    {
        if (fileStreams == null || !fileStreams.Any())
            throw new ArgumentException(NoFileStreamsMessage, nameof(fileStreams));

        if (fileNames == null || !fileNames.Any())
            throw new ArgumentException(NoFileNamesMessage, nameof(fileNames));

        if (contentTypes == null || !contentTypes.Any())
            throw new ArgumentException(NoContentTypesMessage, nameof(contentTypes));

        var streamList = fileStreams.ToList();
        var nameList = fileNames.ToList();
        var typeList = contentTypes.ToList();

        if (streamList.Count != nameList.Count || streamList.Count != typeList.Count)
            throw new ArgumentException(MismatchedCountsMessage);

        var uploadedDocuments = new List<Document>();

        // Parallel document upload for better performance
        var uploadTasks = streamList.Select(async (stream, index) =>
        {
            try
            {
                return await UploadDocumentAsync(stream, nameList[index], typeList[index], uploadedBy);
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogDocumentUploadFailed(logger, nameList[index], ex);
                return null;
            }
        });

        var uploadResults = await Task.WhenAll(uploadTasks);

        uploadedDocuments.AddRange(uploadResults.Where(doc => doc != null)!);

        return uploadedDocuments;
    }

    public async Task<Document?> GetDocumentAsync(Guid id) => await documentRepository.GetByIdAsync(id);

    public async Task<List<Document>> GetAllDocumentsAsync() => await documentRepository.GetAllAsync();

    public async Task<bool> DeleteDocumentAsync(Guid id) => await documentRepository.DeleteAsync(id);

    public Task<Dictionary<string, object>> GetStorageStatisticsAsync()
    {
        var stats = new Dictionary<string, object>
        {
            ["TotalDocuments"] = documentRepository.GetCountAsync().Result,
            ["DocumentCount"] = documentRepository.GetCountAsync().Result,
            ["StorageProvider"] = _options.StorageProvider.ToString(),
            ["MaxChunkSize"] = _options.MaxChunkSize,
            ["ChunkOverlap"] = _options.ChunkOverlap
        };

        return Task.FromResult(stats);
    }

    public async Task<bool> RegenerateAllEmbeddingsAsync()
    {
        try
        {
            ServiceLogMessages.LogEmbeddingRegenerationStarted(logger, null);

            var allDocuments = await documentRepository.GetAllAsync();
            var totalChunks = allDocuments.Sum(d => d.Chunks.Count);
            var processedChunks = 0;
            var successCount = 0;

            // Collect all chunks that need embedding regeneration
            var chunksToProcess = new List<DocumentChunk>();
            var documentChunkMap = new Dictionary<DocumentChunk, Document>();

            foreach (var document in allDocuments)
            {
                ServiceLogMessages.LogDocumentProcessing(logger, document.FileName, document.Chunks.Count, null);

                foreach (var chunk in document.Chunks)
                {
                    // Skip if embedding already exists and is valid
                    if (chunk.Embedding != null && chunk.Embedding.Count > 0)
                    {
                        processedChunks++;
                        continue;
                    }

                    chunksToProcess.Add(chunk);
                    documentChunkMap[chunk] = document;
                }
            }

            ServiceLogMessages.LogTotalChunksToProcess(logger, chunksToProcess.Count, totalChunks, null);

            if (chunksToProcess.Count == 0)
            {
                ServiceLogMessages.LogNoProcessingNeeded(logger, null);
                return true;
            }

            // Process chunks in batches of 128 (VoyageAI max batch size)
            var totalBatches = (int)Math.Ceiling((double)chunksToProcess.Count / VoyageAIMaxBatchSize);

            ServiceLogMessages.LogBatchProcessing(logger, totalBatches, null);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var startIndex = batchIndex * VoyageAIMaxBatchSize;
                var endIndex = Math.Min(startIndex + VoyageAIMaxBatchSize, chunksToProcess.Count);
                var currentBatch = chunksToProcess.Skip(startIndex).Take(endIndex - startIndex).ToList();

                ServiceLogMessages.LogBatchProgress(logger, batchIndex + 1, totalBatches, null);

                // Generate embeddings for current batch
                var batchContents = currentBatch.Select(c => c.Content).ToList();
                var batchEmbeddings = await aiService.GenerateEmbeddingsBatchAsync(batchContents);

                if (batchEmbeddings != null && batchEmbeddings.Count == currentBatch.Count)
                {
                    // Apply embeddings to chunks
                    for (int i = 0; i < currentBatch.Count; i++)
                    {
                        var chunk = currentBatch[i];
                        var embedding = batchEmbeddings[i];

                        if (embedding != null && embedding.Count > 0)
                        {
                            chunk.Embedding = embedding;
                            successCount++;
                            ServiceLogMessages.LogChunkBatchEmbeddingSuccess(logger, i, embedding.Count, null);
                        }
                        else
                        {
                            ServiceLogMessages.LogChunkBatchEmbeddingFailedRetry(logger, chunk.Id, null);

                            // Fallback to individual generation
                            var individualEmbedding = await aiService.GenerateEmbeddingsAsync(chunk.Content);
                            if (individualEmbedding != null && individualEmbedding.Count > 0)
                            {
                                chunk.Embedding = individualEmbedding;
                                successCount++;
                                ServiceLogMessages.LogChunkIndividualEmbeddingSuccessRetry(logger, chunk.Id, individualEmbedding.Count, null);
                            }
                            else
                            {
                                ServiceLogMessages.LogChunkAllEmbeddingMethodsFailed(logger, chunk.Id, null);
                            }
                        }

                        processedChunks++;
                    }
                }
                else
                {
                    ServiceLogMessages.LogBatchFailed(logger, batchIndex + 1, null);

                    // Process chunks individually if batch fails
                    foreach (var chunk in currentBatch)
                    {
                        try
                        {
                            var newEmbedding = await aiService.GenerateEmbeddingsAsync(chunk.Content);

                            if (newEmbedding != null && newEmbedding.Count > 0)
                            {
                                chunk.Embedding = newEmbedding;
                                successCount++;
                                ServiceLogMessages.LogChunkIndividualEmbeddingSuccessFinal(logger, chunk.Id, newEmbedding.Count, null);
                            }
                            else
                            {
                                ServiceLogMessages.LogChunkEmbeddingGenerationFailed(logger, chunk.Id, null);
                            }

                            processedChunks++;
                        }
                        catch (Exception ex)
                        {
                            ServiceLogMessages.LogChunkEmbeddingRegenerationFailed(logger, chunk.Id, ex);
                            processedChunks++;
                        }
                    }
                }

                // Progress update
                ServiceLogMessages.LogProgress(logger, processedChunks, chunksToProcess.Count, successCount, null);

                // Smart rate limiting
                if (batchIndex < totalBatches - 1) // Don't wait after last batch
                {
                    await Task.Delay(RateLimitDelayMs);
                }
            }

            // Save all documents with updated embeddings
            var documentsToUpdate = documentChunkMap.Values.Distinct().ToList();
            ServiceLogMessages.LogSavingDocuments(logger, documentsToUpdate.Count, null);

            foreach (var document in documentsToUpdate)
            {
                await documentRepository.DeleteAsync(document.Id);
                await documentRepository.AddAsync(document);
            }

            ServiceLogMessages.LogEmbeddingRegenerationCompleted(logger, successCount, processedChunks, null);
            return successCount > 0;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogEmbeddingRegenerationFailed(logger, ex);
            return false;
        }
    }

    /// <summary>
    /// Clear all embeddings from all documents
    /// </summary>
    public async Task<bool> ClearAllEmbeddingsAsync()
    {
        try
        {
            ServiceLogMessages.LogEmbeddingClearingStarted(logger, null);

            var allDocuments = await documentRepository.GetAllAsync();
            var totalChunks = allDocuments.Sum(d => d.Chunks.Count);
            var clearedChunks = 0;

            foreach (var document in allDocuments)
            {
                ServiceLogMessages.LogDocumentProcessing(logger, document.FileName, document.Chunks.Count, null);

                foreach (var chunk in document.Chunks)
                {
                    if (chunk.Embedding != null && chunk.Embedding.Count > 0)
                    {
                        chunk.Embedding.Clear();
                        clearedChunks++;
                    }
                }

                // Save document with cleared embeddings
                await documentRepository.DeleteAsync(document.Id);
                await documentRepository.AddAsync(document);
            }

            ServiceLogMessages.LogEmbeddingClearingCompleted(logger, clearedChunks, null);
            return true;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogEmbeddingClearingFailed(logger, ex);
            return false;
        }
    }

    /// <summary>
    /// Clear all documents and their embeddings
    /// </summary>
    public async Task<bool> ClearAllDocumentsAsync()
    {
        try
        {
            ServiceLogMessages.LogDocumentDeletionStarted(logger, null);

            var allDocuments = await documentRepository.GetAllAsync();
            var totalDocuments = allDocuments.Count;
            var totalChunks = allDocuments.Sum(d => d.Chunks.Count);

            // Delete all documents (this will also clear their embeddings)
            foreach (var document in allDocuments)
            {
                await documentRepository.DeleteAsync(document.Id);
            }

            ServiceLogMessages.LogDocumentDeletionCompleted(logger, totalDocuments, totalChunks, null);
            return true;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogDocumentDeletionFailed(logger, ex);
            return false;
        }
    }

    #endregion
}