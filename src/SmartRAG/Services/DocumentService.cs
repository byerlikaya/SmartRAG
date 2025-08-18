using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using SmartRAG.Services.Logging;

namespace SmartRAG.Services;

/// <summary>
/// Implementation of document service focused on CRUD operations
/// </summary>
public class DocumentService(
    IDocumentRepository documentRepository,
    IDocumentParserService documentParserService,
    IDocumentSearchService documentSearchService,
    IOptions<SmartRagOptions> options,
    ILogger<DocumentService> logger) : IDocumentService
{
    private readonly SmartRagOptions _options = options.Value;

    public async Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy)
    {
        var supportedExtensions = documentParserService.GetSupportedFileTypes();
        var supportedContentTypes = documentParserService.GetSupportedContentTypes();

        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(ext) && !supportedExtensions.Contains(ext))
        {
            var list = string.Join(", ", supportedExtensions);
            throw new ArgumentException($"Unsupported file type: {ext}. Supported types: {list}");
        }

        if (!string.IsNullOrWhiteSpace(contentType) && !supportedContentTypes.Any(ct => contentType.StartsWith(ct, StringComparison.OrdinalIgnoreCase)))
        {
            var list = string.Join(", ", supportedContentTypes);
            throw new ArgumentException($"Unsupported content type: {contentType}. Supported types: {list}");
        }

        var document = await documentParserService.ParseDocumentAsync(fileStream, fileName, contentType, uploadedBy);

        // Generate embeddings for all chunks in batch for better performance
        var allChunkContents = document.Chunks.Select(c => c.Content).ToList();
        var allEmbeddings = await documentSearchService.GenerateEmbeddingsBatchAsync(allChunkContents);

        // Apply embeddings to chunks with retry mechanism
        for (int i = 0; i < document.Chunks.Count; i++)
        {
            try
            {
                var chunk = document.Chunks[i];
                // Ensure chunk metadata is consistent
                chunk.DocumentId = document.Id;
                
                // Check if embedding was generated successfully
                if (allEmbeddings != null && i < allEmbeddings.Count && allEmbeddings[i] != null && allEmbeddings[i].Count > 0)
                {
                    chunk.Embedding = allEmbeddings[i];
                    ServiceLogMessages.LogChunkEmbeddingSuccess(logger, i, allEmbeddings[i].Count, null);
                }
                else
                {
                    // Retry individual embedding generation for this chunk
                    ServiceLogMessages.LogChunkBatchEmbeddingFailed(logger, i, null);
                    var individualEmbedding = await documentSearchService.GenerateEmbeddingWithFallbackAsync(chunk.Content);
                    
                    if (individualEmbedding != null && individualEmbedding.Count > 0)
                    {
                        chunk.Embedding = individualEmbedding;
                        ServiceLogMessages.LogChunkIndividualEmbeddingSuccess(logger, i, individualEmbedding.Count, null);
                    }
                    else
                    {
                        ServiceLogMessages.LogChunkEmbeddingFailed(logger, i, null);
                        chunk.Embedding = []; // Empty but not null
                    }
                }
                
                if (chunk.CreatedAt == default)
                    chunk.CreatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogChunkProcessingFailed(logger, i, ex);
                // If embedding generation fails, leave it empty and continue
                document.Chunks[i].Embedding = []; // Empty but not null
            }
        }

        var savedDocument = await documentRepository.AddAsync(document);

        return savedDocument;
    }

    public async Task<List<Document>> UploadDocumentsAsync(IEnumerable<Stream> fileStreams, IEnumerable<string> fileNames, IEnumerable<string> contentTypes, string uploadedBy)
    {
        if (fileStreams == null || !fileStreams.Any())
            throw new ArgumentException("No file streams provided", nameof(fileStreams));

        if (fileNames == null || !fileNames.Any())
            throw new ArgumentException("No file names provided", nameof(fileNames));

        if (contentTypes == null || !contentTypes.Any())
            throw new ArgumentException("No content types provided", nameof(contentTypes));

        var streamList = fileStreams.ToList();
        var nameList = fileNames.ToList();
        var typeList = contentTypes.ToList();

        if (streamList.Count != nameList.Count || streamList.Count != typeList.Count)
            throw new ArgumentException("Number of file streams, names, and content types must match");

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
            const int batchSize = 128;
            var totalBatches = (int)Math.Ceiling((double)chunksToProcess.Count / batchSize);
            
            ServiceLogMessages.LogBatchProcessing(logger, totalBatches, null);
            
            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var startIndex = batchIndex * batchSize;
                var endIndex = Math.Min(startIndex + batchSize, chunksToProcess.Count);
                var currentBatch = chunksToProcess.Skip(startIndex).Take(endIndex - startIndex).ToList();
                
                ServiceLogMessages.LogBatchProgress(logger, batchIndex + 1, totalBatches, null);
                
                // Generate embeddings for current batch
                var batchContents = currentBatch.Select(c => c.Content).ToList();
                var batchEmbeddings = await documentSearchService.GenerateEmbeddingsBatchAsync(batchContents);
                
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
                            var individualEmbedding = await documentSearchService.GenerateEmbeddingWithFallbackAsync(chunk.Content);
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
                            var newEmbedding = await documentSearchService.GenerateEmbeddingWithFallbackAsync(chunk.Content);
                            
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
                    await Task.Delay(1000); // Simple rate limiting
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
}