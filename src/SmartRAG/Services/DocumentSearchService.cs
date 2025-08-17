using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartRAG.Entities;
using SmartRAG.Enums;
using SmartRAG.Factories;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using System.Text.Json;

namespace SmartRAG.Services;

public class DocumentSearchService(
    IDocumentRepository documentRepository,
    IAIService aiService,
    IAIProviderFactory aiProviderFactory,
    IConfiguration configuration,
    SmartRagOptions options,
    ILogger<DocumentSearchService> logger) : IDocumentSearchService
{
    public async Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(query));

        // Use our integrated search algorithm with diversity selection
        var searchResults = await PerformBasicSearchAsync(query, maxResults * 2);

        if (searchResults.Count > 0)
        {
            logger.LogDebug("Search returned {ChunkCount} chunks from {DocumentCount} documents",
                searchResults.Count, searchResults.Select(c => c.DocumentId).Distinct().Count());

            // Apply diversity selection to ensure chunks from different documents
            var diverseResults = ApplyDiversityAndSelect(searchResults, maxResults);

            logger.LogDebug("Final diverse results: {ResultCount} chunks from {DocumentCount} documents",
                diverseResults.Count, diverseResults.Select(c => c.DocumentId).Distinct().Count());

            return diverseResults;
        }

        return searchResults;
    }

    public async Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(query));

        // Check if this is a general conversation query
        if (IsGeneralConversationQuery(query))
        {
            logger.LogDebug("Detected general conversation query, handling without document search");
            var chatResponse = await HandleGeneralConversationAsync(query);
            return new RagResponse
            {
                Answer = chatResponse,
                Sources = new List<SearchSource>(),
                SearchedAt = DateTime.UtcNow,
                Configuration = GetRagConfiguration()
            };
        }

        // Document search query - use our integrated RAG implementation
        return await GenerateBasicRagAnswerAsync(query, maxResults);
    }

    public async Task<List<float>?> GenerateEmbeddingWithFallbackAsync(string text)
    {
        try
        {
            logger.LogDebug("Trying primary AI service for embedding generation");
            var result = await aiService.GenerateEmbeddingsAsync(text);
            if (result != null && result.Count > 0)
            {
                logger.LogDebug("Primary AI service successful: {Dimensions} dimensions", result.Count);
                return result;
            }
            logger.LogDebug("Primary AI service returned null or empty embedding");
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Primary AI service failed");
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
                logger.LogDebug("Trying {Provider} provider for embedding generation", provider);
                var providerEnum = Enum.Parse<AIProvider>(provider);
                var aiProvider = ((AIProviderFactory)aiProviderFactory).CreateProvider(providerEnum);
                var providerConfig = configuration.GetSection($"AI:{provider}").Get<AIProviderConfig>();

                if (providerConfig != null && !string.IsNullOrEmpty(providerConfig.ApiKey))
                {
                    logger.LogDebug("{Provider} config found, API key: {ApiKeyPreview}...",
                        provider, providerConfig.ApiKey.Substring(0, 8));
                    var embedding = await aiProvider.GenerateEmbeddingAsync(text, providerConfig);
                    if (embedding != null && embedding.Count > 0)
                    {
                        logger.LogDebug("{Provider} successful: {Dimensions} dimensions", provider, embedding.Count);
                        return embedding;
                    }
                    else
                    {
                        logger.LogDebug("{Provider} returned null or empty embedding", provider);
                    }
                }
                else
                {
                    logger.LogDebug("{Provider} config not found or API key missing", provider);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "{Provider} provider failed", provider);
                continue;
            }
        }

        logger.LogDebug("All embedding providers failed for text: {TextPreview}...",
            text.Substring(0, Math.Min(50, text.Length)));

        // Special test for VoyageAI if Anthropic is configured
        try
        {
            var anthropicConfig = configuration.GetSection("AI:Anthropic").Get<AIProviderConfig>();
            if (anthropicConfig != null && !string.IsNullOrEmpty(anthropicConfig.EmbeddingApiKey))
            {
                logger.LogDebug("Testing VoyageAI directly with key: {ApiKeyPreview}...",
                    anthropicConfig.EmbeddingApiKey.Substring(0, 8));

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {anthropicConfig.EmbeddingApiKey}");

                var testPayload = new
                {
                    input = new[] { text },
                    model = anthropicConfig.EmbeddingModel ?? "voyage-3.5",
                    input_type = "document"
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(testPayload);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.voyageai.com/v1/embeddings", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                logger.LogDebug("VoyageAI test response: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogDebug("VoyageAI is working! Trying to parse embedding...");
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
                                logger.LogDebug("VoyageAI test embedding generated: {Dimensions} dimensions", testEmbedding.Count);
                                return testEmbedding;
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        logger.LogDebug(parseEx, "Failed to parse VoyageAI response");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "VoyageAI direct test failed");
        }

        return null;
    }

    public async Task<List<List<float>>?> GenerateEmbeddingsBatchAsync(List<string> texts)
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
                    Console.WriteLine($"[DEBUG] Processing VoyageAI batch {i / rateLimitBatchSize + 1}: chunks {i + 1}-{Math.Min(i + rateLimitBatchSize, texts.Count)}");

                    // Generate embeddings for current batch using VoyageAI
                    var batchEmbeddings = await GenerateVoyageAIBatchAsync(currentBatch, anthropicConfig);

                    if (batchEmbeddings != null && batchEmbeddings.Count == currentBatch.Count)
                    {
                        allEmbeddings.AddRange(batchEmbeddings);
                        Console.WriteLine($"[DEBUG] VoyageAI batch {i / rateLimitBatchSize + 1} successful: {batchEmbeddings.Count} embeddings");
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] VoyageAI batch {i / rateLimitBatchSize + 1} failed, using individual fallback");
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
                            // No delay needed for 2000 RPM
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
        logger.LogDebug("Falling back to individual embedding generation for {ChunkCount} chunks", texts.Count);
        var embeddingTasks = texts.Select(async text => await GenerateEmbeddingWithFallbackAsync(text)).ToList();
        var embeddings = await Task.WhenAll(embeddingTasks);

        return embeddings.Where(e => e != null).Select(e => e!).ToList();
    }

    #region Private Helper Methods

    /// <summary>
    /// Enhanced search with intelligent filtering and name detection
    /// </summary>
    private async Task<List<DocumentChunk>> PerformBasicSearchAsync(string query, int maxResults)
    {
        var allDocuments = await documentRepository.GetAllAsync();
        var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();

        logger.LogDebug("PerformBasicSearchAsync: Searching in {DocumentCount} documents with {ChunkCount} chunks",
            allDocuments.Count, allChunks.Count);

        // Try embedding-based search first if available
        try
        {
            var embeddingResults = await TryEmbeddingBasedSearchAsync(query, allChunks, maxResults);
            if (embeddingResults.Count > 0)
            {
                logger.LogDebug("PerformBasicSearchAsync: Embedding search successful, found {ChunkCount} chunks",
                    embeddingResults.Count);
                return embeddingResults;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "PerformBasicSearchAsync: Embedding search failed, using keyword search");
        }

        // Enhanced keyword-based fallback for global content
        var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToList();

        // Extract potential names from ORIGINAL query (not lowercase) - language agnostic
        var potentialNames = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && char.IsUpper(w[0]))
            .ToList();

        logger.LogDebug("PerformBasicSearchAsync: Query words: [{QueryWords}]", string.Join(", ", queryWords));
        logger.LogDebug("PerformBasicSearchAsync: Potential names: [{PotentialNames}]", string.Join(", ", potentialNames));

        var scoredChunks = allChunks.Select(chunk =>
        {
            var score = 0.0;
            var content = chunk.Content.ToLowerInvariant();

            // Special handling for names like "John Smith" - HIGHEST PRIORITY (language agnostic)
            if (potentialNames.Count >= 2)
            {
                var fullName = string.Join(" ", potentialNames);
                if (ContainsNormalizedName(content, fullName))
                {
                    score += 200.0; // Very high weight for full name matches
                    logger.LogDebug("PerformBasicSearchAsync: Found FULL NAME match: '{FullName}' in chunk: {ChunkPreview}...",
                        fullName, chunk.Content.Substring(0, Math.Min(100, chunk.Content.Length)));
                }
                else if (potentialNames.Any(name => ContainsNormalizedName(content, name)))
                {
                    score += 100.0; // High weight for partial name matches
                    var foundNames = potentialNames.Where(name => ContainsNormalizedName(content, name)).ToList();
                    logger.LogDebug("PerformBasicSearchAsync: Found PARTIAL name matches: [{FoundNames}] in chunk: {ChunkPreview}...",
                        string.Join(", ", foundNames), chunk.Content.Substring(0, Math.Min(100, chunk.Content.Length)));
                }
            }

            // Exact word matches
            foreach (var word in queryWords)
            {
                if (content.Contains(word, StringComparison.OrdinalIgnoreCase))
                    score += 2.0; // Higher weight for word matches
            }

            // Generic content quality scoring (language and content agnostic)
            var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount >= 10 && wordCount <= 100) score += 5.0;

            // Bonus for chunks with punctuation (indicates structured content)
            var punctuationCount = content.Count(c => ".,;:!?()[]{}".Contains(c));
            if (punctuationCount >= 3) score += 2.0;

            // Bonus for chunks with numbers (often indicates factual information)
            var numberCount = content.Count(c => char.IsDigit(c));
            if (numberCount >= 2) score += 2.0;

            chunk.RelevanceScore = score;
            return chunk;
        }).ToList();

        var relevantChunks = scoredChunks
            .Where(c => c.RelevanceScore > 0)
            .OrderByDescending(c => c.RelevanceScore)
            .Take(Math.Max(maxResults * 3, 30))
            .ToList();

        logger.LogDebug("PerformBasicSearchAsync: Found {ChunkCount} relevant chunks with enhanced search",
            relevantChunks.Count);

        // If we found chunks with names, prioritize them
        if (potentialNames.Count >= 2)
        {
            var nameChunks = relevantChunks.Where(c =>
                potentialNames.Any(name => c.Content.Contains(name, StringComparison.OrdinalIgnoreCase))).ToList();

            if (nameChunks.Count > 0)
            {
                logger.LogDebug("PerformBasicSearchAsync: Found {NameChunkCount} chunks containing names, prioritizing them",
                    nameChunks.Count);
                return nameChunks.Take(maxResults).ToList();
            }
        }

        return relevantChunks.Take(maxResults).ToList();
    }

    private async Task<RagResponse> GenerateBasicRagAnswerAsync(string query, int maxResults)
    {
        var chunks = await SearchDocumentsAsync(query, maxResults);
        var context = string.Join("\n\n", chunks.Select(c => c.Content));
        var answer = await aiService.GenerateResponseAsync($"Question: {query}\n\nContext: {context}\n\nAnswer:", new List<string> { context });

        return new RagResponse
        {
            Query = query,
            Answer = answer,
            Sources = chunks.Select(c => new SearchSource
            {
                DocumentId = c.DocumentId,
                FileName = "Document",
                RelevantContent = c.Content,
                RelevanceScore = c.RelevanceScore ?? 0.0
            }).ToList(),
            SearchedAt = DateTime.UtcNow,
            Configuration = GetRagConfiguration()
        };
    }

    private static List<DocumentChunk> ApplyDiversityAndSelect(List<DocumentChunk> chunks, int maxResults)
    {
        return chunks.Take(maxResults).ToList();
    }

    private async Task<List<List<float>>?> GenerateVoyageAIBatchAsync(List<string> texts, AIProviderConfig config)
    {
        // VoyageAI batch işlemi için basit implementasyon
        var results = new List<List<float>>();
        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingWithFallbackAsync(text);
            if (embedding != null)
                results.Add(embedding);
        }
        return results;
    }

    private async Task<List<List<float>>> GenerateIndividualEmbeddingsAsync(List<string> texts)
    {
        var results = new List<List<float>>();
        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingWithFallbackAsync(text);
            results.Add(embedding ?? new List<float>());
        }
        return results;
    }

    private RagConfiguration GetRagConfiguration()
    {
        return new RagConfiguration
        {
            AIProvider = options.AIProvider.ToString(),
            StorageProvider = options.StorageProvider.ToString(),
            Model = configuration["AI:OpenAI:Model"] ?? "gpt-3.5-turbo"
        };
    }

    /// <summary>
    /// Try embedding-based search using VoyageAI with intelligent filtering
    /// </summary>
    private async Task<List<DocumentChunk>> TryEmbeddingBasedSearchAsync(string query, List<DocumentChunk> allChunks, int maxResults)
    {
        try
        {
            var anthropicConfig = configuration.GetSection("AI:Anthropic").Get<AIProviderConfig>();
            if (anthropicConfig == null || string.IsNullOrEmpty(anthropicConfig.EmbeddingApiKey))
            {
                logger.LogDebug("Embedding search: No VoyageAI API key found");
                return new List<DocumentChunk>();
            }

            var aiProvider = ((AIProviderFactory)aiProviderFactory).CreateProvider(AIProvider.Anthropic);

            // Generate embedding for query with retry logic
            var queryEmbedding = await GenerateEmbeddingWithRetryAsync(query, anthropicConfig);
            if (queryEmbedding == null || queryEmbedding.Count == 0)
            {
                logger.LogDebug("Embedding search: Failed to generate query embedding");
                return new List<DocumentChunk>();
            }

            // Calculate similarity for all chunks
            var scoredChunks = allChunks.Select(chunk =>
            {
                var similarity = 0.0;
                if (chunk.Embedding != null && chunk.Embedding.Count > 0)
                {
                    similarity = CalculateCosineSimilarity(queryEmbedding, chunk.Embedding);
                }

                chunk.RelevanceScore = similarity;
                return chunk;
            }).ToList();

            // INTELLIGENT FILTERING: Focus on chunks that actually contain the query terms
            var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            // Extract potential names from ORIGINAL query (not lowercase) - language agnostic
            var potentialNames = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && char.IsUpper(w[0]))
                .ToList();

            // Filter chunks that actually contain query terms
            var relevantChunks = scoredChunks.Where(chunk =>
            {
                var content = chunk.Content.ToLowerInvariant();

                // Must contain at least one query word
                var hasQueryWord = queryWords.Any(word => content.Contains(word, StringComparison.OrdinalIgnoreCase));

                // If query has names, prioritize chunks with names
                if (potentialNames.Count >= 2)
                {
                    var fullName = string.Join(" ", potentialNames);
                    var hasFullName = ContainsNormalizedName(content, fullName);
                    var hasPartialName = potentialNames.Any(name => ContainsNormalizedName(content, name));

                    return hasQueryWord && (hasFullName || hasPartialName);
                }

                return hasQueryWord;
            }).ToList();

            logger.LogDebug("Embedding search: Found {ChunkCount} chunks containing query terms", relevantChunks.Count);

            if (relevantChunks.Count == 0)
            {
                logger.LogDebug("Embedding search: No chunks contain query terms, using similarity only");
                relevantChunks = scoredChunks.Where(c => c.RelevanceScore > 0.01).ToList();
            }

            // Sort by relevance score and take top results
            return relevantChunks
                .OrderByDescending(c => c.RelevanceScore)
                .Take(Math.Max(maxResults * 2, 20))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Embedding search failed");
            return new List<DocumentChunk>();
        }
    }

    /// <summary>
    /// Generate embedding with retry logic for rate limiting
    /// </summary>
    private async Task<List<float>?> GenerateEmbeddingWithRetryAsync(string text, AIProviderConfig config)
    {
        var maxRetries = 3;
        var retryDelayMs = 2000;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var aiProvider = ((AIProviderFactory)aiProviderFactory).CreateProvider(AIProvider.Anthropic);
                return await aiProvider.GenerateEmbeddingAsync(text, config);
            }
            catch (Exception ex) when (ex.Message.Contains("TooManyRequests") || ex.Message.Contains("rate limit"))
            {
                if (attempt < maxRetries - 1)
                {
                    var delay = retryDelayMs * (int)Math.Pow(2, attempt);
                    logger.LogDebug("Embedding generation rate limited, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})",
                        delay, attempt + 1, maxRetries);
                    await Task.Delay(delay);
                }
                else
                {
                    logger.LogDebug("Embedding generation rate limited after {MaxRetries} attempts", maxRetries);
                    throw;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Calculate cosine similarity between two vectors
    /// </summary>
    private static double CalculateCosineSimilarity(List<float> a, List<float> b)
    {
        if (a == null || b == null || a.Count == 0 || b.Count == 0) return 0.0;

        var n = Math.Min(a.Count, b.Count);
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

    /// <summary>
    /// Normalize text for better search matching (handles Unicode encoding issues)
    /// </summary>
    private static string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Decode Unicode escape sequences
        var decoded = System.Text.RegularExpressions.Regex.Unescape(text);

        // Normalize Unicode characters
        var normalized = decoded.Normalize(System.Text.NormalizationForm.FormC);

        // Handle common Turkish character variations (can be extended for other languages)
        var characterMappings = new Dictionary<string, string>
        {
            {"ı", "i"}, {"İ", "I"}, {"ğ", "g"}, {"Ğ", "G"},
            {"ü", "u"}, {"Ü", "U"}, {"ş", "s"}, {"Ş", "S"},
            {"ö", "o"}, {"Ö", "O"}, {"ç", "c"}, {"Ç", "C"}
        };

        foreach (var mapping in characterMappings)
        {
            normalized = normalized.Replace(mapping.Key, mapping.Value);
        }

        return normalized;
    }

    /// <summary>
    /// Check if content contains normalized name (handles encoding issues)
    /// </summary>
    private static bool ContainsNormalizedName(string content, string searchName)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(searchName))
            return false;

        var normalizedContent = NormalizeText(content);
        var normalizedSearchName = NormalizeText(searchName);

        // Try exact match first
        if (normalizedContent.Contains(normalizedSearchName, StringComparison.OrdinalIgnoreCase))
            return true;

        // Try partial matches for each word
        var searchWords = normalizedSearchName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var contentWords = normalizedContent.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Check if all search words are present in content
        return searchWords.All(searchWord =>
            contentWords.Any(contentWord =>
                contentWord.Contains(searchWord, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Check if query is a general conversation question (not document search)
    /// </summary>
    private static bool IsGeneralConversationQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;

        // Simple detection: if query has document-like structure, it's document search
        var hasDocumentStructure = query.Any(char.IsDigit) ||
                                query.Contains(':') ||
                                query.Contains('/') ||
                                query.Contains('-') ||
                                query.Length > 50;

        // If it has document structure, it's document search
        // If not, it's general conversation
        return !hasDocumentStructure;
    }

    /// <summary>
    /// Handle general conversation queries
    /// </summary>
    private async Task<string> HandleGeneralConversationAsync(string query)
    {
        try
        {
            var anthropicConfig = configuration.GetSection("AI:Anthropic").Get<AIProviderConfig>();
            if (anthropicConfig == null || string.IsNullOrEmpty(anthropicConfig.ApiKey))
            {
                return "Sorry, I cannot chat right now. Please try again later.";
            }

            var aiProvider = ((AIProviderFactory)aiProviderFactory).CreateProvider(AIProvider.Anthropic);

            var prompt = $@"You are a helpful AI assistant. Answer the user's question naturally and friendly.

User: {query}

Answer:";

            return await aiProvider.GenerateTextAsync(prompt, anthropicConfig);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "General conversation failed");
            return "Sorry, I cannot chat right now. Please try again later.";
        }
    }

    #endregion
}
