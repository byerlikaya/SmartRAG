using SmartRAG.Entities;
using SmartRAG.Interfaces.Document;
using SmartRAG.Interfaces.Support;
using SmartRAG.Services.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmartRAG.Services.Document
{
    /// <summary>
    /// Service for scoring document chunks
    /// </summary>
    public class DocumentScoringService : IDocumentScoringService
    {
        private const double FullNameMatchScoreBoost = 200.0;
        private const double PartialNameMatchScoreBoost = 100.0;
        private const double WordMatchScore = 5.0; // Increased from 2.0 for better relevance
        private const double MultipleWordMatchBonus = 20.0; // Bonus for chunks matching 3+ query words
        private const double WordCountScoreBoost = 5.0;
        private const double PunctuationScoreBoost = 2.0;
        private const double NumberScoreBoost = 2.0;
        private const double NumberedListScoreBoost = 50.0; // High bonus for numbered lists (for counting questions)
        private const double NumberedListItemBonus = 10.0; // Additional bonus per numbered item
        private const double TitlePatternBonus = 15.0; // Bonus for chunks that look like titles/headings

        private const int WordCountMin = 10;
        private const int WordCountMax = 100;
        private const int PunctuationCountThreshold = 3;
        private const int NumberCountThreshold = 2;
        private const int MinPotentialNamesCount = 2;
        private const int ChunkPreviewLength = 100;
        private const double DefaultScoreValue = 0.0;

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

                if (potentialNames.Count >= MinPotentialNamesCount)
                {
                    var fullName = string.Join(" ", potentialNames);
                    if (_textNormalizationService.ContainsNormalizedName(content, fullName))
                    {
                        score += FullNameMatchScoreBoost;
                        ServiceLogMessages.LogFullNameMatch(_logger, _textNormalizationService.SanitizeForLog(fullName), chunk.Content[..Math.Min(ChunkPreviewLength, chunk.Content.Length)], null);
                    }
                    else if (potentialNames.Any(name => _textNormalizationService.ContainsNormalizedName(content, name)))
                    {
                        score += PartialNameMatchScoreBoost;
                        var foundNames = potentialNames.Where(name => _textNormalizationService.ContainsNormalizedName(content, name)).ToList();
                        ServiceLogMessages.LogPartialNameMatches(_logger, string.Join(", ", foundNames.Select(_textNormalizationService.SanitizeForLog)), chunk.Content[..Math.Min(ChunkPreviewLength, chunk.Content.Length)], null);
                    }
                }

                var matchedWords = 0;
                foreach (var word in queryWords)
                {
                    var wordLower = word.ToLowerInvariant();
                    var contentLower = content.ToLowerInvariant();
                    var wordMatched = false;

                    if (contentLower.Contains(wordLower))
                    {
                        score += WordMatchScore;
                        matchedWords++;
                        wordMatched = true;
                    }
                    else if (wordLower.Length >= 4) // Only for words 4+ chars to avoid false matches
                    {
                        for (int len = Math.Min(wordLower.Length, 8); len >= 4; len--)
                        {
                            for (int start = 0; start <= wordLower.Length - len; start++)
                            {
                                var substring = wordLower.Substring(start, len);
                                if (contentLower.Contains(substring))
                                {
                                    score += WordMatchScore * 0.5; // Partial match, lower score
                                    matchedWords++;
                                    wordMatched = true;
                                    break; // Found a match, no need to check shorter substrings
                                }
                            }
                            if (wordMatched) break;
                        }
                    }
                }

                if (matchedWords >= 3)
                {
                    score += MultipleWordMatchBonus;
                }
                else if (matchedWords >= 2)
                {
                    score += MultipleWordMatchBonus * 0.5; // Half bonus for 2 matches
                }

                var isTitleLike = chunk.Content.Length < 200 &&
                    (chunk.Content.Contains(':') ||
                     chunk.Content.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length <= 3);
                if (isTitleLike && matchedWords > 0)
                {
                    score += TitlePatternBonus;
                }

                var wordCount = content.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
                if (wordCount >= WordCountMin && wordCount <= WordCountMax) score += WordCountScoreBoost;

                var punctuationCount = content.Count(c => ".,;:!?()[]{}".Contains(c));
                if (punctuationCount >= PunctuationCountThreshold) score += PunctuationScoreBoost;

                var numberCount = content.Count(c => char.IsDigit(c));
                if (numberCount >= NumberCountThreshold) score += NumberScoreBoost;

                var numberedListPatterns = new[]
                {
                    @"\b\d+\.\s",      // "1. Item"
                    @"\b\d+\)\s",      // "1) Item"
                    @"\b\d+-\s",       // "1- Item"
                    @"\b\d+\s+[A-Z]",  // "1 Item" (number followed by capital letter)
                    @"^\d+\.\s",       // "1. Item" at start of line
                };

                var numberedListCount = numberedListPatterns.Sum(pattern =>
                    Regex.Matches(chunk.Content, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase).Count);

                if (numberedListCount > 0)
                {
                    score += NumberedListScoreBoost + (numberedListCount * NumberedListItemBonus);
                }

                chunk.RelevanceScore = score;
                return chunk;
            }).ToList();
        }
    }
}

