using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartRAG.Extensions
{
    /// <summary>
    /// Extension methods for text search normalization and processing
    /// </summary>
    public static class SearchTextExtensions
    {
        private static readonly Regex NonWordRegex = new Regex(@"[^\p{L}\p{Nd}\s]", RegexOptions.Compiled);
        private static readonly Regex MultiSpaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Culture-agnostic normalization for international text search
        /// Uses Unicode normalization and invariant culture for consistent results
        /// </summary>
        /// <param name="input">Input string to normalize</param>
        /// <returns>Normalized string ready for search operations</returns>
        public static string NormalizeForSearch(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var normalized = input.Normalize(NormalizationForm.FormD);
            var lower = normalized.ToLowerInvariant();
            var noPunct = NonWordRegex.Replace(lower, " ");
            var collapsed = MultiSpaceRegex.Replace(noPunct, " ").Trim();
            return collapsed;
        }

        /// <summary>
        /// Normalizes text for fuzzy matching using Unicode decomposition.
        /// Uses canonical decomposition (FormD) and removes combining marks so that
        /// characters with diacritics match their base forms (e.g. e + acute -> e).
        /// Works for all scripts and languages without language-specific mappings.
        /// </summary>
        public static string NormalizeForSearchMatch(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var sb = new StringBuilder(input.Length);
            var formD = input.Normalize(NormalizationForm.FormC).Normalize(NormalizationForm.FormD);

            foreach (var ch in formD)
            {
                if (char.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;

                sb.Append(ch);
            }

            return sb.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// OCR-tolerant normalization for matching user queries against OCR-extracted content.
        /// Handles common OCR confusions in Latin-derived scripts (e.g. U+0131, digit-letter at word boundary).
        /// Use only for fallback text search against Image/OCR content.
        /// </summary>
        public static string NormalizeForOcrTolerantMatch(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var normalized = input.NormalizeForSearchMatch();
            normalized = normalized.Replace('\u0131', 'i');
            normalized = Regex.Replace(normalized, @"(\p{L})1\b", "$1i");
            return normalized;
        }
    }
}
