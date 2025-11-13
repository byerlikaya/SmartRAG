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

            // Normalize Unicode characters (é -> e, ñ -> n, etc.)
            var normalized = input.Normalize(NormalizationForm.FormD);

            // Use invariant culture for consistent international behavior
            var lower = normalized.ToLowerInvariant();
            var noPunct = NonWordRegex.Replace(lower, " ");
            var collapsed = MultiSpaceRegex.Replace(noPunct, " ").Trim();
            return collapsed;
        }
    }
}
