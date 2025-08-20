using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRAG.Entities;
using SmartRAG.Enums;
using SmartRAG.Factories;
using SmartRAG.Interfaces;
using SmartRAG.Models;
using SmartRAG.Services.Logging;
using System.Text.Json;

namespace SmartRAG.Services;

public class DocumentSearchService(
    IDocumentRepository documentRepository,
    IAIService aiService,
    IAIProviderFactory aiProviderFactory,
    IConfiguration configuration,
    IOptions<SmartRagOptions> options,
    ILogger<DocumentSearchService> logger) : IDocumentSearchService
{
    private readonly SmartRagOptions _options = options.Value;

    /// <summary>
    /// Sanitizes user input for safe logging by removing newlines and carriage returns.
    /// </summary>
    private static string SanitizeForLog(string input)
    {
        if (input == null) return string.Empty;
        return input.Replace("\r", "").Replace("\n", "");
    }
    public async Task<List<DocumentChunk>> SearchDocumentsAsync(string query, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(query));

        // Use our integrated search algorithm with diversity selection
        var searchResults = await PerformBasicSearchAsync(query, maxResults * 2);

        if (searchResults.Count > 0)
        {
            ServiceLogMessages.LogSearchResults(logger, searchResults.Count, searchResults.Select(c => c.DocumentId).Distinct().Count(), null);

            // Apply diversity selection to ensure chunks from different documents
            var diverseResults = ApplyDiversityAndSelect(searchResults, maxResults);

            ServiceLogMessages.LogDiverseResults(logger, diverseResults.Count, diverseResults.Select(c => c.DocumentId).Distinct().Count(), null);

            return diverseResults;
        }

            return searchResults;
    }

    public async Task<RagResponse> GenerateRagAnswerAsync(string query, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(query));

        // Universal approach: Check if documents contain relevant information for the query
        // This approach is language-agnostic and doesn't rely on specific word patterns
        var canAnswerFromDocuments = await CanAnswerFromDocumentsAsync(query);

        if (!canAnswerFromDocuments)
        {
            ServiceLogMessages.LogGeneralConversationQuery(logger, null);
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

    #region Private Helper Methods

    /// <summary>
    /// Enhanced search with intelligent filtering and name detection
    /// </summary>
    private async Task<List<DocumentChunk>> PerformBasicSearchAsync(string query, int maxResults)
    {
        var allDocuments = await documentRepository.GetAllAsync();
        var allChunks = allDocuments.SelectMany(d => d.Chunks).ToList();

        ServiceLogMessages.LogSearchInDocuments(logger, allDocuments.Count, allChunks.Count, null);

        // Try embedding-based search first if available
        try
        {
            var embeddingResults = await TryEmbeddingBasedSearchAsync(query, allChunks, maxResults);
            if (embeddingResults.Count > 0)
            {
                ServiceLogMessages.LogEmbeddingSearchSuccessful(logger, embeddingResults.Count, null);
                return embeddingResults;
            }
        }
        catch (Exception)
        {
            ServiceLogMessages.LogEmbeddingSearchFailed(logger, null);
        }

        // Enhanced keyword-based fallback for global content
        var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToList();

        // Extract potential names from ORIGINAL query (not lowercase) - language agnostic
        var potentialNames = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && char.IsUpper(w[0]))
            .ToList();

        ServiceLogMessages.LogQueryWords(logger, string.Join(", ", queryWords.Select(SanitizeForLog)), null);
        ServiceLogMessages.LogPotentialNames(logger, string.Join(", ", potentialNames.Select(SanitizeForLog)), null);

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
                    ServiceLogMessages.LogFullNameMatch(logger, SanitizeForLog(fullName), chunk.Content.Substring(0, Math.Min(100, chunk.Content.Length)), null);
                }
                else if (potentialNames.Any(name => ContainsNormalizedName(content, name)))
                {
                    score += 100.0; // High weight for partial name matches
                    var foundNames = potentialNames.Where(name => ContainsNormalizedName(content, name)).ToList();
                    ServiceLogMessages.LogPartialNameMatches(logger, string.Join(", ", foundNames.Select(SanitizeForLog)), chunk.Content.Substring(0, Math.Min(100, chunk.Content.Length)), null);
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

        ServiceLogMessages.LogRelevantChunksFound(logger, relevantChunks.Count, null);

        // If we found chunks with names, prioritize them
        if (potentialNames.Count >= 2)
        {
            var nameChunks = relevantChunks.Where(c =>
                potentialNames.Any(name => c.Content.Contains(name, StringComparison.OrdinalIgnoreCase))).ToList();

            if (nameChunks.Count > 0)
            {
                ServiceLogMessages.LogNameChunksFound(logger, nameChunks.Count, null);
                return nameChunks.Take(maxResults).ToList();
            }
        }

        return relevantChunks.Take(maxResults).ToList();
    }

    private async Task<RagResponse> GenerateBasicRagAnswerAsync(string query, int maxResults)
    {
        var chunks = await SearchDocumentsAsync(query, maxResults);
        var context = string.Join("\n\n", chunks.Select(c => c.Content));

        // Enhanced prompt for better AI understanding
        var enhancedPrompt = $@"You are a helpful document analysis assistant. Answer questions based on the provided document context.

IMPORTANT: 
- Carefully analyze the context
- Look for specific information that answers the question
- If you find the information, provide a clear answer
- If you cannot find it, say 'I cannot find this specific information in the provided documents'
- Be precise and use exact information from documents

Question: {query}

Context: {context}

Answer:";

        var answer = await aiService.GenerateResponseAsync(enhancedPrompt, new List<string> { context });

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





    private RagConfiguration GetRagConfiguration()
    {
        return new RagConfiguration
        {
            AIProvider = _options.AIProvider.ToString(),
            StorageProvider = _options.StorageProvider.ToString(),
            Model = configuration["AI:OpenAI:Model"] ?? "gpt-3.5-turbo"
        };
    }

    /// <summary>
    /// Try embedding-based search using configured AI provider
    /// </summary>
    private async Task<List<DocumentChunk>> TryEmbeddingBasedSearchAsync(string query, List<DocumentChunk> allChunks, int maxResults)
    {
        try
        {
            var aiProvider = aiProviderFactory.CreateProvider(_options.AIProvider);
            var providerKey = _options.AIProvider.ToString();
            var providerConfig = configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

            if (providerConfig == null || string.IsNullOrEmpty(providerConfig.ApiKey))
            {
                return new List<DocumentChunk>();
            }

            // Generate embedding for query
            var queryEmbedding = await aiProvider.GenerateEmbeddingAsync(query, providerConfig);
            if (queryEmbedding == null || queryEmbedding.Count == 0)
            {
                return new List<DocumentChunk>();
            }

            // Calculate similarity for all chunks that have embeddings
            var chunksWithEmbeddings = allChunks.Where(c => c.Embedding != null && c.Embedding.Count > 0).ToList();
            
            if (chunksWithEmbeddings.Count == 0)
            {
                return new List<DocumentChunk>();
            }

            var scoredChunks = chunksWithEmbeddings.Select(chunk =>
            {
                var similarity = CalculateCosineSimilarity(queryEmbedding, chunk.Embedding);
                chunk.RelevanceScore = similarity;
                return chunk;
            }).ToList();

            // Filter chunks that contain query terms
            var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            var relevantChunks = scoredChunks.Where(chunk =>
            {
                var content = chunk.Content.ToLowerInvariant();
                return queryWords.Any(word => content.Contains(word, StringComparison.OrdinalIgnoreCase));
            }).ToList();

            if (relevantChunks.Count == 0)
            {
                relevantChunks = scoredChunks.Where(c => c.RelevanceScore > 0.01).ToList();
            }

            return relevantChunks
                .OrderByDescending(c => c.RelevanceScore)
                .Take(Math.Max(maxResults * 2, 20))
                .ToList();
        }
        catch (Exception ex)
        {
            ServiceLogMessages.LogEmbeddingSearchFailedError(logger, ex);
            return new List<DocumentChunk>();
        }
    }



    /// <summary>
    /// Calculate cosine similarity between two vectors
    /// </summary>
    private static double CalculateCosineSimilarity(List<float>? a, List<float>? b)
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
    /// Ultimate language-agnostic approach: ONLY check if documents contain relevant information
    /// No word patterns, no language detection, no grammar analysis, no greeting detection
    /// Pure content-based decision making
    /// </summary>
    private async Task<bool> CanAnswerFromDocumentsAsync(string query)
    {
        try
        {
            // Step 1: Search documents for any content related to the query
            // This works regardless of the language of the query
            var searchResults = await PerformBasicSearchAsync(query, 5);
            
            if (searchResults.Count == 0)
            {
                // No content found that matches the query in any way
                return false;
            }

            // Step 2: Check if we found meaningful content with decent relevance
            var hasRelevantContent = searchResults.Any(chunk => 
                chunk.RelevanceScore > 0.1); // Reasonable threshold

            if (!hasRelevantContent)
            {
                // Found some content but it's not relevant enough
                return false;
            }

            // Step 3: Check if the total content is substantial enough to potentially answer
            var totalContentLength = searchResults
                .Where(c => c.RelevanceScore > 0.1)
                .Sum(c => c.Content.Length);

            var hasSubstantialContent = totalContentLength > 50; // Minimum content threshold

            // Final decision: If we have relevant and substantial content, use document search
            // No other checks - let the content decide!
            return hasRelevantContent && hasSubstantialContent;
        }
        catch (Exception ex)
        {
            // If there's an error, be conservative and assume it's document search
            logger.LogWarning(ex, "Error in CanAnswerFromDocumentsAsync, assuming document search for safety");
            return true;
        }
    }

    /// <summary>
    /// Handle general conversation queries
    /// </summary>
    private async Task<string> HandleGeneralConversationAsync(string query)
    {
        try
        {
            // Use the configured AI provider from options
            var aiProvider = aiProviderFactory.CreateProvider(_options.AIProvider);
            var providerKey = _options.AIProvider.ToString();
            var providerConfig = configuration.GetSection($"AI:{providerKey}").Get<AIProviderConfig>();

            if (providerConfig == null || string.IsNullOrEmpty(providerConfig.ApiKey))
            {
                return "Sorry, I cannot chat right now. Please try again later.";
            }

            var prompt = $@"You are a helpful AI assistant. Answer the user's question naturally and friendly.

User: {query}

Answer:";

            return await aiProvider.GenerateTextAsync(prompt, providerConfig);
        }
        catch (Exception)
        {
            // Log error using structured logging
            return "Sorry, I cannot chat right now. Please try again later.";
        }
    }

    #endregion
}
