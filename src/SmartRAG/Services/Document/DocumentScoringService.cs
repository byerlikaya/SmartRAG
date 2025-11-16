using SmartRAG.Entities;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Support;
using SmartRAG.Services.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for scoring document chunks
    /// </summary>
    public class DocumentScoringService : IDocumentScoringService
    {
        // Scoring weights
        private const double FullNameMatchScoreBoost = 200.0;
        private const double PartialNameMatchScoreBoost = 100.0;
        private const double WordMatchScore = 2.0;
        private const double WordCountScoreBoost = 5.0;
        private const double PunctuationScoreBoost = 2.0;
        private const double NumberScoreBoost = 2.0;

        // Thresholds
        private const int WordCountMin = 10;
        private const int WordCountMax = 100;
        private const int PunctuationCountThreshold = 3;
        private const int NumberCountThreshold = 2;
        private const int MinPotentialNamesCount = 2;
        private const int ChunkPreviewLength = 100;

        // Query processing constants
        private const int MinWordLength = 2;
        private const double DefaultScoreValue = 0.0;
        private const double NormalizedScoreMax = 1.0;
        private const int MinQueryWordsCount = 0;

        private readonly ITextNormalizationService _textNormalizationService;
        private readonly Microsoft.Extensions.Logging.ILogger<DocumentScoringService> _logger;

        /// <summary>
        /// Initializes a new instance of the DocumentScoringService
        /// </summary>
        /// <param name="textNormalizationService">Service for text normalization operations</param>
        /// <param name="logger">Logger instance for this service</param>
        public DocumentScoringService(
            ITextNormalizationService textNormalizationService,
            Microsoft.Extensions.Logging.ILogger<DocumentScoringService> logger)
        {
            _textNormalizationService = textNormalizationService;
            _logger = logger;
        }

        /// <summary>
        /// Scores document chunks based on query relevance
        /// </summary>
        public List<DocumentChunk> ScoreChunks(List<DocumentChunk> chunks, string query, List<string> queryWords, List<string> potentialNames)
        {
            return chunks.Select(chunk =>
            {
                var score = DefaultScoreValue;
                var content = chunk.Content.ToLowerInvariant();

                // Special handling for names like "John Smith" - HIGHEST PRIORITY (language agnostic)
                if (potentialNames.Count >= MinPotentialNamesCount)
                {
                    var fullName = string.Join(" ", potentialNames);
                    if (_textNormalizationService.ContainsNormalizedName(content, fullName))
                    {
                        score += FullNameMatchScoreBoost;
                        ServiceLogMessages.LogFullNameMatch(_logger, _textNormalizationService.SanitizeForLog(fullName), chunk.Content.Substring(0, Math.Min(ChunkPreviewLength, chunk.Content.Length)), null);
                    }
                    else if (potentialNames.Any(name => _textNormalizationService.ContainsNormalizedName(content, name)))
                    {
                        score += PartialNameMatchScoreBoost;
                        var foundNames = potentialNames.Where(name => _textNormalizationService.ContainsNormalizedName(content, name)).ToList();
                        ServiceLogMessages.LogPartialNameMatches(_logger, string.Join(", ", foundNames.Select(_textNormalizationService.SanitizeForLog)), chunk.Content.Substring(0, Math.Min(ChunkPreviewLength, chunk.Content.Length)), null);
                    }
                }

                // Exact word matches
                foreach (var word in queryWords)
                {
                    if (content.ToLowerInvariant().Contains(word.ToLowerInvariant()))
                        score += WordMatchScore;
                }

                // Generic content quality scoring (language and content agnostic)
                var wordCount = content.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
                if (wordCount >= WordCountMin && wordCount <= WordCountMax) score += WordCountScoreBoost;

                // Bonus for chunks with punctuation (indicates structured content)
                var punctuationCount = content.Count(c => ".,;:!?()[]{}".Contains(c));
                if (punctuationCount >= PunctuationCountThreshold) score += PunctuationScoreBoost;

                // Bonus for chunks with numbers (often indicates factual information)
                var numberCount = content.Count(c => char.IsDigit(c));
                if (numberCount >= NumberCountThreshold) score += NumberScoreBoost;

                chunk.RelevanceScore = score;
                return chunk;
            }).ToList();
        }

        /// <summary>
        /// Calculates keyword relevance score for hybrid search
        /// </summary>
        public double CalculateKeywordRelevanceScore(string query, string content)
        {
            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(content))
                return DefaultScoreValue;

            var queryWords = query.ToLowerInvariant()
                .Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > MinWordLength)
                .ToList();

            if (queryWords.Count == MinQueryWordsCount)
                return DefaultScoreValue;

            var contentLower = content.ToLowerInvariant();
            var score = DefaultScoreValue;

            foreach (var word in queryWords)
            {
                // Exact word match (highest score)
                if (contentLower.Contains($" {word} ") || contentLower.StartsWith($"{word} ", System.StringComparison.OrdinalIgnoreCase) || contentLower.EndsWith($" {word}", System.StringComparison.OrdinalIgnoreCase))
                {
                    score += WordMatchScore;
                }
                // Partial word match (medium score)
                else if (contentLower.Contains(word))
                {
                    score += WordMatchScore / 2;
                }
            }

            // Normalize score
            return Math.Min(score / queryWords.Count, NormalizedScoreMax);
        }
    }
}

