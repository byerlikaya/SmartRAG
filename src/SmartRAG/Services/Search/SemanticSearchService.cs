using Microsoft.Extensions.Logging;
using SmartRAG.Interfaces.Search;
using SmartRAG.Services.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRAG.Services.Search
{

    /// <summary>
    /// Enhanced semantic search service for improved search relevance
    /// </summary>
    public class SemanticSearchService : ISemanticSearchService
    {
        #region Constants

        private const int DefaultMaxChunkSize = 100;
        private const double ContextRelevanceMultiplier = 1.2;
        private const double SemanticCoherenceMultiplier = 1.15;
        private const int MinContextWordLength = 3;
        private const int MinThemeWordLength = 4;
        private const int MaxThemeWords = 3;

        private static readonly char[] SentenceEndings = new char[] { '.', '!', '?' };

        #endregion

        #region Fields

        private readonly ILogger<SemanticSearchService> _logger;

        #endregion

        #region Constructor

        public SemanticSearchService(ILogger<SemanticSearchService> logger)
        {
            _logger = logger;
        }

        #endregion

        #region Properties

        protected ILogger Logger => _logger;

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculate enhanced semantic similarity using advanced text analysis
        /// </summary>
        public async Task<double> CalculateEnhancedSemanticSimilarityAsync(string query, string content)
        {
            try
            {
                var queryTokens = SplitIntoChunks(query, DefaultMaxChunkSize);
                var contentTokens = SplitIntoChunks(content, DefaultMaxChunkSize);

                if (queryTokens.Count == 0 || contentTokens.Count == 0)
                    return 0.0;

                var similarity = await CalculateTokenBasedSimilarityAsync(queryTokens, contentTokens);

                var enhancedScore = ApplySemanticEnhancement(similarity, query, content);

                return Math.Min(enhancedScore, 1.0); // Ensure score is between 0-1
            }
            catch (Exception ex)
            {
                ServiceLogMessages.LogSemanticSimilarityCalculationError(Logger, ex);
                return 0.0;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Simple text chunking method
        /// </summary>
        private static List<string> SplitIntoChunks(string text, int maxChunkSize)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            var chunks = new List<string>();
            var sentences = text.Split(SentenceEndings, StringSplitOptions.RemoveEmptyEntries);

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

            var queryWords = new HashSet<string>(queryText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            var contentWords = new HashSet<string>(contentText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

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

            if (ContainsContextualKeywords(query, content))
                enhancement *= ContextRelevanceMultiplier;

            if (HasSemanticCoherence(query, content))
                enhancement *= SemanticCoherenceMultiplier;


            return enhancement;
        }

        /// <summary>
        /// Check if content contains contextual keywords from query
        /// </summary>
        private static bool ContainsContextualKeywords(string query, string content)
        {
            var queryWords = query.ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var contentLower = content.ToLowerInvariant();

            return queryWords.Any(word => word.Length > MinContextWordLength && contentLower.Contains(word));
        }

        /// <summary>
        /// Check semantic coherence between query and content
        /// </summary>
        private static bool HasSemanticCoherence(string query, string content)
        {
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
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > MinThemeWordLength)
                .Take(MaxThemeWords)
                .ToArray();

            return string.Join(" ", words);
        }

        #endregion
    }
}
