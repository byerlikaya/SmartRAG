#nullable enable

using SmartRAG.Interfaces.Support;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartRAG.Services.Support
{
    /// <summary>
    /// Service for text normalization operations
    /// </summary>
    public class TextNormalizationService : ITextNormalizationService
    {
        /// <summary>
        /// Normalizes text for better search matching (handles Unicode encoding issues)
        /// Uses Unicode normalization to handle character variations for all languages generically
        /// </summary>
        public string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Decode Unicode escape sequences
            var decoded = Regex.Unescape(text);

            // Normalize Unicode characters using FormC (Canonical Composition)
            // This handles character variations for all languages generically without hardcoded mappings
            var normalized = decoded.Normalize(System.Text.NormalizationForm.FormC);

            return normalized;
        }

        /// <summary>
        /// Normalizes text for matching purposes (removes control characters and normalizes whitespace)
        /// </summary>
        public string NormalizeForMatching(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = Regex.Replace(value, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);
            normalized = Regex.Replace(normalized, @"\s+", " ");
            return normalized.Trim();
        }

        /// <summary>
        /// Checks if content contains normalized name (handles encoding issues)
        /// </summary>
        public bool ContainsNormalizedName(string content, string searchName)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(searchName))
                return false;

            var normalizedContent = NormalizeText(content);
            var normalizedSearchName = NormalizeText(searchName);

            // Try exact match first
            if (normalizedContent.ToLowerInvariant().Contains(normalizedSearchName.ToLowerInvariant()))
                return true;

            // Try partial matches for each word
            var searchWords = normalizedSearchName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var contentWords = normalizedContent.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Check if all search words are present in content
            return searchWords.All(searchWord =>
                contentWords.Any(contentWord =>
                    contentWord.ToLowerInvariant().Contains(searchWord.ToLowerInvariant())));
        }

        /// <summary>
        /// Sanitizes user input for safe logging by removing control characters and limiting length.
        /// Prevents log injection attacks by removing newlines, carriage returns, and other control characters.
        /// </summary>
        public string SanitizeForLog(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            const int maxLogLength = 500;

            // Remove control characters (including newlines, carriage returns, tabs, etc.)
            var sanitized = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                // Allow printable characters and common whitespace (space only)
                if (!char.IsControl(c) || c == ' ')
                {
                    sanitized.Append(c);
                }
            }

            var result = sanitized.ToString();

            // Limit length to prevent log flooding
            if (result.Length > maxLogLength)
            {
                result = result.Substring(0, maxLogLength) + "... (truncated)";
            }

            return result;
        }
    }
}

