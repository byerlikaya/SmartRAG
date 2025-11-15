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
        /// Tokenizes a query into words, filtering by minimum length
        /// </summary>
        /// <param name="query">Query to tokenize</param>
        /// <returns>List of tokenized words</returns>
        public static List<string> TokenizeQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<string>();
            }

            return query.ToLowerInvariant()
                .Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > MinWordLength)
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

            return query.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > MinWordLength && char.IsUpper(w[0]))
                .ToList();
        }
    }
}

