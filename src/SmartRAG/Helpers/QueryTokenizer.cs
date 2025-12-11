using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartRAG.Helpers
{
    /// <summary>
    /// Helper class for tokenizing queries
    /// </summary>
    public static class QueryTokenizer
    {
        private const int MinWordLength = 2;

        /// <summary>
        /// Tokenizes a query into words, filtering by minimum length.
        /// For agglutinative languages, also extracts root words from suffixed words to improve matching.
        /// </summary>
        /// <param name="query">Query to tokenize</param>
        /// <returns>List of tokenized words including root words</returns>
        public static List<string> TokenizeQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<string>();
            }

            var words = query.ToLowerInvariant()
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > MinWordLength)
                .ToList();

            var expandedWords = new HashSet<string>(words);
            foreach (var word in words)
            {
                if (word.Length >= 6)
                {
                    for (int suffixLen = 2; suffixLen <= Math.Min(4, word.Length - 4); suffixLen++)
                    {
                        var potentialRoot = word[..^suffixLen];
                        if (potentialRoot.Length >= 4)
                        {
                            expandedWords.Add(potentialRoot);
                        }
                    }
                }
            }

            return expandedWords.ToList();
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
                .Where(w => w.Length > MinWordLength && char.IsUpper(w[0]))
                .ToList();
        }
    }
}

