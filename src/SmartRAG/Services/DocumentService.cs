using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    IConfiguration configuration,
    IAIProviderFactory aiProviderFactory,
    SmartRagOptions options,
    ILogger<DocumentService> logger) : IDocumentService
{

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

        logger.LogInformation("Document parsed successfully. Chunks: {ChunkCount}, Total size: {TotalSize} bytes",
            document.Chunks.Count, document.Chunks.Sum(c => c.Content.Length));

        // Generate embeddings for all chunks in batch for better performance
        var allChunkContents = document.Chunks.Select(c => c.Content).ToList();

        logger.LogInformation("Starting batch embedding generation for {ChunkCount} chunks...", allChunkContents.Count);

        // Get the configured AI provider for embeddings
        var providerKey = options.AIProvider.ToString();
        var providerConfig = configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

        if (providerConfig != null && !string.IsNullOrEmpty(providerConfig.ApiKey))
        {
            var aiProvider = aiProviderFactory.CreateProvider(options.AIProvider);

            // Add timeout for large documents
            var embeddingTask = aiProvider.GenerateEmbeddingsBatchAsync(allChunkContents, providerConfig);
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(10));

            var completedTask = await Task.WhenAny(embeddingTask, timeoutTask);
            List<List<float>>? allEmbeddings = null;

            if (completedTask == embeddingTask)
            {
                allEmbeddings = await embeddingTask;
                logger.LogInformation("Batch embedding generation completed successfully.");
            }
            else
            {
                logger.LogWarning("Embedding generation timed out after 10 minutes. Proceeding with partial embeddings.");
            }

            // Apply embeddings to chunks with progress tracking
            var processedChunks = 0;
            var totalChunks = document.Chunks.Count;

            for (int i = 0; i < document.Chunks.Count; i++)
            {
                try
                {
                    var chunk = document.Chunks[i];
                    chunk.DocumentId = document.Id;

                    // Check if embedding was generated successfully
                    if (allEmbeddings != null && i < allEmbeddings.Count && allEmbeddings[i] != null && allEmbeddings[i].Count > 0)
                    {
                        chunk.Embedding = allEmbeddings[i];
                        ServiceLogMessages.LogChunkEmbeddingSuccess(logger, i, allEmbeddings[i].Count, null);
                    }
                    else
                    {
                        // Retry individual embedding generation for this chunk with timeout
                        ServiceLogMessages.LogChunkBatchEmbeddingFailed(logger, i, null);

                        var individualTask = aiProvider.GenerateEmbeddingAsync(chunk.Content, providerConfig);
                        var individualTimeoutTask = Task.Delay(TimeSpan.FromSeconds(30));

                        var individualCompletedTask = await Task.WhenAny(individualTask, individualTimeoutTask);
                        List<float>? individualEmbedding = null;

                        if (individualCompletedTask == individualTask)
                        {
                            individualEmbedding = await individualTask;
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
                        else
                        {
                            logger.LogWarning("Individual embedding generation timed out for chunk {ChunkIndex}", i);
                            chunk.Embedding = []; // Empty but not null
                        }
                    }

                    if (chunk.CreatedAt == default)
                        chunk.CreatedAt = DateTime.UtcNow;

                    processedChunks++;
                    if (processedChunks % 10 == 0 || processedChunks == totalChunks)
                    {
                        logger.LogInformation("Progress: {Processed}/{Total} chunks processed ({Percentage:F1}%)",
                            processedChunks, totalChunks, (processedChunks * 100.0) / totalChunks);
                    }
                }
                catch (Exception ex)
                {
                    ServiceLogMessages.LogChunkProcessingFailed(logger, i, ex);
                    document.Chunks[i].Embedding = []; // Empty but not null
                    processedChunks++;
                }
            }
        }
        else
        {
            logger.LogWarning("No AI provider configuration found for embeddings. Skipping embedding generation.");
            // Set empty embeddings for all chunks
            foreach (var chunk in document.Chunks)
            {
                chunk.Embedding = [];
                chunk.DocumentId = document.Id;
                if (chunk.CreatedAt == default)
                    chunk.CreatedAt = DateTime.UtcNow;
            }
        }

        logger.LogInformation("Starting document save to repository...");

        // Add timeout for repository save operation
        var saveTask = documentRepository.AddAsync(document);
        var saveTimeoutTask = Task.Delay(TimeSpan.FromMinutes(5));

        var saveCompletedTask = await Task.WhenAny(saveTask, saveTimeoutTask);
        Document savedDocument;

        if (saveCompletedTask == saveTask)
        {
            savedDocument = await saveTask;
        }
        else
        {
            logger.LogError("Document save operation timed out after 5 minutes!");
            throw new TimeoutException("Document save operation timed out. The document may be too large.");
        }
        logger.LogInformation("Document uploaded successfully. ID: {DocumentId}, Chunks: {ChunkCount}",
            savedDocument.Id, savedDocument.Chunks.Count);

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

    public async Task<bool> DeleteDocumentAsync(Guid id)
    {
        try
        {
            // Önce document'ı al (embedding'leri temizlemek için)
            var document = await documentRepository.GetByIdAsync(id);
            if (document == null)
            {
                logger.LogWarning("Document not found for deletion: {DocumentId}", id);
                return false;
            }

            // AI provider'dan embedding'leri temizle
            try
            {
                var providerKey = options.AIProvider.ToString();
                var providerConfig = configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

                if (providerConfig != null && !string.IsNullOrEmpty(providerConfig.ApiKey))
                {
                    var aiProvider = aiProviderFactory.CreateProvider(options.AIProvider);
                    await aiProvider.ClearEmbeddingsAsync(document.Chunks);
                    logger.LogDebug("AI provider embeddings cleared for document: {DocumentId}", document.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to clear AI provider embeddings for document: {DocumentId}", document.Id);
            }

            // Chunk'lardaki embedding'leri temizle
            foreach (var chunk in document.Chunks)
            {
                if (chunk.Embedding != null && chunk.Embedding.Count > 0)
                {
                    chunk.Embedding.Clear();
                    chunk.Embedding = null;
                }
            }

            // Document'ı repository'den sil
            var success = await documentRepository.DeleteAsync(id);

            if (success)
            {
                logger.LogInformation("Document deleted successfully: {DocumentId}, {FileName}", id, document.FileName);
            }
            else
            {
                logger.LogWarning("Failed to delete document from repository: {DocumentId}", id);
            }

            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting document: {DocumentId}", id);
            return false;
        }
    }

    public Task<Dictionary<string, object>> GetStorageStatisticsAsync()
    {
        var stats = new Dictionary<string, object>
        {
            ["TotalDocuments"] = documentRepository.GetCountAsync().Result,
            ["DocumentCount"] = documentRepository.GetCountAsync().Result,
            ["StorageProvider"] = options.StorageProvider.ToString(),
            ["MaxChunkSize"] = options.MaxChunkSize,
            ["ChunkOverlap"] = options.ChunkOverlap
        };

        return Task.FromResult(stats);
    }


    /// <summary>
    /// Tüm belgeleri sil (dikkatli kullan!)
    /// </summary>
    public async Task<bool> DeleteAllDocumentsAsync()
    {
        try
        {
            logger.LogWarning("Starting deletion of ALL documents - this action cannot be undone!");

            // Tüm belgeleri al
            var allDocuments = await documentRepository.GetAllAsync();
            var totalDocuments = allDocuments.Count;

            if (totalDocuments == 0)
            {
                logger.LogInformation("No documents found to delete");
                return true;
            }

            logger.LogInformation("Found {DocumentCount} documents to delete", totalDocuments);

            var successCount = 0;
            var failedCount = 0;

            // AI provider'dan tüm embedding'leri temizle (batch işlem)
            try
            {
                var providerKey = options.AIProvider.ToString();
                var providerConfig = configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

                if (providerConfig != null && !string.IsNullOrEmpty(providerConfig.ApiKey))
                {
                    var aiProvider = aiProviderFactory.CreateProvider(options.AIProvider);
                    var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();
                    await aiProvider.ClearEmbeddingsAsync(allChunks);
                    logger.LogInformation("Cleared all embeddings from AI provider");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to clear AI provider embeddings");
            }

            // Her belgeyi tek tek sil
            foreach (var document in allDocuments)
            {
                try
                {
                    logger.LogDebug("Deleting document: {DocumentId}, {FileName}", document.Id, document.FileName);

                    // Chunk'lardaki embedding'leri temizle
                    foreach (var chunk in document.Chunks)
                    {
                        if (chunk.Embedding != null && chunk.Embedding.Count > 0)
                        {
                            chunk.Embedding.Clear();
                            chunk.Embedding = null;
                        }
                    }

                    // Repository'den sil
                    var success = await documentRepository.DeleteAsync(document.Id);

                    if (success)
                    {
                        successCount++;
                        logger.LogDebug("Successfully deleted document: {DocumentId}", document.Id);
                    }
                    else
                    {
                        failedCount++;
                        logger.LogWarning("Failed to delete document from repository: {DocumentId}", document.Id);
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    logger.LogError(ex, "Error deleting document: {DocumentId}", document.Id);
                }
            }

            // Sonuçları logla
            if (failedCount == 0)
            {
                logger.LogInformation("Successfully deleted ALL {SuccessCount} documents", successCount);
            }
            else
            {
                logger.LogWarning("Document deletion completed with errors. Success: {SuccessCount}, Failed: {FailedCount}",
                    successCount, failedCount);
            }

            return successCount > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error during bulk document deletion");
            return false;
        }
    }
}