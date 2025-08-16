
using Microsoft.Extensions.Configuration;
using SmartRAG.Entities;
using SmartRAG.Enums;
using SmartRAG.Factories;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System.Text.Json;

namespace SmartRAG.Services;

/// <summary>
/// Implementation of document service with enhanced semantic search using repository pattern
/// </summary>
public class DocumentService(
    IDocumentRepository documentRepository,
    IDocumentParserService documentParserService,
    IAIService aiService,
    SmartRagOptions options,
    IAIProviderFactory aiProviderFactory,
    IConfiguration configuration) : IDocumentService
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

        // Generate embeddings for all chunks in batch for better performance
        var allChunkContents = document.Chunks.Select(c => c.Content).ToList();
        var allEmbeddings = await TryGenerateEmbeddingsBatchAsync(allChunkContents);

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
                    Console.WriteLine($"[DEBUG] Chunk {i}: Embedding generated successfully ({allEmbeddings[i].Count} dimensions)");
                }
                else
                {
                    // Retry individual embedding generation for this chunk
                    Console.WriteLine($"[DEBUG] Chunk {i}: Batch embedding failed, trying individual generation");
                    var individualEmbedding = await TryGenerateEmbeddingWithFallback(chunk.Content);
                    
                    if (individualEmbedding != null && individualEmbedding.Count > 0)
                    {
                        chunk.Embedding = individualEmbedding;
                        Console.WriteLine($"[DEBUG] Chunk {i}: Individual embedding successful ({individualEmbedding.Count} dimensions)");
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] Chunk {i}: Failed to generate embedding after retry");
                        chunk.Embedding = new List<float>(); // Empty but not null
                    }
                }
                
                if (chunk.CreatedAt == default)
                    chunk.CreatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Chunk {i}: Failed to process: {ex.Message}");
                // If embedding generation fails, leave it empty and continue
                document.Chunks[i].Embedding = new List<float>(); // Empty but not null
            }
        }

        var savedDocument = await documentRepository.AddAsync(document);

        return savedDocument;
    }

    public async Task<Document?> GetDocumentAsync(Guid id) => await documentRepository.GetByIdAsync(id);

    public async Task<List<Document>> GetAllDocumentsAsync() => await documentRepository.GetAllAsync();

    public async Task<bool> DeleteDocumentAsync(Guid id) => await documentRepository.DeleteAsync(id);

    public async Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(query));

        try
        {
            // Use EnhancedSearchService with Semantic Kernel for better search
            var enhancedSearchService = new EnhancedSearchService(aiProviderFactory, documentRepository, configuration);
            var enhancedResults = await enhancedSearchService.EnhancedSemanticSearchAsync(query, maxResults * 2);
            
            if (enhancedResults.Count > 0)
            {
                Console.WriteLine($"[DEBUG] EnhancedSearchService returned {enhancedResults.Count} chunks from {enhancedResults.Select(c => c.DocumentId).Distinct().Count()} documents");
                
                // Apply diversity selection to ensure chunks from different documents
                var diverseResults = ApplyDiversityAndSelect(enhancedResults, maxResults);
                
                Console.WriteLine($"[DEBUG] Final diverse results: {diverseResults.Count} chunks from {diverseResults.Select(c => c.DocumentId).Distinct().Count()} documents");
                
                return diverseResults;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] EnhancedSearchService failed: {ex.Message}. Falling back to basic search.");
        }

        // Fallback to basic search if Semantic Kernel fails
        return await PerformBasicSearchAsync(query, maxResults);
    }

    /// <summary>
    /// Basic search fallback when Semantic Kernel is not available
    /// </summary>
    private async Task<List<DocumentChunk>> PerformBasicSearchAsync(string query, int maxResults)
    {
        var cleanedQuery = query;
        var allDocs = await documentRepository.GetAllAsync();

        // Fix any chunks with missing DocumentId
        foreach (var doc in allDocs)
        {
            foreach (var chunk in doc.Chunks)
            {
                if (chunk.DocumentId == Guid.Empty)
                    chunk.DocumentId = doc.Id;
            }
        }

        var allResults = new List<DocumentChunk>();

        try
        {
            // Try embedding generation
            var queryEmbedding = await TryGenerateEmbeddingWithFallback(cleanedQuery);
            if (queryEmbedding != null && queryEmbedding.Count > 0)
            {
                var vecScored = new List<(DocumentChunk chunk, double score)>();
                foreach (var doc in allDocs)
                {
                    foreach (var chunk in doc.Chunks)
                    {
                        if (chunk.Embedding != null && chunk.Embedding.Count > 0)
                        {
                            var score = ComputeCosineSimilarity(queryEmbedding, chunk.Embedding);
                            vecScored.Add((chunk, score));
                        }
                    }
                }

                var semanticResults = vecScored
                    .OrderByDescending(x => x.score)
                    .Take(maxResults * 2)
                    .Select(x => { x.chunk.RelevanceScore = x.score; return x.chunk; })
                    .ToList();

                allResults.AddRange(semanticResults);
            }
        }
        catch
        {
            // Continue with other search methods
        }

        // Repository search
        var primary = await documentRepository.SearchAsync(cleanedQuery, maxResults * 2);
        allResults.AddRange(primary);

        // Fuzzy search if needed
        if (allResults.Count < maxResults)
        {
            var fuzzyResults = await PerformFuzzySearch(cleanedQuery, maxResults);
            allResults.AddRange(fuzzyResults.Where(f => !allResults.Any(p => p.Id == f.Id)));
        }

        // Remove duplicates and ensure diversity
        var uniqueResults = allResults
            .GroupBy(c => c.Id)
            .Select(g => g.OrderByDescending(c => c.RelevanceScore ?? 0.0).First())
            .ToList();

        return ApplyDiversityAndSelect(uniqueResults, maxResults);
    }

    private async Task<List<float>?> TryGenerateEmbeddingWithFallback(string text)
    {
        try
        {
            Console.WriteLine($"[DEBUG] Trying primary AI service for embedding generation");
            var result = await aiService.GenerateEmbeddingsAsync(text);
            if (result != null && result.Count > 0)
            {
                Console.WriteLine($"[DEBUG] Primary AI service successful: {result.Count} dimensions");
                return result;
            }
            Console.WriteLine($"[DEBUG] Primary AI service returned null or empty embedding");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Primary AI service failed: {ex.Message}");
        }

        var embeddingProviders = new[]
        {
            "Anthropic",
            "OpenAI",
            "Gemini"
        };

        foreach (var provider in embeddingProviders)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Trying {provider} provider for embedding generation");
                var providerEnum = Enum.Parse<AIProvider>(provider);
                var aiProvider = ((AIProviderFactory)aiProviderFactory).CreateProvider(providerEnum);
                var providerConfig = configuration.GetSection($"AI:{provider}").Get<AIProviderConfig>();

                if (providerConfig != null && !string.IsNullOrEmpty(providerConfig.ApiKey))
                {
                    Console.WriteLine($"[DEBUG] {provider} config found, API key: {providerConfig.ApiKey.Substring(0, 8)}...");
                    var embedding = await aiProvider.GenerateEmbeddingAsync(text, providerConfig);
                    if (embedding != null && embedding.Count > 0)
                    {
                        Console.WriteLine($"[DEBUG] {provider} successful: {embedding.Count} dimensions");
                        return embedding;
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] {provider} returned null or empty embedding");
                    }
                }
                else
                {
                    Console.WriteLine($"[DEBUG] {provider} config not found or API key missing");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] {provider} failed: {ex.Message}");
                continue;
            }
        }

        Console.WriteLine($"[DEBUG] All embedding providers failed for text: {text.Substring(0, Math.Min(50, text.Length))}...");
        
        // Special test for VoyageAI if Anthropic is configured
        try
        {
            var anthropicConfig = configuration.GetSection("AI:Anthropic").Get<AIProviderConfig>();
            if (anthropicConfig != null && !string.IsNullOrEmpty(anthropicConfig.EmbeddingApiKey))
            {
                Console.WriteLine($"[DEBUG] Testing VoyageAI directly with key: {anthropicConfig.EmbeddingApiKey.Substring(0, 8)}...");
                
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {anthropicConfig.EmbeddingApiKey}");
                
                var testPayload = new
                {
                    input = new[] { "test" },
                    model = anthropicConfig.EmbeddingModel ?? "voyage-3.5",
                    input_type = "document"
                };
                
                var jsonContent = System.Text.Json.JsonSerializer.Serialize(testPayload);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync("https://api.voyageai.com/v1/embeddings", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"[DEBUG] VoyageAI test response: {response.StatusCode} - {responseContent}");
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DEBUG] VoyageAI is working! Trying to parse embedding...");
                    // Parse the response and return a test embedding
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
                        if (doc.RootElement.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                        {
                            var firstEmbedding = dataArray.EnumerateArray().FirstOrDefault();
                            if (firstEmbedding.TryGetProperty("embedding", out var embeddingArray) && embeddingArray.ValueKind == JsonValueKind.Array)
                            {
                                var testEmbedding = embeddingArray.EnumerateArray()
                                    .Select(x => x.GetSingle())
                                    .ToList();
                                Console.WriteLine($"[DEBUG] VoyageAI test embedding generated: {testEmbedding.Count} dimensions");
                                return testEmbedding;
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"[DEBUG] Failed to parse VoyageAI response: {parseEx.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] VoyageAI direct test failed: {ex.Message}");
        }
        
        return null;
    }

    /// <summary>
    /// Generates embeddings for multiple texts in batch for better performance
    /// </summary>
    private async Task<List<List<float>>?> TryGenerateEmbeddingsBatchAsync(List<string> texts)
    {
        if (texts == null || texts.Count == 0)
            return null;

        try
        {
            // Try batch embedding generation first
            var batchEmbeddings = await aiService.GenerateEmbeddingsBatchAsync(texts);
            if (batchEmbeddings != null && batchEmbeddings.Count == texts.Count)
                return batchEmbeddings;
        }
        catch
        {
            // Fallback to individual generation if batch fails
        }

        // Special handling for VoyageAI: Process in smaller batches to respect 3 RPM limit
        try
        {
            var anthropicConfig = configuration.GetSection("AI:Anthropic").Get<AIProviderConfig>();
            if (anthropicConfig != null && !string.IsNullOrEmpty(anthropicConfig.EmbeddingApiKey))
            {
                Console.WriteLine($"[DEBUG] Trying VoyageAI batch processing with rate limiting...");
                
                // Process in smaller batches (3 chunks per minute = 20 seconds between batches)
                const int rateLimitBatchSize = 3;
                var allEmbeddings = new List<List<float>>();
                
                for (int i = 0; i < texts.Count; i += rateLimitBatchSize)
                {
                    var currentBatch = texts.Skip(i).Take(rateLimitBatchSize).ToList();
                    Console.WriteLine($"[DEBUG] Processing VoyageAI batch {i/rateLimitBatchSize + 1}: chunks {i+1}-{Math.Min(i+rateLimitBatchSize, texts.Count)}");
                    
                    // Generate embeddings for current batch using VoyageAI
                    var batchEmbeddings = await GenerateVoyageAIBatchAsync(currentBatch, anthropicConfig);
                    
                    if (batchEmbeddings != null && batchEmbeddings.Count == currentBatch.Count)
                    {
                        allEmbeddings.AddRange(batchEmbeddings);
                        Console.WriteLine($"[DEBUG] VoyageAI batch {i/rateLimitBatchSize + 1} successful: {batchEmbeddings.Count} embeddings");
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] VoyageAI batch {i/rateLimitBatchSize + 1} failed, using individual fallback");
                        // Fallback to individual generation for this batch
                        var individualEmbeddings = await GenerateIndividualEmbeddingsAsync(currentBatch);
                        allEmbeddings.AddRange(individualEmbeddings);
                    }
                    
                    // Smart rate limiting: Detect if we hit rate limits and adjust
                    if (i + rateLimitBatchSize < texts.Count)
                    {
                        // Check if we got rate limited in the last batch
                        var lastBatchSuccess = batchEmbeddings != null && batchEmbeddings.Count > 0;
                        
                        if (!lastBatchSuccess)
                        {
                            // Rate limited - wait 20 seconds for 3 RPM
                            Console.WriteLine($"[INFO] Rate limit detected, waiting 20 seconds for 3 RPM limit...");
                            await Task.Delay(20000);
                        }
                        else
                        {
                            // No rate limit - continue at full speed (2000 RPM)
                            Console.WriteLine($"[INFO] No rate limit detected, continuing at full speed (2000 RPM)");
                        }
                    }
                }
                
                if (allEmbeddings.Count == texts.Count)
                {
                    Console.WriteLine($"[DEBUG] VoyageAI batch processing completed: {allEmbeddings.Count} embeddings");
                    return allEmbeddings;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] VoyageAI batch processing failed: {ex.Message}");
        }

        // Final fallback: generate embeddings individually (but still in parallel)
        Console.WriteLine($"[DEBUG] Falling back to individual embedding generation for {texts.Count} chunks");
        var embeddingTasks = texts.Select(async text => await TryGenerateEmbeddingWithFallback(text)).ToList();
        var embeddings = await Task.WhenAll(embeddingTasks);
        
        return embeddings.Where(e => e != null).Select(e => e!).ToList();
    }
    
    /// <summary>
    /// Generates embeddings for a batch using VoyageAI directly
    /// </summary>
    private async Task<List<List<float>>?> GenerateVoyageAIBatchAsync(List<string> texts, AIProviderConfig config)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.EmbeddingApiKey}");
            
            var payload = new
            {
                input = texts,
                model = config.EmbeddingModel ?? "voyage-3.5",
                input_type = "document"
            };
            
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync("https://api.voyageai.com/v1/embeddings", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return ParseVoyageAIBatchResponse(responseContent);
            }
            else
            {
                Console.WriteLine($"[DEBUG] VoyageAI batch request failed: {response.StatusCode} - {responseContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] VoyageAI batch generation failed: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Parses VoyageAI batch response
    /// </summary>
    private static List<List<float>>? ParseVoyageAIBatchResponse(string response)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(response);
            
            if (doc.RootElement.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
            {
                var embeddings = new List<List<float>>();
                
                foreach (var item in dataArray.EnumerateArray())
                {
                    if (item.TryGetProperty("embedding", out var embeddingArray) && embeddingArray.ValueKind == JsonValueKind.Array)
                    {
                        var embedding = embeddingArray.EnumerateArray()
                            .Select(x => x.GetSingle())
                            .ToList();
                        embeddings.Add(embedding);
                    }
                }
                
                return embeddings.Count > 0 ? embeddings : null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Failed to parse VoyageAI batch response: {ex.Message}");
        }
        
        return null;
    }
    
    /// <summary>
    /// Generates embeddings individually for a batch as fallback
    /// </summary>
    private async Task<List<List<float>>> GenerateIndividualEmbeddingsAsync(List<string> texts)
    {
        var embeddings = new List<List<float>>();
        
        foreach (var text in texts)
        {
            var embedding = await TryGenerateEmbeddingWithFallback(text);
            embeddings.Add(embedding ?? new List<float>());
        }
        
        return embeddings;
    }

    private static double ComputeCosineSimilarity(List<float> a, List<float> b)
    {
        if (a == null || b == null) return 0.0;
        int n = Math.Min(a.Count, b.Count);
        if (n == 0) return 0.0;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < n; i++)
        {
            double va = a[i];
            double vb = b[i];
            dot += va * vb;
            na += va * va;
            nb += vb * vb;
        }
        if (na == 0 || nb == 0) return 0.0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
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
    /// Regenerate embeddings for all existing documents (useful for fixing missing embeddings)
    /// </summary>
    public async Task<bool> RegenerateAllEmbeddingsAsync()
    {
        try
        {
            Console.WriteLine("[INFO] Starting embedding regeneration for all documents...");
            
            var allDocuments = await documentRepository.GetAllAsync();
            var totalChunks = allDocuments.Sum(d => d.Chunks.Count);
            var processedChunks = 0;
            var successCount = 0;
            
            // Collect all chunks that need embedding regeneration
            var chunksToProcess = new List<DocumentChunk>();
            var documentChunkMap = new Dictionary<DocumentChunk, Document>();
            
            foreach (var document in allDocuments)
            {
                Console.WriteLine($"[INFO] Document: {document.FileName} ({document.Chunks.Count} chunks)");
                
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
            
            Console.WriteLine($"[INFO] Total chunks to process: {chunksToProcess.Count} out of {totalChunks}");
            
            if (chunksToProcess.Count == 0)
            {
                Console.WriteLine("[INFO] All chunks already have valid embeddings. No processing needed.");
                return true;
            }
            
            // Process chunks in batches of 128 (VoyageAI max batch size)
            const int batchSize = 128;
            var totalBatches = (int)Math.Ceiling((double)chunksToProcess.Count / batchSize);
            
            Console.WriteLine($"[INFO] Processing in {totalBatches} batches of {batchSize} chunks");
            
            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var startIndex = batchIndex * batchSize;
                var endIndex = Math.Min(startIndex + batchSize, chunksToProcess.Count);
                var currentBatch = chunksToProcess.Skip(startIndex).Take(endIndex - startIndex).ToList();
                
                Console.WriteLine($"[INFO] Processing batch {batchIndex + 1}/{totalBatches}: chunks {startIndex + 1}-{endIndex}");
                
                // Generate embeddings for current batch
                var batchContents = currentBatch.Select(c => c.Content).ToList();
                var batchEmbeddings = await TryGenerateEmbeddingsBatchAsync(batchContents);
                
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
                            Console.WriteLine($"[DEBUG] Chunk {chunk.Id}: Batch embedding successful ({embedding.Count} dimensions)");
                        }
                        else
                        {
                            Console.WriteLine($"[WARNING] Chunk {chunk.Id}: Batch embedding failed, trying individual generation");
                            
                            // Fallback to individual generation
                            var individualEmbedding = await TryGenerateEmbeddingWithFallback(chunk.Content);
                            if (individualEmbedding != null && individualEmbedding.Count > 0)
                            {
                                chunk.Embedding = individualEmbedding;
                                successCount++;
                                Console.WriteLine($"[DEBUG] Chunk {chunk.Id}: Individual embedding successful ({individualEmbedding.Count} dimensions)");
                            }
                            else
                            {
                                Console.WriteLine($"[WARNING] Chunk {chunk.Id}: All embedding methods failed");
                            }
                        }
                        
                        processedChunks++;
                    }
                }
                else
                {
                    Console.WriteLine($"[WARNING] Batch {batchIndex + 1} failed, processing individually");
                    
                    // Process chunks individually if batch fails
                    foreach (var chunk in currentBatch)
                    {
                        try
                        {
                            var newEmbedding = await TryGenerateEmbeddingWithFallback(chunk.Content);
                            
                            if (newEmbedding != null && newEmbedding.Count > 0)
                            {
                                chunk.Embedding = newEmbedding;
                                successCount++;
                                Console.WriteLine($"[DEBUG] Chunk {chunk.Id}: Individual embedding successful ({newEmbedding.Count} dimensions)");
                            }
                            else
                            {
                                Console.WriteLine($"[WARNING] Chunk {chunk.Id}: Failed to generate embedding");
                            }
                            
                            processedChunks++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Chunk {chunk.Id}: Failed to regenerate embedding: {ex.Message}");
                            processedChunks++;
                        }
                    }
                }
                
                // Progress update
                Console.WriteLine($"[INFO] Progress: {processedChunks}/{chunksToProcess.Count} chunks processed, {successCount} embeddings generated");
                
                // Rate limiting: Wait between batches (VoyageAI 3 RPM limit)
                if (batchIndex < totalBatches - 1) // Don't wait after last batch
                {
                    var waitTime = 20; // 20 seconds for 3 RPM
                    Console.WriteLine($"[INFO] Rate limiting: Waiting {waitTime} seconds before next batch...");
                    await Task.Delay(waitTime * 1000);
                }
            }
            
            // Save all documents with updated embeddings
            var documentsToUpdate = documentChunkMap.Values.Distinct().ToList();
            Console.WriteLine($"[INFO] Saving {documentsToUpdate.Count} documents with updated embeddings...");
            
            foreach (var document in documentsToUpdate)
            {
                await documentRepository.DeleteAsync(document.Id);
                await documentRepository.AddAsync(document);
            }
            
            Console.WriteLine($"[INFO] Embedding regeneration completed. {successCount} embeddings generated for {processedChunks} chunks in {totalBatches} batches.");
            return successCount > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to regenerate embeddings: {ex.Message}");
            return false;
        }
    }

    public async Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(query));

        // Get all documents for cross-document analysis
        var allDocuments = await GetAllDocumentsAsync();

        // Cross-document detection
        var isCrossDocument = IsCrossDocumentQueryAsync(query, allDocuments);

        List<DocumentChunk> relevantChunks;

        // Increase maxResults for better document coverage, but respect user's maxResults
        var adjustedMaxResults = maxResults == 1 ? 1 : Math.Max(maxResults * 2, 5); // Respect maxResults=1, otherwise reasonable increase

        if (isCrossDocument)
        {
            relevantChunks = await PerformCrossDocumentSearchAsync(query, allDocuments, adjustedMaxResults);
        }
        else
        {
            relevantChunks = await PerformStandardSearchAsync(query, adjustedMaxResults);
        }

        // Optimize context assembly: combine chunks intelligently
        var contextMaxResults = isCrossDocument ? Math.Max(maxResults, 3) : maxResults;

        var optimizedChunks = OptimizeContextWindow(relevantChunks, contextMaxResults, query);

        var documentIdToName = new Dictionary<Guid, string>();

        foreach (var docId in optimizedChunks.Select(c => c.DocumentId).Distinct())
        {
            var doc = await GetDocumentAsync(docId);
            if (doc != null)
            {
                documentIdToName[docId] = doc.FileName;
            }
        }

        // Create enhanced context with metadata for better AI understanding
        var enhancedContext = new List<string>();

        foreach (var chunk in optimizedChunks.OrderByDescending(c => c.RelevanceScore ?? 0.0))
        {
            var docName = documentIdToName.TryGetValue(chunk.DocumentId, out var name) ? name : "Document";
            var relevance = chunk.RelevanceScore ?? 0.0;
            var chunkInfo = $"[Document: {docName}, Relevance: {relevance:F3}, Chunk: {chunk.ChunkIndex}]\n{chunk.Content}";
            enhancedContext.Add(chunkInfo);
        }

        var contextText = string.Join("\n\n---\n\n", enhancedContext);

        // Generate RAG answer using AI with enhanced prompt
        var prompt = isCrossDocument
            ? $"You are a precise information retrieval system. Analyze the following context and answer the query step by step.\n\nQuery: {query}\n\nContext:\n{contextText}\n\nInstructions:\n1. Extract specific facts from the context\n2. Answer each part of the query separately\n3. If information is missing, state 'This information is not available in the provided documents'\n4. Use exact quotes from context when possible\n\nAnswer:"
            : $"You are a precise information retrieval system. Analyze the following context and answer the question step by step.\n\nQuestion: {query}\n\nContext:\n{contextText}\n\nInstructions:\n1. Extract specific facts from the context\n2. Answer each part of the question separately\n3. If information is missing, state 'This information is not available in the provided documents'\n4. Use exact quotes from context when possible\n\nAnswer:";

        var answer = await aiService.GenerateResponseAsync(prompt, enhancedContext);

        var sources = optimizedChunks.Select(c => new SearchSource
        {
            DocumentId = c.DocumentId,
            FileName = documentIdToName.TryGetValue(c.DocumentId, out var name) ? name : "Document",
            RelevantContent = c.Content,
            RelevanceScore = c.RelevanceScore ?? 0.0
        }).ToList();

        return new RagResponse
        {
            Query = query,
            Answer = answer,
            Sources = sources,
            SearchedAt = DateTime.UtcNow,
            Configuration = GetRagConfiguration()
        };
    }

    /// <summary>
    /// Applies advanced re-ranking algorithm to improve chunk selection
    /// </summary>
    private static List<DocumentChunk> ApplyReranking(List<DocumentChunk> chunks, string query, int maxResults)
    {
        if (chunks.Count == 0)
            return chunks;

        var queryKeywords = ExtractKeywords(query.ToLowerInvariant());
        var queryLength = query.Length;
        var documentIds = chunks.Select(c => c.DocumentId).Distinct().ToList();

        // Enhanced scoring algorithm
        foreach (var chunk in chunks)
        {
            var originalScore = chunk.RelevanceScore ?? 0.0;
            var enhancedScore = originalScore;

            // Factor 1: Exact keyword matching boost (CRITICAL!)
            var chunkContent = chunk.Content.ToLowerInvariant();
            var exactMatches = 0;

            // Use cleaned keywords (noise/punctuation-safe)
            var cleanedQueryKeywords = ExtractKeywords(query.ToLowerInvariant());
            foreach (var kw in cleanedQueryKeywords)
            {
                if (kw.Length > 2 && chunkContent.Contains(kw))
                {
                    exactMatches++;
                }
            }

            if (cleanedQueryKeywords.Count > 0)
            {
                var exactMatchRatio = (double)exactMatches / cleanedQueryKeywords.Count;
                enhancedScore += exactMatchRatio * 0.6; // 60% boost for exact matches!
            }

            // Additional keyword density boost
            var chunkKeywords = ExtractKeywords(chunkContent);
            var commonKeywords = queryKeywords.Intersect(chunkKeywords, StringComparer.OrdinalIgnoreCase).Count();

            if (queryKeywords.Count > 0)
            {
                var keywordDensity = (double)commonKeywords / queryKeywords.Count;
                enhancedScore += keywordDensity * 0.2; // 20% boost for keyword matches
            }

            // Generic content relevance boost
            var contentBoost = 0.0;

            // Boost for query term matches in content
            var queryTermMatches = queryKeywords.Count(term => chunkContent.Contains(term, StringComparison.OrdinalIgnoreCase));
            contentBoost += Math.Min(0.3, queryTermMatches * 0.1); // Max 30% boost

            enhancedScore += contentBoost;

            // Factor 2: Content length optimization (not too short, not too long)
            var contentLength = chunk.Content.Length;
            var optimalLength = Math.Min(800, Math.Max(200, queryLength * 10)); // Dynamic optimal length
            var lengthScore = 1.0 - Math.Abs(contentLength - optimalLength) / (double)optimalLength;
            enhancedScore += Math.Max(0, lengthScore * 0.15); // 15% boost for optimal length

            // Factor 3: Position in document (earlier chunks often more important)
            var positionBoost = Math.Max(0, 1.0 - (chunk.ChunkIndex * 0.05)); // Decrease by 5% per chunk
            enhancedScore += positionBoost * 0.1; // 10% boost for position

            // Factor 4: Query term proximity (how close query terms are in content)
            var proximityScore = CalculateTermProximity(chunk.Content, queryKeywords);
            enhancedScore += proximityScore * 0.2; // 20% boost for proximity

            // Factor 5: Document diversity boost (NEW!)
            var documentDiversityBoost = CalculateDocumentDiversityBoost(chunk.DocumentId, documentIds, chunks);
            enhancedScore += documentDiversityBoost * 0.15; // 15% boost for diversity

            chunk.RelevanceScore = Math.Min(1.0, enhancedScore); // Cap at 1.0
        }

        return chunks;
    }

    /// <summary>
    /// Calculates how close query terms are to each other in the content
    /// </summary>
    private static double CalculateTermProximity(string content, List<string> queryTerms)
    {
        if (queryTerms.Count == 0) return 0.0;

        var contentLower = content.ToLowerInvariant();
        var termPositions = new List<int>();

        foreach (var term in queryTerms)
        {
            var index = contentLower.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                termPositions.Add(index);
            }
        }

        if (termPositions.Count < 2) return termPositions.Count > 0 ? 0.5 : 0.0;

        // Calculate average distance between terms
        termPositions.Sort();
        var totalDistance = 0;
        for (int i = 1; i < termPositions.Count; i++)
        {
            totalDistance += termPositions[i] - termPositions[i - 1];
        }

        var averageDistance = totalDistance / (termPositions.Count - 1);
        // Closer terms = higher score (inverse relationship)
        return Math.Max(0, 1.0 - averageDistance / 200.0); // Normalize by 200 characters
    }

    /// <summary>
    /// Applies diversity selection to avoid too many chunks from same document
    /// </summary>
    private static List<DocumentChunk> ApplyDiversityAndSelect(List<DocumentChunk> chunks, int maxResults)
    {
        if (chunks.Count == 0) return new List<DocumentChunk>();

        var uniqueDocumentIds = chunks.Select(c => c.DocumentId).Distinct().ToList();

        Console.WriteLine($"[DEBUG] ApplyDiversityAndSelect: Total chunks: {chunks.Count}, Unique documents: {uniqueDocumentIds.Count}");
        Console.WriteLine($"[DEBUG] Document IDs: {string.Join(", ", uniqueDocumentIds.Take(5))}");

        // Calculate min chunks per document - respect maxResults constraint
        var minChunksPerDocument = Math.Max(1, Math.Min(2, Math.Max(1, maxResults / uniqueDocumentIds.Count))); // Min 1, Max 2
        var maxChunksPerDocument = Math.Min(maxResults, Math.Max(minChunksPerDocument, 2)); // Don't exceed maxResults

        Console.WriteLine($"[DEBUG] Min chunks per doc: {minChunksPerDocument}, Max chunks per doc: {maxChunksPerDocument}");

        var selectedChunks = new List<DocumentChunk>();
        var documentChunkCounts = new Dictionary<Guid, int>();

        // First pass: ensure minimum representation from each document, but respect maxResults
        var totalSelected = 0;
        foreach (var documentId in uniqueDocumentIds)
        {
            if (totalSelected >= maxResults) break; // Stop if we've reached maxResults
            
            var availableChunks = chunks.Where(c => c.DocumentId == documentId).ToList();
            var actualMinChunks = Math.Min(minChunksPerDocument, availableChunks.Count);
            
            // Don't exceed maxResults
            var availableSlots = maxResults - totalSelected;
            actualMinChunks = Math.Min(actualMinChunks, availableSlots);

            var documentChunks = availableChunks
                                     .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                                     .Take(actualMinChunks)
                                     .ToList();

            Console.WriteLine($"[DEBUG] Document {documentId}: Available {availableChunks.Count}, Selected {documentChunks.Count} chunks (requested min: {minChunksPerDocument}, actual min: {actualMinChunks})");

            selectedChunks.AddRange(documentChunks);
            documentChunkCounts[documentId] = documentChunks.Count;
            totalSelected += documentChunks.Count;
        }

        // Second pass: fill remaining slots with best remaining chunks, but respect maxResults
        var remainingSlots = maxResults - selectedChunks.Count;
        if (remainingSlots > 0)
        {
            var remainingChunks = chunks.Except(selectedChunks)
                                      .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                                      .ToList();

            foreach (var chunk in remainingChunks)
            {
                if (remainingSlots <= 0 || selectedChunks.Count >= maxResults) break;

                var currentCount = documentChunkCounts.GetValueOrDefault(chunk.DocumentId, 0);
                if (currentCount < maxChunksPerDocument)
                {
                    selectedChunks.Add(chunk);
                    documentChunkCounts[chunk.DocumentId] = currentCount + 1;
                    remainingSlots--;
                }
            }
        }

        // Ensure we don't exceed maxResults
        var finalResult = selectedChunks.Take(maxResults).ToList();

        Console.WriteLine($"[DEBUG] Final result: {finalResult.Count} chunks from {finalResult.Select(c => c.DocumentId).Distinct().Count()} documents (maxResults requested: {maxResults})");

        return finalResult;
    }

    /// <summary>
    /// Performs fuzzy search with typo tolerance
    /// </summary>
    private async Task<List<DocumentChunk>> PerformFuzzySearch(string query, int maxResults)
    {
        var fuzzyResults = new List<DocumentChunk>();

        try
        {
            var allDocs = await documentRepository.GetAllAsync();
            var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var doc in allDocs)
            {
                foreach (var chunk in doc.Chunks)
                {
                    var chunkContent = chunk.Content.ToLowerInvariant();
                    var chunkWords = chunkContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var fuzzyScore = 0.0;
                    var matchedWords = 0;

                    foreach (var queryWord in queryWords)
                    {
                        if (queryWord.Length < 3) continue; // Skip very short words

                        var bestMatch = 0.0;
                        foreach (var chunkWord in chunkWords)
                        {
                            if (chunkWord.Length < 3) continue;

                            // Calculate similarity
                            var similarity = CalculateStringSimilarity(queryWord, chunkWord);
                            if (similarity > bestMatch)
                            {
                                bestMatch = similarity;
                            }
                        }

                        // If similarity is above threshold, count as match
                        if (bestMatch >= 0.7) // 70% similarity threshold
                        {
                            fuzzyScore += bestMatch;
                            matchedWords++;
                        }
                    }

                    // Calculate final fuzzy score
                    if (matchedWords > 0)
                    {
                        var finalScore = (fuzzyScore / queryWords.Length) * 0.8; // Fuzzy matches get 80% of perfect score
                        chunk.RelevanceScore = finalScore;
                        fuzzyResults.Add(chunk);
                    }
                }
            }

            return fuzzyResults
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .Take(maxResults)
                .ToList();
        }
        catch
        {
            return fuzzyResults;
        }
    }

    /// <summary>
    /// Calculates string similarity using Levenshtein distance
    /// </summary>
    private static double CalculateStringSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0.0;

        if (s1 == s2)
            return 1.0;

        var longer = s1.Length > s2.Length ? s1 : s2;
        var shorter = s1.Length > s2.Length ? s2 : s1;

        var editDistance = LevenshteinDistance(longer, shorter);
        return (longer.Length - editDistance) / (double)longer.Length;
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings
    /// </summary>
    private static int LevenshteinDistance(string s1, string s2)
    {
        var len1 = s1.Length;
        var len2 = s2.Length;
        var matrix = new int[len1 + 1, len2 + 1];

        for (int i = 0; i <= len1; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= len2; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= len1; i++)
        {
            for (int j = 1; j <= len2; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[len1, len2];
    }

    /// <summary>
    /// Extracts key words from query for additional search terms
    /// </summary>
    private static List<string> ExtractKeywords(string query)
    {
        var stopWords = new HashSet<string> { "ne", "nedir", "nasıl", "hangi", "kim", "nerede", "ne zaman", "neden",
                                            "what", "how", "where", "when", "why", "who", "which", "is", "are", "the", "a", "an" };

        var words = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !stopWords.Contains(w))
            .ToList();

        return words;
    }

    /// <summary>
    /// Optimizes context window by intelligently selecting and combining chunks
    /// </summary>
    private static List<DocumentChunk> OptimizeContextWindow(List<DocumentChunk> chunks, int maxResults, string query)
    {
        if (chunks.Count == 0) return new List<DocumentChunk>();

        // Group chunks by document for better context
        var documentGroups = chunks.GroupBy(c => c.DocumentId).ToList();

        var finalChunks = new List<DocumentChunk>();
        var remainingSlots = maxResults;

        // Build keyword list from query
        var queryKeywords = ExtractKeywords(query.ToLowerInvariant());
        var targetKeywords = new HashSet<string>(queryKeywords, StringComparer.OrdinalIgnoreCase);

        // Process each document group
        foreach (var group in documentGroups.OrderByDescending(g => g.Max(c => c.RelevanceScore ?? 0.0)))
        {
            if (remainingSlots <= 0) break;

            // Prefer domain keyword matches within the document if available
            var domainMatched = group
                .Select(c => new { Chunk = c, Text = c.Content.ToLowerInvariant() })
                .Where(x => targetKeywords.Any(k => x.Text.Contains(k)))
                .Select(x => x.Chunk)
                .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                .FirstOrDefault();

            var bestChunk = domainMatched ?? group.OrderByDescending(c => c.RelevanceScore ?? 0.0).First();
            finalChunks.Add(bestChunk);
            remainingSlots--;

            // Add additional chunks if slots remain
            if (remainingSlots > 0)
            {
                // Bring in other domain matches first, then top by relevance
                var domainExtras = group
                    .Where(c => !ReferenceEquals(c, bestChunk))
                    .Select(c => new { Chunk = c, Text = c.Content.ToLowerInvariant() })
                    .Where(x => targetKeywords.Any(k => x.Text.Contains(k)))
                    .Select(x => x.Chunk)
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .ToList();

                var nonDomainExtras = group
                    .Where(c => !ReferenceEquals(c, bestChunk) && !domainExtras.Contains(c))
                    .OrderByDescending(c => c.RelevanceScore ?? 0.0)
                    .ToList();

                var extras = domainExtras.Concat(nonDomainExtras)
                    .Take(Math.Min(remainingSlots, 3)) // allow up to 3 extras per doc to improve coverage
                    .ToList();

                finalChunks.AddRange(extras);
                remainingSlots -= extras.Count;
            }
        }

        return finalChunks;
    }

    /// <summary>
    /// Detects if query requires information from multiple documents
    /// </summary>
    private static bool IsCrossDocumentQueryAsync(string query, List<Document> allDocuments)
    {
        if (allDocuments.Count <= 1)
            return false;

        // Extract topics from query
        var queryTopics = ExtractQueryTopics(query);

        var relevantDocs = 0;
        var isCrossDocument = false;

        foreach (var doc in allDocuments)
        {
            var docTopics = ExtractDocumentTopics(doc);
            var matchCount = CalculateTopicMatches(queryTopics, docTopics);

            if (matchCount > 0)
            {
                relevantDocs++;
                if (relevantDocs > 1)
                {
                    isCrossDocument = true;
                    break;
                }
            }
        }

        return isCrossDocument;
    }

    /// <summary>
    /// Calculates topic matches using flexible matching strategies
    /// </summary>
    private static int CalculateTopicMatches(List<string> queryTopics, List<string> docTopics)
    {
        var matchCount = 0.0;

        foreach (var queryTopic in queryTopics)
        {
            // Strategy 1: Exact match
            if (docTopics.Contains(queryTopic, StringComparer.OrdinalIgnoreCase))
            {
                matchCount += 2.0; // Higher weight for exact matches
                continue;
            }

            // Strategy 2: Contains match (partial match)
            var containsMatch = docTopics.Any(dt =>
                dt.Contains(queryTopic, StringComparison.OrdinalIgnoreCase) ||
                queryTopic.Contains(dt, StringComparison.OrdinalIgnoreCase));

            if (containsMatch)
            {
                matchCount += 1.0; // Lower weight for partial matches
                continue;
            }

            // Strategy 3: Word-level match
            var queryWords = queryTopic.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var docWords = docTopics.SelectMany(dt => dt.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToList();

            var wordMatches = queryWords.Count(qw =>
                docWords.Any(dw =>
                    dw.Contains(qw, StringComparison.OrdinalIgnoreCase) ||
                    qw.Contains(dw, StringComparison.OrdinalIgnoreCase)));

            if (wordMatches > 0)
            {
                matchCount += wordMatches * 0.5; // Partial weight for word matches
            }
        }

        return (int)Math.Round(matchCount);
    }

    /// <summary>
    /// Extracts main topics from user query
    /// </summary>
    private static List<string> ExtractQueryTopics(string query)
    {
        var topics = new List<string>();
        var words = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2) // Filter out very short words
            .ToList();

        // Add single words
        topics.AddRange(words);

        // Add bigrams (2-word combinations)
        for (int i = 0; i < words.Count - 1; i++)
        {
            topics.Add($"{words[i]} {words[i + 1]}");
        }

        // Add trigrams (3-word combinations) for better coverage
        for (int i = 0; i < words.Count - 2; i++)
        {
            topics.Add($"{words[i]} {words[i + 1]} {words[i + 2]}");
        }

        // Add individual important words with higher priority
        var importantWords = words.Where(w => w.Length > 4).ToList();
        topics.AddRange(importantWords);

        return topics.Distinct().ToList();
    }

    /// <summary>
    /// Extracts main topics from document content
    /// </summary>
    private static List<string> ExtractDocumentTopics(Document document)
    {
        var topics = new HashSet<string>();
        var content = document.Content.ToLowerInvariant();

        // Extract key phrases from document - increase coverage
        var sentences = content.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);

        // Process more sentences for better topic coverage
        foreach (var sentence in sentences.Take(20)) // Increased from 10 to 20
        {
            var sentenceWords = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            // Add single words
            topics.UnionWith(sentenceWords);

            // Add bigrams
            for (int i = 0; i < sentenceWords.Count - 1; i++)
            {
                topics.Add($"{sentenceWords[i]} {sentenceWords[i + 1]}");
            }

            // Add trigrams
            for (int i = 0; i < sentenceWords.Count - 2; i++)
            {
                topics.Add($"{sentenceWords[i]} {sentenceWords[i + 1]} {sentenceWords[i + 2]}");
            }
        }

        // Also extract from chunk content for better coverage
        foreach (var chunk in document.Chunks.Take(10))
        {
            var chunkWords = chunk.Content.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            topics.UnionWith(chunkWords);
        }

        return topics.Take(50).ToList(); // Increased from 20 to 50
    }

    /// <summary>
    /// Performs cross-document search with enhanced diversity
    /// </summary>
    private async Task<List<DocumentChunk>> PerformCrossDocumentSearchAsync(string query, List<Document> allDocuments, int maxResults)
    {
                    var adjustedMaxResults = maxResults == 1 ? 1 : Math.Max(maxResults, 3); // Respect maxResults=1, otherwise minimum 3

        // Direct search with original query for cross-document
        var searchResults = Math.Max(adjustedMaxResults * 3, options.MaxSearchResults);
        var allChunks = await SearchDocumentsAsync(query, searchResults);

        // Remove duplicates and keep highest score
        var uniqueChunks = allChunks
            .GroupBy(c => c.Id)
            .Select(g => g.OrderByDescending(c => c.RelevanceScore ?? 0.0).First())
            .OrderByDescending(c => c.RelevanceScore ?? 0.0)
            .ToList();

        var rerankedChunks = DocumentService.ApplyReranking(uniqueChunks, query, searchResults);
        var finalChunks = DocumentService.ApplyDiversityAndSelect(rerankedChunks, adjustedMaxResults);

        return finalChunks;
    }

    /// <summary>
    /// Performs standard single-document search
    /// </summary>
    private async Task<List<DocumentChunk>> PerformStandardSearchAsync(string query, int maxResults)
    {
        // Direct search with original query (more reliable)
        var searchResults = Math.Max(maxResults * 2, options.MaxSearchResults);
        var allRelevantChunks = await SearchDocumentsAsync(query, searchResults);

        // Remove duplicates and keep highest score
        var uniqueChunks = allRelevantChunks
            .GroupBy(c => c.Id)
            .Select(g => g.OrderByDescending(c => c.RelevanceScore ?? 0.0).First())
            .OrderByDescending(c => c.RelevanceScore ?? 0.0)
            .ToList();

        // Apply advanced re-ranking algorithm
        var rerankedChunks = DocumentService.ApplyReranking(uniqueChunks, query, searchResults);

        // Apply standard diversity selection
        return DocumentService.ApplyDiversityAndSelect(rerankedChunks, maxResults);
    }

    /// <summary>
    /// Get current RAG configuration dynamically from Program.cs and appsettings.json
    /// </summary>
    private RagConfiguration GetRagConfiguration()
    {
        // Read from Program.cs configuration and appsettings.json
        var currentProvider = GetCurrentAIProviderFromConfig();

        return new RagConfiguration
        {
            AIProvider = currentProvider,
            StorageProvider = GetCurrentStorageProviderFromConfig(),
            Model = GetCurrentModelFromConfig(currentProvider)
        };
    }

    /// <summary>
    /// Get current AI provider from SmartRagOptions configuration
    /// </summary>
    private string GetCurrentAIProviderFromConfig()
    {
        // Use the configured AI provider from SmartRagOptions
        return options.AIProvider.ToString();
    }

    /// <summary>
    /// Get current storage provider from SmartRagOptions configuration
    /// </summary>
    private string GetCurrentStorageProviderFromConfig()
    {
        // Use the configured storage provider from SmartRagOptions
        return options.StorageProvider.ToString();
    }

    /// <summary>
    /// Get current model from configuration based on provider
    /// </summary>
    private string GetCurrentModelFromConfig(string provider)
    {
        // Dynamically build configuration key from provider name
        var configKey = $"AI:{provider}:Model";
        return configuration[configKey] ?? "model-not-configured";
    }

    /// <summary>
    /// Calculates diversity boost to encourage selection from different documents
    /// </summary>
    private static double CalculateDocumentDiversityBoost(Guid documentId, List<Guid> allDocumentIds, List<DocumentChunk> allChunks)
    {
        if (allDocumentIds.Count <= 1) return 0.0;

        // Calculate how many chunks we already have from this document
        var chunksFromThisDoc = allChunks.Count(c => c.DocumentId == documentId);
        var totalChunks = allChunks.Count;

        // If this document is underrepresented, give it a boost
        var expectedChunksPerDoc = (double)totalChunks / allDocumentIds.Count;
        var representationRatio = chunksFromThisDoc / expectedChunksPerDoc;

        // Boost underrepresented documents
        if (representationRatio < 0.8) return 0.3; // 30% boost
        if (representationRatio < 1.0) return 0.15; // 15% boost

        return 0.0; // No boost for overrepresented documents
    }
}