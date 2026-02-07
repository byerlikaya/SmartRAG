
namespace SmartRAG.Services.Document;


/// <summary>
/// Service for analyzing query patterns and detecting numbered lists in document chunks
/// </summary>
public class QueryPatternAnalyzerService : IQueryPatternAnalyzerService
{
    private static readonly Regex NumberedListPattern1 = new(@"\b\d+\.\s", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex NumberedListPattern2 = new (@"\b\d+\)\s", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex NumberedListPattern3 = new (@"\b\d+-\s", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex NumberedListPattern4 = new (@"\b\d+\s+[A-Z]", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex NumberedListPattern5 = new (@"^\d+\.\s", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex[] NumberedListPatterns = {
        NumberedListPattern1,
        NumberedListPattern2,
        NumberedListPattern3,
        NumberedListPattern4,
        NumberedListPattern5
    };

    private static readonly Regex NumericPattern = new (@"\p{Nd}+", RegexOptions.Compiled);
    private static readonly Regex ListIndicatorPattern = new (@"\d+[\.\)]\s", RegexOptions.Compiled);


    /// <summary>
    /// Detects if content contains numbered lists
    /// </summary>
    public bool DetectNumberedLists(string content)
    {
        return !string.IsNullOrWhiteSpace(content) && NumberedListPatterns.Any(pattern => pattern.IsMatch(content));
    }

    /// <summary>
    /// Counts numbered list items in content
    /// </summary>
    public int CountNumberedListItems(string content)
    {
        return string.IsNullOrWhiteSpace(content) ? 0 : NumberedListPatterns.Sum(pattern => pattern.Matches(content).Count);
    }

    /// <summary>
    /// Scores chunks based on numbered list presence and query word matches
    /// </summary>
    public List<DocumentChunk> ScoreChunksByNumberedLists(
        List<DocumentChunk> chunks,
        List<string> queryWords,
        double numberedListBonus,
        double wordMatchBonus)
    {
        if (chunks == null)
        {
            throw new ArgumentNullException(nameof(chunks));
        }

        return chunks.Select(chunk =>
        {
            var baseScore = chunk.RelevanceScore ?? 0.0;
            var numberedListCount = CountNumberedListItems(chunk.Content);
            var wordMatches = queryWords.Count(word =>
                chunk.Content.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

            var bonus = (numberedListCount * numberedListBonus) + (wordMatches * wordMatchBonus);
            chunk.RelevanceScore = baseScore + bonus;

            return chunk;
        }).ToList();
    }

    /// <summary>
    /// Determines if query requires comprehensive search based on pattern analysis
    /// </summary>
    public bool RequiresComprehensiveSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        var trimmed = query.Trim();
        var tokens = trimmed.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // Pattern 1: Question punctuation (works for all languages)
        if (HasQuestionPunctuation(trimmed))
        {
            // Check if it's a counting/listing question by structure
            if (HasNumericPattern(trimmed) || HasListIndicators(trimmed))
            {
                return true;
            }
        }

        // Pattern 2: Numeric patterns (numbers, digits) - often indicates counting questions
        if (HasNumericPattern(trimmed))
        {
            return true;
        }

        // Pattern 3: Query complexity (longer queries often need more context)
        if (tokens.Length >= 6)
        {
            return true;
        }

        // Pattern 4: List indicators (structural patterns that suggest enumeration)
        if (HasListIndicators(trimmed))
        {
            return true;
        }

        return false;
    }

    private static bool HasQuestionPunctuation(string input)
    {
        return input.IndexOf('?', StringComparison.Ordinal) >= 0 ||
               input.IndexOf('¿', StringComparison.Ordinal) >= 0 ||
               input.IndexOf('؟', StringComparison.Ordinal) >= 0;
    }

    private static bool HasNumericPattern(string input)
    {
        if (input.Any(c => char.GetUnicodeCategory(c) == UnicodeCategory.DecimalDigitNumber))
        {
            return true;
        }

        var numericMatches = NumericPattern.Matches(input);
        return numericMatches.Count >= 2;
    }

    private static bool HasListIndicators(string input)
    {
        if (ListIndicatorPattern.IsMatch(input))
        {
            return true;
        }

        var questionCount = input.Count(c => c is '?' or '¿' or '؟');
        return questionCount >= 2;
    }
}


