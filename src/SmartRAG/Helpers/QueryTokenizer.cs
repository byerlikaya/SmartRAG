using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartRAG.Helpers
{
    /// <summary>
    /// Helper class for tokenizing queries
    /// </summary>
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

        /// <summary>
        /// Extracts potential names from query (words starting with uppercase)
        /// </summary>
        /// <param name="query">Query to extract names from</param>
        /// <returns>List of potential names</returns>
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

        // Intentionally no stemming logic here; language-specific normalization should be handled
        // by higher-level services (for example via AI-based analysis) to remain language-agnostic.
    }
}

