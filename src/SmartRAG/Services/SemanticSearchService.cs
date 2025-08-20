using Microsoft.Extensions.Logging;

namespace SmartRAG.Services;

/// <summary>
/// Enhanced semantic search service for improved search relevance
/// </summary>
public class SemanticSearchService
{
    private readonly ILogger<SemanticSearchService> _logger;

    public SemanticSearchService(ILogger<SemanticSearchService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculate enhanced semantic similarity using advanced text analysis
    /// </summary>
    public async Task<double> CalculateEnhancedSemanticSimilarityAsync(string query, string content)
    {
        try
        {
            // Use simple text chunking instead of deprecated TextChunker
            var queryTokens = SplitIntoChunks(query, 100);
            var contentTokens = SplitIntoChunks(content, 100);

            if (queryTokens.Count == 0 || contentTokens.Count == 0)
                return 0.0;

            // Calculate semantic similarity using token overlap and semantic analysis
            var similarity = await CalculateTokenBasedSimilarityAsync(queryTokens, contentTokens);
            
            // Apply semantic enhancement factors
            var enhancedScore = ApplySemanticEnhancement(similarity, query, content);
            
            return Math.Min(enhancedScore, 1.0); // Ensure score is between 0-1
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate enhanced semantic similarity");
            return 0.0;
        }
    }

    /// <summary>
    /// Simple text chunking method
    /// </summary>
    private static List<string> SplitIntoChunks(string text, int maxChunkSize)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();

        var chunks = new List<string>();
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (trimmed.Length > 0)
            {
                if (trimmed.Length <= maxChunkSize)
                {
                    chunks.Add(trimmed);
                }
                else
                {
                    // Split long sentences into smaller chunks
                    for (int i = 0; i < trimmed.Length; i += maxChunkSize)
                    {
                        var chunk = trimmed.Substring(i, Math.Min(maxChunkSize, trimmed.Length - i));
                        chunks.Add(chunk);
                    }
                }
            }
        }

        return chunks;
    }

    /// <summary>
    /// Calculate token-based similarity using text chunking
    /// </summary>
    private static Task<double> CalculateTokenBasedSimilarityAsync(List<string> queryTokens, List<string> contentTokens)
    {
        var queryText = string.Join(" ", queryTokens).ToLowerInvariant();
        var contentText = string.Join(" ", contentTokens).ToLowerInvariant();

        // Calculate Jaccard similarity
        var queryWords = queryText.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var contentWords = contentText.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        var intersection = queryWords.Intersect(contentWords).Count();
        var union = queryWords.Union(contentWords).Count();

        if (union == 0) return Task.FromResult(0.0);
        return Task.FromResult((double)intersection / union);
    }

    /// <summary>
    /// Apply semantic enhancement factors to improve scoring
    /// </summary>
    private static double ApplySemanticEnhancement(double baseScore, string query, string content)
    {
        var enhancement = baseScore;

        // Context relevance enhancement
        if (ContainsContextualKeywords(query, content))
            enhancement *= 1.2;

        // Semantic coherence enhancement
        if (HasSemanticCoherence(query, content))
            enhancement *= 1.15;

        // Domain-specific enhancement removed for SOLID and Generic principles
        // Enhancement factors are now domain-independent and language-agnostic

        return enhancement;
    }

    /// <summary>
    /// Check if content contains contextual keywords from query
    /// </summary>
    private static bool ContainsContextualKeywords(string query, string content)
    {
        var queryWords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var contentLower = content.ToLowerInvariant();

        return queryWords.Any(word => word.Length > 3 && contentLower.Contains(word));
    }

    /// <summary>
    /// Check semantic coherence between query and content
    /// </summary>
    private static bool HasSemanticCoherence(string query, string content)
    {
        // Simple semantic coherence check
        var queryTheme = ExtractTheme(query);
        var contentTheme = ExtractTheme(content);

        return !string.IsNullOrEmpty(queryTheme) && 
               !string.IsNullOrEmpty(contentTheme) && 
               queryTheme.Equals(contentTheme, StringComparison.OrdinalIgnoreCase);
    }



    /// <summary>
    /// Extract theme from text (simplified)
    /// </summary>
    private static string ExtractTheme(string text)
    {
        var words = text.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4)
            .Take(3)
            .ToArray();

        return string.Join(" ", words);
    }
}
