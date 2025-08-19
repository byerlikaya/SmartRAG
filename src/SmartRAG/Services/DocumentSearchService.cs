using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    SmartRagOptions options,
    ILogger<DocumentSearchService> logger) : IDocumentSearchService
{

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

        // Enhanced keyword-based search for global content
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
                if (content.Contains(fullName, StringComparison.OrdinalIgnoreCase))
                {
                    score += 200.0; // Very high weight for full name matches
                    ServiceLogMessages.LogFullNameMatch(logger, SanitizeForLog(fullName), chunk.Content.Substring(0, Math.Min(100, chunk.Content.Length)), null);
                }
                else if (potentialNames.Any(name => content.Contains(name, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 100.0; // High weight for partial name matches
                    var foundNames = potentialNames.Where(name => content.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
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
            AIProvider = options.AIProvider.ToString(),
            StorageProvider = options.StorageProvider.ToString(),
            Model = configuration["AI:OpenAI:Model"] ?? "gpt-3.5-turbo"
        };
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
            var aiProvider = aiProviderFactory.CreateProvider(options.AIProvider);
            var providerKey = options.AIProvider.ToString();
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
