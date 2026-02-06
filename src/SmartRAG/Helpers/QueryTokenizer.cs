using System.Text;
using System.Text.RegularExpressions;

namespace SmartRAG.Helpers;


public static class QueryTokenizer
{
    private const int MinTokenLength = 2;

    /// <summary>
    /// Tokenizes a query into words, filtering by minimum length.
    /// This is a basic tokenization that works with any language.
    /// Language-specific normalization (such as stemming or morphological analysis) should be handled
    /// by higher-level services to remain language-agnostic.
    /// </summary>
    /// <param name="query">Query to tokenize</param>
    /// <returns>List of tokenized words</returns>
    public static List<string> TokenizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<string>();
        }

        var normalizedQuery = query.ToLowerInvariant();
        var tokens = TokenizeByAlphanumericCharacters(normalizedQuery);

        return tokens
            .Where(w => w.Length > MinTokenLength)
            .ToList();
    }

    public static List<string> ExtractPotentialNames(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<string>();
        }

        return query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > MinTokenLength && char.IsUpper(w[0]))
            .ToList();
    }

    /// <summary>
    /// Returns words for phrase extraction including short tokens.
    /// Use for entity phrase extraction; TokenizeQuery filters these out for general matching.
    /// </summary>
    public static List<string> GetWordsForPhraseExtraction(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<string>();
        }

        var tokens = TokenizeByAlphanumericCharacters(query.ToLowerInvariant());
        return tokens.Where(w => w.Length >= 1).ToList();
    }

    private static List<string> TokenizeByAlphanumericCharacters(string text)
    {
        var result = new List<string>();
        var current = new StringBuilder();

        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                current.Append(ch);
            }
            else if (current.Length > 0)
            {
                result.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }

    /// <summary>
    /// Checks if query contains numeric indicators using generic structural patterns
    /// Detects numeric values, percentages, currency symbols without language-specific terms
    /// </summary>
    public static bool ContainsNumericIndicators(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        var hasNumericPattern = Regex.IsMatch(query,
            @"\b\d+\s*[%€$£¥₺]|\b\d+\s+\p{L}{3,}|\b\p{L}{3,}\s+\d+",
            RegexOptions.IgnoreCase);

        if (hasNumericPattern)
            return true;

        var hasPercentageSymbol = query.Contains('%');
        if (hasPercentageSymbol)
            return true;

        var hasCurrencySymbol = query.Any(c => c == '€' || c == '$' || c == '£' || c == '¥' || c == '₺');
        return hasCurrencySymbol;
    }
}


