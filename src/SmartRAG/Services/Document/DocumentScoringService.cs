
namespace SmartRAG.Services.Document;


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
    private const double NumberedListScoreBoost = 50.0; // Bonus for numbered lists (structural pattern, generic)
    private const double NumberedListItemBonus = 10.0; // Additional bonus per numbered item
    private const double TitlePatternBonus = 15.0; // Bonus for chunks that look like titles/headings

    private const int WordCountMin = 10;
    private const int WordCountMax = 100;
    private const int PunctuationCountThreshold = 3;
    private const int MinPotentialNamesCount = 2;
    private const double DefaultScoreValue = 0.0;

    private readonly ITextNormalizationService _textNormalizationService;

    /// <summary>
    /// Initializes a new instance of the DocumentScoringService
    /// </summary>
    /// <param name="textNormalizationService">Service for text normalization operations</param>
    public DocumentScoringService(
        ITextNormalizationService textNormalizationService)
    {
        _textNormalizationService = textNormalizationService;
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
            var fileName = chunk.FileName?.ToLowerInvariant() ?? string.Empty;
            var searchableText = string.Concat(content, " ", fileName);

            if (potentialNames.Count >= MinPotentialNamesCount)
            {
                var fullName = string.Join(" ", potentialNames.Select(n => n.ToLowerInvariant()));
                if (_textNormalizationService.ContainsNormalizedName(searchableText, fullName))
                {
                    score += FullNameMatchScoreBoost;
                }
                else if (potentialNames.Any(name => _textNormalizationService.ContainsNormalizedName(searchableText, name)))
                {
                    score += PartialNameMatchScoreBoost;
                }
            }

            var fileNamePhraseBonus = GetFileNamePhraseBonus(fileName, queryWords, potentialNames);
            score += fileNamePhraseBonus;

            var matchedWords = 0;
            foreach (var word in queryWords)
            {
                var wordLower = word.ToLowerInvariant();
                var wordMatched = false;

                if (searchableText.Contains(wordLower))
                {
                    score += WordMatchScore;
                    matchedWords++;
                    wordMatched = true;
                }
                else if (wordLower.Length >= 4)
                {
                    for (int len = Math.Min(wordLower.Length, 8); len >= 4; len--)
                    {
                        for (int start = 0; start <= wordLower.Length - len; start++)
                        {
                            var substring = wordLower.Substring(start, len);
                            if (searchableText.Contains(substring))
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

            // No content-specific bonuses (prices, numbers, etc.) - only generic structural patterns
            // Relevance is determined by word matches and structural content (numbered lists, titles), not by specific content types

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

    private static double GetFileNamePhraseBonus(string fileNameLower, List<string> queryWords, List<string> potentialNames)
    {
        if (string.IsNullOrWhiteSpace(fileNameLower))
            return 0;

        if (potentialNames != null && potentialNames.Count >= 2)
        {
            var entityPhrase = string.Join(" ", potentialNames.Select(n => n.ToLowerInvariant()));
            if (fileNameLower.Contains(entityPhrase))
                return FullNameMatchScoreBoost;
        }

        if (queryWords == null || queryWords.Count < 2)
            return 0;

        for (int i = 0; i < queryWords.Count - 1; i++)
        {
            var w1 = queryWords[i].ToLowerInvariant();
            var w2 = queryWords[i + 1].ToLowerInvariant();
            if (w1.Length >= 1 && w2.Length >= 3)
            {
                var phrase = $"{w1} {w2}";
                if (fileNameLower.Contains(phrase))
                    return FullNameMatchScoreBoost;
            }
        }
        return 0;
    }
}


